using System;
using System.Collections.Generic;
using SuperOttie.Game;
using SuperOttie.WordHunt;
using UnityEngine;
using UnityEngine.UIElements;

namespace SuperOttie.UI
{
    /// <summary>
    /// Draws a <see cref="WordHuntRound"/>: the 4x4 tiles, the trail behind the finger, the word being traced,
    /// the clock and the three word slots. Also maps panel positions to tiles for the controller.
    /// </summary>
    public sealed class WordHuntView
    {
        public const float TileSize = 176f;
        public const float TileGap = 18f;
        public const float BoardPadding = 24f;

        /// <summary>
        /// Tiles respond inside a circle, not their whole square, so a diagonal drag passes through the gap
        /// between the two side neighbours without catching either (as in iMessage Word Hunt).
        /// </summary>
        public const float HitRadius = TileSize * 0.46f;

        const float LowTime = 10f;
        const float FeedbackSeconds = 0.75f;

        static readonly Color TrailNeutral = new Color(1f, 1f, 1f, 0.55f);
        static readonly Color TrailNew = new Color(0.45f, 0.8f, 0.3f, 0.75f);
        static readonly Color TrailFound = new Color(1f, 0.8f, 0.15f, 0.75f);

        readonly VisualElement _board, _trail, _bubble, _timeFill, _result, _ottie, _art;
        readonly Label _word, _time, _points, _resultText;
        readonly VisualElement[] _tiles = new VisualElement[LetterGrid.CellCount];
        readonly Label[] _letters = new Label[LetterGrid.CellCount];
        readonly List<(VisualElement slot, Label word)> _slots = new List<(VisualElement, Label)>();
        readonly GameAssets _assets;

        IReadOnlyList<int> _trailPath = Array.Empty<int>();
        PathState _trailState;
        float _feedbackTime;
        string _shownTime;
        int _shownFound = -1;
        bool _shaking;

        public WordHuntView(VisualElement root, GameAssets assets)
        {
            _assets = assets;
            _board = Q(root, "wordhunt-board");
            _bubble = Q(root, "wordhunt-bubble");
            _timeFill = Q(root, "wordhunt-timefill");
            _result = Q(root, "wordhunt-result");
            _ottie = Q(root, "wordhunt-ottie");
            _word = root.Q<Label>("wordhunt-word");
            _time = root.Q<Label>("wordhunt-time");
            _points = root.Q<Label>("wordhunt-points");
            _resultText = root.Q<Label>("wordhunt-result-text");
            _art = Q(root, "wordhunt-art");
            SetImage(_art, assets.titleArt);
            SetImage(_ottie, assets.playerIdle);

            float side = BoardPadding * 2f + LetterGrid.Size * TileSize + (LetterGrid.Size - 1) * TileGap;
            _board.style.width = side;
            _board.style.height = side;
            for (int cell = 0; cell < LetterGrid.CellCount; cell++)
            {
                var tile = new VisualElement { pickingMode = PickingMode.Ignore };
                tile.AddToClassList("wh-tile");
                var origin = TileOrigin(cell);
                tile.style.left = origin.x;
                tile.style.top = origin.y;
                tile.style.width = TileSize;
                tile.style.height = TileSize;
                var letter = new Label { pickingMode = PickingMode.Ignore };
                letter.AddToClassList("wh-letter");
                tile.Add(letter);
                _board.Add(tile);
                _tiles[cell] = tile;
                _letters[cell] = letter;
            }

            // Drawn above the tiles so the line reads on top, like the classic.
            _trail = new VisualElement { pickingMode = PickingMode.Ignore };
            _trail.AddToClassList("wh-trail");
            _trail.generateVisualContent += DrawTrail;
            _board.Add(_trail);

            var slots = Q(root, "wordhunt-slots");
            for (int i = 0; i < WordHuntRound.DefaultWordsToWin; i++)
            {
                var slot = new VisualElement { pickingMode = PickingMode.Ignore };
                slot.AddToClassList("wh-slot");
                var star = new VisualElement { pickingMode = PickingMode.Ignore };
                star.AddToClassList("wh-slot-star");
                SetImage(star, assets.iconStar);
                var word = new Label("?") { pickingMode = PickingMode.Ignore };
                word.AddToClassList("wh-slot-word");
                slot.Add(star);
                slot.Add(word);
                slots.Add(slot);
                _slots.Add((slot, word));
            }
        }

        static VisualElement Q(VisualElement root, string name) =>
            root.Q(name) ?? throw new InvalidOperationException($"UI element '{name}' missing from Game.uxml");

        static void SetImage(VisualElement e, Sprite sprite)
        {
            if (sprite != null) e.style.backgroundImage = new StyleBackground(sprite);
        }

        /// <summary>Top-left of a tile inside the board's content box.</summary>
        static Vector2 TileOrigin(int cell) => new Vector2(
            BoardPadding + LetterGrid.Column(cell) * (TileSize + TileGap),
            BoardPadding + LetterGrid.Row(cell) * (TileSize + TileGap));

        static Vector2 TileCenter(int cell) => TileOrigin(cell) + Vector2.one * (TileSize * 0.5f);

        /// <summary>The tile under a panel-space point (inside its hit circle), or -1.</summary>
        public int CellAt(Vector2 panelPosition)
        {
            if (_board.panel == null) return -1;
            // Tiles are placed inside the board's border, so measure from there.
            var local = _board.WorldToLocal(panelPosition)
                - new Vector2(_board.resolvedStyle.borderLeftWidth, _board.resolvedStyle.borderTopWidth);
            return CellAtLocal(local);
        }

        /// <summary><see cref="CellAt"/> in tile coordinates (inside the board border); unit-testable without a panel.</summary>
        public static int CellAtLocal(Vector2 local)
        {
            for (int cell = 0; cell < LetterGrid.CellCount; cell++)
                if ((local - TileCenter(cell)).sqrMagnitude <= HitRadius * HitRadius) return cell;
            return -1;
        }

        /// <summary>Resets the screen for a new round, over the backdrop of the course being played.</summary>
        public void Begin(WordHuntRound round, Sprite backdrop)
        {
            SetImage(_art, backdrop != null ? backdrop : _assets.titleArt);
            for (int cell = 0; cell < LetterGrid.CellCount; cell++)
                _letters[cell].text = char.ToUpperInvariant(round.Grid[cell]).ToString();
            foreach (var (slot, word) in _slots)
            {
                slot.RemoveFromClassList("wh-slot-filled");
                word.text = "?";
            }
            _feedbackTime = 0f;
            _shownTime = null;
            _shownFound = -1;
            _result.style.display = DisplayStyle.None;
            SetImage(_ottie, _assets.playerIdle);
            _points.text = "0 points";
            ShowPath(Array.Empty<int>(), PathState.Neutral, string.Empty);
            Refresh(round);
        }

        /// <summary>Shows the traced (or typed) path and word.</summary>
        public void ShowPath(IReadOnlyList<int> path, PathState state, string word)
        {
            for (int cell = 0; cell < LetterGrid.CellCount; cell++) _tiles[cell].EnableInClassList("wh-tile-on", Contains(path, cell));
            _board.EnableInClassList("wh-state-new", state == PathState.NewWord);
            _board.EnableInClassList("wh-state-found", state == PathState.AlreadyFound);
            _trailPath = path;
            _trailState = state;
            _trail.MarkDirtyRepaint();

            if (_feedbackTime > 0f && word.Length == 0) return; // let the last result linger until a new trace starts
            _feedbackTime = 0f;
            _shaking = false;
            _bubble.style.translate = StyleKeyword.Null;
            _word.text = word.ToUpperInvariant();
            _bubble.style.opacity = word.Length > 0 ? 1f : 0f;
            SetBubbleState(state == PathState.NewWord ? "wh-state-new" : state == PathState.AlreadyFound ? "wh-state-found" : null);
        }

        static bool Contains(IReadOnlyList<int> path, int cell)
        {
            for (int i = 0; i < path.Count; i++)
                if (path[i] == cell) return true;
            return false;
        }

        void SetBubbleState(string className)
        {
            _bubble.EnableInClassList("wh-state-new", className == "wh-state-new");
            _bubble.EnableInClassList("wh-state-found", className == "wh-state-found");
            _bubble.EnableInClassList("wh-state-bad", className == "wh-state-bad");
        }

        /// <summary>Briefly shows how a submitted word went ("+400", "Already found", "Not a word").</summary>
        public void ShowResult(SubmitResult result, string word, int points)
        {
            string text;
            string state;
            switch (result)
            {
                case SubmitResult.Found:
                    text = $"{word.ToUpperInvariant()}  +{points}";
                    state = "wh-state-new";
                    break;
                case SubmitResult.AlreadyFound:
                    text = "Already found";
                    state = "wh-state-found";
                    break;
                case SubmitResult.NotAWord:
                    text = "Not a word";
                    state = "wh-state-bad";
                    break;
                case SubmitResult.NotOnBoard:
                    text = "Not on the board";
                    state = "wh-state-bad";
                    break;
                default:
                    return;
            }
            _word.text = text;
            SetBubbleState(state);
            _bubble.style.opacity = 1f;
            _feedbackTime = FeedbackSeconds;
            _shaking = state == "wh-state-bad";
        }

        /// <summary>Per-frame refresh of the clock, the slots and the points.</summary>
        public void Refresh(WordHuntRound round)
        {
            // Strings are rebuilt only when what they show changes (no per-frame garbage).
            int seconds = Mathf.CeilToInt(round.TimeLeft);
            string clock = $"{seconds / 60}:{seconds % 60:00}";
            if (clock != _shownTime)
            {
                _time.text = clock;
                _shownTime = clock;
                bool low = round.TimeLeft <= LowTime && !round.IsWon;
                _time.EnableInClassList("wordhunt-time-low", low);
                _timeFill.EnableInClassList("wordhunt-time-low", low);
            }
            if (round.Found.Count != _shownFound)
            {
                _shownFound = round.Found.Count;
                for (int i = 0; i < _slots.Count; i++)
                {
                    bool filled = i < round.Found.Count;
                    _slots[i].slot.EnableInClassList("wh-slot-filled", filled);
                    _slots[i].word.text = filled ? round.Found[i].ToUpperInvariant() : "?";
                }
                _points.text = $"{round.Points} points";
            }
            _timeFill.style.width = Length.Percent(Mathf.Clamp01(round.TimeLeft / WordHuntRound.DefaultTimeLimit) * 100f);
        }

        public void ShowOutcome(bool won)
        {
            _resultText.text = won ? "You won a life!" : "Time's up!";
            _result.style.display = DisplayStyle.Flex;
            SetImage(_ottie, won ? _assets.playerWin : _assets.playerHurt);
            ShowPath(Array.Empty<int>(), PathState.Neutral, string.Empty);
        }

        public void Tick(float unscaledDt)
        {
            if (_feedbackTime <= 0f) return;
            _feedbackTime -= unscaledDt;
            float k = Mathf.Clamp01(_feedbackTime / FeedbackSeconds);
            if (_shaking) _bubble.style.translate = new Translate(Mathf.Sin(_feedbackTime * 60f) * 14f * k, 0f);
            if (_feedbackTime <= 0f)
            {
                _bubble.style.opacity = 0f;
                _bubble.style.translate = StyleKeyword.Null;
                _shaking = false;
            }
        }

        void DrawTrail(MeshGenerationContext ctx)
        {
            if (_trailPath.Count < 2) return;
            var p = ctx.painter2D;
            p.lineWidth = 30f;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            p.strokeColor = _trailState == PathState.NewWord ? TrailNew : _trailState == PathState.AlreadyFound ? TrailFound : TrailNeutral;
            p.BeginPath();
            // The trail is absolutely positioned like the tiles, so they share coordinates.
            p.MoveTo(TileCenter(_trailPath[0]));
            for (int i = 1; i < _trailPath.Count; i++) p.LineTo(TileCenter(_trailPath[i]));
            p.Stroke();
        }
    }
}
