using System.Collections;
using NUnit.Framework;
using SuperOttie.Audio;
using SuperOttie.Core;
using SuperOttie.Game;
using SuperOttie.Input;
using SuperOttie.UI;
using SuperOttie.Level;
using SuperOttie.View;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace SuperOttie.Tests
{
    public class GameFlowTests
    {
        // These tests run the real game, which saves progress to PlayerPrefs; keep the editor's own progress intact.
        int? _savedProgress;

        [SetUp]
        public void SaveProgress()
        {
            _savedProgress = PlayerPrefs.HasKey(PlayerPrefsCourseProgressStore.Key) ? PlayerPrefs.GetInt(PlayerPrefsCourseProgressStore.Key) : (int?)null;
            PlayerPrefs.DeleteKey(PlayerPrefsCourseProgressStore.Key);
        }

        [TearDown]
        public void RestoreProgress()
        {
            if (_savedProgress.HasValue) PlayerPrefs.SetInt(PlayerPrefsCourseProgressStore.Key, _savedProgress.Value);
            else PlayerPrefs.DeleteKey(PlayerPrefsCourseProgressStore.Key);
            Time.timeScale = 1f;
        }

        static IEnumerator BootToMenu(System.Action<GameManager> found)
        {
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null);
            Assert.That(game.Current, Is.EqualTo(GameManager.Phase.Title));
            found(game);
        }

        static IEnumerator WaitForPlaying(GameManager game)
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            while (game.Current != GameManager.Phase.Playing && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(game.Current, Is.EqualTo(GameManager.Phase.Playing));
        }

        static VisualElement Card(int number) =>
            Object.FindFirstObjectByType<UIDocument>().rootVisualElement.Q($"course-{number}");

        [UnityTest]
        public IEnumerator CourseSelect_FreshSave_OnlyFirstCourseIsPlayable()
        {
            GameManager game = null;
            yield return BootToMenu(g => game = g);
            Assert.That(game.Progress.UnlockedCount, Is.EqualTo(1));

            game.OpenCourses();
            Assert.That(game.Current, Is.EqualTo(GameManager.Phase.Courses));
            yield return null;
            Assert.That(Card(1).ClassListContains("course-locked"), Is.False);
            Assert.That(Card(1).ClassListContains("course-next"), Is.True, "the first course is the suggestion");
            Assert.That(Card(2).ClassListContains("course-locked"), Is.True);
            Assert.That(Card(game.LevelCount).ClassListContains("course-locked"), Is.True);

            game.StartCourse(1);
            Assert.That(game.Current, Is.EqualTo(GameManager.Phase.Courses), "a locked course can't be started");

            game.StartCourse(0);
            Assert.That(game.Current, Is.EqualTo(GameManager.Phase.Intro));
        }

        [UnityTest]
        public IEnumerator ClearingACourse_UnlocksAndSavesTheNext_ThenItCanBeChosen()
        {
            GameManager game = null;
            yield return BootToMenu(g => game = g);
            game.StartNewGame();
            yield return WaitForPlaying(game);

            game.Level.Goal.Reach(game.Level.Player);
            Assert.That(game.Current, Is.EqualTo(GameManager.Phase.LevelClear));
            Assert.That(game.Progress.IsCleared(0), Is.True);
            Assert.That(game.Progress.IsUnlocked(1), Is.True);
            Assert.That(PlayerPrefs.GetInt(PlayerPrefsCourseProgressStore.Key), Is.EqualTo(1), "saved straight away");

            game.GoToTitle();
            game.OpenCourses();
            yield return null;
            Assert.That(Card(1).ClassListContains("course-cleared"), Is.True);
            Assert.That(Card(2).ClassListContains("course-locked"), Is.False);
            Assert.That(Card(2).ClassListContains("course-next"), Is.True);

            game.StartCourse(1);
            yield return WaitForPlaying(game);
            Assert.That(game.Session.LevelIndex, Is.EqualTo(1));
            Assert.That(game.Session.Lives, Is.EqualTo(GameSession.StartingLives), "a course pick starts a fresh run");
            Assert.That(game.Session.Score, Is.Zero);
        }

        static MenuView Menu(GameManager game) =>
            ((GameUI)typeof(GameManager).GetField("_ui", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(game)).Menu;

        [UnityTest]
        public IEnumerator Keyboard_EnterOnTheMenu_StartsANewGame()
        {
            GameManager game = null;
            yield return BootToMenu(g => game = g);
            Menu(game).Handle(GameUI.Screen.Title, MenuCommand.Submit);
            Assert.That(game.Current, Is.EqualTo(GameManager.Phase.Intro));
            Assert.That(game.Session.LevelIndex, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Keyboard_NavigatesToCourses_AndBackOut()
        {
            GameManager game = null;
            yield return BootToMenu(g => game = g);
            var menu = Menu(game);
            menu.Handle(GameUI.Screen.Title, MenuCommand.Down); // reveal the highlight on New Game
            menu.Handle(GameUI.Screen.Title, MenuCommand.Down); // move to Courses
            menu.Handle(GameUI.Screen.Title, MenuCommand.Submit);
            Assert.That(game.Current, Is.EqualTo(GameManager.Phase.Courses));

            menu.Handle(GameUI.Screen.Courses, MenuCommand.Back);
            Assert.That(game.Current, Is.EqualTo(GameManager.Phase.Title));
        }

        [UnityTest]
        public IEnumerator Keyboard_EnterOnCourses_PlaysTheSuggestedCourse_AndLockedOnesStayShut()
        {
            PlayerPrefs.SetInt(PlayerPrefsCourseProgressStore.Key, 0b1); // course 1 cleared, course 2 suggested
            GameManager game = null;
            yield return BootToMenu(g => game = g);
            var menu = Menu(game);
            game.OpenCourses();

            menu.Handle(GameUI.Screen.Courses, MenuCommand.Right); // reveal on course 2
            menu.Handle(GameUI.Screen.Courses, MenuCommand.Right); // course 3, locked
            Assert.That(menu.Handle(GameUI.Screen.Courses, MenuCommand.Submit), Is.False);
            Assert.That(game.Current, Is.EqualTo(GameManager.Phase.Courses));

            game.GoToTitle();
            game.OpenCourses();
            menu.Handle(GameUI.Screen.Courses, MenuCommand.Submit); // no highlight yet: the gold course
            Assert.That(game.Current, Is.EqualTo(GameManager.Phase.Intro));
            Assert.That(game.Session.LevelIndex, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SavedProgress_IsLoadedAtBoot()
        {
            PlayerPrefs.SetInt(PlayerPrefsCourseProgressStore.Key, 0b111);
            GameManager game = null;
            yield return BootToMenu(g => game = g);
            Assert.That(game.Progress.ClearedCount, Is.EqualTo(3));
            Assert.That(game.Progress.UnlockedCount, Is.EqualTo(Mathf.Min(4, game.LevelCount)));
        }

        [UnityTest]
        public IEnumerator MainScene_BootsToTitle_ThenStartsFirstLevel()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null);
            Assert.That(game.Current, Is.EqualTo(GameManager.Phase.Title));
            Assert.That(game.LevelCount, Is.GreaterThanOrEqualTo(3));

            game.StartNewGame();
            Assert.That(game.Current, Is.EqualTo(GameManager.Phase.Intro));
            float deadline = Time.realtimeSinceStartup + 5f;
            while (game.Current != GameManager.Phase.Playing && Time.realtimeSinceStartup < deadline) yield return null;

            Assert.That(game.Current, Is.EqualTo(GameManager.Phase.Playing));
            Assert.That(game.Level?.Player, Is.Not.Null);
            yield return new WaitForSeconds(0.5f);
            Assert.That(game.Level.Player.IsGrounded, Is.True, "Ottie spawns standing on solid ground");

            game.Pause();
            Assert.That(game.Current, Is.EqualTo(GameManager.Phase.Paused));
            Assert.That(Time.timeScale, Is.Zero);
            game.Resume();
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            game.GoToTitle();
            Assert.That(game.Current, Is.EqualTo(GameManager.Phase.Title));
        }

        /// <summary>Every shipped level builds without errors and spawns Ottie on the ground next to a goal.</summary>
        [UnityTest]
        public IEnumerator AllBundledLevels_BuildAndStartGrounded()
        {
            RuntimeSettings.Apply();
            var assets = GameAssets.Load();
            var camGo = new GameObject("TestCamera");
            camGo.AddComponent<Camera>();
            var cam = camGo.AddComponent<PlatformerCamera>();

            foreach (var text in assets.levels)
            {
                var data = LevelParser.Parse(text.text);
                var ctx = new LevelContext(data, assets, new GameSession(), new NullAudio(), cam.Camera);
                var level = LevelBuilder.Build(ctx, new ScriptedInput());
                cam.Follow(level.Player.transform, level.CameraBounds);
                Assert.That(level.Goal, Is.Not.Null, data.Name);
                yield return new WaitForSeconds(0.4f);
                Assert.That(level.Player.IsGrounded, Is.True, data.Name);
                Assert.That(level.Player.State, Is.EqualTo(Player.PlayerController.LifeState.Alive), data.Name);
                level.Destroy();
                yield return null;
            }
            Object.Destroy(camGo);
        }
    }
}
