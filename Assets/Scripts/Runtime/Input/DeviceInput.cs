using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace SuperOttie.Input
{
    /// <summary>
    /// Merges touch buttons, keyboard and gamepad into one <see cref="IPlayerInput"/>, and exposes
    /// menu-level signals (taps, pause, confirm).
    /// </summary>
    public sealed class DeviceInput : IPlayerInput
    {
        readonly TouchZones _zones;
        readonly EdgeDetector _edges = new EdgeDetector();
        readonly List<Vector2> _touchPositions = new List<Vector2>(8);
        readonly List<Vector2> _tapsThisFrame = new List<Vector2>(4);
        int _lastRefreshFrame = -1;

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

        /// <summary>Which on-screen buttons are held right now (for pressed-state feedback).</summary>
        public TouchButtons CurrentTouchButtons
        {
            get
            {
                Refresh();
                return _zones.Evaluate(_touchPositions);
            }
        }

        public InputFrame Poll()
        {
            Refresh();
            var touch = _zones.Evaluate(_touchPositions);
            float move = touch.Move;
            bool jump = touch.Jump;

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
            _touchPositions.Clear();
            _tapsThisFrame.Clear();

            foreach (var t in Touch.activeTouches)
            {
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) continue;
                _touchPositions.Add(t.screenPosition);
                if (t.phase == TouchPhase.Began) _tapsThisFrame.Add(t.screenPosition);
            }

            // Mouse doubles as a single touch in the editor / simulator pointer.
            var mouse = Mouse.current;
            if (mouse != null && Touch.activeTouches.Count == 0)
            {
                var pos = mouse.position.ReadValue();
                if (mouse.leftButton.isPressed) _touchPositions.Add(pos);
                if (mouse.leftButton.wasPressedThisFrame) _tapsThisFrame.Add(pos);
            }
        }
    }
}
