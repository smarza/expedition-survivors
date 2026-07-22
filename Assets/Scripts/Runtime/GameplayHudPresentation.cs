using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public static class GameplayHudLayoutMetrics
    {
        public const float TopStripHeight = 36f;
        public const float PlayerStripHeight = 72f;
        public const float BottomBarHeight = 88f;
        public const float ObjectiveRailWidth = 252f;
        public const float LootStripHeight = 64f;
        public const float LootColorTrackerHeaderHeight = 38f;
        public const float LootColorTrackerRowHeight = 20f;
        public const float LootColorTrackerBottomPadding = 8f;
        public const float LootPanelInnerGap = 6f;
        public const float ObjectivePanelGap = 8f;
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

        public static float ResolveLootColorTrackerHeight(int trackedEffectCount)
        {
            var safeCount = Mathf.Max(0, trackedEffectCount);
            return LootColorTrackerHeaderHeight +
                safeCount * LootColorTrackerRowHeight +
                LootColorTrackerBottomPadding;
        }

        public static float ResolveLootPanelTotalHeight(int trackedEffectCount)
        {
            return LootStripHeight + LootPanelInnerGap + ResolveLootColorTrackerHeight(trackedEffectCount);
        }

        public static float ResolveObjectivePanelHeight(GameDirector director)
        {
            var route = director?.Route;
            if (route == null)
            {
                return 132f;
            }

            var height = 132f;

            if (route.OptionalShardObjective > 0)
            {
                height += 16f;
            }

            var extractingAtBeacon = route.BossKilled &&
                route.CurrentPhase == ExpeditionPhase.Extraction &&
                route.PartyAtExtractionBeacon;

            if (route.IsDeploying || extractingAtBeacon)
            {
                return Mathf.Max(height, 162f);
            }

            if (route.BossKilled && route.CurrentPhase == ExpeditionPhase.Extraction)
            {
                height += 18f;
            }
            else if (route.CanSpawnBoss() && !route.BossSpawned)
            {
                height += 16f;
            }

            return height;
        }

        public static float ResolveLootStripTop(GameDirector director)
        {
            return ObjectivePanelTop + ResolveObjectivePanelHeight(director) + ObjectivePanelGap;
        }
    }

    public static class GameplayHudLabels
    {
        public static string ResolveBossObjective(SharedExpeditionRouteModel route, float remainingBossSeconds)
        {
            if (!route.BossSpawned)
            {
                return $"JOTUNN IN {FormatTime(remainingBossSeconds)}";
            }

            if (route.ExpectedBossCount > 1)
            {
                var remainingBosses = route.ExpectedBossCount - route.BossesDefeatedCount;
                return $"TWIN JOTUNN — {remainingBosses} REMAIN";
            }

            return "DEFEAT THE JOTUNN";
        }

        private static string FormatTime(float seconds)
        {
            var total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
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
            DrawDamageTakenVignette(screen, director);
            DrawBossProximityVignette(screen, director);
        }

        public static void DrawDamageTakenVignette(Rect screen, GameDirector director)
        {
            if (director?.PlayerHurtFeedback == null)
            {
                return;
            }

            var strength = 0f;
            for (var i = 0; i < director.Players.Count; i++)
            {
                strength = Mathf.Max(strength, director.PlayerHurtFeedback.ResolveVignetteStrength(i));
            }

            if (strength <= 0.01f)
            {
                return;
            }

            if (PresentationPreferences.Data.ReducedFlashes)
            {
                strength *= 0.55f;
            }

            var alpha = strength * 0.52f;
            var color = new Color(0.42f, 0.04f, 0.03f, alpha);
            var edgeThickness = Mathf.Lerp(24f, 88f, strength);

            SurvivorsStylePresentation.DrawPanel(new Rect(screen.x, screen.y, screen.width, edgeThickness), color);
            SurvivorsStylePresentation.DrawPanel(
                new Rect(screen.x, screen.yMax - edgeThickness, screen.width, edgeThickness),
                color);
            SurvivorsStylePresentation.DrawPanel(new Rect(screen.x, screen.y, edgeThickness, screen.height), color);
            SurvivorsStylePresentation.DrawPanel(
                new Rect(screen.xMax - edgeThickness, screen.y, edgeThickness, screen.height),
                color);
        }

        public static void DrawPlayerHealthBar(
            Rect rect,
            float fill,
            float ghostFill,
            float damagePulse,
            bool lowHealth,
            Color fillColor,
            string label,
            GUIStyle labelStyle,
            System.Action<Rect, Color> drawPanel)
        {
            const float inset = 2f;
            drawPanel?.Invoke(rect, new Color(0.02f, 0.035f, 0.045f, 1f));

            var clampedFill = Mathf.Clamp01(fill);
            var clampedGhost = Mathf.Clamp01(Mathf.Max(clampedFill, ghostFill));
            var ghostWidth = Mathf.Max(0f, (rect.width - inset * 2f) * clampedGhost);
            if (ghostWidth > 0f)
            {
                drawPanel?.Invoke(
                    new Rect(rect.x + inset, rect.y + inset, ghostWidth, rect.height - inset * 2f),
                    new Color(fillColor.r * 0.55f, fillColor.g * 0.35f, fillColor.b * 0.35f, 0.72f));
            }

            var fillWidth = Mathf.Max(0f, (rect.width - inset * 2f) * clampedFill);
            if (fillWidth > 0f)
            {
                var barColor = fillColor;
                if (damagePulse > 0.01f)
                {
                    var pulseStrength = damagePulse * (PresentationPreferences.Data.ReducedFlashes ? 0.45f : 0.85f);
                    barColor = Color.Lerp(fillColor, new Color(1f, 0.28f, 0.22f), pulseStrength);
                }
                else if (lowHealth)
                {
                    var heartbeat = 0.55f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5.2f)) * 0.45f;
                    if (PresentationPreferences.Data.ReducedFlashes)
                    {
                        heartbeat = 0.7f;
                    }

                    barColor = Color.Lerp(fillColor, new Color(0.92f, 0.22f, 0.18f), heartbeat * 0.55f);
                }

                drawPanel?.Invoke(
                    new Rect(rect.x + inset, rect.y + inset, fillWidth, rect.height - inset * 2f),
                    barColor);
            }

            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            GUI.Label(rect, label, labelStyle);
        }

        public static void DrawBossProximityVignette(Rect screen, GameDirector director)
        {
            if (director == null)
            {
                return;
            }

            var pressure = director.ResolveBossProximityPressure();
            if (pressure <= 0.01f)
            {
                return;
            }

            if (PresentationPreferences.Data.ReducedFlashes)
            {
                pressure *= 0.5f;
            }

            var pulse = 0.82f + 0.18f * Mathf.Sin(Time.unscaledTime * 4.8f);
            var alpha = pressure * pulse * 0.46f;
            var color = new Color(0.36f, 0.03f, 0.02f, alpha);
            var edgeThickness = Mathf.Lerp(32f, 104f, pressure);

            SurvivorsStylePresentation.DrawPanel(new Rect(screen.x, screen.y, screen.width, edgeThickness), color);
            SurvivorsStylePresentation.DrawPanel(
                new Rect(screen.x, screen.yMax - edgeThickness, screen.width, edgeThickness),
                color);
            SurvivorsStylePresentation.DrawPanel(new Rect(screen.x, screen.y, edgeThickness, screen.height), color);
            SurvivorsStylePresentation.DrawPanel(
                new Rect(screen.xMax - edgeThickness, screen.y, edgeThickness, screen.height),
                color);
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
            var timerText = GameplayHudLabels.ResolveBossObjective(director.Route, remainingBoss);
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
            var body = route.IsDeploying
                ? BiomeCatalog.ResolveDeploymentCountdownLine(route.DeploymentRemaining)
                : $"{map.KillObjectiveLabel}  {route.DraugrKills}/{route.RequiredKillObjective}";

            if (!route.IsDeploying && route.OptionalShardObjective > 0)
            {
                body += $"\n{map.OptionalPickupLabel}  {route.RuneShardsCollected}/{route.OptionalShardObjective}";
            }

            if (!route.IsDeploying && route.BossKilled && route.CurrentPhase == ExpeditionPhase.Extraction)
            {
                body += route.PartyAtExtractionBeacon
                    ? $"\nEXTRACTING {route.ExtractionHoldRemaining:0.0}s"
                    : "\nREACH BEACON";
            }

            GUI.Label(new Rect(rail.x + 10f, rail.y + 32f, rail.width - 20f, rail.height - 40f), body, styles.Body);

            if (route.IsDeploying)
            {
                var barRect = new Rect(rail.x + 10f, rail.y + rail.height - 28f, rail.width - 20f, 10f);
                SurvivorsStylePresentation.DrawFlatPanel(barRect, new Color(0.018f, 0.048f, 0.068f, 1f), 1f);
                var fillRect = new Rect(barRect.x, barRect.y, barRect.width * route.DeploymentProgress, barRect.height);
                SurvivorsStylePresentation.DrawFlatPanel(fillRect, PresentationTheme.Accent, 1f);
            }
        }

        public static void DrawDeploymentOverlay(Rect screen, GameDirector director, SurvivorsHudStyles styles)
        {
            var route = director?.Route;
            if (route == null || !route.IsDeploying)
            {
                return;
            }

            var map = director.SelectedMap;
            var dim = new Rect(0f, 0f, screen.width, screen.height);
            SurvivorsStylePresentation.DrawFlatPanel(dim, new Color(0.01f, 0.03f, 0.05f, 0.42f), 1f);

            var bannerWidth = 980f;
            var bannerHeight = 168f;
            var banner = new Rect(
                (screen.width - bannerWidth) * 0.5f,
                screen.height * 0.5f - bannerHeight * 0.5f,
                bannerWidth,
                bannerHeight);

            SurvivorsStylePresentation.DrawFlatPanel(banner, new Color(0.018f, 0.045f, 0.062f, 0.97f), 1f);
            SurvivorsStylePresentation.DrawBorder(banner, new Color(0.32f, 0.68f, 0.78f, 0.9f), 3f);

            GUI.Label(
                new Rect(banner.x + 28f, banner.y + 18f, banner.width - 56f, 30f),
                BiomeCatalog.ResolveDeploymentHeadline(map.BiomeId),
                styles.SectionTitle);
            GUI.Label(
                new Rect(banner.x + 28f, banner.y + 52f, banner.width - 56f, 44f),
                BiomeCatalog.ResolveDeploymentDetail(map.BiomeId),
                styles.Caption);
            GUI.Label(
                new Rect(banner.x + 28f, banner.y + 98f, banner.width - 56f, 36f),
                BiomeCatalog.ResolveDeploymentCountdownLine(route.DeploymentRemaining),
                styles.Display);

            var barRect = new Rect(banner.x + 28f, banner.y + bannerHeight - 24f, banner.width - 56f, 12f);
            SurvivorsStylePresentation.DrawFlatPanel(barRect, new Color(0.018f, 0.048f, 0.068f, 1f), 1f);
            var fillRect = new Rect(barRect.x, barRect.y, barRect.width * route.DeploymentProgress, barRect.height);
            SurvivorsStylePresentation.DrawFlatPanel(fillRect, PresentationTheme.Accent, 1f);
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
            var definition = loot.GetLeadingProgressDefinition();
            var required = loot.GetRequiredCount(definition);
            if (required <= 0 || definition == null)
            {
                return;
            }

            var effect = director.TemporaryEffect;
            var activeEffects = effect?.ActiveEffects;
            var hasActiveEffects = activeEffects != null && activeEffects.Count > 0;
            var definitions = DevelopmentTuningResolver.ResolveAllLootDefinitions();
            var stripLeft = screen.width - GameplayHudLayoutMetrics.ObjectiveRailWidth - GameplayHudLayoutMetrics.ScreenPadding;
            var stripTop = GameplayHudLayoutMetrics.ResolveLootStripTop(director);
            var panelHeight = GameplayHudLayoutMetrics.ResolveLootPanelTotalHeight(definitions.Count);
            var strip = new Rect(
                stripLeft,
                stripTop,
                GameplayHudLayoutMetrics.ObjectiveRailWidth,
                panelHeight);

            var summaryRect = new Rect(
                strip.x,
                strip.y,
                strip.width,
                GameplayHudLayoutMetrics.LootStripHeight);

            var trackerHeight = GameplayHudLayoutMetrics.ResolveLootColorTrackerHeight(definitions.Count);
            var trackerRect = new Rect(
                strip.x,
                strip.y + GameplayHudLayoutMetrics.LootStripHeight + GameplayHudLayoutMetrics.LootPanelInnerGap,
                strip.width,
                trackerHeight);

            SurvivorsStylePresentation.DrawFlatPanel(strip, SurvivorsStylePresentation.PanelNavyInset, 1f,
                new Color(0.18f, 0.24f, 0.32f, 0.85f));

            DrawLootSummaryStrip(summaryRect, loot, definition, required, activeEffects,
                hasActiveEffects, styles);

            DrawLootColorTracker(trackerRect, loot, effect, definitions, styles);
        }

        private static void DrawLootSummaryStrip(
            Rect strip,
            SharedLootProgressModel loot,
            LootEffectDefinition definition,
            int required,
            IReadOnlyList<ActiveTemporaryEffect> activeEffects,
            bool hasActiveEffects,
            SurvivorsHudStyles styles)
        {
            var theme = definition.ThemeColor;
            var nearActivation = loot.IsNearActivation(definition) && !hasActiveEffects;
            var panelColor = hasActiveEffects || nearActivation
                ? new Color(theme.r * 0.22f, theme.g * 0.22f, theme.b * 0.22f, 0.96f)
                : SurvivorsStylePresentation.PanelNavyInset;
            var borderColor = hasActiveEffects
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
            var currentCount = loot.GetCount(definition);
            var counter = hasActiveEffects
                ? BuildActiveEffectCounter(activeEffects)
                : $"{currentCount}/{required}";
            GUI.Label(new Rect(strip.x + 30f, strip.y + 18f, strip.width - 38f, 16f), counter, counterStyle);

            var progressRect = new Rect(strip.x + 8f, strip.y + 36f, strip.width - 16f, 10f);
            var progressFill = hasActiveEffects
                ? ResolveActiveEffectProgressFill(activeEffects)
                : currentCount / (float)required;
            DrawLootProgressBar(progressRect, progressFill, theme, hasActiveEffects);

            var hint = hasActiveEffects
                ? BuildActiveEffectHint(activeEffects)
                : nearActivation
                    ? "NEAR ACTIVATION"
                    : BuildCollectionHint(definition, required);
            GUI.Label(new Rect(strip.x + 8f, strip.y + 48f, strip.width - 16f, 12f), hint, styles.Hint);
        }

        private static void DrawLootColorTracker(
            Rect trackerRect,
            SharedLootProgressModel loot,
            SharedTemporaryEffectModel effect,
            IReadOnlyList<LootEffectDefinition> definitions,
            SurvivorsHudStyles styles)
        {
            SurvivorsStylePresentation.DrawFlatPanel(trackerRect, new Color(0.03f, 0.06f, 0.10f, 0.98f), 1f);

            GUI.Label(
                new Rect(trackerRect.x + 8f, trackerRect.y + 8f, trackerRect.width - 16f, 14f),
                "TIMED BUFFS",
                styles.Micro);
            GUI.Label(
                new Rect(trackerRect.x + 8f, trackerRect.y + 22f, trackerRect.width - 16f, 12f),
                "FILL ROWS FOR TIMED BONUSES",
                styles.Micro);

            var rowTop = trackerRect.y + GameplayHudLayoutMetrics.LootColorTrackerHeaderHeight;

            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition == null)
                {
                    continue;
                }

                var rowRect = new Rect(
                    trackerRect.x + 6f,
                    rowTop + i * GameplayHudLayoutMetrics.LootColorTrackerRowHeight,
                    trackerRect.width - 12f,
                    GameplayHudLayoutMetrics.LootColorTrackerRowHeight);

                DrawLootColorTrackerRow(rowRect, loot, effect, definition, styles);
            }
        }

        private static void DrawLootColorTrackerRow(
            Rect rowRect,
            SharedLootProgressModel loot,
            SharedTemporaryEffectModel effect,
            LootEffectDefinition definition,
            SurvivorsHudStyles styles)
        {
            var theme = definition.ThemeColor;
            var required = loot.GetRequiredCount(definition);
            var currentCount = loot.GetCount(definition);
            var isActive = effect != null && effect.IsActive(definition.Id);
            var isNearActivation = !isActive && loot.IsNearActivation(definition);
            var hasProgress = currentCount > 0 || isActive;

            if (isNearActivation)
            {
                var pulse = 0.35f + Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.35f;
                var highlightBorder = Color.Lerp(theme, SurvivorsStylePresentation.BorderGoldBright, pulse);
                SurvivorsStylePresentation.DrawFlatPanel(rowRect, new Color(0.04f, 0.08f, 0.12f, 0.95f), 1f,
                    highlightBorder);
            }
            else if (hasProgress)
            {
                SurvivorsStylePresentation.DrawFlatPanel(rowRect,
                    new Color(theme.r * 0.12f, theme.g * 0.12f, theme.b * 0.12f, 0.92f), 1f,
                    new Color(theme.r * 0.45f, theme.g * 0.45f, theme.b * 0.45f, 0.75f));
            }

            var iconRect = new Rect(rowRect.x + 2f, rowRect.y + 5f, 12f, 12f);
            ItemPresentation.DrawItemIcon(iconRect, definition.Id, ItemIconSize.Small, false, theme);

            const float counterWidth = 44f;
            const float labelWidth = 58f;
            var counterRect = new Rect(rowRect.xMax - counterWidth, rowRect.y + 3f, counterWidth, 14f);
            var labelRect = new Rect(rowRect.x + 16f, rowRect.y + 2f, labelWidth, 16f);

            var labelStyle = styles.Caption;
            labelStyle.wordWrap = false;
            labelStyle.clipping = TextClipping.Overflow;
            labelStyle.normal.textColor = hasProgress
                ? Color.Lerp(SurvivorsStylePresentation.TextLight, theme, 0.35f)
                : SurvivorsStylePresentation.TextMuted;
            GUI.Label(labelRect, ResolveLootTrackerLabel(definition), labelStyle);

            var counterStyle = styles.StatValue;
            counterStyle.normal.textColor = isActive ? theme : labelStyle.normal.textColor;
            var counterText = isActive
                ? ResolveActiveTimerLabel(effect, definition.Id)
                : $"{currentCount}/{required}";
            GUI.Label(counterRect, counterText, counterStyle);

            var barLeft = labelRect.xMax + 6f;
            var barRight = counterRect.x - 4f;
            var barWidth = Mathf.Max(20f, barRight - barLeft);
            var barRect = new Rect(barLeft, rowRect.y + 9f, barWidth, 6f);
            var barFill = isActive
                ? ResolveActiveEffectProgressFillForDefinition(effect, definition.Id)
                : required > 0
                    ? currentCount / (float)required
                    : 0f;
            DrawLootProgressBar(barRect, barFill, theme, isActive);
        }

        private static string ResolveLootTrackerLabel(LootEffectDefinition definition)
        {
            switch (definition.EffectType)
            {
                case TemporaryEffectType.Regeneration:
                    return "HEAL";
                case TemporaryEffectType.CriticalChance:
                    return "CRIT";
                case TemporaryEffectType.MoveSpeed:
                    return "SPEED";
                case TemporaryEffectType.DamageBoost:
                    return "DMG";
                case TemporaryEffectType.Invincibility:
                    return "SHIELD";
                default:
                    return "LOOT";
            }
        }

        private static string ResolveActiveTimerLabel(SharedTemporaryEffectModel effect, string definitionId)
        {
            if (effect == null)
            {
                return "ACTIVE";
            }

            for (var i = 0; i < effect.ActiveEffects.Count; i++)
            {
                var activeEffect = effect.ActiveEffects[i];
                if (activeEffect.Definition.Id != definitionId)
                {
                    continue;
                }

                return $"{activeEffect.Remaining:0.0}s";
            }

            return "ACTIVE";
        }

        private static float ResolveActiveEffectProgressFillForDefinition(
            SharedTemporaryEffectModel effect,
            string definitionId)
        {
            if (effect == null)
            {
                return 0f;
            }

            for (var i = 0; i < effect.ActiveEffects.Count; i++)
            {
                var activeEffect = effect.ActiveEffects[i];
                if (activeEffect.Definition.Id != definitionId)
                {
                    continue;
                }

                return activeEffect.Remaining /
                    Mathf.Max(0.01f, activeEffect.Definition.EffectDuration);
            }

            return 0f;
        }

        private static string BuildActiveEffectCounter(IReadOnlyList<ActiveTemporaryEffect> activeEffects)
        {
            if (activeEffects.Count == 1)
            {
                return "ACTIVE";
            }

            return $"ACTIVE x{activeEffects.Count}";
        }

        private static float ResolveActiveEffectProgressFill(IReadOnlyList<ActiveTemporaryEffect> activeEffects)
        {
            var longestRemaining = 0f;
            var longestDuration = 0.01f;

            for (var i = 0; i < activeEffects.Count; i++)
            {
                var activeEffect = activeEffects[i];
                if (activeEffect.Remaining > longestRemaining)
                {
                    longestRemaining = activeEffect.Remaining;
                    longestDuration = Mathf.Max(0.01f, activeEffect.Definition.EffectDuration);
                }
            }

            return longestRemaining / longestDuration;
        }

        private static string BuildActiveEffectHint(IReadOnlyList<ActiveTemporaryEffect> activeEffects)
        {
            if (activeEffects.Count == 0)
            {
                return string.Empty;
            }

            if (activeEffects.Count == 1)
            {
                return BuildEffectTimerHint(activeEffects[0]);
            }

            var hintParts = new System.Text.StringBuilder();
            for (var i = 0; i < activeEffects.Count && i < 2; i++)
            {
                if (i > 0)
                {
                    hintParts.Append("  ");
                }

                hintParts.Append(BuildEffectTimerHint(activeEffects[i]));
            }

            if (activeEffects.Count > 2)
            {
                hintParts.Append($" +{activeEffects.Count - 2}");
            }

            return hintParts.ToString();
        }

        private static string BuildCollectionHint(LootEffectDefinition definition, int required)
        {
            switch (definition.EffectType)
            {
                case TemporaryEffectType.Regeneration:
                    return $"COLLECT {required} FOR PARTY REGEN";
                case TemporaryEffectType.CriticalChance:
                    return $"COLLECT {required} FOR PARTY CRIT";
                case TemporaryEffectType.MoveSpeed:
                    return $"COLLECT {required} FOR PARTY SPEED";
                case TemporaryEffectType.DamageBoost:
                    return $"COLLECT {required} FOR PARTY DAMAGE";
                case TemporaryEffectType.Invincibility:
                    return $"COLLECT {required} FOR PARTY SHIELD";
                default:
                    return $"COLLECT {required} TO ACTIVATE";
            }
        }

        private static string BuildEffectTimerHint(ActiveTemporaryEffect activeEffect)
        {
            switch (activeEffect.Definition.EffectType)
            {
                case TemporaryEffectType.Regeneration:
                    return $"REGEN {activeEffect.Remaining:0.0}s";
                case TemporaryEffectType.CriticalChance:
                    return $"CRIT {activeEffect.Remaining:0.0}s";
                case TemporaryEffectType.MoveSpeed:
                    return $"SPEED {activeEffect.Remaining:0.0}s";
                case TemporaryEffectType.DamageBoost:
                    return $"DAMAGE {activeEffect.Remaining:0.0}s";
                case TemporaryEffectType.Invincibility:
                    return $"SHIELD {activeEffect.Remaining:0.0}s";
                default:
                    return $"{activeEffect.Remaining:0.0}s";
            }
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
            if (effect == null || !effect.HasActiveEffect)
            {
                return defaultColor;
            }

            ActiveTemporaryEffect regenEffect = null;
            ActiveTemporaryEffect shieldEffect = null;

            for (var i = 0; i < effect.ActiveEffects.Count; i++)
            {
                var activeEffect = effect.ActiveEffects[i];
                if (activeEffect.Definition.EffectType == TemporaryEffectType.Regeneration)
                {
                    regenEffect = activeEffect;
                }

                if (activeEffect.Definition.EffectType == TemporaryEffectType.Invincibility)
                {
                    shieldEffect = activeEffect;
                }
            }

            var pulseTarget = regenEffect ?? shieldEffect;
            if (pulseTarget == null)
            {
                return defaultColor;
            }

            var applies = pulseTarget.Definition.EffectTarget == TemporaryEffectTarget.WholeParty ||
                pulseTarget.ActivatorPlayerIndex == playerIndex;
            if (!applies)
            {
                return defaultColor;
            }

            var pulse = 0.55f + Mathf.Abs(Mathf.Sin(Time.time * 5.5f)) * 0.45f;
            if (PresentationPreferences.Data.ReducedFlashes)
            {
                pulse = 0.75f;
            }

            var theme = pulseTarget.Definition.ThemeColor;
            var blendStrength = regenEffect != null ? pulse : pulse * 0.55f;
            return Color.Lerp(defaultColor, theme, blendStrength);
        }
    }
}
