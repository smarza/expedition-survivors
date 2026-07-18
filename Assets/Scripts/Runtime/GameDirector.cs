using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public sealed class GameDirector : MonoBehaviour
    {
        public RunState State { get; private set; } = RunState.MainMenu;
        public readonly List<PlayerController> Players = new List<PlayerController>(2);
        public PlayerController Player => Players.Count > 0 ? Players[0] : null;
        public readonly List<Enemy> Enemies = new List<Enemy>(384);
        public Transform RunRoot => _runRoot;
        public int Level => _runModel.Level;
        public int Experience => _runModel.Experience;
        public int ExperienceToNext => _runModel.ExperienceToNext;
        public int Kills { get; private set; }
        public int RunRenown { get; private set; }
        public float Elapsed => _runModel.Elapsed;
        public bool BossSpawned => _runModel.BossTriggered;
        public int RunSeed { get; private set; } = 1;
        public RunRandom Rng { get; private set; } = new RunRandom(1);
        public PerformanceMetrics Metrics { get; } = new PerformanceMetrics();
        public bool ShowPerformanceMetrics { get; private set; }
        public string FoundationStatus { get; private set; } = "NOT CHECKED";
        public int PendingPlayerCount { get; private set; } = 1;
        public MapDefinition SelectedMap { get; private set; } = ContentCatalog.Maps[0];
        public readonly CharacterDefinition[] SelectedCharacters =
            { ContentCatalog.Characters[0], ContentCatalog.Characters[1] };
        public IReadOnlyList<RewardOption> CurrentRewards => _currentRewards;
        public int RewardTurnPlayerIndex => _runModel.RewardTurnPlayerIndex;
        public RunSimulationPhase SimulationPhase => _runModel.Phase;
        public RunOutcome Outcome => _runModel.Outcome;

        private Transform _runRoot;
        private Camera _camera;
        private CameraFollow _cameraFollow;
        private GameHUD _hud;
        private float _spawnTimer;
        private readonly List<RewardOption> _currentRewards = new List<RewardOption>(4);
        private readonly List<Enemy> _spatialScratch = new List<Enemy>(192);
        private bool _runRecorded;
        private readonly SharedRunModel _runModel = new SharedRunModel();
        private Transform _poolRoot;
        private ComponentPool<Enemy> _enemyPool;
        private ComponentPool<AxeProjectile> _projectilePool;
        private ComponentPool<ExperienceGem> _gemPool;
        private ComponentPool<PulseVisual> _pulsePool;
        private ComponentPool<UltimatePulseVisual> _ultimatePulsePool;
        private SpatialHashGrid<Enemy> _enemyGrid;

        public int ActiveProjectiles => _projectilePool?.ActiveCount ?? 0;
        public int ActiveGems => _gemPool?.ActiveCount ?? 0;
        public int PooledEnemyCount => _enemyPool?.AvailableCount ?? 0;
        public int PooledProjectileCount => _projectilePool?.AvailableCount ?? 0;
        public int PooledGemCount => _gemPool?.AvailableCount ?? 0;
        public int CreatedPooledObjects => (_enemyPool?.Created ?? 0) + (_projectilePool?.Created ?? 0) + (_gemPool?.Created ?? 0) + (_pulsePool?.Created ?? 0) + (_ultimatePulsePool?.Created ?? 0);
        public int ReusedPooledObjects => (_enemyPool?.Reused ?? 0) + (_projectilePool?.Reused ?? 0) + (_gemPool?.Reused ?? 0) + (_pulsePool?.Reused ?? 0) + (_ultimatePulsePool?.Reused ?? 0);
        public int SpatialCellCount => _enemyGrid?.OccupiedCellCount ?? 0;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            ProductionContentRuntime.Load();
            SelectedMap = ContentCatalog.Map(0);
            SelectedCharacters[0] = ContentCatalog.Character(0);
            SelectedCharacters[1] = ContentCatalog.Character(Mathf.Min(1, ContentCatalog.Characters.Length - 1));
            SaveService.Load();
            InitializeProductionFoundation();
            CreateCamera();
            _hud = gameObject.AddComponent<GameHUD>();
            _hud.Initialize(this);
        }

        private void CreateCamera()
        {
            var cameraObject = new GameObject("Expedition Camera");
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 6f;
            _camera.backgroundColor = new Color(0.035f, 0.075f, 0.11f);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _cameraFollow = cameraObject.AddComponent<CameraFollow>();
            _cameraFollow.Director = this;
        }

        private void Update()
        {
            Metrics.Tick(Time.unscaledDeltaTime, _enemyGrid?.QueryCount ?? 0);
            if (LocalInputRouter.MetricsPressed()) ShowPerformanceMetrics = !ShowPerformanceMetrics;
            if (LocalInputRouter.DetailsPressed()) ToggleBuildDetails();
            if (LocalInputRouter.PausePressed()) TogglePause();
            if (State != RunState.Playing) return;

            _runModel.Advance(Time.deltaTime);
            _spawnTimer -= Time.deltaTime;
            CleanupEnemyList();

            var difficulty = 1f + Elapsed / SelectedMap.DifficultyRamp;
            if (_spawnTimer <= 0f && Enemies.Count < 260)
            {
                var groupSize = Mathf.Clamp(1 + Mathf.FloorToInt(Elapsed / 35f), 1, 7);
                for (var i = 0; i < groupSize; i++) SpawnEnemy(false, difficulty);
                _spawnTimer = Mathf.Max(SelectedMap.MinimumSpawnInterval,
                    SelectedMap.BaseSpawnInterval - Elapsed * 0.0014f);
            }

            if (_runModel.TryTriggerBoss(SelectedMap.BossSpawnTime))
            {
                SpawnEnemy(true, difficulty);
                _hud.SetAnnouncement("THE JOTUNN HAS FOUND YOU", 3.6f);
            }

            // This map ends only when its Jotunn is defeated. The configured
            // duration marks the target expedition length, not a free victory.
        }

        public void BeginRunSetup(int playerCount)
        {
            Time.timeScale = 1f;
            PendingPlayerCount = Mathf.Clamp(playerCount, 1, 2);
            LocalInputRouter.BeginSession(PendingPlayerCount);
            SelectedCharacters[0] = ContentCatalog.Characters[0];
            SelectedCharacters[1] = ContentCatalog.Characters[1];
            State = RunState.CharacterSelect;
        }

        public void ConfirmCharacters(int firstCharacter, int secondCharacter)
        {
            SelectedCharacters[0] = ContentCatalog.Character(firstCharacter);
            SelectedCharacters[1] = ContentCatalog.Character(secondCharacter);
            State = RunState.MapSelect;
        }

        public void SelectMapAndStart(int mapIndex)
        {
            SelectedMap = ContentCatalog.Map(mapIndex);
            StartRun(PendingPlayerCount);
        }

        public void StartRun(int playerCount = 1)
        {
            StartRunWithSeed(playerCount, GenerateRunSeed(), false);
        }

        public void ReplayRun()
        {
            StartRunWithSeed(Mathf.Max(1, Players.Count), RunSeed, true);
        }

        private void StartRunWithSeed(int playerCount, int seed, bool replayingSeed)
        {
            Time.timeScale = 1f;
            ReleasePooledSimulation();
            if (_runRoot != null) Destroy(_runRoot.gameObject);
            Enemies.Clear();
            Players.Clear();
            RunSeed = seed == 0 ? 1 : seed;
            Rng = new RunRandom(RunSeed);
            _runRoot = new GameObject("Current Expedition").transform;
            _runRoot.SetParent(transform, false);
            CreateArena();

            playerCount = Mathf.Clamp(playerCount, 1, 2);
            for (var i = 0; i < playerCount; i++)
            {
                var definition = SelectedCharacters[Mathf.Clamp(i, 0, SelectedCharacters.Length - 1)];
                var playerObject = new GameObject(definition.Name);
                playerObject.transform.SetParent(_runRoot, false);
                playerObject.transform.position = new Vector3((i - (playerCount - 1) * 0.5f) * 1.5f, 0f, 0f);
                var player = playerObject.AddComponent<PlayerController>();
                player.Initialize(this, i, definition);
                Players.Add(player);
            }
            _runModel.Begin(playerCount, BalanceRules.ExperienceToNext);
            Kills = 0;
            RunRenown = 0;
            _spawnTimer = 0.2f;
            _runRecorded = false;
            State = RunState.Playing;
            var expeditionLabel = playerCount > 1
                ? $"{SelectedCharacters[0].Name.ToUpperInvariant()} + {SelectedCharacters[1].Name.ToUpperInvariant()} — {SelectedMap.Name.ToUpperInvariant()}"
                : $"{SelectedCharacters[0].Name.ToUpperInvariant()} — {SelectedMap.Name.ToUpperInvariant()}";
            _hud.SetAnnouncement(replayingSeed
                ? $"REPLAYING SEED {RunSeed} — {expeditionLabel}"
                : expeditionLabel, 3f);
        }

        private void CreateArena()
        {
            var ground = new GameObject("Frozen Shore");
            ground.transform.SetParent(_runRoot, false);
            ground.transform.localScale = new Vector3(80f, 80f, 1f);
            var groundRenderer = ground.AddComponent<SpriteRenderer>();
            groundRenderer.sprite = RuntimeAssets.Circle;
            groundRenderer.color = SelectedMap.GroundColor;
            groundRenderer.sortingOrder = -20;

            for (var i = 0; i < 90; i++)
            {
                var marker = new GameObject(i % 5 == 0 ? "Rune Stone" : "Ice Shard");
                marker.transform.SetParent(_runRoot, false);
                marker.transform.position = new Vector3(Rng.Range(-38f, 38f), Rng.Range(-38f, 38f), 0f);
                marker.transform.localScale = Vector3.one * Rng.Range(0.08f, 0.28f);
                var renderer = marker.AddComponent<SpriteRenderer>();
                renderer.sprite = i % 5 == 0 ? RuntimeAssets.Diamond : RuntimeAssets.Circle;
                renderer.color = i % 5 == 0 ? new Color(0.22f, 0.45f, 0.49f) : new Color(0.17f, 0.25f, 0.28f);
                renderer.sortingOrder = -10;
            }
        }

        private void SpawnEnemy(bool boss, float difficulty)
        {
            if (Players.Count == 0) return;
            var angle = Rng.Range(0f, Mathf.PI * 2f);
            var distance = Rng.Range(8.5f, 11.8f);
            var position = GroupCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            var enemy = _enemyPool.Get(position);
            enemy.Initialize(this, difficulty, boss);
            Enemies.Add(enemy);
            _enemyGrid.Add(enemy);
        }

        public Enemy GetNearestEnemy(Vector2 origin)
        {
            return _enemyGrid.FindNearest(origin, 32f, enemy => enemy != null && enemy.Alive);
        }

        public PlayerController GetNearestLivingPlayer(Vector2 origin)
        {
            PlayerController nearest = null;
            var best = float.MaxValue;
            for (var i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if (player == null || !player.IsAlive) continue;
                var distance = ((Vector2)player.transform.position - origin).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                nearest = player;
            }
            return nearest;
        }

        public Vector2 GroupCenter
        {
            get
            {
                if (Players.Count == 0) return Vector2.zero;
                var sum = Vector2.zero;
                var count = 0;
                for (var i = 0; i < Players.Count; i++)
                {
                    if (Players[i] == null) continue;
                    sum += (Vector2)Players[i].transform.position;
                    count++;
                }
                return count > 0 ? sum / count : Vector2.zero;
            }
        }

        public Vector2 ConstrainToCoopRange(PlayerController movingPlayer, Vector2 requested)
        {
            if (Players.Count <= 1) return requested;
            var otherCenter = Vector2.zero;
            var count = 0;
            for (var i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if (player == null || player == movingPlayer) continue;
                otherCenter += (Vector2)player.transform.position;
                count++;
            }
            if (count == 0) return requested;
            otherCenter /= count;
            var offset = requested - otherCenter;
            const float maximumSeparation = 11.5f;
            return offset.sqrMagnitude <= maximumSeparation * maximumSeparation
                ? requested
                : otherCenter + offset.normalized * maximumSeparation;
        }

        public int DamageEnemiesInRadius(Vector2 center, float radius, float damage, float knockback)
        {
            var hitCount = 0;
            GetEnemiesInRadius(center, radius + 0.9f, _spatialScratch);
            for (var i = _spatialScratch.Count - 1; i >= 0; i--)
            {
                var enemy = _spatialScratch[i];
                if (enemy == null || !enemy.Alive) continue;
                if ((enemy.Position - center).sqrMagnitude <= (radius + enemy.Radius) * (radius + enemy.Radius))
                {
                    enemy.TakeDamage(damage, knockback, center);
                    hitCount++;
                }
            }
            return hitCount;
        }

        public void OnEnemyKilled(Enemy enemy, int experienceValue, bool boss)
        {
            if (enemy == null) return;
            Kills++;
            if (Rng.Chance(0.18f)) RunRenown++;
            var position = enemy.transform.position;
            var gem = _gemPool.Get(position);
            gem.Initialize(this, experienceValue);
            ReleaseEnemy(enemy);
            if (boss) EndRun(true);
        }

        public void OnPlayerDowned(PlayerController player)
        {
            var living = 0;
            for (var i = 0; i < Players.Count; i++)
                if (Players[i] != null && Players[i].IsAlive) living++;
            if (living == 0) EndRun(false);
            else _hud.SetAnnouncement($"{player.HeroName.ToUpperInvariant()} IS DOWN — STAY CLOSE TO REVIVE", 3f);
        }

        public void OnPlayerRevived(PlayerController player) =>
            _hud.SetAnnouncement($"{player.HeroName.ToUpperInvariant()} RETURNS TO THE SAGA", 2.2f);

        public void AddExperience(int amount)
        {
            if (State != RunState.Playing) return;
            if (_runModel.AddExperience(amount)) OfferLevelUp();
        }

        private void OfferLevelUp()
        {
            _currentRewards.Clear();
            var builds = new PlayerBuild[Mathf.Max(1, Players.Count)];
            for (var i = 0; i < builds.Length; i++) builds[i] = Players[i].Build;
            _currentRewards.AddRange(RewardFactory.Generate(builds, RewardTurnPlayerIndex, Players.Count, Rng));
            State = RunState.LevelUp;
            Time.timeScale = 0f;
        }

        public void ChooseReward(int index)
        {
            if (State != RunState.LevelUp || index < 0 || index >= _currentRewards.Count) return;
            var option = _currentRewards[index];
            var appliedNames = new List<string>(2);
            if (option.Shared)
            {
                for (var i = 0; i < Players.Count; i++)
                {
                    if (RewardEffects.Apply(Players[i], option)) appliedNames.Add(Players[i].HeroName);
                }
            }
            else if (option.TargetPlayerIndex >= 0 && option.TargetPlayerIndex < Players.Count)
            {
                var target = Players[option.TargetPlayerIndex];
                if (RewardEffects.Apply(target, option)) appliedNames.Add(target.HeroName);
            }
            var chooser = Players[Mathf.Clamp(RewardTurnPlayerIndex, 0, Players.Count - 1)].HeroName;
            var destination = option.Shared ? "THE TEAM" : (appliedNames.Count > 0 ? appliedNames[0].ToUpperInvariant() : "NO TARGET");
            _hud.SetAnnouncement($"{chooser.ToUpperInvariant()} CHOSE {option.Item.Name.ToUpperInvariant()} FOR {destination}", 3.2f);
            _runModel.CompleteReward();
            State = RunState.Playing;
            Time.timeScale = 1f;
        }

        public void EndRun(bool victory)
        {
            if (!_runModel.Complete(victory)) return;
            State = _runModel.Outcome == RunOutcome.Victory ? RunState.Victory : RunState.GameOver;
            Time.timeScale = 0f;
            if (!_runRecorded)
            {
                SaveService.RecordRun(Kills, RunRenown, Elapsed, victory);
                _runRecorded = true;
            }
        }

        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            _runModel.Reset();
            State = RunState.MainMenu;
            LocalInputRouter.BeginSession(1);
            ReleasePooledSimulation();
            if (_runRoot != null) Destroy(_runRoot.gameObject);
            Enemies.Clear();
            Players.Clear();
            _camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        public void TogglePause()
        {
            if (State == RunState.Playing)
            {
                State = RunState.Paused;
                Time.timeScale = 0f;
            }
            else if (State == RunState.Paused)
            {
                State = RunState.Playing;
                Time.timeScale = 1f;
            }
        }

        public void ToggleBuildDetails()
        {
            if (State == RunState.Playing)
            {
                State = RunState.BuildDetails;
                Time.timeScale = 0f;
            }
            else if (State == RunState.BuildDetails)
            {
                State = RunState.Playing;
                Time.timeScale = 1f;
            }
        }

        public void ShowPulse(Vector2 position, int playerIndex)
        {
            _pulsePool.Get(position).Initialize(this, playerIndex);
        }

        public void Announce(string message, float duration) => _hud.SetAnnouncement(message, duration);

        public void ShowUltimate(Vector2 position, int playerIndex, float radius)
        {
            _ultimatePulsePool.Get(position).Initialize(this, playerIndex, radius);
        }

        private void CleanupEnemyList()
        {
            for (var i = Enemies.Count - 1; i >= 0; i--)
                if (Enemies[i] == null || !Enemies[i].Alive) Enemies.RemoveAt(i);
        }

        public void UpdateEnemySpatial(Enemy enemy)
        {
            if (enemy != null && enemy.Alive) _enemyGrid.Update(enemy);
        }

        public void GetEnemiesInRadius(Vector2 center, float radius, List<Enemy> results) =>
            _enemyGrid.QueryRadius(center, radius, results);

        public AxeProjectile SpawnProjectile(Vector2 position)
        {
            return _projectilePool.Get(position);
        }

        public void ReleaseProjectile(AxeProjectile projectile) => _projectilePool.Release(projectile);

        public void ReleaseExperienceGem(ExperienceGem gem) => _gemPool.Release(gem);

        private void ReleaseEnemy(Enemy enemy)
        {
            _enemyGrid.Remove(enemy);
            Enemies.Remove(enemy);
            _enemyPool.Release(enemy);
        }

        private void InitializeProductionFoundation()
        {
            _poolRoot = new GameObject("Production Object Pools").transform;
            _poolRoot.SetParent(transform, false);
            _enemyGrid = new SpatialHashGrid<Enemy>(2.5f, enemy => enemy.Position);
            _enemyPool = new ComponentPool<Enemy>(() => CreatePooled<Enemy>("Pooled Enemy"), _poolRoot, 96);
            _projectilePool = new ComponentPool<AxeProjectile>(() => CreatePooled<AxeProjectile>("Pooled Frost Axe"), _poolRoot, 64);
            _gemPool = new ComponentPool<ExperienceGem>(() => CreatePooled<ExperienceGem>("Pooled Experience"), _poolRoot, 96);
            _pulsePool = new ComponentPool<PulseVisual>(() => CreatePooled<PulseVisual>("Pooled Raven Guard Pulse"), _poolRoot, 16);
            _ultimatePulsePool = new ComponentPool<UltimatePulseVisual>(() => CreatePooled<UltimatePulseVisual>("Pooled Ultimate Pulse"), _poolRoot, 4);
            if (ProductionFoundationChecks.Run(out var report))
            {
                FoundationStatus = $"READY — {report}";
                Debug.Log($"Project Expedition 0.7 foundation: {FoundationStatus}");
            }
            else
            {
                FoundationStatus = $"FAILED — {report}";
                Debug.LogError($"Project Expedition 0.7 foundation: {FoundationStatus}");
            }
        }

        private T CreatePooled<T>(string objectName) where T : Component
        {
            var pooledObject = new GameObject(objectName);
            pooledObject.transform.SetParent(_poolRoot, false);
            return pooledObject.AddComponent<T>();
        }

        private void ReleasePooledSimulation()
        {
            _enemyPool?.ReleaseAll();
            _projectilePool?.ReleaseAll();
            _gemPool?.ReleaseAll();
            _pulsePool?.ReleaseAll();
            _ultimatePulsePool?.ReleaseAll();
            _enemyGrid?.Clear();
        }

        public void ReleasePulse(PulseVisual pulse) => _pulsePool.Release(pulse);

        public void ReleaseUltimatePulse(UltimatePulseVisual pulse) => _ultimatePulsePool.Release(pulse);

        private int GenerateRunSeed()
        {
            unchecked
            {
                var ticks = System.DateTime.UtcNow.Ticks;
                var seed = (int)(ticks ^ (ticks >> 32) ^ SaveService.Data.RunsCompleted * 486187739);
                return seed == 0 ? 1 : seed;
            }
        }
    }

    public sealed class CameraFollow : MonoBehaviour
    {
        public GameDirector Director;
        private void LateUpdate()
        {
            if (Director == null || Director.Players.Count == 0) return;
            var center = Director.GroupCenter;
            var maximumDistance = 0f;
            for (var i = 0; i < Director.Players.Count; i++)
            {
                var player = Director.Players[i];
                if (player == null) continue;
                maximumDistance = Mathf.Max(maximumDistance, Vector2.Distance(center, player.transform.position));
            }
            var desired = new Vector3(center.x, center.y, -10f);
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            var camera = GetComponent<Camera>();
            var targetSize = Mathf.Clamp(6f + maximumDistance * 0.62f, 6f, 9.5f);
            camera.orthographicSize = Mathf.Lerp(camera.orthographicSize, targetSize, 1f - Mathf.Exp(-5f * Time.unscaledDeltaTime));
        }
    }

    public sealed class PulseVisual : MonoBehaviour, IPoolableComponent
    {
        private SpriteRenderer _renderer;
        private float _time;
        private int _playerIndex;
        private GameDirector _director;
        public void Initialize(GameDirector director, int playerIndex)
        {
            _director = director;
            _playerIndex = playerIndex;
            _time = 0f;
            gameObject.name = "Raven Shield Pulse";
            var color = playerIndex == 0 ? new Color(0.35f, 0.9f, 1f) : new Color(1f, 0.58f, 0.22f);
            _renderer.color = new Color(color.r, color.g, color.b, 0.33f);
        }
        private void Awake()
        {
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = RuntimeAssets.Circle;
            _renderer.color = _playerIndex == 0
                ? new Color(0.35f, 0.9f, 1f, 0.33f)
                : new Color(1f, 0.58f, 0.22f, 0.33f);
            _renderer.sortingOrder = 14;
            transform.localScale = Vector3.zero;
        }
        private void Update()
        {
            _time += Time.deltaTime;
            transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 5.2f, _time / 0.35f);
            var color = _playerIndex == 0 ? new Color(0.35f, 0.9f, 1f) : new Color(1f, 0.58f, 0.22f);
            _renderer.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.35f, 0f, _time / 0.35f));
            if (_time >= 0.35f) _director.ReleasePulse(this);
        }

        public void OnReleasedToPool()
        {
            _director = null;
            _time = 0f;
            transform.localScale = Vector3.zero;
        }
    }

    public sealed class UltimatePulseVisual : MonoBehaviour, IPoolableComponent
    {
        private SpriteRenderer _renderer;
        private float _time;
        private int _playerIndex;
        private float _radius;
        private GameDirector _director;

        public void Initialize(GameDirector director, int playerIndex, float radius)
        {
            _director = director;
            _playerIndex = playerIndex;
            _radius = radius;
            _time = 0f;
            gameObject.name = "Ultimate Shockwave";
        }

        private void Awake()
        {
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = RuntimeAssets.Circle;
            _renderer.sortingOrder = 16;
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            _time += Time.unscaledDeltaTime;
            const float duration = 0.85f;
            var progress = Mathf.Clamp01(_time / duration);
            transform.localScale = Vector3.one * Mathf.Lerp(0.2f, _radius * 2f, progress);
            var color = _playerIndex == 0 ? new Color(0.35f, 0.9f, 1f) : new Color(1f, 0.58f, 0.22f);
            _renderer.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.52f, 0f, progress));
            if (_time >= duration) _director.ReleaseUltimatePulse(this);
        }

        public void OnReleasedToPool()
        {
            _director = null;
            _time = 0f;
            _radius = 0f;
            transform.localScale = Vector3.zero;
        }
    }
}
