using System;
using UnityEngine;

namespace ProjectExpedition
{
    public enum LootCollectResult : byte
    {
        Incremented,
        Activated,
        DiscardedWhileActive
    }

    public sealed class SharedLootProgressModel
    {
        private LootEffectDefinition _trackedDefinition = LootEffectCatalog.DefaultRunLoot;

        public int CurrentCount { get; private set; }
        public int RequiredCount => _trackedDefinition?.RequiredCount ?? 0;
        public LootEffectDefinition TrackedDefinition => _trackedDefinition;
        public bool IsNearActivation =>
            RequiredCount > 0 && CurrentCount >= Mathf.RoundToInt(RequiredCount * 0.8f);

        public void Begin(LootEffectDefinition definition = null)
        {
            _trackedDefinition = definition ?? LootEffectCatalog.DefaultRunLoot;
            CurrentCount = 0;
        }

        public bool TryRollDrop(int playerLevel, int kills, int playerCount, RunRandom random)
        {
            if (_trackedDefinition == null || random == null)
            {
                return false;
            }

            var chance = _trackedDefinition.EvaluateDropChance(playerLevel, kills, playerCount);
            return random.Chance(chance);
        }

        public LootCollectResult OnCollected(bool effectActive)
        {
            if (_trackedDefinition == null)
            {
                return LootCollectResult.Incremented;
            }

            if (effectActive)
            {
                if (_trackedDefinition.CollectWhileActive == LootCollectWhileActive.Discard)
                {
                    return LootCollectResult.DiscardedWhileActive;
                }

                if (_trackedDefinition.CollectWhileActive == LootCollectWhileActive.Extend)
                {
                    CurrentCount = Math.Min(RequiredCount, CurrentCount + 1);
                    return LootCollectResult.Incremented;
                }
            }

            CurrentCount++;

            if (CurrentCount < RequiredCount)
            {
                return LootCollectResult.Incremented;
            }

            CurrentCount = 0;
            return LootCollectResult.Activated;
        }
    }
}
