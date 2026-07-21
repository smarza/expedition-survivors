using System;
using UnityEngine;

namespace ProjectExpedition
{
    public static class TitleScreenPresentation
    {
        public const int ButtonCount = 2;

        public static void DrawBackground(Rect screen)
        {
            if (UiArtCatalog.TryGetTitleArt(out var titleArt))
            {
                GUI.DrawTexture(screen, titleArt, ScaleMode.ScaleAndCrop);

                var vignette = new Rect(screen.x, screen.y + screen.height * 0.55f, screen.width, screen.height * 0.45f);
                SurvivorsStylePresentation.DrawPanel(vignette, new Color(0.02f, 0.05f, 0.09f, 0.82f));
                return;
            }

            SurvivorsStylePresentation.DrawScreenBackground(screen);
            DrawProceduralTitleArt(screen);
        }

        public static void Draw(Rect screen, SurvivorsHudStyles styles, int selection, Func<BindingAction, string> prompt)
        {
            DrawBackground(screen);

            var titleRect = new Rect(screen.x + 160f, screen.y + 120f, screen.width - 320f, 72f);
            GUI.Label(titleRect, "PROJECT EXPEDITION", styles.Title);

            var subtitleRect = new Rect(screen.x + 200f, titleRect.yMax + 8f, screen.width - 400f, 32f);
            GUI.Label(subtitleRect, "SURVIVE THE LONG NIGHT — CHOOSE YOUR EXPEDITION", styles.Caption);

            const float footerY = 820f;
            const float buttonWidth = 360f;
            const float buttonHeight = 56f;
            const float buttonGap = 48f;
            var totalWidth = buttonWidth * 2f + buttonGap;
            var startX = screen.x + (screen.width - totalWidth) * 0.5f;

            var campRect = new Rect(startX, footerY, buttonWidth, buttonHeight);
            var settingsRect = new Rect(startX + buttonWidth + buttonGap, footerY, buttonWidth, buttonHeight);

            if (selection == 0)
            {
                SurvivorsStylePresentation.DrawBorder(
                    new Rect(campRect.x - 4f, campRect.y - 4f, campRect.width + 8f, campRect.height + 8f),
                    SurvivorsStylePresentation.BorderGoldBright, 2f);
            }

            if (selection == 1)
            {
                SurvivorsStylePresentation.DrawBorder(
                    new Rect(settingsRect.x - 4f, settingsRect.y - 4f, settingsRect.width + 8f, settingsRect.height + 8f),
                    SurvivorsStylePresentation.BorderGoldBright, 2f);
            }

            if (SurvivorsStylePresentation.DrawVsFooterButton(campRect, "ENTER CAMP", SurvivorsButtonKind.Green))
            {
                OnCampSelected?.Invoke();
            }

            if (SurvivorsStylePresentation.DrawVsFooterButton(settingsRect, "SETTINGS", SurvivorsButtonKind.Blue))
            {
                OnSettingsSelected?.Invoke();
            }

            var hint = $"{prompt(BindingAction.Submit)} SELECT  •  {prompt(BindingAction.MoveLeft)} CHOOSE";
            GUI.Label(new Rect(screen.x + 320f, footerY + 72f, screen.width - 640f, 28f), hint, styles.Hint);
        }

        public static Action OnCampSelected;
        public static Action OnSettingsSelected;

        private static void DrawProceduralTitleArt(Rect screen)
        {
            var artRect = new Rect(screen.x + 80f, screen.y + 200f, screen.width - 160f, 520f);
            SurvivorsStylePresentation.DrawFlatPanel(artRect, SurvivorsStylePresentation.PanelNavy, 1f);

            var aurora = new Rect(artRect.x + artRect.width * 0.15f, artRect.y + 40f, artRect.width * 0.7f, 120f);
            SurvivorsStylePresentation.DrawPanel(aurora, new Color(0.18f, 0.72f, 0.62f, 0.35f));

            var fire = new Rect(artRect.center.x - 40f, artRect.yMax - 140f, 80f, 80f);
            SurvivorsStylePresentation.DrawPanel(fire, new Color(0.92f, 0.48f, 0.12f, 0.65f));
        }
    }
}
