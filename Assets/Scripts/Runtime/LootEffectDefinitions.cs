using UnityEngine;

namespace ProjectExpedition
{
    public enum TemporaryEffectType : byte
    {
        Regeneration,
        MoveSpeed,
        CriticalChance
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

        public static LootEffectDefinition FindById(string id)
        {
            if (HealingEmbers.Id == id)
            {
                return HealingEmbers;
            }

            return null;
        }

        public static LootEffectDefinition DefaultRunLoot => HealingEmbers;
    }
}
