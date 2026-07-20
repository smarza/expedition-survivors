using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public enum UnlockCategory
    {
        Hero,
        Expedition
    }

    public enum CodexCategory
    {
        Hero,
        Expedition,
        Weapon,
        Gear,
        Evolution,
        Relic
    }

    public enum CodexVisibility
    {
        Hidden,
        Hint,
        Discovered
    }

    public readonly struct UnlockDefinition
    {
        public readonly string ContentId;
        public readonly string DisplayName;
        public readonly int RenownCost;
        public readonly UnlockCategory Category;

        public UnlockDefinition(string contentId, string displayName, int renownCost, UnlockCategory category)
        {
            ContentId = contentId;
            DisplayName = displayName;
            RenownCost = renownCost;
            Category = category;
        }
    }

    public readonly struct CodexDefinition
    {
        public readonly string ContentId;
        public readonly CodexCategory Category;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly string HintCondition;
        public readonly string EvolutionBaseId;
        public readonly string EvolutionCatalystId;

        public CodexDefinition(
            string contentId,
            CodexCategory category,
            string displayName,
            string description,
            string hintCondition = null,
            string evolutionBaseId = null,
            string evolutionCatalystId = null)
        {
            ContentId = contentId;
            Category = category;
            DisplayName = displayName;
            Description = description;
            HintCondition = hintCondition;
            EvolutionBaseId = evolutionBaseId;
            EvolutionCatalystId = evolutionCatalystId;
        }
    }

    public sealed class PurchaseResult
    {
        public bool Success;
        public string Message;
    }

    /// <summary>
    /// Presentation-free camp progression rules: renown unlocks, mastery accrual and codex discovery.
    /// </summary>
    public static class SharedMetaProgressionModel
    {
        public const string HaldorId = "ravenbound.haldor";
        public const string EiraId = "ravenbound.eira";
        public const string SylvaId = "oathbound.sylva";
        public const string MaraId = "ironway.mara";
        public const string ScoutMapId = "frostbound.scout";
        public const string SagaMapId = "frostbound.saga";

        private static readonly string[] StarterUnlockIds = { HaldorId, ScoutMapId };

        private static readonly UnlockDefinition[] UnlockCatalog =
        {
            new UnlockDefinition(HaldorId, "Haldor Stormborn", 0, UnlockCategory.Hero),
            new UnlockDefinition(ScoutMapId, "The Frostbound Shore (Scout)", 0, UnlockCategory.Expedition),
            new UnlockDefinition(SylvaId, "Sylva Reedwalker", 75, UnlockCategory.Hero),
            new UnlockDefinition(EiraId, "Eira Raven-Sworn", 110, UnlockCategory.Hero),
            new UnlockDefinition(MaraId, "Captain Mara Voss", 145, UnlockCategory.Hero),
            new UnlockDefinition(SagaMapId, "The Frostbound Shore: Long Night", 200, UnlockCategory.Expedition)
        };

        private static readonly CodexDefinition[] CodexCatalog =
        {
            new CodexDefinition(HaldorId, CodexCategory.Hero, "Haldor Stormborn",
                "Ravenbound expedition leader — Frost Axe and Raven Guard."),
            new CodexDefinition(EiraId, CodexCategory.Hero, "Eira Raven-Sworn",
                "Ravenbound storm scout — swift pathfinder with Murder of Ravens."),
            new CodexDefinition(SylvaId, CodexCategory.Hero, "Sylva Reedwalker",
                "Oathbound canopy warden — thorn lash and canopy vortex."),
            new CodexDefinition(MaraId, CodexCategory.Hero, "Captain Mara Voss",
                "Ironway field captain — signal flare and supply pulse."),
            new CodexDefinition(ScoutMapId, CodexCategory.Expedition, "The Frostbound Shore (Scout)",
                "Five-minute Scout route with objectives, Jotunn boss and extraction beacon."),
            new CodexDefinition(SagaMapId, CodexCategory.Expedition, "The Frostbound Shore: Long Night",
                "Twelve-minute expedition with denser phases and stronger elites."),
            new CodexDefinition("weapon.frost_axe", CodexCategory.Weapon, "Frost Axe",
                "Automatic rune-axe throws with critical strikes."),
            new CodexDefinition("weapon.raven_guard", CodexCategory.Weapon, "Raven Guard",
                "Automatic shield shockwave around the owner."),
            new CodexDefinition("weapon.grove_thorn_lash", CodexCategory.Weapon, "Grove Thorn Lash",
                "Fast thorn pulse around Sylva."),
            new CodexDefinition("weapon.canopy_vortex", CodexCategory.Weapon, "Canopy Vortex",
                "Radial canopy burst."),
            new CodexDefinition("weapon.signal_flare", CodexCategory.Weapon, "Signal Flare",
                "Explosive signal projectile."),
            new CodexDefinition("weapon.supply_pulse", CodexCategory.Weapon, "Supply Pulse",
                "Periodic heal pulse."),
            new CodexDefinition("gear.jotunn_rune", CodexCategory.Gear, "Jotunn Rune",
                "Catalyst — prepares Frost Axe for the Jotunn Cleaver evolution."),
            new CodexDefinition("gear.bear_blooded", CodexCategory.Gear, "Bear-Blooded",
                "Catalyst — prepares Raven Guard for the Storm Aegis evolution."),
            new CodexDefinition("gear.grove_seed", CodexCategory.Gear, "Grove Seed",
                "Catalyst — prepares Grove Thorn Lash for the Grove Crown evolution."),
            new CodexDefinition("gear.flare_core", CodexCategory.Gear, "Flare Core",
                "Catalyst — prepares Signal Flare for the Signal Storm evolution."),
            new CodexDefinition("evolution.jotunn_cleaver", CodexCategory.Evolution, "Jotunn Cleaver",
                "Frost axes explode and split damage across clustered enemies.",
                "Max Frost Axe + Jotunn Rune catalyst discovered.",
                "weapon.frost_axe", "gear.jotunn_rune"),
            new CodexDefinition("evolution.storm_aegis", CodexCategory.Evolution, "Storm Aegis",
                "Raven Guard becomes larger and restores health when it erupts.",
                "Max Raven Guard + Bear-Blooded catalyst discovered.",
                "weapon.raven_guard", "gear.bear_blooded"),
            new CodexDefinition("evolution.grove_crown", CodexCategory.Evolution, "Grove Crown",
                "Grove Thorn Lash leaves damaging thorn patches.",
                "Max Grove Thorn Lash + Grove Seed catalyst discovered.",
                "weapon.grove_thorn_lash", "gear.grove_seed"),
            new CodexDefinition("evolution.signal_storm", CodexCategory.Evolution, "Signal Storm",
                "Signal Flares chain to two nearby enemies after impact.",
                "Max Signal Flare + Flare Core catalyst discovered.",
                "weapon.signal_flare", "gear.flare_core"),
            new CodexDefinition("relic.jotunn_echo", CodexCategory.Relic, "Jotunn Echo",
                "Trophy from a Scout victory — echoes of the fallen Jotunn."),
            new CodexDefinition("relic.jotunn_echo_warden", CodexCategory.Relic, "Jotunn Echo Warden",
                "Trophy from a Scout victory with all rune shards recovered.")
        };

        public static IReadOnlyList<UnlockDefinition> Unlocks => UnlockCatalog;

        public static IReadOnlyList<CodexDefinition> CodexEntries => CodexCatalog;

        public const float RenownKillPickupChance = 0.10f;
        public const int RenownVictoryBonus = 25;
        public const int RenownKillBonusDivisor = 15;

        public static int AvailableRenown(MetaProgress progress)
        {
            if (progress == null)
            {
                return 0;
            }

            return Math.Max(0, progress.TotalRenown - progress.SpentRenown);
        }

        public static int ResolveMastery(MetaProgress progress, string characterId)
        {
            if (progress == null || string.IsNullOrWhiteSpace(characterId))
            {
                return 0;
            }

            switch (characterId)
            {
                case HaldorId:
                    return progress.HaldorMastery;
                case SylvaId:
                    return progress.SylvaMastery;
                case MaraId:
                    return progress.MaraMastery;
                case EiraId:
                    return progress.EiraMastery;
                default:
                    return 0;
            }
        }

        public static string ResolveMasteryWeaponId(string characterId)
        {
            switch (characterId)
            {
                case HaldorId:
                    return "weapon.frost_axe";
                case EiraId:
                    return "weapon.raven_guard";
                case SylvaId:
                    return "weapon.grove_thorn_lash";
                case MaraId:
                    return "weapon.signal_flare";
                default:
                    return null;
            }
        }

        public static int CalculateMasteryGain(int kills, bool victory)
        {
            return Math.Max(1, kills / 25) + (victory ? 3 : 0);
        }

        public static int CalculateRunRenownEarned(int recoveredRenown, int kills, bool victory)
        {
            var killBonus = Math.Max(1, kills / RenownKillBonusDivisor);
            var victoryBonus = victory ? RenownVictoryBonus : 0;
            return recoveredRenown + killBonus + victoryBonus;
        }

        public static float MasteryDamageMultiplier(int mastery)
        {
            return 1f + Math.Min(0.15f, Math.Max(0, mastery) * 0.005f);
        }

        public static void ApplyMasteryToProgress(MetaProgress progress, string characterId, int kills, bool victory)
        {
            if (progress == null || string.IsNullOrWhiteSpace(characterId))
            {
                return;
            }

            var gain = CalculateMasteryGain(kills, victory);

            switch (characterId)
            {
                case HaldorId:
                    progress.HaldorMastery += gain;
                    break;
                case SylvaId:
                    progress.SylvaMastery += gain;
                    break;
                case MaraId:
                    progress.MaraMastery += gain;
                    break;
                case EiraId:
                    progress.EiraMastery += gain;
                    break;
            }
        }

        public static void EnsureStarterUnlocks(MetaProgress progress)
        {
            if (progress == null)
            {
                return;
            }

            if (progress.UnlockedContentIds == null)
            {
                progress.UnlockedContentIds = new string[0];
            }

            for (var i = 0; i < StarterUnlockIds.Length; i++)
            {
                AddUnlock(progress, StarterUnlockIds[i]);
            }
        }

        public static void MigrateToVersionFour(MetaProgress progress)
        {
            if (progress == null)
            {
                return;
            }

            EnsureStarterUnlocks(progress);

            if (progress.DiscoveredCodexIds == null)
            {
                progress.DiscoveredCodexIds = new string[0];
            }

            if (progress.RunsCompleted >= 1 || progress.TotalRenown >= 50)
            {
                AddUnlock(progress, SylvaId);
            }

            if (progress.RelicsCollected != null && progress.RelicsCollected.Length > 0)
            {
                AddUnlock(progress, EiraId);
            }

            if (progress.RunsCompleted >= 2 && progress.BestKills >= 150)
            {
                AddUnlock(progress, MaraId);
            }

            if (HasRelic(progress, "relic.jotunn_echo") || HasRelic(progress, "relic.jotunn_echo_warden"))
            {
                AddUnlock(progress, SagaMapId);
            }
        }

        public static UnlockDefinition? FindUnlock(string contentId)
        {
            for (var i = 0; i < UnlockCatalog.Length; i++)
            {
                if (UnlockCatalog[i].ContentId == contentId)
                {
                    return UnlockCatalog[i];
                }
            }

            return null;
        }

        public static CodexDefinition? FindCodexEntry(string contentId)
        {
            for (var i = 0; i < CodexCatalog.Length; i++)
            {
                if (CodexCatalog[i].ContentId == contentId)
                {
                    return CodexCatalog[i];
                }
            }

            return null;
        }

        public static bool IsUnlocked(MetaProgress progress, string contentId)
        {
            if (progress == null || string.IsNullOrWhiteSpace(contentId))
            {
                return false;
            }

            EnsureStarterUnlocks(progress);

            return ContainsUnlock(progress, contentId);
        }

        public static bool IsCharacterUnlocked(MetaProgress progress, int characterIndex)
        {
            var character = ContentCatalog.Character(characterIndex);
            return IsUnlocked(progress, character.Id);
        }

        public static bool IsMapUnlocked(MetaProgress progress, int mapIndex)
        {
            var map = ContentCatalog.Map(mapIndex);
            return IsUnlocked(progress, map.Id);
        }

        public static bool CanPurchase(MetaProgress progress, string contentId)
        {
            if (progress == null || string.IsNullOrWhiteSpace(contentId))
            {
                return false;
            }

            if (IsUnlocked(progress, contentId))
            {
                return false;
            }

            var unlock = FindUnlock(contentId);
            if (!unlock.HasValue)
            {
                return false;
            }

            return AvailableRenown(progress) >= unlock.Value.RenownCost;
        }

        public static PurchaseResult TryPurchaseUnlock(MetaProgress progress, string contentId)
        {
            var result = new PurchaseResult();

            if (progress == null)
            {
                result.Message = "No save data.";
                return result;
            }

            if (IsUnlocked(progress, contentId))
            {
                result.Message = "Already unlocked.";
                return result;
            }

            var unlock = FindUnlock(contentId);
            if (!unlock.HasValue)
            {
                result.Message = "Unknown unlock.";
                return result;
            }

            if (AvailableRenown(progress) < unlock.Value.RenownCost)
            {
                result.Message = "Insufficient renown.";
                return result;
            }

            progress.SpentRenown += unlock.Value.RenownCost;
            AddUnlock(progress, contentId);
            result.Success = true;
            result.Message = unlock.Value.DisplayName;
            return result;
        }

        public static bool DiscoverCodex(MetaProgress progress, string contentId)
        {
            if (progress == null || string.IsNullOrWhiteSpace(contentId))
            {
                return false;
            }

            if (!FindCodexEntry(contentId).HasValue)
            {
                return false;
            }

            if (progress.DiscoveredCodexIds == null)
            {
                progress.DiscoveredCodexIds = new string[0];
            }

            for (var i = 0; i < progress.DiscoveredCodexIds.Length; i++)
            {
                if (progress.DiscoveredCodexIds[i] == contentId)
                {
                    return false;
                }
            }

            var updated = new string[progress.DiscoveredCodexIds.Length + 1];
            for (var i = 0; i < progress.DiscoveredCodexIds.Length; i++)
            {
                updated[i] = progress.DiscoveredCodexIds[i];
            }

            updated[progress.DiscoveredCodexIds.Length] = contentId;
            progress.DiscoveredCodexIds = updated;
            return true;
        }

        public static bool IsCodexDiscovered(MetaProgress progress, string contentId)
        {
            if (progress?.DiscoveredCodexIds == null)
            {
                return false;
            }

            for (var i = 0; i < progress.DiscoveredCodexIds.Length; i++)
            {
                if (progress.DiscoveredCodexIds[i] == contentId)
                {
                    return true;
                }
            }

            return false;
        }

        public static CodexVisibility ResolveCodexVisibility(MetaProgress progress, CodexDefinition entry)
        {
            if (IsCodexDiscovered(progress, entry.ContentId))
            {
                return CodexVisibility.Discovered;
            }

            if (entry.Category != CodexCategory.Evolution)
            {
                return CodexVisibility.Hidden;
            }

            if (string.IsNullOrWhiteSpace(entry.EvolutionBaseId) || string.IsNullOrWhiteSpace(entry.EvolutionCatalystId))
            {
                return CodexVisibility.Hidden;
            }

            var baseDiscovered = IsCodexDiscovered(progress, entry.EvolutionBaseId);
            var catalystDiscovered = IsCodexDiscovered(progress, entry.EvolutionCatalystId);

            if (baseDiscovered && catalystDiscovered)
            {
                return CodexVisibility.Hint;
            }

            return CodexVisibility.Hidden;
        }

        public static int NextGridIndex(int itemCount, int columns, int currentIndex, int columnDelta, int rowDelta)
        {
            if (itemCount <= 1 || (columnDelta == 0 && rowDelta == 0))
            {
                return Mathf.Clamp(currentIndex, 0, Mathf.Max(0, itemCount - 1));
            }

            var rows = Mathf.Max(1, (itemCount + columns - 1) / columns);
            var column = currentIndex % columns;
            var row = currentIndex / columns;

            if (columnDelta != 0)
            {
                column += columnDelta;
                if (column >= columns)
                {
                    column = 0;
                    row++;
                }
                else if (column < 0)
                {
                    column = columns - 1;
                    row--;
                }
            }

            if (rowDelta != 0)
            {
                row += rowDelta;
            }

            row = WrapIndex(row, rows);
            var index = row * columns + column;

            if (index < itemCount)
            {
                return index;
            }

            for (var step = 0; step < rows * columns; step++)
            {
                if (columnDelta != 0)
                {
                    column += columnDelta;
                    if (column >= columns)
                    {
                        column = 0;
                        row++;
                    }
                    else if (column < 0)
                    {
                        column = columns - 1;
                        row--;
                    }
                }

                if (rowDelta != 0)
                {
                    row += rowDelta;
                }

                row = WrapIndex(row, rows);
                index = row * columns + column;
                if (index < itemCount)
                {
                    return index;
                }
            }

            return currentIndex;
        }

        public static int NextCharacterIndex(int currentIndex, int columnDelta, int rowDelta)
        {
            return NextGridIndex(
                ContentCatalog.Characters.Length,
                CharacterSelectPresentation.GridColumns,
                currentIndex,
                columnDelta,
                rowDelta);
        }

        public static int NextMapIndex(int currentIndex, int columnDelta, int rowDelta)
        {
            return NextGridIndex(
                ContentCatalog.Maps.Length,
                MapSelectPresentation.GridColumns,
                currentIndex,
                columnDelta,
                rowDelta);
        }

        public static int NextUnlockedCharacterIndex(MetaProgress progress, int currentIndex, int direction)
        {
            var count = ContentCatalog.Characters.Length;
            if (count <= 0)
            {
                return 0;
            }

            var index = currentIndex;
            for (var step = 0; step < count; step++)
            {
                index = WrapIndex(index + direction, count);
                if (IsCharacterUnlocked(progress, index))
                {
                    return index;
                }
            }

            return currentIndex;
        }

        public static int NextUnlockedMapIndex(MetaProgress progress, int currentIndex, int direction)
        {
            var count = ContentCatalog.Maps.Length;
            if (count <= 0)
            {
                return 0;
            }

            var index = currentIndex;
            for (var step = 0; step < count; step++)
            {
                index = WrapIndex(index + direction, count);
                if (IsMapUnlocked(progress, index))
                {
                    return index;
                }
            }

            return currentIndex;
        }

        public static int FirstUnlockedCharacterIndex(MetaProgress progress)
        {
            for (var i = 0; i < ContentCatalog.Characters.Length; i++)
            {
                if (IsCharacterUnlocked(progress, i))
                {
                    return i;
                }
            }

            return 0;
        }

        public static int FirstUnlockedMapIndex(MetaProgress progress)
        {
            for (var i = 0; i < ContentCatalog.Maps.Length; i++)
            {
                if (IsMapUnlocked(progress, i))
                {
                    return i;
                }
            }

            return 0;
        }

        public static string FindCheapestAffordableUnlockId(MetaProgress progress)
        {
            string bestId = null;
            var bestCost = int.MaxValue;

            for (var i = 0; i < UnlockCatalog.Length; i++)
            {
                var unlock = UnlockCatalog[i];
                if (unlock.RenownCost <= 0)
                {
                    continue;
                }

                if (IsUnlocked(progress, unlock.ContentId))
                {
                    continue;
                }

                if (AvailableRenown(progress) < unlock.RenownCost)
                {
                    continue;
                }

                if (unlock.RenownCost < bestCost)
                {
                    bestCost = unlock.RenownCost;
                    bestId = unlock.ContentId;
                }
            }

            return bestId;
        }

        public static int CountAffordableUnlocks(MetaProgress progress)
        {
            var count = 0;

            for (var i = 0; i < UnlockCatalog.Length; i++)
            {
                if (CanPurchase(progress, UnlockCatalog[i].ContentId))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool ContainsUnlock(MetaProgress progress, string contentId)
        {
            if (progress == null || string.IsNullOrWhiteSpace(contentId))
            {
                return false;
            }

            var unlocks = progress.UnlockedContentIds;
            if (unlocks == null)
            {
                return false;
            }

            for (var i = 0; i < unlocks.Length; i++)
            {
                if (unlocks[i] == contentId)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddUnlock(MetaProgress progress, string contentId)
        {
            if (string.IsNullOrWhiteSpace(contentId) || ContainsUnlock(progress, contentId))
            {
                return;
            }

            var unlocks = progress.UnlockedContentIds ?? new string[0];
            var updated = new string[unlocks.Length + 1];
            for (var i = 0; i < unlocks.Length; i++)
            {
                updated[i] = unlocks[i];
            }

            updated[unlocks.Length] = contentId;
            progress.UnlockedContentIds = updated;
        }

        private static bool HasRelic(MetaProgress progress, string relicId)
        {
            if (progress?.RelicsCollected == null)
            {
                return false;
            }

            for (var i = 0; i < progress.RelicsCollected.Length; i++)
            {
                if (progress.RelicsCollected[i] == relicId)
                {
                    return true;
                }
            }

            return false;
        }

        private static int WrapIndex(int value, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            var wrapped = value % count;
            if (wrapped < 0)
            {
                wrapped += count;
            }

            return wrapped;
        }
    }
}
