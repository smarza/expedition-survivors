using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectExpedition
{
    public static class TouchInputRouter
    {
        private const float MovementDeadzone = 0.18f;
        private static Vector2 _virtualStick;
        private static bool _ultimatePressed;
        private static bool _pausePressed;
        private static bool _detailsPressed;
        private static bool _submitPressed;
        private static int _touchPlayerIndex;

        public static bool IsTouchActive
        {
            get
            {
                var mode = PresentationPreferences.Data.TouchControls;

                if (mode == TouchControlsMode.Off || Touchscreen.current == null)
                {
                    return false;
                }

                return mode == TouchControlsMode.On || Touchscreen.current.primaryTouch.press.isPressed ||
                       Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            }
        }

        public static bool ShouldShowOverlay
        {
            get
            {
                var mode = PresentationPreferences.Data.TouchControls;

                if (mode == TouchControlsMode.Off)
                {
                    return false;
                }

                return Touchscreen.current != null;
            }
        }

        public static void BeginFrame()
        {
            _ultimatePressed = false;
            _pausePressed = false;
            _detailsPressed = false;
            _submitPressed = false;
        }

        public static void SetVirtualStick(Vector2 value)
        {
            _virtualStick = Vector2.ClampMagnitude(value, 1f);
            if (_virtualStick.sqrMagnitude > MovementDeadzone * MovementDeadzone)
            {
                LocalInputRouter.MarkTouch();
            }
        }

        public static void PressUltimate() => _ultimatePressed = true;
        public static void PressPause() => _pausePressed = true;
        public static void PressDetails() => _detailsPressed = true;
        public static void PressSubmit() => _submitPressed = true;

        public static void SetTouchPlayerIndex(int playerIndex) => _touchPlayerIndex = playerIndex;

        public static Vector2 ReadTouchMovement(int playerIndex, int playerCount)
        {
            if (!IsTouchActive || playerIndex != _touchPlayerIndex)
            {
                return Vector2.zero;
            }

            if (_virtualStick.sqrMagnitude <= MovementDeadzone * MovementDeadzone)
            {
                return Vector2.zero;
            }

            return _virtualStick;
        }

        public static bool TouchUltimatePressed(int playerIndex, int playerCount)
        {
            if (!IsTouchActive || playerIndex != _touchPlayerIndex)
            {
                return false;
            }

            if (!_ultimatePressed)
            {
                return false;
            }

            LocalInputRouter.MarkTouch();
            return true;
        }

        public static bool TouchPausePressed()
        {
            if (!IsTouchActive || !_pausePressed)
            {
                return false;
            }

            LocalInputRouter.MarkTouch();
            return true;
        }

        public static bool TouchDetailsPressed()
        {
            if (!IsTouchActive || !_detailsPressed)
            {
                return false;
            }

            LocalInputRouter.MarkTouch();
            return true;
        }

        public static bool TouchSubmitPressed(int playerIndex, int playerCount)
        {
            if (!IsTouchActive || playerIndex != _touchPlayerIndex || !_submitPressed)
            {
                return false;
            }

            LocalInputRouter.MarkTouch();
            return true;
        }

        public static bool RectTapped(Rect rect)
        {
            var touch = Touchscreen.current;
            if (touch == null)
            {
                return false;
            }

            var press = touch.primaryTouch;
            if (!press.press.wasPressedThisFrame)
            {
                return false;
            }

            var position = press.position.ReadValue();
            var inside = rect.Contains(position);

            if (inside)
            {
                LocalInputRouter.MarkTouch();
            }

            return inside;
        }
    }
}
