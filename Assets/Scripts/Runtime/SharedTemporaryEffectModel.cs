using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public sealed class SharedTemporaryEffectModel
    {
        public bool HasActiveEffect { get; private set; }
        public LootEffectDefinition ActiveDefinition { get; private set; }
        public float Remaining { get; private set; }
        public int ActivatorPlayerIndex { get; private set; } = -1;
        public bool JustActivated { get; private set; }
        public bool JustExpired { get; private set; }
        public Color LastExpiredThemeColor { get; private set; }

        public void Activate(LootEffectDefinition definition, int activatorPlayerIndex)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            ActiveDefinition = definition;
            Remaining = definition.EffectDuration;
            ActivatorPlayerIndex = activatorPlayerIndex;
            HasActiveEffect = true;
            JustActivated = true;
            JustExpired = false;
        }

        public void Advance(float deltaTime, IReadOnlyList<PlayerController> players)
        {
            JustActivated = false;
            JustExpired = false;

            if (!HasActiveEffect || deltaTime <= 0f || ActiveDefinition == null)
            {
                return;
            }

            ApplyEffectTick(deltaTime, players);
            Remaining -= deltaTime;

            if (Remaining > 0f)
            {
                return;
            }

            HasActiveEffect = false;
            Remaining = 0f;
            LastExpiredThemeColor = ActiveDefinition.ThemeColor;
            JustExpired = true;
            ActiveDefinition = null;
            ActivatorPlayerIndex = -1;
        }

        public void Clear()
        {
            HasActiveEffect = false;
            Remaining = 0f;
            ActiveDefinition = null;
            ActivatorPlayerIndex = -1;
            JustActivated = false;
            JustExpired = false;
        }

        private void ApplyEffectTick(float deltaTime, IReadOnlyList<PlayerController> players)
        {
            if (players == null || players.Count == 0)
            {
                return;
            }

            switch (ActiveDefinition.EffectType)
            {
                case TemporaryEffectType.Regeneration:
                    ApplyRegeneration(deltaTime, players);
                    break;
            }
        }

        private void ApplyRegeneration(float deltaTime, IReadOnlyList<PlayerController> players)
        {
            var healAmount = ActiveDefinition.EffectIntensity * deltaTime;

            if (ActiveDefinition.EffectTarget == TemporaryEffectTarget.CollectorOnly)
            {
                ApplyHealToPlayer(players, ActivatorPlayerIndex, healAmount);
                return;
            }

            for (var i = 0; i < players.Count; i++)
            {
                ApplyHealToPlayer(players, i, healAmount);
            }
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
    }
}
