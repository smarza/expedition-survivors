using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ProjectExpedition.Editor
{
    public static class ProductionContentValidator
    {
        [MenuItem("Expedition/Validate Production Content")]
        public static void ValidateFromMenu()
        {
            var database = Resources.Load<ProductionContentDatabase>("Content/ProductionContent");

            if (database == null)
            {
                Debug.LogError("Production content asset not found at Resources/Content/ProductionContent.");
                return;
            }

            if (TryValidate(database, out var report))
            {
                Debug.Log($"Production content validation passed. {report}");
            }
            else
            {
                Debug.LogError($"Production content validation failed.\n{report}");
            }
        }

        public static bool TryValidate(ProductionContentDatabase database, out string report)
        {
            var messages = new StringBuilder();

            if (!ValidateRecordSections(database, messages))
            {
                report = messages.ToString();
                return false;
            }

            if (!ValidateStableIds(database, messages))
            {
                report = messages.ToString();
                return false;
            }

            if (!ValidateMaps(database, messages))
            {
                report = messages.ToString();
                return false;
            }

            if (!ValidateEvolutionRecipes(database, messages))
            {
                report = messages.ToString();
                return false;
            }

            if (!ValidateWeaponProfiles(database, messages))
            {
                report = messages.ToString();
                return false;
            }

            if (!ValidateLootDefinitions(messages))
            {
                report = messages.ToString();
                return false;
            }

            report =
                $"{database.characters.Length} characters, {database.maps.Length} maps, " +
                $"{database.items.Length} items, {database.enemies.Length} enemies, " +
                $"1 loot effect.";
            return true;
        }

        private static bool ValidateLootDefinitions(StringBuilder messages)
        {
            var healing = LootEffectCatalog.HealingEmbers;

            if (healing == null || string.IsNullOrWhiteSpace(healing.Id))
            {
                messages.AppendLine("Default loot effect is missing a stable id.");
                return false;
            }

            if (healing.RequiredCount <= 0 || healing.EffectDuration <= 0f || healing.EffectIntensity <= 0f)
            {
                messages.AppendLine("Default loot effect has invalid activation or duration values.");
                return false;
            }

            if (healing.MinimumDropChance <= 0f || healing.BaseDropChance < healing.MinimumDropChance)
            {
                messages.AppendLine("Default loot effect has invalid drop chance bounds.");
                return false;
            }

            return true;
        }

        private static bool ValidateRecordSections(ProductionContentDatabase database, StringBuilder messages)
        {
            if (database.characters == null || database.characters.Length == 0)
            {
                messages.AppendLine("Characters section is empty.");
                return false;
            }

            if (database.maps == null || database.maps.Length == 0)
            {
                messages.AppendLine("Maps section is empty.");
                return false;
            }

            if (database.items == null || database.items.Length == 0)
            {
                messages.AppendLine("Items section is empty.");
                return false;
            }

            if (database.enemies == null || database.enemies.Length == 0)
            {
                messages.AppendLine("Enemies section is empty.");
                return false;
            }

            return true;
        }

        private static bool ValidateStableIds(ProductionContentDatabase database, StringBuilder messages)
        {
            var ids = new HashSet<string>();

            if (!ValidateIdList(database.characters, "character", ids, messages))
            {
                return false;
            }

            if (!ValidateIdList(database.maps, "map", ids, messages))
            {
                return false;
            }

            if (!ValidateIdList(database.items, "item", ids, messages))
            {
                return false;
            }

            if (!ValidateIdList(database.enemies, "enemy", ids, messages))
            {
                return false;
            }

            return true;
        }

        private static bool ValidateIdList<T>(T[] records, string label, HashSet<string> ids, StringBuilder messages)
            where T : class
        {
            for (var i = 0; i < records.Length; i++)
            {
                var record = records[i];
                if (record == null)
                {
                    messages.AppendLine($"Null {label} record at index {i}.");
                    return false;
                }

                var id = ReadId(record);
                if (string.IsNullOrWhiteSpace(id))
                {
                    messages.AppendLine($"{label} record at index {i} has an empty stable ID.");
                    return false;
                }

                if (!ids.Add(id))
                {
                    messages.AppendLine($"Duplicate stable ID: {id}.");
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateMaps(ProductionContentDatabase database, StringBuilder messages)
        {
            var enemyIds = BuildEnemyIdSet(database.enemies);

            for (var i = 0; i < database.maps.Length; i++)
            {
                var map = database.maps[i];
                if (map == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(map.biomeId))
                {
                    messages.AppendLine($"Map {map.id} is missing biomeId.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(map.regularEnemyId)
                    || string.IsNullOrWhiteSpace(map.eliteEnemyId)
                    || string.IsNullOrWhiteSpace(map.bossEnemyId))
                {
                    messages.AppendLine($"Map {map.id} is missing enemy ID fields.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(map.victoryRelicStandardId)
                    || string.IsNullOrWhiteSpace(map.victoryRelicBonusId))
                {
                    messages.AppendLine($"Map {map.id} is missing victory relic fields.");
                    return false;
                }

                if (!enemyIds.Contains(map.regularEnemyId)
                    || !enemyIds.Contains(map.eliteEnemyId)
                    || !enemyIds.Contains(map.bossEnemyId))
                {
                    messages.AppendLine($"Map {map.id} references an enemy ID that is not authored.");
                    return false;
                }

                if (map.requiredKillObjective <= 0 || map.extractionDuration <= 0f)
                {
                    messages.AppendLine($"Map {map.id} has invalid objective or extraction duration.");
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateEvolutionRecipes(ProductionContentDatabase database, StringBuilder messages)
        {
            var itemIds = BuildItemIdSet(database.items);

            for (var i = 0; i < database.items.Length; i++)
            {
                var item = database.items[i];
                if (item == null || item.category != ItemCategory.Evolution)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.evolutionOf) || string.IsNullOrWhiteSpace(item.catalystId))
                {
                    messages.AppendLine($"Evolution {item.id} is missing recipe IDs.");
                    return false;
                }

                if (!itemIds.Contains(item.evolutionOf) || !itemIds.Contains(item.catalystId))
                {
                    messages.AppendLine($"Evolution {item.id} references missing base or catalyst IDs.");
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateWeaponProfiles(ProductionContentDatabase database, StringBuilder messages)
        {
            for (var i = 0; i < database.items.Length; i++)
            {
                var item = database.items[i];
                if (item == null || item.category != ItemCategory.Weapon)
                {
                    continue;
                }

                if (!WeaponProfile.TryGet(item.id, out _))
                {
                    messages.AppendLine($"Weapon {item.id} has no SharedWeaponRegistry profile.");
                    return false;
                }

                if (item.levelEffects == null || item.levelEffects.Length != item.maxLevel)
                {
                    messages.AppendLine($"Weapon {item.id} levelEffects length must match maxLevel.");
                    return false;
                }
            }

            return true;
        }

        private static HashSet<string> BuildItemIdSet(ItemContentRecord[] items)
        {
            var itemIds = new HashSet<string>();

            for (var i = 0; i < items.Length; i++)
            {
                if (items[i] != null && !string.IsNullOrWhiteSpace(items[i].id))
                {
                    itemIds.Add(items[i].id);
                }
            }

            return itemIds;
        }

        private static HashSet<string> BuildEnemyIdSet(EnemyContentRecord[] enemies)
        {
            var enemyIds = new HashSet<string>();

            for (var i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null && !string.IsNullOrWhiteSpace(enemies[i].id))
                {
                    enemyIds.Add(enemies[i].id);
                }
            }

            return enemyIds;
        }

        private static string ReadId(object record)
        {
            switch (record)
            {
                case CharacterContentRecord character:
                    return character.id;
                case MapContentRecord map:
                    return map.id;
                case ItemContentRecord item:
                    return item.id;
                case EnemyContentRecord enemy:
                    return enemy.id;
                default:
                    return string.Empty;
            }
        }
    }
}
