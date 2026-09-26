using System.Collections.Generic;
using NUnit.Framework;
using SuperOttie.Input;
using SuperOttie.View;
using UnityEngine;

namespace SuperOttie.Tests
{
    public class TouchZonesTests
    {
        // Stick centred at (200, 200) with a 100 px radius; jump button far right.
        static TouchZones Zones() => new TouchZones
        {
            StickCenter = new Vector2(200, 200),
            StickRadius = 100,
            Jump = new Rect(900, 100, 120, 120),
        };

        static List<TouchPoint> Touches(params (int id, float x, float y)[] points)
        {
            var list = new List<TouchPoint>();
            foreach (var (id, x, y) in points) list.Add(new TouchPoint(id, new Vector2(x, y)));
            return list;
        }

        [Test]
        public void Unconfigured_ReportsNothing()
        {
            var r = new TouchZones().Evaluate(Touches((1, 200, 200)));
            Assert.That(r.StickHeld || r.Jump, Is.False);
            Assert.That(r.Move, Is.EqualTo(0f));
        }

        [Test]
        public void CentredStick_IsInsideDeadZone()
        {
            var r = Zones().Evaluate(Touches((1, 210, 205)));
            Assert.That(r.StickHeld, Is.True);
            Assert.That(r.Move, Is.EqualTo(0f));
        }

        [Test]
        public void Deflection_RampsToFullSpeed()
        {
            var z = Zones();
            Assert.That(z.Evaluate(Touches((1, 240, 200))).Move, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(z.Evaluate(Touches((1, 260, 200))).Move, Is.EqualTo(1f));
            Assert.That(z.Evaluate(Touches((1, 140, 200))).Move, Is.EqualTo(-1f));
        }

        [Test]
        public void SlidingFinger_ReversesWithoutLifting()
        {
            var z = Zones();
            Assert.That(z.Evaluate(Touches((7, 280, 200))).Move, Is.EqualTo(1f));
            Assert.That(z.Evaluate(Touches((7, 200, 210))).Move, Is.EqualTo(0f));
            Assert.That(z.Evaluate(Touches((7, 120, 190))).Move, Is.EqualTo(-1f));
        }

        [Test]
        public void OwnedFinger_KeepsStickWhenDraggedFarOutside()
        {
            var z = Zones();
            z.Evaluate(Touches((3, 200, 200)));
            var r = z.Evaluate(Touches((3, 600, 500)));
            Assert.That(r.StickHeld, Is.True);
            Assert.That(r.Move, Is.EqualTo(1f));
            Assert.That(r.Stick.magnitude, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void TouchStartingFarAway_DoesNotGrabStick()
        {
            var r = Zones().Evaluate(Touches((1, 600, 500)));
            Assert.That(r.StickHeld, Is.False);
            Assert.That(r.Move, Is.EqualTo(0f));
        }

        [Test]
        public void NearMiss_WithinGrabRadius_GrabsStick()
        {
            var r = Zones().Evaluate(Touches((1, 350, 200)));
            Assert.That(r.StickHeld, Is.True);
            Assert.That(r.Move, Is.EqualTo(1f));
        }

        [Test]
        public void MultiTouch_RunAndJumpTogether()
        {
            var r = Zones().Evaluate(Touches((1, 280, 200), (2, 950, 160)));
            Assert.That(r.Move, Is.EqualTo(1f));
            Assert.That(r.Jump, Is.True);
        }

        [Test]
        public void LiftingStickFinger_ReleasesIt()
        {
            var z = Zones();
            z.Evaluate(Touches((1, 280, 200)));
            var r = z.Evaluate(Touches((2, 950, 160)));
            Assert.That(r.StickHeld, Is.False);
            Assert.That(r.Move, Is.EqualTo(0f));
            Assert.That(r.Jump, Is.True);
        }

        [Test]
        public void StickFinger_NeverPressesJump()
        {
            var z = Zones();
            z.Evaluate(Touches((1, 200, 200)));
            var r = z.Evaluate(Touches((1, 950, 160)));
            Assert.That(r.StickHeld, Is.True);
            Assert.That(r.Jump, Is.False);
        }

        [Test]
        public void JumpNearMiss_WithinForgiveness_Counts()
        {
            var r = Zones().Evaluate(Touches((1, 895, 160)));
            Assert.That(r.Jump, Is.True);
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
