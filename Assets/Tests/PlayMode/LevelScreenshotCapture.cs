using System.Collections;
using System.IO;
using NUnit.Framework;
using SuperOttie.Audio;
using SuperOttie.Core;
using SuperOttie.Game;
using SuperOttie.Level;
using SuperOttie.View;
using UnityEngine;
using UnityEngine.TestTools;

namespace SuperOttie.Tests
{
    /// <summary>
    /// Visual QA helper (not part of the normal run): renders a frame from each course at iPhone 17 Pro
    /// landscape resolution into Logs/Screenshots. Run with -testFilter LevelScreenshotCapture.
    /// </summary>
    [Explicit("Visual QA utility")]
    public class LevelScreenshotCapture
    {
        [UnityTest]
        public IEnumerator CaptureEachCourse()
        {
            RuntimeSettings.Apply();
            Directory.CreateDirectory("Logs/Screenshots");
            var assets = GameAssets.Load();
            var camGo = new GameObject("Camera");
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            var cam = camGo.AddComponent<Camera>();
            var rig = camGo.AddComponent<PlatformerCamera>();
            var rt = new RenderTexture(2622, 1206, 24);
            cam.targetTexture = rt;

            for (int i = 0; i < assets.levels.Length; i++)
            {
                var data = LevelParser.Parse(assets.levels[i].text);
                var ctx = new LevelContext(data, assets, new GameSession(), new NullAudio(), cam);
                var level = LevelBuilder.Build(ctx, new ScriptedInput());
                // Frame an interesting stretch a little way into the course.
                level.Player.transform.position = new Vector3(Mathf.Min(40f, data.Width - 20f), 2f);
                rig.Follow(level.Player.transform, level.CameraBounds);
                yield return new WaitForSeconds(0.12f);
                cam.Render();
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                RenderTexture.active = null;
                File.WriteAllBytes($"Logs/Screenshots/level{i + 1}.png", tex.EncodeToPNG());
                Object.Destroy(tex);
                level.Destroy();
                yield return null;
            }
            cam.targetTexture = null;
            rt.Release();
            Object.Destroy(camGo);
        }
    }
}
