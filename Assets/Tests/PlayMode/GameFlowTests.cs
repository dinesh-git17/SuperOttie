using System.Collections;
using NUnit.Framework;
using SuperOttie.Audio;
using SuperOttie.Core;
using SuperOttie.Game;
using SuperOttie.Level;
using SuperOttie.View;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SuperOttie.Tests
{
    public class GameFlowTests
    {
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
