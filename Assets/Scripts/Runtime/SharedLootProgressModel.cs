using System;
using System.Collections.Generic;
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
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();
        private readonly Dictionary<string, LootEffectDefinition> _definitionsById =
            new Dictionary<string, LootEffectDefinition>();
        private LootEffectDefinition _lastCollectedDefinition;
        private int _globalRequiredCount = 10;

        public LootEffectDefinition LastCollectedDefinition => _lastCollectedDefinition;
        public int GlobalRequiredCount => _globalRequiredCount;

        public void Begin(IReadOnlyList<LootEffectDefinition> definitions = null)
        {
            _counts.Clear();
            _definitionsById.Clear();
            _lastCollectedDefinition = null;
            _globalRequiredCount = 10;

            var source = definitions ?? DevelopmentTuningResolver.ResolveAllLootDefinitions();
            for (var i = 0; i < source.Count; i++)
            {
                var definition = source[i];
                if (definition == null || string.IsNullOrEmpty(definition.Id))
                {
                    continue;
                }

                _definitionsById[definition.Id] = definition;
                _counts[definition.Id] = 0;
                _globalRequiredCount = definition.RequiredCount;
            }
        }

        public int GetCount(LootEffectDefinition definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.Id))
            {
                return 0;
            }

            return _counts.TryGetValue(definition.Id, out var count) ? count : 0;
        }

        public int GetRequiredCount(LootEffectDefinition definition)
        {
            if (definition == null)
            {
                return _globalRequiredCount;
            }

            return definition.RequiredCount;
        }

        public bool IsNearActivation(LootEffectDefinition definition)
        {
            var required = GetRequiredCount(definition);
            if (required <= 0)
            {
                return false;
            }

            var count = GetCount(definition);
            return count >= Mathf.RoundToInt(required * 0.8f);
        }

        public LootEffectDefinition GetLeadingProgressDefinition()
        {
            LootEffectDefinition leading = null;
            var leadingRatio = -1f;

            foreach (var pair in _definitionsById)
            {
                var definition = pair.Value;
                var required = GetRequiredCount(definition);
                if (required <= 0)
                {
                    continue;
                }

                var ratio = GetCount(definition) / (float)required;
                if (ratio > leadingRatio)
                {
                    leadingRatio = ratio;
                    leading = definition;
                }
            }

            if (leading != null)
            {
                return leading;
            }

            return _lastCollectedDefinition ?? LootEffectCatalog.DefaultRunLoot;
        }

        public bool TryRollDrop(int playerLevel, int kills, int playerCount, RunRandom random,
            out LootEffectDefinition droppedDefinition)
        {
            droppedDefinition = null;

            if (random == null || _definitionsById.Count == 0)
            {
                return false;
            }

            if (DevelopmentTuningResolver.ShouldForceLootDrop())
            {
                droppedDefinition = RollDropDefinition(random);
                return droppedDefinition != null;
            }

            var chance = LootEffectCatalog.EvaluateAnyDropChance(playerLevel, kills, playerCount);
            if (!random.Chance(chance))
            {
                return false;
            }

            droppedDefinition = RollDropDefinition(random);
            return droppedDefinition != null;
        }

        public LootCollectResult OnCollected(LootEffectDefinition definition, Func<string, bool> isEffectActiveForDefinition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.Id))
            {
                return LootCollectResult.Incremented;
            }

            if (!_definitionsById.ContainsKey(definition.Id))
            {
                _definitionsById[definition.Id] = definition;
                _counts[definition.Id] = 0;
            }

            _lastCollectedDefinition = definition;
            var isActive = isEffectActiveForDefinition != null && isEffectActiveForDefinition(definition.Id);

            if (isActive && definition.CollectWhileActive == LootCollectWhileActive.Discard)
            {
                return LootCollectResult.DiscardedWhileActive;
            }

            if (isActive && definition.CollectWhileActive == LootCollectWhileActive.Extend)
            {
                var required = GetRequiredCount(definition);
                _counts[definition.Id] = Math.Min(required, GetCount(definition) + 1);
                return LootCollectResult.Incremented;
            }

            var nextCount = GetCount(definition) + 1;
            var activationRequired = GetRequiredCount(definition);

            if (isActive && definition.CollectWhileActive == LootCollectWhileActive.Bank)
            {
                _counts[definition.Id] = Math.Min(activationRequired, nextCount);
                return LootCollectResult.Incremented;
            }

            _counts[definition.Id] = nextCount;

            if (nextCount < activationRequired)
            {
                return LootCollectResult.Incremented;
            }

            _counts[definition.Id] = 0;
            return LootCollectResult.Activated;
        }

        public bool TryConsumeBankedActivation(LootEffectDefinition definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.Id))
            {
                return false;
            }

            var activationRequired = GetRequiredCount(definition);
            if (GetCount(definition) < activationRequired)
            {
                return false;
            }

            _counts[definition.Id] = 0;
            return true;
        }

        private LootEffectDefinition RollDropDefinition(RunRandom random)
        {
            var resolved = DevelopmentTuningResolver.ResolveAllLootDefinitions();
            if (resolved.Count == 0)
            {
                return LootEffectCatalog.DefaultRunLoot;
            }

            var index = random.Range(0, resolved.Count);
            return resolved[index];
        }
    }
}
