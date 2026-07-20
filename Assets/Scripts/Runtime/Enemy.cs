using UnityEngine;

namespace ProjectExpedition
{
    public sealed class Enemy : MonoBehaviour, IPoolableComponent
    {
        public bool Alive => _model.Alive;
        public bool Boss => _model.Boss;
        public float Health => _model.Health;
        public float Speed => _model.Speed;
        public float ContactDamage => _model.ContactDamage;
        public float Radius => _model.Radius;
        public int ExperienceValue => _model.ExperienceValue;
        public Vector2 Position => _model.Position;

        private GameDirector _director;
        private readonly SharedEnemyModel _model = new SharedEnemyModel();
        private SpriteRenderer _renderer;
        private GameObject _crown;
        private Color _baseColor;
        private float _animationSeed;

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
            var definition = boss ? EnemyCatalog.Jotunn : EnemyCatalog.Draugr;
            gameObject.name = definition.Name;
            var rolledBaseSpeed = director.Rng.Range(definition.MinimumSpeed, definition.MaximumSpeed);
            var rolledRadius = director.Rng.Range(definition.MinimumRadius, definition.MaximumRadius);
            var rolledExperience = director.Rng.Range(definition.MinimumExperience, definition.MaximumExperienceExclusive);
            _model.Begin(transform.position, definition, difficulty, rolledBaseSpeed, rolledRadius,
                rolledExperience);
            transform.localScale = Vector3.one * Radius * 2f;
            _baseColor = boss || !director.Rng.Chance(0.5f) ? definition.PrimaryColor : definition.AlternateColor;
            _renderer.color = _baseColor;
            _renderer.sortingOrder = boss ? 8 : 5;
            _crown.SetActive(boss);
            _animationSeed = transform.position.x * 0.31f + transform.position.y * 0.17f;
        }

        private void Update()
        {
            if (!Alive || _director == null || _director.State != RunState.Playing) return;
            var target = _director.GetNearestLivingPlayer(Position);
            if (target == null) return;
            var result = _model.AdvanceTowards((Vector2)target.transform.position, 0.38f, Time.deltaTime);
            if ((result & EnemyAdvanceResult.Moved) != 0)
            {
                transform.position = (Vector3)Position;
                _director.UpdateEnemySpatial(this);
            }
            transform.Rotate(0f, 0f, (Boss ? 16f : 36f) * Time.deltaTime);
            var breathe = 1f + Mathf.Sin(Time.time * (Boss ? 2.2f : 4.1f) + _animationSeed) * (Boss ? 0.025f : 0.045f);
            transform.localScale = Vector3.one * Radius * 2f * breathe;

            if ((result & EnemyAdvanceResult.ContactTriggered) != 0)
                target.TakeDamage(ContactDamage);
        }

        public EnemyDamageResult TakeDamage(float amount, float knockback, Vector2 source)
        {
            var result = _model.TakeDamage(amount, knockback, source);
            if (result == EnemyDamageResult.Ignored) return result;
            if ((result & EnemyDamageResult.Moved) != 0)
            {
                transform.position = (Vector3)Position;
                _director.UpdateEnemySpatial(this);
            }
            _renderer.color = Color.white;
            CancelInvoke(nameof(RestoreColor));
            Invoke(nameof(RestoreColor), 0.07f);
            if ((result & EnemyDamageResult.Killed) != 0)
                _director.OnEnemyKilled(this, ExperienceValue, Boss);
            return result;
        }

        private void RestoreColor()
        {
            if (_renderer != null)
                _renderer.color = _baseColor;
        }

        public void OnReleasedToPool()
        {
            _model.Stop();
            CancelInvoke();
            _director = null;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
            if (_crown != null) _crown.SetActive(false);
        }
    }
}
