using System;
using UnityEngine;

namespace ProjectExpedition
{
    public static class GameHudTitleScreen
    {
        public static void Update(ref int selection, Action enterCamp, Action openSettings)
        {
            TitleScreenPresentation.OnCampSelected = enterCamp;
            TitleScreenPresentation.OnSettingsSelected = openSettings;

            var direction = LocalInputRouter.AnyMenuHorizontalPressed();
            if (direction != 0)
            {
                selection = Wrap(selection + direction, TitleScreenPresentation.ButtonCount);
            }

            if (!LocalInputRouter.AnyMenuSubmitPressed())
            {
                return;
            }

            if (selection == 0)
            {
                enterCamp?.Invoke();
            }
            else
            {
                openSettings?.Invoke();
            }
        }

        public static void Draw(SurvivorsHudStyles styles, int selection, Func<BindingAction, string> prompt)
        {
            TitleScreenPresentation.Draw(new Rect(0f, 0f, 1920f, 1080f), styles, selection, prompt);
        }

        private static int Wrap(int value, int count) => (value % count + count) % count;
    }

    public static class GameHudCampScreen
    {
        public const int NavButtonCount = 4;
    }

    public static class GameHudMapSelectScreen
    {
        public static void DrawHeader(SurvivorsHudStyles styles)
        {
            SurvivorsStylePresentation.DrawScreenBackground(new Rect(0f, 0f, 1920f, 1080f));
            GUI.Label(new Rect(430f, 48f, 1060f, 56f), "CHOOSE THE EXPEDITION", styles.Title);
            GUI.Label(new Rect(360f, 108f, 1200f, 32f),
                "Select a route — locked expeditions can be previewed at camp renown cost.", styles.Caption);
        }
    }

    public static class GameHudRunScreen
    {
        public static void Draw(
            GameDirector director,
            SurvivorsHudStyles styles,
            Func<BindingAction, string> prompt,
            Action drawLegacyBuildTray,
            Action drawLegacyObjective,
            Action drawLegacyHints,
            Action drawLegacyPerformance)
        {
            GameplayHudPresentation.Draw(
                new Rect(0f, 0f, 1920f, 1080f),
                director,
                styles,
                prompt,
                null,
                drawLegacyBuildTray,
                drawLegacyObjective,
                drawLegacyHints,
                drawLegacyPerformance);
        }
    }

    public static class GameHudRunModalScreen
    {
    }
}
