using System;
using UnityEngine;

namespace ProjectExpedition
{
    public enum ChallengeTier
    {
        Standard,
        Veteran
    }

    public enum ChallengeMutator
    {
        None,
        SwarmSurge,
        IronResolve,
        GlassCannon,
        RelentlessClock
    }

    public readonly struct ChallengeProfile
    {
        public readonly ChallengeTier Tier;
        public readonly ChallengeMutator MutatorA;
        public readonly ChallengeMutator MutatorB;

        public ChallengeProfile(ChallengeTier tier, ChallengeMutator mutatorA, ChallengeMutator mutatorB)
        {
            Tier = tier;
            MutatorA = mutatorA;
            MutatorB = mutatorB;
        }

        public bool HasMutator(ChallengeMutator mutator)
        {
            return mutator != ChallengeMutator.None && (MutatorA == mutator || MutatorB == mutator);
        }
    }

    /// <summary>
    /// Presentation-free challenge modifiers applied to spawn pressure, enemy stats,
    /// reward generation and renown payout.
    /// </summary>
    public static class SharedChallengeProfileModel
    {
        public const float DefaultVeteranHealthMultiplier = 1.25f;
        public const float DefaultVeteranSpawnRateMultiplier = 0.85f;
        public const float DefaultSwarmSurgeGroupBonus = 1f;
        public const int DefaultSwarmSurgeMaximumGroupSize = 8;
        public const float DefaultGlassCannonDamageTakenMultiplier = 1.4f;
        public const float DefaultGlassCannonWeaponDamageMultiplier = 1.25f;
        public const float DefaultRelentlessClockBossTimeMultiplier = 0.85f;
        public const float DefaultRelentlessClockKillObjectiveMultiplier = 0.9f;

        public static float ResolveRenownMultiplier(ChallengeProfile profile)
        {
            var multiplier = profile.Tier == ChallengeTier.Veteran ? 1.25f : 1f;

            if (profile.HasMutator(ChallengeMutator.SwarmSurge))
            {
                multiplier *= 1.10f;
            }

            if (profile.HasMutator(ChallengeMutator.IronResolve))
            {
                multiplier *= 1.15f;
            }

            if (profile.HasMutator(ChallengeMutator.GlassCannon))
            {
                multiplier *= 1.20f;
            }

            if (profile.HasMutator(ChallengeMutator.RelentlessClock))
            {
                multiplier *= 1.15f;
            }

            return multiplier;
        }

        public static float ApplyEnemyHealthMultiplier(float baseHealth, ChallengeProfile profile)
        {
            var multiplier = profile.Tier == ChallengeTier.Veteran
                ? DevelopmentTuningResolver.VeteranHealthMultiplier
                : 1f;
            return baseHealth * multiplier;
        }

        public static float ApplySpawnInterval(float interval, ChallengeProfile profile)
        {
            if (profile.Tier == ChallengeTier.Veteran)
            {
                interval *= DevelopmentTuningResolver.VeteranSpawnRateMultiplier;
            }

            return interval;
        }

        public static int ApplyGroupSize(int groupSize, ChallengeProfile profile)
        {
            if (profile.HasMutator(ChallengeMutator.SwarmSurge))
            {
                groupSize += Mathf.RoundToInt(DevelopmentTuningResolver.SwarmSurgeGroupBonus);
            }

            return Mathf.Clamp(groupSize, 1, DevelopmentTuningResolver.SwarmSurgeMaximumGroupSize);
        }

        public static float ApplyPlayerDamageTakenMultiplier(float damage, ChallengeProfile profile)
        {
            if (profile.HasMutator(ChallengeMutator.GlassCannon))
            {
                return damage * DevelopmentTuningResolver.GlassCannonDamageTakenMultiplier;
            }

            return damage;
        }

        public static float ApplyWeaponDamageMultiplier(float damage, ChallengeProfile profile)
        {
            if (profile.HasMutator(ChallengeMutator.GlassCannon))
            {
                return damage * DevelopmentTuningResolver.GlassCannonWeaponDamageMultiplier;
            }

            return damage;
        }

        public static int ApplyRequiredKillObjective(int requiredKills, ChallengeProfile profile)
        {
            if (profile.HasMutator(ChallengeMutator.RelentlessClock))
            {
                return Mathf.Max(1, Mathf.RoundToInt(requiredKills *
                    DevelopmentTuningResolver.RelentlessClockKillObjectiveMultiplier));
            }

            return requiredKills;
        }

        public static float ApplyBossSpawnTime(float bossSpawnTime, ChallengeProfile profile)
        {
            if (profile.HasMutator(ChallengeMutator.RelentlessClock))
            {
                return bossSpawnTime * DevelopmentTuningResolver.RelentlessClockBossTimeMultiplier;
            }

            return bossSpawnTime;
        }

        public static bool AllowsHealingRewards(ChallengeProfile profile)
        {
            return !profile.HasMutator(ChallengeMutator.IronResolve);
        }

        public static string DescribeMutator(ChallengeMutator mutator)
        {
            switch (mutator)
            {
                case ChallengeMutator.SwarmSurge:
                    return "Swarm Surge — larger enemy groups.";
                case ChallengeMutator.IronResolve:
                    return "Iron Resolve — no healing rewards.";
                case ChallengeMutator.GlassCannon:
                    return "Glass Cannon — +25% weapon damage, +40% damage taken.";
                case ChallengeMutator.RelentlessClock:
                    return "Relentless Clock — earlier boss and lower kill objective.";
                default:
                    return string.Empty;
            }
        }

        public static string MutatorUnlockId(ChallengeMutator mutator)
        {
            switch (mutator)
            {
                case ChallengeMutator.SwarmSurge:
                    return "challenge.swarm_surge";
                case ChallengeMutator.IronResolve:
                    return "challenge.iron_resolve";
                case ChallengeMutator.GlassCannon:
                    return "challenge.glass_cannon";
                case ChallengeMutator.RelentlessClock:
                    return "challenge.relentless_clock";
                default:
                    return string.Empty;
            }
        }

        public static ChallengeMutator MutatorForBiome(string biomeId)
        {
            if (biomeId == BiomeCatalog.CanopyId)
            {
                return ChallengeMutator.SwarmSurge;
            }

            if (biomeId == BiomeCatalog.RelayId)
            {
                return ChallengeMutator.RelentlessClock;
            }

            return ChallengeMutator.IronResolve;
        }
    }
}
