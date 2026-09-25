using NUnit.Framework;
using SuperOttie.Player;
using UnityEngine;

namespace SuperOttie.Tests
{
    /// <summary>
    /// Simulates the motor over a flat floor at y = 0 so jump physics can be checked without Unity physics.
    /// </summary>
    public class PlatformerMotorTests
    {
        const float Dt = 1f / 60f;

        MotorSettings _settings;
        PlatformerMotor _motor;
        float _y;

        [SetUp]
        public void SetUp()
        {
            _settings = new MotorSettings();
            _motor = new PlatformerMotor(_settings);
            _y = 0f;
        }

        /// <summary>Advances one step; the floor clamps y at 0.</summary>
        void Step(float move, bool jumpHeld, bool? groundedOverride = null)
        {
            bool grounded = groundedOverride ?? (_y <= 0f && _motor.Velocity.y <= 0f);
            _motor.Step(move, jumpHeld, grounded, Dt);
            _y += _motor.Velocity.y * Dt;
            if (_y < 0f)
            {
                _y = 0f;
                _motor.Velocity.y = 0f;
            }
        }

        float JumpAndMeasureApex(float holdSeconds)
        {
            _motor.QueueJump();
            float apex = 0f;
            for (int i = 0; i < 180; i++)
            {
                Step(0f, i * Dt < holdSeconds);
                apex = Mathf.Max(apex, _y);
            }
            return apex;
        }

        [Test]
        public void DerivedJumpVelocityAndGravity_MatchKinematics()
        {
            float v = _settings.JumpVelocity, g = _settings.Gravity;
            Assert.That(v * v / (2f * g), Is.EqualTo(_settings.jumpHeight).Within(1e-4));
            Assert.That(v / g, Is.EqualTo(_settings.timeToApex).Within(1e-4));
        }

        [Test]
        public void Run_AcceleratesToMaxSpeedAndNoFurther()
        {
            for (int i = 0; i < 120; i++) Step(1f, false);
            Assert.That(_motor.Velocity.x, Is.EqualTo(_settings.maxRunSpeed).Within(1e-4));
        }

        [Test]
        public void ReleasingStick_DeceleratesToStop()
        {
            for (int i = 0; i < 60; i++) Step(1f, false);
            for (int i = 0; i < 60; i++) Step(0f, false);
            Assert.That(_motor.Velocity.x, Is.EqualTo(0f).Within(1e-4));
        }

        [Test]
        public void FullJump_ReachesConfiguredHeight()
        {
            float apex = JumpAndMeasureApex(holdSeconds: 10f);
            Assert.That(apex, Is.EqualTo(_settings.jumpHeight).Within(0.25f));
        }

        [Test]
        public void TapJump_IsMuchLowerThanFullJump()
        {
            float tap = JumpAndMeasureApex(holdSeconds: 0.05f);
            SetUp();
            float full = JumpAndMeasureApex(holdSeconds: 10f);
            Assert.That(tap, Is.LessThan(full * 0.6f));
            Assert.That(tap, Is.GreaterThan(0.5f));
        }

        [Test]
        public void Jump_LandsBackOnFloor()
        {
            JumpAndMeasureApex(10f);
            Assert.That(_y, Is.EqualTo(0f));
            Assert.That(_motor.IsJumping, Is.False);
        }

        [Test]
        public void CoyoteTime_AllowsJumpJustAfterLeavingLedge()
        {
            Step(0f, false, groundedOverride: true);
            // Walked off the ledge: two airborne frames (well within coyote time).
            _motor.Step(0f, false, false, Dt);
            _motor.Step(0f, false, false, Dt);
            _motor.QueueJump();
            var result = _motor.Step(0f, true, false, Dt);
            Assert.That(result.Jumped, Is.True);
        }

        [Test]
        public void CoyoteTime_Expires()
        {
            Step(0f, false, groundedOverride: true);
            for (int i = 0; i < 20; i++) _motor.Step(0f, false, false, Dt); // 0.33 s airborne
            _motor.QueueJump();
            Assert.That(_motor.Step(0f, true, false, Dt).Jumped, Is.False);
        }

        [Test]
        public void JumpBuffer_JumpsOnLandingIfPressedJustBefore()
        {
            _motor.Velocity = new Vector2(0f, -5f);
            _motor.Step(0f, false, false, Dt); // falling, coyote long gone
            for (int i = 0; i < 10; i++) _motor.Step(0f, false, false, Dt);
            _motor.QueueJump();
            _motor.Step(0f, true, false, Dt); // still airborne: buffered
            var landed = _motor.Step(0f, true, true, Dt);
            Assert.That(landed.Jumped, Is.True);
        }

        [Test]
        public void JumpBuffer_Expires()
        {
            _motor.Velocity = new Vector2(0f, -5f);
            for (int i = 0; i < 10; i++) _motor.Step(0f, false, false, Dt);
            _motor.QueueJump();
            for (int i = 0; i < 20; i++) _motor.Step(0f, true, false, Dt); // 0.33 s: buffer expired
            Assert.That(_motor.Step(0f, true, true, Dt).Jumped, Is.False);
        }

        [Test]
        public void Falling_IsCappedAtTerminalVelocity()
        {
            for (int i = 0; i < 600; i++) _motor.Step(0f, false, false, Dt);
            Assert.That(_motor.Velocity.y, Is.EqualTo(-_settings.maxFallSpeed).Within(1e-4));
        }

        [Test]
        public void Bounce_HeldJumpBouncesHigher()
        {
            _motor.Bounce(false);
            float low = _motor.Velocity.y;
            _motor.Bounce(true);
            Assert.That(_motor.Velocity.y, Is.GreaterThan(low));
        }

        [Test]
        public void HitCeiling_StopsRising()
        {
            _motor.QueueJump();
            _motor.Step(0f, true, true, Dt);
            Assert.That(_motor.Velocity.y, Is.GreaterThan(0f));
            _motor.HitCeiling();
            Assert.That(_motor.Velocity.y, Is.EqualTo(0f));
            Assert.That(_motor.IsJumping, Is.False);
        }
    }
}
