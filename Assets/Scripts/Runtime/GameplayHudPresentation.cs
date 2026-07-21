using System;
using UnityEngine;

namespace ProjectExpedition
{
    public static class GameplayHudLayoutMetrics
    {
        public const float TopStripHeight = 36f;
        public const float PlayerStripHeight = 72f;
        public const float BottomBarHeight = 88f;
        public const float ObjectiveRailWidth = 220f;
        public const float LootStripHeight = 64f;
        public const float ObjectivePanelGap = 6f;
        public const float ScreenPadding = 12f;
        public const float BuildIconSize = 32f;
        public const float BarHeight = 20f;
        public const float SecondaryBarHeight = 18f;
        public const float BarVerticalGap = 5f;
        public const float PlayerCaptionTopPadding = 6f;
        public const float PlayerCaptionLineHeight = 20f;
        public const float BuildTrayTopOffset = 52f;
        public const float BuildPlayerLabelHeight = 18f;

        public static float PlayerHealthBarTop(float rowTop)
        {
            return rowTop + PlayerCaptionTopPadding + PlayerCaptionLineHeight;
        }

        public static float PlayerUltimateBarTop(float rowTop)
        {
            return PlayerHealthBarTop(rowTop) + BarHeight + BarVerticalGap;
        }

        public static float ObjectivePanelTop => ScreenPadding + TopStripHeight + 8f;

        public static float ResolveObjectivePanelHeight(GameDirector director)
        {
            var route = director?.Route;
            if (route == null)
            {
                return 118f;
            }

            var extractingAtBeacon = route.BossKilled &&
                route.CurrentPhase == ExpeditionPhase.Extraction &&
                route.PartyAtExtractionBeacon;

            return extractingAtBeacon ? 148f : 118f;
        }

        public static float ResolveLootStripTop(GameDirector director)
        {
            return ObjectivePanelTop + ResolveObjectivePanelHeight(director) + ObjectivePanelGap;
        }
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

                GUI.Label(new Rect(row.x + 10f, row.y + GameplayHudLayoutMetrics.PlayerCaptionTopPadding, 260f,
                        GameplayHudLayoutMetrics.PlayerCaptionLineHeight),
                    $"P{i + 1}  {player.HeroName.ToUpperInvariant()}  L{director.Level}", styles.Caption);

                var barY = GameplayHudLayoutMetrics.PlayerHealthBarTop(row.y);
                var barWidth = row.width - 20f;
                var healthLabel = player.IsDowned
                    ? $"DOWN {Mathf.RoundToInt(player.ReviveProgress * 100f)}%"
                    : $"{Mathf.CeilToInt(player.Health)}/{Mathf.CeilToInt(player.MaxHealth)}";

                drawBar?.Invoke(
                    (int)(row.x + 10f),
                    (int)barY,
                    (int)barWidth,
                    (int)GameplayHudLayoutMetrics.BarHeight);

                GUI.Label(new Rect(row.x + 10f, barY, barWidth, GameplayHudLayoutMetrics.BarHeight),
                    healthLabel, styles.Micro);

                var ultimateFill = player.UltimateReady ? 1f : 1f - player.UltimateRemaining / Mathf.Max(1f, player.UltimateCooldown);
                var ultimateY = GameplayHudLayoutMetrics.PlayerUltimateBarTop(row.y);
                drawBar?.Invoke((int)(row.x + 10f), (int)ultimateY, (int)barWidth,
                    (int)GameplayHudLayoutMetrics.SecondaryBarHeight);
                GUI.Label(new Rect(row.x + 10f, ultimateY, barWidth, GameplayHudLayoutMetrics.SecondaryBarHeight),
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

            var xpRect = new Rect(bar.x + 8f, bar.y + 8f, bar.width - 16f, GameplayHudLayoutMetrics.BarHeight);
            drawBar?.Invoke((int)xpRect.x, (int)xpRect.y, (int)xpRect.width, (int)xpRect.height);
            GUI.Label(xpRect, $"XP {director.Experience}/{director.ExperienceToNext}", styles.Micro);

            var trayRect = new Rect(bar.x + 8f, bar.y + GameplayHudLayoutMetrics.BuildTrayTopOffset, bar.width - 16f,
                GameplayHudLayoutMetrics.BuildIconSize + 4f);
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

        public static void DrawLootProgressStrip(Rect screen, GameDirector director, SurvivorsHudStyles styles)
        {
            if (director?.LootProgress == null)
            {
                return;
            }

            var loot = director.LootProgress;
            var definition = loot.TrackedDefinition ?? LootEffectCatalog.DefaultRunLoot;
            var required = loot.RequiredCount;
            if (required <= 0)
            {
                return;
            }

            var effect = director.TemporaryEffect;
            var effectActive = effect != null && effect.HasActiveEffect && effect.ActiveDefinition != null;
            var stripLeft = screen.width - GameplayHudLayoutMetrics.ObjectiveRailWidth - GameplayHudLayoutMetrics.ScreenPadding;
            var stripTop = GameplayHudLayoutMetrics.ResolveLootStripTop(director);
            var strip = new Rect(
                stripLeft,
                stripTop,
                GameplayHudLayoutMetrics.ObjectiveRailWidth,
                GameplayHudLayoutMetrics.LootStripHeight);

            var theme = definition.ThemeColor;
            var nearActivation = loot.IsNearActivation && !effectActive;
            var panelColor = effectActive || nearActivation
                ? new Color(theme.r * 0.22f, theme.g * 0.22f, theme.b * 0.22f, 0.96f)
                : SurvivorsStylePresentation.PanelNavyInset;
            var borderColor = effectActive
                ? theme
                : nearActivation
                    ? Color.Lerp(theme, SurvivorsStylePresentation.BorderGoldBright,
                        0.35f + Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.35f)
                    : new Color(theme.r * 0.55f, theme.g * 0.55f, theme.b * 0.55f, 0.85f);

            SurvivorsStylePresentation.DrawFlatPanel(strip, panelColor, 1f, borderColor);

            var iconRect = new Rect(strip.x + 8f, strip.y + 8f, 18f, 18f);
            ItemPresentation.DrawItemIcon(iconRect, definition.Id, ItemIconSize.Small, false, theme);

            GUI.Label(new Rect(strip.x + 30f, strip.y + 6f, strip.width - 38f, 14f), "PARTY LOOT", styles.Micro);

            var counterStyle = styles.Caption;
            counterStyle.normal.textColor = theme;
            var counter = effectActive
                ? "ACTIVE"
                : $"{loot.CurrentCount}/{required}";
            GUI.Label(new Rect(strip.x + 30f, strip.y + 18f, strip.width - 38f, 16f), counter, counterStyle);

            var progressRect = new Rect(strip.x + 8f, strip.y + 36f, strip.width - 16f, 10f);
            var progressFill = effectActive
                ? effect.Remaining / Mathf.Max(0.01f, effect.ActiveDefinition.EffectDuration)
                : loot.CurrentCount / (float)required;
            DrawLootProgressBar(progressRect, progressFill, theme, effectActive);

            var hint = effectActive
                ? $"REGEN {effect.Remaining:0.0}s"
                : nearActivation
                    ? "NEAR ACTIVATION"
                    : $"COLLECT {required} FOR PARTY REGEN";
            GUI.Label(new Rect(strip.x + 8f, strip.y + 48f, strip.width - 16f, 12f), hint, styles.Hint);
        }

        private static void DrawLootProgressBar(Rect rect, float fill, Color fillColor, bool pulsing)
        {
            const float inset = 1f;
            SurvivorsStylePresentation.DrawFlatPanel(rect, new Color(0.02f, 0.04f, 0.07f, 1f), 1f);

            var clampedFill = Mathf.Clamp01(fill);
            var fillWidth = Mathf.Max(0f, (rect.width - inset * 2f) * clampedFill);
            if (fillWidth <= 0f)
            {
                return;
            }

            var barColor = fillColor;
            if (pulsing)
            {
                var pulse = 0.65f + Mathf.Abs(Mathf.Sin(Time.time * 5.5f)) * 0.35f;
                barColor = Color.Lerp(fillColor, Color.white, pulse * 0.35f);
            }

            SurvivorsStylePresentation.DrawFlatPanel(
                new Rect(rect.x + inset, rect.y + inset, fillWidth, rect.height - inset * 2f),
                barColor,
                0f);
        }

        public static Color ResolveHealthBarColor(int playerIndex, GameDirector director, Color defaultColor)
        {
            var effect = director?.TemporaryEffect;
            if (effect == null || !effect.HasActiveEffect || effect.ActiveDefinition == null)
            {
                return defaultColor;
            }

            var applies = effect.ActiveDefinition.EffectTarget == TemporaryEffectTarget.WholeParty ||
                effect.ActivatorPlayerIndex == playerIndex;
            if (!applies || effect.ActiveDefinition.EffectType != TemporaryEffectType.Regeneration)
            {
                return defaultColor;
            }

            var pulse = 0.55f + Mathf.Abs(Mathf.Sin(Time.time * 5.5f)) * 0.45f;
            if (PresentationPreferences.Data.ReducedFlashes)
            {
                pulse = 0.75f;
            }

            var theme = effect.ActiveDefinition.ThemeColor;
            return Color.Lerp(defaultColor, theme, pulse);
        }
    }
}
