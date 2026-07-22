using UnityEngine;

namespace ProjectExpedition
{
    public static class DevelopmentTuningDefaults
    {
        public static DevelopmentTuningProfileData Create()
        {
            var healing = LootEffectCatalog.HealingEmbers;
            var profile = new DevelopmentTuningProfileData
            {
                BaseDropChance = healing.BaseDropChance,
                MinimumDropChance = healing.MinimumDropChance,
                RarityReductionPerLevel = healing.RarityReductionPerLevel,
                RequiredCount = healing.RequiredCount,
                MaximumActiveEnemies = SharedSpawnModel.DefaultMaximumActiveEnemies,
                MinimumSpawnDistance = SharedSpawnModel.DefaultMinimumSpawnDistance,
                MaximumSpawnDistance = SharedSpawnModel.DefaultMaximumSpawnDistance,
                InitialSpawnDelay = SharedSpawnModel.DefaultInitialSpawnDelay,
                GroupGrowthSeconds = SharedSpawnModel.DefaultGroupGrowthSeconds,
                MaximumGroupSize = SharedSpawnModel.DefaultMaximumGroupSize,
                IntervalAccelerationPerSecond = SharedSpawnModel.DefaultIntervalAccelerationPerSecond,
                PlayerCollisionRadius = BalanceRules.DefaultPlayerCollisionRadius,
                RegularEnemyLevelOffset = BalanceRules.DefaultRegularEnemyLevelOffset,
                EliteEnemyLevelOffset = BalanceRules.DefaultEliteEnemyLevelOffset,
                BossEnemyLevelOffset = BalanceRules.DefaultBossEnemyLevelOffset,
                ExperiencePerEnemyLevel = BalanceRules.DefaultExperiencePerEnemyLevel,
                EliteExperienceMultiplier = BalanceRules.DefaultEliteExperienceMultiplier,
                XpBase = BalanceRules.DefaultXpBase,
                XpLinear = BalanceRules.DefaultXpLinear,
                XpPower = BalanceRules.DefaultXpPower,
                XpPowerScale = BalanceRules.DefaultXpPowerScale,
                CoopXpMultiplier = BalanceRules.DefaultCoopXpMultiplier,
                UltimateCooldownFloor = BalanceRules.DefaultUltimateCooldownFloor,
                UltimateCooldownUpgradeMultiplier = BalanceRules.DefaultUltimateCooldownUpgradeMultiplier,
                DefaultMagnetRadius = SharedPlayerModel.DefaultMagnetRadius,
                DamageImmunityDuration = SharedPlayerModel.DefaultDamageImmunityDuration,
                UltimateImmunityDuration = SharedPlayerModel.DefaultUltimateImmunityDuration,
                ReviveImmunityDuration = SharedPlayerModel.DefaultReviveImmunityDuration,
                ReviveDuration = SharedPlayerModel.DefaultReviveDuration,
                ReviveDecayRate = SharedPlayerModel.DefaultReviveDecayRate,
                ReviveHealthFraction = SharedPlayerModel.DefaultReviveHealthFraction,
                PlayerContactKnockback = 0.35f,
                PlayerBossContactKnockback = 0.55f,
                PlayerBossSlamKnockback = 0.9f,
                PlayerBossChargeKnockbackMultiplier = 1.2f,
                PlayerHurtTraumaBase = 0.12f,
                PlayerHurtTraumaHeavyBonus = 0.18f,
                PlayerHurtVignetteScale = 1f,
                PlayerLowHealthThreshold = 0.3f,
                VeteranHealthMultiplier = SharedChallengeProfileModel.DefaultVeteranHealthMultiplier,
                VeteranSpawnRateMultiplier = SharedChallengeProfileModel.DefaultVeteranSpawnRateMultiplier,
                SwarmSurgeGroupBonus = SharedChallengeProfileModel.DefaultSwarmSurgeGroupBonus,
                SwarmSurgeMaximumGroupSize = SharedChallengeProfileModel.DefaultSwarmSurgeMaximumGroupSize,
                GlassCannonDamageTakenMultiplier =
                    SharedChallengeProfileModel.DefaultGlassCannonDamageTakenMultiplier,
                GlassCannonWeaponDamageMultiplier =
                    SharedChallengeProfileModel.DefaultGlassCannonWeaponDamageMultiplier,
                RelentlessClockBossTimeMultiplier =
                    SharedChallengeProfileModel.DefaultRelentlessClockBossTimeMultiplier,
                RelentlessClockKillObjectiveMultiplier =
                    SharedChallengeProfileModel.DefaultRelentlessClockKillObjectiveMultiplier,
                ForceLootDropChance = false,
                SkipBossGate = false,
                CharacterOverrides = System.Array.Empty<CharacterOverrideEntry>(),
                MapOverrides = System.Array.Empty<MapOverrideEntry>(),
                EnemyOverrides = System.Array.Empty<EnemyOverrideEntry>(),
                WeaponOverrides = System.Array.Empty<WeaponOverrideEntry>(),
                LootOverrides = System.Array.Empty<LootOverrideEntry>()
            };

            return profile;
        }

        public static DevelopmentTuningProfileData Clone(DevelopmentTuningProfileData source)
        {
            if (source == null)
            {
                return Create();
            }

            var json = JsonUtility.ToJson(source);
            var clone = JsonUtility.FromJson<DevelopmentTuningProfileData>(json) ??
                        new DevelopmentTuningProfileData();
            Sanitize(clone);
            return clone;
        }

        public static void Sanitize(DevelopmentTuningProfileData profile)
        {
            if (profile == null)
            {
                return;
            }

            profile.Version = 1;
            profile.BaseDropChance = Mathf.Clamp(profile.BaseDropChance, 0f, 1f);
            profile.MinimumDropChance = Mathf.Clamp(profile.MinimumDropChance, 0f, profile.BaseDropChance);
            profile.RarityReductionPerLevel = Mathf.Max(0f, profile.RarityReductionPerLevel);
            profile.RequiredCount = Mathf.Max(1, profile.RequiredCount);
            profile.MaximumActiveEnemies = Mathf.Max(1, profile.MaximumActiveEnemies);
            profile.MinimumSpawnDistance = Mathf.Max(0.1f, profile.MinimumSpawnDistance);
            profile.MaximumSpawnDistance = Mathf.Max(profile.MinimumSpawnDistance, profile.MaximumSpawnDistance);
            profile.InitialSpawnDelay = Mathf.Max(0f, profile.InitialSpawnDelay);
            profile.GroupGrowthSeconds = Mathf.Max(1f, profile.GroupGrowthSeconds);
            profile.MaximumGroupSize = Mathf.Max(1, profile.MaximumGroupSize);
            profile.IntervalAccelerationPerSecond = Mathf.Max(0f, profile.IntervalAccelerationPerSecond);
            profile.PlayerCollisionRadius = Mathf.Max(0.01f, profile.PlayerCollisionRadius);
            profile.RegularEnemyLevelOffset = Mathf.Max(0, profile.RegularEnemyLevelOffset);
            profile.EliteEnemyLevelOffset = Mathf.Max(0, profile.EliteEnemyLevelOffset);
            profile.BossEnemyLevelOffset = Mathf.Max(0, profile.BossEnemyLevelOffset);
            profile.ExperiencePerEnemyLevel = Mathf.Max(0f, profile.ExperiencePerEnemyLevel);
            profile.EliteExperienceMultiplier = Mathf.Max(0.01f, profile.EliteExperienceMultiplier);
            profile.XpBase = Mathf.Max(0f, profile.XpBase);
            profile.XpLinear = Mathf.Max(0f, profile.XpLinear);
            profile.XpPower = Mathf.Max(0.01f, profile.XpPower);
            profile.XpPowerScale = Mathf.Max(0f, profile.XpPowerScale);
            profile.CoopXpMultiplier = Mathf.Max(1f, profile.CoopXpMultiplier);
            profile.UltimateCooldownFloor = Mathf.Max(0f, profile.UltimateCooldownFloor);
            profile.UltimateCooldownUpgradeMultiplier =
                Mathf.Clamp(profile.UltimateCooldownUpgradeMultiplier, 0.01f, 1f);
            profile.DefaultMagnetRadius = Mathf.Max(0f, profile.DefaultMagnetRadius);
            profile.DamageImmunityDuration = Mathf.Max(0f, profile.DamageImmunityDuration);
            profile.UltimateImmunityDuration = Mathf.Max(0f, profile.UltimateImmunityDuration);
            profile.ReviveImmunityDuration = Mathf.Max(0f, profile.ReviveImmunityDuration);
            profile.ReviveDuration = Mathf.Max(0.1f, profile.ReviveDuration);
            profile.ReviveDecayRate = Mathf.Clamp01(profile.ReviveDecayRate);
            profile.ReviveHealthFraction = Mathf.Clamp01(profile.ReviveHealthFraction);
            profile.PlayerContactKnockback = Mathf.Max(0f, profile.PlayerContactKnockback);
            profile.PlayerBossContactKnockback = Mathf.Max(0f, profile.PlayerBossContactKnockback);
            profile.PlayerBossSlamKnockback = Mathf.Max(0f, profile.PlayerBossSlamKnockback);
            profile.PlayerBossChargeKnockbackMultiplier =
                Mathf.Max(0.01f, profile.PlayerBossChargeKnockbackMultiplier);
            profile.PlayerHurtTraumaBase = Mathf.Max(0f, profile.PlayerHurtTraumaBase);
            profile.PlayerHurtTraumaHeavyBonus = Mathf.Max(0f, profile.PlayerHurtTraumaHeavyBonus);
            profile.PlayerHurtVignetteScale = Mathf.Max(0f, profile.PlayerHurtVignetteScale);
            profile.PlayerLowHealthThreshold = Mathf.Clamp01(profile.PlayerLowHealthThreshold);
            profile.VeteranHealthMultiplier = Mathf.Max(0.01f, profile.VeteranHealthMultiplier);
            profile.VeteranSpawnRateMultiplier = Mathf.Max(0.01f, profile.VeteranSpawnRateMultiplier);
            profile.SwarmSurgeGroupBonus = Mathf.Max(0f, profile.SwarmSurgeGroupBonus);
            profile.SwarmSurgeMaximumGroupSize = Mathf.Max(1, profile.SwarmSurgeMaximumGroupSize);
            profile.GlassCannonDamageTakenMultiplier =
                Mathf.Max(0.01f, profile.GlassCannonDamageTakenMultiplier);
            profile.GlassCannonWeaponDamageMultiplier =
                Mathf.Max(0.01f, profile.GlassCannonWeaponDamageMultiplier);
            profile.RelentlessClockBossTimeMultiplier =
                Mathf.Max(0.01f, profile.RelentlessClockBossTimeMultiplier);
            profile.RelentlessClockKillObjectiveMultiplier =
                Mathf.Max(0.01f, profile.RelentlessClockKillObjectiveMultiplier);

            if (profile.CharacterOverrides == null)
            {
                profile.CharacterOverrides = System.Array.Empty<CharacterOverrideEntry>();
            }

            if (profile.MapOverrides == null)
            {
                profile.MapOverrides = System.Array.Empty<MapOverrideEntry>();
            }

            if (profile.EnemyOverrides == null)
            {
                profile.EnemyOverrides = System.Array.Empty<EnemyOverrideEntry>();
            }

            if (profile.WeaponOverrides == null)
            {
                profile.WeaponOverrides = System.Array.Empty<WeaponOverrideEntry>();
            }

            if (profile.LootOverrides == null)
            {
                profile.LootOverrides = System.Array.Empty<LootOverrideEntry>();
            }
        }
    }
}
