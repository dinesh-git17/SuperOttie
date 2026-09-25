"""Image processing helpers for turning generated art (on white) into game-ready sprites.

Codex/ChatGPT Images cannot emit transparency, so every asset is generated on a flat
white background and cut out here:

* ``remove_white_background`` flood-fills near-white pixels connected to the image border,
  then "un-mixes" the anti-aliased fringe so edges blend cleanly over any colour.
* ``split_components`` finds the individual poses/objects on a sheet.
* ``fit_on_canvas`` scales and pads a cut-out so its pivot lands at bottom-centre.
* ``make_seamless_h`` / ``make_seamless`` cross-fade edges so textures tile.
"""

from __future__ import annotations

from dataclasses import dataclass

import numpy as np
from PIL import Image
from scipy import ndimage


@dataclass(frozen=True)
class Box:
    x0: int
    y0: int
    x1: int
    y1: int  # exclusive

    @property
    def w(self) -> int:
        return self.x1 - self.x0

    @property
    def h(self) -> int:
        return self.y1 - self.y0

    @property
    def cx(self) -> float:
        return (self.x0 + self.x1) / 2

    @property
    def cy(self) -> float:
        return (self.y0 + self.y1) / 2


def load_rgb(path) -> np.ndarray:
    return np.asarray(Image.open(path).convert("RGB")).astype(np.float32)


def whiteness_distance(rgb: np.ndarray) -> np.ndarray:
    """Per-pixel distance from pure white, 0 (white) .. 255 (a fully saturated/dark channel)."""
    return 255.0 - rgb.min(axis=2)


def remove_white_background(
    rgb: np.ndarray,
    hard_tol: float = 18.0,
    soft_range: float = 70.0,
    fill_holes_min_area: int | None = None,
    hole_tol: float = 6.0,
) -> np.ndarray:
    """Return an RGBA float array with the white backdrop removed.

    hard_tol: pixels closer than this to white AND connected to the border are background.
    soft_range: width (in whiteness units) of the anti-aliased band that gets partial alpha.
    fill_holes_min_area: if set, enclosed near-pure-white regions at least this large
        (e.g. the gap between an arm and the body) are also treated as background.
    """
    dist = whiteness_distance(rgb)
    candidate = dist < hard_tol
    labels, _ = ndimage.label(candidate)
    border_ids = np.unique(
        np.concatenate([labels[0, :], labels[-1, :], labels[:, 0], labels[:, -1]])
    )
    border_ids = border_ids[border_ids != 0]
    background = np.isin(labels, border_ids)

    if fill_holes_min_area:
        pure = dist < hole_tol
        hole_labels, n = ndimage.label(pure & ~background)
        if n:
            sizes = ndimage.sum(np.ones_like(dist), hole_labels, index=np.arange(1, n + 1))
            big = np.flatnonzero(sizes >= fill_holes_min_area) + 1
            background |= np.isin(hole_labels, big)

    # Fringe: foreground pixels within a few px of the background get alpha from whiteness.
    near_bg = ndimage.binary_dilation(background, iterations=3) & ~background
    alpha = np.ones_like(dist)
    alpha[background] = 0.0
    fringe_alpha = np.clip((dist - hard_tol * 0.5) / soft_range, 0.0, 1.0)
    alpha[near_bg] = np.minimum(alpha[near_bg], fringe_alpha[near_bg])

    # Un-premultiply the white that was mixed into the fringe: c = (c - (1-a)*255) / a.
    rgba = np.zeros(rgb.shape[:2] + (4,), np.float32)
    a = alpha[..., None]
    safe = np.maximum(a, 1e-3)
    rgba[..., :3] = np.clip((rgb - (1.0 - a) * 255.0) / safe, 0, 255)
    rgba[..., 3] = alpha * 255.0
    rgba[alpha <= 0.0] = 0.0
    return rgba


def split_components(alpha: np.ndarray, min_area: int = 400, merge_px: int = 6) -> list[Box]:
    """Bounding boxes of separate objects, in reading order (rows top->bottom, then left->right)."""
    mask = alpha > 20
    if merge_px:
        mask = ndimage.binary_dilation(mask, iterations=merge_px)
    labels, n = ndimage.label(mask)
    boxes: list[Box] = []
    for i, sl in enumerate(ndimage.find_objects(labels), start=1):
        if sl is None:
            continue
        area = int((labels[sl] == i).sum())
        if area < min_area:
            continue
        y, x = sl
        boxes.append(
            Box(
                max(0, x.start + merge_px),
                max(0, y.start + merge_px),
                min(alpha.shape[1], x.stop - merge_px),
                min(alpha.shape[0], y.stop - merge_px),
            )
        )
    if not boxes:
        return boxes
    # Group into rows: boxes whose vertical centres are within half the median height.
    boxes.sort(key=lambda b: b.cy)
    med_h = float(np.median([b.h for b in boxes]))
    rows: list[list[Box]] = []
    for b in boxes:
        if rows and abs(b.cy - np.mean([r.cy for r in rows[-1]])) < med_h * 0.5:
            rows[-1].append(b)
        else:
            rows.append([b])
    return [b for row in rows for b in sorted(row, key=lambda b: b.cx)]


def crop(rgba: np.ndarray, box: Box, pad: int = 0) -> np.ndarray:
    y0, y1 = max(0, box.y0 - pad), min(rgba.shape[0], box.y1 + pad)
    x0, x1 = max(0, box.x0 - pad), min(rgba.shape[1], box.x1 + pad)
    return rgba[y0:y1, x0:x1]


def tight(rgba: np.ndarray, thresh: int = 8) -> np.ndarray:
    ys, xs = np.nonzero(rgba[..., 3] > thresh)
    if len(xs) == 0:
        return rgba
    return rgba[ys.min() : ys.max() + 1, xs.min() : xs.max() + 1]


def to_image(rgba: np.ndarray) -> Image.Image:
    return Image.fromarray(np.clip(rgba, 0, 255).astype(np.uint8), "RGBA")


def resize(rgba: np.ndarray, scale: float) -> np.ndarray:
    """Premultiplied-alpha Lanczos resize, so transparent pixels don't bleed dark halos."""
    h, w = rgba.shape[:2]
    nw, nh = max(1, round(w * scale)), max(1, round(h * scale))
    return resize_to(rgba, nw, nh)


def resize_to(rgba: np.ndarray, nw: int, nh: int) -> np.ndarray:
    a = rgba[..., 3:4] / 255.0
    premul = np.concatenate([rgba[..., :3] * a, rgba[..., 3:4]], axis=2)
    chans = [
        np.asarray(Image.fromarray(premul[..., c].astype(np.float32), "F").resize((nw, nh), Image.LANCZOS))
        for c in range(4)
    ]
    out = np.stack(chans, axis=2)
    out[..., 3] = np.clip(out[..., 3], 0, 255)
    af = np.maximum(out[..., 3:4] / 255.0, 1e-4)
    out[..., :3] = np.clip(out[..., :3] / af, 0, 255)
    out[out[..., 3] < 1] = 0
    return out


def alpha_centroid_x(rgba: np.ndarray) -> float:
    a = rgba[..., 3]
    cols = a.sum(axis=0)
    return float((cols * np.arange(a.shape[1])).sum() / max(cols.sum(), 1e-6))


def fit_on_canvas(rgba: np.ndarray, canvas_w: int, canvas_h: int, anchor_x: float | None = None) -> np.ndarray:
    """Place a cut-out on a transparent canvas, bottom-aligned, with ``anchor_x`` (default: the
    alpha centroid) at the horizontal centre. Content wider than the canvas is clipped."""
    h, w = rgba.shape[:2]
    ax = alpha_centroid_x(rgba) if anchor_x is None else anchor_x
    out = np.zeros((canvas_h, canvas_w, 4), np.float32)
    ox = int(round(canvas_w / 2 - ax))
    oy = canvas_h - h
    sx0, sy0 = max(0, -ox), max(0, -oy)
    dx0, dy0 = max(0, ox), max(0, oy)
    cw = min(w - sx0, canvas_w - dx0)
    ch = min(h - sy0, canvas_h - dy0)
    out[dy0 : dy0 + ch, dx0 : dx0 + cw] = rgba[sy0 : sy0 + ch, sx0 : sx0 + cw]
    return out


def make_seamless_h(img: np.ndarray, blend: int) -> np.ndarray:
    """Cross-fade the last ``blend`` columns into the first ones; the result tiles horizontally."""
    w = img.shape[1]
    out = img[:, : w - blend].copy()
    t = np.linspace(0.0, 1.0, blend, dtype=np.float32)[None, :, None]
    out[:, :blend] = img[:, w - blend :] * (1 - t) + img[:, :blend] * t
    return out


def make_seamless(img: np.ndarray, blend: int) -> np.ndarray:
    out = make_seamless_h(img, blend)
    return np.transpose(make_seamless_h(np.transpose(out, (1, 0, 2)), blend), (1, 0, 2))
