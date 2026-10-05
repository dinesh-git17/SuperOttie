using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace SuperOttie.Input
{
    /// <summary>
    /// Merges the touch stick and jump button, keyboard and gamepad into one <see cref="IPlayerInput"/>, and exposes
    /// menu-level signals (taps, pause, confirm, menu navigation).
    /// </summary>
    public sealed class DeviceInput : IPlayerInput
    {
        readonly TouchZones _zones;
        readonly EdgeDetector _edges = new EdgeDetector();
        readonly List<TouchPoint> _touches = new List<TouchPoint>(8);
        readonly List<Vector2> _tapsThisFrame = new List<Vector2>(4);
        TouchButtons _buttons;
        int _lastRefreshFrame = -1;
        const int MouseFingerId = -1;

        public DeviceInput(TouchZones zones)
        {
            _zones = zones;
            if (!EnhancedTouchSupport.enabled) EnhancedTouchSupport.Enable();
        }

        /// <summary>Screen positions (bottom-left origin) where a touch or click started this frame.</summary>
        public IReadOnlyList<Vector2> TapsThisFrame
        {
            get
            {
                Refresh();
                return _tapsThisFrame;
            }
        }

        /// <summary>State of the on-screen stick and jump button this frame (for drawing them).</summary>
        public TouchButtons CurrentTouchButtons
        {
            get
            {
                Refresh();
                return _buttons;
            }
        }

        public InputFrame Poll()
        {
            Refresh();
            float move = _buttons.Move;
            bool jump = _buttons.Jump;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) move -= 1f;
                if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) move += 1f;
                jump |= kb.spaceKey.isPressed || kb.upArrowKey.isPressed || kb.wKey.isPressed || kb.zKey.isPressed;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                float stick = pad.leftStick.x.ReadValue() + pad.dpad.x.ReadValue();
                if (Mathf.Abs(stick) > 0.3f) move += Mathf.Sign(stick);
                jump |= pad.buttonSouth.isPressed;
            }

            return _edges.Build(Mathf.Clamp(move, -1f, 1f), jump);
        }

        public void ResetEdges() => _edges.Reset();

        /// <summary>Any "confirm" action: a tap, click, Enter/Space, or gamepad A/Start.</summary>
        public bool ConfirmPressed()
        {
            Refresh();
            if (_tapsThisFrame.Count > 0) return true;
            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) return true;
            var pad = Gamepad.current;
            return pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame);
        }

        /// <summary>The menu action pressed this frame: arrows/WASD or d-pad/stick, Enter/Space or A/Start, Esc or B.</summary>
        public MenuCommand ReadMenuCommand()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) return MenuCommand.Up;
                if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) return MenuCommand.Down;
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) return MenuCommand.Left;
                if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) return MenuCommand.Right;
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) return MenuCommand.Submit;
                if (kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame) return MenuCommand.Back;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                if (pad.dpad.up.wasPressedThisFrame || pad.leftStick.up.wasPressedThisFrame) return MenuCommand.Up;
                if (pad.dpad.down.wasPressedThisFrame || pad.leftStick.down.wasPressedThisFrame) return MenuCommand.Down;
                if (pad.dpad.left.wasPressedThisFrame || pad.leftStick.left.wasPressedThisFrame) return MenuCommand.Left;
                if (pad.dpad.right.wasPressedThisFrame || pad.leftStick.right.wasPressedThisFrame) return MenuCommand.Right;
                if (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame) return MenuCommand.Submit;
                if (pad.buttonEast.wasPressedThisFrame) return MenuCommand.Back;
            }
            return MenuCommand.None;
        }

        public bool PauseKeyPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)) return true;
            var pad = Gamepad.current;
            return pad != null && pad.startButton.wasPressedThisFrame;
        }

        void Refresh()
        {
            if (_lastRefreshFrame == Time.frameCount) return;
            _lastRefreshFrame = Time.frameCount;
            _touches.Clear();
            _tapsThisFrame.Clear();

            foreach (var t in Touch.activeTouches)
            {
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) continue;
                _touches.Add(new TouchPoint(t.touchId, t.screenPosition));
                if (t.phase == TouchPhase.Began) _tapsThisFrame.Add(t.screenPosition);
            }

            // Mouse doubles as a single touch in the editor / simulator pointer.
            var mouse = Mouse.current;
            if (mouse != null && Touch.activeTouches.Count == 0)
            {
                var pos = mouse.position.ReadValue();
                if (mouse.leftButton.isPressed) _touches.Add(new TouchPoint(MouseFingerId, pos));
                if (mouse.leftButton.wasPressedThisFrame) _tapsThisFrame.Add(pos);
            }

            _buttons = _zones.Evaluate(_touches);
        }
    }
}
