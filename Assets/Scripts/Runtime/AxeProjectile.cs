using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public sealed class AxeProjectile : MonoBehaviour, IPoolableComponent
    {
        private GameDirector _director;
        private PlayerController _owner;
        private readonly SharedProjectileModel _flight = new SharedProjectileModel();
        private readonly HashSet<Enemy> _hits = new HashSet<Enemy>();
        private readonly List<Enemy> _nearby = new List<Enemy>(24);
        private SharedProjectileBehavior _behavior;
        private float _trailTimer;
        private float _healTrailTimer;
        private float _knockback;
        private float _criticalKnockback;
        private int _remainingChainHits;
        private bool _returningToOwner;

        private void Awake()
        {
            _renderer = gameObject.GetComponent<SpriteRenderer>();
            if (_renderer == null)
            {
                _renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            _renderer.sprite = RuntimeAssets.Diamond;
            _renderer.sortingOrder = 15;
        }

        private SpriteRenderer _renderer;

        public void Initialize(
            GameDirector director,
            Vector2 direction,
            SharedEffectRequest effect,
            SharedProjectileBehavior behavior = default,
            PlayerController owner = null)
        {
            _director = director;
            _owner = owner;
            _behavior = behavior;
            _flight.Begin(
                transform.position,
                direction,
                effect.Damage,
                effect.Speed,
                effect.Pierce,
                effect.Critical,
                effect.Evolved,
                effect.Duration,
                effect.Critical ? effect.Radius : effect.Radius);
            _knockback = effect.Knockback;
            _criticalKnockback = effect.Knockback;
            _hits.Clear();
            _trailTimer = 0f;
            _healTrailTimer = 0f;
            _remainingChainHits = behavior.ChainHits;
            _returningToOwner = false;
            transform.localScale = new Vector3(0.62f, 0.24f, 1f) * (effect.Critical ? 1.25f : 1f);
            gameObject.name = effect.Critical ? "Critical Projectile" : "Projectile";
            _renderer.color = effect.Critical ? new Color(1f, 0.82f, 0.25f) : new Color(0.42f, 0.91f, 1f);
        }

        private void Update()
        {
            if (_director == null || _director.State != RunState.Playing)
            {
                return;
            }

            UpdateReturningFlight();
            _flight.Advance(Time.deltaTime, _director.ObstacleLayout.Obstacles);
            transform.position = _flight.Position;
            transform.Rotate(0f, 0f, 740f * Time.deltaTime);
            _trailTimer -= Time.deltaTime;
            if (_trailTimer <= 0f)
            {
                _trailTimer = 0.075f;
                _director.Present(PresentationCue.ProjectileTrail, transform.position, _renderer.color, 0.18f);
            }

            ApplyHealTrail();
            ResolveEnemyHits();

            if (!_flight.Active && _director != null)
            {
                _director.ReleaseProjectile(this);
            }
        }

        private void UpdateReturningFlight()
        {
            if (!_behavior.ReturnsToOwner || _owner == null)
            {
                return;
            }

            if (!_returningToOwner && _flight.RemainingLifetime <= _flight.TotalLifetime * 0.45f)
            {
                BeginReturnFlight();
            }

            if (!_returningToOwner)
            {
                return;
            }

            var ownerPosition = (Vector2)_owner.transform.position;
            _flight.SetDirection(ownerPosition - _flight.Position);

            var distanceToOwner = (ownerPosition - _flight.Position).sqrMagnitude;
            if (distanceToOwner <= 0.35f * 0.35f)
            {
                _flight.Stop();
            }
        }

        private void BeginReturnFlight()
        {
            _returningToOwner = true;
            _flight.ReactivateForReturn();
            var ownerPosition = (Vector2)_owner.transform.position;
            _flight.SetDirection(ownerPosition - _flight.Position);
        }

        private void ApplyHealTrail()
        {
            if (_behavior.HealTrailAmount <= 0f || _owner == null)
            {
                return;
            }

            _healTrailTimer -= Time.deltaTime;
            if (_healTrailTimer > 0f)
            {
                return;
            }

            _healTrailTimer = _behavior.HealTrailInterval;
            _owner.Heal(_behavior.HealTrailAmount);
        }

        private void ResolveEnemyHits()
        {
            _director.GetEnemiesInRadius(transform.position, _flight.Radius + 0.9f, _nearby);
            for (var i = _nearby.Count - 1; i >= 0; i--)
            {
                var enemy = _nearby[i];
                if (enemy == null || !enemy.Alive || _hits.Contains(enemy))
                {
                    continue;
                }

                if (!_flight.Overlaps(enemy.Position, enemy.Radius))
                {
                    continue;
                }

                _hits.Add(enemy);
                var knockback = _flight.Critical ? _criticalKnockback : _knockback;
                enemy.TakeDamage(_flight.Damage, knockback, transform.position);
                _director.Present(PresentationCue.Impact, enemy.Position, _renderer.color,
                    _flight.Critical ? 0.8f : 0.45f);
                ApplyEvolvedHitEffects(enemy);
                TryChainToNearbyEnemies(enemy);
                _flight.RegisterHit();

                if (!_flight.Active && _behavior.ReturnsToOwner && _owner != null)
                {
                    BeginReturnFlight();
                }

                if (!_flight.Active)
                {
                    _director.ReleaseProjectile(this);
                    return;
                }
            }
        }

        private void ApplyEvolvedHitEffects(Enemy enemy)
        {
            if (!_flight.Evolved)
            {
                return;
            }

            if (_flight.Critical && _behavior.CritExplosionRadius > 0f)
            {
                _director.ResolveAreaEffect(
                    enemy.Position,
                    SharedEffectPipeline.CreateNorthGaleCritExplosion(_flight.Damage));
                return;
            }

            if (_behavior.ChainHits > 0 || _behavior.HealTrailAmount > 0f)
            {
                return;
            }

            _director.ResolveAreaEffect(
                enemy.Position,
                SharedEffectPipeline.CreateJotunnCleaverExplosion(_flight.Damage));
        }

        private void TryChainToNearbyEnemies(Enemy sourceEnemy)
        {
            if (_remainingChainHits <= 0)
            {
                return;
            }

            _director.GetEnemiesInRadius(sourceEnemy.Position, _behavior.ChainRadius + 0.9f, _nearby);
            for (var i = 0; i < _nearby.Count && _remainingChainHits > 0; i++)
            {
                var enemy = _nearby[i];
                if (enemy == null || !enemy.Alive || enemy == sourceEnemy || _hits.Contains(enemy))
                {
                    continue;
                }

                if ((enemy.Position - sourceEnemy.Position).sqrMagnitude >
                    (_behavior.ChainRadius + enemy.Radius) * (_behavior.ChainRadius + enemy.Radius))
                {
                    continue;
                }

                _hits.Add(enemy);
                var chainEffect = SharedEffectPipeline.CreateSignalStormChain(_flight.Damage);
                enemy.TakeDamage(chainEffect.Damage, chainEffect.Knockback, sourceEnemy.Position);
                _director.Present(PresentationCue.Impact, enemy.Position, _renderer.color, 0.55f);
                _remainingChainHits--;
            }
        }

        public void OnReleasedToPool()
        {
            _director = null;
            _owner = null;
            _behavior = default;
            _flight.Stop();
            _trailTimer = 0f;
            _healTrailTimer = 0f;
            _hits.Clear();
            _nearby.Clear();
            _remainingChainHits = 0;
            _returningToOwner = false;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
