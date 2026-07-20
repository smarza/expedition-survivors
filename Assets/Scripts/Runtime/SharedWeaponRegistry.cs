using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public enum WeaponBehaviorKind : byte
    {
        ProjectileVolley,
        OwnerPulse,
        RadialBurst,
        OrbitBlade
    }

    public readonly struct WeaponFireEvent
    {
        public readonly string WeaponId;
        public readonly WeaponBehaviorKind Behavior;

        public WeaponFireEvent(string weaponId, WeaponBehaviorKind behavior)
        {
            WeaponId = weaponId;
            Behavior = behavior;
        }
    }

    public readonly struct WeaponProfile
    {
        public readonly string Id;
        public readonly WeaponBehaviorKind Behavior;
        public readonly float BaseDamage;
        public readonly float BaseCooldown;
        public readonly int BaseCount;
        public readonly int BasePierce;
        public readonly float BaseCriticalChance;
        public readonly float InitialTimerDelay;
        public readonly float ProjectileSpeed;
        public readonly float ProjectileDuration;
        public readonly float HitRadius;
        public readonly float CriticalHitRadius;
        public readonly float Knockback;
        public readonly float CriticalKnockback;
        public readonly float PulseRadius;
        public readonly float PulseKnockback;
        public readonly float PulseHealPerHit;
        public readonly float PulseHealCap;
        public readonly float RadialRadius;
        public readonly int RadialBurstCount;
        public readonly float OrbitRadius;
        public readonly float OrbitSpeedDegrees;
        public readonly float SpreadDegrees;
        public readonly float HealAmount;
        public readonly bool StartsActive;

        public WeaponProfile(
            string id, WeaponBehaviorKind behavior, float baseDamage, float baseCooldown,
            int baseCount = 1, int basePierce = 1, float baseCriticalChance = 0f,
            float initialTimerDelay = 0.25f, float projectileSpeed = 9.5f, float projectileDuration = 3.2f,
            float hitRadius = 0.25f, float criticalHitRadius = 0.32f, float knockback = 0.18f,
            float criticalKnockback = 0.42f, float pulseRadius = 2.55f, float pulseKnockback = 0.72f,
            float pulseHealPerHit = 0f, float pulseHealCap = 0f, float radialRadius = 2.5f,
            int radialBurstCount = 8, float orbitRadius = 1.55f, float orbitSpeedDegrees = 200f,
            float spreadDegrees = 10f, float healAmount = 0f, bool startsActive = true)
        {
            Id = id;
            Behavior = behavior;
            BaseDamage = baseDamage;
            BaseCooldown = baseCooldown;
            BaseCount = baseCount;
            BasePierce = basePierce;
            BaseCriticalChance = baseCriticalChance;
            InitialTimerDelay = initialTimerDelay;
            ProjectileSpeed = projectileSpeed;
            ProjectileDuration = projectileDuration;
            HitRadius = hitRadius;
            CriticalHitRadius = criticalHitRadius;
            Knockback = knockback;
            CriticalKnockback = criticalKnockback;
            PulseRadius = pulseRadius;
            PulseKnockback = pulseKnockback;
            PulseHealPerHit = pulseHealPerHit;
            PulseHealCap = pulseHealCap;
            RadialRadius = radialRadius;
            RadialBurstCount = radialBurstCount;
            OrbitRadius = orbitRadius;
            OrbitSpeedDegrees = orbitSpeedDegrees;
            SpreadDegrees = spreadDegrees;
            HealAmount = healAmount;
            StartsActive = startsActive;
        }

        public static bool TryGet(string weaponId, out WeaponProfile profile)
        {
            for (var i = 0; i < All.Length; i++)
            {
                if (All[i].Id != weaponId)
                {
                    continue;
                }

                profile = All[i];
                return true;
            }

            profile = default;
            return false;
        }

        private static readonly WeaponProfile[] All =
        {
            new WeaponProfile(
                "weapon.frost_axe", WeaponBehaviorKind.ProjectileVolley, 24f, 0.82f,
                basePierce: 1, baseCriticalChance: 0.08f, initialTimerDelay: 0.25f,
                projectileSpeed: 9.5f, projectileDuration: 3.2f),
            new WeaponProfile(
                "weapon.raven_guard", WeaponBehaviorKind.OwnerPulse, 20f, 5.2f,
                initialTimerDelay: 4.5f, pulseRadius: 2.55f, pulseKnockback: 0.72f),
            new WeaponProfile(
                "weapon.north_wind_spear", WeaponBehaviorKind.ProjectileVolley, 28f, 1.05f,
                basePierce: 3, initialTimerDelay: 0.35f, projectileSpeed: 11f, projectileDuration: 2.6f,
                hitRadius: 0.22f, criticalHitRadius: 0.28f, knockback: 0.22f, criticalKnockback: 0.38f),
            new WeaponProfile(
                "weapon.rune_bolt", WeaponBehaviorKind.ProjectileVolley, 12f, 0.38f,
                baseCriticalChance: 0.04f, initialTimerDelay: 0.15f, projectileSpeed: 13f,
                projectileDuration: 2f, hitRadius: 0.18f, criticalHitRadius: 0.24f, knockback: 0.12f,
                criticalKnockback: 0.28f),
            new WeaponProfile(
                "weapon.oath_ring", WeaponBehaviorKind.OrbitBlade, 11f, 0.55f,
                baseCount: 2, basePierce: 1, initialTimerDelay: 0f, orbitRadius: 1.55f,
                orbitSpeedDegrees: 200f, hitRadius: 0.28f),
            new WeaponProfile(
                "weapon.grove_thorn_lash", WeaponBehaviorKind.OwnerPulse, 15f, 3.4f,
                initialTimerDelay: 2.2f, pulseRadius: 1.95f, pulseKnockback: 0.55f),
            new WeaponProfile(
                "weapon.canopy_vortex", WeaponBehaviorKind.RadialBurst, 19f, 3.8f,
                initialTimerDelay: 3f, radialRadius: 2.6f, radialBurstCount: 8, projectileSpeed: 8.5f,
                projectileDuration: 1.8f, hitRadius: 0.2f, knockback: 0.35f),
            new WeaponProfile(
                "weapon.driftwood_staff", WeaponBehaviorKind.RadialBurst, 28f, 6f,
                initialTimerDelay: 4.8f, radialRadius: 3f, radialBurstCount: 6, projectileSpeed: 6.5f,
                projectileDuration: 2.4f, hitRadius: 0.24f, knockback: 0.48f),
            new WeaponProfile(
                "weapon.signal_flare", WeaponBehaviorKind.ProjectileVolley, 30f, 1.35f,
                initialTimerDelay: 0.4f, projectileSpeed: 8.5f, projectileDuration: 2.2f,
                hitRadius: 0.3f, criticalHitRadius: 0.36f, knockback: 0.28f, criticalKnockback: 0.52f),
            new WeaponProfile(
                "weapon.supply_pulse", WeaponBehaviorKind.OwnerPulse, 0f, 7.5f,
                initialTimerDelay: 6f, pulseRadius: 2.1f, healAmount: 10f),
            new WeaponProfile(
                "weapon.iron_beacon", WeaponBehaviorKind.OwnerPulse, 22f, 6.8f,
                initialTimerDelay: 5f, pulseRadius: 3.4f, pulseKnockback: 0.45f),
            new WeaponProfile(
                "weapon.tide_caller", WeaponBehaviorKind.ProjectileVolley, 18f, 0.95f,
                baseCount: 3, basePierce: 1, initialTimerDelay: 0.3f, projectileSpeed: 7.8f,
                projectileDuration: 3f, spreadDegrees: 18f, hitRadius: 0.26f, criticalHitRadius: 0.3f,
                knockback: 0.16f, criticalKnockback: 0.34f)
        };
    }

    public sealed class WeaponInstance
    {
        public readonly WeaponProfile Profile;
        public readonly string WeaponId;
        public readonly WeaponBehaviorKind Behavior;

        public float Damage;
        public float Cooldown;
        public int Count;
        public int Pierce;
        public float CriticalChance;
        public float Timer;
        public float OrbitAngle;
        public float OrbitRadius;
        public float OrbitSpeedDegrees;
        public float PulseRadius;
        public float PulseKnockback;
        public float PulseHealPerHit;
        public float PulseHealCap;
        public float RadialRadius;
        public int RadialBurstCount;
        public float ProjectileSpeed;
        public float ProjectileDuration;
        public float HitRadius;
        public float CriticalHitRadius;
        public float Knockback;
        public float CriticalKnockback;
        public float SpreadDegrees;
        public float HealAmount;
        public bool IsActive;
        public bool IsEvolved;
        public float ExplosionDamageMultiplier;
        public float ExplosionRadius;

        public WeaponInstance(WeaponProfile profile)
        {
            Profile = profile;
            WeaponId = profile.Id;
            Behavior = profile.Behavior;
            ResetFromProfile();
        }

        public void ResetFromProfile()
        {
            Damage = Profile.BaseDamage;
            Cooldown = Profile.BaseCooldown;
            Count = Profile.BaseCount;
            Pierce = Profile.BasePierce;
            CriticalChance = Profile.BaseCriticalChance;
            Timer = Profile.InitialTimerDelay;
            OrbitAngle = 0f;
            OrbitRadius = Profile.OrbitRadius;
            OrbitSpeedDegrees = Profile.OrbitSpeedDegrees;
            PulseRadius = Profile.PulseRadius;
            PulseKnockback = Profile.PulseKnockback;
            PulseHealPerHit = Profile.PulseHealPerHit;
            PulseHealCap = Profile.PulseHealCap;
            RadialRadius = Profile.RadialRadius;
            RadialBurstCount = Profile.RadialBurstCount;
            ProjectileSpeed = Profile.ProjectileSpeed;
            ProjectileDuration = Profile.ProjectileDuration;
            HitRadius = Profile.HitRadius;
            CriticalHitRadius = Profile.CriticalHitRadius;
            Knockback = Profile.Knockback;
            CriticalKnockback = Profile.CriticalKnockback;
            SpreadDegrees = Profile.SpreadDegrees;
            HealAmount = Profile.HealAmount;
            IsActive = Profile.StartsActive;
            IsEvolved = false;
            ExplosionDamageMultiplier = 0f;
            ExplosionRadius = 0f;
        }

        public Vector2 CalculateProjectileDirection(Vector2 baseDirection, int projectileIndex)
        {
            var normalized = baseDirection.sqrMagnitude > 0.0001f
                ? baseDirection.normalized
                : Vector2.right;
            var offsetDegrees = (projectileIndex - (Count - 1) * 0.5f) * SpreadDegrees;
            var radians = offsetDegrees * Mathf.Deg2Rad;
            var cosine = Mathf.Cos(radians);
            var sine = Mathf.Sin(radians);
            return new Vector2(
                normalized.x * cosine - normalized.y * sine,
                normalized.x * sine + normalized.y * cosine);
        }

        public Vector2 CalculateRadialDirection(int burstIndex)
        {
            var step = 360f / Mathf.Max(1, RadialBurstCount);
            var radians = (burstIndex * step) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        public Vector2 CalculateOrbitPosition(Vector2 ownerPosition, int bladeIndex)
        {
            var step = 360f / Mathf.Max(1, Count);
            var radians = (OrbitAngle + bladeIndex * step) * Mathf.Deg2Rad;
            return ownerPosition + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * OrbitRadius;
        }

        public SharedEffectRequest CreateProjectileEffect(bool critical) => new SharedEffectRequest(
            SharedEffectKind.Projectile, SharedEffectTarget.Enemies, SharedEffectStacking.Independent,
            Damage * (critical ? 2f : 1f), critical ? CriticalHitRadius : HitRadius,
            critical ? CriticalKnockback : Knockback, ProjectileSpeed, ProjectileDuration, Pierce,
            critical, IsEvolved);

        public SharedEffectRequest CreatePulseEffect() => new SharedEffectRequest(
            SharedEffectKind.AreaDamage, SharedEffectTarget.Enemies, SharedEffectStacking.Independent,
            Damage, PulseRadius, PulseKnockback, evolved: IsEvolved);

        public SharedEffectRequest CreateOrbitEffect() => new SharedEffectRequest(
            SharedEffectKind.AreaDamage, SharedEffectTarget.Enemies, SharedEffectStacking.Independent,
            Damage, HitRadius, Knockback, evolved: IsEvolved);

        public float CalculatePulseHealing(int hitCount) =>
            IsEvolved && hitCount > 0
                ? Mathf.Min(PulseHealCap, hitCount * PulseHealPerHit)
                : 0f;
    }

    public sealed class SharedWeaponRegistry
    {
        private const string FrostAxeId = "weapon.frost_axe";
        private const string RavenGuardId = "weapon.raven_guard";
        private const float FrostAxeCriticalMultiplier = 2f;
        private const float MinimumAxeCooldown = 0.24f;
        private const float MinimumPulseCooldown = 1.6f;

        private readonly List<WeaponInstance> _equipped = new List<WeaponInstance>(8);
        private readonly List<WeaponFireEvent> _scratchEvents = new List<WeaponFireEvent>(8);
        private WeaponInstance _frostAxe;
        private WeaponInstance _ravenGuard;

        public IReadOnlyList<WeaponFireEvent> LastFireEvents => _scratchEvents;

        public float AxeDamage => _frostAxe != null ? _frostAxe.Damage : 24f;
        public float AxeCooldown => _frostAxe != null ? _frostAxe.Cooldown : 0.82f;
        public int AxeCount => _frostAxe != null ? _frostAxe.Count : 1;
        public int AxePierce => _frostAxe != null ? _frostAxe.Pierce : 1;
        public float CriticalChance => _frostAxe != null ? _frostAxe.CriticalChance : 0.08f;
        public bool HasShieldPulse => _ravenGuard != null && _ravenGuard.IsActive;
        public float ShieldDamage => _ravenGuard != null ? _ravenGuard.Damage : 20f;
        public float ShieldCooldown => _ravenGuard != null ? _ravenGuard.Cooldown : 5.2f;
        public bool FrostAxeEvolved => _frostAxe != null && _frostAxe.IsEvolved;
        public bool RavenGuardEvolved => _ravenGuard != null && _ravenGuard.IsEvolved;
        public float AxeProjectileSpeed => _frostAxe != null ? _frostAxe.ProjectileSpeed : 9.5f;
        public float AxeProjectileDuration => _frostAxe != null ? _frostAxe.ProjectileDuration : 3.2f;
        public float AxeHitRadius => _frostAxe != null ? _frostAxe.HitRadius : 0.25f;
        public float CriticalAxeHitRadius => _frostAxe != null ? _frostAxe.CriticalHitRadius : 0.32f;
        public float AxeKnockback => _frostAxe != null ? _frostAxe.Knockback : 0.18f;
        public float CriticalAxeKnockback => _frostAxe != null ? _frostAxe.CriticalKnockback : 0.42f;
        public float CriticalDamageMultiplier => FrostAxeCriticalMultiplier;
        public float ShieldRadius => _ravenGuard != null
            ? _ravenGuard.PulseRadius
            : RavenGuardEvolved ? 3.35f : 2.55f;
        public float ShieldKnockback => _ravenGuard != null ? _ravenGuard.PulseKnockback : 0.72f;
        public float ShieldHealingPerHit => _ravenGuard != null ? _ravenGuard.PulseHealPerHit : 0f;
        public float ShieldHealingCap => _ravenGuard != null ? _ravenGuard.PulseHealCap : 0f;
        public float CleaverExplosionDamageMultiplier => _frostAxe != null ? _frostAxe.ExplosionDamageMultiplier : 0f;
        public float CleaverExplosionRadius => _frostAxe != null ? _frostAxe.ExplosionRadius : 0f;

        public void Begin()
        {
            _equipped.Clear();
            _scratchEvents.Clear();
            EquipWeapon(FrostAxeId);
            EquipWeapon(RavenGuardId);
            RefreshLegacyReferences();
        }

        public void SyncFromBuild(PlayerBuild build)
        {
            if (build == null)
            {
                throw new ArgumentNullException(nameof(build));
            }

            var desired = new HashSet<string>();
            for (var i = 0; i < build.Items.Count; i++)
            {
                var itemState = build.Items[i];
                var definition = ItemCatalog.Find(itemState.ItemId);
                if (definition == null || definition.Category != ItemCategory.Weapon)
                {
                    continue;
                }

                desired.Add(itemState.ItemId);
                if (FindInstance(itemState.ItemId) == null)
                {
                    EquipWeapon(itemState.ItemId);
                }
            }

            for (var i = _equipped.Count - 1; i >= 0; i--)
            {
                if (desired.Contains(_equipped[i].WeaponId))
                {
                    continue;
                }

                _equipped.RemoveAt(i);
            }

            RefreshLegacyReferences();
        }

        public WeaponInstance FindInstance(string weaponId)
        {
            for (var i = 0; i < _equipped.Count; i++)
            {
                if (_equipped[i].WeaponId == weaponId)
                {
                    return _equipped[i];
                }
            }

            return null;
        }

        public IReadOnlyList<WeaponInstance> EquippedWeapons => _equipped;

        public void ApplyMastery(int mastery)
        {
            if (_frostAxe == null)
            {
                return;
            }

            _frostAxe.Damage *= 1f + Mathf.Min(0.15f, Mathf.Max(0, mastery) * 0.005f);
        }

        public IReadOnlyList<WeaponFireEvent> Advance(float deltaTime)
        {
            _scratchEvents.Clear();
            if (deltaTime <= 0f)
            {
                return _scratchEvents;
            }

            for (var i = 0; i < _equipped.Count; i++)
            {
                var weapon = _equipped[i];
                if (!weapon.IsActive)
                {
                    continue;
                }

                if (weapon.Behavior == WeaponBehaviorKind.OrbitBlade)
                {
                    weapon.OrbitAngle += weapon.OrbitSpeedDegrees * deltaTime;
                    weapon.Timer -= deltaTime;
                    if (weapon.Timer <= 0f)
                    {
                        weapon.Timer = weapon.Cooldown;
                        _scratchEvents.Add(new WeaponFireEvent(weapon.WeaponId, weapon.Behavior));
                    }

                    continue;
                }

                weapon.Timer -= deltaTime;
                if (weapon.Timer > 0f)
                {
                    continue;
                }

                weapon.Timer = weapon.Cooldown;
                _scratchEvents.Add(new WeaponFireEvent(weapon.WeaponId, weapon.Behavior));
            }

            return _scratchEvents;
        }

        public WeaponAdvanceResult MapLegacyAdvanceResult()
        {
            var result = WeaponAdvanceResult.None;
            for (var i = 0; i < _scratchEvents.Count; i++)
            {
                var fireEvent = _scratchEvents[i];
                if (fireEvent.WeaponId == FrostAxeId)
                {
                    result |= WeaponAdvanceResult.FireAxeVolley;
                }

                if (fireEvent.WeaponId == RavenGuardId)
                {
                    result |= WeaponAdvanceResult.TriggerRavenGuard;
                }
            }

            return result;
        }

        public bool ApplyUpgrade(string weaponId, UpgradeId upgradeId)
        {
            var weapon = FindInstance(weaponId);
            if (weapon == null)
            {
                return false;
            }

            return ApplyUpgradeToInstance(weapon, upgradeId);
        }

        public bool ApplyUpgrade(UpgradeId upgradeId)
        {
            var weaponId = ResolveLegacyWeaponId(upgradeId);
            if (string.IsNullOrEmpty(weaponId))
            {
                return false;
            }

            return ApplyUpgrade(weaponId, upgradeId);
        }

        public bool ApplyEvolution(string evolutionId)
        {
            if (evolutionId == "evolution.jotunn_cleaver" && _frostAxe != null && !_frostAxe.IsEvolved)
            {
                _frostAxe.IsEvolved = true;
                _frostAxe.Damage *= 1.55f;
                _frostAxe.Pierce += 2;
                _frostAxe.ExplosionDamageMultiplier = 0.42f;
                _frostAxe.ExplosionRadius = 1.25f;
                return true;
            }

            if (evolutionId == "evolution.storm_aegis" && _ravenGuard != null && !_ravenGuard.IsEvolved)
            {
                _ravenGuard.IsEvolved = true;
                _ravenGuard.Damage *= 1.35f;
                _ravenGuard.PulseRadius = 3.35f;
                _ravenGuard.PulseHealPerHit = 0.65f;
                _ravenGuard.PulseHealCap = 9f;
                return true;
            }

            if (evolutionId == "evolution.oath_maelstrom")
            {
                var weapon = FindInstance("weapon.oath_ring");
                if (weapon == null || weapon.IsEvolved)
                {
                    return false;
                }

                weapon.IsEvolved = true;
                weapon.Count = Mathf.Min(6, weapon.Count + 1);
                weapon.OrbitRadius *= 1.25f;
                return true;
            }

            return false;
        }

        public Vector2 CalculateAxeDirection(Vector2 baseDirection, int projectileIndex)
        {
            if (_frostAxe == null)
            {
                return Vector2.right;
            }

            return _frostAxe.CalculateProjectileDirection(baseDirection, projectileIndex);
        }

        public SharedEffectRequest CreateAxeEffect(bool critical)
        {
            if (_frostAxe == null)
            {
                return new SharedEffectRequest(
                    SharedEffectKind.Projectile, SharedEffectTarget.Enemies, SharedEffectStacking.Independent,
                    24f * (critical ? 2f : 1f), critical ? 0.32f : 0.25f,
                    critical ? 0.42f : 0.18f, 9.5f, 3.2f, 1, critical, false);
            }

            return _frostAxe.CreateProjectileEffect(critical);
        }

        public SharedEffectRequest CreateRavenGuardEffect()
        {
            if (_ravenGuard == null)
            {
                return new SharedEffectRequest(
                    SharedEffectKind.AreaDamage, SharedEffectTarget.Enemies, SharedEffectStacking.Independent,
                    20f, 2.55f, 0.72f);
            }

            return _ravenGuard.CreatePulseEffect();
        }

        public float CalculateRavenGuardHealing(int hitCount) =>
            _ravenGuard != null ? _ravenGuard.CalculatePulseHealing(hitCount) : 0f;

        private void EquipWeapon(string weaponId)
        {
            if (!WeaponProfile.TryGet(weaponId, out var profile))
            {
                return;
            }

            if (FindInstance(weaponId) != null)
            {
                return;
            }

            _equipped.Add(new WeaponInstance(profile));
        }

        private void RefreshLegacyReferences()
        {
            _frostAxe = FindInstance(FrostAxeId);
            _ravenGuard = FindInstance(RavenGuardId);
        }

        private static string ResolveLegacyWeaponId(UpgradeId upgradeId)
        {
            switch (upgradeId)
            {
                case UpgradeId.AxeDamage:
                case UpgradeId.AxeSpeed:
                case UpgradeId.ExtraAxe:
                case UpgradeId.AxePierce:
                case UpgradeId.CriticalRunes:
                    return FrostAxeId;
                case UpgradeId.ShieldPulse:
                case UpgradeId.ShieldDamage:
                case UpgradeId.ShieldDamageAndSpeed:
                    return RavenGuardId;
                case UpgradeId.OrbitDamage:
                case UpgradeId.OrbitSpeed:
                case UpgradeId.ExtraOrbit:
                    return "weapon.oath_ring";
                case UpgradeId.RadialDamage:
                case UpgradeId.RadialSpeed:
                case UpgradeId.ExtraRadial:
                    return null;
                default:
                    return null;
            }
        }

        private static bool ApplyUpgradeToInstance(WeaponInstance weapon, UpgradeId upgradeId)
        {
            switch (upgradeId)
            {
                case UpgradeId.AxeDamage:
                    weapon.Damage *= 1.26f;
                    return true;
                case UpgradeId.AxeSpeed:
                    weapon.Cooldown = Mathf.Max(MinimumAxeCooldown, weapon.Cooldown * 0.86f);
                    return true;
                case UpgradeId.ExtraAxe:
                    weapon.Count = Mathf.Min(5, weapon.Count + 1);
                    return true;
                case UpgradeId.AxePierce:
                    weapon.Pierce += 1;
                    return true;
                case UpgradeId.ShieldPulse:
                    weapon.IsActive = true;
                    weapon.Timer = 0.1f;
                    return true;
                case UpgradeId.ShieldDamage:
                    weapon.Damage *= 1.42f;
                    return true;
                case UpgradeId.ShieldDamageAndSpeed:
                    weapon.Damage *= 1.42f;
                    weapon.Cooldown = Mathf.Max(MinimumPulseCooldown, weapon.Cooldown * 0.86f);
                    return true;
                case UpgradeId.CriticalRunes:
                    if (weapon.Behavior != WeaponBehaviorKind.ProjectileVolley)
                    {
                        return false;
                    }

                    weapon.CriticalChance = Mathf.Min(0.55f, weapon.CriticalChance + 0.09f);
                    return true;
                case UpgradeId.OrbitDamage:
                    if (weapon.Behavior != WeaponBehaviorKind.OrbitBlade)
                    {
                        return false;
                    }

                    weapon.Damage *= 1.3f;
                    return true;
                case UpgradeId.OrbitSpeed:
                    if (weapon.Behavior != WeaponBehaviorKind.OrbitBlade)
                    {
                        return false;
                    }

                    weapon.OrbitSpeedDegrees *= 1.2f;
                    weapon.Cooldown = Mathf.Max(0.2f, weapon.Cooldown * 0.86f);
                    return true;
                case UpgradeId.ExtraOrbit:
                    if (weapon.Behavior != WeaponBehaviorKind.OrbitBlade)
                    {
                        return false;
                    }

                    weapon.Count = Mathf.Min(6, weapon.Count + 1);
                    return true;
                case UpgradeId.RadialDamage:
                    if (weapon.Behavior != WeaponBehaviorKind.RadialBurst)
                    {
                        return false;
                    }

                    weapon.Damage *= 1.28f;
                    return true;
                case UpgradeId.RadialSpeed:
                    if (weapon.Behavior != WeaponBehaviorKind.RadialBurst)
                    {
                        return false;
                    }

                    weapon.Cooldown = Mathf.Max(1.2f, weapon.Cooldown * 0.86f);
                    return true;
                case UpgradeId.ExtraRadial:
                    if (weapon.Behavior != WeaponBehaviorKind.RadialBurst)
                    {
                        return false;
                    }

                    weapon.RadialBurstCount = Mathf.Min(16, weapon.RadialBurstCount + 1);
                    return true;
                default:
                    return false;
            }
        }
    }
}
