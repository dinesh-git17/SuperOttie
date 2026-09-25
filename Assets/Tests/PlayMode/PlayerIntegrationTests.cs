using System.Collections;
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
    /// <summary>Scripted "thumbs" for driving Ottie in tests.</summary>
    public sealed class ScriptedInput : IPlayerInput
    {
        readonly EdgeDetector _edges = new EdgeDetector();
        public float Move;
        public bool Jump;

        public InputFrame Poll() => _edges.Build(Move, Jump);
    }

    /// <summary>
    /// End-to-end gameplay checks: real physics, real level builder, real assets, scripted input.
    /// Maps are tiny levels in the shipping format; ground is rows 0-1 so the floor is y = 2.
    /// </summary>
    public class PlayerIntegrationTests
    {
        const float Floor = 2f;

        GameObject _camGo;
        PlatformerCamera _camera;
        GameSession _session;
        ScriptedInput _input;
        LevelContext _ctx;
        BuiltLevel _level;
        bool _died;

        PlayerController Player => _level.Player;

        [SetUp]
        public void SetUp()
        {
            RuntimeSettings.Apply();
            Time.timeScale = 1f;
            _camGo = new GameObject("TestCamera");
            _camGo.AddComponent<Camera>();
            _camera = _camGo.AddComponent<PlatformerCamera>();
            _session = new GameSession();
            _input = new ScriptedInput();
            _died = false;
        }

        [TearDown]
        public void TearDown()
        {
            _level?.Destroy();
            Object.Destroy(_camGo);
            Time.timeScale = 1f;
        }

        IEnumerator Load(params string[] rows)
        {
            var data = LevelParser.Parse("---\n" + string.Join("\n", rows) + "\n");
            _ctx = new LevelContext(data, GameAssets.Load(), _session, new NullAudio(), _camera.Camera);
            _ctx.PlayerDied += () => _died = true;
            _level = LevelBuilder.Build(_ctx, _input, withBackground: false);
            _camera.Follow(_level.Player.transform, _level.CameraBounds);
            yield return new WaitForFixedUpdate();
        }

        static IEnumerator Seconds(float s) => new WaitForSecondsTest(s);

        sealed class WaitForSecondsTest : CustomYieldInstruction
        {
            readonly float _until;
            public WaitForSecondsTest(float s) => _until = Time.time + s;
            public override bool keepWaiting => Time.time < _until;
        }

        IEnumerator JumpFor(float seconds)
        {
            _input.Jump = true;
            yield return Seconds(seconds);
            _input.Jump = false;
        }

        [UnityTest]
        public IEnumerator Player_LandsOnGround()
        {
            yield return Load(
                "..........",
                "..P......F",
                "##########",
                "##########");
            yield return Seconds(0.5f);
            Assert.That(Player.IsGrounded, Is.True);
            Assert.That(Player.transform.position.y, Is.EqualTo(Floor).Within(0.06f));
        }

        [UnityTest]
        public IEnumerator Player_RunsRightAtFullSpeed()
        {
            yield return Load(
                "P...............................F",
                "#################################",
                "#################################");
            yield return Seconds(0.3f);
            float x0 = Player.transform.position.x;
            _input.Move = 1f;
            yield return Seconds(1f);
            float travelled = Player.transform.position.x - x0;
            Assert.That(travelled, Is.GreaterThan(6f));
            Assert.That(Player.Velocity.x, Is.EqualTo(Player.Motor.Settings.maxRunSpeed).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator Player_WallStopsRunning()
        {
            yield return Load(
                "....S.....",
                "....S.....",
                "P...S....F",
                "##########",
                "##########");
            _input.Move = 1f;
            yield return Seconds(1f);
            Assert.That(Player.transform.position.x, Is.LessThan(4f));
            Assert.That(Player.transform.position.x, Is.GreaterThan(3.5f));
        }

        [UnityTest]
        public IEnumerator Player_FullJumpClearsFourTiles()
        {
            yield return Load(
                "..........",
                "..........",
                "..........",
                "..........",
                "..........",
                "..........",
                "..P......F",
                "##########",
                "##########");
            yield return Seconds(0.3f);
            float maxY = 0f;
            _input.Jump = true;
            for (float t = 0f; t < 1.2f; t += Time.deltaTime)
            {
                maxY = Mathf.Max(maxY, Player.transform.position.y);
                yield return null;
            }
            _input.Jump = false;
            Assert.That(maxY - Floor, Is.GreaterThan(3.9f));
            Assert.That(maxY - Floor, Is.LessThan(4.8f));
        }

        [UnityTest]
        public IEnumerator QuestionBlock_GivesCoinFromBelow()
        {
            yield return Load(
                "..........",
                "..?.......",
                "..........",
                "..........",
                "..P......F",
                "##########",
                "##########");
            yield return Seconds(0.3f);
            yield return JumpFor(0.5f);
            yield return Seconds(0.5f);
            Assert.That(_session.Coins, Is.EqualTo(1));
            Assert.That(Object.FindFirstObjectByType<QuestionBlock>().IsUsed, Is.True);

            // A used block gives nothing more.
            yield return JumpFor(0.5f);
            yield return Seconds(0.6f);
            Assert.That(_session.Coins, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SmallPlayer_BumpsBrickWithoutBreaking()
        {
            yield return Load(
                "..........",
                "..B.......",
                "..........",
                "..........",
                "..P......F",
                "##########",
                "##########");
            yield return Seconds(0.3f);
            yield return JumpFor(0.4f);
            yield return Seconds(0.6f);
            Assert.That(Object.FindFirstObjectByType<BrickBlock>(), Is.Not.Null);
            Assert.That(Player.IsGrounded, Is.True);
        }

        [UnityTest]
        public IEnumerator PowerUp_EmergesAndGrowsOttie_ThenBigOttieBreaksBricks()
        {
            yield return Load(
                "....................",
                "..M......B..........",
                "....................",
                "....................",
                "..P................F",
                "####################",
                "####################");
            yield return Seconds(0.3f);
            yield return JumpFor(0.3f);
            yield return Seconds(0.3f);
            Assert.That(Object.FindFirstObjectByType<PowerUpFish>(), Is.Not.Null, "fish spawned");

            // Chase the fish to the right.
            yield return Seconds(0.8f);
            _input.Move = 1f;
            for (float t = 0f; t < 3f && !Player.IsBig; t += Time.deltaTime) yield return null;
            _input.Move = 0f;
            Assert.That(Player.IsBig, Is.True);
            Assert.That(Player.transform.localScale.x, Is.EqualTo(PlayerController.BigScale).Within(1e-3f));

            // Walk back under the brick at x=9 and break it.
            yield return Seconds(0.4f);
            float dir = Mathf.Sign(9.5f - Player.transform.position.x);
            _input.Move = dir * 0.35f; // walk slowly so it stops right under the brick
            while (Mathf.Abs(Player.transform.position.x - 9.5f) > 0.15f) yield return null;
            _input.Move = 0f;
            yield return Seconds(0.4f);
            yield return JumpFor(0.4f);
            yield return Seconds(0.6f);
            Assert.That(Object.FindFirstObjectByType<BrickBlock>(), Is.Null, "brick broken");
            Assert.That(_session.Score, Is.GreaterThanOrEqualTo(1000 + BrickBlock.BreakScore));
        }

        [UnityTest]
        public IEnumerator FallingOnCrab_StompsItAndBounces()
        {
            yield return Load(
                "..P.......",
                "..........",
                "..........",
                "..........",
                "..........",
                "...e.....F",
                "##########",
                "##########");
            bool bounced = false;
            for (float t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                if (Player.Velocity.y > 5f) bounced = true;
                yield return null;
            }
            Assert.That(bounced, Is.True);
            Assert.That(_died, Is.False);
            Assert.That(_session.Score, Is.EqualTo(100));
            Assert.That(Object.FindFirstObjectByType<CrabEnemy>(), Is.Null, "stomped crab is removed");
        }

        [UnityTest]
        public IEnumerator CrabTouchFromSide_DefeatsSmallOttie()
        {
            yield return Load(
                "..............",
                "..P....e.....F",
                "##############",
                "##############");
            for (float t = 0f; t < 5f && !_died; t += Time.deltaTime) yield return null;
            Assert.That(_died, Is.True);
            Assert.That(Player.State, Is.EqualTo(PlayerController.LifeState.Dying));
        }

        [UnityTest]
        public IEnumerator CrabTouch_ShrinksBigOttieInsteadOfDefeating()
        {
            yield return Load(
                "..............",
                "..P....e.....F",
                "##############",
                "##############");
            Player.CollectPowerUp();
            for (float t = 0f; t < 5f && Player.IsBig; t += Time.deltaTime) yield return null;
            Assert.That(Player.IsBig, Is.False);
            Assert.That(Player.IsInvincible, Is.True);
            Assert.That(_died, Is.False);
            yield return Seconds(0.5f);
            Assert.That(_died, Is.False, "invincibility frames protect from the same crab");
        }

        [UnityTest]
        public IEnumerator Pufferfish_CannotBeStomped()
        {
            yield return Load(
                "..P.......",
                "..........",
                "..........",
                "..........",
                "..f.......",
                "..........",
                ".........F",
                "##########",
                "##########");
            for (float t = 0f; t < 3f && !_died; t += Time.deltaTime) yield return null;
            Assert.That(_died, Is.True);
        }

        [UnityTest]
        public IEnumerator FallingIntoPit_Defeats()
        {
            yield return Load(
                "P.........",
                "#.....####",
                "#.....##F#",
                "#.....####");
            _input.Move = 1f;
            for (float t = 0f; t < 3f && !_died; t += Time.deltaTime) yield return null;
            Assert.That(_died, Is.True);
        }

        [UnityTest]
        public IEnumerator Coins_AreCollectedByTouch()
        {
            yield return Load(
                "...............",
                "P..ccc........F",
                "###############",
                "###############");
            _input.Move = 1f;
            yield return Seconds(1.2f);
            Assert.That(_session.Coins, Is.EqualTo(3));
            Assert.That(Object.FindFirstObjectByType<Coin>(), Is.Null);
        }

        [UnityTest]
        public IEnumerator ReachingFlag_CompletesLevel()
        {
            yield return Load(
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "..P.......F...",
                "##############",
                "##############");
            int flagScore = -1;
            bool finished = false;
            _ctx.GoalReached += s => flagScore = s;
            _ctx.PlayerFinishedGoal += () => finished = true;
            _input.Move = 1f;
            for (float t = 0f; t < 6f && !finished; t += Time.deltaTime) yield return null;
            Assert.That(_level.Goal.IsReached, Is.True);
            Assert.That(flagScore, Is.GreaterThan(0));
            Assert.That(finished, Is.True);
            Assert.That(Player.State, Is.EqualTo(PlayerController.LifeState.Goal));
            Assert.That(_session.Score, Is.EqualTo(flagScore));
        }
    }
}
