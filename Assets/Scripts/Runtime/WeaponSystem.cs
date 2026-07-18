using UnityEngine;

namespace ProjectExpedition
{
    /// <summary>
    /// Local presentation adapter for the shared automatic-weapon model.
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
        public bool FrostAxeEvolved => Model.FrostAxeEvolved;
        public bool RavenGuardEvolved => Model.RavenGuardEvolved;

        internal SharedWeaponModel Model { get; } = new SharedWeaponModel();

        private readonly GameDirector _director;
        private readonly PlayerController _owner;

        public WeaponSystem(GameDirector director, PlayerController owner)
        {
            _director = director;
            _owner = owner;
        }

        public void ApplyMastery(int mastery) => Model.ApplyMastery(mastery);

        public void Tick(float deltaTime)
        {
            var triggers = Model.Advance(deltaTime);
            if ((triggers & WeaponAdvanceResult.FireAxeVolley) != 0) ThrowAxes();
            if ((triggers & WeaponAdvanceResult.TriggerRavenGuard) == 0) return;

            var effect = Model.CreateRavenGuardEffect();
            var hits = _director.ResolveAreaEffect(_owner.transform.position, effect);
            var healing = Model.CalculateRavenGuardHealing(hits);
            if (healing > 0f) _owner.Heal(healing);
            _director.ShowPulse(_owner.transform.position, _owner.PlayerIndex);
        }

        private void ThrowAxes()
        {
            var nearest = _director.GetNearestEnemy(_owner.transform.position);
            if (nearest == null) return;
            var baseDirection = nearest.Position - (Vector2)_owner.transform.position;
            for (var i = 0; i < Model.AxeCount; i++)
            {
                var critical = _director.Rng.Chance(Model.CriticalChance);
                var effect = Model.CreateAxeEffect(critical);
                var axe = _director.SpawnProjectile(_owner.transform.position);
                axe.Initialize(_director, Model.CalculateAxeDirection(baseDirection, i), effect);
            }
        }
    }
}
