using System;
using System.Collections.Generic;
using SuperOttie.Core;
using SuperOttie.Game;
using SuperOttie.Input;
using UnityEngine;
using UnityEngine.UIElements;

namespace SuperOttie.UI
{
    /// <summary>
    /// View layer over the UI Toolkit document. Owns screen visibility, HUD text, the safe-area
    /// padding and the screen-space layout of the touch controls. All taps are routed through
    /// <see cref="HandleTap"/> so the UI doesn't depend on an EventSystem and supports multi-touch.
    /// </summary>
    public sealed class GameUI
    {
        public enum Screen
        {
            None,
            Title,
            Courses,
            OutOfLives,
            WordHunt,
            Intro,
            Playing,
            Paused,
            GameOver,
            Victory,
        }

        readonly VisualElement _root, _hud, _controls, _banner, _fade;
        readonly VisualElement _title, _courses, _outOfLives, _wordHunt, _intro, _pause, _gameOver, _victory;
        readonly VisualElement _stick, _stickKnob, _btnJump, _btnPause;
        readonly Label _lives, _coins, _score, _world, _time;
        readonly Label _introWorld, _introName, _introLives;
        readonly Label _gameOverScore, _victoryScore, _victoryBest, _bannerText, _pauseSound;
        readonly List<Label> _blinking = new List<Label>();
        readonly List<(VisualElement element, Screen screen, Action action)> _taps = new List<(VisualElement, Screen, Action)>();
        readonly List<VisualElement> _safeAreas = new List<VisualElement>();
        readonly TouchZones _zones;
        readonly List<(Label label, float age)> _popups = new List<(Label, float)>();
        const float PopupLifetime = 0.9f;

        Rect _appliedSafeArea;
        float _blinkTime;
        float _bannerTime;
        float _fadeTarget, _fadeValue;

        public event Action PauseRequested;
        public event Action ResumeRequested;
        public event Action RestartRequested;
        public event Action QuitRequested;
        public event Action SoundToggleRequested;

        public Screen Current { get; private set; } = Screen.None;
        public MenuView Menu { get; }
        public WordHuntView WordHunt { get; }
        public TouchZones Zones => _zones;
        public bool IsFadeComplete => Mathf.Approximately(_fadeValue, _fadeTarget);

        public GameUI(VisualElement documentRoot, GameAssets assets, TouchZones zones)
        {
            _zones = zones;
            _root = documentRoot.Q("root") ?? documentRoot;
            if (assets.font != null) _root.style.unityFontDefinition = FontDefinition.FromFont(assets.font);

            _hud = Q("hud");
            _controls = Q("controls");
            _banner = Q("banner");
            _fade = Q("fade");
            _title = Q("title");
            _courses = Q("courses");
            _outOfLives = Q("outoflives");
            _wordHunt = Q("wordhunt");
            _intro = Q("intro");
            _pause = Q("pause");
            _gameOver = Q("gameover");
            _victory = Q("victory");
            _stick = Q("stick");
            _stickKnob = Q("stick-knob");
            _btnJump = Q("btn-jump");
            _btnPause = Q("btn-pause");

            _lives = L("hud-lives");
            _coins = L("hud-coins");
            _score = L("hud-score");
            _world = L("hud-world");
            _time = L("hud-time");
            _introWorld = L("intro-world");
            _introName = L("intro-name");
            _introLives = L("intro-lives");
            _gameOverScore = L("gameover-score");
            _victoryScore = L("victory-score");
            _victoryBest = L("victory-best");
            _bannerText = L("banner-text");
            _pauseSound = L("pause-sound");

            SetImage(_stick, assets.stickBase);
            SetImage(_stickKnob, assets.stickKnob);
            SetImage(_btnJump, assets.buttonJump);
            SetImage(_btnPause, assets.buttonPause);
            SetImage(Q("hud-life-icon"), assets.iconLife);
            SetImage(Q("intro-life-icon"), assets.iconLife);
            SetImage(Q("hud-coin-icon"), assets.coin);
            SetImage(Q("victory-ottie"), assets.playerWin);

            _root.Query<Label>(className: "blink").ForEach(l => _blinking.Add(l));

            _taps.Add((_btnPause, Screen.Playing, () => PauseRequested?.Invoke()));
            _taps.Add((Q("pause-resume"), Screen.Paused, () => ResumeRequested?.Invoke()));
            _taps.Add((Q("pause-restart"), Screen.Paused, () => RestartRequested?.Invoke()));
            _taps.Add((_pauseSound, Screen.Paused, () => SoundToggleRequested?.Invoke()));
            _taps.Add((Q("pause-quit"), Screen.Paused, () => QuitRequested?.Invoke()));
            _root.Query(className: "safe-area").ForEach(e => _safeAreas.Add(e));
            Menu = new MenuView(_root, assets, (element, screen, action) => _taps.Add((element, screen, action)));
            WordHunt = new WordHuntView(_root, assets);

            Show(Screen.None);
        }

        VisualElement Q(string name) => _root.Q(name) ?? throw new InvalidOperationException($"UI element '{name}' missing from Game.uxml");

        Label L(string name) => _root.Q<Label>(name) ?? throw new InvalidOperationException($"Label '{name}' missing from Game.uxml");

        static void SetImage(VisualElement e, Sprite sprite)
        {
            if (sprite != null) e.style.backgroundImage = new StyleBackground(sprite);
        }

        static void SetVisible(VisualElement e, bool visible) => e.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        public void Show(Screen screen)
        {
            Current = screen;
            bool gameplay = screen == Screen.Playing || screen == Screen.Paused;
            SetVisible(_hud, gameplay);
            SetVisible(_controls, screen == Screen.Playing);
            if (screen != Screen.Playing) _zones.Release();
            SetVisible(_title, screen == Screen.Title);
            SetVisible(_courses, screen == Screen.Courses);
            SetVisible(_outOfLives, screen == Screen.OutOfLives);
            SetVisible(_wordHunt, screen == Screen.WordHunt);
            SetVisible(_intro, screen == Screen.Intro);
            SetVisible(_pause, screen == Screen.Paused);
            SetVisible(_gameOver, screen == Screen.GameOver);
            SetVisible(_victory, screen == Screen.Victory);
            if (!gameplay)
            {
                HideBanner();
                ClearPopups();
            }
        }

        public void SetIntro(int levelNumber, string levelName, int lives)
        {
            _introWorld.text = $"WORLD 1-{levelNumber}";
            _introName.text = levelName;
            _introLives.text = $"x {lives}";
        }

        (int lives, int coins, int score, int time, int level) _hudShown = (-1, -1, -1, -1, -1);

        /// <summary>Updates HUD labels, formatting strings only when a value actually changed (no per-frame garbage).</summary>
        public void SetHud(GameSession session, int time, int levelNumber)
        {
            var now = (session.Lives, session.Coins, session.Score, time, levelNumber);
            if (now == _hudShown) return;
            if (now.Lives != _hudShown.lives) _lives.text = $"x{now.Lives}";
            if (now.Coins != _hudShown.coins) _coins.text = $"x{now.Coins:00}";
            if (now.Score != _hudShown.score) _score.text = now.Score.ToString("000000");
            if (now.time != _hudShown.time) _time.text = now.time.ToString("000");
            if (now.levelNumber != _hudShown.level) _world.text = $"1-{now.levelNumber}";
            _hudShown = now;
        }

        public void SetGameOver(int score) => _gameOverScore.text = $"Score {score:000000}";

        public void SetVictory(int score, int best)
        {
            _victoryScore.text = $"Score {score:000000}";
            _victoryBest.text = $"Best {best:000000}";
        }

        public void SetSoundLabel(bool muted)
        {
            _pauseSound.text = muted ? "Sound: Off" : "Sound: On";
            Menu.SetSoundIcon(muted);
        }

        public void ShowBanner(string text, float seconds)
        {
            _bannerText.text = text;
            _bannerTime = seconds;
            SetVisible(_banner, true);
        }

        public void HideBanner()
        {
            _bannerTime = 0f;
            SetVisible(_banner, false);
        }

        /// <summary>Floating score text that rises and fades above a world position.</summary>
        public void ShowPopup(Vector3 worldPosition, string text, Camera camera)
        {
            if (_root.panel == null || camera == null) return;
            var p = RuntimePanelUtils.CameraTransformWorldToPanel(_root.panel, worldPosition, camera);
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("popup");
            label.style.left = p.x;
            label.style.top = p.y;
            _root.Add(label);
            _popups.Add((label, 0f));
        }

        void TickPopups(float dt)
        {
            for (int i = _popups.Count - 1; i >= 0; i--)
            {
                var (label, age) = _popups[i];
                age += dt;
                if (age >= PopupLifetime)
                {
                    label.RemoveFromHierarchy();
                    _popups.RemoveAt(i);
                    continue;
                }
                _popups[i] = (label, age);
                float k = age / PopupLifetime;
                label.style.translate = new Translate(Length.Percent(-50), -90f * k);
                label.style.opacity = 1f - k * k;
            }
        }

        public void ClearPopups()
        {
            foreach (var (label, _) in _popups) label.RemoveFromHierarchy();
            _popups.Clear();
        }

        /// <summary>0 = clear, 1 = black. Animated in <see cref="Tick"/>.</summary>
        public void FadeTo(float value) => _fadeTarget = Mathf.Clamp01(value);

        public void SetFadeImmediate(float value)
        {
            _fadeTarget = _fadeValue = Mathf.Clamp01(value);
            _fade.style.opacity = _fadeValue;
        }

        /// <summary>Call every frame with unscaled time (works while paused).</summary>
        public void Tick(float unscaledDt, TouchButtons pressed)
        {
            ApplySafeArea();
            UpdateTouchZones();

            _stick.EnableInClassList("stick-held", pressed.StickHeld);
            // The knob travels up to the ring's inner edge, following the finger in both axes.
            float travel = (_stick.resolvedStyle.width - _stickKnob.resolvedStyle.width) * 0.5f;
            if (!(travel > 0f)) travel = 0f;
            _stickKnob.style.translate = new Translate(pressed.Stick.x * travel, -pressed.Stick.y * travel);
            _btnJump.EnableInClassList("ctrl-pressed", pressed.Jump);

            _blinkTime += unscaledDt;
            float blink = 0.55f + 0.45f * Mathf.Cos(_blinkTime * 4f);
            foreach (var l in _blinking) l.style.opacity = blink;

            if (_bannerTime > 0f)
            {
                _bannerTime -= unscaledDt;
                float pop = Mathf.Clamp01((2.2f - _bannerTime) * 6f);
                _banner.style.scale = new Scale(Vector3.one * Mathf.Lerp(0.4f, 1f, pop));
                if (_bannerTime <= 0f) HideBanner();
            }

            Menu.Tick(unscaledDt, Current);
            TickPopups(unscaledDt);
            _fadeValue = Mathf.MoveTowards(_fadeValue, _fadeTarget, unscaledDt * 3f);
            _fade.style.opacity = _fadeValue;
        }

        /// <summary>Routes a tap (screen pixels, bottom-left origin). Returns true if a UI control used it.</summary>
        public bool HandleTap(Vector2 screenPosition)
        {
            var p = ScreenToPanel(screenPosition);
            foreach (var (element, screen, action) in _taps)
            {
                if (screen != Current) continue;
                var r = element.worldBound;
                // Small grace margin so near-misses on small buttons still land.
                r = new Rect(r.x - 16f, r.y - 16f, r.width + 32f, r.height + 32f);
                if (r.Contains(p))
                {
                    action();
                    return true;
                }
            }
            return false;
        }

        float PanelScale
        {
            get
            {
                float w = _root.panel?.visualTree.layout.width ?? 0f;
                return w > 0f && !float.IsNaN(w) ? UnityEngine.Screen.width / w : 1f;
            }
        }

        /// <summary>Screen pixels (bottom-left origin) to panel coordinates (top-left origin, reference pixels).</summary>
        public Vector2 ScreenToPanel(Vector2 screen)
        {
            float s = PanelScale;
            return new Vector2(screen.x / s, (UnityEngine.Screen.height - screen.y) / s);
        }

        Rect PanelToScreen(Rect r)
        {
            float s = PanelScale;
            return new Rect(r.x * s, UnityEngine.Screen.height - r.yMax * s, r.width * s, r.height * s);
        }

        void UpdateTouchZones()
        {
            if (_controls.resolvedStyle.display == DisplayStyle.None) return;
            var stick = _stick.worldBound;
            if (float.IsNaN(stick.width) || stick.width <= 0f) return;
            var stickOnScreen = PanelToScreen(stick);
            _zones.StickCenter = stickOnScreen.center;
            _zones.StickRadius = stickOnScreen.width * 0.5f;
            _zones.Jump = PanelToScreen(_btnJump.worldBound);
        }

        /// <summary>Pads gameplay and menu UI away from the Dynamic Island, rounded corners and home indicator.</summary>
        void ApplySafeArea()
        {
            var safe = UnityEngine.Screen.safeArea;
            if (safe == _appliedSafeArea) return;
            float w = _root.panel?.visualTree.layout.width ?? 0f;
            if (!(w > 0f)) return; // layout not ready yet; try again next frame
            float s = UnityEngine.Screen.width / w;
            _appliedSafeArea = safe;
            foreach (var e in _safeAreas)
            {
                e.style.left = safe.xMin / s;
                e.style.right = (UnityEngine.Screen.width - safe.xMax) / s;
                e.style.top = (UnityEngine.Screen.height - safe.yMax) / s;
                e.style.bottom = safe.yMin / s;
            }
        }
    }
}
