using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    [Serializable]
    public sealed class CharacterContentRecord
    {
        public string id;
        public string displayName;
        public string tribe;
        public string role;
        [TextArea] public string description;
        public Color color = Color.white;
        public float maxHealth = 120f;
        public float moveSpeed = 4.5f;
        public float armor = 1f;
        public string ultimateName;
        [TextArea] public string ultimateDescription;
        public float ultimateCooldown = 60f;
        public float ultimateDamage = 120f;
        public float ultimateRadius = 6f;
        public string[] starterWeaponIds = new[] { "weapon.frost_axe", "weapon.raven_guard" };

        public CharacterDefinition Build() => new CharacterDefinition(
            id, displayName, tribe, role, description, color, maxHealth, moveSpeed, armor,
            ultimateName, ultimateDescription, ultimateCooldown, ultimateDamage, ultimateRadius,
            starterWeaponIds);
    }

    [Serializable]
    public sealed class MapContentRecord
    {
        public string id;
        public string displayName;
        public string region;
        [TextArea] public string description;
        public string durationLabel;
        public float duration = 300f;
        public float bossSpawnTime = 240f;
        public float baseSpawnInterval = 0.86f;
        public float minimumSpawnInterval = 0.24f;
        public float difficultyRamp = 46f;
        public Color groundColor = new Color(0.075f, 0.13f, 0.16f);
        public int weaponSlots = 4;
        public int gearSlots = 4;
        public int requiredKillObjective = 80;
        public int optionalShardObjective = 5;
        public float extractionDuration = 15f;
        public float extractionBeaconX;
        public float extractionBeaconY = 14f;

        public MapDefinition Build() => new MapDefinition(
            id, displayName, region, description, durationLabel, duration, bossSpawnTime,
            baseSpawnInterval, minimumSpawnInterval, difficultyRamp, groundColor, weaponSlots, gearSlots,
            requiredKillObjective, optionalShardObjective, extractionDuration, extractionBeaconX,
            extractionBeaconY);
    }

    [Serializable]
    public sealed class ItemContentRecord
    {
        public string id;
        public string displayName;
        public string shortName;
        [TextArea] public string description;
        public ItemCategory category;
        public int maxLevel = 1;
        public Color color = Color.white;
        public UpgradeId[] levelEffects = new UpgradeId[0];
        public string evolutionOf;
        public string catalystId;

        public ItemDefinition Build() => new ItemDefinition(
            id, displayName, shortName, description, category, maxLevel, color,
            levelEffects, evolutionOf, catalystId);
    }

    [Serializable]
    public sealed class EnemyContentRecord
    {
        public string id;
        public string displayName;
        public bool boss;
        public float baseHealth;
        public float healthPerDifficulty;
        public float minimumSpeed;
        public float maximumSpeed;
        public float speedPerDifficulty;
        public float baseContactDamage;
        public float contactDamagePerDifficulty;
        public float minimumRadius;
        public float maximumRadius;
        public int minimumExperience;
        public int maximumExperienceExclusive;
        public Color primaryColor = Color.white;
        public Color alternateColor = Color.white;

        public EnemyDefinition Build() => new EnemyDefinition(
            id, displayName, boss, baseHealth, healthPerDifficulty, minimumSpeed, maximumSpeed,
            speedPerDifficulty, baseContactDamage, contactDamagePerDifficulty, minimumRadius,
            maximumRadius, minimumExperience, maximumExperienceExclusive, primaryColor, alternateColor);
    }

    public sealed class EnemyDefinition
    {
        public readonly string Id;
        public readonly string Name;
        public readonly bool Boss;
        public readonly float BaseHealth;
        public readonly float HealthPerDifficulty;
        public readonly float MinimumSpeed;
        public readonly float MaximumSpeed;
        public readonly float SpeedPerDifficulty;
        public readonly float BaseContactDamage;
        public readonly float ContactDamagePerDifficulty;
        public readonly float MinimumRadius;
        public readonly float MaximumRadius;
        public readonly int MinimumExperience;
        public readonly int MaximumExperienceExclusive;
        public readonly Color PrimaryColor;
        public readonly Color AlternateColor;

        public EnemyDefinition(
            string id, string name, bool boss, float baseHealth, float healthPerDifficulty,
            float minimumSpeed, float maximumSpeed, float speedPerDifficulty,
            float baseContactDamage, float contactDamagePerDifficulty,
            float minimumRadius, float maximumRadius, int minimumExperience,
            int maximumExperienceExclusive, Color primaryColor, Color alternateColor)
        {
            Id = id;
            Name = name;
            Boss = boss;
            BaseHealth = baseHealth;
            HealthPerDifficulty = healthPerDifficulty;
            MinimumSpeed = minimumSpeed;
            MaximumSpeed = maximumSpeed;
            SpeedPerDifficulty = speedPerDifficulty;
            BaseContactDamage = baseContactDamage;
            ContactDamagePerDifficulty = contactDamagePerDifficulty;
            MinimumRadius = minimumRadius;
            MaximumRadius = maximumRadius;
            MinimumExperience = minimumExperience;
            MaximumExperienceExclusive = maximumExperienceExclusive;
            PrimaryColor = primaryColor;
            AlternateColor = alternateColor;
        }
    }

    public static class EnemyCatalog
    {
        public static EnemyDefinition Draugr { get; private set; } = new EnemyDefinition(
            "enemy.draugr_raider", "Draugr Raider", false, 20f, 5.5f, 1.25f, 2.1f, 0.025f,
            9f, 0.6f, 0.28f, 0.43f, 2, 5,
            new Color(0.35f, 0.46f, 0.39f), new Color(0.43f, 0.35f, 0.47f));

        public static EnemyDefinition FrostWraithCaptain { get; private set; } = new EnemyDefinition(
            "enemy.frost_wraith_captain", "Frost Wraith Captain", false, 95f, 18f, 1.5f, 2.0f, 0.02f,
            14f, 1.0f, 0.38f, 0.52f, 8, 14,
            new Color(0.55f, 0.82f, 0.95f), new Color(0.35f, 0.65f, 0.88f));

        public static EnemyDefinition Jotunn { get; private set; } = new EnemyDefinition(
            "enemy.jotunn_warlord", "Jotunn Warlord", true, 620f, 80f, 1.35f, 1.35f, 0f,
            22f, 0f, 0.85f, 0.85f, 60, 61,
            new Color(0.62f, 0.18f, 0.24f), new Color(0.62f, 0.18f, 0.24f));

        public static EnemyDefinition[] All { get; private set; } = new EnemyDefinition[0];

        static EnemyCatalog()
        {
            All = new[] { Draugr, FrostWraithCaptain, Jotunn };
        }

        public static void Apply(ProductionContentDatabase database)
        {
            if (database == null || database.enemies == null || database.enemies.Length < 2) return;
            var definitions = BuildValid(database.enemies);
            if (definitions.Count < 2) return;
            var draugr = Find(definitions, "enemy.draugr_raider");
            var jotunn = Find(definitions, "enemy.jotunn_warlord");
            if (draugr == null || jotunn == null) return;
            Draugr = draugr;
            Jotunn = jotunn;
            var frostWraith = Find(definitions, "enemy.frost_wraith_captain");
            if (frostWraith != null)
                FrostWraithCaptain = frostWraith;

            All = definitions.ToArray();
        }

        private static List<EnemyDefinition> BuildValid(EnemyContentRecord[] records)
        {
            var result = new List<EnemyDefinition>(records.Length);
            for (var i = 0; i < records.Length; i++)
                if (records[i] != null && !string.IsNullOrWhiteSpace(records[i].id)) result.Add(records[i].Build());
            return result;
        }

        private static EnemyDefinition Find(List<EnemyDefinition> definitions, string id)
        {
            for (var i = 0; i < definitions.Count; i++) if (definitions[i].Id == id) return definitions[i];
            return null;
        }
    }

    public static class ProductionContentRuntime
    {
        public static bool LoadedFromAssets { get; private set; }
        public static string SourceLabel => LoadedFromAssets ? "SCRIPTABLEOBJECT" : "CODE FALLBACK";

        public static void Load()
        {
            var database = Resources.Load<ProductionContentDatabase>("Content/ProductionContent");
            if (database == null)
            {
                LoadedFromAssets = false;
                return;
            }
            if (!ValidateDatabase(database, out var error))
            {
                LoadedFromAssets = false;
                Debug.LogError($"Production content asset rejected; using code fallback. {error}");
                return;
            }
            ContentCatalog.Apply(database);
            ItemCatalog.Apply(database);
            EnemyCatalog.Apply(database);
            LoadedFromAssets = true;
        }

        private static bool ValidateDatabase(ProductionContentDatabase database, out string report)
        {
            if (database.characters == null || database.characters.Length == 0 ||
                database.maps == null || database.maps.Length == 0 ||
                database.items == null || database.items.Length == 0 ||
                database.enemies == null || database.enemies.Length == 0)
            {
                report = "One or more content sections are empty.";
                return false;
            }

            var ids = new HashSet<string>();
            var itemIds = new HashSet<string>();
            for (var i = 0; i < database.characters.Length; i++)
            {
                if (database.characters[i] == null) { report = "Character record is null."; return false; }
                if (!AddId(ids, database.characters[i].id, out report)) return false;
            }
            for (var i = 0; i < database.maps.Length; i++)
            {
                if (database.maps[i] == null) { report = "Map record is null."; return false; }
                if (!AddId(ids, database.maps[i].id, out report)) return false;
            }
            for (var i = 0; i < database.items.Length; i++)
            {
                var item = database.items[i];
                if (item == null) { report = "Item record is null."; return false; }
                if (!AddId(ids, item.id, out report)) return false;
                itemIds.Add(item.id);
            }
            for (var i = 0; i < database.enemies.Length; i++)
            {
                if (database.enemies[i] == null) { report = "Enemy record is null."; return false; }
                if (!AddId(ids, database.enemies[i].id, out report)) return false;
            }

            for (var i = 0; i < database.items.Length; i++)
            {
                var item = database.items[i];
                if (item.category != ItemCategory.Evolution) continue;
                if (itemIds.Contains(item.evolutionOf) && itemIds.Contains(item.catalystId)) continue;
                report = $"Evolution {item.id} references missing recipe IDs.";
                return false;
            }
            report = string.Empty;
            return true;
        }

        public static bool Validate(out string report)
        {
            var ids = new HashSet<string>();
            for (var i = 0; i < ContentCatalog.Characters.Length; i++)
                if (!AddId(ids, ContentCatalog.Characters[i]?.Id, out report)) return false;
            for (var i = 0; i < ContentCatalog.Maps.Length; i++)
                if (!AddId(ids, ContentCatalog.Maps[i]?.Id, out report)) return false;
            for (var i = 0; i < ItemCatalog.All.Length; i++)
                if (!AddId(ids, ItemCatalog.All[i]?.Id, out report)) return false;
            for (var i = 0; i < EnemyCatalog.All.Length; i++)
                if (!AddId(ids, EnemyCatalog.All[i]?.Id, out report)) return false;

            for (var i = 0; i < ItemCatalog.All.Length; i++)
            {
                var item = ItemCatalog.All[i];
                if (!item.IsEvolution) continue;
                if (ItemCatalog.Find(item.EvolutionOf) != null && ItemCatalog.Find(item.CatalystId) != null) continue;
                report = $"Evolution {item.Id} has an invalid recipe.";
                return false;
            }
            report = $"{ContentCatalog.Characters.Length} characters, {ContentCatalog.Maps.Length} maps, {ItemCatalog.All.Length} items and {EnemyCatalog.All.Length} enemies.";
            return true;
        }

        private static bool AddId(HashSet<string> ids, string id, out string report)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                report = "Content contains an empty stable ID.";
                return false;
            }
            if (!ids.Add(id))
            {
                report = $"Duplicate stable ID: {id}.";
                return false;
            }
            report = string.Empty;
            return true;
        }
    }
}
