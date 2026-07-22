using System;
using UnityEngine;

namespace ProjectExpedition
{
    public static class CampPresentation
    {
        public static void DrawShell(Rect screen, SurvivorsHudStyles styles, string ledgerLabel)
        {
            SurvivorsStylePresentation.DrawScreenBackground(screen);

            GUI.Label(new Rect(screen.x + 80f, screen.y + 34f, screen.width - 160f, 56f),
                "PROJECT EXPEDITION", styles.Title);
            GUI.Label(new Rect(screen.x + 84f, screen.y + 96f, screen.width - 168f, 32f),
                "FROSTBOUND CAMP — REST BY THE CONVERGENCE FIRE, THEN CHOOSE YOUR NEXT ROUTE", styles.Caption);

            var ledgerRect = new Rect(screen.x + 80f, screen.y + 140f, screen.width - 160f, 48f);
            SurvivorsStylePresentation.DrawCoinBadge(ledgerRect, ledgerLabel);
        }

        public static bool DrawBackToTitleButton(Rect screen, string label)
        {
            var backRect = new Rect(screen.xMax - 320f, screen.y + 34f, 240f, 56f);
            return SurvivorsStylePresentation.DrawButton(backRect, label, SurvivorsButtonKind.Red);
        }

        public static void DrawNavButton(Rect rect, string label, bool selected, bool highlight, Action onClick)
        {
            if (highlight)
            {
                SurvivorsStylePresentation.DrawBorder(
                    new Rect(rect.x - 8f, rect.y - 8f, rect.width + 16f, rect.height + 16f),
                    SurvivorsStylePresentation.BorderGoldBright, 2f);
            }

            if (selected)
            {
                SurvivorsStylePresentation.DrawBorder(
                    new Rect(rect.x - 4f, rect.y - 4f, rect.width + 8f, rect.height + 8f),
                    SurvivorsStylePresentation.BorderGold, 2f);
            }

            if (SurvivorsStylePresentation.DrawVsFooterButton(rect, label, SurvivorsButtonKind.Gold))
            {
                onClick?.Invoke();
            }
        }

        public static void DrawRelicVaultEntry(Rect rect, string relicId, string label, GUIStyle nameStyle)
        {
            var iconRect = new Rect(rect.x, rect.y + 2f, 36f, 36f);
            ItemPresentation.DrawRelicIcon(iconRect, relicId);
            GUI.Label(new Rect(rect.x + 44f, rect.y + 8f, rect.width - 48f, 24f), label, nameStyle);
        }
    }
}
