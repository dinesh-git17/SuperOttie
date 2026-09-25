using System.Collections;
using System.Linq;
using SuperOttie.Audio;
using SuperOttie.Core;
using SuperOttie.Input;
using SuperOttie.Level;
using SuperOttie.UI;
using SuperOttie.View;
using UnityEngine;
using UnityEngine.UIElements;

namespace SuperOttie.Game
{
    /// <summary>
    /// Top-level game flow: title, level intro card, play, pause, death, course clear, game over and
    /// the final victory screen. Owns the session, the current level and the UI.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public enum Phase
        {
            Boot,
            Title,
            Intro,
            Playing,
            Paused,
            Dying,
            LevelClear,
            GameOver,
            Victory,
        }

        const float IntroSeconds = 2.2f;
        const int TimeBonusPerUnit = 50;

        [SerializeField] GameAssets assets;
        [SerializeField] UIDocument document;
        [SerializeField] AudioManager audioManager;
        [SerializeField] PlatformerCamera gameCamera;

        readonly GameSession _session = new GameSession();
        readonly TouchZones _zones = new TouchZones();
        readonly IHighScoreStore _highScores = new PlayerPrefsHighScoreStore();

        LevelData[] _levels;
        DeviceInput _input;
        GameUI _ui;
        LevelTimer _timer;
        LevelContext _ctx;
        BuiltLevel _level;
        Coroutine _flow;
        bool _checkpointReached;

        public Phase Current { get; private set; } = Phase.Boot;
        public GameSession Session => _session;
        public BuiltLevel Level => _level;
        public int LevelCount => _levels.Length;

        void Awake()
        {
            RuntimeSettings.Apply();
            if (assets == null) assets = GameAssets.Load();
            _levels = assets.levels.Select(t => LevelParser.Parse(t.text)).ToArray();

            audioManager.Init(assets);
            _input = new DeviceInput(_zones);
            _ui = new GameUI(document.rootVisualElement, assets, _zones);
            _ui.PauseRequested += Pause;
            _ui.ResumeRequested += Resume;
            _ui.RestartRequested += RestartLevel;
            _ui.QuitRequested += QuitToTitle;
            _ui.SoundToggleRequested += ToggleSound;
            _ui.SetSoundLabel(audioManager.Muted);
            _session.ExtraLife += () => audioManager.Play(Sfx.OneUp);
        }

        void Start() => GoToTitle();

        void Update()
        {
            _ui.Tick(Time.unscaledDeltaTime, Current == Phase.Playing ? _input.CurrentTouchButtons : default);

            bool tapConsumed = false;
            foreach (var tap in _input.TapsThisFrame)
            {
                if (_ui.HandleTap(tap))
                {
                    tapConsumed = true;
                    audioManager.Play(Sfx.UiTap);
                }
            }

            switch (Current)
            {
                case Phase.Title:
                    if (!tapConsumed && _input.ConfirmPressed()) StartNewGame();
                    break;
                case Phase.Playing:
                    if (_input.PauseKeyPressed())
                    {
                        Pause();
                        break;
                    }
                    _timer.Tick(Time.deltaTime);
                    break;
                case Phase.Paused:
                    if (_input.PauseKeyPressed()) Resume();
                    break;
            }

            if (_level != null && _timer != null) _ui.SetHud(_session, _timer.Display, _session.LevelIndex + 1);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && Current == Phase.Playing) Pause();
        }

        // ---------------------------------------------------------------- flow

        public void StartNewGame()
        {
            _session.Reset();
            RunFlow(LevelIntro(0));
        }

        void RunFlow(IEnumerator routine)
        {
            if (_flow != null) StopCoroutine(_flow);
            _flow = StartCoroutine(routine);
        }

        public void GoToTitle()
        {
            if (_flow != null) StopCoroutine(_flow);
            _flow = null;
            Time.timeScale = 1f;
            CleanupLevel();
            Current = Phase.Title;
            _ui.SetTitle(_highScores.Load());
            _ui.Show(GameUI.Screen.Title);
            _ui.SetFadeImmediate(0f);
            audioManager.PlayMusic(assets.musicTitle);
        }

        IEnumerator LevelIntro(int index, bool fromCheckpoint = false)
        {
            Time.timeScale = 1f;
            Current = Phase.Intro;
            _session.LevelIndex = index;
            audioManager.StopMusic();
            CleanupLevel();
            var data = _levels[index];
            _ui.SetIntro(index + 1, data.Name, _session.Lives);
            _ui.Show(GameUI.Screen.Intro);
            _ui.SetFadeImmediate(0f);
            yield return new WaitForSecondsRealtime(IntroSeconds);

            _checkpointReached = fromCheckpoint && data.Checkpoint.HasValue;
            BuildLevel(data, _checkpointReached);
            _ui.Show(GameUI.Screen.Playing);
            audioManager.PlayMusic(assets.GetTheme(data.Theme).music);
            Current = Phase.Playing;
        }

        void BuildLevel(LevelData data, bool fromCheckpoint)
        {
            _ctx = new LevelContext(data, assets, _session, audioManager, gameCamera.Camera);
            _ctx.PlayerDied += OnPlayerDied;
            _ctx.GoalReached += OnGoalReached;
            _ctx.PlayerFinishedGoal += OnPlayerFinishedGoal;
            _ctx.CheckpointReached += () => _checkpointReached = true;
            _ctx.Popup += (pos, text) => _ui.ShowPopup(pos, text, gameCamera.Camera);
            _input.ResetEdges();
            _level = LevelBuilder.Build(_ctx, _input, fromCheckpoint: fromCheckpoint);
            gameCamera.Follow(_level.Player.transform, _level.CameraBounds);

            _timer = new LevelTimer(data.TimeLimit);
            _timer.HurryUp += () =>
            {
                audioManager.Play(Sfx.Hurry);
                audioManager.PlayMusic(assets.GetTheme(data.Theme).music, true, 1.2f);
                _ui.ShowBanner("HURRY UP!", 2.2f);
            };
            _timer.Expired += () => _level?.Player.Die();
        }

        void CleanupLevel()
        {
            gameCamera.ClearTarget();
            _level?.Destroy();
            _level = null;
            _ctx = null;
            _timer = null;
        }

        void OnPlayerDied()
        {
            if (Current != Phase.Playing) return;
            Current = Phase.Dying;
            audioManager.PlayMusic(assets.jingleDeath, false);
            RunFlow(AfterDeath());
        }

        IEnumerator AfterDeath()
        {
            yield return new WaitForSeconds(3f);
            _ui.FadeTo(1f);
            yield return new WaitForSecondsRealtime(0.4f);
            if (_session.LoseLife()) yield return GameOver();
            else yield return LevelIntro(_session.LevelIndex, _checkpointReached);
        }

        void OnGoalReached(int flagScore)
        {
            Current = Phase.LevelClear;
            audioManager.StopMusic();
        }

        void OnPlayerFinishedGoal() => RunFlow(LevelClear());

        IEnumerator LevelClear()
        {
            audioManager.PlayMusic(assets.jingleLevelClear, false);
            _ui.ShowBanner("COURSE CLEAR!", 3.2f);
            yield return new WaitForSeconds(0.8f);

            // Count remaining time into the score, a few units per frame.
            while (_timer.Display > 0)
            {
                int taken = _timer.Drain(3);
                _session.AddScore(taken * TimeBonusPerUnit);
                audioManager.Play(Sfx.UiTap);
                yield return new WaitForSeconds(0.03f);
            }
            yield return new WaitForSeconds(1.2f);
            _ui.FadeTo(1f);
            yield return new WaitForSecondsRealtime(0.45f);

            int next = _session.LevelIndex + 1;
            if (next < _levels.Length) yield return LevelIntro(next);
            else yield return Victory();
        }

        IEnumerator GameOver()
        {
            Current = Phase.GameOver;
            CleanupLevel();
            _highScores.Save(_session.Score);
            _ui.SetGameOver(_session.Score);
            _ui.Show(GameUI.Screen.GameOver);
            _ui.SetFadeImmediate(0f);
            audioManager.PlayMusic(assets.jingleGameOver, false);
            yield return WaitForConfirm(1.2f);
            GoToTitle();
        }

        IEnumerator Victory()
        {
            Current = Phase.Victory;
            CleanupLevel();
            _highScores.Save(_session.Score);
            _ui.SetVictory(_session.Score, _highScores.Load());
            _ui.Show(GameUI.Screen.Victory);
            _ui.SetFadeImmediate(0f);
            audioManager.Play(Sfx.Fireworks);
            audioManager.PlayMusic(assets.musicEnding);
            yield return WaitForConfirm(1.5f);
            GoToTitle();
        }

        IEnumerator WaitForConfirm(float minimumSeconds)
        {
            yield return new WaitForSecondsRealtime(minimumSeconds);
            while (!_input.ConfirmPressed()) yield return null;
        }

        // ---------------------------------------------------------------- pause menu

        public void Pause()
        {
            if (Current != Phase.Playing) return;
            Current = Phase.Paused;
            Time.timeScale = 0f;
            audioManager.SetMusicPaused(true);
            audioManager.Play(Sfx.Pause);
            _ui.Show(GameUI.Screen.Paused);
        }

        public void Resume()
        {
            if (Current != Phase.Paused) return;
            Current = Phase.Playing;
            Time.timeScale = 1f;
            _input.ResetEdges();
            audioManager.SetMusicPaused(false);
            _ui.Show(GameUI.Screen.Playing);
        }

        void RestartLevel()
        {
            if (Current != Phase.Paused) return;
            audioManager.SetMusicPaused(false);
            RunFlow(LevelIntro(_session.LevelIndex));
        }

        void QuitToTitle()
        {
            if (Current != Phase.Paused) return;
            audioManager.SetMusicPaused(false);
            GoToTitle();
        }

        void ToggleSound()
        {
            audioManager.Muted = !audioManager.Muted;
            _ui.SetSoundLabel(audioManager.Muted);
        }
    }
}
