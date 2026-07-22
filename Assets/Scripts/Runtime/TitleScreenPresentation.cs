using System;
using UnityEngine;

namespace ProjectExpedition
{
    public static class TitleScreenPresentation
    {
        public const int ButtonCount = 2;

        private static class LayoutMetrics
        {
            public const float HeaderGradientHeight = 280f;
            public const float FooterGradientHeight = 320f;
            public const float TitleTopPadding = 72f;
            public const float TitleHeight = 64f;
            public const float AccentLineHeight = 2f;
            public const float AccentLineGap = 10f;
            public const float SubtitleHeight = 48f;
            public const float FooterY = 820f;
            public const float ButtonWidth = 360f;
            public const float ButtonHeight = 56f;
            public const float ButtonGap = 48f;
            public const float FooterPanelPadding = 24f;
        }

        public static void DrawBackground(Rect screen)
        {
            if (UiArtCatalog.TryGetTitleArt(out var titleArt))
            {
                GUI.DrawTexture(screen, titleArt, ScaleMode.ScaleAndCrop);
                DrawArtOverlays(screen);
                return;
            }

            SurvivorsStylePresentation.DrawScreenBackground(screen);
            DrawProceduralTitleArt(screen);
        }

        public static void Draw(Rect screen, SurvivorsHudStyles styles, int selection, Func<BindingAction, string> prompt)
        {
            DrawBackground(screen);
            DrawHeader(screen);
            DrawFooter(screen, styles, selection, prompt);
        }

        public static Action OnCampSelected;
        public static Action OnSettingsSelected;

        private static void DrawArtOverlays(Rect screen)
        {
            var topGradient = new Rect(screen.x, screen.y, screen.width, LayoutMetrics.HeaderGradientHeight);
            SurvivorsStylePresentation.DrawVerticalGradient(
                topGradient,
                new Color(0.02f, 0.05f, 0.09f, 0.92f),
                new Color(0.02f, 0.05f, 0.09f, 0f));

            var bottomGradient = new Rect(
                screen.x,
                screen.y + screen.height - LayoutMetrics.FooterGradientHeight,
                screen.width,
                LayoutMetrics.FooterGradientHeight);
            SurvivorsStylePresentation.DrawVerticalGradient(
                bottomGradient,
                new Color(0.02f, 0.05f, 0.09f, 0f),
                new Color(0.02f, 0.05f, 0.09f, 0.88f));
        }

        private static void DrawHeader(Rect screen)
        {
            var titleStyle = SurvivorsStylePresentation.CreateLabelStyle(
                48, FontStyle.Bold, SurvivorsStylePresentation.TextGold, TextAnchor.MiddleCenter);
            var titleRect = new Rect(screen.x + 120f, screen.y + LayoutMetrics.TitleTopPadding, screen.width - 240f, LayoutMetrics.TitleHeight);
            var shadowColor = new Color(0.01f, 0.03f, 0.06f, 0.85f);

            SurvivorsStylePresentation.DrawShadowedLabel(titleRect, "PROJECT EXPEDITION", titleStyle, shadowColor, 3f);

            var accentY = titleRect.yMax + LayoutMetrics.AccentLineGap;
            var accentWidth = Mathf.Min(520f, screen.width * 0.34f);
            var accentRect = new Rect(screen.center.x - accentWidth * 0.5f, accentY, accentWidth, LayoutMetrics.AccentLineHeight);
            SurvivorsStylePresentation.DrawPanel(accentRect, SurvivorsStylePresentation.BorderGoldBright);

            var subtitleStyle = SurvivorsStylePresentation.CreateLabelStyle(
                17, FontStyle.Bold, SurvivorsStylePresentation.TextLight, TextAnchor.UpperCenter);
            subtitleStyle.wordWrap = true;

            var subtitleRect = new Rect(
                screen.x + 160f,
                accentY + LayoutMetrics.AccentLineHeight + LayoutMetrics.AccentLineGap,
                screen.width - 320f,
                LayoutMetrics.SubtitleHeight);
            SurvivorsStylePresentation.DrawShadowedLabel(
                subtitleRect,
                "SURVIVE THE LONG NIGHT — CHOOSE YOUR EXPEDITION",
                subtitleStyle,
                shadowColor,
                2f);
        }

        private static void DrawFooter(Rect screen, SurvivorsHudStyles styles, int selection, Func<BindingAction, string> prompt)
        {
            const float footerY = LayoutMetrics.FooterY;
            const float buttonWidth = LayoutMetrics.ButtonWidth;
            const float buttonHeight = LayoutMetrics.ButtonHeight;
            const float buttonGap = LayoutMetrics.ButtonGap;
            var totalWidth = buttonWidth * 2f + buttonGap;
            var startX = screen.x + (screen.width - totalWidth) * 0.5f;

            var footerPanelRect = new Rect(
                startX - LayoutMetrics.FooterPanelPadding,
                footerY - LayoutMetrics.FooterPanelPadding,
                totalWidth + LayoutMetrics.FooterPanelPadding * 2f,
                buttonHeight + LayoutMetrics.FooterPanelPadding * 2f + 40f);
            SurvivorsStylePresentation.DrawFlatPanel(
                footerPanelRect,
                new Color(SurvivorsStylePresentation.PanelNavy.r, SurvivorsStylePresentation.PanelNavy.g, SurvivorsStylePresentation.PanelNavy.b, 0.72f),
                1f);

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
