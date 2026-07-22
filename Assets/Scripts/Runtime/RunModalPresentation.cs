using System;
using UnityEngine;

namespace ProjectExpedition
{
    public static class RunModalPresentation
    {
        public static void DrawModalBackground(Rect screen)
        {
            SurvivorsStylePresentation.DrawScreenBackground(screen);
            SurvivorsStylePresentation.DrawPanel(
                new Rect(0f, 0f, screen.width, screen.height),
                new Color(0.02f, 0.05f, 0.08f, 0.55f));
        }

        public static void DrawRewardCard(
            Rect rect,
            RewardOption option,
            int index,
            bool selected,
            GUIStyle heading,
            GUIStyle body,
            GUIStyle category,
            Action onChoose)
        {
            var fill = selected ? SurvivorsStylePresentation.TileSelected : SurvivorsStylePresentation.TileBackground;
            SurvivorsStylePresentation.DrawFlatPanel(rect, fill, selected ? 2f : 1f,
                selected ? SurvivorsStylePresentation.BorderGoldBright : SurvivorsStylePresentation.BorderGoldDim);

            if (option?.Item == null)
            {
                return;
            }

            var iconRect = new Rect(rect.x + 20f, rect.y + 16f, 48f, 48f);
            ItemPresentation.DrawItemIcon(iconRect, option.Item.Id, ItemIconSize.Medium,
                option.Item.Category == ItemCategory.Evolution, option.Item.Color);

            GUI.Label(new Rect(rect.x + 80f, rect.y + 18f, rect.width - 100f, 44f),
                $"{index + 1}. {option.Item.Name}", heading);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 72f, rect.width - 40f, 24f),
                RewardCategoryLabel(option), category);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 98f, rect.width - 40f, rect.height - 170f),
                option.Item.Description, body);

            var buttonRect = new Rect(rect.x + 20f, rect.yMax - 64f, rect.width - 40f, 48f);

            if (SurvivorsStylePresentation.DrawVsFooterButton(buttonRect, "CHOOSE REWARD", SurvivorsButtonKind.Green))
            {
                onChoose?.Invoke();
            }
        }

        public static void DrawSettingsShell(Rect panel, SurvivorsHudStyles styles, string title)
        {
            SurvivorsStylePresentation.DrawFlatPanel(panel, SurvivorsStylePresentation.PanelNavy, 2f);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 16f, panel.width - 48f, 40f), title, styles.Heading);
        }

        public static void DrawPauseShell(Rect panel, SurvivorsHudStyles styles)
        {
            SurvivorsStylePresentation.DrawFlatPanel(panel, SurvivorsStylePresentation.PanelNavy, 2f);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 20f, panel.width - 48f, 36f), "EXPEDITION PAUSED", styles.Heading);
        }

        public static void DrawResultsShell(Rect panel, SurvivorsHudStyles styles, string flavorTitle, bool victory, Color accent)
        {
            SurvivorsStylePresentation.DrawFlatPanel(panel, SurvivorsStylePresentation.PanelNavy, 2f, accent);
            DrawPanel(new Rect(panel.x, panel.y, panel.width, 6f), accent);

            DrawRunOutcomeBanner(
                new Rect(panel.x + 32f, panel.y + 18f, panel.width - 64f, 80f),
                victory,
                accent,
                styles);

            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 108f, panel.width - 48f, 40f),
                flavorTitle,
                styles.Heading);
        }

        public static void DrawRunOutcomeBanner(Rect rect, bool victory, Color accent, SurvivorsHudStyles styles)
        {
            var fill = victory
                ? new Color(accent.r * 0.14f, accent.g * 0.14f, accent.b * 0.14f, 0.98f)
                : new Color(0.16f, 0.04f, 0.05f, 0.98f);
            SurvivorsStylePresentation.DrawFlatPanel(rect, fill, 2f, accent);

            var label = victory ? "VICTORY" : "DEFEAT";
            var labelStyle = victory ? styles.RunOutcomeVictory : styles.RunOutcomeDefeat;
            GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, rect.height - 16f), label, labelStyle);
        }

        public static void DrawRunOutcomeTint(Rect screen, bool victory)
        {
            var tint = victory
                ? new Color(0.04f, 0.16f, 0.20f, 0.42f)
                : new Color(0.20f, 0.03f, 0.05f, 0.52f);
            SurvivorsStylePresentation.DrawPanel(screen, tint);
        }

        private static void DrawPanel(Rect rect, Color color)
        {
            SurvivorsStylePresentation.DrawPanel(rect, color);
        }

        private static string RewardCategoryLabel(RewardOption option)
        {
            if (option?.Item == null)
            {
                return string.Empty;
            }

            switch (option.Item.Category)
            {
                case ItemCategory.Weapon: return "WEAPON";
                case ItemCategory.Gear: return "GEAR";
                case ItemCategory.Evolution: return "EVOLUTION";
                case ItemCategory.Boon: return "BOON";
                default: return option.Item.Category.ToString().ToUpperInvariant();
            }
        }
    }
}
