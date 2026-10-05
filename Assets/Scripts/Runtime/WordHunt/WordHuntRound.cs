using System;
using System.Collections.Generic;
using System.Text;

namespace SuperOttie.WordHunt
{
    public enum SubmitResult
    {
        None,
        TooShort,
        NotAWord,
        NotOnBoard,
        AlreadyFound,
        Found,
    }

    /// <summary>How the traced path reads right now (drives the tile colours, like iMessage Word Hunt).</summary>
    public enum PathState
    {
        Neutral,
        NewWord,
        AlreadyFound,
    }

    /// <summary>
    /// One timed round: trace words through neighbouring tiles; finding <see cref="WordsToWin"/> new words before
    /// the clock runs out wins. Pure C# so the rules are unit-tested; the view only feeds it touched cells.
    /// </summary>
    public sealed class WordHuntRound
    {
        public const float DefaultTimeLimit = 80f;
        public const int DefaultWordsToWin = 3;

        readonly WordList _words;
        readonly List<int> _path = new List<int>(LetterGrid.CellCount);
        readonly List<string> _found = new List<string>();
        readonly StringBuilder _current = new StringBuilder(LetterGrid.CellCount);

        public LetterGrid Grid { get; }
        public int WordsToWin { get; }
        public float TimeLeft { get; private set; }
        public int Points { get; private set; }

        public IReadOnlyList<int> Path => _path;
        public IReadOnlyList<string> Found => _found;
        public string CurrentWord => _current.ToString();

        public bool IsWon => _found.Count >= WordsToWin;
        public bool IsLost => !IsWon && TimeLeft <= 0f;
        public bool IsOver => IsWon || IsLost;

        public WordHuntRound(LetterGrid grid, WordList words, float timeLimit = DefaultTimeLimit, int wordsToWin = DefaultWordsToWin)
        {
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _words = words ?? throw new ArgumentNullException(nameof(words));
            TimeLeft = timeLimit;
            WordsToWin = wordsToWin;
        }

        /// <summary>iMessage Word Hunt scoring: 100, 400, 800, 1400, then 400 more per extra letter.</summary>
        public static int PointsFor(int length)
        {
            if (length < GridSolver.MinLength) return 0;
            switch (length)
            {
                case 3: return 100;
                case 4: return 400;
                case 5: return 800;
                default: return 1400 + (length - 6) * 400;
            }
        }

        public void Tick(float dt)
        {
            if (IsOver || dt <= 0f) return;
            TimeLeft = Math.Max(0f, TimeLeft - dt);
            if (IsLost) ClearPath();
        }

        /// <summary>
        /// The finger is over <paramref name="cell"/>: start a path, extend it to a neighbour, or slide back onto
        /// the previous tile to undo the last letter. Returns true when the path changed.
        /// </summary>
        public bool Touch(int cell)
        {
            if (IsOver || cell < 0 || cell >= LetterGrid.CellCount) return false;
            if (_path.Count == 0)
            {
                Push(cell);
                return true;
            }
            int last = _path[_path.Count - 1];
            if (cell == last) return false;
            if (_path.Count >= 2 && cell == _path[_path.Count - 2])
            {
                _path.RemoveAt(_path.Count - 1);
                _current.Length--;
                return true;
            }
            if (_path.Contains(cell) || !LetterGrid.Adjacent(last, cell)) return false;
            Push(cell);
            return true;
        }

        void Push(int cell)
        {
            _path.Add(cell);
            _current.Append(Grid[cell]);
        }

        public PathState State => StateOf(CurrentWord);

        /// <summary>How a word would score right now (also used to colour a typed word's path).</summary>
        public PathState StateOf(string word)
        {
            if (word == null || word.Length < GridSolver.MinLength) return PathState.Neutral;
            if (_found.Contains(word)) return PathState.AlreadyFound;
            return _words.Contains(word) ? PathState.NewWord : PathState.Neutral;
        }

        /// <summary>The finger lifted: score the traced word (if any) and clear the path.</summary>
        public SubmitResult Release()
        {
            if (IsOver || _path.Count == 0)
            {
                ClearPath();
                return SubmitResult.None;
            }
            var result = Score(CurrentWord);
            ClearPath();
            return result;
        }

        /// <summary>Keyboard play: the word must be traceable on the board.</summary>
        public SubmitResult SubmitTyped(string word)
        {
            if (IsOver || string.IsNullOrWhiteSpace(word)) return SubmitResult.None;
            word = word.Trim().ToLowerInvariant();
            if (word.Length < GridSolver.MinLength) return SubmitResult.TooShort;
            if (Grid.FindPath(word) == null) return SubmitResult.NotOnBoard;
            return Score(word);
        }

        SubmitResult Score(string word)
        {
            if (word.Length < GridSolver.MinLength) return SubmitResult.TooShort;
            if (_found.Contains(word)) return SubmitResult.AlreadyFound;
            if (!_words.Contains(word)) return SubmitResult.NotAWord;
            _found.Add(word);
            Points += PointsFor(word.Length);
            return SubmitResult.Found;
        }

        void ClearPath()
        {
            _path.Clear();
            _current.Clear();
        }
    }
}
