using UnityEngine;

namespace ProjectExpedition
{
    public sealed class CharacterDefinition
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Tribe;
        public readonly string Role;
        public readonly string Description;
        public readonly string LockedPreviewLine;
        public readonly Color Color;
        public readonly float MaxHealth;
        public readonly float MoveSpeed;
        public readonly float Armor;
        public readonly string UltimateName;
        public readonly string UltimateDescription;
        public readonly float UltimateCooldown;
        public readonly float UltimateDamage;
        public readonly float UltimateRadius;
        public readonly string[] StarterWeaponIds;

        public CharacterDefinition(
            string id, string name, string tribe, string role, string description, Color color,
            float maxHealth, float moveSpeed, float armor, string ultimateName,
            string ultimateDescription, float ultimateCooldown, float ultimateDamage, float ultimateRadius,
            string[] starterWeaponIds = null, string lockedPreviewLine = "")
        {
            Id = id;
            Name = name;
            Tribe = tribe;
            Role = role;
            Description = description;
            LockedPreviewLine = lockedPreviewLine ?? string.Empty;
            Color = color;
            MaxHealth = maxHealth;
            MoveSpeed = moveSpeed;
            Armor = armor;
            UltimateName = ultimateName;
            UltimateDescription = ultimateDescription;
            UltimateCooldown = ultimateCooldown;
            UltimateDamage = ultimateDamage;
            UltimateRadius = ultimateRadius;
            StarterWeaponIds = starterWeaponIds ?? new[] { "weapon.frost_axe", "weapon.raven_guard" };
        }
    }

    public sealed class MapDefinition
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Region;
        public readonly string Description;
        public readonly string LockedPreviewLine;
        public readonly string DurationLabel;
        public readonly float Duration;
        public readonly float BossSpawnTime;
        public readonly float BaseSpawnInterval;
        public readonly float MinimumSpawnInterval;
        public readonly float DifficultyRamp;
        public readonly Color GroundColor;
        public readonly int WeaponSlots;
        public readonly int GearSlots;
        public readonly int RequiredKillObjective;
        public readonly int OptionalShardObjective;
        public readonly float ExtractionDuration;
        public readonly float ExtractionBeaconX;
        public readonly float ExtractionBeaconY;

        public MapDefinition(
            string id, string name, string region, string description, string durationLabel,
            float duration, float bossSpawnTime, float baseSpawnInterval,
            float minimumSpawnInterval, float difficultyRamp, Color groundColor,
            int weaponSlots, int gearSlots, int requiredKillObjective, int optionalShardObjective,
            float extractionDuration, float extractionBeaconX, float extractionBeaconY,
            string lockedPreviewLine = "")
        {
            Id = id;
            Name = name;
            Region = region;
            Description = description;
            LockedPreviewLine = lockedPreviewLine ?? string.Empty;
            DurationLabel = durationLabel;
            Duration = duration;
            BossSpawnTime = bossSpawnTime;
            BaseSpawnInterval = baseSpawnInterval;
            MinimumSpawnInterval = minimumSpawnInterval;
            DifficultyRamp = difficultyRamp;
            GroundColor = groundColor;
            WeaponSlots = weaponSlots;
            GearSlots = gearSlots;
            RequiredKillObjective = requiredKillObjective;
            OptionalShardObjective = optionalShardObjective;
            ExtractionDuration = extractionDuration;
            ExtractionBeaconX = extractionBeaconX;
            ExtractionBeaconY = extractionBeaconY;
        }
    }

    public static class ContentCatalog
    {
        public static CharacterDefinition[] Characters { get; private set; } = new[]
        {
            new CharacterDefinition(
                "ravenbound.haldor", "Haldor Stormborn", "Ravenbound Vikings", "Expedition Leader",
                "A relentless Viking shield-bearer who turns every fallen enemy into another verse of his saga.",
                new Color(0.29f, 0.65f, 0.82f), 150f, 4.45f, 2f,
                "Ravenstorm",
                "Haldor becomes briefly untouchable and calls a devastating storm of frost axes and ravens around him.",
                60f, 145f, 6.8f,
                new[] { "weapon.frost_axe", "weapon.raven_guard" }),
            new CharacterDefinition(
                "ravenbound.eira", "Eira Raven-Sworn", "Ravenbound Vikings", "Storm Scout",
                "A swift pathfinder whose ravens mark openings before the enemy realizes it has been surrounded.",
                new Color(0.88f, 0.52f, 0.24f), 122f, 5.05f, 0.5f,
                "Murder of Ravens",
                "Eira releases a focused murder of ravens that tears through every nearby enemy.",
                52f, 112f, 5.8f,
                new[] { "weapon.frost_axe", "weapon.raven_guard" },
                "A Ravenbound pathfinder whose ravens find openings before the enemy realizes it has been surrounded."),
            new CharacterDefinition(
                "oathbound.sylva", "Sylva Reedwalker", "Oathbound Grove", "Canopy Warden",
                "An oathbound grove warden who binds thorn and canopy magic — fictional woodland culture, not any real people.",
                new Color(0.38f, 0.78f, 0.42f), 128f, 4.85f, 1f,
                "Verdant Tempest",
                "Sylva becomes briefly untouchable and unleashes an expanding thorn ring through nearby enemies.",
                54f, 118f, 6.2f,
                new[] { "weapon.grove_thorn_lash", "weapon.canopy_vortex" },
                "An Oathbound grove warden — thorn and canopy magic still waiting to be revealed."),
            new CharacterDefinition(
                "ironway.mara", "Captain Mara Voss", "Ironway Expedition Corps", "Field Captain",
                "A modern expedition corps captain who marks targets with signal gear and keeps the squad alive in the field.",
                new Color(0.62f, 0.68f, 0.78f), 135f, 4.65f, 1.5f,
                "Orbital Barrage",
                "Mara becomes briefly untouchable and calls a concentrated strike zone onto the nearest threat cluster.",
                58f, 132f, 6.5f,
                new[] { "weapon.signal_flare", "weapon.supply_pulse" },
                "An Ironway field captain — battlefield tactics and squad support still to be discovered.")
        };

        public static MapDefinition[] Maps { get; private set; } = new[]
        {
            new MapDefinition(
                "frostbound.scout", "The Frostbound Shore", "Jotunn Coast",
                "A shorter expedition used to learn the shore, complete a build and confront its Jotunn guardian.",
                "SCOUT EXPEDITION — 5 MIN", 300f, 240f, 0.86f, 0.24f, 46f,
                new Color(0.075f, 0.13f, 0.16f), 4, 4, 150, 5, 15f, 0f, 14f),
            new MapDefinition(
                "frostbound.saga", "The Frostbound Shore: Long Night", "Jotunn Coast",
                "The full twelve-minute route. Denser phases, stronger elites and a late Jotunn confrontation.",
                "QUICK EXPEDITION — 12 MIN", 720f, 630f, 0.82f, 0.18f, 42f,
                new Color(0.055f, 0.105f, 0.145f), 6, 6, 180, 5, 15f, 0f, 14f,
                "The full Frostbound Shore route — denser phases and a late Jotunn confrontation still await unlock.")
        };

        public static CharacterDefinition Character(int index) =>
            Characters[Mathf.Clamp(index, 0, Characters.Length - 1)];

        public static CharacterDefinition FindCharacter(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return null;
            }

            for (var i = 0; i < Characters.Length; i++)
            {
                if (Characters[i].Id == characterId)
                {
                    return Characters[i];
                }
            }

            return null;
        }

        public static int CharacterIndex(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return -1;
            }

            for (var i = 0; i < Characters.Length; i++)
            {
                if (Characters[i].Id == characterId)
                {
                    return i;
                }
            }

            return -1;
        }

        public static MapDefinition Map(int index) =>
            Maps[Mathf.Clamp(index, 0, Maps.Length - 1)];

        public static void Apply(ProductionContentDatabase database)
        {
            if (database == null) return;
            if (database.characters != null && database.characters.Length > 0)
            {
                var definitions = new System.Collections.Generic.List<CharacterDefinition>(database.characters.Length);
                for (var i = 0; i < database.characters.Length; i++)
                    if (database.characters[i] != null && !string.IsNullOrWhiteSpace(database.characters[i].id))
                        definitions.Add(database.characters[i].Build());
                if (definitions.Count > 0) Characters = definitions.ToArray();
            }
            if (database.maps != null && database.maps.Length > 0)
            {
                var definitions = new System.Collections.Generic.List<MapDefinition>(database.maps.Length);
                for (var i = 0; i < database.maps.Length; i++)
                    if (database.maps[i] != null && !string.IsNullOrWhiteSpace(database.maps[i].id))
                        definitions.Add(database.maps[i].Build());
                if (definitions.Count > 0) Maps = definitions.ToArray();
            }
        }
    }

    public static class BalanceRules
    {
        public static int ExperienceToNext(int currentLevel, int playerCount)
        {
            var level = Mathf.Max(1, currentLevel);
            var soloRequirement = 48f + level * 8f + Mathf.Pow(level, 1.35f) * 2f;
            var partyMultiplier = playerCount > 1 ? 1.35f : 1f;
            return Mathf.RoundToInt(soloRequirement * partyMultiplier);
        }

        public static float UltimateCooldown(float baseCooldown, int cooldownUpgrades)
        {
            var multiplier = Mathf.Pow(0.9f, Mathf.Max(0, cooldownUpgrades));
            return Mathf.Max(28f, baseCooldown * multiplier);
        }
    }
}
