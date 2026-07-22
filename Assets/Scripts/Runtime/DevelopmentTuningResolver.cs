using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public static class DevelopmentTuningResolver
    {
        private static DevelopmentTuningProfileData Profile => DevelopmentTuningService.Active;

        public static LootEffectDefinition ResolveLootDefinition()
        {
            return ResolveLootDefinition(LootEffectCatalog.HealingEmbers);
        }

        public static LootEffectDefinition ResolveLootDefinition(LootEffectDefinition baseDefinition)
        {
            if (baseDefinition == null)
            {
                return ResolveLootDefinition();
            }

            var profile = Profile;
            var overrideEntry = FindLootOverride(baseDefinition.Id);
            var effectDuration = overrideEntry != null && overrideEntry.OverrideEffectDuration
                ? overrideEntry.EffectDuration
                : baseDefinition.EffectDuration;
            var effectIntensity = overrideEntry != null && overrideEntry.OverrideEffectIntensity
                ? overrideEntry.EffectIntensity
                : baseDefinition.EffectIntensity;

            return new LootEffectDefinition(
                baseDefinition.Id,
                baseDefinition.DisplayName,
                baseDefinition.ThemeColor,
                profile.BaseDropChance,
                profile.MinimumDropChance,
                profile.RarityReductionPerLevel,
                profile.RequiredCount,
                effectDuration,
                effectIntensity,
                baseDefinition.EffectType,
                baseDefinition.CollectWhileActive,
                baseDefinition.EffectTarget);
        }

        public static IReadOnlyList<LootEffectDefinition> ResolveAllLootDefinitions()
        {
            var resolved = new List<LootEffectDefinition>(LootEffectCatalog.All.Length);
            for (var i = 0; i < LootEffectCatalog.All.Length; i++)
            {
                resolved.Add(ResolveLootDefinition(LootEffectCatalog.All[i]));
            }

            return resolved;
        }

        public static bool ShouldForceLootDrop() => Profile.ForceLootDropChance;

        public static bool ShouldSkipBossGate() => Profile.SkipBossGate;

        public static int MaximumActiveEnemies => Profile.MaximumActiveEnemies;

        public static float MinimumSpawnDistance => Profile.MinimumSpawnDistance;

        public static float MaximumSpawnDistance => Profile.MaximumSpawnDistance;

        public static float InitialSpawnDelay => Profile.InitialSpawnDelay;

        public static float GroupGrowthSeconds => Profile.GroupGrowthSeconds;

        public static int MaximumGroupSize => Profile.MaximumGroupSize;

        public static float IntervalAccelerationPerSecond => Profile.IntervalAccelerationPerSecond;

        public static float PlayerCollisionRadius => Profile.PlayerCollisionRadius;

        public static int RegularEnemyLevelOffset => Profile.RegularEnemyLevelOffset;

        public static int EliteEnemyLevelOffset => Profile.EliteEnemyLevelOffset;

        public static int BossEnemyLevelOffset => Profile.BossEnemyLevelOffset;

        public static float ExperiencePerEnemyLevel => Profile.ExperiencePerEnemyLevel;

        public static float EliteExperienceMultiplier => Profile.EliteExperienceMultiplier;

        public static int ExperienceToNext(int currentLevel, int playerCount)
        {
            var profile = Profile;
            var level = Mathf.Max(1, currentLevel);
            var soloRequirement = profile.XpBase + level * profile.XpLinear +
                                  Mathf.Pow(level, profile.XpPower) * profile.XpPowerScale;
            var partyMultiplier = playerCount > 1 ? profile.CoopXpMultiplier : 1f;
            return Mathf.RoundToInt(soloRequirement * partyMultiplier);
        }

        public static float UltimateCooldown(float baseCooldown, int cooldownUpgrades)
        {
            var profile = Profile;
            var multiplier = Mathf.Pow(profile.UltimateCooldownUpgradeMultiplier, Mathf.Max(0, cooldownUpgrades));
            return Mathf.Max(profile.UltimateCooldownFloor, baseCooldown * multiplier);
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

        public static int ExperienceForEnemy(int baseExperienceRoll, int enemyLevel, bool elite, bool boss)
        {
            var safeLevel = Mathf.Max(1, enemyLevel);
            var multiplier = 1f + (safeLevel - 1) * ExperiencePerEnemyLevel;

            if (elite)
            {
                multiplier *= EliteExperienceMultiplier;
            }

            return Mathf.Max(1, Mathf.RoundToInt(baseExperienceRoll * multiplier));
        }

        public static float DefaultMagnetRadius => Profile.DefaultMagnetRadius;

        public static float DamageImmunityDuration => Profile.DamageImmunityDuration;

        public static float UltimateImmunityDuration => Profile.UltimateImmunityDuration;

        public static float ReviveImmunityDuration => Profile.ReviveImmunityDuration;

        public static float ReviveDuration => Profile.ReviveDuration;

        public static float ReviveDecayRate => Profile.ReviveDecayRate;

        public static float ReviveHealthFraction => Profile.ReviveHealthFraction;

        public static float VeteranHealthMultiplier => Profile.VeteranHealthMultiplier;

        public static float VeteranSpawnRateMultiplier => Profile.VeteranSpawnRateMultiplier;

        public static float SwarmSurgeGroupBonus => Profile.SwarmSurgeGroupBonus;

        public static int SwarmSurgeMaximumGroupSize => Profile.SwarmSurgeMaximumGroupSize;

        public static float GlassCannonDamageTakenMultiplier => Profile.GlassCannonDamageTakenMultiplier;

        public static float GlassCannonWeaponDamageMultiplier => Profile.GlassCannonWeaponDamageMultiplier;

        public static float RelentlessClockBossTimeMultiplier => Profile.RelentlessClockBossTimeMultiplier;

        public static float RelentlessClockKillObjectiveMultiplier => Profile.RelentlessClockKillObjectiveMultiplier;

        public static CharacterDefinition ResolveCharacter(CharacterDefinition baseDefinition)
        {
            if (baseDefinition == null)
            {
                return null;
            }

            var entry = FindCharacterOverride(baseDefinition.Id);
            if (entry == null)
            {
                return baseDefinition;
            }

            return new CharacterDefinition(
                baseDefinition.Id,
                baseDefinition.Name,
                baseDefinition.Tribe,
                baseDefinition.Role,
                baseDefinition.Description,
                baseDefinition.Color,
                entry.OverrideMaxHealth ? entry.MaxHealth : baseDefinition.MaxHealth,
                entry.OverrideMoveSpeed ? entry.MoveSpeed : baseDefinition.MoveSpeed,
                entry.OverrideArmor ? entry.Armor : baseDefinition.Armor,
                baseDefinition.UltimateName,
                baseDefinition.UltimateDescription,
                entry.OverrideUltimateCooldown ? entry.UltimateCooldown : baseDefinition.UltimateCooldown,
                entry.OverrideUltimateDamage ? entry.UltimateDamage : baseDefinition.UltimateDamage,
                entry.OverrideUltimateRadius ? entry.UltimateRadius : baseDefinition.UltimateRadius,
                baseDefinition.StarterWeaponIds,
                baseDefinition.LockedPreviewLine);
        }

        public static CharacterDefinition ResolveCharacter(string characterId)
        {
            return ResolveCharacter(ContentCatalog.FindCharacter(characterId));
        }

        public static MapDefinition ResolveMap(MapDefinition baseDefinition)
        {
            if (baseDefinition == null)
            {
                return null;
            }

            var entry = FindMapOverride(baseDefinition.Id);
            if (entry == null)
            {
                return baseDefinition;
            }

            return new MapDefinition(
                baseDefinition.Id,
                baseDefinition.Name,
                baseDefinition.Region,
                baseDefinition.Description,
                baseDefinition.DurationLabel,
                entry.OverrideDuration ? entry.Duration : baseDefinition.Duration,
                entry.OverrideBossSpawnTime ? entry.BossSpawnTime : baseDefinition.BossSpawnTime,
                entry.OverrideBaseSpawnInterval ? entry.BaseSpawnInterval : baseDefinition.BaseSpawnInterval,
                entry.OverrideMinimumSpawnInterval ? entry.MinimumSpawnInterval : baseDefinition.MinimumSpawnInterval,
                entry.OverrideDifficultyRamp ? entry.DifficultyRamp : baseDefinition.DifficultyRamp,
                baseDefinition.GroundColor,
                baseDefinition.WeaponSlots,
                baseDefinition.GearSlots,
                entry.OverrideRequiredKillObjective ? entry.RequiredKillObjective : baseDefinition.RequiredKillObjective,
                entry.OverrideOptionalShardObjective ? entry.OptionalShardObjective : baseDefinition.OptionalShardObjective,
                entry.OverrideExtractionDuration ? entry.ExtractionDuration : baseDefinition.ExtractionDuration,
                baseDefinition.ExtractionBeaconX,
                baseDefinition.ExtractionBeaconY,
                baseDefinition.LockedPreviewLine,
                baseDefinition.BiomeId,
                baseDefinition.RegularEnemyId,
                baseDefinition.EliteEnemyId,
                baseDefinition.BossEnemyId,
                baseDefinition.VictoryRelicStandardId,
                baseDefinition.VictoryRelicBonusId,
                baseDefinition.KillObjectiveLabel,
                baseDefinition.OptionalPickupLabel,
                baseDefinition.PhaseAnnouncements,
                baseDefinition.BossEntranceAnnouncement,
                baseDefinition.EliteSpawnAnnouncement,
                baseDefinition.LandmarkProfileId);
        }

        public static MapDefinition ResolveMap(string mapId)
        {
            return ResolveMap(ContentCatalog.FindMap(mapId));
        }

        public static EnemyDefinition ResolveEnemy(EnemyDefinition baseDefinition)
        {
            if (baseDefinition == null)
            {
                return null;
            }

            var entry = FindEnemyOverride(baseDefinition.Id);
            if (entry == null)
            {
                return baseDefinition;
            }

            return new EnemyDefinition(
                baseDefinition.Id,
                baseDefinition.Name,
                baseDefinition.Boss,
                entry.OverrideBaseHealth ? entry.BaseHealth : baseDefinition.BaseHealth,
                entry.OverrideHealthPerDifficulty ? entry.HealthPerDifficulty : baseDefinition.HealthPerDifficulty,
                entry.OverrideMinimumSpeed ? entry.MinimumSpeed : baseDefinition.MinimumSpeed,
                entry.OverrideMaximumSpeed ? entry.MaximumSpeed : baseDefinition.MaximumSpeed,
                entry.OverrideSpeedPerDifficulty ? entry.SpeedPerDifficulty : baseDefinition.SpeedPerDifficulty,
                entry.OverrideBaseContactDamage ? entry.BaseContactDamage : baseDefinition.BaseContactDamage,
                entry.OverrideContactDamagePerDifficulty
                    ? entry.ContactDamagePerDifficulty
                    : baseDefinition.ContactDamagePerDifficulty,
                baseDefinition.MinimumRadius,
                baseDefinition.MaximumRadius,
                entry.OverrideMinimumExperience ? entry.MinimumExperience : baseDefinition.MinimumExperience,
                entry.OverrideMaximumExperienceExclusive
                    ? entry.MaximumExperienceExclusive
                    : baseDefinition.MaximumExperienceExclusive,
                baseDefinition.PrimaryColor,
                baseDefinition.AlternateColor);
        }

        public static EnemyDefinition ResolveEnemy(string enemyId)
        {
            return ResolveEnemy(EnemyCatalog.FindById(enemyId));
        }

        public static bool TryResolveWeapon(string weaponId, out WeaponProfile profile)
        {
            if (!WeaponProfile.TryGetBase(weaponId, out var baseProfile))
            {
                profile = default;
                return false;
            }

            profile = ApplyWeaponOverride(baseProfile);
            return true;
        }

        public static WeaponProfile ApplyWeaponOverride(WeaponProfile baseProfile)
        {
            var entry = FindWeaponOverride(baseProfile.Id);
            if (entry == null)
            {
                return baseProfile;
            }

            return new WeaponProfile(
                baseProfile.Id,
                baseProfile.Behavior,
                entry.OverrideBaseDamage ? entry.BaseDamage : baseProfile.BaseDamage,
                entry.OverrideBaseCooldown ? entry.BaseCooldown : baseProfile.BaseCooldown,
                entry.OverrideBaseCount ? entry.BaseCount : baseProfile.BaseCount,
                entry.OverrideBasePierce ? entry.BasePierce : baseProfile.BasePierce,
                entry.OverrideBaseCriticalChance ? entry.BaseCriticalChance : baseProfile.BaseCriticalChance,
                baseProfile.InitialTimerDelay,
                entry.OverrideProjectileSpeed ? entry.ProjectileSpeed : baseProfile.ProjectileSpeed,
                baseProfile.ProjectileDuration,
                baseProfile.HitRadius,
                baseProfile.CriticalHitRadius,
                baseProfile.Knockback,
                baseProfile.CriticalKnockback,
                entry.OverridePulseRadius ? entry.PulseRadius : baseProfile.PulseRadius,
                baseProfile.PulseKnockback,
                baseProfile.PulseHealPerHit,
                baseProfile.PulseHealCap,
                entry.OverrideRadialRadius ? entry.RadialRadius : baseProfile.RadialRadius,
                baseProfile.RadialBurstCount,
                entry.OverrideOrbitRadius ? entry.OrbitRadius : baseProfile.OrbitRadius,
                baseProfile.OrbitSpeedDegrees,
                baseProfile.SpreadDegrees,
                baseProfile.HealAmount,
                baseProfile.StartsActive);
        }

        public static CharacterOverrideEntry GetOrCreateCharacterOverride(string id)
        {
            var entry = FindCharacterOverride(id);
            if (entry != null)
            {
                return entry;
            }

            var list = new CharacterOverrideEntry[Profile.CharacterOverrides.Length + 1];
            Profile.CharacterOverrides.CopyTo(list, 0);
            entry = new CharacterOverrideEntry { Id = id };
            list[list.Length - 1] = entry;
            Profile.CharacterOverrides = list;
            return entry;
        }

        public static MapOverrideEntry GetOrCreateMapOverride(string id)
        {
            var entry = FindMapOverride(id);
            if (entry != null)
            {
                return entry;
            }

            var list = new MapOverrideEntry[Profile.MapOverrides.Length + 1];
            Profile.MapOverrides.CopyTo(list, 0);
            entry = new MapOverrideEntry { Id = id };
            list[list.Length - 1] = entry;
            Profile.MapOverrides = list;
            return entry;
        }

        public static EnemyOverrideEntry GetOrCreateEnemyOverride(string id)
        {
            var entry = FindEnemyOverride(id);
            if (entry != null)
            {
                return entry;
            }

            var list = new EnemyOverrideEntry[Profile.EnemyOverrides.Length + 1];
            Profile.EnemyOverrides.CopyTo(list, 0);
            entry = new EnemyOverrideEntry { Id = id };
            list[list.Length - 1] = entry;
            Profile.EnemyOverrides = list;
            return entry;
        }

        public static WeaponOverrideEntry GetOrCreateWeaponOverride(string id)
        {
            var entry = FindWeaponOverride(id);
            if (entry != null)
            {
                return entry;
            }

            var list = new WeaponOverrideEntry[Profile.WeaponOverrides.Length + 1];
            Profile.WeaponOverrides.CopyTo(list, 0);
            entry = new WeaponOverrideEntry { Id = id };
            list[list.Length - 1] = entry;
            Profile.WeaponOverrides = list;
            return entry;
        }

        private static CharacterOverrideEntry FindCharacterOverride(string id)
        {
            var overrides = Profile.CharacterOverrides;
            for (var i = 0; i < overrides.Length; i++)
            {
                if (overrides[i].Id == id)
                {
                    return overrides[i];
                }
            }

            return null;
        }

        private static MapOverrideEntry FindMapOverride(string id)
        {
            var overrides = Profile.MapOverrides;
            for (var i = 0; i < overrides.Length; i++)
            {
                if (overrides[i].Id == id)
                {
                    return overrides[i];
                }
            }

            return null;
        }

        private static EnemyOverrideEntry FindEnemyOverride(string id)
        {
            var overrides = Profile.EnemyOverrides;
            for (var i = 0; i < overrides.Length; i++)
            {
                if (overrides[i].Id == id)
                {
                    return overrides[i];
                }
            }

            return null;
        }

        private static WeaponOverrideEntry FindWeaponOverride(string id)
        {
            var overrides = Profile.WeaponOverrides;
            for (var i = 0; i < overrides.Length; i++)
            {
                if (overrides[i].Id == id)
                {
                    return overrides[i];
                }
            }

            return null;
        }

        private static LootOverrideEntry FindLootOverride(string id)
        {
            var overrides = Profile.LootOverrides;
            for (var i = 0; i < overrides.Length; i++)
            {
                if (overrides[i].Id == id)
                {
                    return overrides[i];
                }
            }

            return null;
        }

        public static LootOverrideEntry GetOrCreateLootOverride(string id)
        {
            var entry = FindLootOverride(id);
            if (entry != null)
            {
                return entry;
            }

            var list = new LootOverrideEntry[Profile.LootOverrides.Length + 1];
            Profile.LootOverrides.CopyTo(list, 0);
            entry = new LootOverrideEntry { Id = id };
            list[list.Length - 1] = entry;
            Profile.LootOverrides = list;
            return entry;
        }
    }
}
