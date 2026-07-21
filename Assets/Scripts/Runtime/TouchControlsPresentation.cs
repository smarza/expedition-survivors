using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectExpedition
{
    public static class TouchControlsPresentation
    {
        private static Vector2 _stickOrigin;
        private static bool _stickActive;
        private const float StickRadius = 72f;

        public static void Draw(GameDirector director)
        {
            if (!TouchInputRouter.ShouldShowOverlay || director == null)
            {
                return;
            }

            TouchInputRouter.BeginFrame();
            TouchInputRouter.SetTouchPlayerIndex(0);

            if (director.State == RunState.Playing || director.State == RunState.Paused)
            {
                DrawGameplayOverlay();
            }
        }

        private static void DrawGameplayOverlay()
        {
            var stickBase = new Rect(48f, 880f, 160f, 160f);
            var ultimateRect = new Rect(1712f, 900f, 96f, 96f);
            var pauseRect = new Rect(1600f, 900f, 96f, 48f);
            var detailsRect = new Rect(1600f, 960f, 96f, 48f);

            SurvivorsStylePresentation.DrawFlatPanel(stickBase, new Color(0.04f, 0.08f, 0.12f, 0.45f), 1f);
            GUI.Label(new Rect(stickBase.x, stickBase.y + 6f, stickBase.width, 20f), "MOVE", SurvivorsStylePresentation.CreateLabelStyle(
                10, FontStyle.Bold, SurvivorsStylePresentation.TextMuted, TextAnchor.MiddleCenter));

            HandleVirtualStick(stickBase);

            if (SurvivorsStylePresentation.DrawButton(ultimateRect, "ULT", SurvivorsButtonKind.Gold))
            {
                TouchInputRouter.PressUltimate();
            }

            if (SurvivorsStylePresentation.DrawButton(pauseRect, "II", SurvivorsButtonKind.Blue))
            {
                TouchInputRouter.PressPause();
            }

            if (SurvivorsStylePresentation.DrawButton(detailsRect, "BLD", SurvivorsButtonKind.Blue))
            {
                TouchInputRouter.PressDetails();
            }
        }

        private static void HandleVirtualStick(Rect stickBase)
        {
            var touch = Touchscreen.current;
            if (touch == null)
            {
                if (!_stickActive)
                {
                    TouchInputRouter.SetVirtualStick(Vector2.zero);
                }

                return;
            }

            var center = stickBase.center;
            var press = touch.primaryTouch;
            var position = press.position.ReadValue();

            if (press.press.wasPressedThisFrame && stickBase.Contains(position))
            {
                _stickActive = true;
                _stickOrigin = position;
            }

            if (!press.press.isPressed)
            {
                _stickActive = false;
                TouchInputRouter.SetVirtualStick(Vector2.zero);
                return;
            }

            if (!_stickActive)
            {
                return;
            }

            var delta = (Vector2)position - _stickOrigin;
            var normalized = delta / StickRadius;
            TouchInputRouter.SetVirtualStick(Vector2.ClampMagnitude(normalized, 1f));

            var knobSize = 36f;
            var knobCenter = center + Vector2.ClampMagnitude(delta, StickRadius);
            var knobRect = new Rect(knobCenter.x - knobSize * 0.5f, knobCenter.y - knobSize * 0.5f, knobSize, knobSize);
            SurvivorsStylePresentation.DrawPanel(knobRect, new Color(0.92f, 0.82f, 0.28f, 0.75f));
        }
    }
}
