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
        public readonly string BiomeId;
        public readonly string RegularEnemyId;
        public readonly string EliteEnemyId;
        public readonly string BossEnemyId;
        public readonly string VictoryRelicStandardId;
        public readonly string VictoryRelicBonusId;
        public readonly string KillObjectiveLabel;
        public readonly string OptionalPickupLabel;
        public readonly string[] PhaseAnnouncements;
        public readonly string BossEntranceAnnouncement;
        public readonly string EliteSpawnAnnouncement;
        public readonly string LandmarkProfileId;

        public MapDefinition(
            string id, string name, string region, string description, string durationLabel,
            float duration, float bossSpawnTime, float baseSpawnInterval,
            float minimumSpawnInterval, float difficultyRamp, Color groundColor,
            int weaponSlots, int gearSlots, int requiredKillObjective, int optionalShardObjective,
            float extractionDuration, float extractionBeaconX, float extractionBeaconY,
            string lockedPreviewLine = "",
            string biomeId = BiomeCatalog.FrostboundId,
            string regularEnemyId = "enemy.draugr_raider",
            string eliteEnemyId = "enemy.frost_wraith_captain",
            string bossEnemyId = "enemy.jotunn_warlord",
            string victoryRelicStandardId = "relic.jotunn_echo",
            string victoryRelicBonusId = "relic.jotunn_echo_warden",
            string killObjectiveLabel = "DRAUGR",
            string optionalPickupLabel = "SHARDS",
            string[] phaseAnnouncements = null,
            string bossEntranceAnnouncement = null,
            string eliteSpawnAnnouncement = null,
            string landmarkProfileId = BiomeCatalog.FrostboundId)
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
            BiomeId = biomeId;
            RegularEnemyId = regularEnemyId;
            EliteEnemyId = eliteEnemyId;
            BossEnemyId = bossEnemyId;
            VictoryRelicStandardId = victoryRelicStandardId;
            VictoryRelicBonusId = victoryRelicBonusId;
            KillObjectiveLabel = killObjectiveLabel;
            OptionalPickupLabel = optionalPickupLabel;
            PhaseAnnouncements = phaseAnnouncements ?? BiomeCatalog.ResolvePhaseAnnouncements(biomeId);
            BossEntranceAnnouncement = bossEntranceAnnouncement ??
                BiomeCatalog.ResolveBossEntranceAnnouncement(biomeId);
            EliteSpawnAnnouncement = eliteSpawnAnnouncement ??
                BiomeCatalog.ResolveEliteSpawnAnnouncement(biomeId);
            LandmarkProfileId = landmarkProfileId;
        }
    }

    public static class ContentCatalog
    {
        public static CharacterDefinition[] Characters { get; private set; } = BuildDefaultCharacters();

        public static MapDefinition[] Maps { get; private set; } = BuildDefaultMaps();

        private static CharacterDefinition[] BuildDefaultCharacters()
        {
            return new[]
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
                    "oathbound.bren", "Bren Oakhart", "Oathbound Grove", "Root Binder",
                    "An oathbound rootbinder who anchors allies with living bark and slow, punishing grove magic.",
                    new Color(0.32f, 0.62f, 0.38f), 132f, 4.55f, 1.25f,
                    "Living Bulwark",
                    "Bren becomes briefly untouchable and erupts a ring of roots that slows nearby enemies.",
                    56f, 108f, 5.8f,
                    new[] { "weapon.driftwood_staff", "weapon.oath_ring" },
                    "An Oathbound rootbinder — living bark and grove magic still waiting to be revealed."),
                new CharacterDefinition(
                    "ironway.mara", "Captain Mara Voss", "Ironway Expedition Corps", "Field Captain",
                    "A modern expedition corps captain who marks targets with signal gear and keeps the squad alive in the field.",
                    new Color(0.62f, 0.68f, 0.78f), 135f, 4.65f, 1.5f,
                    "Orbital Barrage",
                    "Mara becomes briefly untouchable and calls a concentrated strike zone onto the nearest threat cluster.",
                    58f, 132f, 6.5f,
                    new[] { "weapon.signal_flare", "weapon.supply_pulse" },
                    "An Ironway field captain — battlefield tactics and squad support still to be discovered."),
                new CharacterDefinition(
                    "ironway.rex", "Rex Calder", "Ironway Expedition Corps", "Breacher",
                    "An Ironway demolitions specialist who breaks enemy lines with beacons, charges and heavy knockback.",
                    new Color(0.72f, 0.58f, 0.42f), 142f, 4.40f, 2f,
                    "Breaching Charge",
                    "Rex becomes briefly untouchable and fires a breaching line through the nearest threat cluster.",
                    55f, 128f, 5.5f,
                    new[] { "weapon.iron_beacon", "weapon.tide_caller" },
                    "An Ironway breacher — demolitions and frontline pressure still to be discovered.")
            };
        }

        private static MapDefinition[] BuildDefaultMaps()
        {
            return new[]
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
                    "The full Frostbound Shore route — denser phases and a late Jotunn confrontation still await unlock."),
                new MapDefinition(
                    "oathbound.scout", "The Verdant Canopy", "Oathbound Canopy",
                    "A shorter grove route through living canopy, thorn spirits and a Heartwood guardian.",
                    "SCOUT EXPEDITION — 5 MIN", 300f, 240f, 0.88f, 0.24f, 44f,
                    new Color(0.06f, 0.12f, 0.08f), 4, 4, 140, 5, 15f, 0f, 14f,
                    lockedPreviewLine: "The Verdant Canopy — thorn spirits and a Heartwood guardian still await unlock.",
                    biomeId: BiomeCatalog.CanopyId,
                    regularEnemyId: "enemy.bramble_stalker",
                    eliteEnemyId: "enemy.canopy_warden",
                    bossEnemyId: "enemy.heartwood_colossus",
                    victoryRelicStandardId: "relic.heartwood_echo",
                    victoryRelicBonusId: "relic.heartwood_echo_warden",
                    killObjectiveLabel: "STALKERS",
                    optionalPickupLabel: "SAP"),
                new MapDefinition(
                    "oathbound.saga", "The Verdant Canopy: Deep Root", "Oathbound Canopy",
                    "The full canopy route with denser root paths, stronger wardens and a late Heartwood confrontation.",
                    "QUICK EXPEDITION — 12 MIN", 720f, 630f, 0.84f, 0.18f, 40f,
                    new Color(0.045f, 0.095f, 0.065f), 6, 6, 170, 5, 15f, 0f, 14f,
                    lockedPreviewLine: "The full Verdant Canopy route — denser phases still await unlock.",
                    biomeId: BiomeCatalog.CanopyId,
                    regularEnemyId: "enemy.bramble_stalker",
                    eliteEnemyId: "enemy.canopy_warden",
                    bossEnemyId: "enemy.heartwood_colossus",
                    victoryRelicStandardId: "relic.heartwood_echo",
                    victoryRelicBonusId: "relic.heartwood_echo_warden",
                    killObjectiveLabel: "STALKERS",
                    optionalPickupLabel: "SAP"),
                new MapDefinition(
                    "ironway.scout", "The Scorched Relay", "Ironway Relay Front",
                    "A shorter relay assault through scrap drones, signal raiders and a siege automaton.",
                    "SCOUT EXPEDITION — 5 MIN", 300f, 240f, 0.84f, 0.24f, 48f,
                    new Color(0.11f, 0.10f, 0.09f), 4, 4, 155, 4, 15f, 0f, 14f,
                    lockedPreviewLine: "The Scorched Relay — scrap drones and a siege automaton still await unlock.",
                    biomeId: BiomeCatalog.RelayId,
                    regularEnemyId: "enemy.scrap_drone",
                    eliteEnemyId: "enemy.signal_raider",
                    bossEnemyId: "enemy.siege_automaton",
                    victoryRelicStandardId: "relic.siege_echo",
                    victoryRelicBonusId: "relic.siege_echo_warden",
                    killObjectiveLabel: "DRONES",
                    optionalPickupLabel: "CRATES"),
                new MapDefinition(
                    "ironway.saga", "The Scorched Relay: Siege Line", "Ironway Relay Front",
                    "The full relay route with denser signal pressure, stronger raiders and a late siege confrontation.",
                    "QUICK EXPEDITION — 12 MIN", 720f, 630f, 0.80f, 0.18f, 44f,
                    new Color(0.085f, 0.075f, 0.07f), 6, 6, 185, 4, 15f, 0f, 14f,
                    lockedPreviewLine: "The full Scorched Relay route — denser siege pressure still awaits unlock.",
                    biomeId: BiomeCatalog.RelayId,
                    regularEnemyId: "enemy.scrap_drone",
                    eliteEnemyId: "enemy.signal_raider",
                    bossEnemyId: "enemy.siege_automaton",
                    victoryRelicStandardId: "relic.siege_echo",
                    victoryRelicBonusId: "relic.siege_echo_warden",
                    killObjectiveLabel: "DRONES",
                    optionalPickupLabel: "CRATES")
            };
        }

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

        public static MapDefinition FindMap(string mapId)
        {
            if (string.IsNullOrWhiteSpace(mapId))
            {
                return null;
            }

            for (var i = 0; i < Maps.Length; i++)
            {
                if (Maps[i].Id == mapId)
                {
                    return Maps[i];
                }
            }

            return null;
        }

        public static void Apply(ProductionContentDatabase database)
        {
            if (database == null)
            {
                return;
            }

            if (database.characters != null && database.characters.Length > 0)
            {
                var definitions = new System.Collections.Generic.List<CharacterDefinition>(database.characters.Length);

                for (var i = 0; i < database.characters.Length; i++)
                {
                    if (database.characters[i] != null && !string.IsNullOrWhiteSpace(database.characters[i].id))
                    {
                        definitions.Add(database.characters[i].Build());
                    }
                }

                if (definitions.Count > 0)
                {
                    Characters = definitions.ToArray();
                }
            }

            if (database.maps != null && database.maps.Length > 0)
            {
                var definitions = new System.Collections.Generic.List<MapDefinition>(database.maps.Length);

                for (var i = 0; i < database.maps.Length; i++)
                {
                    if (database.maps[i] != null && !string.IsNullOrWhiteSpace(database.maps[i].id))
                    {
                        definitions.Add(database.maps[i].Build());
                    }
                }

                if (definitions.Count > 0)
                {
                    Maps = definitions.ToArray();
                }
            }
        }
    }

    public static class BalanceRules
    {
        public const float PlayerCollisionRadius = 0.38f;
        public const int RegularEnemyLevelOffset = 1;
        public const int EliteEnemyLevelOffset = 2;
        public const int BossEnemyLevelOffset = 3;
        public const float ExperiencePerEnemyLevel = 0.12f;
        public const float EliteExperienceMultiplier = 1.35f;

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

        public static int ComputeEnemyLevel(int playerLevel, bool boss, bool elite)
        {
            var safePlayerLevel = Mathf.Max(1, playerLevel);

            if (boss)
            {
                return safePlayerLevel + BossEnemyLevelOffset;
            }

            if (elite)
            {
                return safePlayerLevel + EliteEnemyLevelOffset;
            }

            return safePlayerLevel + RegularEnemyLevelOffset;
        }

        public static float LevelToDifficulty(int enemyLevel) => Mathf.Max(1f, enemyLevel);

        public static float ResolveEffectiveDifficulty(float timeDifficulty, int enemyLevel)
        {
            return Mathf.Max(timeDifficulty, LevelToDifficulty(enemyLevel));
        }

        public static int ExperienceForEnemy(int baseExperienceRoll, int enemyLevel, bool elite, bool boss)
        {
            var safeLevel = Mathf.Max(1, enemyLevel);
            var multiplier = 1f + (safeLevel - 1) * ExperiencePerEnemyLevel;

            if (elite)
            {
                multiplier *= EliteExperienceMultiplier;
            }

            if (boss)
            {
                multiplier *= 1f;
            }

            return Mathf.Max(1, Mathf.RoundToInt(baseExperienceRoll * multiplier));
        }
    }
}
