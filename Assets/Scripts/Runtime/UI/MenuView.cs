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
    /// The main menu (New Game, Courses, sound) and the course select grid. Touch goes through the taps
    /// <see cref="GameUI"/> routes; keyboard and gamepad go through <see cref="Handle"/>.
    /// </summary>
    public sealed class MenuView
    {
        public readonly struct CourseInfo
        {
            public readonly string Name;
            public readonly Sprite Thumbnail;

            public CourseInfo(string name, Sprite thumbnail)
            {
                Name = name;
                Thumbnail = thumbnail;
            }
        }

        /// <summary>Cards per row; matches the grid width in Game.uss.</summary>
        public const int GridColumns = 3;

        const int TitleNew = 0, TitleCourses = 1, TitleSound = 2;
        const float ShakeSeconds = 0.35f;

        readonly GameAssets _assets;
        readonly Action<VisualElement, GameUI.Screen, Action> _registerTap;
        readonly VisualElement[] _titleItems;
        readonly VisualElement _logo, _newButton, _soundIcon, _grid;
        readonly Label _best, _coursesCount, _tally, _hint;
        readonly MenuFocus _titleFocus = new MenuFocus(3);
        readonly List<VisualElement> _cards = new List<VisualElement>();
        MenuFocus _courseFocus = new MenuFocus(1, GridColumns);
        CourseProgress _progress;
        VisualElement _shaking;
        float _shakeTime, _time;

        public event Action NewGameRequested;
        public event Action CoursesRequested;
        public event Action BackRequested;
        public event Action SoundToggleRequested;
        public event Action<int> CourseRequested;
        public event Action LockedCourseChosen;

        public MenuView(VisualElement root, GameAssets assets, Action<VisualElement, GameUI.Screen, Action> registerTap)
        {
            _assets = assets;
            _registerTap = registerTap;

            _logo = Q(root, "title-logo");
            _newButton = Q(root, "menu-new");
            var courses = Q(root, "menu-courses");
            var sound = Q(root, "menu-sound");
            _soundIcon = Q(root, "menu-sound-icon");
            _titleItems = new[] { _newButton, courses, sound };
            _grid = Q(root, "courses-grid");
            _best = root.Q<Label>("title-best");
            _coursesCount = root.Q<Label>("menu-courses-count");
            _tally = root.Q<Label>("courses-tally");
            _hint = root.Q<Label>("courses-hint");

            SetImage(Q(root, "title-art"), assets.titleArt);
            SetImage(Q(root, "courses-art"), assets.titleArt);
            SetImage(_logo, assets.logo);
            SetImage(Q(root, "courses-back-icon"), assets.iconBack);
            SetImage(Q(root, "courses-tally-icon"), assets.iconStar);
            Q(root, "title-scrim").style.backgroundImage = new StyleBackground(MakeScrim());

            registerTap(_newButton, GameUI.Screen.Title, () => Activate(TitleNew));
            registerTap(courses, GameUI.Screen.Title, () => Activate(TitleCourses));
            registerTap(sound, GameUI.Screen.Title, () => Activate(TitleSound));
            registerTap(Q(root, "courses-back"), GameUI.Screen.Courses, () => BackRequested?.Invoke());
        }

        static VisualElement Q(VisualElement root, string name) =>
            root.Q(name) ?? throw new InvalidOperationException($"UI element '{name}' missing from Game.uxml");

        static void SetImage(VisualElement e, Sprite sprite)
        {
            if (sprite != null) e.style.backgroundImage = new StyleBackground(sprite);
        }

        /// <summary>Builds one card per course. Call once, after the levels are parsed.</summary>
        public void SetCourses(IReadOnlyList<CourseInfo> courses)
        {
            _grid.Clear();
            _cards.Clear();
            for (int i = 0; i < courses.Count; i++)
            {
                var card = new VisualElement { name = $"course-{i + 1}", pickingMode = PickingMode.Ignore };
                card.AddToClassList("course-card");

                var thumb = Child(card, "course-thumb");
                SetImage(thumb, courses[i].Thumbnail);
                SetImage(Child(thumb, "course-lock"), _assets.iconLock);
                SetImage(Child(thumb, "course-star"), _assets.iconStar);

                var footer = Child(card, "course-footer");
                AddLabel(footer, $"1-{i + 1}", "course-num");
                AddLabel(footer, courses[i].Name, "course-name");

                _grid.Add(card);
                _cards.Add(card);
                int index = i;
                _registerTap(card, GameUI.Screen.Courses, () =>
                {
                    _courseFocus.Hide();
                    ChooseCourse(index);
                    ApplyFocus();
                });
            }
            _courseFocus = new MenuFocus(courses.Count, GridColumns);
        }

        static VisualElement Child(VisualElement parent, string className)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList(className);
            parent.Add(e);
            return e;
        }

        static void AddLabel(VisualElement parent, string text, string className)
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            l.AddToClassList(className);
            parent.Add(l);
        }

        /// <summary>Refreshes the main menu. <paramref name="focusCourses"/> keeps the highlight on Courses when coming back.</summary>
        public void ShowTitle(int bestScore, CourseProgress progress, bool focusCourses = false)
        {
            _progress = progress;
            _best.text = $"Best {bestScore:000000}";
            _best.style.display = bestScore > 0 ? DisplayStyle.Flex : DisplayStyle.None; // nothing to brag about yet
            _coursesCount.text = $"{progress.UnlockedCount}/{progress.CourseCount}";
            bool keepHighlight = focusCourses && _titleFocus.Visible;
            _titleFocus.Reset(keepHighlight ? TitleCourses : TitleNew, keepHighlight);
            ApplyFocus();
        }

        /// <summary>Refreshes the course cards. The highlight starts on the suggested course, shown only for keyboard players.</summary>
        public void ShowCourses(CourseProgress progress)
        {
            _progress = progress;
            _tally.text = $"{progress.ClearedCount}/{progress.CourseCount}";
            _hint.text = progress.AllCleared ? "Every course cleared! Replay any of them" : "Clear a course to open the next one";
            int next = progress.AllCleared ? -1 : progress.NextUp;
            for (int i = 0; i < _cards.Count; i++)
            {
                _cards[i].EnableInClassList("course-locked", !progress.IsUnlocked(i));
                _cards[i].EnableInClassList("course-cleared", progress.IsCleared(i));
                _cards[i].EnableInClassList("course-next", i == next);
            }
            _courseFocus.Reset(progress.NextUp, _titleFocus.Visible);
            ApplyFocus();
        }

        public void SetSoundIcon(bool muted) => SetImage(_soundIcon, muted ? _assets.iconSoundOff : _assets.iconSoundOn);

        /// <summary>Keyboard / gamepad input on a menu screen. Returns true when it moved or pressed something.</summary>
        public bool Handle(GameUI.Screen screen, MenuCommand command)
        {
            bool handled = screen switch
            {
                GameUI.Screen.Title => HandleTitle(command),
                GameUI.Screen.Courses => HandleCourses(command),
                _ => false,
            };
            ApplyFocus();
            return handled;
        }

        bool HandleTitle(MenuCommand command)
        {
            switch (command)
            {
                case MenuCommand.Up: return _titleFocus.Move(0, -1);
                case MenuCommand.Down: return _titleFocus.Move(0, 1);
                case MenuCommand.Submit:
                    Activate(_titleFocus.Index, fromKeyboard: true);
                    return true;
                default: return false;
            }
        }

        bool HandleCourses(MenuCommand command)
        {
            switch (command)
            {
                case MenuCommand.Up: return _courseFocus.Move(0, -1);
                case MenuCommand.Down: return _courseFocus.Move(0, 1);
                case MenuCommand.Left: return _courseFocus.Move(-1, 0);
                case MenuCommand.Right: return _courseFocus.Move(1, 0);
                case MenuCommand.Back:
                    BackRequested?.Invoke();
                    return true;
                case MenuCommand.Submit:
                    // With no highlight showing, the index is still the suggested (gold) course.
                    return ChooseCourse(_courseFocus.Index);
                default: return false;
            }
        }

        /// <summary>A tap hides the highlight but leaves its position, so Enter still means New Game afterwards.</summary>
        void Activate(int item, bool fromKeyboard = false)
        {
            if (fromKeyboard) _titleFocus.Reset(item, visible: true);
            else _titleFocus.Hide();
            switch (item)
            {
                case TitleNew: NewGameRequested?.Invoke(); break;
                case TitleCourses: CoursesRequested?.Invoke(); break;
                case TitleSound: SoundToggleRequested?.Invoke(); break;
            }
            ApplyFocus();
        }

        bool ChooseCourse(int index)
        {
            if (_progress != null && !_progress.IsUnlocked(index))
            {
                _shaking = _cards[index];
                _shakeTime = ShakeSeconds;
                LockedCourseChosen?.Invoke();
                return false;
            }
            CourseRequested?.Invoke(index);
            return true;
        }

        void ApplyFocus()
        {
            for (int i = 0; i < _titleItems.Length; i++)
                _titleItems[i].EnableInClassList("menu-focused", _titleFocus.Visible && _titleFocus.Index == i);
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].EnableInClassList("menu-focused", _courseFocus.Visible && _courseFocus.Index == i);
        }

        /// <summary>Idle motion: the logo bobs, New Game breathes, and a locked card shakes when picked.</summary>
        public void Tick(float unscaledDt, GameUI.Screen screen)
        {
            _time += unscaledDt;
            if (screen == GameUI.Screen.Title)
            {
                _logo.style.translate = new Translate(0f, Mathf.Sin(_time * 1.8f) * 8f);
                bool focused = _titleFocus.Visible && _titleFocus.Index == TitleNew;
                if (!focused) _newButton.style.scale = new Scale(Vector3.one * (1f + 0.025f * Mathf.Sin(_time * 3.2f)));
                else _newButton.style.scale = StyleKeyword.Null;
            }

            if (_shaking != null)
            {
                _shakeTime -= unscaledDt;
                float k = Mathf.Clamp01(_shakeTime / ShakeSeconds);
                _shaking.style.translate = new Translate(Mathf.Sin(_time * 60f) * 14f * k, 0f);
                if (_shakeTime <= 0f)
                {
                    _shaking.style.translate = StyleKeyword.Null;
                    _shaking = null;
                }
            }
        }

        /// <summary>A left-to-right fade from warm brown to clear, so the menu column reads over the bright art.</summary>
        static Texture2D MakeScrim()
        {
            const int width = 256;
            var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "TitleScrim",
            };
            var color = new Color32(40, 24, 14, 0);
            for (int x = 0; x < width; x++)
            {
                float t = x / (width - 1f);
                float a = t < 0.35f ? 0.55f : Mathf.Lerp(0.55f, 0f, Mathf.SmoothStep(0f, 1f, (t - 0.35f) / 0.3f));
                color.a = (byte)(a * 255f);
                tex.SetPixel(x, 0, color);
            }
            tex.Apply();
            return tex;
        }
    }
}
