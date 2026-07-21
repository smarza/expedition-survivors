using UnityEngine;

namespace ProjectExpedition
{
    public sealed class Enemy : MonoBehaviour, IPoolableComponent
    {
        public bool Alive => _model.Alive;
        public bool Boss => _model.Boss;
        public bool Elite => _elite;
        public float Health => _model.Health;
        public float Speed => _model.Speed;
        public float ContactDamage => _model.ContactDamage;
        public float Radius => _model.Radius;
        public int ExperienceValue => _model.ExperienceValue;
        public int EnemyLevel => _model.EnemyLevel;
        public Vector2 Position => _model.Position;

        private GameDirector _director;
        private readonly SharedEnemyModel _model = new SharedEnemyModel();
        private bool _elite;
        private SpriteRenderer _renderer;
        private GameObject _crown;
        private EnemyLevelLabelPresentation _levelLabel;
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

            var labelObject = new GameObject("Level Label");
            labelObject.transform.SetParent(transform, false);
            _levelLabel = labelObject.AddComponent<EnemyLevelLabelPresentation>();
        }

        public void Initialize(GameDirector director, float difficulty, bool boss, bool elite = false)
        {
            var enemyLevel = BalanceRules.ComputeEnemyLevel(director.Level, boss, elite);
            Initialize(director, difficulty, enemyLevel, boss, elite);
        }

        public void Initialize(GameDirector director, float difficulty, int enemyLevel, bool boss,
            bool elite = false)
        {
            var map = director.SelectedMap;
            var definition = boss
                ? EnemyCatalog.FindById(map.BossEnemyId)
                : elite
                    ? EnemyCatalog.FindById(map.EliteEnemyId)
                    : EnemyCatalog.FindById(map.RegularEnemyId);

            Initialize(director, difficulty, enemyLevel, definition, elite && !boss);
        }

        public void Initialize(GameDirector director, float difficulty, EnemyDefinition definition, bool elite = false)
        {
            var enemyLevel = BalanceRules.ComputeEnemyLevel(director.Level, definition != null && definition.Boss,
                elite);
            Initialize(director, difficulty, enemyLevel, definition, elite);
        }

        public void Initialize(GameDirector director, float difficulty, int enemyLevel,
            EnemyDefinition definition, bool elite = false)
        {
            _director = director;
            _elite = elite && definition != null && !definition.Boss;

            if (definition == null)
            {
                definition = EnemyCatalog.Draugr;
            }

            gameObject.name = definition.Name;
            var isBoss = definition.Boss;
            var rolledBaseSpeed = director.Rng.Range(definition.MinimumSpeed, definition.MaximumSpeed);
            var rolledRadius = director.Rng.Range(definition.MinimumRadius, definition.MaximumRadius);
            var rolledExperience = director.Rng.Range(definition.MinimumExperience, definition.MaximumExperienceExclusive);
            var scaledExperience = BalanceRules.ExperienceForEnemy(rolledExperience, enemyLevel, _elite, isBoss);
            _model.Begin(transform.position, definition, difficulty, enemyLevel, rolledBaseSpeed, rolledRadius,
                scaledExperience, director.SelectedChallenge);
            transform.localScale = Vector3.one * Radius * 2f;
            _baseColor = isBoss || _elite || !director.Rng.Chance(0.5f)
                ? definition.PrimaryColor
                : definition.AlternateColor;
            _renderer.color = _baseColor;
            _renderer.sortingOrder = isBoss ? 8 : _elite ? 7 : 5;
            _crown.SetActive(isBoss);
            _animationSeed = transform.position.x * 0.31f + transform.position.y * 0.17f;
            _levelLabel.Initialize(Radius);
            _levelLabel.SetLevel(enemyLevel, director.Level, true);
        }

        private void Update()
        {
            if (!Alive || _director == null || _director.State != RunState.Playing) return;
            var target = _director.GetNearestLivingPlayer(Position);
            if (target == null) return;
            var result = _model.AdvanceTowards((Vector2)target.transform.position,
                BalanceRules.PlayerCollisionRadius, Time.deltaTime, _director.ObstacleLayout.Obstacles);
            if ((result & EnemyAdvanceResult.Moved) != 0)
            {
                transform.position = (Vector3)Position;
                _director.UpdateEnemySpatial(this);
            }
            transform.Rotate(0f, 0f, (Boss ? 28f : 36f) * Time.deltaTime);
            var breathe = 1f + Mathf.Sin(Time.time * (Boss ? 2.8f : 4.1f) + _animationSeed) * (Boss ? 0.05f : 0.045f);
            transform.localScale = Vector3.one * Radius * 2f * breathe;
            _levelLabel.RefreshVerticalOffset(Radius);
            _levelLabel.SetLevel(EnemyLevel, _director.Level, true);

            if ((result & EnemyAdvanceResult.ContactTriggered) != 0)
                target.TakeDamage(ContactDamage);
        }

        public EnemyDamageResult TakeDamage(float amount, float knockback, Vector2 source)
        {
            var result = _model.TakeDamage(amount, knockback, source, _director.ObstacleLayout.Obstacles);
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
                _director.OnEnemyKilled(this, ExperienceValue, Boss, Elite);
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
            _elite = false;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
            if (_crown != null) _crown.SetActive(false);
            if (_levelLabel != null) _levelLabel.SetLevel(0, 0, false);
        }
    }
}
