"""Authoring helper that lays out the level maps and writes Assets/Levels/levelN.txt.
Coordinates: x from the left, y from the bottom. Ground is rows 0-1, so the walkable floor is y=2.
Map legend is documented in LevelParser.cs."""

from pathlib import Path

OUT = Path.home() / "Develop/SuperOttie/Assets/Levels"
H = 14
FLOOR = 2  # first empty row above the ground


class Level:
    def __init__(self, name, theme, time, width):
        self.name, self.theme, self.time, self.w = name, theme, time, width
        self.g = [["."] * width for _ in range(H)]
        for x in range(width):
            self.g[0][x] = self.g[1][x] = "#"

    def put(self, x, y, s):
        for i, ch in enumerate(s):
            self.g[y][x + i] = ch

    def pit(self, x0, width):
        for x in range(x0, x0 + width):
            self.g[0][x] = self.g[1][x] = "."

    def pipe(self, x, height):
        top = FLOOR + height - 1
        self.put(x, top, "[]")
        for y in range(FLOOR, top):
            self.put(x, y, "||")

    def stairs_up(self, x0, n, base=FLOOR):
        for i in range(n):
            for y in range(base, base + i + 1):
                self.g[y][x0 + i] = "S"

    def stairs_down(self, x0, n, base=FLOOR):
        for i in range(n):
            for y in range(base, base + n - i):
                self.g[y][x0 + i] = "S"

    def column(self, x, height, base=FLOOR):
        for y in range(base, base + height):
            self.g[y][x] = "S"

    def platform(self, x, y, width, ch="S"):
        self.put(x, y, ch * width)

    def coins(self, x, y, n):
        self.put(x, y, "c" * n)

    def write(self, index):
        rows = ["".join(r).rstrip(".") or "." for r in reversed(self.g)]
        text = f"name: {self.name}\ntheme: {self.theme}\ntime: {self.time}\n---\n" + "\n".join(rows) + "\n"
        OUT.mkdir(parents=True, exist_ok=True)
        (OUT / f"level{index}.txt").write_text(text)
        print(f"level{index}.txt  {self.w}x{H}")


def level1():
    L = Level("Sunny Meadow", "day", 300, 212)
    L.put(3, FLOOR, "P")
    L.put(1, FLOOR, "d"); L.put(9, FLOOR, "w"); L.put(12, FLOOR, "n")
    L.put(16, 5, "?")
    L.put(20, 5, "BMB?B"); L.put(22, 9, "?")
    L.put(24, FLOOR, "e")
    L.pipe(29, 2); L.put(33, FLOOR, "d")
    L.coins(32, 6, 4)
    L.pipe(39, 3); L.put(43, FLOOR, "e"); L.put(46, FLOOR, "w")
    L.pipe(49, 4); L.put(53, FLOOR, "e"); L.put(55, FLOOR, "e")
    L.pipe(58, 4)
    L.coins(64, 5, 3)
    L.pit(69, 2)
    L.put(75, FLOOR, "r")
    L.put(78, 5, "B?B")
    L.put(81, 9, "BBBBBBBB"); L.put(83, 10, "e"); L.put(86, 10, "e")
    L.pit(87, 3)
    L.put(92, 9, "BBB?"); L.put(95, 5, "?")
    L.put(99, FLOOR, "e"); L.put(101, FLOOR, "e")
    L.put(104, 5, "BM"); L.put(106, FLOOR, "d")
    L.put(110, 5, "?"); L.put(113, 5, "?"); L.put(113, 9, "M"); L.put(116, 5, "?")
    L.put(112, FLOOR, "e"); L.put(114, FLOOR, "e")
    L.put(118, FLOOR, "K")
    L.put(122, 5, "B"); L.put(125, 9, "BBB"); L.put(126, 10, "e")
    L.put(130, 9, "B??B"); L.put(131, 5, "BB")
    L.stairs_up(136, 4); L.stairs_down(141, 4)
    L.put(147, FLOOR, "w")
    L.stairs_up(150, 4); L.column(154, 4); L.pit(155, 2); L.stairs_down(157, 4)
    L.put(162, FLOOR, "d")
    L.pipe(165, 2); L.put(168, 5, "BB?B"); L.put(170, FLOOR, "e"); L.put(172, FLOOR, "e")
    L.pipe(176, 2)
    L.stairs_up(180, 8); L.column(188, 8)
    L.put(198, FLOOR, "F")
    L.put(202, FLOOR, "d"); L.put(205, FLOOR, "n"); L.put(208, FLOOR, "w")
    return L


def level2():
    L = Level("Sunset Shore", "sunset", 300, 206)
    L.put(3, FLOOR, "P"); L.put(1, FLOOR, "r"); L.put(8, FLOOR, "w")
    L.put(14, 5, "?B?B?"); L.coins(14, 8, 5)
    L.put(21, FLOOR, "e")
    L.pit(25, 3); L.put(26, 4, "f")
    L.platform(31, 5, 3); L.coins(31, 6, 3)
    L.pit(36, 4); L.put(37, 4, "f")
    L.put(44, 5, "BMB"); L.put(48, FLOOR, "e"); L.put(50, FLOOR, "e")
    L.pipe(54, 3); L.put(58, FLOOR, "r")
    L.pit(61, 4); L.platform(62, 5, 2); L.put(64, 7, "cc")
    L.put(70, FLOOR, "e"); L.put(72, FLOOR, "e"); L.put(74, FLOOR, "e")
    L.put(69, 5, "BBBBBB"); L.coins(69, 6, 6)
    L.pipe(79, 2); L.pipe(86, 4); L.put(84, FLOOR, "e")
    L.put(91, 6, "f")
    L.pit(94, 4); L.platform(95, 4, 2); L.put(95, 5, "cc")
    L.put(101, 5, "?"); L.put(104, 5, "M"); L.put(104, 9, "?"); L.put(107, 5, "?")
    L.put(106, FLOOR, "e"); L.put(109, FLOOR, "e")
    L.put(111, FLOOR, "K")
    L.stairs_up(114, 3); L.pit(117, 2); L.stairs_down(119, 3); L.put(118, 7, "f")
    L.put(126, FLOOR, "d")
    L.put(129, 5, "BBB"); L.put(133, 9, "BBBB"); L.put(134, 10, "e")
    L.pipe(139, 3); L.put(143, FLOOR, "e"); L.pipe(146, 2)
    L.pit(150, 4); L.put(151, 5, "f"); L.coins(150, 8, 4)
    L.put(157, 5, "?B?"); L.put(161, FLOOR, "e"); L.put(163, FLOOR, "e")
    L.stairs_up(168, 8); L.column(176, 8)
    L.put(188, FLOOR, "F")
    L.put(192, FLOOR, "r"); L.put(196, FLOOR, "n"); L.put(200, FLOOR, "w")
    return L


def level3():
    L = Level("Twilight Woods", "twilight", 320, 216)
    L.put(3, FLOOR, "P"); L.put(8, FLOOR, "d")
    L.put(13, 5, "M"); L.put(16, 5, "B?B?B"); L.put(18, 9, "c")
    L.put(22, FLOOR, "e"); L.put(24, FLOOR, "e")
    L.pipe(28, 3); L.put(29, 8, "f")
    L.pipe(35, 4); L.put(33, FLOOR, "e")
    L.pit(40, 4); L.put(41, 4, "f"); L.coins(40, 8, 4)
    L.platform(45, 5, 3); L.put(46, 6, "e"); L.platform(53, 8, 4); L.coins(53, 9, 4)
    L.pit(51, 4); L.put(58, FLOOR, "e")
    L.put(62, 5, "?B?B?"); L.put(64, 9, "M")
    L.put(66, FLOOR, "e"); L.put(68, FLOOR, "e"); L.put(70, FLOOR, "e")
    L.pipe(74, 2); L.pipe(80, 3); L.pipe(86, 4)
    L.put(77, FLOOR, "e"); L.put(83, FLOOR, "e")
    L.pit(91, 3); L.put(92, 5, "f")
    L.stairs_up(96, 4); L.pit(100, 2); L.put(100, 9, "f"); L.stairs_down(102, 4)
    L.put(109, FLOOR, "K")
    L.put(110, 5, "BBB?BBB"); L.put(113, 9, "?"); L.put(112, 6, "e"); L.put(115, 6, "e")
    L.put(119, FLOOR, "e"); L.put(121, FLOOR, "e")
    L.pit(125, 4); L.platform(126, 5, 1); L.platform(128, 7, 1); L.put(128, 8, "c")
    L.put(134, FLOOR, "r")
    L.pipe(137, 2); L.put(141, FLOOR, "e"); L.pipe(144, 3); L.put(148, FLOOR, "e"); L.pipe(151, 4)
    L.put(155, 7, "f")
    L.pit(158, 3); L.put(162, 5, "?M?"); L.put(166, FLOOR, "e"); L.put(168, FLOOR, "e")
    L.stairs_up(172, 4); L.pit(176, 2); L.stairs_up(178, 6, base=FLOOR); L.column(184, 8); L.column(183, 7)
    L.put(196, FLOOR, "F")
    L.put(200, FLOOR, "d"); L.put(204, FLOOR, "n"); L.put(208, FLOOR, "w"); L.put(212, FLOOR, "r")
    return L


if __name__ == "__main__":
    for i, lv in enumerate([level1(), level2(), level3()], start=1):
        lv.write(i)
