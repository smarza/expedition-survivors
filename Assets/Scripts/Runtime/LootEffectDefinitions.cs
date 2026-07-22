using UnityEngine;

namespace ProjectExpedition
{
    public enum TemporaryEffectType : byte
    {
        Regeneration,
        MoveSpeed,
        CriticalChance,
        DamageBoost,
        Invincibility
    }

    public enum LootCollectWhileActive : byte
    {
        Discard,
        Bank,
        Extend
    }

    public enum TemporaryEffectTarget : byte
    {
        WholeParty,
        CollectorOnly
    }

    public sealed class LootEffectDefinition
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly Color ThemeColor;
        public readonly float BaseDropChance;
        public readonly float MinimumDropChance;
        public readonly float RarityReductionPerLevel;
        public readonly int RequiredCount;
        public readonly float EffectDuration;
        public readonly float EffectIntensity;
        public readonly TemporaryEffectType EffectType;
        public readonly LootCollectWhileActive CollectWhileActive;
        public readonly TemporaryEffectTarget EffectTarget;

        public LootEffectDefinition(
            string id,
            string displayName,
            Color themeColor,
            float baseDropChance,
            float minimumDropChance,
            float rarityReductionPerLevel,
            int requiredCount,
            float effectDuration,
            float effectIntensity,
            TemporaryEffectType effectType,
            LootCollectWhileActive collectWhileActive = LootCollectWhileActive.Discard,
            TemporaryEffectTarget effectTarget = TemporaryEffectTarget.WholeParty)
        {
            Id = id;
            DisplayName = displayName;
            ThemeColor = themeColor;
            BaseDropChance = baseDropChance;
            MinimumDropChance = minimumDropChance;
            RarityReductionPerLevel = rarityReductionPerLevel;
            RequiredCount = requiredCount;
            EffectDuration = effectDuration;
            EffectIntensity = effectIntensity;
            EffectType = effectType;
            CollectWhileActive = collectWhileActive;
            EffectTarget = effectTarget;
        }

        public float EvaluateDropChance(int playerLevel, int kills, int playerCount)
        {
            var safeLevel = Mathf.Max(1, playerLevel);
            var levelScale = 1f / (1f + (safeLevel - 1) * RarityReductionPerLevel);
            var partyScale = playerCount > 1 ? 0.92f : 1f;
            return Mathf.Max(MinimumDropChance, BaseDropChance * levelScale * partyScale);
        }
    }

    public static class LootEffectCatalog
    {
        public static readonly LootEffectDefinition HealingEmbers = new LootEffectDefinition(
            "loot.healing_embers",
            "Healing Embers",
            new Color(0.28f, 0.72f, 1f),
            0.04f,
            0.005f,
            0.08f,
            10,
            8f,
            8f,
            TemporaryEffectType.Regeneration,
            LootCollectWhileActive.Discard,
            TemporaryEffectTarget.WholeParty);

        public static readonly LootEffectDefinition CriticalFlare = new LootEffectDefinition(
            "loot.critical_flare",
            "Critical Flare",
            new Color(1f, 0.55f, 0.12f),
            0.04f,
            0.005f,
            0.08f,
            10,
            8f,
            0.25f,
            TemporaryEffectType.CriticalChance,
            LootCollectWhileActive.Discard,
            TemporaryEffectTarget.WholeParty);

        public static readonly LootEffectDefinition SwiftTrail = new LootEffectDefinition(
            "loot.swift_trail",
            "Swift Trail",
            new Color(0.35f, 0.92f, 0.38f),
            0.04f,
            0.005f,
            0.08f,
            10,
            8f,
            0.46f,
            TemporaryEffectType.MoveSpeed,
            LootCollectWhileActive.Discard,
            TemporaryEffectTarget.WholeParty);

        public static readonly LootEffectDefinition WrathEmbers = new LootEffectDefinition(
            "loot.wrath_embers",
            "Wrath Embers",
            new Color(0.95f, 0.22f, 0.18f),
            0.04f,
            0.005f,
            0.08f,
            10,
            8f,
            1.25f,
            TemporaryEffectType.DamageBoost,
            LootCollectWhileActive.Discard,
            TemporaryEffectTarget.WholeParty);

        public static readonly LootEffectDefinition AegisVeil = new LootEffectDefinition(
            "loot.aegis_veil",
            "Aegis Veil",
            new Color(0.72f, 0.32f, 0.95f),
            0.04f,
            0.005f,
            0.08f,
            10,
            6f,
            1f,
            TemporaryEffectType.Invincibility,
            LootCollectWhileActive.Discard,
            TemporaryEffectTarget.WholeParty);

        public static readonly LootEffectDefinition[] All =
        {
            HealingEmbers,
            CriticalFlare,
            SwiftTrail,
            WrathEmbers,
            AegisVeil
        };

        public static LootEffectDefinition FindById(string id)
        {
            for (var i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id)
                {
                    return All[i];
                }
            }

            return null;
        }

        public static LootEffectDefinition DefaultRunLoot => HealingEmbers;

        public static float EvaluateAnyDropChance(int playerLevel, int kills, int playerCount)
        {
            if (All.Length == 0)
            {
                return 0f;
            }

            return All[0].EvaluateDropChance(playerLevel, kills, playerCount);
        }

        public static LootEffectDefinition RollDropDefinition(RunRandom random)
        {
            if (random == null || All.Length == 0)
            {
                return DefaultRunLoot;
            }

            var index = random.Range(0, All.Length);
            return All[index];
        }
    }
}
