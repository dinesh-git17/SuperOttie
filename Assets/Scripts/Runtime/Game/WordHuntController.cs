using System;
using System.Collections.Generic;
using System.Text;
using SuperOttie.Audio;
using SuperOttie.Input;
using SuperOttie.UI;
using SuperOttie.WordHunt;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SuperOttie.Game
{
    /// <summary>
    /// Runs one Word Hunt round: rolls a board, feeds the finger (or typed letters) into the
    /// <see cref="WordHuntRound"/>, plays the sounds, and reports the outcome to the game flow.
    /// </summary>
    public sealed class WordHuntController
    {
        const float WarningSeconds = 10f;
        const int MaxTypedLetters = 12;

        static WordList _sharedWords; // ~150k words, parsed once on first use

        readonly GameAssets _assets;
        readonly WordHuntView _view;
        readonly DeviceInput _input;
        readonly IGameAudio _audio;
        readonly Func<Vector2, Vector2> _screenToPanel;
        readonly StringBuilder _typed = new StringBuilder(MaxTypedLetters);
        Keyboard _keyboard;
        bool _pointerDown, _warned, _finished;

        public WordHuntRound Round { get; private set; }

        /// <summary>A new word was found; the argument is its points.</summary>
        public event Action<int> WordScored;

        /// <summary>The round ended; true when it was won.</summary>
        public event Action<bool> Finished;

        public WordHuntController(GameAssets assets, WordHuntView view, DeviceInput input, IGameAudio audio, Func<Vector2, Vector2> screenToPanel)
        {
            _assets = assets;
            _view = view;
            _input = input;
            _audio = audio;
            _screenToPanel = screenToPanel;
        }

        public static WordList LoadWords(GameAssets assets)
        {
            if (_sharedWords == null)
            {
                if (assets.wordList == null) throw new InvalidOperationException("GameAssets.wordList is missing. Run 'Super Ottie > Setup Project'.");
                _sharedWords = WordList.Parse(assets.wordList.text);
            }
            return _sharedWords;
        }

        public void Begin(int seed, Sprite backdrop)
        {
            var words = LoadWords(_assets);
            Round = new WordHuntRound(GridGenerator.Generate(words, new System.Random(seed)), words);
            _pointerDown = _warned = _finished = false;
            _typed.Clear();
            _view.Begin(Round, backdrop);
            SetKeyboard(Keyboard.current);
        }

        /// <summary>Stops listening for typed letters (the round is over or the screen was left).</summary>
        public void End() => SetKeyboard(null);

        void SetKeyboard(Keyboard keyboard)
        {
            if (_keyboard != null) _keyboard.onTextInput -= OnTextInput;
            _keyboard = keyboard;
            if (_keyboard != null) _keyboard.onTextInput += OnTextInput;
        }

        public void Tick(float dt)
        {
            if (Round == null || _finished) return;
            Round.Tick(dt);
            if (!Round.IsOver)
            {
                HandlePointer();
                HandleKeys();
                if (!_warned && Round.TimeLeft <= WarningSeconds)
                {
                    _warned = true;
                    _audio.Play(Sfx.Hurry);
                }
            }
            _view.Refresh(Round);
            _view.Tick(dt);
            if (Round.IsOver) Finish();
        }

        void HandlePointer()
        {
            if (_input.TryGetPointer(out var screen))
            {
                if (!_pointerDown)
                {
                    _pointerDown = true;
                    _typed.Clear();
                }
                int cell = _view.CellAt(_screenToPanel(screen));
                if (cell >= 0 && Round.Touch(cell))
                {
                    _audio.Play(Sfx.UiTap);
                    _view.ShowPath(Round.Path, Round.State, Round.CurrentWord);
                }
            }
            else if (_pointerDown)
            {
                _pointerDown = false;
                string word = Round.CurrentWord;
                Report(Round.Release(), word);
                _view.ShowPath(Round.Path, PathState.Neutral, string.Empty);
            }
        }

        // ---------------------------------------------------------------- keyboard: type a word, Enter to submit

        void OnTextInput(char c)
        {
            if (Round == null || Round.IsOver || _pointerDown) return;
            if (c < 'A' || (c > 'Z' && c < 'a') || c > 'z' || _typed.Length >= MaxTypedLetters) return;
            _typed.Append(char.ToLowerInvariant(c));
            ShowTyped();
        }

        void HandleKeys()
        {
            if (_keyboard == null || _pointerDown) return;
            if (_keyboard.backspaceKey.wasPressedThisFrame && _typed.Length > 0)
            {
                _typed.Length--;
                ShowTyped();
            }
            else if (_keyboard.escapeKey.wasPressedThisFrame && _typed.Length > 0)
            {
                _typed.Clear();
                ShowTyped();
            }
            else if ((_keyboard.enterKey.wasPressedThisFrame || _keyboard.numpadEnterKey.wasPressedThisFrame) && _typed.Length > 0)
            {
                string word = _typed.ToString();
                _typed.Clear();
                Report(Round.SubmitTyped(word), word);
                _view.ShowPath(Array.Empty<int>(), PathState.Neutral, string.Empty);
            }
        }

        void ShowTyped()
        {
            string word = _typed.ToString();
            IReadOnlyList<int> path = Round.Grid.FindPath(word);
            _view.ShowPath(path ?? (IReadOnlyList<int>)Array.Empty<int>(), path != null ? Round.StateOf(word) : PathState.Neutral, word);
        }

        // ---------------------------------------------------------------- results

        void Report(SubmitResult result, string word)
        {
            int points = WordHuntRound.PointsFor(word.Length);
            switch (result)
            {
                case SubmitResult.Found:
                    _audio.Play(Sfx.Coin);
                    WordScored?.Invoke(points);
                    break;
                case SubmitResult.AlreadyFound:
                case SubmitResult.NotAWord:
                case SubmitResult.NotOnBoard:
                    _audio.Play(Sfx.Bump);
                    break;
            }
            _view.ShowResult(result, word, points);
        }

        void Finish()
        {
            _finished = true;
            End();
            _view.ShowOutcome(Round.IsWon);
            Finished?.Invoke(Round.IsWon);
        }
    }
}
