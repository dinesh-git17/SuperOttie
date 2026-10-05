using System;
using System.Collections.Generic;
using System.Text;

namespace SuperOttie.WordHunt
{
    /// <summary>A 4x4 board of letters, cells numbered 0-15 left to right, top to bottom.</summary>
    public sealed class LetterGrid
    {
        public const int Size = 4;
        public const int CellCount = Size * Size;

        readonly char[] _letters;

        public LetterGrid(string letters)
        {
            if (letters == null || letters.Length != CellCount) throw new ArgumentException($"A grid needs {CellCount} letters.", nameof(letters));
            _letters = letters.ToLowerInvariant().ToCharArray();
            foreach (char c in _letters)
                if (c < 'a' || c > 'z') throw new ArgumentException($"'{c}' is not a letter.", nameof(letters));
        }

        public char this[int cell] => _letters[cell];

        public static int Row(int cell) => cell / Size;
        public static int Column(int cell) => cell % Size;

        /// <summary>Touching cells, including diagonals.</summary>
        public static bool Adjacent(int a, int b) =>
            a != b && Math.Abs(Row(a) - Row(b)) <= 1 && Math.Abs(Column(a) - Column(b)) <= 1;

        /// <summary>The cells spelling <paramref name="word"/> through neighbours without reusing one, or null.</summary>
        public List<int> FindPath(string word)
        {
            if (string.IsNullOrEmpty(word)) return null;
            word = word.ToLowerInvariant();
            var path = new List<int>(word.Length);
            var used = new bool[CellCount];
            for (int start = 0; start < CellCount; start++)
                if (Extend(word, start, path, used)) return path;
            return null;
        }

        bool Extend(string word, int cell, List<int> path, bool[] used)
        {
            if (used[cell] || _letters[cell] != word[path.Count]) return false;
            path.Add(cell);
            used[cell] = true;
            if (path.Count == word.Length) return true;
            for (int next = 0; next < CellCount; next++)
                if (Adjacent(cell, next) && Extend(word, next, path, used)) return true;
            path.RemoveAt(path.Count - 1);
            used[cell] = false;
            return false;
        }

        public override string ToString() => new string(_letters);

        public string ToRows()
        {
            var sb = new StringBuilder();
            for (int r = 0; r < Size; r++) sb.Append(_letters, r * Size, Size).Append(r < Size - 1 ? "/" : "");
            return sb.ToString();
        }
    }

    /// <summary>Lists every dictionary word a board contains (depth-first search pruned by prefixes).</summary>
    public static class GridSolver
    {
        public const int MinLength = 3;

        public static SortedSet<string> FindAll(LetterGrid grid, WordList words)
        {
            var found = new SortedSet<string>(StringComparer.Ordinal);
            var used = new bool[LetterGrid.CellCount];
            var sb = new StringBuilder(LetterGrid.CellCount);
            for (int cell = 0; cell < LetterGrid.CellCount; cell++) Search(grid, words, cell, used, sb, found);
            return found;
        }

        static void Search(LetterGrid grid, WordList words, int cell, bool[] used, StringBuilder sb, SortedSet<string> found)
        {
            sb.Append(grid[cell]);
            string prefix = sb.ToString();
            if (words.HasPrefix(prefix))
            {
                used[cell] = true;
                if (prefix.Length >= MinLength && words.Contains(prefix)) found.Add(prefix);
                for (int next = 0; next < LetterGrid.CellCount; next++)
                    if (!used[next] && LetterGrid.Adjacent(cell, next)) Search(grid, words, next, used, sb, found);
                used[cell] = false;
            }
            sb.Length--;
        }
    }

    /// <summary>
    /// Rolls boards from a classic set of 16 letter dice (the Q face swapped for an E, since Word Hunt has no
    /// Qu tile) and keeps rolling until a board has plenty of words, so no round is a dud.
    /// </summary>
    public static class GridGenerator
    {
        public const int MinWords = 40;
        const int MaxAttempts = 80;

        static readonly string[] Dice =
        {
            "aaeegn", "abbjoo", "achops", "affkps", "aoottw", "cimotu", "deilrx", "delrvy",
            "distty", "eeghnw", "eeinsu", "ehrtvw", "eiosst", "elrtty", "himneu", "hlnnrz",
        };

        public static LetterGrid Generate(WordList words, Random rng, int minWords = MinWords)
        {
            LetterGrid best = null;
            int bestCount = -1;
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var grid = Roll(rng);
                int count = GridSolver.FindAll(grid, words).Count;
                if (count >= minWords) return grid;
                if (count > bestCount)
                {
                    best = grid;
                    bestCount = count;
                }
            }
            return best; // practically unreachable: most rolls clear the bar
        }

        static LetterGrid Roll(Random rng)
        {
            var order = new int[Dice.Length];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            var letters = new char[LetterGrid.CellCount];
            for (int cell = 0; cell < letters.Length; cell++)
            {
                var die = Dice[order[cell]];
                letters[cell] = die[rng.Next(die.Length)];
            }
            return new LetterGrid(new string(letters));
        }
    }
}
