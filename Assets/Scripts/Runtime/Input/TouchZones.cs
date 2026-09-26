using System.Collections.Generic;
using UnityEngine;

namespace SuperOttie.Input
{
    /// <summary>One active finger in screen pixels (origin bottom-left). Id is stable for the touch's lifetime.</summary>
    public readonly struct TouchPoint
    {
        public readonly int Id;
        public readonly Vector2 Position;

        public TouchPoint(int id, Vector2 position)
        {
            Id = id;
            Position = position;
        }
    }

    public struct TouchButtons
    {
        /// <summary>-1 (left) .. 1 (right), from the thumbstick's horizontal deflection.</summary>
        public float Move;

        /// <summary>Knob offset from the stick centre in stick radii (length at most 1), for drawing.</summary>
        public Vector2 Stick;

        public bool StickHeld;
        public bool Jump;
    }

    /// <summary>
    /// Screen-space layout of the on-screen controls (a thumbstick and a jump button) and the stick's
    /// finger tracking. A finger that lands on the stick owns it until lifted, so it can slide left
    /// and right freely, even past the ring, without the thumb leaving the glass.
    /// The UI updates the layout after each layout pass; input evaluates it once per frame.
    /// </summary>
    public sealed class TouchZones
    {
        public Vector2 StickCenter;
        public float StickRadius;
        public Rect Jump;

        /// <summary>A touch starting within this many stick radii of the centre grabs the stick.</summary>
        public float StickGrabRadius = 1.6f;

        /// <summary>Deflection (in radii) below which the stick reads as centred.</summary>
        public float DeadZone = 0.2f;

        /// <summary>Deflection (in radii) at which the stick reaches full speed.</summary>
        public float FullDeflection = 0.6f;

        /// <summary>Fraction the jump hit area is grown by, so near-misses still count.</summary>
        public float Forgiveness = 0.2f;

        const int NoFinger = int.MinValue;
        int _stickFinger = NoFinger;

        public bool IsConfigured => StickRadius > 0f && Jump.width > 0f;

        public TouchButtons Evaluate(IReadOnlyList<TouchPoint> touches)
        {
            var result = new TouchButtons();
            if (!IsConfigured)
            {
                _stickFinger = NoFinger;
                return result;
            }

            int owner = FindStickFinger(touches);
            _stickFinger = owner >= 0 ? touches[owner].Id : NoFinger;
            if (owner >= 0)
            {
                var offset = Vector2.ClampMagnitude((touches[owner].Position - StickCenter) / StickRadius, 1f);
                result.StickHeld = true;
                result.Stick = offset;
                result.Move = AxisValue(offset.x);
            }

            var jump = Grow(Jump);
            for (int i = 0; i < touches.Count; i++)
                if (i != owner && jump.Contains(touches[i].Position)) result.Jump = true;
            return result;
        }

        /// <summary>Forgets the finger that owns the stick (e.g. when the controls are hidden).</summary>
        public void Release() => _stickFinger = NoFinger;

        /// <summary>Maps horizontal deflection to run input: dead zone, then a linear ramp to full speed.</summary>
        public float AxisValue(float x)
        {
            float magnitude = Mathf.InverseLerp(DeadZone, FullDeflection, Mathf.Abs(x));
            return Mathf.Sign(x) * magnitude;
        }

        int FindStickFinger(IReadOnlyList<TouchPoint> touches)
        {
            for (int i = 0; i < touches.Count; i++)
                if (touches[i].Id == _stickFinger) return i;

            float grab = StickRadius * StickGrabRadius;
            int best = -1;
            float bestDistance = grab * grab;
            for (int i = 0; i < touches.Count; i++)
            {
                float d = (touches[i].Position - StickCenter).sqrMagnitude;
                if (d <= bestDistance)
                {
                    best = i;
                    bestDistance = d;
                }
            }
            return best;
        }

        Rect Grow(Rect r)
        {
            float gx = r.width * Forgiveness * 0.5f, gy = r.height * Forgiveness * 0.5f;
            return new Rect(r.x - gx, r.y - gy, r.width + gx * 2f, r.height + gy * 2f);
        }
    }
}
