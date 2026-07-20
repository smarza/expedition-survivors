using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    /// <summary>
    /// Local presentation adapter for the shared automatic-weapon registry.
    /// </summary>
    public sealed class WeaponSystem
    {
        public float AxeDamage => Model.AxeDamage;
        public float AxeCooldown => Model.AxeCooldown;
        public int AxeCount => Model.AxeCount;
        public int AxePierce => Model.AxePierce;
        public float CriticalChance => Model.CriticalChance;
        public bool HasShieldPulse => Model.HasShieldPulse;
        public float ShieldDamage => Model.ShieldDamage;
        public float ShieldCooldown => Model.ShieldCooldown;
        public float ShieldRadius => Model.ShieldRadius;
        public float ShieldKnockback => Model.ShieldKnockback;
        public float ShieldHealingPerHit => Model.ShieldHealingPerHit;
        public float ShieldHealingCap => Model.ShieldHealingCap;
        public float AxeProjectileSpeed => Model.AxeProjectileSpeed;
        public float AxeProjectileDuration => Model.AxeProjectileDuration;
        public float AxeHitRadius => Model.AxeHitRadius;
        public float CriticalAxeHitRadius => Model.CriticalAxeHitRadius;
        public float AxeKnockback => Model.AxeKnockback;
        public float CriticalAxeKnockback => Model.CriticalAxeKnockback;
        public float CriticalDamageMultiplier => Model.CriticalDamageMultiplier;
        public float CleaverExplosionDamageMultiplier => Model.CleaverExplosionDamageMultiplier;
        public float CleaverExplosionRadius => Model.CleaverExplosionRadius;
        public bool FrostAxeEvolved => Model.FrostAxeEvolved;
        public bool RavenGuardEvolved => Model.RavenGuardEvolved;

        internal SharedWeaponModel Model { get; } = new SharedWeaponModel();

        public IReadOnlyList<WeaponInstance> EquippedWeapons => Model.Registry.EquippedWeapons;

        private readonly GameDirector _director;
        private readonly PlayerController _owner;

        public WeaponSystem(GameDirector director, PlayerController owner)
        {
            _director = director;
            _owner = owner;
        }

        public void ApplyMastery(int mastery) => Model.ApplyMastery(mastery);

        public void ApplyHeroMastery(string characterId, int mastery) =>
            Model.ApplyHeroMastery(characterId, mastery);

        public void SyncFromBuild(PlayerBuild build) => Model.SyncFromBuild(build);

        public void Tick(float deltaTime)
        {
            Model.Advance(deltaTime);
            var fireEvents = Model.LastFireEvents;
            for (var i = 0; i < fireEvents.Count; i++)
            {
                FireWeapon(fireEvents[i]);
            }
        }

        private void FireWeapon(WeaponFireEvent fireEvent)
        {
            var weapon = Model.Registry.FindInstance(fireEvent.WeaponId);
            if (weapon == null)
            {
                return;
            }

            switch (fireEvent.Behavior)
            {
                case WeaponBehaviorKind.ProjectileVolley:
                    ThrowProjectileVolley(weapon);
                    break;
                case WeaponBehaviorKind.OwnerPulse:
                    TriggerOwnerPulse(weapon);
                    break;
                case WeaponBehaviorKind.RadialBurst:
                    TriggerRadialBurst(weapon);
                    break;
                case WeaponBehaviorKind.OrbitBlade:
                    TriggerOrbitBlade(weapon);
                    break;
            }
        }

        private void ThrowProjectileVolley(WeaponInstance weapon)
        {
            var nearest = _director.GetNearestEnemy(_owner.transform.position);
            if (nearest == null)
            {
                return;
            }

            var baseDirection = nearest.Position - (Vector2)_owner.transform.position;
            _owner.PresentAttack(baseDirection);
            _director.Present(PresentationCue.AxeThrow, _owner.transform.position,
                _owner.Definition.Color, 0.5f);

            var projectileBehavior = weapon.CreateProjectileBehavior(_owner.PlayerIndex);
            for (var i = 0; i < weapon.Count; i++)
            {
                var usesCritical = weapon.WeaponId == ItemCatalog.FrostAxe.Id
                    ? Model.CriticalChance
                    : weapon.CriticalChance;
                var critical = usesCritical > 0f && _director.Rng.Chance(usesCritical);
                var effect = weapon.WeaponId == ItemCatalog.FrostAxe.Id
                    ? Model.CreateAxeEffect(critical)
                    : weapon.CreateProjectileEffect(critical);
                var direction = weapon.WeaponId == ItemCatalog.FrostAxe.Id
                    ? Model.CalculateAxeDirection(baseDirection, i)
                    : weapon.CalculateProjectileDirection(baseDirection, i);
                var projectile = _director.SpawnProjectile(_owner.transform.position);
                projectile.Initialize(_director, direction, effect, projectileBehavior, _owner);
            }
        }

        private void TriggerOwnerPulse(WeaponInstance weapon)
        {
            if (weapon.HealAmount > 0f)
            {
                _owner.Heal(weapon.HealAmount);
                ApplySupplyChainEffects(weapon);
                _director.ShowPulse(_owner.transform.position, _owner.PlayerIndex);
                return;
            }

            var effect = weapon.WeaponId == ItemCatalog.RavenGuard.Id
                ? Model.CreateRavenGuardEffect()
                : CreateOwnerPulseEffect(weapon);
            var hits = _director.ResolveAreaEffect(_owner.transform.position, effect);
            var healing = weapon.WeaponId == ItemCatalog.RavenGuard.Id
                ? Model.CalculateRavenGuardHealing(hits)
                : weapon.CalculatePulseHealing(hits);
            if (healing > 0f)
            {
                _owner.Heal(healing);
            }

            ApplyOwnerPulseEvolutionEffects(weapon);
            _director.ShowPulse(_owner.transform.position, _owner.PlayerIndex);
        }

        private SharedEffectRequest CreateOwnerPulseEffect(WeaponInstance weapon)
        {
            var effect = weapon.CreatePulseEffect();
            if (weapon.BreachKnockbackMultiplier <= 1f)
            {
                return effect;
            }

            return new SharedEffectRequest(
                effect.Kind, effect.Target, effect.Stacking, effect.Damage,
                effect.Radius, weapon.ResolvePulseKnockback(), effect.Speed, effect.Duration,
                effect.Pierce, effect.Critical, effect.Evolved);
        }

        private void ApplyOwnerPulseEvolutionEffects(WeaponInstance weapon)
        {
            if (weapon.WeaponId == ItemCatalog.GroveThornLash.Id && weapon.IsEvolved)
            {
                _director.SpawnPersistentZone(
                    (Vector2)_owner.transform.position,
                    weapon.PersistentZoneRadius,
                    weapon.PersistentZoneTickDamage,
                    0.35f,
                    weapon.PersistentZoneDuration,
                    weapon.PersistentZoneTickInterval,
                    false,
                    _owner.PlayerIndex);
            }

            if (weapon.WeaponId == ItemCatalog.IronBeacon.Id && weapon.IsEvolved)
            {
                if (weapon.SanctuaryDuration > 0f)
                {
                    _owner.ApplyTemporaryArmorAura(weapon.SanctuaryArmorBonus, weapon.SanctuaryDuration);
                }

                if (weapon.BreachArmorAuraDuration > 0f)
                {
                    _owner.ApplyTemporaryArmorAura(weapon.BreachArmorAuraBonus, weapon.BreachArmorAuraDuration);
                }
            }
        }

        private void ApplySupplyChainEffects(WeaponInstance weapon)
        {
            if (!weapon.IsEvolved || weapon.SupplyChainMagnetDuration <= 0f)
            {
                return;
            }

            _owner.ApplyTemporaryMagnetBoost(weapon.SupplyChainMagnetBoost, weapon.SupplyChainMagnetDuration);

            for (var i = 0; i < _director.Players.Count; i++)
            {
                var ally = _director.Players[i];
                if (ally == null || ally == _owner || !ally.IsDowned)
                {
                    continue;
                }

                var distance = ((Vector2)ally.transform.position - (Vector2)_owner.transform.position).sqrMagnitude;
                if (distance > weapon.PulseRadius * weapon.PulseRadius)
                {
                    continue;
                }

                ally.Heal(weapon.SupplyChainDownedHeal);
            }
        }

        private void TriggerRadialBurst(WeaponInstance weapon)
        {
            var origin = (Vector2)_owner.transform.position;
            _director.Present(PresentationCue.AxeThrow, origin, _owner.Definition.Color, 0.45f);

            for (var i = 0; i < weapon.RadialBurstCount; i++)
            {
                var direction = weapon.CalculateRadialDirection(i);
                var effect = new SharedEffectRequest(
                    SharedEffectKind.Projectile, SharedEffectTarget.Enemies,
                    SharedEffectStacking.Independent, weapon.Damage, weapon.HitRadius,
                    weapon.Knockback, weapon.ProjectileSpeed, weapon.ProjectileDuration,
                    weapon.Pierce, false, weapon.IsEvolved);
                var projectile = _director.SpawnProjectile(origin);
                projectile.Initialize(_director, direction, effect);
            }

            if (weapon.IsEvolved && weapon.PersistentZoneDuration > 0f)
            {
                _director.SpawnPersistentZone(
                    origin,
                    weapon.PersistentZoneRadius,
                    weapon.PersistentZoneTickDamage,
                    0.42f,
                    weapon.PersistentZoneDuration,
                    weapon.PersistentZoneTickInterval,
                    false,
                    _owner.PlayerIndex);
            }

            if (weapon.IsEvolved && weapon.StaggeredBurstWaves > 1 && weapon.PendingRadialBursts <= 0)
            {
                weapon.PendingRadialBursts = weapon.StaggeredBurstWaves - 1;
                weapon.PendingRadialBurstTimer = weapon.StaggeredBurstDelay;
            }
        }

        private void TriggerOrbitBlade(WeaponInstance weapon)
        {
            var ownerPosition = (Vector2)_owner.transform.position;

            for (var bladeIndex = 0; bladeIndex < weapon.Count; bladeIndex++)
            {
                var bladePosition = weapon.CalculateOrbitPosition(ownerPosition, bladeIndex);
                var effect = weapon.CreateOrbitEffect();
                _director.ResolveAreaEffect(bladePosition, effect);
                _director.Present(PresentationCue.ProjectileTrail, bladePosition, _owner.Definition.Color, 0.12f);
            }
        }
    }
}
