"""Touch-control button art (final, not placeholder): soft translucent discs with bold glyphs.
Drawn at 4x and downsampled for smooth edges."""

from pathlib import Path

from PIL import Image, ImageDraw

UI = Path.home() / "Develop/SuperOttie/Assets/Art/UI"
S = 4  # supersample
SIZE = 256


def disc():
    im = Image.new("RGBA", (SIZE * S, SIZE * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    m = 8 * S
    d.ellipse((m, m, SIZE * S - m, SIZE * S - m), fill=(40, 24, 16, 120), outline=(255, 255, 255, 235), width=10 * S)
    return im, d


def finish(im, name):
    UI.mkdir(parents=True, exist_ok=True)
    im.resize((SIZE, SIZE), Image.LANCZOS).save(UI / f"{name}.png")
    print("wrote", name)


def arrow(direction):
    im, d = disc()
    c = SIZE * S / 2
    w = 62 * S
    if direction == "right":
        pts = [(c - w * 0.6, c - w), (c + w, c), (c - w * 0.6, c + w)]
    else:
        pts = [(c + w * 0.6, c - w), (c - w, c), (c + w * 0.6, c + w)]
    d.polygon(pts, fill=(255, 255, 255, 245))
    return im


def jump():
    im, d = disc()
    c = SIZE * S / 2
    w = 60 * S
    d.polygon([(c - w, c + w * 0.35), (c, c - w * 0.75), (c + w, c + w * 0.35)], fill=(255, 255, 255, 245))
    d.rounded_rectangle((c - w * 0.35, c + w * 0.2, c + w * 0.35, c + w * 0.95), 10 * S, fill=(255, 255, 255, 245))
    return im


def pause():
    im, d = disc()
    c = SIZE * S / 2
    bw, bh, gap = 26 * S, 100 * S, 22 * S
    for x in (c - gap - bw, c + gap):
        d.rounded_rectangle((x, c - bh / 2, x + bw, c + bh / 2), 8 * S, fill=(255, 255, 255, 245))
    return im


if __name__ == "__main__":
    finish(arrow("left"), "btn_left")
    finish(arrow("right"), "btn_right")
    finish(jump(), "btn_jump")
    finish(pause(), "btn_pause")
