using System;
using UnityEngine;

namespace SuperOttie.Player
{
    [Serializable]
    public sealed class MotorSettings
    {
        [Header("Run")]
        public float maxRunSpeed = 7.5f;
        public float groundAcceleration = 38f;
        public float groundDeceleration = 42f;
        public float turnAcceleration = 70f;
        public float airAcceleration = 28f;
        public float airDeceleration = 10f;

        [Header("Jump")]
        [Tooltip("Apex height of a full (held) jump from standstill, in tiles.")]
        public float jumpHeight = 4.3f;
        [Tooltip("Seconds from take-off to apex for a full jump.")]
        public float timeToApex = 0.42f;
        [Tooltip("Extra gravity while falling, for a snappy arc.")]
        public float fallGravityMultiplier = 1.7f;
        [Tooltip("Extra gravity while rising after jump is released (variable jump height).")]
        public float jumpCutGravityMultiplier = 3f;
        public float maxFallSpeed = 22f;
        public float coyoteTime = 0.1f;
        public float jumpBufferTime = 0.12f;
        public float stompBounceSpeed = 12f;

        public float Gravity => 2f * jumpHeight / (timeToApex * timeToApex);
        public float JumpVelocity => 2f * jumpHeight / timeToApex;
    }

    public struct MotorStepResult
    {
        public bool Jumped;
    }

    /// <summary>
    /// Pure platformer movement model: acceleration curves, gravity, coyote time, jump buffering
    /// and variable jump height. It knows nothing about Unity physics; the controller feeds it the
    /// grounded state and applies the velocity it returns, which makes it fully unit-testable.
    /// </summary>
    public sealed class PlatformerMotor
    {
        readonly MotorSettings _s;
        float _coyote;
        float _buffer;

        public PlatformerMotor(MotorSettings settings) => _s = settings ?? throw new ArgumentNullException(nameof(settings));

        public MotorSettings Settings => _s;
        public Vector2 Velocity;

        /// <summary>True while rising from a jump that can still be cut short by releasing the button.</summary>
        public bool IsJumping { get; private set; }

        public void QueueJump() => _buffer = _s.jumpBufferTime;

        public MotorStepResult Step(float move, bool jumpHeld, bool grounded, float dt)
        {
            var result = new MotorStepResult();
            if (dt <= 0f) return result;

            _coyote = grounded ? _s.coyoteTime : _coyote - dt;
            _buffer -= dt;

            // Horizontal.
            float target = Mathf.Clamp(move, -1f, 1f) * _s.maxRunSpeed;
            float accel;
            if (Mathf.Abs(move) > 0.01f)
            {
                bool turning = Mathf.Abs(Velocity.x) > 0.01f && Mathf.Sign(target) != Mathf.Sign(Velocity.x);
                accel = turning ? _s.turnAcceleration : grounded ? _s.groundAcceleration : _s.airAcceleration;
            }
            else accel = grounded ? _s.groundDeceleration : _s.airDeceleration;
            Velocity.x = Mathf.MoveTowards(Velocity.x, target, accel * dt);

            // Jump (buffered press + coyote window).
            if (_buffer > 0f && _coyote > 0f)
            {
                Velocity.y = _s.JumpVelocity;
                _buffer = 0f;
                _coyote = 0f;
                IsJumping = true;
                result.Jumped = true;
            }

            // Vertical.
            if (grounded && !result.Jumped && Velocity.y <= 0f)
            {
                Velocity.y = 0f;
                IsJumping = false;
            }
            else
            {
                float g = _s.Gravity;
                if (Velocity.y < 0f) g *= _s.fallGravityMultiplier;
                else if (IsJumping && !jumpHeld) g *= _s.jumpCutGravityMultiplier;
                Velocity.y = Mathf.Max(Velocity.y - g * dt, -_s.maxFallSpeed);
                if (Velocity.y <= 0f) IsJumping = false;
            }
            return result;
        }

        /// <summary>Bounce off a stomped enemy; holding jump gives a full-height bounce.</summary>
        public void Bounce(bool jumpHeld)
        {
            Velocity.y = jumpHeld ? _s.JumpVelocity : _s.stompBounceSpeed;
            IsJumping = jumpHeld;
            _coyote = 0f;
        }

        /// <summary>Head hit a ceiling: stop rising immediately.</summary>
        public void HitCeiling()
        {
            if (Velocity.y > 0f) Velocity.y = 0f;
            IsJumping = false;
        }

        public void Reset()
        {
            Velocity = Vector2.zero;
            IsJumping = false;
            _coyote = 0f;
            _buffer = 0f;
        }
    }
}
