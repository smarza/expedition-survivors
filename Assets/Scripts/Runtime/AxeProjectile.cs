using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public sealed class AxeProjectile : MonoBehaviour, IPoolableComponent
    {
        private GameDirector _director;
        private Vector2 _direction;
        private float _damage;
        private float _speed;
        private float _life;
        private int _pierce;
        private float _radius;
        private bool _critical;
        private bool _evolved;
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

        public void Initialize(GameDirector director, Vector2 direction, float damage, float speed, int pierce, bool critical, bool evolved = false)
        {
            _director = director;
            _direction = direction.normalized;
            _damage = damage;
            _speed = speed;
            _pierce = pierce;
            _critical = critical;
            _evolved = evolved;
            _life = 3.2f;
            _radius = critical ? 0.32f : 0.25f;
            _hits.Clear();
            transform.localScale = new Vector3(0.62f, 0.24f, 1f) * (critical ? 1.25f : 1f);
            gameObject.name = critical ? "Critical Frost Axe" : "Frost Axe";
            _renderer.color = critical ? new Color(1f, 0.82f, 0.25f) : new Color(0.42f, 0.91f, 1f);
        }

        private void Update()
        {
            if (_director == null || _director.State != RunState.Playing) return;
            transform.position += (Vector3)(_direction * _speed * Time.deltaTime);
            transform.Rotate(0f, 0f, 740f * Time.deltaTime);
            _life -= Time.deltaTime;

            _director.GetEnemiesInRadius(transform.position, _radius + 0.9f, _nearby);
            for (var i = _nearby.Count - 1; i >= 0; i--)
            {
                var enemy = _nearby[i];
                if (enemy == null || !enemy.Alive || _hits.Contains(enemy)) continue;
                var range = _radius + enemy.Radius;
                if (((Vector2)transform.position - enemy.Position).sqrMagnitude > range * range) continue;
                _hits.Add(enemy);
                enemy.TakeDamage(_damage, _critical ? 0.42f : 0.18f, transform.position);
                if (_evolved) _director.DamageEnemiesInRadius(enemy.Position, 1.25f, _damage * 0.42f, 0.3f);
                _pierce--;
                if (_pierce < 0) { _director.ReleaseProjectile(this); return; }
            }
            if (_life <= 0f) _director.ReleaseProjectile(this);
        }

        public void OnReleasedToPool()
        {
            _director = null;
            _life = 0f;
            _hits.Clear();
            _nearby.Clear();
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
