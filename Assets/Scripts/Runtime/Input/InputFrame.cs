namespace SuperOttie.Input
{
    /// <summary>One frame of player intent, independent of the device it came from.</summary>
    public struct InputFrame
    {
        /// <summary>-1 (left) .. 1 (right).</summary>
        public float Move;
        public bool JumpHeld;

        /// <summary>True only on the frame the jump button went down.</summary>
        public bool JumpPressed;
    }

    /// <summary>One menu action from a keyboard or gamepad (touch menus are tapped directly).</summary>
    public enum MenuCommand
    {
        None,
        Up,
        Down,
        Left,
        Right,
        Submit,
        Back,
    }

    public interface IPlayerInput
    {
        /// <summary>Called once per rendered frame by the player.</summary>
        InputFrame Poll();
    }

    /// <summary>Turns "held" states into an <see cref="InputFrame"/> with edge detection.</summary>
    public sealed class EdgeDetector
    {
        bool _wasJumpHeld;

        public InputFrame Build(float move, bool jumpHeld)
        {
            var frame = new InputFrame
            {
                Move = move < -1f ? -1f : move > 1f ? 1f : move,
                JumpHeld = jumpHeld,
                JumpPressed = jumpHeld && !_wasJumpHeld,
            };
            _wasJumpHeld = jumpHeld;
            return frame;
        }

        public void Reset() => _wasJumpHeld = false;
    }
}
