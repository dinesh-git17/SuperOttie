"""Simple drawn stand-ins for every game sprite, written with the SAME names/sizes the real art
pipeline (process.py) produces, so the Unity project runs before generated art exists.
It never overwrites an existing file unless --force is given."""

import sys
from pathlib import Path

from PIL import Image, ImageDraw

ART = Path.home() / "Develop/SuperOttie/Assets/Art"
FORCE = "--force" in sys.argv
OUT = "#3b2414"


def save(img, rel):
    p = ART / rel
    p.parent.mkdir(parents=True, exist_ok=True)
    if p.exists() and not FORCE:
        return
    img.save(p)
    print("wrote", rel)


def canvas(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))


def otter(pose):
    im = canvas(256, 256)
    d = ImageDraw.Draw(im)
    bob = {"jump": -10, "fall": 0}.get(pose, 0)
    # tail, body, head, glasses
    d.ellipse((50, 190 + bob, 110, 225 + bob), fill="#6b4226", outline=OUT, width=5)
    d.ellipse((85, 130 + bob, 175, 250 + bob), fill="#8a5a36", outline=OUT, width=5)
    d.ellipse((105, 160 + bob, 160, 240 + bob), fill="#f1d9b5")
    d.ellipse((80, 70 + bob, 190, 170 + bob), fill="#8a5a36", outline=OUT, width=5)
    d.ellipse((140, 115 + bob, 195, 160 + bob), fill="#f1d9b5", outline=OUT, width=4)
    for cx in (135, 172):
        d.ellipse((cx - 18, 92 + bob, cx + 18, 128 + bob), fill="#fff", outline="#111", width=6)
        if pose == "hurt":
            d.line((cx - 8, 102 + bob, cx + 8, 118 + bob), fill="#111", width=4)
            d.line((cx - 8, 118 + bob, cx + 8, 102 + bob), fill="#111", width=4)
        else:
            d.ellipse((cx - 4, 104 + bob, cx + 8, 116 + bob), fill="#111")
    d.ellipse((186, 128 + bob, 198, 138 + bob), fill="#2b1a10")
    legs = {"run_0": (-14, 14), "run_1": (0, 0), "run_2": (14, -14), "run_3": (0, 0)}.get(pose, (0, 0))
    if pose not in ("jump",):
        for dx in legs:
            d.rounded_rectangle((120 + dx, 232, 146 + dx, 256), 8, fill="#6b4226", outline=OUT, width=4)
    if pose in ("win", "jump"):
        d.line((110, 160 + bob, 90, 110 + bob), fill=OUT, width=14)
        d.line((160, 160 + bob, 185, 110 + bob), fill=OUT, width=14)
    return im


def main():
    for pose in ["idle", "run_0", "run_1", "run_2", "run_3", "jump", "fall", "hurt", "win"]:
        save(otter(pose), f"Sprites/Player/ottie_{pose}.png")

    for i in range(2):
        im = canvas(192, 160)
        d = ImageDraw.Draw(im)
        d.ellipse((36, 50, 156, 140), fill="#e2463a", outline=OUT, width=6)
        for k in range(3):
            x = 50 + k * 30 + (8 if i else 0)
            d.line((x, 130, x - 12, 158), fill=OUT, width=6)
            d.line((x + 60, 130, x + 72, 158), fill=OUT, width=6)
        d.ellipse((30, 20, 70, 60), fill="#e2463a", outline=OUT, width=5)
        d.ellipse((60, 70, 80, 90), fill="#fff", outline=OUT, width=3)
        d.ellipse((100, 70, 120, 90), fill="#fff", outline=OUT, width=3)
        save(im, f"Sprites/Enemies/crab_walk_{i}.png")
    im = canvas(192, 160)
    ImageDraw.Draw(im).ellipse((20, 120, 172, 160), fill="#e2463a", outline=OUT, width=6)
    save(im, "Sprites/Enemies/crab_flat.png")
    for i in range(2):
        im = canvas(160, 160)
        d = ImageDraw.Draw(im)
        for a in range(0, 360, 30):
            import math

            r = math.radians(a)
            d.line((80, 80, 80 + 76 * math.cos(r), 80 + 76 * math.sin(r)), fill=OUT, width=6)
        d.ellipse((20, 20, 140, 140), fill="#6a7ce0", outline=OUT, width=6)
        d.ellipse((40, 55, 62, 77), fill="#fff", outline=OUT, width=3)
        d.polygon([(120, 60 + i * 20), (150, 40 + i * 20), (150, 90 + i * 20)], fill="#9b6ae0", outline=OUT)
        save(im, f"Sprites/Enemies/puffer_{i}.png")

    im = canvas(96, 96)
    d = ImageDraw.Draw(im)
    d.ellipse((6, 6, 90, 90), fill="#ffcc2e", outline=OUT, width=6)
    d.ellipse((30, 30, 66, 66), outline="#d99a00", width=5)
    save(im, "Sprites/Items/coin.png")
    im = canvas(128, 96)
    d = ImageDraw.Draw(im)
    d.ellipse((10, 18, 100, 80), fill="#ffb300", outline=OUT, width=6)
    d.polygon([(95, 48), (124, 20), (124, 76)], fill="#ffb300", outline=OUT)
    d.ellipse((24, 34, 42, 52), fill="#fff", outline=OUT, width=3)
    save(im, "Sprites/Items/fish.png")

    def block(fill, rel, mark=None):
        im = canvas(128, 128)
        d = ImageDraw.Draw(im)
        d.rounded_rectangle((2, 2, 125, 125), 12, fill=fill, outline=OUT, width=6)
        for x, y in [(18, 18), (110, 18), (18, 110), (110, 110)]:
            d.ellipse((x - 5, y - 5, x + 5, y + 5), fill=OUT)
        if mark == "?":
            d.text((48, 28), "?", fill="#fff", font_size=72)
        if mark == "brick":
            for y in (42, 84):
                d.line((4, y, 124, y), fill=OUT, width=5)
            for y0, xs in [(4, [64]), (42, [32, 96]), (84, [64])]:
                for x in xs:
                    d.line((x, y0, x, y0 + 40), fill=OUT, width=5)
        save(im, rel)

    block("#f6c21c", "Sprites/Blocks/block_question.png", "?")
    block("#9a6b44", "Sprites/Blocks/block_used.png")
    block("#d0692f", "Sprites/Blocks/block_brick.png", "brick")
    block("#9aa0a8", "Sprites/Blocks/block_stone.png")

    im = canvas(128, 128)
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, 127, 127), fill="#9b5f32")
    d.rectangle((0, 0, 127, 34), fill="#5cc23a")
    for x in range(0, 128, 32):
        d.ellipse((x, 22, x + 32, 46), fill="#5cc23a")
    save(im, "Sprites/Tiles/tile_grass.png")
    im = canvas(128, 128)
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, 127, 127), fill="#9b5f32")
    for x, y in [(20, 30), (80, 70), (40, 100), (100, 20)]:
        d.ellipse((x, y, x + 12, y + 9), fill="#7c4a26")
    save(im, "Sprites/Tiles/tile_dirt.png")

    im = canvas(256, 128)
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((2, 2, 253, 125), 10, fill="#3fbf4a", outline=OUT, width=6)
    save(im, "Sprites/Goal/pipe_top.png")
    im = canvas(256, 128)
    d = ImageDraw.Draw(im)
    d.rectangle((16, 0, 239, 127), fill="#35a83f")
    d.line((16, 0, 16, 127), fill=OUT, width=6)
    d.line((239, 0, 239, 127), fill=OUT, width=6)
    d.rectangle((50, 0, 64, 127), fill="#7fe07f")
    save(im, "Sprites/Goal/pipe_body.png")
    im = canvas(32, 128)
    ImageDraw.Draw(im).rectangle((8, 0, 23, 127), fill="#8bd48b", outline=OUT, width=3)
    save(im, "Sprites/Goal/flag_pole.png")
    im = canvas(48, 48)
    ImageDraw.Draw(im).ellipse((2, 2, 45, 45), fill="#ffcc2e", outline=OUT, width=4)
    save(im, "Sprites/Goal/flag_ball.png")
    im = canvas(160, 128)
    ImageDraw.Draw(im).polygon([(158, 4), (158, 124), (4, 64)], fill="#1fb5a8", outline=OUT)
    save(im, "Sprites/Goal/flag.png")

    for name, color in [("bush", "#46a83a"), ("flowers", "#f5f1d0"), ("reeds", "#6f8f3a"), ("sign", "#a8743e")]:
        im = canvas(192, 128)
        ImageDraw.Draw(im).ellipse((10, 30, 182, 150), fill=color, outline=OUT, width=5)
        save(im, f"Sprites/Decor/{name}.png")

    for name, top, bottom in [("bg_day", (120, 196, 255), (190, 235, 170)), ("bg_sunset", (255, 160, 110), (120, 80, 120)), ("bg_twilight", (40, 40, 110), (20, 70, 80))]:
        im = Image.new("RGB", (1536, 1024))
        d = ImageDraw.Draw(im)
        for y in range(1024):
            t = y / 1023
            d.line((0, y, 1535, y), fill=tuple(int(a + (b - a) * t) for a, b in zip(top, bottom)))
        for x in range(-200, 1800, 400):
            d.ellipse((x, 700, x + 600, 1300), fill=tuple(int(c * 0.8) for c in bottom))
        save(im, f"Backgrounds/{name}.png")

    im = Image.new("RGB", (1536, 1024), (120, 196, 255))
    d = ImageDraw.Draw(im)
    d.rectangle((0, 820, 1535, 1023), fill="#5cc23a")
    im.paste(otter("jump").resize((512, 512)), (950, 380), otter("jump").resize((512, 512)))
    save(im, "UI/title_art.png")
    im = canvas(1200, 500)
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((10, 10, 1190, 490), 80, fill="#ffc53a", outline=OUT, width=16)
    d.text((170, 110), "SUPER OTTIE", fill=OUT, font_size=150)
    save(im, "UI/logo.png")
    face = otter("idle").crop((70, 60, 210, 180)).resize((128, 110))
    im = canvas(128, 128)
    im.paste(face, (0, 10), face)
    save(im, "UI/icon_life.png")
    im = Image.new("RGB", (1024, 1024), (120, 196, 255))
    big = otter("idle").resize((1024, 1024))
    im.paste(big, (0, 40), big)
    save(im, "AppIcon/app_icon.png")


if __name__ == "__main__":
    main()
