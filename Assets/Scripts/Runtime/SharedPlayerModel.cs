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
        private const float InitialUltimateCharge = 0.3f;
        private const float DamageImmunityDuration = 0.22f;
        private const float UltimateImmunityDuration = 1.25f;
        private const float ReviveImmunityDuration = 1f;
        private const float ReviveDuration = 2.5f;
        private const float ReviveDecayRate = 0.35f;
        private const float ReviveHealthFraction = 0.42f;

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

            var requested = currentPosition + movement * MoveSpeed * deltaTime;

            if (obstacles == null || obstacles.Count == 0)
            {
                return requested;
            }

            return SharedMovementCollision.AdvanceCircleTowardsTarget(
                currentPosition, BalanceRules.PlayerCollisionRadius, requested,
                MoveSpeed * deltaTime, obstacles);
        }

        public bool TryActivateUltimate()
        {
            if (IsDowned || !UltimateReady) return false;
            UltimateRemaining = UltimateCooldown;
            InvulnerabilityRemaining = Mathf.Max(InvulnerabilityRemaining, UltimateImmunityDuration);
            return true;
        }

        public PlayerDamageResult TakeDamage(float rawDamage)
        {
            if (!IsAlive || InvulnerabilityRemaining > 0f) return PlayerDamageResult.Ignored;
            Health -= Mathf.Max(1f, rawDamage - EffectiveArmor);
            InvulnerabilityRemaining = DamageImmunityDuration;
            if (Health > 0f) return PlayerDamageResult.Damaged;

            Health = 0f;
            IsDowned = true;
            ReviveProgress = 0f;
            return PlayerDamageResult.Downed;
        }

        public bool AdvanceRevival(bool rescuerNearby, float deltaTime)
        {
            if (!IsDowned || deltaTime <= 0f) return false;
            var direction = rescuerNearby ? 1f : -ReviveDecayRate;
            ReviveProgress = Mathf.Clamp01(ReviveProgress + direction * deltaTime / ReviveDuration);
            if (ReviveProgress < 1f) return false;

            IsDowned = false;
            ReviveProgress = 0f;
            Health = MaxHealth * ReviveHealthFraction;
            InvulnerabilityRemaining = ReviveImmunityDuration;
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
