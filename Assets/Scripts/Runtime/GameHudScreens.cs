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
        public static void DrawBackground()
        {
            SurvivorsStylePresentation.DrawScreenBackground(new Rect(0f, 0f, 1920f, 1080f));
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
