using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public sealed class AxeProjectile : MonoBehaviour, IPoolableComponent
    {
        private GameDirector _director;
        private readonly SharedProjectileModel _flight = new SharedProjectileModel();
        private readonly HashSet<Enemy> _hits = new HashSet<Enemy>();
        private readonly List<Enemy> _nearby = new List<Enemy>(24);
        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = gameObject.GetComponent<SpriteRenderer>();
            if (_renderer == null) _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = RuntimeAssets.Diamond;
            _renderer.sortingOrder = 15;
        }

        public void Initialize(GameDirector director, Vector2 direction, SharedEffectRequest effect)
        {
            _director = director;
            _flight.Begin(transform.position, direction, effect.Damage, effect.Speed, effect.Pierce,
                effect.Critical, effect.Evolved, effect.Duration);
            _hits.Clear();
            transform.localScale = new Vector3(0.62f, 0.24f, 1f) * (effect.Critical ? 1.25f : 1f);
            gameObject.name = effect.Critical ? "Critical Frost Axe" : "Frost Axe";
            _renderer.color = effect.Critical ? new Color(1f, 0.82f, 0.25f) : new Color(0.42f, 0.91f, 1f);
        }

        private void Update()
        {
            if (_director == null || _director.State != RunState.Playing) return;
            _flight.Advance(Time.deltaTime);
            transform.position = _flight.Position;
            transform.Rotate(0f, 0f, 740f * Time.deltaTime);

            _director.GetEnemiesInRadius(transform.position, _flight.Radius + 0.9f, _nearby);
            for (var i = _nearby.Count - 1; i >= 0; i--)
            {
                var enemy = _nearby[i];
                if (enemy == null || !enemy.Alive || _hits.Contains(enemy)) continue;
                if (!_flight.Overlaps(enemy.Position, enemy.Radius)) continue;
                _hits.Add(enemy);
                enemy.TakeDamage(_flight.Damage, _flight.Critical ? 0.42f : 0.18f, transform.position);
                if (_flight.Evolved)
                    _director.ResolveAreaEffect(enemy.Position,
                        SharedEffectPipeline.CreateJotunnCleaverExplosion(_flight.Damage));
                _flight.RegisterHit();
                if (!_flight.Active) { _director.ReleaseProjectile(this); return; }
            }
            if (!_flight.Active) _director.ReleaseProjectile(this);
        }

        public void OnReleasedToPool()
        {
            _director = null;
            _flight.Stop();
            _hits.Clear();
            _nearby.Clear();
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
