using System;
using UnityEngine;

namespace ProjectExpedition
{
    public static class GameplayHudLayoutMetrics
    {
        public const float TopStripHeight = 36f;
        public const float PlayerStripHeight = 48f;
        public const float BottomBarHeight = 56f;
        public const float ObjectiveRailWidth = 220f;
        public const float ScreenPadding = 12f;
        public const float BuildIconSize = 32f;
        public const float BarHeight = 10f;
    }

    public static class GameplayHudPresentation
    {
        public static void Draw(
            Rect screen,
            GameDirector director,
            SurvivorsHudStyles styles,
            Func<BindingAction, string> prompt,
            Action<int, int, int, int> drawBar,
            Action drawBuildTray,
            Action drawObjectivePanel,
            Action drawFirstRunHints,
            Action drawPerformancePanel)
        {
            if (director?.Player == null)
            {
                return;
            }

            DrawTopStrip(screen, director, styles, prompt);
            DrawPlayerStrip(screen, director, styles, drawBar);
            DrawBottomBar(screen, director, styles, drawBar, drawBuildTray);
            drawObjectivePanel?.Invoke();
            drawFirstRunHints?.Invoke();

            if (director.ShowPerformanceMetrics)
            {
                drawPerformancePanel?.Invoke();
            }

            TouchControlsPresentation.Draw(director);
        }

        private static void DrawTopStrip(Rect screen, GameDirector director, SurvivorsHudStyles styles, Func<BindingAction, string> prompt)
        {
            var strip = new Rect(
                GameplayHudLayoutMetrics.ScreenPadding,
                GameplayHudLayoutMetrics.ScreenPadding,
                screen.width - GameplayHudLayoutMetrics.ObjectiveRailWidth - GameplayHudLayoutMetrics.ScreenPadding * 2f,
                GameplayHudLayoutMetrics.TopStripHeight);

            SurvivorsStylePresentation.DrawFlatPanel(strip, SurvivorsStylePresentation.PanelNavy, 1f);

            var remainingBoss = Mathf.Max(0f, director.Route.BossSpawnTime - director.Elapsed);
            var timerText = director.BossSpawned ? "DEFEAT THE JOTUNN" : $"JOTUNN IN {FormatTime(remainingBoss)}";
            var timerLine = $"{FormatTime(director.Elapsed)} / {FormatTime(director.SelectedMap.Duration)}  •  {timerText}";

            GUI.Label(new Rect(strip.x + 12f, strip.y + 6f, strip.width * 0.55f, 24f), timerLine, styles.Micro);

            var statsLine = $"KILLS {director.Kills}  •  RENOWN {director.RunRenown}  •  ENEMIES {director.Enemies.Count}";
            GUI.Label(new Rect(strip.x + strip.width * 0.45f, strip.y + 6f, strip.width * 0.53f, 24f), statsLine, styles.Micro);

            var controlHint = $"{prompt(BindingAction.Ultimate)} ULT  •  {prompt(BindingAction.Pause)} PAUSE";
            GUI.Label(new Rect(strip.x + 12f, strip.y + 18f, strip.width - 24f, 16f), controlHint, styles.Hint);
        }

        private static void DrawPlayerStrip(Rect screen, GameDirector director, SurvivorsHudStyles styles, Action<int, int, int, int> drawBar)
        {
            var stripTop = GameplayHudLayoutMetrics.ScreenPadding + GameplayHudLayoutMetrics.TopStripHeight + 8f;
            var stripWidth = screen.width - GameplayHudLayoutMetrics.ObjectiveRailWidth - GameplayHudLayoutMetrics.ScreenPadding * 2f;

            for (var i = 0; i < director.Players.Count; i++)
            {
                var player = director.Players[i];
                if (player == null)
                {
                    continue;
                }

                var row = new Rect(
                    GameplayHudLayoutMetrics.ScreenPadding,
                    stripTop + i * (GameplayHudLayoutMetrics.PlayerStripHeight + 4f),
                    stripWidth,
                    GameplayHudLayoutMetrics.PlayerStripHeight);

                SurvivorsStylePresentation.DrawFlatPanel(row, SurvivorsStylePresentation.PanelNavyInset, 1f);

                GUI.Label(new Rect(row.x + 10f, row.y + 4f, 220f, 18f),
                    $"P{i + 1}  {player.HeroName.ToUpperInvariant()}  L{director.Level}", styles.Caption);

                var barY = row.y + 24f;
                var barWidth = row.width - 20f;
                var healthLabel = player.IsDowned
                    ? $"DOWN {Mathf.RoundToInt(player.ReviveProgress * 100f)}%"
                    : $"{Mathf.CeilToInt(player.Health)}/{Mathf.CeilToInt(player.MaxHealth)}";

                drawBar?.Invoke(
                    (int)(row.x + 10f),
                    (int)barY,
                    (int)barWidth,
                    (int)GameplayHudLayoutMetrics.BarHeight);

                GUI.Label(new Rect(row.x + 10f, barY - 1f, barWidth, GameplayHudLayoutMetrics.BarHeight + 2f),
                    healthLabel, styles.Micro);

                var ultimateFill = player.UltimateReady ? 1f : 1f - player.UltimateRemaining / Mathf.Max(1f, player.UltimateCooldown);
                var ultimateY = barY + GameplayHudLayoutMetrics.BarHeight + 4f;
                drawBar?.Invoke((int)(row.x + 10f), (int)ultimateY, (int)barWidth, (int)(GameplayHudLayoutMetrics.BarHeight - 2f));
                GUI.Label(new Rect(row.x + 10f, ultimateY, barWidth, GameplayHudLayoutMetrics.BarHeight),
                    player.UltimateReady ? "ULT READY" : $"{player.UltimateRemaining:0}s", styles.Hint);
            }
        }

        private static void DrawBottomBar(
            Rect screen,
            GameDirector director,
            SurvivorsHudStyles styles,
            Action<int, int, int, int> drawBar,
            Action drawBuildTray)
        {
            var bar = new Rect(
                GameplayHudLayoutMetrics.ScreenPadding,
                screen.height - GameplayHudLayoutMetrics.BottomBarHeight - GameplayHudLayoutMetrics.ScreenPadding,
                screen.width - GameplayHudLayoutMetrics.ScreenPadding * 2f,
                GameplayHudLayoutMetrics.BottomBarHeight);

            SurvivorsStylePresentation.DrawFlatPanel(bar, SurvivorsStylePresentation.PanelNavy, 1f);

            var xpRect = new Rect(bar.x + 8f, bar.y + 8f, bar.width - 16f, 12f);
            drawBar?.Invoke((int)xpRect.x, (int)xpRect.y, (int)xpRect.width, (int)xpRect.height);
            GUI.Label(xpRect, $"XP {director.Experience}/{director.ExperienceToNext}", styles.Hint);

            var trayRect = new Rect(bar.x + 8f, bar.y + 24f, bar.width - 16f, GameplayHudLayoutMetrics.BuildIconSize + 4f);
            drawBuildTray?.Invoke();
        }

        public static void DrawObjectiveRail(Rect screen, GameDirector director, SurvivorsHudStyles styles, Action<int, int, int, int> drawPanel)
        {
            var route = director.Route;
            if (route == null)
            {
                return;
            }

            var rail = new Rect(
                screen.width - GameplayHudLayoutMetrics.ObjectiveRailWidth - GameplayHudLayoutMetrics.ScreenPadding,
                GameplayHudLayoutMetrics.ScreenPadding + GameplayHudLayoutMetrics.TopStripHeight + 8f,
                GameplayHudLayoutMetrics.ObjectiveRailWidth,
                280f);

            drawPanel?.Invoke((int)rail.x, (int)rail.y, (int)rail.width, (int)rail.height);
            SurvivorsStylePresentation.DrawFlatPanel(rail, SurvivorsStylePresentation.PanelNavy, 1f);
            GUI.Label(new Rect(rail.x + 10f, rail.y + 8f, rail.width - 20f, 20f), "OBJECTIVES", styles.Caption);

            var map = director.SelectedMap;
            var body = $"{map.KillObjectiveLabel}  {route.DraugrKills}/{route.RequiredKillObjective}";

            if (route.OptionalShardObjective > 0)
            {
                body += $"\n{map.OptionalPickupLabel}  {route.RuneShardsCollected}/{route.OptionalShardObjective}";
            }

            if (route.BossKilled && route.CurrentPhase == ExpeditionPhase.Extraction)
            {
                body += route.PartyAtExtractionBeacon
                    ? $"\nEXTRACTING {route.ExtractionHoldRemaining:0.0}s"
                    : "\nREACH BEACON";
            }

            GUI.Label(new Rect(rail.x + 10f, rail.y + 32f, rail.width - 20f, rail.height - 40f), body, styles.Body);
        }

        private static string FormatTime(float seconds)
        {
            var total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
