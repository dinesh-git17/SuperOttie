using System.Collections.Generic;
using NUnit.Framework;
using SuperOttie.Input;
using SuperOttie.View;
using UnityEngine;

namespace SuperOttie.Tests
{
    public class TouchZonesTests
    {
        static TouchZones Zones() => new TouchZones
        {
            Left = new Rect(0, 0, 100, 100),
            Right = new Rect(120, 0, 100, 100),
            Jump = new Rect(900, 0, 120, 120),
            Forgiveness = 0.2f,
        };

        [Test]
        public void Unconfigured_ReportsNothing()
        {
            var r = new TouchZones().Evaluate(new List<Vector2> { new Vector2(10, 10) });
            Assert.That(r.Left || r.Right || r.Jump, Is.False);
        }

        [Test]
        public void MultiTouch_RunAndJumpTogether()
        {
            var r = Zones().Evaluate(new List<Vector2> { new Vector2(170, 50), new Vector2(950, 60) });
            Assert.That(r.Right, Is.True);
            Assert.That(r.Jump, Is.True);
            Assert.That(r.Move, Is.EqualTo(1f));
        }

        [Test]
        public void GapBetweenArrows_PicksNearestArrow()
        {
            var r = Zones().Evaluate(new List<Vector2> { new Vector2(108, 50) });
            Assert.That(r.Left, Is.True);
            Assert.That(r.Right, Is.False);
            r = Zones().Evaluate(new List<Vector2> { new Vector2(113, 50) });
            Assert.That(r.Right, Is.True);
        }

        [Test]
        public void NearMiss_WithinForgiveness_Counts()
        {
            var r = Zones().Evaluate(new List<Vector2> { new Vector2(895, 60) });
            Assert.That(r.Jump, Is.True);
        }

        [Test]
        public void FarAway_Ignored()
        {
            var r = Zones().Evaluate(new List<Vector2> { new Vector2(500, 500) });
            Assert.That(r.Left || r.Right || r.Jump, Is.False);
        }
    }

    public class EdgeDetectorTests
    {
        [Test]
        public void JumpPressed_OnlyOnFirstHeldFrame()
        {
            var e = new EdgeDetector();
            Assert.That(e.Build(0, true).JumpPressed, Is.True);
            Assert.That(e.Build(0, true).JumpPressed, Is.False);
            Assert.That(e.Build(0, false).JumpPressed, Is.False);
            Assert.That(e.Build(0, true).JumpPressed, Is.True);
        }

        [Test]
        public void Move_IsClamped() => Assert.That(new EdgeDetector().Build(3f, false).Move, Is.EqualTo(1f));
    }

    public class CameraMathTests
    {
        [Test]
        public void ClampKeepsViewInsideLevel()
        {
            var bounds = new Rect(0, 0, 200, 16);
            var p = PlatformerCamera.ClampToBounds(new Vector2(-5, -5), bounds, 6f, 2f);
            Assert.That(p, Is.EqualTo(new Vector2(12f, 6f)));
            p = PlatformerCamera.ClampToBounds(new Vector2(500, 500), bounds, 6f, 2f);
            Assert.That(p, Is.EqualTo(new Vector2(188f, 10f)));
        }

        [Test]
        public void NarrowLevel_IsCentered()
        {
            var p = PlatformerCamera.ClampToBounds(new Vector2(3, 6), new Rect(0, 0, 10, 16), 6f, 2f);
            Assert.That(p.x, Is.EqualTo(5f));
        }

        [Test]
        public void ParallaxWrap_StaysWithinHalfPeriodOfCamera()
        {
            const float period = 20f;
            for (float camX = 0f; camX < 500f; camX += 3.7f)
            {
                float x = ParallaxBackground.WrappedX(camX, 0.8f, period);
                Assert.That(Mathf.Abs(x - camX), Is.LessThanOrEqualTo(period * 0.5f + 1e-3f));
            }
        }
    }
}
