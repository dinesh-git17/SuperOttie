"""Turns raw Codex/ChatGPT Images output (raw/*.png, white backgrounds) into the game's sprites.

    .venv/bin/python process.py [asset ...]     # default: every asset whose raw file exists

Writes into ~/Develop/SuperOttie/Assets/Art (same names as placeholders.py) and a checkerboard
contact sheet per asset into previews/ for visual QA.
"""

import sys
from pathlib import Path

import numpy as np
from PIL import Image

import artkit as ak

ROOT = Path(__file__).parent
RAW = ROOT / "raw"
PREVIEW = ROOT / "previews"
ART = Path.home() / "Develop/SuperOttie/Assets/Art"
PPU = 128


class SheetError(RuntimeError):
    pass


def save(rgba, rel):
    path = ART / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    ak.to_image(rgba).save(path)
    return path


def checker_preview(name, images):
    """Lay cut-outs on a checkerboard so halos and holes are easy to spot."""
    pad = 16
    h = max(i.shape[0] for i in images) + pad * 2
    w = sum(i.shape[1] + pad for i in images) + pad
    yy, xx = np.mgrid[0:h, 0:w]
    board = np.where(((yy // 16) + (xx // 16)) % 2 == 0, 200, 150).astype(np.float32)
    canvas = np.stack([board, board * 0.9 + 30, board], axis=2)
    x = pad
    for img in images:
        a = img[..., 3:4] / 255.0
        y0 = h - pad - img.shape[0]
        region = canvas[y0 : y0 + img.shape[0], x : x + img.shape[1]]
        region[:] = region * (1 - a) + img[..., :3] * a
        x += img.shape[1] + pad
    PREVIEW.mkdir(exist_ok=True)
    Image.fromarray(canvas.astype(np.uint8)).save(PREVIEW / f"{name}.png")


def cutout_sheet(name, expected, holes=None, merge_px=6, min_area=1500):
    rgb = ak.load_rgb(RAW / f"{name}.png")
    rgba = ak.remove_white_background(rgb, fill_holes_min_area=holes)
    boxes = ak.split_components(rgba[..., 3], min_area=min_area, merge_px=merge_px)
    if len(boxes) != expected:
        raise SheetError(f"{name}: expected {expected} objects, found {len(boxes)}: {boxes}")
    return [ak.tight(ak.crop(rgba, b)) for b in boxes]


def uniform_scale(parts, ref_index, ref_size, axis):
    """One scale for a whole sheet (keeps poses consistent), set by a reference part's height/width."""
    ref = parts[ref_index].shape[0 if axis == "h" else 1]
    return ref_size / ref


# ---------------------------------------------------------------------------------------------

def player_sheet():
    parts = cutout_sheet("player_sheet", 8, holes=900)
    s = uniform_scale(parts, 0, 1.28 * PPU, "h")  # idle Ottie is ~1.3 tiles tall
    scaled = [ak.resize(p, s) for p in parts]
    cw = max(256, max(p.shape[1] for p in scaled) + 8)
    ch = max(256, max(p.shape[0] for p in scaled) + 8)
    cw += cw % 2
    framed = [ak.fit_on_canvas(p, cw, ch) for p in scaled]
    idle, r1, r2, r3, jump, fall, hurt, win = framed
    out = {"idle": idle, "run_0": r1, "run_1": r2, "run_2": r3, "run_3": r2, "jump": jump, "fall": fall, "hurt": hurt, "win": win}
    for k, v in out.items():
        save(v, f"Sprites/Player/ottie_{k}.png")

    # HUD life icon: Ottie's head from the idle pose.
    head = ak.tight(scaled[0][: int(scaled[0].shape[0] * 0.58)])
    hs = 120 / max(head.shape[:2])
    icon = ak.fit_on_canvas(ak.resize(head, hs), 128, 128)
    save(icon, "UI/icon_life.png")
    checker_preview("player_sheet", framed + [icon])


def enemies_sheet():
    parts = cutout_sheet("enemies_sheet", 5)
    crab_s = uniform_scale(parts, 0, 118, "w")
    # Walk frames are matched by height so the crab doesn't pulse; the flat pose keeps the sheet scale.
    walk1_s = crab_s * parts[0].shape[0] / parts[1].shape[0]
    crabs = [ak.fit_on_canvas(ak.resize(p, s), 192, 160) for p, s in zip(parts[:3], [crab_s, walk1_s, crab_s])]
    puff_s = uniform_scale(parts, 3, 118, "w")
    puffs = [ak.fit_on_canvas(ak.resize(p, puff_s), 160, 160) for p in parts[3:]]
    for i, name in enumerate(["crab_walk_0", "crab_walk_1", "crab_flat"]):
        save(crabs[i], f"Sprites/Enemies/{name}.png")
    for i in range(2):
        save(puffs[i], f"Sprites/Enemies/puffer_{i}.png")
    checker_preview("enemies_sheet", crabs + puffs)


def square_block(p, size=PPU):
    return ak.resize_to(p, size, size)


def items_sheet():
    parts = cutout_sheet("items_sheet", 6, holes=None)
    coin = ak.resize(parts[0], 92 / max(parts[0].shape[:2]))
    fish = ak.resize(parts[1], 112 / parts[1].shape[1])
    blocks = [square_block(p) for p in parts[2:]]
    save(coin, "Sprites/Items/coin.png")
    save(fish, "Sprites/Items/fish.png")
    for b, name in zip(blocks, ["block_question", "block_used", "block_brick", "block_stone"]):
        save(b, f"Sprites/Blocks/{name}.png")
    checker_preview("items_sheet", [coin, fish] + blocks)


def tiles_sheet():
    parts = cutout_sheet("tiles_sheet", 2)
    grass, dirt = [ak.resize_to(p, 512, 512) for p in parts]
    # Trim the drawn border a little: generators like to add an outline around squares.
    grass = ak.resize_to(grass[6:-6, 10:-10], 512, 512)
    dirt = ak.resize_to(dirt[10:-10, 10:-10], 512, 512)
    dirt = ak.make_seamless(dirt, 96)
    dirt = ak.resize_to(dirt, 512, 512)
    grass = ak.resize_to(ak.make_seamless_h(grass, 96), 512, 512)
    # Blend the lower part of the grass tile into the dirt texture so it continues seamlessly below.
    h = grass.shape[0]
    t = np.clip((np.arange(h) / h - 0.5) / 0.45, 0, 1)[:, None, None]
    grass = grass * (1 - t) + dirt * t
    grass[..., 3] = np.maximum(grass[..., 3], 0)
    dirt[..., 3] = 255
    grass_t = ak.resize_to(grass, PPU, PPU)
    dirt_t = ak.resize_to(dirt, PPU, PPU)
    save(grass_t, "Sprites/Tiles/tile_grass.png")
    save(dirt_t, "Sprites/Tiles/tile_dirt.png")
    # Preview a 4x3 patch to judge seams.
    row_g = np.concatenate([grass_t] * 4, axis=1)
    row_d = np.concatenate([dirt_t] * 4, axis=1)
    checker_preview("tiles_sheet", [np.concatenate([row_g, row_d, row_d], axis=0)])


def row_widths(p):
    a = p[..., 3] > 40
    widths = np.zeros(a.shape[0])
    for y in range(a.shape[0]):
        xs = np.flatnonzero(a[y])
        widths[y] = xs[-1] - xs[0] + 1 if len(xs) else 0
    return widths


def goal_sheet():
    parts = cutout_sheet("goal_sheet", 3, merge_px=4)
    # Reading order is left to right, as requested in the prompt: pipe, pole, flag.
    pipe, pole, flag = parts
    if pole.shape[0] / pole.shape[1] < pipe.shape[0] / pipe.shape[1]:
        raise SheetError("goal_sheet: expected pipe, pole, flag from left to right")

    # Pipe: rim rows are the wide ones at the top.
    widths = row_widths(pipe)
    maxw = widths.max()
    rim_end = next(y for y in range(int(len(widths) * 0.05), len(widths)) if widths[y] < maxw * 0.93)
    sx = 256 / maxw
    rim = ak.tight(pipe[:rim_end])
    top = ak.resize_to(rim, 256, 128)
    body_slice = ak.tight(pipe[int(len(widths) * 0.55) : int(len(widths) * 0.8)])
    body = ak.resize_to(body_slice, max(2, round(body_slice.shape[1] * sx)), 256)
    body = np.transpose(ak.make_seamless_h(np.transpose(body, (1, 0, 2)), 64), (1, 0, 2))
    body = ak.resize_to(body, body.shape[1], 128)
    body_canvas = np.zeros((128, 256, 4), np.float32)
    x0 = (256 - body.shape[1]) // 2
    body_canvas[:, x0 : x0 + body.shape[1]] = body[:, : 256 - x0]
    save(top, "Sprites/Goal/pipe_top.png")
    save(body_canvas, "Sprites/Goal/pipe_body.png")

    # Pole: ball = wide rows near the top, shaft below.
    pw = row_widths(pole)
    shaft_w = np.median(pw[len(pw) // 2 :])
    ball_search = int(len(pw) * 0.15)
    ball_mid = int(np.argmax(pw[:ball_search]))  # widest row of the ball
    ball_end = next(y for y in range(ball_mid, len(pw)) if pw[y] < shaft_w * 1.3)
    ball = ak.tight(pole[:ball_end])
    ball = ak.resize(ball, 48 / max(ball.shape[1], 1))
    shaft = ak.tight(pole[int(len(pw) * 0.4) : int(len(pw) * 0.6)])
    shaft = ak.resize_to(shaft, 22, 128)
    shaft = np.transpose(ak.make_seamless_h(np.transpose(ak.resize_to(shaft, 22, 160), (1, 0, 2)), 32), (1, 0, 2))
    shaft = ak.resize_to(shaft, 22, 128)
    pole_canvas = np.zeros((128, 32, 4), np.float32)
    pole_canvas[:, 5:27] = shaft
    save(ball, "Sprites/Goal/flag_ball.png")
    save(pole_canvas, "Sprites/Goal/flag_pole.png")

    flag_img = ak.resize(flag, 160 / flag.shape[1])
    save(flag_img, "Sprites/Goal/flag.png")
    checker_preview("goal_sheet", [top, body_canvas, ball, np.concatenate([pole_canvas] * 3, axis=0), flag_img])


def decor_sheet():
    parts = cutout_sheet("decor_sheet", 4)
    sizes = {"bush": ("w", 190), "flowers": ("w", 110), "reeds": ("h", 150), "sign": ("h", 115)}
    out = []
    for p, (name, (axis, target)) in zip(parts, sizes.items()):
        s = target / (p.shape[1] if axis == "w" else p.shape[0])
        img = ak.resize(p, s)
        save(img, f"Sprites/Decor/{name}.png")
        out.append(img)
    checker_preview("decor_sheet", out)


def background(name):
    rgb = ak.load_rgb(RAW / f"{name}.png")
    rgba = np.concatenate([rgb, np.full(rgb.shape[:2] + (1,), 255, np.float32)], axis=2)
    seamless = ak.make_seamless_h(rgba, 200)
    save(seamless, f"Backgrounds/{name}.png")
    checker_preview(name, [ak.resize(np.concatenate([seamless, seamless], axis=1), 0.35)])


def title_art():
    rgb = ak.load_rgb(RAW / "title_art.png")
    rgba = np.concatenate([rgb, np.full(rgb.shape[:2] + (1,), 255, np.float32)], axis=2)
    save(rgba, "UI/title_art.png")


def logo():
    rgb = ak.load_rgb(RAW / "logo.png")
    rgba = ak.remove_white_background(rgb, fill_holes_min_area=400)
    img = ak.tight(rgba)
    img = ak.resize(img, min(1.0, 1200 / img.shape[1]))
    save(img, "UI/logo.png")
    checker_preview("logo", [ak.resize(img, 0.5)])


def app_icon():
    img = Image.open(RAW / "app_icon.png").convert("RGB").resize((1024, 1024), Image.LANCZOS)
    path = ART / "AppIcon/app_icon.png"
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path)


STEPS = {
    "player_sheet": player_sheet,
    "enemies_sheet": enemies_sheet,
    "items_sheet": items_sheet,
    "tiles_sheet": tiles_sheet,
    "goal_sheet": goal_sheet,
    "decor_sheet": decor_sheet,
    "bg_day": lambda: background("bg_day"),
    "bg_sunset": lambda: background("bg_sunset"),
    "bg_twilight": lambda: background("bg_twilight"),
    "title_art": title_art,
    "logo": logo,
    "app_icon": app_icon,
}

if __name__ == "__main__":
    names = sys.argv[1:] or [n for n in STEPS if (RAW / f"{n}.png").exists()]
    failed = False
    for n in names:
        try:
            STEPS[n]()
            print("ok  ", n)
        except Exception as e:  # report every asset, then fail
            failed = True
            print("FAIL", n, "-", e)
    sys.exit(1 if failed else 0)
