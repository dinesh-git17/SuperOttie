using System.Collections.Generic;
using UnityEngine;

namespace SuperOttie.Input
{
    public struct TouchButtons
    {
        public bool Left;
        public bool Right;
        public bool Jump;

        public float Move => (Right ? 1f : 0f) - (Left ? 1f : 0f);
    }

    /// <summary>
    /// Screen-space hit areas of the on-screen buttons (origin bottom-left, pixels).
    /// The UI updates these after layout; input polls them each frame. Fingers may slide
    /// between buttons, so every active touch is re-tested every frame.
    /// </summary>
    public sealed class TouchZones
    {
        public Rect Left;
        public Rect Right;
        public Rect Jump;

        /// <summary>Fraction the hit areas are grown by, so near-misses still count.</summary>
        public float Forgiveness = 0.2f;

        public bool IsConfigured => Left.width > 0f && Right.width > 0f && Jump.width > 0f;

        public TouchButtons Evaluate(IReadOnlyList<Vector2> touches)
        {
            var result = new TouchButtons();
            if (!IsConfigured) return result;
            var left = Grow(Left);
            var right = Grow(Right);
            var jump = Grow(Jump);
            for (int i = 0; i < touches.Count; i++)
            {
                var p = touches[i];
                if (jump.Contains(p)) result.Jump = true;
                bool inLeft = left.Contains(p), inRight = right.Contains(p);
                if (inLeft && inRight)
                {
                    // Grown rects can overlap between the arrows: pick the nearer centre.
                    if ((p - Left.center).sqrMagnitude <= (p - Right.center).sqrMagnitude) result.Left = true;
                    else result.Right = true;
                }
                else if (inLeft) result.Left = true;
                else if (inRight) result.Right = true;
            }
            return result;
        }

        Rect Grow(Rect r)
        {
            float gx = r.width * Forgiveness * 0.5f, gy = r.height * Forgiveness * 0.5f;
            return new Rect(r.x - gx, r.y - gy, r.width + gx * 2f, r.height + gy * 2f);
        }
    }
}
