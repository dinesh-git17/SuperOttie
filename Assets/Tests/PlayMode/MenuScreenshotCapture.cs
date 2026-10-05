using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SuperOttie.Core;
using SuperOttie.Game;
using SuperOttie.Input;
using SuperOttie.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace SuperOttie.Tests
{
    /// <summary>
    /// Visual QA helper (not part of the normal run): renders the main menu and course select at iPhone 17 Pro
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
            var menu = ((GameUI)typeof(GameManager).GetField("_ui", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(game)).Menu;

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
