using System;
using UnityEngine;

namespace ProjectExpedition
{
    [Serializable]
    public sealed class DevelopmentTuningProfileData
    {
        public int Version = 1;

        public float BaseDropChance = 0.04f;
        public float MinimumDropChance = 0.005f;
        public float RarityReductionPerLevel = 0.08f;
        public int RequiredCount = 10;

        public int MaximumActiveEnemies = 260;
        public float MinimumSpawnDistance = 8.5f;
        public float MaximumSpawnDistance = 11.8f;
        public float InitialSpawnDelay = 0.2f;
        public float GroupGrowthSeconds = 35f;
        public int MaximumGroupSize = 7;
        public float IntervalAccelerationPerSecond = 0.0014f;

        public float PlayerCollisionRadius = 0.38f;
        public int RegularEnemyLevelOffset = 1;
        public int EliteEnemyLevelOffset = 2;
        public int BossEnemyLevelOffset = 3;
        public float ExperiencePerEnemyLevel = 0.12f;
        public float EliteExperienceMultiplier = 1.35f;
        public float XpBase = 48f;
        public float XpLinear = 8f;
        public float XpPower = 1.35f;
        public float XpPowerScale = 2f;
        public float CoopXpMultiplier = 1.35f;
        public float UltimateCooldownFloor = 28f;
        public float UltimateCooldownUpgradeMultiplier = 0.9f;

        public float DefaultMagnetRadius = 1.7f;
        public float DamageImmunityDuration = 0.22f;
        public float UltimateImmunityDuration = 1.25f;
        public float ReviveImmunityDuration = 1f;
        public float ReviveDuration = 2.5f;
        public float ReviveDecayRate = 0.35f;
        public float ReviveHealthFraction = 0.42f;

        public float VeteranHealthMultiplier = 1.25f;
        public float VeteranSpawnRateMultiplier = 0.85f;
        public float SwarmSurgeGroupBonus = 1f;
        public int SwarmSurgeMaximumGroupSize = 8;
        public float GlassCannonDamageTakenMultiplier = 1.4f;
        public float GlassCannonWeaponDamageMultiplier = 1.25f;
        public float RelentlessClockBossTimeMultiplier = 0.85f;
        public float RelentlessClockKillObjectiveMultiplier = 0.9f;

        public bool ForceLootDropChance;
        public bool SkipBossGate;

        public CharacterOverrideEntry[] CharacterOverrides = Array.Empty<CharacterOverrideEntry>();
        public MapOverrideEntry[] MapOverrides = Array.Empty<MapOverrideEntry>();
        public EnemyOverrideEntry[] EnemyOverrides = Array.Empty<EnemyOverrideEntry>();
        public WeaponOverrideEntry[] WeaponOverrides = Array.Empty<WeaponOverrideEntry>();
        public LootOverrideEntry[] LootOverrides = Array.Empty<LootOverrideEntry>();
    }

    [Serializable]
    public sealed class LootOverrideEntry
    {
        public string Id;
        public bool OverrideEffectDuration;
        public float EffectDuration;
        public bool OverrideEffectIntensity;
        public float EffectIntensity;
    }

    [Serializable]
    public sealed class CharacterOverrideEntry
    {
        public string Id;
        public bool OverrideMaxHealth;
        public float MaxHealth;
        public bool OverrideMoveSpeed;
        public float MoveSpeed;
        public bool OverrideArmor;
        public float Armor;
        public bool OverrideUltimateCooldown;
        public float UltimateCooldown;
        public bool OverrideUltimateDamage;
        public float UltimateDamage;
        public bool OverrideUltimateRadius;
        public float UltimateRadius;
    }

    [Serializable]
    public sealed class MapOverrideEntry
    {
        public string Id;
        public bool OverrideDuration;
        public float Duration;
        public bool OverrideBossSpawnTime;
        public float BossSpawnTime;
        public bool OverrideBaseSpawnInterval;
        public float BaseSpawnInterval;
        public bool OverrideMinimumSpawnInterval;
        public float MinimumSpawnInterval;
        public bool OverrideDifficultyRamp;
        public float DifficultyRamp;
        public bool OverrideRequiredKillObjective;
        public int RequiredKillObjective;
        public bool OverrideOptionalShardObjective;
        public int OptionalShardObjective;
        public bool OverrideExtractionDuration;
        public float ExtractionDuration;
    }

    [Serializable]
    public sealed class EnemyOverrideEntry
    {
        public string Id;
        public bool OverrideBaseHealth;
        public float BaseHealth;
        public bool OverrideHealthPerDifficulty;
        public float HealthPerDifficulty;
        public bool OverrideMinimumSpeed;
        public float MinimumSpeed;
        public bool OverrideMaximumSpeed;
        public float MaximumSpeed;
        public bool OverrideSpeedPerDifficulty;
        public float SpeedPerDifficulty;
        public bool OverrideBaseContactDamage;
        public float BaseContactDamage;
        public bool OverrideContactDamagePerDifficulty;
        public float ContactDamagePerDifficulty;
        public bool OverrideMinimumExperience;
        public int MinimumExperience;
        public bool OverrideMaximumExperienceExclusive;
        public int MaximumExperienceExclusive;
    }

    [Serializable]
    public sealed class WeaponOverrideEntry
    {
        public string Id;
        public bool OverrideBaseDamage;
        public float BaseDamage;
        public bool OverrideBaseCooldown;
        public float BaseCooldown;
        public bool OverrideBaseCount;
        public int BaseCount;
        public bool OverrideBasePierce;
        public int BasePierce;
        public bool OverrideBaseCriticalChance;
        public float BaseCriticalChance;
        public bool OverrideProjectileSpeed;
        public float ProjectileSpeed;
        public bool OverridePulseRadius;
        public float PulseRadius;
        public bool OverrideRadialRadius;
        public float RadialRadius;
        public bool OverrideOrbitRadius;
        public float OrbitRadius;
    }

    public enum DevelopmentTuningTab
    {
        Loot,
        Spawn,
        Experience,
        Player,
        Challenge,
        Maps,
        Heroes,
        Enemies,
        Weapons
    }

    public sealed class DevelopmentTuningUiState
    {
        public DevelopmentTuningTab Tab = DevelopmentTuningTab.Loot;
        public int Selection;
        public int ContentIndex;
    }

    public readonly struct DevelopmentTuningFieldDescriptor
    {
        public readonly string Label;
        public readonly float Minimum;
        public readonly float Maximum;
        public readonly float Step;
        public readonly bool IsInteger;

        public DevelopmentTuningFieldDescriptor(
            string label, float minimum, float maximum, float step, bool isInteger = false)
        {
            Label = label;
            Minimum = minimum;
            Maximum = maximum;
            Step = step;
            IsInteger = isInteger;
        }
    }
}
