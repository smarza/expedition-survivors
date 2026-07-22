using System;
using UnityEngine;

namespace ProjectExpedition
{
    public static class DevelopmentTuningPresentation
    {
        private const int TabRowIndex = 0;
        private const int FirstFieldRowIndex = 1;

        public static void Update(GameDirector director, DevelopmentTuningUiState state)
        {
            if (director == null || state == null || !director.ShowDevelopmentTuning)
            {
                return;
            }

            var vertical = LocalInputRouter.AnyMenuVerticalPressed();
            var horizontal = LocalInputRouter.AnyMenuHorizontalPressed();
            var rowCount = CountRows(director, state.Tab);
            var maxSelection = rowCount - 1;
            var includeApplyRow = director.State == RunState.Playing;

            if (vertical != 0)
            {
                state.Selection = Mathf.Clamp(state.Selection + vertical, 0, maxSelection);
            }

            if (horizontal != 0)
            {
                if (state.Selection == TabRowIndex)
                {
                    state.Tab = ShiftTab(state.Tab, horizontal);
                    state.Selection = FirstFieldRowIndex;
                    state.ContentIndex = 0;
                }
                else if (IsContentPickerRow(state))
                {
                    AdjustContentIndex(state, horizontal);
                }
                else if (IsFooterRow(state, rowCount, includeApplyRow))
                {
                    ActivateFooter(director, state, horizontal, rowCount, includeApplyRow);
                }
                else
                {
                    AdjustSelectedField(state, horizontal);
                }
            }

            if (LocalInputRouter.AnyMenuSubmitPressed())
            {
                if (IsToggleRow(state))
                {
                    ToggleBooleanField(state);
                }
                else if (IsFooterRow(state, rowCount, includeApplyRow))
                {
                    ActivateFooter(director, state, 1, rowCount, includeApplyRow);
                }
            }

            if (LocalInputRouter.MenuBackPressed() || LocalInputRouter.PausePressed())
            {
                director.CloseDevelopmentTuning();
            }
        }

        public static void Draw(
            GameDirector director,
            DevelopmentTuningUiState state,
            SurvivorsHudStyles styles,
            GUIStyle button,
            GUIStyle rowTitle,
            GUIStyle rowValue,
            GUIStyle micro,
            Action<Rect, Color> drawPanel,
            Action<Rect, bool> drawSelection)
        {
            if (director == null || state == null || !director.ShowDevelopmentTuning)
            {
                return;
            }

            RunModalPresentation.DrawModalBackground(new Rect(0f, 0f, 1920f, 1080f));
            var panel = new Rect(80f, 48f, 1760f, 920f);
            RunModalPresentation.DrawSettingsShell(panel, styles, "DEVELOPMENT TUNING — F4");

            DrawTabStrip(panel, state, rowTitle, drawPanel, drawSelection);
            DrawFieldRows(director, panel, state, button, rowTitle, rowValue, drawPanel, drawSelection);
            DrawFooter(director, panel, state, button, rowTitle, drawPanel, drawSelection);

            GUI.Label(new Rect(220f, 1010f, 1480f, 32f),
                "F4 TOGGLE  •  UP/DOWN NAVIGATE  •  LEFT/RIGHT ADJUST  •  EXPORT COPIES JSON  •  ESC CLOSE",
                micro);
        }

        private static void DrawTabStrip(
            Rect panel,
            DevelopmentTuningUiState state,
            GUIStyle rowTitle,
            Action<Rect, Color> drawPanel,
            Action<Rect, bool> drawSelection)
        {
            var tabs = (DevelopmentTuningTab[])Enum.GetValues(typeof(DevelopmentTuningTab));
            var tabWidth = (panel.width - 40f) / tabs.Length;
            var y = panel.y + 72f;

            for (var i = 0; i < tabs.Length; i++)
            {
                var rect = new Rect(panel.x + 20f + i * tabWidth, y, tabWidth - 6f, 42f);
                var selected = state.Selection == TabRowIndex && state.Tab == tabs[i];
                drawSelection(new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f), selected);
                drawPanel(rect, state.Tab == tabs[i]
                    ? new Color(0.09f, 0.18f, 0.22f, 1f)
                    : new Color(0.03f, 0.06f, 0.08f, 1f));
                GUI.Label(rect, TabLabel(tabs[i]), rowTitle);
            }
        }

        private static void DrawFieldRows(
            GameDirector director,
            Rect panel,
            DevelopmentTuningUiState state,
            GUIStyle button,
            GUIStyle rowTitle,
            GUIStyle rowValue,
            Action<Rect, Color> drawPanel,
            Action<Rect, bool> drawSelection)
        {
            var startY = panel.y + 130f;
            var rowHeight = 58f;
            var scalarIndex = 0;
            var lastFieldRow = CountRows(director, state.Tab) - FooterRowCount(director.State == RunState.Playing);

            for (var rowIndex = FirstFieldRowIndex; rowIndex < lastFieldRow; rowIndex++)
            {
                var y = startY + (rowIndex - FirstFieldRowIndex) * rowHeight;

                if (UsesContentPicker(state.Tab) && rowIndex == FirstFieldRowIndex)
                {
                    DrawAdjustableRow(panel, y, rowHeight, ContentPickerLabel(state), ContentPickerValue(state),
                        state, -1, state.Selection == rowIndex, button, rowTitle, rowValue, drawPanel,
                        drawSelection, false);
                    continue;
                }

                DrawAdjustableRow(panel, y, rowHeight, ScalarLabel(state, scalarIndex),
                    ScalarValue(state, scalarIndex), state, scalarIndex, state.Selection == rowIndex, button,
                    rowTitle, rowValue, drawPanel, drawSelection, IsToggleField(state.Tab, scalarIndex));
                scalarIndex++;
            }
        }

        private static void DrawFooter(
            GameDirector director,
            Rect panel,
            DevelopmentTuningUiState state,
            GUIStyle button,
            GUIStyle rowTitle,
            Action<Rect, Color> drawPanel,
            Action<Rect, bool> drawSelection)
        {
            var includeApplyRow = director.State == RunState.Playing;
            var rowCount = CountRows(director, state.Tab);
            var exportIndex = FooterExportIndex(rowCount, includeApplyRow);
            var resetIndex = FooterResetIndex(rowCount, includeApplyRow);
            var applyIndex = rowCount - 2;
            var closeIndex = rowCount - 1;
            var y = panel.y + 130f + (exportIndex - FirstFieldRowIndex) * 58f;

            DrawFooterButton(panel, y, "EXPORT TO CLIPBOARD", state.Selection == exportIndex, button, rowTitle,
                drawPanel, drawSelection, new Color(0.1f, 0.2f, 0.32f, 1f),
                () => ConfirmClipboardExport(director));
            y += 58f;

            DrawFooterButton(panel, y, "RESET TO DEFAULTS", state.Selection == resetIndex, button, rowTitle,
                drawPanel, drawSelection, new Color(0.45f, 0.12f, 0.14f, 1f),
                () => DevelopmentTuningService.ResetToDefaults());
            y += 58f;

            if (includeApplyRow)
            {
                DrawFooterButton(panel, y, "APPLY TO RUN", state.Selection == applyIndex, button, rowTitle,
                    drawPanel, drawSelection, new Color(0.12f, 0.28f, 0.18f, 1f),
                    () => director.ApplyDevelopmentTuningToActiveRun());
                y += 58f;
            }

            DrawFooterButton(panel, y, "CLOSE", state.Selection == closeIndex, button, rowTitle, drawPanel,
                drawSelection, new Color(0.45f, 0.12f, 0.14f, 1f),
                () => director.CloseDevelopmentTuning());
        }

        private static void ConfirmClipboardExport(GameDirector director)
        {
            if (director == null)
            {
                return;
            }

            var exported = DevelopmentTuningService.ExportToClipboard();
            var message = exported
                ? "DEV TUNING JSON COPIED TO CLIPBOARD"
                : "DEV TUNING CLIPBOARD EXPORT FAILED";
            director.Announce(message, 2.4f);
        }

        private static void DrawAdjustableRow(
            Rect panel,
            float y,
            float rowHeight,
            string label,
            string value,
            DevelopmentTuningUiState state,
            int fieldIndex,
            bool selected,
            GUIStyle button,
            GUIStyle rowTitle,
            GUIStyle rowValue,
            Action<Rect, Color> drawPanel,
            Action<Rect, bool> drawSelection,
            bool toggleOnly)
        {
            var rect = new Rect(panel.x + 24f, y, panel.width - 48f, rowHeight - 6f);
            drawSelection(new Rect(rect.x - 4f, rect.y - 4f, rect.width + 8f, rect.height + 8f), selected);
            drawPanel(rect, new Color(0.018f, 0.05f, 0.07f, 1f));
            GUI.Label(new Rect(rect.x + 18f, rect.y, 620f, rect.height), label, rowTitle);

            if (!toggleOnly)
            {
                if (GUI.Button(new Rect(rect.x + rect.width - 170f, rect.y + 6f, 54f, 46f), "−", button))
                {
                    if (fieldIndex >= 0)
                    {
                        AdjustSelectedFieldByIndex(state, fieldIndex, -1);
                    }
                    else
                    {
                        AdjustContentIndex(state, -1);
                    }
                }

                GUI.Label(new Rect(rect.x + rect.width - 300f, rect.y, 120f, rect.height), value, rowValue);

                if (GUI.Button(new Rect(rect.x + rect.width - 108f, rect.y + 6f, 54f, 46f), "+", button))
                {
                    if (fieldIndex >= 0)
                    {
                        AdjustSelectedFieldByIndex(state, fieldIndex, 1);
                    }
                    else
                    {
                        AdjustContentIndex(state, 1);
                    }
                }
            }
            else
            {
                GUI.Label(new Rect(rect.x + rect.width - 220f, rect.y, 200f, rect.height), value, rowValue);
            }
        }

        private static void DrawFooterButton(
            Rect panel,
            float y,
            string label,
            bool selected,
            GUIStyle button,
            GUIStyle rowTitle,
            Action<Rect, Color> drawPanel,
            Action<Rect, bool> drawSelection,
            Color background,
            Action onClick)
        {
            var rect = new Rect(panel.x + 520f, y, 720f, 52f);
            drawSelection(new Rect(rect.x - 4f, rect.y - 4f, rect.width + 8f, rect.height + 8f), selected);
            drawPanel(rect, background);

            if (GUI.Button(rect, label, button))
            {
                onClick?.Invoke();
            }

            GUI.Label(new Rect(rect.x + 18f, rect.y + 10f, rect.width - 36f, 32f), label, rowTitle);
        }

        private static int CountRows(GameDirector director, DevelopmentTuningTab tab)
        {
            var rows = FirstFieldRowIndex + ScalarFieldCount(tab) + FooterRowCount(
                director != null && director.State == RunState.Playing);

            if (UsesContentPicker(tab))
            {
                rows++;
            }

            return rows;
        }

        private static int FooterRowCount(bool includeApplyRow) => includeApplyRow ? 4 : 3;

        private static int FooterExportIndex(int rowCount, bool includeApplyRow) =>
            rowCount - (includeApplyRow ? 4 : 3);

        private static int FooterResetIndex(int rowCount, bool includeApplyRow) =>
            rowCount - (includeApplyRow ? 3 : 2);

        private static bool UsesContentPicker(DevelopmentTuningTab tab)
        {
            return tab == DevelopmentTuningTab.Loot || tab == DevelopmentTuningTab.Maps ||
                tab == DevelopmentTuningTab.Heroes || tab == DevelopmentTuningTab.Enemies ||
                tab == DevelopmentTuningTab.Weapons;
        }

        private static bool IsContentPickerRow(DevelopmentTuningUiState state)
        {
            return UsesContentPicker(state.Tab) && state.Selection == FirstFieldRowIndex;
        }

        private static bool IsFooterRow(DevelopmentTuningUiState state, int rowCount, bool includeApplyRow)
        {
            return state.Selection >= FooterExportIndex(rowCount, includeApplyRow);
        }

        private static bool IsToggleRow(DevelopmentTuningUiState state)
        {
            return IsToggleField(state.Tab, FieldIndexFromSelection(state));
        }

        private static int FieldIndexFromSelection(DevelopmentTuningUiState state)
        {
            var index = state.Selection - FirstFieldRowIndex;

            if (UsesContentPicker(state.Tab))
            {
                index--;
            }

            return index;
        }

        private static bool IsToggleField(DevelopmentTuningTab tab, int fieldIndex)
        {
            return tab == DevelopmentTuningTab.Loot && fieldIndex == 6;
        }

        private static int ScalarFieldCount(DevelopmentTuningTab tab)
        {
            switch (tab)
            {
                case DevelopmentTuningTab.Loot: return 7;
                case DevelopmentTuningTab.Spawn: return 7;
                case DevelopmentTuningTab.Experience: return 11;
                case DevelopmentTuningTab.Player: return 7;
                case DevelopmentTuningTab.Challenge: return 8;
                case DevelopmentTuningTab.Maps: return 9;
                case DevelopmentTuningTab.Heroes: return 6;
                case DevelopmentTuningTab.Enemies: return 8;
                case DevelopmentTuningTab.Weapons: return 8;
                default: return 0;
            }
        }

        private static DevelopmentTuningTab ShiftTab(DevelopmentTuningTab tab, int direction)
        {
            var values = (DevelopmentTuningTab[])Enum.GetValues(typeof(DevelopmentTuningTab));
            var index = Array.IndexOf(values, tab) + direction;

            if (index < 0)
            {
                index = values.Length - 1;
            }

            if (index >= values.Length)
            {
                index = 0;
            }

            return values[index];
        }

        private static string TabLabel(DevelopmentTuningTab tab)
        {
            switch (tab)
            {
                case DevelopmentTuningTab.Loot: return "LOOT";
                case DevelopmentTuningTab.Spawn: return "SPAWN";
                case DevelopmentTuningTab.Experience: return "XP";
                case DevelopmentTuningTab.Player: return "PLAYER";
                case DevelopmentTuningTab.Challenge: return "CHALLENGE";
                case DevelopmentTuningTab.Maps: return "MAPS";
                case DevelopmentTuningTab.Heroes: return "HEROES";
                case DevelopmentTuningTab.Enemies: return "ENEMIES";
                case DevelopmentTuningTab.Weapons: return "WEAPONS";
                default: return tab.ToString().ToUpperInvariant();
            }
        }

        private static void AdjustContentIndex(DevelopmentTuningUiState state, int direction)
        {
            var count = ContentCount(state.Tab);

            if (count <= 0)
            {
                return;
            }

            state.ContentIndex = (state.ContentIndex + direction + count) % count;
        }

        private static int ContentCount(DevelopmentTuningTab tab)
        {
            switch (tab)
            {
                case DevelopmentTuningTab.Loot: return LootEffectCatalog.All.Length;
                case DevelopmentTuningTab.Maps: return ContentCatalog.Maps.Length;
                case DevelopmentTuningTab.Heroes: return ContentCatalog.Characters.Length;
                case DevelopmentTuningTab.Enemies: return EnemyCatalog.All.Length;
                case DevelopmentTuningTab.Weapons: return WeaponProfile.AllWeapons.Length;
                default: return 0;
            }
        }

        private static string ContentPickerLabel(DevelopmentTuningUiState state)
        {
            switch (state.Tab)
            {
                case DevelopmentTuningTab.Loot: return "SELECT LOOT";
                case DevelopmentTuningTab.Maps: return "SELECT MAP";
                case DevelopmentTuningTab.Heroes: return "SELECT HERO";
                case DevelopmentTuningTab.Enemies: return "SELECT ENEMY";
                case DevelopmentTuningTab.Weapons: return "SELECT WEAPON";
                default: return "SELECT ENTRY";
            }
        }

        private static string ContentPickerValue(DevelopmentTuningUiState state)
        {
            switch (state.Tab)
            {
                case DevelopmentTuningTab.Loot:
                    return SelectedLootDefinition(state).DisplayName.ToUpperInvariant();
                case DevelopmentTuningTab.Maps:
                    return ContentCatalog.Map(Mathf.Clamp(state.ContentIndex, 0,
                        ContentCatalog.Maps.Length - 1)).Name.ToUpperInvariant();
                case DevelopmentTuningTab.Heroes:
                    return ContentCatalog.Character(Mathf.Clamp(state.ContentIndex, 0,
                        ContentCatalog.Characters.Length - 1)).Name.ToUpperInvariant();
                case DevelopmentTuningTab.Enemies:
                    return EnemyCatalog.All[Mathf.Clamp(state.ContentIndex, 0, EnemyCatalog.All.Length - 1)]
                        .Name.ToUpperInvariant();
                case DevelopmentTuningTab.Weapons:
                    return WeaponProfile.AllWeapons[Mathf.Clamp(state.ContentIndex, 0,
                        WeaponProfile.AllWeapons.Length - 1)].Id.ToUpperInvariant();
                default: return string.Empty;
            }
        }

        private static string ScalarLabel(DevelopmentTuningUiState state, int fieldIndex)
        {
            switch (state.Tab)
            {
                case DevelopmentTuningTab.Loot:
                    switch (fieldIndex)
                    {
                        case 0: return "BASE DROP CHANCE";
                        case 1: return "MINIMUM DROP CHANCE";
                        case 2: return "RARITY REDUCTION PER LEVEL";
                        case 3: return "REQUIRED COUNT";
                        case 4: return "EFFECT DURATION";
                        case 5: return LootIntensityLabel(state);
                        case 6: return "FORCE LOOT DROPS";
                        default: return string.Empty;
                    }
                case DevelopmentTuningTab.Spawn:
                    switch (fieldIndex)
                    {
                        case 0: return "MAX ACTIVE ENEMIES";
                        case 1: return "MIN SPAWN DISTANCE";
                        case 2: return "MAX SPAWN DISTANCE";
                        case 3: return "INITIAL SPAWN DELAY";
                        case 4: return "GROUP GROWTH SECONDS";
                        case 5: return "MAX GROUP SIZE";
                        case 6: return "INTERVAL ACCELERATION";
                        default: return string.Empty;
                    }
                case DevelopmentTuningTab.Experience:
                    switch (fieldIndex)
                    {
                        case 0: return "PLAYER COLLISION RADIUS";
                        case 1: return "REGULAR ENEMY LEVEL OFFSET";
                        case 2: return "ELITE ENEMY LEVEL OFFSET";
                        case 3: return "BOSS ENEMY LEVEL OFFSET";
                        case 4: return "XP PER ENEMY LEVEL";
                        case 5: return "ELITE XP MULTIPLIER";
                        case 6: return "XP BASE";
                        case 7: return "XP LINEAR";
                        case 8: return "XP POWER";
                        case 9: return "XP POWER SCALE";
                        case 10: return "CO-OP XP MULTIPLIER";
                        default: return string.Empty;
                    }
                case DevelopmentTuningTab.Player:
                    switch (fieldIndex)
                    {
                        case 0: return "DEFAULT MAGNET RADIUS";
                        case 1: return "DAMAGE IMMUNITY";
                        case 2: return "ULTIMATE IMMUNITY";
                        case 3: return "REVIVE IMMUNITY";
                        case 4: return "REVIVE DURATION";
                        case 5: return "REVIVE DECAY RATE";
                        case 6: return "REVIVE HEALTH FRACTION";
                        default: return string.Empty;
                    }
                case DevelopmentTuningTab.Challenge:
                    switch (fieldIndex)
                    {
                        case 0: return "VETERAN HEALTH MULTIPLIER";
                        case 1: return "VETERAN SPAWN RATE MULTIPLIER";
                        case 2: return "SWARM SURGE GROUP BONUS";
                        case 3: return "SWARM SURGE MAX GROUP";
                        case 4: return "GLASS CANNON DAMAGE TAKEN";
                        case 5: return "GLASS CANNON WEAPON DAMAGE";
                        case 6: return "RELENTLESS BOSS TIME MULT";
                        case 7: return "RELENTLESS KILL OBJECTIVE MULT";
                        default: return string.Empty;
                    }
                case DevelopmentTuningTab.Maps:
                    switch (fieldIndex)
                    {
                        case 0: return "DURATION";
                        case 1: return "BOSS SPAWN TIME";
                        case 2: return "BASE SPAWN INTERVAL";
                        case 3: return "MIN SPAWN INTERVAL";
                        case 4: return "DIFFICULTY RAMP";
                        case 5: return "REQUIRED KILL OBJECTIVE";
                        case 6: return "OPTIONAL SHARD OBJECTIVE";
                        case 7: return "EXTRACTION DURATION";
                        case 8: return "SKIP BOSS GATE";
                        default: return string.Empty;
                    }
                case DevelopmentTuningTab.Heroes:
                    switch (fieldIndex)
                    {
                        case 0: return "MAX HEALTH";
                        case 1: return "MOVE SPEED";
                        case 2: return "ARMOR";
                        case 3: return "ULTIMATE COOLDOWN";
                        case 4: return "ULTIMATE DAMAGE";
                        case 5: return "ULTIMATE RADIUS";
                        default: return string.Empty;
                    }
                case DevelopmentTuningTab.Enemies:
                    switch (fieldIndex)
                    {
                        case 0: return "BASE HEALTH";
                        case 1: return "HEALTH PER DIFFICULTY";
                        case 2: return "MIN SPEED";
                        case 3: return "MAX SPEED";
                        case 4: return "SPEED PER DIFFICULTY";
                        case 5: return "BASE CONTACT DAMAGE";
                        case 6: return "CONTACT DAMAGE PER DIFF";
                        case 7: return "MIN EXPERIENCE";
                        default: return string.Empty;
                    }
                case DevelopmentTuningTab.Weapons:
                    switch (fieldIndex)
                    {
                        case 0: return "BASE DAMAGE";
                        case 1: return "BASE COOLDOWN";
                        case 2: return "BASE COUNT";
                        case 3: return "BASE PIERCE";
                        case 4: return "CRITICAL CHANCE";
                        case 5: return "PROJECTILE SPEED";
                        case 6: return "PULSE RADIUS";
                        case 7: return "ORBIT RADIUS";
                        default: return string.Empty;
                    }
                default: return string.Empty;
            }
        }

        private static string ScalarValue(DevelopmentTuningUiState state, int fieldIndex)
        {
            var profile = DevelopmentTuningService.Active;

            switch (state.Tab)
            {
                case DevelopmentTuningTab.Loot:
                    switch (fieldIndex)
                    {
                        case 0: return FormatPercent(profile.BaseDropChance);
                        case 1: return FormatPercent(profile.MinimumDropChance);
                        case 2: return FormatFloat(profile.RarityReductionPerLevel, 3);
                        case 3: return profile.RequiredCount.ToString();
                        case 4: return FormatFloat(ResolveSelectedLoot(state).EffectDuration, 1);
                        case 5: return FormatLootIntensityValue(state);
                        case 6: return profile.ForceLootDropChance ? "ON" : "OFF";
                        default: return string.Empty;
                    }
                case DevelopmentTuningTab.Spawn:
                    switch (fieldIndex)
                    {
                        case 0: return profile.MaximumActiveEnemies.ToString();
                        case 1: return FormatFloat(profile.MinimumSpawnDistance, 2);
                        case 2: return FormatFloat(profile.MaximumSpawnDistance, 2);
                        case 3: return FormatFloat(profile.InitialSpawnDelay, 2);
                        case 4: return FormatFloat(profile.GroupGrowthSeconds, 1);
                        case 5: return profile.MaximumGroupSize.ToString();
                        case 6: return FormatFloat(profile.IntervalAccelerationPerSecond, 4);
                        default: return string.Empty;
                    }
                case DevelopmentTuningTab.Experience:
                    switch (fieldIndex)
                    {
                        case 0: return FormatFloat(profile.PlayerCollisionRadius, 2);
                        case 1: return profile.RegularEnemyLevelOffset.ToString();
                        case 2: return profile.EliteEnemyLevelOffset.ToString();
                        case 3: return profile.BossEnemyLevelOffset.ToString();
                        case 4: return FormatFloat(profile.ExperiencePerEnemyLevel, 2);
                        case 5: return FormatFloat(profile.EliteExperienceMultiplier, 2);
                        case 6: return FormatFloat(profile.XpBase, 1);
                        case 7: return FormatFloat(profile.XpLinear, 1);
                        case 8: return FormatFloat(profile.XpPower, 2);
                        case 9: return FormatFloat(profile.XpPowerScale, 1);
                        case 10: return FormatFloat(profile.CoopXpMultiplier, 2);
                        default: return string.Empty;
                    }
                case DevelopmentTuningTab.Player:
                    switch (fieldIndex)
                    {
                        case 0: return FormatFloat(profile.DefaultMagnetRadius, 2);
                        case 1: return FormatFloat(profile.DamageImmunityDuration, 2);
                        case 2: return FormatFloat(profile.UltimateImmunityDuration, 2);
                        case 3: return FormatFloat(profile.ReviveImmunityDuration, 2);
                        case 4: return FormatFloat(profile.ReviveDuration, 2);
                        case 5: return FormatPercent(profile.ReviveDecayRate);
                        case 6: return FormatPercent(profile.ReviveHealthFraction);
                        default: return string.Empty;
                    }
                case DevelopmentTuningTab.Challenge:
                    switch (fieldIndex)
                    {
                        case 0: return FormatFloat(profile.VeteranHealthMultiplier, 2);
                        case 1: return FormatFloat(profile.VeteranSpawnRateMultiplier, 2);
                        case 2: return FormatFloat(profile.SwarmSurgeGroupBonus, 1);
                        case 3: return profile.SwarmSurgeMaximumGroupSize.ToString();
                        case 4: return FormatFloat(profile.GlassCannonDamageTakenMultiplier, 2);
                        case 5: return FormatFloat(profile.GlassCannonWeaponDamageMultiplier, 2);
                        case 6: return FormatFloat(profile.RelentlessClockBossTimeMultiplier, 2);
                        case 7: return FormatFloat(profile.RelentlessClockKillObjectiveMultiplier, 2);
                        default: return string.Empty;
                    }
                case DevelopmentTuningTab.Maps:
                    if (fieldIndex == 8)
                    {
                        return profile.SkipBossGate ? "ON" : "OFF";
                    }

                    return FormatContentMapValue(state, fieldIndex);
                case DevelopmentTuningTab.Heroes:
                    return FormatContentHeroValue(state, fieldIndex);
                case DevelopmentTuningTab.Enemies:
                    return FormatContentEnemyValue(state, fieldIndex);
                case DevelopmentTuningTab.Weapons:
                    return FormatContentWeaponValue(state, fieldIndex);
                default: return string.Empty;
            }
        }

        private static string FormatContentMapValue(DevelopmentTuningUiState state, int fieldIndex)
        {
            var map = ContentCatalog.Map(Mathf.Clamp(state.ContentIndex, 0, ContentCatalog.Maps.Length - 1));
            var resolved = DevelopmentTuningResolver.ResolveMap(map);

            switch (fieldIndex)
            {
                case 0: return FormatFloat(resolved.Duration, 0);
                case 1: return FormatFloat(resolved.BossSpawnTime, 0);
                case 2: return FormatFloat(resolved.BaseSpawnInterval, 2);
                case 3: return FormatFloat(resolved.MinimumSpawnInterval, 2);
                case 4: return FormatFloat(resolved.DifficultyRamp, 1);
                case 5: return resolved.RequiredKillObjective.ToString();
                case 6: return resolved.OptionalShardObjective.ToString();
                case 7: return FormatFloat(resolved.ExtractionDuration, 1);
                default: return string.Empty;
            }
        }

        private static string FormatContentHeroValue(DevelopmentTuningUiState state, int fieldIndex)
        {
            var hero = ContentCatalog.Character(Mathf.Clamp(state.ContentIndex, 0,
                ContentCatalog.Characters.Length - 1));
            var resolved = DevelopmentTuningResolver.ResolveCharacter(hero);

            switch (fieldIndex)
            {
                case 0: return FormatFloat(resolved.MaxHealth, 0);
                case 1: return FormatFloat(resolved.MoveSpeed, 2);
                case 2: return FormatFloat(resolved.Armor, 2);
                case 3: return FormatFloat(resolved.UltimateCooldown, 0);
                case 4: return FormatFloat(resolved.UltimateDamage, 0);
                case 5: return FormatFloat(resolved.UltimateRadius, 1);
                default: return string.Empty;
            }
        }

        private static string FormatContentEnemyValue(DevelopmentTuningUiState state, int fieldIndex)
        {
            var enemy = EnemyCatalog.All[Mathf.Clamp(state.ContentIndex, 0, EnemyCatalog.All.Length - 1)];
            var resolved = DevelopmentTuningResolver.ResolveEnemy(enemy);

            switch (fieldIndex)
            {
                case 0: return FormatFloat(resolved.BaseHealth, 1);
                case 1: return FormatFloat(resolved.HealthPerDifficulty, 2);
                case 2: return FormatFloat(resolved.MinimumSpeed, 2);
                case 3: return FormatFloat(resolved.MaximumSpeed, 2);
                case 4: return FormatFloat(resolved.SpeedPerDifficulty, 3);
                case 5: return FormatFloat(resolved.BaseContactDamage, 1);
                case 6: return FormatFloat(resolved.ContactDamagePerDifficulty, 2);
                case 7: return resolved.MinimumExperience.ToString();
                default: return string.Empty;
            }
        }

        private static string FormatContentWeaponValue(DevelopmentTuningUiState state, int fieldIndex)
        {
            var weapon = WeaponProfile.AllWeapons[Mathf.Clamp(state.ContentIndex, 0,
                WeaponProfile.AllWeapons.Length - 1)];
            var resolved = DevelopmentTuningResolver.ApplyWeaponOverride(weapon);

            switch (fieldIndex)
            {
                case 0: return FormatFloat(resolved.BaseDamage, 1);
                case 1: return FormatFloat(resolved.BaseCooldown, 2);
                case 2: return resolved.BaseCount.ToString();
                case 3: return resolved.BasePierce.ToString();
                case 4: return FormatPercent(resolved.BaseCriticalChance);
                case 5: return FormatFloat(resolved.ProjectileSpeed, 1);
                case 6: return FormatFloat(resolved.PulseRadius, 2);
                case 7: return FormatFloat(resolved.OrbitRadius, 2);
                default: return string.Empty;
            }
        }

        private static void AdjustSelectedField(DevelopmentTuningUiState state, int direction)
        {
            AdjustSelectedFieldByIndex(state, FieldIndexFromSelection(state), direction);
        }

        private static void AdjustSelectedFieldByIndex(DevelopmentTuningUiState state, int fieldIndex, int direction)
        {
            var profile = DevelopmentTuningService.Active;

            switch (state.Tab)
            {
                case DevelopmentTuningTab.Loot:
                    AdjustLoot(profile, state, fieldIndex, direction);
                    break;
                case DevelopmentTuningTab.Spawn:
                    AdjustSpawn(profile, fieldIndex, direction);
                    break;
                case DevelopmentTuningTab.Experience:
                    AdjustExperience(profile, fieldIndex, direction);
                    break;
                case DevelopmentTuningTab.Player:
                    AdjustPlayer(profile, fieldIndex, direction);
                    break;
                case DevelopmentTuningTab.Challenge:
                    AdjustChallenge(profile, fieldIndex, direction);
                    break;
                case DevelopmentTuningTab.Maps:
                    if (fieldIndex == 8)
                    {
                        profile.SkipBossGate = !profile.SkipBossGate;
                    }
                    else
                    {
                        AdjustMapOverride(state, fieldIndex, direction);
                    }

                    break;
                case DevelopmentTuningTab.Heroes:
                    AdjustHeroOverride(state, fieldIndex, direction);
                    break;
                case DevelopmentTuningTab.Enemies:
                    AdjustEnemyOverride(state, fieldIndex, direction);
                    break;
                case DevelopmentTuningTab.Weapons:
                    AdjustWeaponOverride(state, fieldIndex, direction);
                    break;
            }

            DevelopmentTuningService.NotifyChanged();
        }

        private static void ToggleBooleanField(DevelopmentTuningUiState state)
        {
            var fieldIndex = FieldIndexFromSelection(state);
            AdjustSelectedFieldByIndex(state, fieldIndex, 1);
        }

        private static void ActivateFooter(
            GameDirector director,
            DevelopmentTuningUiState state,
            int direction,
            int rowCount,
            bool includeApplyRow)
        {
            if (direction <= 0)
            {
                return;
            }

            var resetIndex = FooterResetIndex(rowCount, includeApplyRow);
            var applyIndex = rowCount - 2;
            var closeIndex = rowCount - 1;
            var exportIndex = FooterExportIndex(rowCount, includeApplyRow);

            if (state.Selection == exportIndex)
            {
                ConfirmClipboardExport(director);
            }
            else if (state.Selection == resetIndex)
            {
                DevelopmentTuningService.ResetToDefaults();
            }
            else if (includeApplyRow && state.Selection == applyIndex)
            {
                director.ApplyDevelopmentTuningToActiveRun();
            }
            else if (state.Selection == closeIndex)
            {
                director.CloseDevelopmentTuning();
            }
        }

        private static void AdjustLoot(DevelopmentTuningProfileData profile, DevelopmentTuningUiState state,
            int fieldIndex, int direction)
        {
            switch (fieldIndex)
            {
                case 0: profile.BaseDropChance = Step(profile.BaseDropChance, 0.005f, direction, 0f, 1f); break;
                case 1: profile.MinimumDropChance = Step(profile.MinimumDropChance, 0.001f, direction, 0f, profile.BaseDropChance); break;
                case 2: profile.RarityReductionPerLevel = Step(profile.RarityReductionPerLevel, 0.01f, direction, 0f, 1f); break;
                case 3: profile.RequiredCount = (int)Step(profile.RequiredCount, 1f, direction, 1f, 100f); break;
                case 4: AdjustLootDurationOverride(state, direction); break;
                case 5: AdjustLootIntensityOverride(state, direction); break;
                case 6: profile.ForceLootDropChance = !profile.ForceLootDropChance; break;
            }
        }

        private static LootEffectDefinition SelectedLootDefinition(DevelopmentTuningUiState state)
        {
            var index = Mathf.Clamp(state.ContentIndex, 0, LootEffectCatalog.All.Length - 1);
            return LootEffectCatalog.All[index];
        }

        private static LootEffectDefinition ResolveSelectedLoot(DevelopmentTuningUiState state)
        {
            return DevelopmentTuningResolver.ResolveLootDefinition(SelectedLootDefinition(state));
        }

        private static string LootIntensityLabel(DevelopmentTuningUiState state)
        {
            switch (SelectedLootDefinition(state).EffectType)
            {
                case TemporaryEffectType.Regeneration: return "REGEN HP/S";
                case TemporaryEffectType.CriticalChance: return "CRIT CHANCE BONUS";
                case TemporaryEffectType.MoveSpeed: return "MOVE SPEED BONUS";
                case TemporaryEffectType.DamageBoost: return "DAMAGE MULTIPLIER";
                case TemporaryEffectType.Invincibility: return "SHIELD (IGNORED)";
                default: return "EFFECT INTENSITY";
            }
        }

        private static string FormatLootIntensityValue(DevelopmentTuningUiState state)
        {
            var loot = ResolveSelectedLoot(state);
            switch (SelectedLootDefinition(state).EffectType)
            {
                case TemporaryEffectType.CriticalChance: return FormatPercent(loot.EffectIntensity);
                case TemporaryEffectType.DamageBoost: return FormatFloat(loot.EffectIntensity, 2);
                case TemporaryEffectType.Invincibility: return "ACTIVE";
                default: return FormatFloat(loot.EffectIntensity, 2);
            }
        }

        private static void AdjustLootDurationOverride(DevelopmentTuningUiState state, int direction)
        {
            var baseDefinition = SelectedLootDefinition(state);
            var entry = DevelopmentTuningResolver.GetOrCreateLootOverride(baseDefinition.Id);
            if (!entry.OverrideEffectDuration)
            {
                entry.OverrideEffectDuration = true;
                entry.EffectDuration = baseDefinition.EffectDuration;
            }

            entry.EffectDuration = Step(entry.EffectDuration, 0.5f, direction, 0.5f, 120f);
        }

        private static void AdjustLootIntensityOverride(DevelopmentTuningUiState state, int direction)
        {
            var baseDefinition = SelectedLootDefinition(state);
            if (baseDefinition.EffectType == TemporaryEffectType.Invincibility)
            {
                return;
            }

            var entry = DevelopmentTuningResolver.GetOrCreateLootOverride(baseDefinition.Id);
            if (!entry.OverrideEffectIntensity)
            {
                entry.OverrideEffectIntensity = true;
                entry.EffectIntensity = baseDefinition.EffectIntensity;
            }

            var step = baseDefinition.EffectType == TemporaryEffectType.CriticalChance ? 0.01f : 0.05f;
            var maximum = baseDefinition.EffectType == TemporaryEffectType.DamageBoost ? 5f : 100f;
            entry.EffectIntensity = Step(entry.EffectIntensity, step, direction, 0f, maximum);
        }

        private static void AdjustSpawn(DevelopmentTuningProfileData profile, int fieldIndex, int direction)
        {
            switch (fieldIndex)
            {
                case 0: profile.MaximumActiveEnemies = (int)Step(profile.MaximumActiveEnemies, 5f, direction, 1f, 1000f); break;
                case 1: profile.MinimumSpawnDistance = Step(profile.MinimumSpawnDistance, 0.1f, direction, 0.1f, 50f); break;
                case 2: profile.MaximumSpawnDistance = Step(profile.MaximumSpawnDistance, 0.1f, direction, profile.MinimumSpawnDistance, 50f); break;
                case 3: profile.InitialSpawnDelay = Step(profile.InitialSpawnDelay, 0.05f, direction, 0f, 10f); break;
                case 4: profile.GroupGrowthSeconds = Step(profile.GroupGrowthSeconds, 1f, direction, 1f, 300f); break;
                case 5: profile.MaximumGroupSize = (int)Step(profile.MaximumGroupSize, 1f, direction, 1f, 50f); break;
                case 6: profile.IntervalAccelerationPerSecond = Step(profile.IntervalAccelerationPerSecond, 0.0001f, direction, 0f, 0.05f); break;
            }
        }

        private static void AdjustExperience(DevelopmentTuningProfileData profile, int fieldIndex, int direction)
        {
            switch (fieldIndex)
            {
                case 0: profile.PlayerCollisionRadius = Step(profile.PlayerCollisionRadius, 0.01f, direction, 0.01f, 2f); break;
                case 1: profile.RegularEnemyLevelOffset = (int)Step(profile.RegularEnemyLevelOffset, 1f, direction, 0f, 20f); break;
                case 2: profile.EliteEnemyLevelOffset = (int)Step(profile.EliteEnemyLevelOffset, 1f, direction, 0f, 20f); break;
                case 3: profile.BossEnemyLevelOffset = (int)Step(profile.BossEnemyLevelOffset, 1f, direction, 0f, 20f); break;
                case 4: profile.ExperiencePerEnemyLevel = Step(profile.ExperiencePerEnemyLevel, 0.01f, direction, 0f, 2f); break;
                case 5: profile.EliteExperienceMultiplier = Step(profile.EliteExperienceMultiplier, 0.05f, direction, 0.01f, 10f); break;
                case 6: profile.XpBase = Step(profile.XpBase, 1f, direction, 0f, 500f); break;
                case 7: profile.XpLinear = Step(profile.XpLinear, 1f, direction, 0f, 100f); break;
                case 8: profile.XpPower = Step(profile.XpPower, 0.05f, direction, 0.1f, 5f); break;
                case 9: profile.XpPowerScale = Step(profile.XpPowerScale, 0.1f, direction, 0f, 20f); break;
                case 10: profile.CoopXpMultiplier = Step(profile.CoopXpMultiplier, 0.05f, direction, 1f, 5f); break;
            }
        }

        private static void AdjustPlayer(DevelopmentTuningProfileData profile, int fieldIndex, int direction)
        {
            switch (fieldIndex)
            {
                case 0: profile.DefaultMagnetRadius = Step(profile.DefaultMagnetRadius, 0.1f, direction, 0f, 20f); break;
                case 1: profile.DamageImmunityDuration = Step(profile.DamageImmunityDuration, 0.02f, direction, 0f, 5f); break;
                case 2: profile.UltimateImmunityDuration = Step(profile.UltimateImmunityDuration, 0.05f, direction, 0f, 10f); break;
                case 3: profile.ReviveImmunityDuration = Step(profile.ReviveImmunityDuration, 0.05f, direction, 0f, 10f); break;
                case 4: profile.ReviveDuration = Step(profile.ReviveDuration, 0.1f, direction, 0.1f, 20f); break;
                case 5: profile.ReviveDecayRate = Step(profile.ReviveDecayRate, 0.01f, direction, 0f, 1f); break;
                case 6: profile.ReviveHealthFraction = Step(profile.ReviveHealthFraction, 0.01f, direction, 0.01f, 1f); break;
            }
        }

        private static void AdjustChallenge(DevelopmentTuningProfileData profile, int fieldIndex, int direction)
        {
            switch (fieldIndex)
            {
                case 0: profile.VeteranHealthMultiplier = Step(profile.VeteranHealthMultiplier, 0.05f, direction, 0.01f, 10f); break;
                case 1: profile.VeteranSpawnRateMultiplier = Step(profile.VeteranSpawnRateMultiplier, 0.05f, direction, 0.01f, 10f); break;
                case 2: profile.SwarmSurgeGroupBonus = Step(profile.SwarmSurgeGroupBonus, 1f, direction, 0f, 10f); break;
                case 3: profile.SwarmSurgeMaximumGroupSize = (int)Step(profile.SwarmSurgeMaximumGroupSize, 1f, direction, 1f, 50f); break;
                case 4: profile.GlassCannonDamageTakenMultiplier = Step(profile.GlassCannonDamageTakenMultiplier, 0.05f, direction, 0.01f, 10f); break;
                case 5: profile.GlassCannonWeaponDamageMultiplier = Step(profile.GlassCannonWeaponDamageMultiplier, 0.05f, direction, 0.01f, 10f); break;
                case 6: profile.RelentlessClockBossTimeMultiplier = Step(profile.RelentlessClockBossTimeMultiplier, 0.05f, direction, 0.01f, 10f); break;
                case 7: profile.RelentlessClockKillObjectiveMultiplier = Step(profile.RelentlessClockKillObjectiveMultiplier, 0.05f, direction, 0.01f, 10f); break;
            }
        }

        private static void AdjustMapOverride(DevelopmentTuningUiState state, int fieldIndex, int direction)
        {
            var map = ContentCatalog.Map(Mathf.Clamp(state.ContentIndex, 0, ContentCatalog.Maps.Length - 1));
            var entry = DevelopmentTuningResolver.GetOrCreateMapOverride(map.Id);
            var resolved = DevelopmentTuningResolver.ResolveMap(map);

            switch (fieldIndex)
            {
                case 0:
                    var duration = entry.OverrideDuration ? entry.Duration : resolved.Duration;
                    entry.OverrideDuration = true;
                    entry.Duration = Step(duration, 10f, direction, 30f, 3600f);
                    break;
                case 1:
                    var bossSpawnTime = entry.OverrideBossSpawnTime ? entry.BossSpawnTime : resolved.BossSpawnTime;
                    entry.OverrideBossSpawnTime = true;
                    entry.BossSpawnTime = Step(bossSpawnTime, 5f, direction, 10f, 3600f);
                    break;
                case 2:
                    var baseSpawnInterval = entry.OverrideBaseSpawnInterval
                        ? entry.BaseSpawnInterval
                        : resolved.BaseSpawnInterval;
                    entry.OverrideBaseSpawnInterval = true;
                    entry.BaseSpawnInterval = Step(baseSpawnInterval, 0.02f, direction, 0.05f, 10f);
                    break;
                case 3:
                    var minimumSpawnInterval = entry.OverrideMinimumSpawnInterval
                        ? entry.MinimumSpawnInterval
                        : resolved.MinimumSpawnInterval;
                    entry.OverrideMinimumSpawnInterval = true;
                    entry.MinimumSpawnInterval = Step(minimumSpawnInterval, 0.02f, direction, 0.01f, 10f);
                    break;
                case 4:
                    var difficultyRamp = entry.OverrideDifficultyRamp ? entry.DifficultyRamp : resolved.DifficultyRamp;
                    entry.OverrideDifficultyRamp = true;
                    entry.DifficultyRamp = Step(difficultyRamp, 1f, direction, 1f, 500f);
                    break;
                case 5:
                    var requiredKillObjective = entry.OverrideRequiredKillObjective
                        ? entry.RequiredKillObjective
                        : resolved.RequiredKillObjective;
                    entry.OverrideRequiredKillObjective = true;
                    entry.RequiredKillObjective = (int)Step(requiredKillObjective, 5f, direction, 1f, 5000f);
                    break;
                case 6:
                    var optionalShardObjective = entry.OverrideOptionalShardObjective
                        ? entry.OptionalShardObjective
                        : resolved.OptionalShardObjective;
                    entry.OverrideOptionalShardObjective = true;
                    entry.OptionalShardObjective = (int)Step(optionalShardObjective, 1f, direction, 0f, 100f);
                    break;
                case 7:
                    var extractionDuration = entry.OverrideExtractionDuration
                        ? entry.ExtractionDuration
                        : resolved.ExtractionDuration;
                    entry.OverrideExtractionDuration = true;
                    entry.ExtractionDuration = Step(extractionDuration, 1f, direction, 1f, 120f);
                    break;
            }
        }

        private static void AdjustHeroOverride(DevelopmentTuningUiState state, int fieldIndex, int direction)
        {
            var hero = ContentCatalog.Character(Mathf.Clamp(state.ContentIndex, 0, ContentCatalog.Characters.Length - 1));
            var entry = DevelopmentTuningResolver.GetOrCreateCharacterOverride(hero.Id);
            var resolved = DevelopmentTuningResolver.ResolveCharacter(hero);

            switch (fieldIndex)
            {
                case 0:
                    var maxHealth = entry.OverrideMaxHealth ? entry.MaxHealth : resolved.MaxHealth;
                    entry.OverrideMaxHealth = true;
                    entry.MaxHealth = Step(maxHealth, 5f, direction, 1f, 2000f);
                    break;
                case 1:
                    var moveSpeed = entry.OverrideMoveSpeed ? entry.MoveSpeed : resolved.MoveSpeed;
                    entry.OverrideMoveSpeed = true;
                    entry.MoveSpeed = Step(moveSpeed, 0.1f, direction, 0f, 20f);
                    break;
                case 2:
                    var armor = entry.OverrideArmor ? entry.Armor : resolved.Armor;
                    entry.OverrideArmor = true;
                    entry.Armor = Step(armor, 0.25f, direction, 0f, 50f);
                    break;
                case 3:
                    var ultimateCooldown = entry.OverrideUltimateCooldown
                        ? entry.UltimateCooldown
                        : resolved.UltimateCooldown;
                    entry.OverrideUltimateCooldown = true;
                    entry.UltimateCooldown = Step(ultimateCooldown, 1f, direction, 1f, 600f);
                    break;
                case 4:
                    var ultimateDamage = entry.OverrideUltimateDamage
                        ? entry.UltimateDamage
                        : resolved.UltimateDamage;
                    entry.OverrideUltimateDamage = true;
                    entry.UltimateDamage = Step(ultimateDamage, 5f, direction, 0f, 2000f);
                    break;
                case 5:
                    var ultimateRadius = entry.OverrideUltimateRadius
                        ? entry.UltimateRadius
                        : resolved.UltimateRadius;
                    entry.OverrideUltimateRadius = true;
                    entry.UltimateRadius = Step(ultimateRadius, 0.2f, direction, 0f, 50f);
                    break;
            }
        }

        private static void AdjustEnemyOverride(DevelopmentTuningUiState state, int fieldIndex, int direction)
        {
            var enemy = EnemyCatalog.All[Mathf.Clamp(state.ContentIndex, 0, EnemyCatalog.All.Length - 1)];
            var entry = DevelopmentTuningResolver.GetOrCreateEnemyOverride(enemy.Id);
            var resolved = DevelopmentTuningResolver.ResolveEnemy(enemy);

            switch (fieldIndex)
            {
                case 0:
                    entry.OverrideBaseHealth = true;
                    entry.BaseHealth = Step(entry.OverrideBaseHealth ? entry.BaseHealth : resolved.BaseHealth, 1f, direction, 1f, 5000f);
                    break;
                case 1:
                    entry.OverrideHealthPerDifficulty = true;
                    entry.HealthPerDifficulty = Step(entry.OverrideHealthPerDifficulty ? entry.HealthPerDifficulty : resolved.HealthPerDifficulty, 0.25f, direction, 0f, 500f);
                    break;
                case 2:
                    entry.OverrideMinimumSpeed = true;
                    entry.MinimumSpeed = Step(entry.OverrideMinimumSpeed ? entry.MinimumSpeed : resolved.MinimumSpeed, 0.05f, direction, 0f, 20f);
                    break;
                case 3:
                    entry.OverrideMaximumSpeed = true;
                    entry.MaximumSpeed = Step(entry.OverrideMaximumSpeed ? entry.MaximumSpeed : resolved.MaximumSpeed, 0.05f, direction, 0f, 20f);
                    break;
                case 4:
                    entry.OverrideSpeedPerDifficulty = true;
                    entry.SpeedPerDifficulty = Step(entry.OverrideSpeedPerDifficulty ? entry.SpeedPerDifficulty : resolved.SpeedPerDifficulty, 0.005f, direction, 0f, 2f);
                    break;
                case 5:
                    entry.OverrideBaseContactDamage = true;
                    entry.BaseContactDamage = Step(entry.OverrideBaseContactDamage ? entry.BaseContactDamage : resolved.BaseContactDamage, 0.5f, direction, 0f, 500f);
                    break;
                case 6:
                    entry.OverrideContactDamagePerDifficulty = true;
                    entry.ContactDamagePerDifficulty = Step(entry.OverrideContactDamagePerDifficulty ? entry.ContactDamagePerDifficulty : resolved.ContactDamagePerDifficulty, 0.05f, direction, 0f, 50f);
                    break;
                case 7:
                    entry.OverrideMinimumExperience = true;
                    entry.MinimumExperience = (int)Step(entry.OverrideMinimumExperience ? entry.MinimumExperience : resolved.MinimumExperience, 1f, direction, 0f, 1000f);
                    break;
            }
        }

        private static void AdjustWeaponOverride(DevelopmentTuningUiState state, int fieldIndex, int direction)
        {
            var weapon = WeaponProfile.AllWeapons[Mathf.Clamp(state.ContentIndex, 0, WeaponProfile.AllWeapons.Length - 1)];
            var entry = DevelopmentTuningResolver.GetOrCreateWeaponOverride(weapon.Id);
            var resolved = DevelopmentTuningResolver.ApplyWeaponOverride(weapon);

            switch (fieldIndex)
            {
                case 0:
                    entry.OverrideBaseDamage = true;
                    entry.BaseDamage = Step(entry.OverrideBaseDamage ? entry.BaseDamage : resolved.BaseDamage, 1f, direction, 0f, 500f);
                    break;
                case 1:
                    entry.OverrideBaseCooldown = true;
                    entry.BaseCooldown = Step(entry.OverrideBaseCooldown ? entry.BaseCooldown : resolved.BaseCooldown, 0.05f, direction, 0.01f, 60f);
                    break;
                case 2:
                    entry.OverrideBaseCount = true;
                    entry.BaseCount = (int)Step(entry.OverrideBaseCount ? entry.BaseCount : resolved.BaseCount, 1f, direction, 1f, 20f);
                    break;
                case 3:
                    entry.OverrideBasePierce = true;
                    entry.BasePierce = (int)Step(entry.OverrideBasePierce ? entry.BasePierce : resolved.BasePierce, 1f, direction, 0f, 50f);
                    break;
                case 4:
                    entry.OverrideBaseCriticalChance = true;
                    entry.BaseCriticalChance = Step(entry.OverrideBaseCriticalChance ? entry.BaseCriticalChance : resolved.BaseCriticalChance, 0.01f, direction, 0f, 1f);
                    break;
                case 5:
                    entry.OverrideProjectileSpeed = true;
                    entry.ProjectileSpeed = Step(entry.OverrideProjectileSpeed ? entry.ProjectileSpeed : resolved.ProjectileSpeed, 0.25f, direction, 0f, 50f);
                    break;
                case 6:
                    entry.OverridePulseRadius = true;
                    entry.PulseRadius = Step(entry.OverridePulseRadius ? entry.PulseRadius : resolved.PulseRadius, 0.1f, direction, 0f, 50f);
                    break;
                case 7:
                    entry.OverrideOrbitRadius = true;
                    entry.OrbitRadius = Step(entry.OverrideOrbitRadius ? entry.OrbitRadius : resolved.OrbitRadius, 0.1f, direction, 0f, 50f);
                    break;
            }
        }

        private static float Step(float current, float step, int direction, float minimum, float maximum)
        {
            return Mathf.Clamp(current + step * direction, minimum, maximum);
        }

        private static string FormatFloat(float value, int decimals) => value.ToString($"F{decimals}");

        private static string FormatPercent(float value) => $"{value * 100f:0.#}%";
    }
}
