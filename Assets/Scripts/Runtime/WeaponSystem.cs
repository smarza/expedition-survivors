using UnityEngine;

namespace ProjectExpedition
{
    public sealed class WeaponSystem
    {
        public float AxeDamage { get; private set; } = 24f;
        public float AxeCooldown { get; private set; } = 0.82f;
        public int AxeCount { get; private set; } = 1;
        public int AxePierce { get; private set; } = 1;
        public float CriticalChance { get; private set; } = 0.08f;
        public bool HasShieldPulse { get; private set; } = true;
        public float ShieldDamage { get; private set; } = 20f;
        public bool FrostAxeEvolved { get; private set; }
        public bool RavenGuardEvolved { get; private set; }

        private readonly GameDirector _director;
        private readonly PlayerController _owner;
        private float _axeTimer = 0.25f;
        private float _pulseTimer = 4.5f;

        public WeaponSystem(GameDirector director, PlayerController owner)
        {
            _director = director;
            _owner = owner;
        }

        public void ApplyMastery(int mastery)
        {
            AxeDamage *= 1f + Mathf.Min(0.15f, mastery * 0.005f);
        }

        public void Tick(float deltaTime)
        {
            _axeTimer -= deltaTime;
            _pulseTimer -= deltaTime;
            if (_axeTimer <= 0f)
            {
                ThrowAxes();
                _axeTimer = AxeCooldown;
            }
            if (HasShieldPulse && _pulseTimer <= 0f)
            {
                var radius = RavenGuardEvolved ? 3.35f : 2.55f;
                var hits = _director.DamageEnemiesInRadius(_owner.transform.position, radius, ShieldDamage, 0.72f);
                if (RavenGuardEvolved && hits > 0) _owner.Heal(Mathf.Min(9f, hits * 0.65f));
                _director.ShowPulse(_owner.transform.position, _owner.PlayerIndex);
                _pulseTimer = 5.2f;
            }
        }

        private void ThrowAxes()
        {
            var nearest = _director.GetNearestEnemy(_owner.transform.position);
            if (nearest == null) return;
            var baseDirection = (nearest.Position - (Vector2)_owner.transform.position).normalized;
            for (var i = 0; i < AxeCount; i++)
            {
                var offset = (i - (AxeCount - 1) * 0.5f) * 10f;
                var direction = Quaternion.Euler(0f, 0f, offset) * baseDirection;
                var critical = _director.Rng.Chance(CriticalChance);
                var axe = _director.SpawnProjectile(_owner.transform.position);
                axe.Initialize(_director, direction,
                    AxeDamage * (critical ? 2f : 1f), 9.5f, AxePierce, critical, FrostAxeEvolved);
            }
        }

        public void Apply(UpgradeId id)
        {
            switch (id)
            {
                case UpgradeId.AxeDamage: AxeDamage *= 1.26f; break;
                case UpgradeId.AxeSpeed: AxeCooldown = Mathf.Max(0.24f, AxeCooldown * 0.86f); break;
                case UpgradeId.ExtraAxe: AxeCount = Mathf.Min(5, AxeCount + 1); break;
                case UpgradeId.AxePierce: AxePierce += 1; break;
                case UpgradeId.ShieldPulse: HasShieldPulse = true; _pulseTimer = 0.1f; break;
                case UpgradeId.ShieldDamage: ShieldDamage *= 1.42f; break;
                case UpgradeId.CriticalRunes: CriticalChance = Mathf.Min(0.55f, CriticalChance + 0.09f); break;
            }
        }

        public void EvolveFrostAxe()
        {
            FrostAxeEvolved = true;
            AxeDamage *= 1.55f;
            AxePierce += 2;
        }

        public void EvolveRavenGuard()
        {
            RavenGuardEvolved = true;
            ShieldDamage *= 1.35f;
        }
    }
}
