using System;
using UnityEngine;

namespace ProjectExpedition
{
    public enum SharedEffectKind : byte
    {
        Projectile,
        AreaDamage,
        Heal
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

    /// <summary>
    /// Presentation-free automatic-weapon state. The adapter supplies targets,
    /// seeded critical rolls and presentation while this model owns timing,
    /// derived statistics, upgrades and evolution behavior.
    /// </summary>
    public sealed class SharedWeaponModel
    {
        private const float InitialAxeDelay = 0.25f;
        private const float InitialPulseDelay = 4.5f;
        private const float PulseCooldown = 5.2f;
        private const float ProjectileSpeed = 9.5f;
        private const float ProjectileDuration = 3.2f;

        private float _axeTimer;
        private float _pulseTimer;

        public float AxeDamage { get; private set; }
        public float AxeCooldown { get; private set; }
        public int AxeCount { get; private set; }
        public int AxePierce { get; private set; }
        public float CriticalChance { get; private set; }
        public bool HasShieldPulse { get; private set; }
        public float ShieldDamage { get; private set; }
        public bool FrostAxeEvolved { get; private set; }
        public bool RavenGuardEvolved { get; private set; }

        public SharedWeaponModel() => Begin();

        public void Begin()
        {
            AxeDamage = 24f;
            AxeCooldown = 0.82f;
            AxeCount = 1;
            AxePierce = 1;
            CriticalChance = 0.08f;
            HasShieldPulse = true;
            ShieldDamage = 20f;
            FrostAxeEvolved = false;
            RavenGuardEvolved = false;
            _axeTimer = InitialAxeDelay;
            _pulseTimer = InitialPulseDelay;
        }

        public void ApplyMastery(int mastery) =>
            AxeDamage *= 1f + Mathf.Min(0.15f, Mathf.Max(0, mastery) * 0.005f);

        public WeaponAdvanceResult Advance(float deltaTime)
        {
            if (deltaTime <= 0f) return WeaponAdvanceResult.None;
            _axeTimer -= deltaTime;
            _pulseTimer -= deltaTime;
            var result = WeaponAdvanceResult.None;

            if (_axeTimer <= 0f)
            {
                _axeTimer = AxeCooldown;
                result |= WeaponAdvanceResult.FireAxeVolley;
            }
            if (HasShieldPulse && _pulseTimer <= 0f)
            {
                _pulseTimer = PulseCooldown;
                result |= WeaponAdvanceResult.TriggerRavenGuard;
            }
            return result;
        }

        public Vector2 CalculateAxeDirection(Vector2 baseDirection, int projectileIndex)
        {
            var normalized = baseDirection.sqrMagnitude > 0.0001f
                ? baseDirection.normalized
                : Vector2.right;
            var offsetDegrees = (projectileIndex - (AxeCount - 1) * 0.5f) * 10f;
            var radians = offsetDegrees * Mathf.Deg2Rad;
            var cosine = Mathf.Cos(radians);
            var sine = Mathf.Sin(radians);
            return new Vector2(normalized.x * cosine - normalized.y * sine,
                normalized.x * sine + normalized.y * cosine);
        }

        public SharedEffectRequest CreateAxeEffect(bool critical) => new SharedEffectRequest(
            SharedEffectKind.Projectile, SharedEffectTarget.Enemies,
            SharedEffectStacking.Independent, AxeDamage * (critical ? 2f : 1f),
            critical ? 0.32f : 0.25f, critical ? 0.42f : 0.18f,
            ProjectileSpeed, ProjectileDuration, AxePierce, critical, FrostAxeEvolved);

        public SharedEffectRequest CreateRavenGuardEffect() => new SharedEffectRequest(
            SharedEffectKind.AreaDamage, SharedEffectTarget.Enemies,
            SharedEffectStacking.Independent, ShieldDamage,
            RavenGuardEvolved ? 3.35f : 2.55f, 0.72f, evolved: RavenGuardEvolved);

        public float CalculateRavenGuardHealing(int hitCount) =>
            RavenGuardEvolved && hitCount > 0 ? Mathf.Min(9f, hitCount * 0.65f) : 0f;

        public bool ApplyUpgrade(UpgradeId id)
        {
            switch (id)
            {
                case UpgradeId.AxeDamage: AxeDamage *= 1.26f; return true;
                case UpgradeId.AxeSpeed:
                    AxeCooldown = Mathf.Max(0.24f, AxeCooldown * 0.86f);
                    return true;
                case UpgradeId.ExtraAxe: AxeCount = Mathf.Min(5, AxeCount + 1); return true;
                case UpgradeId.AxePierce: AxePierce += 1; return true;
                case UpgradeId.ShieldPulse: HasShieldPulse = true; _pulseTimer = 0.1f; return true;
                case UpgradeId.ShieldDamage: ShieldDamage *= 1.42f; return true;
                case UpgradeId.CriticalRunes:
                    CriticalChance = Mathf.Min(0.55f, CriticalChance + 0.09f);
                    return true;
                default: return false;
            }
        }

        public bool ApplyEvolution(string evolutionId)
        {
            if (evolutionId == "evolution.jotunn_cleaver" && !FrostAxeEvolved)
            {
                FrostAxeEvolved = true;
                AxeDamage *= 1.55f;
                AxePierce += 2;
                return true;
            }
            if (evolutionId == "evolution.storm_aegis" && !RavenGuardEvolved)
            {
                RavenGuardEvolved = true;
                ShieldDamage *= 1.35f;
                return true;
            }
            return false;
        }
    }

    public static class SharedEffectPipeline
    {
        public static bool ApplyUpgrade(SharedPlayerModel player, SharedWeaponModel weapons,
            UpgradeId id)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (weapons == null) throw new ArgumentNullException(nameof(weapons));
            if (weapons.ApplyUpgrade(id)) return true;

            switch (id)
            {
                case UpgradeId.MoveSpeed: player.AddMoveSpeed(0.46f); return true;
                case UpgradeId.MaxHealth: player.AddMaxHealth(24f); return true;
                case UpgradeId.Armor: player.AddArmor(1f); return true;
                case UpgradeId.Magnet: player.AddMagnet(0.55f); return true;
                case UpgradeId.UltimateCooldown: player.ImproveUltimateCooldown(); return true;
                case UpgradeId.UltimateDamage: player.ImproveUltimateDamage(); return true;
                case UpgradeId.Heal: player.Heal(24f); return true;
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
    }
}
