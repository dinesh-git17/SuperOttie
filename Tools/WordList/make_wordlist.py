"""Builds the Word Hunt dictionary: Assets/Data/words.txt from the ENABLE list, minus the blocklist.

    python3 Tools/WordList/make_wordlist.py

ENABLE (enable1.txt, public domain) comes from https://github.com/dolph/dictionary. Words of 3 to 12 letters are
kept: shorter ones don't count in Word Hunt, and longer ones are practically impossible to trace on a 4x4 grid.
Every blocked word is printed so the prefix (*) entries in blocklist.txt can be reviewed.
"""

from pathlib import Path

HERE = Path(__file__).parent
OUT = HERE.parent.parent / "Assets/Data/words.txt"
MIN_LEN, MAX_LEN = 3, 12


def load_blocklist():
    exact, stems = set(), []
    for line in (HERE / "blocklist.txt").read_text().splitlines():
        line = line.strip().lower()
        if not line or line.startswith("#"):
            continue
        if line.endswith("*"):
            stems.append(line[:-1])
        else:
            exact.add(line)
    return exact, tuple(stems)


def main():
    exact, stems = load_blocklist()
    words, blocked = [], []
    for w in (HERE / "enable1.txt").read_text().split():
        w = w.strip().lower()
        if not (MIN_LEN <= len(w) <= MAX_LEN) or not w.isalpha() or not w.isascii():
            continue
        if w in exact or w.startswith(stems):
            blocked.append(w)
            continue
        words.append(w)
    words.sort()
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text("\n".join(words) + "\n")
    print(f"wrote {len(words)} words to {OUT.relative_to(HERE.parent.parent)}; blocked {len(blocked)}:")
    print(" ".join(blocked))


if __name__ == "__main__":
    main()
