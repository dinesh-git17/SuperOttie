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
    L.put(35, FLOOR, "e")
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
    L.put(33, FLOOR, "e")
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
    L.put(31, FLOOR, "e")
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


def level4():
    L = Level("Autumn Grove", "autumn", 300, 218)
    L.put(3, FLOOR, "P"); L.put(1, FLOOR, "d"); L.put(9, FLOOR, "w"); L.put(12, FLOOR, "r")
    L.put(15, 5, "?B?"); L.put(16, 9, "M"); L.coins(21, 6, 3)
    L.pipe(26, 2); L.put(31, FLOOR, "e"); L.pipe(34, 3)
    L.pit(39, 3); L.coins(39, 7, 3)
    L.put(45, FLOOR, "e"); L.put(47, FLOOR, "e"); L.put(49, FLOOR, "e")
    L.put(44, 5, "BBBBBBB"); L.put(46, 9, "B?B"); L.coins(45, 6, 5)
    L.stairs_up(55, 3); L.pit(58, 2); L.put(58, 7, "f"); L.stairs_down(60, 3)
    L.put(66, FLOOR, "d")
    L.platform(69, 5, 3); L.pit(73, 4); L.platform(74, 4, 2); L.coins(74, 5, 2)
    L.put(81, FLOOR, "e"); L.put(83, FLOOR, "e")
    L.pipe(86, 4); L.put(90, 7, "f"); L.pipe(93, 3)
    L.put(98, 5, "?M?"); L.put(101, FLOOR, "e")
    L.put(106, FLOOR, "K"); L.put(108, FLOOR, "w")
    L.put(112, 5, "BB??BB"); L.put(113, 9, "BBBB"); L.put(114, 10, "e"); L.put(116, FLOOR, "e"); L.put(118, FLOOR, "e")
    L.pit(122, 4); L.put(123, 5, "f")
    L.stairs_up(129, 4); L.column(133, 4); L.pit(134, 2); L.stairs_down(136, 4)
    L.put(142, FLOOR, "r")
    L.put(146, 5, "?"); L.put(149, 5, "B?B"); L.put(152, 5, "?")
    L.put(148, FLOOR, "e"); L.put(151, FLOOR, "e"); L.put(154, FLOOR, "e")
    L.pipe(158, 2); L.pit(161, 3); L.pipe(166, 3); L.put(162, 7, "f")
    L.put(171, FLOOR, "e"); L.put(173, FLOOR, "e")
    L.stairs_up(178, 8); L.column(186, 8)
    L.put(198, FLOOR, "F")
    L.put(202, FLOOR, "d"); L.put(206, FLOOR, "n"); L.put(210, FLOOR, "w")
    return L


def level5():
    L = Level("Frosty Peaks", "snow", 320, 222)
    L.put(3, FLOOR, "P"); L.put(1, FLOOR, "r"); L.put(8, FLOOR, "d")
    L.put(14, 5, "B?M?B"); L.coins(14, 9, 5)
    L.pit(24, 3); L.platform(28, 5, 3); L.pit(32, 4); L.put(33, 5, "f")
    L.put(38, FLOOR, "e"); L.put(40, FLOOR, "e")
    L.stairs_up(44, 4); L.column(48, 4); L.pit(49, 2); L.stairs_down(51, 4); L.coins(49, 9, 2)
    L.put(59, FLOOR, "e"); L.put(61, FLOOR, "e"); L.put(63, FLOOR, "e")
    L.put(60, 5, "?B?B?")
    L.pipe(68, 3); L.put(72, 8, "f"); L.pipe(75, 4); L.put(80, FLOOR, "e")
    L.pit(84, 4); L.platform(85, 5, 2); L.put(85, 6, "cc")
    L.pit(90, 4); L.platform(91, 4, 2); L.put(91, 5, "cc")
    L.put(97, FLOOR, "w")
    L.put(100, 5, "BMB"); L.put(104, FLOOR, "e"); L.put(106, FLOOR, "e")
    L.put(110, FLOOR, "K")
    L.put(114, 5, "BBBBBB"); L.coins(114, 6, 6); L.put(116, 9, "?"); L.put(115, FLOOR, "e"); L.put(118, FLOOR, "e")
    L.stairs_up(123, 3); L.pit(126, 2); L.put(126, 8, "f"); L.stairs_down(128, 3)
    L.pipe(136, 2); L.put(139, FLOOR, "e"); L.pipe(142, 3); L.put(145, FLOOR, "e"); L.pipe(148, 4)
    L.pit(153, 3); L.put(154, 5, "f"); L.coins(153, 8, 3)
    L.put(159, 5, "?M?"); L.put(162, FLOOR, "e"); L.put(164, FLOOR, "e"); L.put(166, FLOOR, "e")
    L.stairs_up(170, 4); L.pit(174, 2); L.stairs_up(176, 6); L.column(182, 8); L.column(181, 7)
    L.put(194, FLOOR, "F")
    L.put(198, FLOOR, "d"); L.put(202, FLOOR, "n"); L.put(206, FLOOR, "r")
    return L


def level6():
    L = Level("Crystal Caverns", "cave", 340, 232)
    L.put(3, FLOOR, "P"); L.put(1, FLOOR, "w"); L.put(8, FLOOR, "d")
    L.put(13, 5, "M"); L.put(16, 5, "B?B?B"); L.put(18, 9, "?")
    L.pit(26, 4); L.put(27, 4, "f"); L.coins(26, 8, 4)
    L.put(33, FLOOR, "e"); L.put(35, FLOOR, "e")
    L.pipe(38, 3); L.put(42, 8, "f"); L.pipe(45, 4)
    L.put(50, FLOOR, "e"); L.put(52, FLOOR, "e"); L.put(54, FLOOR, "e")
    L.put(49, 5, "BBBBBB"); L.put(51, 9, "?B?")
    L.pit(59, 4); L.platform(60, 5, 2); L.put(60, 6, "cc")
    L.stairs_up(68, 4); L.column(72, 4); L.column(73, 4); L.pit(74, 2); L.put(74, 9, "f"); L.stairs_down(76, 4)
    L.put(81, FLOOR, "r")
    L.platform(82, 5, 3); L.put(83, 6, "e"); L.platform(89, 8, 4); L.coins(89, 9, 4); L.pit(87, 4)
    L.put(95, FLOOR, "e"); L.put(97, FLOOR, "e")
    L.put(100, 5, "?M?"); L.put(104, FLOOR, "e")
    L.put(108, FLOOR, "K")
    L.put(112, 5, "BBB?BBB"); L.put(115, 9, "?"); L.put(113, 6, "e"); L.put(117, 6, "e")
    L.put(121, FLOOR, "e"); L.put(123, FLOOR, "e"); L.put(125, FLOOR, "e")
    L.pit(129, 4); L.platform(130, 5, 1); L.platform(132, 7, 1); L.put(132, 8, "c")
    L.pipe(137, 2); L.put(141, FLOOR, "e"); L.pipe(144, 3); L.put(148, FLOOR, "e"); L.pipe(151, 4)
    L.put(156, 7, "f")
    L.pit(159, 4); L.put(160, 5, "f"); L.coins(159, 8, 4)
    L.put(166, 5, "?B?B?"); L.put(168, 9, "M")
    L.put(167, FLOOR, "e"); L.put(169, FLOOR, "e"); L.put(171, FLOOR, "e")
    L.stairs_up(176, 4); L.pit(180, 2); L.stairs_up(182, 4, base=FLOOR); L.pit(186, 2)
    L.stairs_up(188, 8); L.column(196, 8)
    L.put(208, FLOOR, "F")
    L.put(212, FLOOR, "d"); L.put(216, FLOOR, "n"); L.put(220, FLOOR, "w"); L.put(224, FLOOR, "r")
    return L


if __name__ == "__main__":
    for i, lv in enumerate([level1(), level2(), level3(), level4(), level5(), level6()], start=1):
        lv.write(i)
