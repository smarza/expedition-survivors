using UnityEngine;

namespace ProjectExpedition
{
    public sealed class Enemy : MonoBehaviour, IPoolableComponent
    {
        public bool Alive { get; private set; }
        public float Radius { get; private set; }
        public int ExperienceValue { get; private set; }
        public Vector2 Position => transform.position;

        private GameDirector _director;
        private float _health;
        private float _speed;
        private float _contactDamage;
        private float _contactTimer;
        private bool _boss;
        private SpriteRenderer _renderer;
        private GameObject _crown;
        private Color _baseColor;

        private void Awake()
        {
            _renderer = gameObject.GetComponent<SpriteRenderer>();
            if (_renderer == null) _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = RuntimeAssets.Circle;

            _crown = new GameObject("Jotunn Rune");
            _crown.transform.SetParent(transform, false);
            _crown.transform.localPosition = Vector3.up * 0.45f;
            _crown.transform.localScale = Vector3.one * 0.35f;
            var rune = _crown.AddComponent<SpriteRenderer>();
            rune.sprite = RuntimeAssets.Diamond;
            rune.color = new Color(1f, 0.32f, 0.24f);
            rune.sortingOrder = 9;
            _crown.SetActive(false);
        }

        public void Initialize(GameDirector director, float difficulty, bool boss)
        {
            _director = director;
            _boss = boss;
            var definition = boss ? EnemyCatalog.Jotunn : EnemyCatalog.Draugr;
            gameObject.name = definition.Name;
            Alive = true;
            _health = definition.BaseHealth + difficulty * definition.HealthPerDifficulty;
            _speed = director.Rng.Range(definition.MinimumSpeed, definition.MaximumSpeed) + difficulty * definition.SpeedPerDifficulty;
            _contactDamage = definition.BaseContactDamage + difficulty * definition.ContactDamagePerDifficulty;
            Radius = director.Rng.Range(definition.MinimumRadius, definition.MaximumRadius);
            ExperienceValue = director.Rng.Range(definition.MinimumExperience, definition.MaximumExperienceExclusive);
            _contactTimer = 0f;
            transform.localScale = Vector3.one * Radius * 2f;
            _baseColor = boss || !director.Rng.Chance(0.5f) ? definition.PrimaryColor : definition.AlternateColor;
            _renderer.color = _baseColor;
            _renderer.sortingOrder = boss ? 8 : 5;
            _crown.SetActive(boss);
        }

        private void Update()
        {
            if (!Alive || _director == null || _director.State != RunState.Playing) return;
            var target = _director.GetNearestLivingPlayer(Position);
            if (target == null) return;
            var delta = (Vector2)target.transform.position - Position;
            if (delta.sqrMagnitude > 0.001f)
            {
                transform.position += (Vector3)(delta.normalized * _speed * Time.deltaTime);
                _director.UpdateEnemySpatial(this);
            }
            transform.Rotate(0f, 0f, (_boss ? 16f : 36f) * Time.deltaTime);

            _contactTimer -= Time.deltaTime;
            var hitRange = Radius + 0.38f;
            if (delta.sqrMagnitude <= hitRange * hitRange && _contactTimer <= 0f)
            {
                target.TakeDamage(_contactDamage);
                _contactTimer = 0.7f;
            }
        }

        public void TakeDamage(float amount, float knockback, Vector2 source)
        {
            if (!Alive) return;
            _health -= amount;
            if (knockback > 0f)
            {
                var away = Position - source;
                if (away.sqrMagnitude > 0.001f)
                {
                    transform.position += (Vector3)(away.normalized * knockback);
                    _director.UpdateEnemySpatial(this);
                }
            }
            _renderer.color = Color.white;
            CancelInvoke(nameof(RestoreColor));
            Invoke(nameof(RestoreColor), 0.07f);
            if (_health <= 0f) Die();
        }

        private void RestoreColor()
        {
            if (_renderer != null)
                _renderer.color = _baseColor;
        }

        private void Die()
        {
            Alive = false;
            _director.OnEnemyKilled(this, ExperienceValue, _boss);
        }

        public void OnReleasedToPool()
        {
            Alive = false;
            CancelInvoke();
            _director = null;
            _health = 0f;
            _contactTimer = 0f;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
            if (_crown != null) _crown.SetActive(false);
        }
    }
}
