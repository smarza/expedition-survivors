using UnityEngine;

namespace ProjectExpedition
{
    public sealed class Enemy : MonoBehaviour, IPoolableComponent
    {
        private const float BossEnrageHealthFraction = 0.35f;
        private const float BossKnockbackResistance = 0.1f;
        private const float BossChargeContactMultiplier = 1.28f;
        private const float BossSlamInterval = 5.8f;
        private const float BossSlamEnragedInterval = 4.1f;
        private const float BossSlamTelegraphDuration = 0.92f;
        private const float BossSlamRadius = 4.6f;

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

        private enum BossCombatPhase
        {
            Stalking,
            Telegraphing,
            Charging,
            Recovering
        }

        private GameDirector _director;
        private readonly SharedEnemyModel _model = new SharedEnemyModel();
        private bool _elite;
        private SpriteRenderer _renderer;
        private GameObject _crown;
        private EnemyLevelLabelPresentation _levelLabel;
        private BossMenacePresentation _menacePresentation;
        private Color _baseColor;
        private float _animationSeed;
        private float _maxHealth;
        private bool _bossEnraged;
        private bool _bossEnrageAnnounced;
        private BossCombatPhase _bossPhase;
        private float _bossPhaseElapsed;
        private float _bossPhaseDuration;
        private Vector2 _chargeTarget;
        private float _chargeTrailTimer;
        private float _slamCooldownRemaining;
        private bool _slamTelegraphing;
        private float _slamTelegraphElapsed;

        private void Awake()
        {
            _renderer = gameObject.GetComponent<SpriteRenderer>();
            if (_renderer == null)
            {
                _renderer = gameObject.AddComponent<SpriteRenderer>();
            }

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

            _menacePresentation = gameObject.GetComponent<BossMenacePresentation>();
            if (_menacePresentation == null)
            {
                _menacePresentation = gameObject.AddComponent<BossMenacePresentation>();
            }
        }

        public void Initialize(GameDirector director, float difficulty, bool boss, bool elite = false)
        {
            var enemyLevel = BalanceRules.ComputeEnemyLevel(director.Level, boss, elite);
            Initialize(director, difficulty, enemyLevel, boss, elite);
        }

        public void Initialize(GameDirector director, float difficulty, int enemyLevel, bool boss,
            bool elite = false)
        {
            var map = DevelopmentTuningResolver.ResolveMap(director.SelectedMap);
            var definition = boss
                ? DevelopmentTuningResolver.ResolveEnemy(map.BossEnemyId)
                : elite
                    ? DevelopmentTuningResolver.ResolveEnemy(map.EliteEnemyId)
                    : DevelopmentTuningResolver.ResolveEnemy(map.RegularEnemyId);

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

            _maxHealth = _model.Health;
            _bossEnraged = false;
            _bossEnrageAnnounced = false;
            ResetBossCombatCycle(isBoss);
        }

        private void Update()
        {
            if (!Alive || _director == null || _director.State != RunState.Playing)
            {
                return;
            }

            var target = _director.GetNearestLivingPlayer(Position);
            if (target == null)
            {
                return;
            }

            if (Boss)
            {
                UpdateBoss(target);
                return;
            }

            UpdateRegularEnemy(target);
        }

        private void UpdateRegularEnemy(PlayerController target)
        {
            var result = _model.AdvanceTowards((Vector2)target.transform.position,
                BalanceRules.PlayerCollisionRadius, Time.deltaTime, _director.ObstacleLayout.Obstacles);
            ApplyMovementResult(result, target, 1f);
            ApplyIdlePresentation(false);
        }

        private void UpdateBoss(PlayerController target)
        {
            UpdateBossEnrageState();

            _bossPhaseElapsed += Time.deltaTime;
            if (_bossPhaseElapsed >= _bossPhaseDuration)
            {
                AdvanceBossPhase(target);
            }

            var speedMultiplier = ResolveBossSpeedMultiplier();
            var moveTarget = _bossPhase == BossCombatPhase.Charging ? _chargeTarget : (Vector2)target.transform.position;
            var result = _model.AdvanceTowards(moveTarget, BalanceRules.PlayerCollisionRadius, Time.deltaTime,
                _director.ObstacleLayout.Obstacles, speedMultiplier);
            ApplyMovementResult(result, target, _bossPhase == BossCombatPhase.Charging ? BossChargeContactMultiplier : 1f);
            ApplyBossPresentation();
            EmitBossChargeTrail();
            ApplyBossProximityPressure();
            UpdateBossSlam();
        }

        private void UpdateBossEnrageState()
        {
            if (_maxHealth <= 0f)
            {
                return;
            }

            var healthFraction = Health / _maxHealth;
            if (healthFraction > BossEnrageHealthFraction)
            {
                return;
            }

            if (_bossEnraged)
            {
                return;
            }

            _bossEnraged = true;
            if (_bossEnrageAnnounced)
            {
                return;
            }

            _bossEnrageAnnounced = true;
            _director.Present(PresentationCue.BossSpawn, Position, new Color(1f, 0.18f, 0.1f), Radius * 2.4f);
            _director.AddCameraTrauma(0.38f);
            _director.Announce("THE JOTUNN RAGES — IT WILL NOT STOP", 2.8f);
        }

        private void ResetBossCombatCycle(bool isBoss)
        {
            if (!isBoss)
            {
                _menacePresentation.Hide();
                return;
            }

            _bossPhase = BossCombatPhase.Stalking;
            _bossPhaseElapsed = 0f;
            _bossPhaseDuration = ResolveBossStalkDuration();
            _chargeTrailTimer = 0f;
            _slamCooldownRemaining = ResolveSlamInterval();
            _slamTelegraphing = false;
            _slamTelegraphElapsed = 0f;
            _menacePresentation.Initialize(_baseColor, Radius);
            _menacePresentation.Show();
            _menacePresentation.SetPresentation(BossMenaceVisualPhase.Stalking, 0f, false);
        }

        private void AdvanceBossPhase(PlayerController target)
        {
            switch (_bossPhase)
            {
                case BossCombatPhase.Stalking:
                    BeginBossTelegraph(target);
                    break;
                case BossCombatPhase.Telegraphing:
                    BeginBossCharge();
                    break;
                case BossCombatPhase.Charging:
                    BeginBossRecovery();
                    break;
                default:
                    BeginBossStalk();
                    break;
            }
        }

        private void BeginBossStalk()
        {
            _bossPhase = BossCombatPhase.Stalking;
            _bossPhaseElapsed = 0f;
            _bossPhaseDuration = ResolveBossStalkDuration();
            _renderer.color = _baseColor;
        }

        private void BeginBossTelegraph(PlayerController target)
        {
            _bossPhase = BossCombatPhase.Telegraphing;
            _bossPhaseElapsed = 0f;
            _bossPhaseDuration = _bossEnraged ? 0.72f : 1.05f;
            _chargeTarget = target.transform.position;
            _director.Present(PresentationCue.Impact, Position, new Color(1f, 0.28f, 0.14f), Radius * 1.6f);
            _director.AddCameraTrauma(_bossEnraged ? 0.16f : 0.12f);
        }

        private void BeginBossCharge()
        {
            _bossPhase = BossCombatPhase.Charging;
            _bossPhaseElapsed = 0f;
            _bossPhaseDuration = _bossEnraged ? 0.62f : 0.52f;
            _chargeTrailTimer = 0f;
            _director.Present(PresentationCue.Ultimate, Position, new Color(1f, 0.22f, 0.12f), Radius * 1.35f);
            _director.AddCameraTrauma(_bossEnraged ? 0.24f : 0.18f);
        }

        private void BeginBossRecovery()
        {
            _bossPhase = BossCombatPhase.Recovering;
            _bossPhaseElapsed = 0f;
            _bossPhaseDuration = _bossEnraged ? 0.42f : 0.72f;
            _renderer.color = _baseColor;
        }

        private float ResolveBossStalkDuration() => _bossEnraged ? 1.55f : 2.65f;

        private float ResolveBossSpeedMultiplier()
        {
            if (_slamTelegraphing)
            {
                return 0.1f;
            }

            switch (_bossPhase)
            {
                case BossCombatPhase.Telegraphing:
                    return _bossEnraged ? 0.42f : 0.34f;
                case BossCombatPhase.Charging:
                    return _bossEnraged ? 4.9f : 4.1f;
                case BossCombatPhase.Recovering:
                    return _bossEnraged ? 0.72f : 0.58f;
                default:
                    return _bossEnraged ? 1.28f : 1.12f;
            }
        }

        private BossMenaceVisualPhase ResolveBossVisualPhase()
        {
            if (_slamTelegraphing)
            {
                return BossMenaceVisualPhase.SlamTelegraph;
            }

            switch (_bossPhase)
            {
                case BossCombatPhase.Telegraphing:
                    return BossMenaceVisualPhase.Telegraphing;
                case BossCombatPhase.Charging:
                    return BossMenaceVisualPhase.Charging;
                case BossCombatPhase.Recovering:
                    return BossMenaceVisualPhase.Recovering;
                default:
                    return BossMenaceVisualPhase.Stalking;
            }
        }

        private void ApplyBossPresentation()
        {
            var phaseProgress = _bossPhaseDuration <= 0f ? 0f : _bossPhaseElapsed / _bossPhaseDuration;
            if (_slamTelegraphing)
            {
                phaseProgress = Mathf.Clamp01(_slamTelegraphElapsed / BossSlamTelegraphDuration);
            }

            _menacePresentation.SetPresentation(ResolveBossVisualPhase(), phaseProgress, _bossEnraged);

            var breatheAmplitude = _bossPhase == BossCombatPhase.Telegraphing ? 0.11f : 0.085f;
            if (_bossEnraged)
            {
                breatheAmplitude += 0.03f;
            }

            var breathe = 1f + Mathf.Sin(Time.time * (_bossEnraged ? 3.6f : 2.8f) + _animationSeed) * breatheAmplitude;
            transform.localScale = Vector3.one * Radius * 2f * breathe;

            var rotationSpeed = _bossPhase == BossCombatPhase.Charging
                ? (_bossEnraged ? 120f : 96f)
                : (_bossEnraged ? 42f : 28f);
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

            if (_bossPhase == BossCombatPhase.Telegraphing || _slamTelegraphing)
            {
                var telegraphTint = Color.Lerp(_baseColor, new Color(1f, 0.24f, 0.12f),
                    0.35f + phaseProgress * 0.45f);
                _renderer.color = telegraphTint;
            }
            else if (_bossPhase == BossCombatPhase.Charging)
            {
                _renderer.color = Color.Lerp(new Color(1f, 0.34f, 0.16f), _baseColor, phaseProgress * 0.65f);
            }
            else if (_bossEnraged)
            {
                var ragePulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6.2f + _animationSeed);
                _renderer.color = Color.Lerp(_baseColor, new Color(1f, 0.18f, 0.1f), ragePulse * 0.42f);
            }

            _levelLabel.RefreshForEnemyRadius(Radius);
            _levelLabel.SetLevel(EnemyLevel, _director.Level, true);

            if (_crown != null && _crown.activeSelf)
            {
                var crownScale = 0.35f * (1f + Mathf.Sin(Time.time * 7.4f + _animationSeed) * 0.14f);
                _crown.transform.localScale = Vector3.one * crownScale;
            }
        }

        private void ApplyIdlePresentation(bool isBoss)
        {
            transform.Rotate(0f, 0f, (isBoss ? 28f : 36f) * Time.deltaTime);
            var breathe = 1f + Mathf.Sin(Time.time * (isBoss ? 2.8f : 4.1f) + _animationSeed) * (isBoss ? 0.05f : 0.045f);
            transform.localScale = Vector3.one * Radius * 2f * breathe;
            _levelLabel.RefreshForEnemyRadius(Radius);
            _levelLabel.SetLevel(EnemyLevel, _director.Level, true);
        }

        private void EmitBossChargeTrail()
        {
            if (_bossPhase != BossCombatPhase.Charging)
            {
                return;
            }

            _chargeTrailTimer += Time.deltaTime;
            if (_chargeTrailTimer < 0.055f)
            {
                return;
            }

            _chargeTrailTimer = 0f;
            _director.Present(PresentationCue.ProjectileTrail, Position,
                new Color(1f, 0.28f, 0.12f, 0.82f), Radius * 0.55f);
        }

        private void ApplyBossProximityPressure()
        {
            var nearestDistance = _director.DistanceToNearestLivingPlayer(Position);
            if (nearestDistance > 9f)
            {
                return;
            }

            var pressure = 1f - nearestDistance / 9f;
            if (_bossPhase == BossCombatPhase.Charging)
            {
                _director.AddCameraTrauma(0.045f * pressure * Time.deltaTime * 60f);
            }
            else if (_bossPhase == BossCombatPhase.Telegraphing || _slamTelegraphing)
            {
                _director.AddCameraTrauma(0.02f * pressure * Time.deltaTime * 60f);
            }
        }

        private void UpdateBossSlam()
        {
            if (_bossPhase == BossCombatPhase.Charging || _bossPhase == BossCombatPhase.Telegraphing)
            {
                return;
            }

            if (_slamTelegraphing)
            {
                _slamTelegraphElapsed += Time.deltaTime;
                if (_slamTelegraphElapsed >= BossSlamTelegraphDuration)
                {
                    ExecuteBossSlam();
                    _slamTelegraphing = false;
                    _slamTelegraphElapsed = 0f;
                    _slamCooldownRemaining = ResolveSlamInterval();
                }

                return;
            }

            _slamCooldownRemaining -= Time.deltaTime;
            if (_slamCooldownRemaining <= 0f)
            {
                BeginBossSlamTelegraph();
            }
        }

        private void BeginBossSlamTelegraph()
        {
            _slamTelegraphing = true;
            _slamTelegraphElapsed = 0f;
            _director.Present(PresentationCue.Impact, Position, new Color(1f, 0.18f, 0.08f), Radius * 1.25f);
            _director.AddCameraTrauma(_bossEnraged ? 0.14f : 0.1f);
        }

        private void ExecuteBossSlam()
        {
            _director.ResolveBossSlam(Position, BossSlamRadius, ContactDamage * 0.88f, Radius * 2f);
        }

        private float ResolveSlamInterval() => _bossEnraged ? BossSlamEnragedInterval : BossSlamInterval;

        private void ApplyMovementResult(EnemyAdvanceResult result, PlayerController target, float contactMultiplier)
        {
            if ((result & EnemyAdvanceResult.Moved) != 0)
            {
                transform.position = (Vector3)Position;
                _director.UpdateEnemySpatial(this);
            }

            if ((result & EnemyAdvanceResult.ContactTriggered) != 0)
            {
                target.TakeDamage(ContactDamage * contactMultiplier);
                if (Boss)
                {
                    _director.Present(PresentationCue.Impact, Position, new Color(1f, 0.22f, 0.12f), Radius * 1.85f);
                    _director.AddCameraTrauma(_bossPhase == BossCombatPhase.Charging ? 0.32f : 0.22f);
                }
            }
        }

        public EnemyDamageResult TakeDamage(float amount, float knockback, Vector2 source)
        {
            if (Boss)
            {
                knockback *= BossKnockbackResistance;
            }

            var result = _model.TakeDamage(amount, knockback, source, _director.ObstacleLayout.Obstacles);
            if (result == EnemyDamageResult.Ignored)
            {
                return result;
            }

            if ((result & EnemyDamageResult.Moved) != 0)
            {
                transform.position = (Vector3)Position;
                _director.UpdateEnemySpatial(this);
            }

            _renderer.color = Boss ? new Color(1f, 0.72f, 0.58f) : Color.white;
            CancelInvoke(nameof(RestoreColor));
            Invoke(nameof(RestoreColor), Boss ? 0.05f : 0.07f);

            if ((result & EnemyDamageResult.Killed) != 0)
            {
                _director.OnEnemyKilled(this, ExperienceValue, Boss, Elite);
            }

            return result;
        }

        private void RestoreColor()
        {
            if (_renderer == null)
            {
                return;
            }

            if (Boss && (_bossPhase == BossCombatPhase.Telegraphing || _slamTelegraphing))
            {
                return;
            }

            _renderer.color = _baseColor;
        }

        public void OnReleasedToPool()
        {
            _model.Stop();
            CancelInvoke();
            _director = null;
            _elite = false;
            _maxHealth = 0f;
            _bossEnraged = false;
            _bossEnrageAnnounced = false;
            _bossPhase = BossCombatPhase.Stalking;
            _bossPhaseElapsed = 0f;
            _bossPhaseDuration = 0f;
            _chargeTrailTimer = 0f;
            _slamCooldownRemaining = 0f;
            _slamTelegraphing = false;
            _slamTelegraphElapsed = 0f;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
            if (_crown != null)
            {
                _crown.SetActive(false);
            }

            if (_levelLabel != null)
            {
                _levelLabel.SetLevel(0, 0, false);
            }

            if (_menacePresentation != null)
            {
                _menacePresentation.Hide();
            }
        }
    }

    public enum BossMenaceVisualPhase
    {
        Stalking,
        Telegraphing,
        Charging,
        Recovering,
        SlamTelegraph
    }

    public sealed class BossMenacePresentation : MonoBehaviour
    {
        private SpriteRenderer _outerAura;
        private SpriteRenderer _innerAura;
        private SpriteRenderer _telegraphRing;
        private Color _baseColor;
        private float _radius;
        private BossMenaceVisualPhase _phase;
        private float _phaseProgress;
        private bool _enraged;
        private float _pulseSeed;

        public void Initialize(Color baseColor, float radius)
        {
            _baseColor = baseColor;
            _radius = Mathf.Max(0.5f, radius);
            _phase = BossMenaceVisualPhase.Stalking;
            _phaseProgress = 0f;
            _enraged = false;
            _pulseSeed = transform.position.x * 0.19f + transform.position.y * 0.11f;
            EnsureRenderers();
        }

        public void SetPresentation(BossMenaceVisualPhase phase, float phaseProgress, bool enraged)
        {
            _phase = phase;
            _phaseProgress = Mathf.Clamp01(phaseProgress);
            _enraged = enraged;
            RefreshVisuals();
        }

        private void EnsureRenderers()
        {
            _outerAura = EnsureChildRenderer("Boss Outer Aura", 2, RuntimeAssets.Circle);
            _innerAura = EnsureChildRenderer("Boss Inner Aura", 3, RuntimeAssets.Circle);
            _telegraphRing = EnsureChildRenderer("Boss Telegraph Ring", 6, RuntimeAssets.Circle);
            _telegraphRing.gameObject.SetActive(false);
            RefreshVisuals();
        }

        private SpriteRenderer EnsureChildRenderer(string objectName, int sortingOrder, Sprite sprite)
        {
            var child = transform.Find(objectName);
            GameObject childObject;
            if (child == null)
            {
                childObject = new GameObject(objectName);
                childObject.transform.SetParent(transform, false);
            }
            else
            {
                childObject = child.gameObject;
            }

            var renderer = childObject.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = childObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void RefreshVisuals()
        {
            if (_outerAura == null || _innerAura == null || _telegraphRing == null)
            {
                return;
            }

            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (_enraged ? 5.4f : 3.2f) + _pulseSeed);
            var menaceColor = _enraged
                ? Color.Lerp(_baseColor, new Color(1f, 0.22f, 0.12f), 0.72f)
                : Color.Lerp(_baseColor, new Color(0.92f, 0.28f, 0.18f), 0.35f);

            var outerScale = _radius * (_enraged ? 3.8f : 3.2f) * (0.94f + pulse * 0.12f);
            var innerScale = _radius * (_enraged ? 2.35f : 2.05f) * (0.9f + pulse * 0.18f);
            var outerAlpha = _phase == BossMenaceVisualPhase.Charging ? 0.42f : 0.24f + pulse * 0.12f;
            var innerAlpha = _phase == BossMenaceVisualPhase.Telegraphing
                ? 0.34f + _phaseProgress * 0.28f
                : 0.18f + pulse * 0.14f;

            _outerAura.transform.localScale = Vector3.one * outerScale;
            _innerAura.transform.localScale = Vector3.one * innerScale;
            _outerAura.color = new Color(menaceColor.r, menaceColor.g, menaceColor.b, outerAlpha);
            _innerAura.color = new Color(menaceColor.r * 1.08f, menaceColor.g * 0.72f, menaceColor.b * 0.62f, innerAlpha);

            var telegraphActive = _phase == BossMenaceVisualPhase.Telegraphing ||
                                  _phase == BossMenaceVisualPhase.Charging ||
                                  _phase == BossMenaceVisualPhase.SlamTelegraph;
            _telegraphRing.gameObject.SetActive(telegraphActive);
            if (!telegraphActive)
            {
                return;
            }

            if (_phase == BossMenaceVisualPhase.SlamTelegraph)
            {
                var slamScale = _radius * Mathf.Lerp(2.2f, _enraged ? 10.5f : 9.2f, _phaseProgress);
                var slamAlpha = 0.12f + _phaseProgress * 0.46f;
                _telegraphRing.transform.localScale = Vector3.one * slamScale;
                _telegraphRing.color = new Color(1f, 0.16f, 0.06f, slamAlpha);
                return;
            }

            var telegraphScale = _radius * Mathf.Lerp(1.4f, _enraged ? 5.6f : 4.8f, _phaseProgress);
            var telegraphAlpha = _phase == BossMenaceVisualPhase.Charging
                ? Mathf.Lerp(0.42f, 0f, _phaseProgress)
                : 0.16f + _phaseProgress * 0.34f;
            _telegraphRing.transform.localScale = Vector3.one * telegraphScale;
            _telegraphRing.color = new Color(1f, 0.24f, 0.12f, telegraphAlpha);
        }

        public void Hide()
        {
            if (_outerAura != null)
            {
                _outerAura.gameObject.SetActive(false);
            }

            if (_innerAura != null)
            {
                _innerAura.gameObject.SetActive(false);
            }

            if (_telegraphRing != null)
            {
                _telegraphRing.gameObject.SetActive(false);
            }
        }

        public void Show()
        {
            if (_outerAura != null)
            {
                _outerAura.gameObject.SetActive(true);
            }

            if (_innerAura != null)
            {
                _innerAura.gameObject.SetActive(true);
            }

            RefreshVisuals();
        }
    }
}
