using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SuperOttie.Core;
using SuperOttie.Game;
using SuperOttie.Input;
using SuperOttie.UI;
using SuperOttie.WordHunt;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace SuperOttie.Tests
{
    /// <summary>
    /// Visual QA helper (not part of the normal run): renders the main menu, course select, out-of-lives offer and
    /// a Word Hunt round at iPhone 17 Pro
    /// landscape resolution into Logs/Screenshots. Run with -testFilter MenuScreenshotCapture.
    /// </summary>
    [Explicit("Visual QA utility")]
    public class MenuScreenshotCapture
    {
        const string HighScoreKey = "superottie.highscore";

        [UnityTest]
        public IEnumerator CaptureMenus()
        {
            string key = PlayerPrefsCourseProgressStore.Key;
            int? saved = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null;
            PlayerPrefs.SetInt(key, 0b111); // three courses cleared, the fourth open
            int? savedBest = PlayerPrefs.HasKey(HighScoreKey) ? PlayerPrefs.GetInt(HighScoreKey) : (int?)null;
            PlayerPrefs.SetInt(HighScoreKey, 48250);
            Directory.CreateDirectory("Logs/Screenshots");

            SceneManager.LoadScene("Main");
            yield return null;
            var game = Object.FindFirstObjectByType<GameManager>();
            var panel = Object.FindFirstObjectByType<UIDocument>().panelSettings;
            var rt = new RenderTexture(2622, 1206, 24);
            panel.targetTexture = rt;
            var ui = (GameUI)typeof(GameManager).GetField("_ui", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(game);
            var menu = ui.Menu;

            yield return Capture(rt, "menu_title");
            menu.Handle(GameUI.Screen.Title, MenuCommand.Down);
            menu.Handle(GameUI.Screen.Title, MenuCommand.Down);
            yield return Capture(rt, "menu_title_focus");

            game.GoToTitle();
            game.OpenCourses();
            yield return Capture(rt, "menu_courses");
            menu.Handle(GameUI.Screen.Courses, MenuCommand.Right);
            menu.Handle(GameUI.Screen.Courses, MenuCommand.Right);
            yield return Capture(rt, "menu_courses_focus");

            // Out of lives, then a Word Hunt round with one word found and a second being traced.
            game.GoToTitle();
            game.StartNewGame();
            while (game.Current != GameManager.Phase.Playing) yield return null;
            while (game.Session.Lives > 1) game.Session.LoseLife();
            game.Session.AddScore(12450);
            game.Level.Player.Die();
            while (game.Current != GameManager.Phase.OutOfLives) yield return null;
            yield return Capture(rt, "outoflives");

            game.PlayWordHunt();
            var round = game.WordHuntRound;
            var words = GridSolver.FindAll(round.Grid, WordHuntController.LoadWords(GameAssets.Load()));
            round.SubmitTyped(words.First(w => w.Length == 4));
            round.Tick(23f);
            var tracing = words.Where(w => w.Length == 5).DefaultIfEmpty(words.Last()).First();
            foreach (int cell in round.Grid.FindPath(tracing)) round.Touch(cell);
            ui.WordHunt.ShowPath(round.Path, round.State, round.CurrentWord);
            yield return Capture(rt, "wordhunt");

            panel.targetTexture = null;
            rt.Release();
            if (saved.HasValue) PlayerPrefs.SetInt(key, saved.Value);
            else PlayerPrefs.DeleteKey(key);
            if (savedBest.HasValue) PlayerPrefs.SetInt(HighScoreKey, savedBest.Value);
            else PlayerPrefs.DeleteKey(HighScoreKey);
        }

        static IEnumerator Capture(RenderTexture rt, string name)
        {
            // Let layout, transitions and the idle animations settle.
            yield return new WaitForSecondsRealtime(0.4f);
            yield return null; // (WaitForEndOfFrame never fires in batch mode)
            yield return null;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = null;
            File.WriteAllBytes($"Logs/Screenshots/{name}.png", tex.EncodeToPNG());
            Object.Destroy(tex);
        }
    }
}
