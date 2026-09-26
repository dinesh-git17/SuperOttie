using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SuperOttie.Audio;
using SuperOttie.Core;
using SuperOttie.Entities;
using SuperOttie.Game;
using SuperOttie.Input;
using SuperOttie.Level;
using SuperOttie.Player;
using SuperOttie.View;
using UnityEngine;
using UnityEngine.TestTools;

namespace SuperOttie.Tests
{
    /// <summary>
    /// A simple autopilot runs right and jumps at walls and pits. With enemies removed it proves every
    /// shipped level's floor route is completable with the real movement physics (not just the lint).
    /// </summary>
    public class LevelPlaythroughTests
    {
        sealed class AutoPilot : IPlayerInput
        {
            readonly EdgeDetector _edges = new EdgeDetector();
            readonly int[] _surface;
            public PlayerController Player;
            bool _holding;
            float _held;
            float _holdFor;
            float _sinceGrounded;

            const float CoyoteGrace = 0.07f;

            public AutoPilot(int[] surface) => _surface = surface;

            public InputFrame Poll()
            {
                if (Player == null) return default;
                var p = Player.transform.position;
                _sinceGrounded = Player.IsGrounded ? 0f : _sinceGrounded + Time.deltaTime;
                // Like a player, it may still jump a moment after running off a ledge (the motor's coyote time).
                bool canJump = Player.IsGrounded || (_sinceGrounded < CoyoteGrace && Player.Velocity.y <= 0f);
                if (_holding)
                {
                    _held += Time.deltaTime;
                    // Hold for a full-height jump; let go once falling so the next jump is a fresh press.
                    // Like a player: short hops for small steps, full jumps for pits and tall walls.
                    if (_held >= _holdFor || (_held > 0.12f && Player.Velocity.y <= 0f)) _holding = false;
                }
                else if (canJump && ObstacleAhead(p, out int rise))
                {
                    _holding = true;
                    _held = 0f;
                    _holdFor = rise <= 1 ? 0.07f : rise == 2 ? 0.15f : rise == 3 ? 0.24f : 1f;
                }
                return _edges.Build(1f, _holding);
            }

            /// <param name="rise">How many cells higher the obstacle is (99 = needs a full jump).</param>
            bool ObstacleAhead(Vector3 p, out int rise)
            {
                rise = 0;
                float front = p.x + PlayerController.ColliderSize.x * 0.5f;
                int feet = Mathf.RoundToInt(p.y);
                int first = Mathf.FloorToInt(front), near = Mathf.FloorToInt(front + 1.1f), far = Mathf.FloorToInt(front + 2f);
                bool pitSoon = false;
                for (int c = first; c <= far && c < _surface.Length; c++)
                    if (c >= 0 && _surface[c] == 0) pitSoon = true;

                for (int c = first; c <= near && c < _surface.Length; c++)
                {
                    if (c < 0) continue;
                    if (_surface[c] == 0 || _surface[c] > feet)
                    {
                        // A step right before a pit: one full jump over both, like a player would.
                        rise = _surface[c] == 0 || pitSoon ? 99 : _surface[c] - feet;
                        return true;
                    }
                }
                return false;
            }
        }

        static IEnumerable<int> LevelNumbers() => Enumerable.Range(1, GameAssets.Load().levels.Length);

        [UnityTest, Timeout(120000)]
        public IEnumerator Autopilot_FinishesLevel([ValueSource(nameof(LevelNumbers))] int levelNumber)
        {
            var levelText = GameAssets.Load().levels[levelNumber - 1];
            RuntimeSettings.Apply();
            Time.timeScale = 2f;
            var camGo = new GameObject("TestCamera");
            camGo.AddComponent<Camera>();
            var cam = camGo.AddComponent<PlatformerCamera>();
            var data = LevelParser.Parse(levelText.text);
            var pilot = new AutoPilot(LevelLint.FloorSurface(data));
            var ctx = new LevelContext(data, GameAssets.Load(), new GameSession(), new NullAudio(), cam.Camera);
            bool finished = false, died = false;
            ctx.PlayerFinishedGoal += () => finished = true;
            ctx.PlayerDied += () => died = true;
            var level = LevelBuilder.Build(ctx, pilot, withBackground: false);
            pilot.Player = level.Player;
            cam.Follow(level.Player.transform, level.CameraBounds);
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) Object.Destroy(enemy.gameObject);

            float stuckTimer = 0f, lastX = 0f;
            try
            {
                for (float t = 0f; t < 90f && !finished && !died; t += Time.deltaTime)
                {
                    float x = level.Player.transform.position.x;
                    stuckTimer = x > lastX + 0.01f ? 0f : stuckTimer + Time.deltaTime;
                    lastX = Mathf.Max(lastX, x);
                    Assert.That(stuckTimer, Is.LessThan(6f), $"{data.Name}: autopilot stuck at x={x:F1}");
                    yield return null;
                }
                Assert.That(died, Is.False, $"{data.Name}: autopilot died near x={level.Player.transform.position.x:F1}");
                Assert.That(finished, Is.True, $"{data.Name}: did not reach the flag (x={level.Player.transform.position.x:F1})");
            }
            finally
            {
                Time.timeScale = 1f;
                level.Destroy();
                Object.Destroy(camGo);
            }
        }
    }
}
