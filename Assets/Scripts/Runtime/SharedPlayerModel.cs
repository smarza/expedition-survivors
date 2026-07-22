using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public enum PlayerDamageResult : byte
    {
        Ignored,
        Damaged,
        Downed
    }

    /// <summary>
    /// Presentation-free survivor state shared by every player adapter. It owns
    /// deterministic statistics, damage, knockdown, revival, movement requests
    /// and Ultimate timing, but it does not read input or touch GameObjects.
    /// </summary>
    public sealed class SharedPlayerModel
    {
        public const float DefaultMagnetRadius = 1.7f;
        public const float DefaultDamageImmunityDuration = 0.22f;
        public const float DefaultUltimateImmunityDuration = 1.25f;
        public const float DefaultReviveImmunityDuration = 1f;
        public const float DefaultReviveDuration = 2.5f;
        public const float DefaultReviveDecayRate = 0.35f;
        public const float DefaultReviveHealthFraction = 0.42f;

        private const float InitialUltimateCharge = 0.3f;

        private Func<float, int, float> _ultimateCooldownRule;
        private float _baseUltimateCooldown;
        private float _baseUltimateDamage;
        private float _baseUltimateRadius;
        private int _ultimateCooldownUpgrades;
        private float _ultimateDamageMultiplier = 1f;
        private float _healthRegenPerSecond;
        private float _temporaryMagnetBonus;
        private float _temporaryMagnetRemaining;
        private float _temporaryArmorBonus;
        private float _temporaryArmorRemaining;
        private readonly List<TemporaryLootContribution> _lootContributions = new List<TemporaryLootContribution>();
        private float _temporaryMoveSpeedBonus;
        private float _temporaryCriticalBonus;
        private float _temporaryDamageMultiplier = 1f;

        public float TemporaryCriticalBonus => _temporaryCriticalBonus;
        public float TemporaryDamageMultiplier => _temporaryDamageMultiplier;

        public float MaxHealth { get; private set; }
        public float Health { get; private set; }
        public float MoveSpeed { get; private set; }
        public float Armor { get; private set; }
        public float MagnetRadius { get; private set; }
        public bool IsAlive => Health > 0f && !IsDowned;
        public bool IsDowned { get; private set; }
        public float ReviveProgress { get; private set; }
        public float InvulnerabilityRemaining { get; private set; }
        public float UltimateRemaining { get; private set; }
        public float UltimateCooldown { get; private set; }
        public bool UltimateReady => UltimateRemaining <= 0f;
        public float UltimateDamage => _baseUltimateDamage * _ultimateDamageMultiplier;
        public float UltimateRadius => _baseUltimateRadius *
            (1f + (_ultimateDamageMultiplier - 1f) * 0.35f);
        public float EffectiveArmor => Armor + _temporaryArmorBonus;
        public float EffectiveMagnetRadius => MagnetRadius + _temporaryMagnetBonus;
        public float EffectiveMoveSpeed => MoveSpeed + _temporaryMoveSpeedBonus;

        public void Begin(float maxHealth, float moveSpeed, float armor,
            float ultimateCooldown, float ultimateDamage, float ultimateRadius,
            Func<float, int, float> ultimateCooldownRule, float magnetRadius = 1.7f)
        {
            _ultimateCooldownRule = ultimateCooldownRule ??
                throw new ArgumentNullException(nameof(ultimateCooldownRule));
            MaxHealth = Mathf.Max(1f, maxHealth);
            Health = MaxHealth;
            MoveSpeed = Mathf.Max(0f, moveSpeed);
            Armor = Mathf.Max(0f, armor);
            MagnetRadius = Mathf.Max(0f, magnetRadius);
            _baseUltimateCooldown = Mathf.Max(0f, ultimateCooldown);
            _baseUltimateDamage = Mathf.Max(0f, ultimateDamage);
            _baseUltimateRadius = Mathf.Max(0f, ultimateRadius);
            _ultimateCooldownUpgrades = 0;
            _ultimateDamageMultiplier = 1f;
            _healthRegenPerSecond = 0f;
            _temporaryMagnetBonus = 0f;
            _temporaryMagnetRemaining = 0f;
            _temporaryArmorBonus = 0f;
            _temporaryArmorRemaining = 0f;
            _lootContributions.Clear();
            _temporaryMoveSpeedBonus = 0f;
            _temporaryCriticalBonus = 0f;
            _temporaryDamageMultiplier = 1f;
            UltimateCooldown = _baseUltimateCooldown;
            UltimateRemaining = UltimateCooldown * InitialUltimateCharge;
            InvulnerabilityRemaining = 0f;
            IsDowned = false;
            ReviveProgress = 0f;
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            if (_temporaryMagnetRemaining > 0f)
            {
                _temporaryMagnetRemaining = Mathf.Max(0f, _temporaryMagnetRemaining - deltaTime);
                if (_temporaryMagnetRemaining <= 0f)
                {
                    _temporaryMagnetBonus = 0f;
                }
            }

            if (_temporaryArmorRemaining > 0f)
            {
                _temporaryArmorRemaining = Mathf.Max(0f, _temporaryArmorRemaining - deltaTime);
                if (_temporaryArmorRemaining <= 0f)
                {
                    _temporaryArmorBonus = 0f;
                }
            }

            if (IsDowned)
            {
                return;
            }

            InvulnerabilityRemaining = Mathf.Max(0f, InvulnerabilityRemaining - deltaTime);
            UltimateRemaining = Mathf.Max(0f, UltimateRemaining - deltaTime);

            if (_healthRegenPerSecond > 0f)
            {
                Heal(_healthRegenPerSecond * deltaTime);
            }
        }

        public Vector2 CalculateRequestedPosition(Vector2 currentPosition, Vector2 movement,
            float deltaTime, IReadOnlyList<ObstacleDefinition> obstacles = null)
        {
            if (IsDowned || deltaTime <= 0f) return currentPosition;

            var requested = currentPosition + movement * EffectiveMoveSpeed * deltaTime;

            if (obstacles == null || obstacles.Count == 0)
            {
                return requested;
            }

            return SharedMovementCollision.AdvanceCircleTowardsTarget(
                currentPosition, BalanceRules.PlayerCollisionRadius, requested,
                EffectiveMoveSpeed * deltaTime, obstacles);
        }

        public bool TryActivateUltimate()
        {
            if (IsDowned || !UltimateReady) return false;
            UltimateRemaining = UltimateCooldown;
            InvulnerabilityRemaining = Mathf.Max(InvulnerabilityRemaining,
                DevelopmentTuningResolver.UltimateImmunityDuration);
            return true;
        }

        public PlayerDamageResult TakeDamage(float rawDamage)
        {
            if (!IsAlive || InvulnerabilityRemaining > 0f) return PlayerDamageResult.Ignored;
            Health -= Mathf.Max(1f, rawDamage - EffectiveArmor);
            InvulnerabilityRemaining = DevelopmentTuningResolver.DamageImmunityDuration;
            if (Health > 0f) return PlayerDamageResult.Damaged;

            Health = 0f;
            IsDowned = true;
            ReviveProgress = 0f;
            return PlayerDamageResult.Downed;
        }

        public bool AdvanceRevival(bool rescuerNearby, float deltaTime)
        {
            if (!IsDowned || deltaTime <= 0f) return false;
            var direction = rescuerNearby ? 1f : -DevelopmentTuningResolver.ReviveDecayRate;
            ReviveProgress = Mathf.Clamp01(ReviveProgress + direction * deltaTime /
                DevelopmentTuningResolver.ReviveDuration);
            if (ReviveProgress < 1f) return false;

            IsDowned = false;
            ReviveProgress = 0f;
            Health = MaxHealth * DevelopmentTuningResolver.ReviveHealthFraction;
            InvulnerabilityRemaining = DevelopmentTuningResolver.ReviveImmunityDuration;
            return true;
        }

        public void Heal(float amount) => Health = Mathf.Min(MaxHealth, Health + amount);

        public void AddMoveSpeed(float amount) => MoveSpeed += amount;

        public void AddArmor(float amount) => Armor += amount;

        public void AddMagnet(float amount) => MagnetRadius += amount;

        public void AddMaxHealth(float amount)
        {
            MaxHealth += amount;
            Health += amount;
        }

        public void AddHealthRegen(float amountPerSecond, float maximumPerSecond)
        {
            _healthRegenPerSecond = Mathf.Min(maximumPerSecond, _healthRegenPerSecond + amountPerSecond);
        }

        public void ApplyTemporaryMagnetBoost(float amount, float durationSeconds)
        {
            _temporaryMagnetBonus = amount;
            _temporaryMagnetRemaining = Mathf.Max(_temporaryMagnetRemaining, durationSeconds);
        }

        public void ApplyTemporaryArmorAura(float amount, float durationSeconds)
        {
            _temporaryArmorBonus = amount;
            _temporaryArmorRemaining = Mathf.Max(_temporaryArmorRemaining, durationSeconds);
        }

        public void ApplyTemporaryLootMoveSpeed(string instanceKey, float amount)
        {
            AddLootContribution(instanceKey, TemporaryEffectType.MoveSpeed, amount, 1f);
        }

        public void ApplyTemporaryLootCriticalBonus(string instanceKey, float amount)
        {
            AddLootContribution(instanceKey, TemporaryEffectType.CriticalChance, amount, 1f);
        }

        public void ApplyTemporaryLootDamageMultiplier(string instanceKey, float multiplier)
        {
            AddLootContribution(instanceKey, TemporaryEffectType.DamageBoost, 0f, multiplier);
        }

        public void RemoveTemporaryLootBonus(string instanceKey)
        {
            for (var i = _lootContributions.Count - 1; i >= 0; i--)
            {
                if (_lootContributions[i].InstanceKey == instanceKey)
                {
                    _lootContributions.RemoveAt(i);
                }
            }

            RecalculateLootBonuses();
        }

        public void RefreshTemporaryInvulnerability(float remaining)
        {
            InvulnerabilityRemaining = Mathf.Max(InvulnerabilityRemaining, remaining);
        }

        private void AddLootContribution(string instanceKey, TemporaryEffectType effectType, float flatAmount,
            float damageMultiplier)
        {
            for (var i = 0; i < _lootContributions.Count; i++)
            {
                if (_lootContributions[i].InstanceKey == instanceKey)
                {
                    return;
                }
            }

            _lootContributions.Add(new TemporaryLootContribution(instanceKey, effectType, flatAmount, damageMultiplier));
            RecalculateLootBonuses();
        }

        private void RecalculateLootBonuses()
        {
            _temporaryMoveSpeedBonus = 0f;
            _temporaryCriticalBonus = 0f;
            _temporaryDamageMultiplier = 1f;

            for (var i = 0; i < _lootContributions.Count; i++)
            {
                var contribution = _lootContributions[i];
                switch (contribution.EffectType)
                {
                    case TemporaryEffectType.MoveSpeed:
                        _temporaryMoveSpeedBonus += contribution.FlatAmount;
                        break;
                    case TemporaryEffectType.CriticalChance:
                        _temporaryCriticalBonus += contribution.FlatAmount;
                        break;
                    case TemporaryEffectType.DamageBoost:
                        _temporaryDamageMultiplier *= contribution.DamageMultiplier;
                        break;
                }
            }
        }

        private readonly struct TemporaryLootContribution
        {
            public readonly string InstanceKey;
            public readonly TemporaryEffectType EffectType;
            public readonly float FlatAmount;
            public readonly float DamageMultiplier;

            public TemporaryLootContribution(string instanceKey, TemporaryEffectType effectType, float flatAmount,
                float damageMultiplier)
            {
                InstanceKey = instanceKey;
                EffectType = effectType;
                FlatAmount = flatAmount;
                DamageMultiplier = damageMultiplier;
            }
        }

        public void ImproveUltimateCooldown()
        {
            _ultimateCooldownUpgrades++;
            var previous = UltimateCooldown;
            UltimateCooldown = Mathf.Max(0f,
                _ultimateCooldownRule(_baseUltimateCooldown, _ultimateCooldownUpgrades));
            if (previous > 0f)
                UltimateRemaining = Mathf.Min(UltimateCooldown,
                    UltimateRemaining * UltimateCooldown / previous);
        }

        public void ImproveUltimateDamage() => _ultimateDamageMultiplier *= 1.3f;
    }
}
