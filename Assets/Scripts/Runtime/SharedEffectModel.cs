using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public enum SharedEffectKind : byte
    {
        Projectile,
        AreaDamage,
        Heal,
        PersistentZone
    }

    public enum SharedEffectTarget : byte
    {
        Enemies,
        Owner
    }

    public enum SharedEffectStacking : byte
    {
        Independent,
        Refresh
    }

    public readonly struct SharedEffectRequest
    {
        public readonly SharedEffectKind Kind;
        public readonly SharedEffectTarget Target;
        public readonly SharedEffectStacking Stacking;
        public readonly float Damage;
        public readonly float Radius;
        public readonly float Knockback;
        public readonly float Speed;
        public readonly float Duration;
        public readonly int Pierce;
        public readonly bool Critical;
        public readonly bool Evolved;

        public SharedEffectRequest(SharedEffectKind kind, SharedEffectTarget target,
            SharedEffectStacking stacking, float damage, float radius, float knockback,
            float speed = 0f, float duration = 0f, int pierce = 0, bool critical = false,
            bool evolved = false)
        {
            Kind = kind;
            Target = target;
            Stacking = stacking;
            Damage = Mathf.Max(0f, damage);
            Radius = Mathf.Max(0f, radius);
            Knockback = Mathf.Max(0f, knockback);
            Speed = Mathf.Max(0f, speed);
            Duration = Mathf.Max(0f, duration);
            Pierce = Mathf.Max(0, pierce);
            Critical = critical;
            Evolved = evolved;
        }
    }

    [Flags]
    public enum WeaponAdvanceResult : byte
    {
        None = 0,
        FireAxeVolley = 1,
        TriggerRavenGuard = 2
    }

    public readonly struct SharedProjectileBehavior
    {
        public readonly string WeaponId;
        public readonly int ChainHits;
        public readonly float ChainRadius;
        public readonly float ChainDamageMultiplier;
        public readonly bool ReturnsToOwner;
        public readonly int OwnerPlayerIndex;
        public readonly float CritExplosionRadius;
        public readonly float CritExplosionMultiplier;
        public readonly float HealTrailAmount;
        public readonly float HealTrailInterval;

        public SharedProjectileBehavior(
            string weaponId = null,
            int chainHits = 0,
            float chainRadius = 0f,
            float chainDamageMultiplier = 0f,
            bool returnsToOwner = false,
            int ownerPlayerIndex = 0,
            float critExplosionRadius = 0f,
            float critExplosionMultiplier = 0f,
            float healTrailAmount = 0f,
            float healTrailInterval = 0f)
        {
            WeaponId = weaponId;
            ChainHits = chainHits;
            ChainRadius = chainRadius;
            ChainDamageMultiplier = chainDamageMultiplier;
            ReturnsToOwner = returnsToOwner;
            OwnerPlayerIndex = ownerPlayerIndex;
            CritExplosionRadius = critExplosionRadius;
            CritExplosionMultiplier = critExplosionMultiplier;
            HealTrailAmount = healTrailAmount;
            HealTrailInterval = healTrailInterval;
        }
    }

    public sealed class SharedPersistentZoneInstance
    {
        public Vector2 Position;
        public float Radius;
        public float DamagePerTick;
        public float Knockback;
        public float RemainingDuration;
        public float TickInterval;
        public float TickTimer;
        public bool FollowOwner;
        public int OwnerPlayerIndex;
    }

    public sealed class SharedPersistentZoneModel
    {
        private readonly List<SharedPersistentZoneInstance> _zones = new List<SharedPersistentZoneInstance>(16);

        public IReadOnlyList<SharedPersistentZoneInstance> ActiveZones => _zones;

        public void Clear() => _zones.Clear();

        public void Spawn(
            Vector2 position,
            float radius,
            float damagePerTick,
            float knockback,
            float duration,
            float tickInterval,
            bool followOwner,
            int ownerPlayerIndex)
        {
            _zones.Add(new SharedPersistentZoneInstance
            {
                Position = position,
                Radius = radius,
                DamagePerTick = damagePerTick,
                Knockback = knockback,
                RemainingDuration = duration,
                TickInterval = tickInterval,
                TickTimer = tickInterval,
                FollowOwner = followOwner,
                OwnerPlayerIndex = ownerPlayerIndex
            });
        }

        public void Advance(float deltaTime, Func<int, Vector2> resolveOwnerPosition)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            for (var i = _zones.Count - 1; i >= 0; i--)
            {
                var zone = _zones[i];
                zone.RemainingDuration -= deltaTime;

                if (zone.FollowOwner && resolveOwnerPosition != null)
                {
                    zone.Position = resolveOwnerPosition(zone.OwnerPlayerIndex);
                }

                zone.TickTimer -= deltaTime;
                if (zone.RemainingDuration <= 0f)
                {
                    _zones.RemoveAt(i);
                }
            }
        }

        public bool TryConsumeTick(int zoneIndex, out SharedEffectRequest effect, out Vector2 center)
        {
            effect = default;
            center = default;

            if (zoneIndex < 0 || zoneIndex >= _zones.Count)
            {
                return false;
            }

            var zone = _zones[zoneIndex];
            if (zone.TickTimer > 0f || zone.RemainingDuration <= 0f)
            {
                return false;
            }

            center = zone.Position;
            effect = SharedEffectPipeline.CreatePersistentZoneTick(zone.DamagePerTick, zone.Radius, zone.Knockback);
            zone.TickTimer = zone.TickInterval;
            return true;
        }
    }

    /// <summary>
    /// Presentation-free automatic-weapon facade. Delegates state, timing and upgrades
    /// to SharedWeaponRegistry while preserving the legacy public surface for tests.
    /// </summary>
    public sealed class SharedWeaponModel
    {
        internal SharedWeaponRegistry Registry { get; } = new SharedWeaponRegistry();

        public float AxeDamage => Registry.AxeDamage;
        public float AxeCooldown => Registry.AxeCooldown;
        public int AxeCount => Registry.AxeCount;
        public int AxePierce => Registry.AxePierce;
        public float CriticalChance => Registry.CriticalChance;
        public bool HasShieldPulse => Registry.HasShieldPulse;
        public float ShieldDamage => Registry.ShieldDamage;
        public float ShieldCooldown => Registry.ShieldCooldown;
        public bool FrostAxeEvolved => Registry.FrostAxeEvolved;
        public bool RavenGuardEvolved => Registry.RavenGuardEvolved;
        public float AxeProjectileSpeed => Registry.AxeProjectileSpeed;
        public float AxeProjectileDuration => Registry.AxeProjectileDuration;
        public float AxeHitRadius => Registry.AxeHitRadius;
        public float CriticalAxeHitRadius => Registry.CriticalAxeHitRadius;
        public float AxeKnockback => Registry.AxeKnockback;
        public float CriticalAxeKnockback => Registry.CriticalAxeKnockback;
        public float CriticalDamageMultiplier => Registry.CriticalDamageMultiplier;
        public float ShieldRadius => Registry.ShieldRadius;
        public float ShieldKnockback => Registry.ShieldKnockback;
        public float ShieldHealingPerHit => Registry.ShieldHealingPerHit;
        public float ShieldHealingCap => Registry.ShieldHealingCap;
        public float CleaverExplosionDamageMultiplier => Registry.CleaverExplosionDamageMultiplier;
        public float CleaverExplosionRadius => Registry.CleaverExplosionRadius;

        public SharedWeaponModel() => Begin();

        public void Begin() => Registry.Begin();

        public void SyncFromBuild(PlayerBuild build) => Registry.SyncFromBuild(build);

        public void ApplyMastery(int mastery) => Registry.ApplyMastery(mastery);

        public void ApplyHeroMastery(string characterId, int mastery) =>
            Registry.ApplyHeroMastery(characterId, mastery);

        public WeaponAdvanceResult Advance(float deltaTime)
        {
            Registry.Advance(deltaTime);
            return Registry.MapLegacyAdvanceResult();
        }

        public IReadOnlyList<WeaponFireEvent> LastFireEvents => Registry.LastFireEvents;

        public Vector2 CalculateAxeDirection(Vector2 baseDirection, int projectileIndex) =>
            Registry.CalculateAxeDirection(baseDirection, projectileIndex);

        public SharedEffectRequest CreateAxeEffect(bool critical) => Registry.CreateAxeEffect(critical);

        public SharedEffectRequest CreateRavenGuardEffect() => Registry.CreateRavenGuardEffect();

        public float CalculateRavenGuardHealing(int hitCount) =>
            Registry.CalculateRavenGuardHealing(hitCount);

        public bool ApplyUpgrade(UpgradeId id) => Registry.ApplyUpgrade(id);

        public bool ApplyUpgrade(string weaponId, UpgradeId id) => Registry.ApplyUpgrade(weaponId, id);

        public bool ApplyEvolution(string evolutionId) => Registry.ApplyEvolution(evolutionId);
    }

    public static class SharedEffectPipeline
    {
        public static bool ApplyUpgrade(SharedPlayerModel player, SharedWeaponModel weapons,
            UpgradeId id, string weaponId = null)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (weapons == null) throw new ArgumentNullException(nameof(weapons));

            if (!string.IsNullOrEmpty(weaponId))
            {
                if (weapons.ApplyUpgrade(weaponId, id)) return true;
            }
            else if (weapons.ApplyUpgrade(id))
            {
                return true;
            }

            switch (id)
            {
                case UpgradeId.MoveSpeed: player.AddMoveSpeed(0.46f); return true;
                case UpgradeId.MaxHealth: player.AddMaxHealth(24f); return true;
                case UpgradeId.Armor: player.AddArmor(1f); return true;
                case UpgradeId.Magnet: player.AddMagnet(0.55f); return true;
                case UpgradeId.UltimateCooldown: player.ImproveUltimateCooldown(); return true;
                case UpgradeId.UltimateDamage: player.ImproveUltimateDamage(); return true;
                case UpgradeId.Heal: player.Heal(24f); return true;
                case UpgradeId.SapRegen:
                    player.AddMaxHealth(18f);
                    player.AddHealthRegen(0.4f, 2f);
                    return true;
                case UpgradeId.None: return true;
                default: return false;
            }
        }

        public static SharedEffectRequest CreateUltimate(SharedPlayerModel player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            return new SharedEffectRequest(SharedEffectKind.AreaDamage, SharedEffectTarget.Enemies,
                SharedEffectStacking.Independent, player.UltimateDamage, player.UltimateRadius, 3.2f);
        }

        public static SharedEffectRequest CreateJotunnCleaverExplosion(float projectileDamage) =>
            new SharedEffectRequest(SharedEffectKind.AreaDamage, SharedEffectTarget.Enemies,
                SharedEffectStacking.Independent, projectileDamage * 0.42f, 1.25f, 0.3f);

        public static SharedEffectRequest CreatePersistentZoneTick(float damage, float radius, float knockback) =>
            new SharedEffectRequest(SharedEffectKind.PersistentZone, SharedEffectTarget.Enemies,
                SharedEffectStacking.Independent, damage, radius, knockback);

        public static SharedEffectRequest CreateNorthGaleCritExplosion(float projectileDamage) =>
            new SharedEffectRequest(SharedEffectKind.AreaDamage, SharedEffectTarget.Enemies,
                SharedEffectStacking.Independent, projectileDamage * 0.55f, 1.35f, 0.38f);

        public static SharedEffectRequest CreateSignalStormChain(float projectileDamage) =>
            new SharedEffectRequest(SharedEffectKind.AreaDamage, SharedEffectTarget.Enemies,
                SharedEffectStacking.Independent, projectileDamage * 0.65f, 0.28f, 0.22f);
    }
}
