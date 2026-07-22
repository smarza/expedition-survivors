using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public sealed class ActiveTemporaryEffect
    {
        public readonly LootEffectDefinition Definition;
        public float Remaining;
        public int ActivatorPlayerIndex;
        public readonly string InstanceKey;

        public ActiveTemporaryEffect(LootEffectDefinition definition, int activatorPlayerIndex, string instanceKey)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Remaining = definition.EffectDuration;
            ActivatorPlayerIndex = activatorPlayerIndex;
            InstanceKey = instanceKey ?? definition.Id;
        }
    }

    public sealed class SharedTemporaryEffectModel
    {
        private readonly List<ActiveTemporaryEffect> _activeEffects = new List<ActiveTemporaryEffect>();
        private int _activationSequence;

        public bool HasActiveEffect => _activeEffects.Count > 0;
        public IReadOnlyList<ActiveTemporaryEffect> ActiveEffects => _activeEffects;
        public bool JustActivated { get; private set; }
        public bool JustExpired { get; private set; }
        public Color LastExpiredThemeColor { get; private set; }
        public LootEffectDefinition LastActivatedDefinition { get; private set; }

        public bool IsActive(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
            {
                return false;
            }

            for (var i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].Definition.Id == definitionId)
                {
                    return true;
                }
            }

            return false;
        }

        public ActiveTemporaryEffect GetMostRecentActiveEffect()
        {
            if (_activeEffects.Count == 0)
            {
                return null;
            }

            return _activeEffects[_activeEffects.Count - 1];
        }

        public void Activate(LootEffectDefinition definition, int activatorPlayerIndex,
            IReadOnlyList<PlayerController> players)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            _activationSequence++;
            var instanceKey = $"{definition.Id}#{_activationSequence}";
            var effect = new ActiveTemporaryEffect(definition, activatorPlayerIndex, instanceKey);
            _activeEffects.Add(effect);
            LastActivatedDefinition = definition;
            JustActivated = true;
            JustExpired = false;
            ApplyActivationBonuses(effect, players);
        }

        public void Advance(float deltaTime, IReadOnlyList<PlayerController> players)
        {
            JustActivated = false;
            JustExpired = false;

            if (deltaTime <= 0f || _activeEffects.Count == 0)
            {
                return;
            }

            for (var i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                ApplyEffectTick(effect, deltaTime, players);
                effect.Remaining -= deltaTime;

                if (effect.Remaining > 0f)
                {
                    continue;
                }

                RemoveEffectAtIndex(i, players);
            }
        }

        public void Clear(IReadOnlyList<PlayerController> players)
        {
            for (var i = _activeEffects.Count - 1; i >= 0; i--)
            {
                RemoveEffectAtIndex(i, players);
            }

            _activeEffects.Clear();
            JustActivated = false;
            JustExpired = false;
            LastActivatedDefinition = null;
        }

        private void RemoveEffectAtIndex(int index, IReadOnlyList<PlayerController> players)
        {
            var effect = _activeEffects[index];
            LastExpiredThemeColor = effect.Definition.ThemeColor;
            JustExpired = true;
            RemoveActivationBonuses(effect, players);
            _activeEffects.RemoveAt(index);
        }

        private static void ApplyActivationBonuses(ActiveTemporaryEffect effect, IReadOnlyList<PlayerController> players)
        {
            switch (effect.Definition.EffectType)
            {
                case TemporaryEffectType.MoveSpeed:
                    ApplyToTargets(effect, players,
                        player => player.ApplyTemporaryLootMoveSpeed(effect.InstanceKey, effect.Definition.EffectIntensity));
                    break;
                case TemporaryEffectType.CriticalChance:
                    ApplyToTargets(effect, players,
                        player => player.ApplyTemporaryLootCriticalBonus(effect.InstanceKey,
                            effect.Definition.EffectIntensity));
                    break;
                case TemporaryEffectType.DamageBoost:
                    ApplyToTargets(effect, players,
                        player => player.ApplyTemporaryLootDamageMultiplier(effect.InstanceKey,
                            effect.Definition.EffectIntensity));
                    break;
            }
        }

        private static void RemoveActivationBonuses(ActiveTemporaryEffect effect, IReadOnlyList<PlayerController> players)
        {
            if (players == null || players.Count == 0)
            {
                return;
            }

            switch (effect.Definition.EffectType)
            {
                case TemporaryEffectType.MoveSpeed:
                case TemporaryEffectType.CriticalChance:
                case TemporaryEffectType.DamageBoost:
                    ApplyToTargets(effect, players, player => player.RemoveTemporaryLootBonus(effect.InstanceKey));
                    break;
            }
        }

        private static void ApplyEffectTick(ActiveTemporaryEffect effect, float deltaTime,
            IReadOnlyList<PlayerController> players)
        {
            if (players == null || players.Count == 0)
            {
                return;
            }

            switch (effect.Definition.EffectType)
            {
                case TemporaryEffectType.Regeneration:
                    ApplyRegeneration(effect, deltaTime, players);
                    break;
                case TemporaryEffectType.Invincibility:
                    ApplyInvincibility(effect, players);
                    break;
            }
        }

        private static void ApplyRegeneration(ActiveTemporaryEffect effect, float deltaTime,
            IReadOnlyList<PlayerController> players)
        {
            var healAmount = effect.Definition.EffectIntensity * deltaTime;

            if (effect.Definition.EffectTarget == TemporaryEffectTarget.CollectorOnly)
            {
                ApplyHealToPlayer(players, effect.ActivatorPlayerIndex, healAmount);
                return;
            }

            for (var i = 0; i < players.Count; i++)
            {
                ApplyHealToPlayer(players, i, healAmount);
            }
        }

        private static void ApplyInvincibility(ActiveTemporaryEffect effect, IReadOnlyList<PlayerController> players)
        {
            if (effect.Definition.EffectTarget == TemporaryEffectTarget.CollectorOnly)
            {
                ApplyInvincibilityToPlayer(players, effect.ActivatorPlayerIndex, effect.Remaining);
                return;
            }

            for (var i = 0; i < players.Count; i++)
            {
                ApplyInvincibilityToPlayer(players, i, effect.Remaining);
            }
        }

        private static void ApplyInvincibilityToPlayer(IReadOnlyList<PlayerController> players, int playerIndex,
            float remaining)
        {
            if (playerIndex < 0 || playerIndex >= players.Count)
            {
                return;
            }

            var player = players[playerIndex];
            if (player == null || !player.IsAlive)
            {
                return;
            }

            player.RefreshTemporaryInvulnerability(remaining);
        }

        private static void ApplyHealToPlayer(IReadOnlyList<PlayerController> players, int playerIndex,
            float healAmount)
        {
            if (playerIndex < 0 || playerIndex >= players.Count)
            {
                return;
            }

            var player = players[playerIndex];
            if (player == null || !player.IsAlive)
            {
                return;
            }

            player.Heal(healAmount);
        }

        private static void ApplyToTargets(ActiveTemporaryEffect effect, IReadOnlyList<PlayerController> players,
            Action<PlayerController> apply)
        {
            if (players == null || apply == null)
            {
                return;
            }

            if (effect.Definition.EffectTarget == TemporaryEffectTarget.CollectorOnly)
            {
                if (effect.ActivatorPlayerIndex >= 0 && effect.ActivatorPlayerIndex < players.Count)
                {
                    var collector = players[effect.ActivatorPlayerIndex];
                    if (collector != null)
                    {
                        apply(collector);
                    }
                }

                return;
            }

            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player != null)
                {
                    apply(player);
                }
            }
        }
    }
}
