using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public sealed class GameDirector : MonoBehaviour
    {
        public RunState State { get; private set; } = RunState.TitleScreen;
        public readonly List<PlayerController> Players = new List<PlayerController>(2);
        public PlayerController Player => Players.Count > 0 ? Players[0] : null;
        public readonly List<Enemy> Enemies = new List<Enemy>(384);
        public Transform RunRoot => _runRoot;
        public int Level => _runModel.Level;
        public int Experience => _runModel.Experience;
        public int ExperienceToNext => _runModel.ExperienceToNext;
        public int Kills { get; private set; }
        public int RunRenown { get; private set; }
        public int LastRunRenownEarned { get; private set; }
        public float Elapsed => _runModel.Elapsed;
        public bool BossSpawned => _routeModel.BossSpawned;
        public SharedExpeditionRouteModel Route => _routeModel;
        public SharedObstacleLayoutModel ObstacleLayout => _obstacleLayout;
        public SharedLootProgressModel LootProgress => _lootProgress;
        public SharedTemporaryEffectModel TemporaryEffect => _temporaryEffect;
        public int RunSeed { get; private set; } = 1;
        public RunRandom Rng { get; private set; } = new RunRandom(1);
        public PerformanceMetrics Metrics { get; } = new PerformanceMetrics();
        public bool ShowPerformanceMetrics { get; private set; }
        public bool ShowDevelopmentTuning { get; private set; }
        public string FoundationStatus { get; private set; } = "NOT CHECKED";
        public int PendingPlayerCount { get; private set; } = 1;
        public MapDefinition SelectedMap { get; private set; } = ContentCatalog.Maps[0];

        public MapDefinition ActiveMap => DevelopmentTuningResolver.ResolveMap(SelectedMap);
        public ChallengeProfile SelectedChallenge { get; private set; } =
            new ChallengeProfile(ChallengeTier.Standard, ChallengeMutator.None, ChallengeMutator.None);
        public readonly CharacterDefinition[] SelectedCharacters =
            { ContentCatalog.Characters[0], ContentCatalog.Characters[1] };
        public IReadOnlyList<RewardOption> CurrentRewards => _currentRewards;
        public int RewardTurnPlayerIndex => _runModel.RewardTurnPlayerIndex;
        public RunSimulationPhase SimulationPhase => _runModel.Phase;
        public RunOutcome Outcome => _runModel.Outcome;
        public RunEndPresentationPhase EndPresentationPhase { get; private set; } = RunEndPresentationPhase.Summary;
        public float EndBeatRemaining { get; private set; }
        public RunEndCause EndCause { get; private set; }
        public PresentationDirector Presentation => _presentation;

        private Transform _runRoot;
        private Camera _camera;
        private CameraFollow _cameraFollow;
        private GameHUD _hud;
        private PresentationDirector _presentation;
        private RunState _settingsReturnState = RunState.TitleScreen;
        private readonly List<RewardOption> _currentRewards = new List<RewardOption>(4);
        private readonly List<Enemy> _spatialScratch = new List<Enemy>(192);
        private bool _runRecorded;
        private readonly SharedRunModel _runModel = new SharedRunModel();
        private readonly SharedExpeditionRouteModel _routeModel = new SharedExpeditionRouteModel();
        private readonly SharedSpawnModel _spawnModel = new SharedSpawnModel();
        private readonly SharedObstacleLayoutModel _obstacleLayout = new SharedObstacleLayoutModel();
        private readonly SharedLootProgressModel _lootProgress = new SharedLootProgressModel();
        private readonly SharedTemporaryEffectModel _temporaryEffect = new SharedTemporaryEffectModel();
        private readonly SharedPersistentZoneModel _persistentZones = new SharedPersistentZoneModel();
        private Transform _poolRoot;
        private ComponentPool<Enemy> _enemyPool;
        private ComponentPool<AxeProjectile> _projectilePool;
        private ComponentPool<ExperienceGem> _gemPool;
        private ComponentPool<RuneShardPickup> _shardPool;
        private ComponentPool<LootPickup> _lootPool;
        private ComponentPool<PulseVisual> _pulsePool;
        private ComponentPool<UltimatePulseVisual> _ultimatePulsePool;
        private SpatialHashGrid<Enemy> _enemyGrid;
        private bool _warlordEliteSpawned;
        private GameObject _extractionBeacon;
        private GameObject _extractionBeaconPlaceholder;
        private const float UltimateGemBurstDuration = 0.95f;

        private const float TwinBossSpawnChance = 0.03f;

        private Vector2 _gemRepulsionOrigin;
        private float _gemRepulsionRadius;
        private float _gemRepulsionUntil;
        private float _developmentTuningPreviousTimeScale = 1f;
        private readonly PlayerHurtFeedbackTracker _playerHurtFeedback = new PlayerHurtFeedbackTracker();

        public PlayerHurtFeedbackTracker PlayerHurtFeedback => _playerHurtFeedback;

        public int ActiveProjectiles => _projectilePool?.ActiveCount ?? 0;
        public int ActiveGems => _gemPool?.ActiveCount ?? 0;
        public int PooledEnemyCount => _enemyPool?.AvailableCount ?? 0;
        public int PooledProjectileCount => _projectilePool?.AvailableCount ?? 0;
        public int PooledGemCount => _gemPool?.AvailableCount ?? 0;
        public int PooledShardCount => _shardPool?.AvailableCount ?? 0;
        public int CreatedPooledObjects => (_enemyPool?.Created ?? 0) + (_projectilePool?.Created ?? 0) + (_gemPool?.Created ?? 0) + (_shardPool?.Created ?? 0) + (_pulsePool?.Created ?? 0) + (_ultimatePulsePool?.Created ?? 0);
        public int ReusedPooledObjects => (_enemyPool?.Reused ?? 0) + (_projectilePool?.Reused ?? 0) + (_gemPool?.Reused ?? 0) + (_shardPool?.Reused ?? 0) + (_pulsePool?.Reused ?? 0) + (_ultimatePulsePool?.Reused ?? 0);
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
            PresentationPreferences.Load();
            DevelopmentTuningService.Load();
            InitializeProductionFoundation();
            CreateCamera();
            _presentation = gameObject.AddComponent<PresentationDirector>();
            _presentation.Initialize(this, _cameraFollow);
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
            DisableForeignCameras(_camera);
            EnsureSingleAudioListener(cameraObject);
        }

        private static void DisableForeignCameras(Camera activeCamera)
        {
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            for (var i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] == activeCamera)
                    continue;

                cameras[i].enabled = false;
            }
        }

        private static void EnsureSingleAudioListener(GameObject cameraObject)
        {
            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            for (var i = 0; i < listeners.Length; i++)
            {
                if (listeners[i].gameObject == cameraObject)
                    continue;

                Object.Destroy(listeners[i]);
            }

            if (cameraObject.GetComponent<AudioListener>() == null)
                cameraObject.AddComponent<AudioListener>();
        }

        private void Update()
        {
            Metrics.Tick(Time.unscaledDeltaTime, _enemyGrid?.QueryCount ?? 0);
            if (LocalInputRouter.MetricsPressed()) ShowPerformanceMetrics = !ShowPerformanceMetrics;
            if (LocalInputRouter.DevelopmentTuningPressed()) ToggleDevelopmentTuning();
            if (LocalInputRouter.DetailsPressed()) ToggleBuildDetails();
            if (LocalInputRouter.PausePressed()) TogglePause();
            if (State != RunState.Playing) return;

            _runModel.Advance(Time.deltaTime);
            CleanupEnemyList();
            _routeModel.Advance(Elapsed, GroupCenter);

            var routeAnnouncement = _routeModel.ConsumeAnnouncement();
            if (!string.IsNullOrEmpty(routeAnnouncement))
                _hud.SetAnnouncement(routeAnnouncement, 3.6f);

            if (_routeModel.IsExtractionComplete())
            {
                EndRun(true);
                return;
            }

            AdvancePersistentZones(Time.deltaTime);
            _temporaryEffect.Advance(Time.deltaTime, Players);
            HandleTemporaryEffectPresentation();
            _playerHurtFeedback.Advance(Time.deltaTime, Players);

            if (!_routeModel.BossKilled)
            {
                if (_routeModel.CanSpawnBoss())
                {
                    _runModel.TryTriggerBoss(_routeModel.BossSpawnTime);
                }

                var spawn = _spawnModel.Advance(Time.deltaTime, Elapsed, ActiveMap, Enemies.Count,
                    _routeModel.CanSpawnBoss() && !_routeModel.BossSpawned, SelectedChallenge);
                TrySpawnWarlordElite(spawn.Difficulty);

                for (var i = 0; i < spawn.RegularEnemyCount; i++)
                    SpawnEnemy(false, spawn.Difficulty);

                if (spawn.SpawnBoss && !_routeModel.BossSpawned)
                {
                    var expectedBossCount = Rng.Chance(TwinBossSpawnChance) ? 2 : 1;
                    SpawnBossWaveWithEntrance(spawn.Difficulty, expectedBossCount);
                    _routeModel.MarkBossSpawned(expectedBossCount);
                }
            }
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

        public void SetSelectedChallenge(ChallengeProfile challenge)
        {
            SelectedChallenge = challenge;
        }

        public float ApplyWeaponDamage(float damage, int ownerPlayerIndex = -1)
        {
            var scaled = SharedChallengeProfileModel.ApplyWeaponDamageMultiplier(damage, SelectedChallenge);

            if (ownerPlayerIndex >= 0 && ownerPlayerIndex < Players.Count)
            {
                var owner = Players[ownerPlayerIndex];
                if (owner != null)
                {
                    scaled *= owner.TemporaryDamageMultiplier;
                }
            }

            return scaled;
        }

        public void SelectMapAndStart(int mapIndex)
        {
            SelectedMap = ContentCatalog.Map(mapIndex);
            if (!SaveService.IsMapUnlocked(mapIndex))
            {
                return;
            }

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
            ClearExtractionBeacon();
            _extractionBeaconPlaceholder = null;
            if (_runRoot != null) Destroy(_runRoot.gameObject);
            Enemies.Clear();
            Players.Clear();
            RunSeed = seed == 0 ? 1 : seed;
            Rng = new RunRandom(RunSeed);
            _runRoot = new GameObject("Current Expedition").transform;
            _runRoot.SetParent(transform, false);
            CreateArena();
            _presentation.AttachRunAmbience(_runRoot);

            playerCount = Mathf.Clamp(playerCount, 1, 2);
            for (var i = 0; i < playerCount; i++)
            {
                var definition = DevelopmentTuningResolver.ResolveCharacter(
                    SelectedCharacters[Mathf.Clamp(i, 0, SelectedCharacters.Length - 1)]);
                var playerObject = new GameObject(definition.Name);
                playerObject.transform.SetParent(_runRoot, false);
                playerObject.transform.position = new Vector3((i - (playerCount - 1) * 0.5f) * 1.5f, 0f, 0f);
                var player = playerObject.AddComponent<PlayerController>();
                player.Initialize(this, i, definition);
                Players.Add(player);
            }
            _runModel.Begin(playerCount, BalanceRules.ExperienceToNext);
            _routeModel.Begin(SelectedMap.Id, SelectedChallenge);
            Kills = 0;
            RunRenown = 0;
            _spawnModel.Begin();
            _lootProgress.Begin();
            _temporaryEffect.Clear(Players);
            _playerHurtFeedback.Reset();
            _persistentZones.Clear();
            _runRecorded = false;
            _warlordEliteSpawned = false;
            EndPresentationPhase = RunEndPresentationPhase.Summary;
            EndBeatRemaining = 0f;
            ClearExtractionBeacon();
            SpawnRuneShards();
            State = RunState.Playing;
            var expeditionLabel = playerCount > 1
                ? $"{SelectedCharacters[0].Name.ToUpperInvariant()} + {SelectedCharacters[1].Name.ToUpperInvariant()} — {SelectedMap.Name.ToUpperInvariant()}"
                : $"{SelectedCharacters[0].Name.ToUpperInvariant()} — {SelectedMap.Name.ToUpperInvariant()}";
            SaveService.RecordLastRunCharacters(
                SelectedCharacters[0].Id,
                playerCount > 1 ? SelectedCharacters[1].Id : null);
            DiscoverRunStartCodex(playerCount);
            _hud.SetAnnouncement(replayingSeed
                ? $"REPLAYING SEED {RunSeed} — {expeditionLabel}"
                : expeditionLabel, 3f);
            var openingRouteAnnouncement = _routeModel.ConsumeAnnouncement();
            if (!string.IsNullOrEmpty(openingRouteAnnouncement))
                _hud.SetAnnouncement(openingRouteAnnouncement, 3.6f);
        }

        private void CreateArena()
        {
            var ground = new GameObject("Expedition Ground");
            ground.transform.SetParent(_runRoot, false);
            ground.transform.localScale = new Vector3(80f, 80f, 1f);
            var groundRenderer = ground.AddComponent<SpriteRenderer>();
            groundRenderer.sprite = RuntimeAssets.Circle;
            groundRenderer.color = SelectedMap.GroundColor;
            groundRenderer.sortingOrder = -20;

            CreateBiomeScatter(SelectedMap.LandmarkProfileId);
            CreateAuthoredLandmarks(SelectedMap.LandmarkProfileId);
            CreateObstacleLayout();
        }

        private void CreateObstacleLayout()
        {
            _obstacleLayout.Load(ObstacleLayoutCatalog.ForMap(SelectedMap));
            var root = new GameObject("Obstacle Layout");
            root.transform.SetParent(_runRoot, false);

            for (var i = 0; i < _obstacleLayout.Obstacles.Count; i++)
            {
                var obstacle = _obstacleLayout.Obstacles[i];
                var part = new GameObject($"Obstacle {i + 1}");
                part.transform.SetParent(root.transform, false);
                part.transform.position = new Vector3(obstacle.Center.x, obstacle.Center.y, 0f);
                ObstaclePresentation.Attach(part, obstacle);
            }
        }

        private void CreateBiomeScatter(string landmarkProfileId)
        {
            if (landmarkProfileId == BiomeCatalog.CanopyId)
            {
                for (var i = 0; i < 90; i++)
                {
                    var marker = new GameObject(i % 5 == 0 ? "Canopy Leaf" : "Root Shard");
                    marker.transform.SetParent(_runRoot, false);
                    marker.transform.position = new Vector3(Rng.Range(-38f, 38f), Rng.Range(-38f, 38f), 0f);
                    marker.transform.localScale = Vector3.one * Rng.Range(0.08f, 0.28f);
                    var renderer = marker.AddComponent<SpriteRenderer>();
                    renderer.sprite = i % 5 == 0 ? RuntimeAssets.Diamond : RuntimeAssets.Circle;
                    renderer.color = i % 5 == 0
                        ? new Color(0.28f, 0.52f, 0.24f)
                        : new Color(0.18f, 0.32f, 0.18f);
                    renderer.sortingOrder = -10;
                }

                return;
            }

            if (landmarkProfileId == BiomeCatalog.RelayId)
            {
                for (var i = 0; i < 90; i++)
                {
                    var marker = new GameObject(i % 5 == 0 ? "Signal Scrap" : "Relay Debris");
                    marker.transform.SetParent(_runRoot, false);
                    marker.transform.position = new Vector3(Rng.Range(-38f, 38f), Rng.Range(-38f, 38f), 0f);
                    marker.transform.localScale = Vector3.one * Rng.Range(0.08f, 0.28f);
                    var renderer = marker.AddComponent<SpriteRenderer>();
                    renderer.sprite = i % 5 == 0 ? RuntimeAssets.Diamond : RuntimeAssets.Circle;
                    renderer.color = i % 5 == 0
                        ? new Color(0.62f, 0.48f, 0.22f)
                        : new Color(0.28f, 0.26f, 0.24f);
                    renderer.sortingOrder = -10;
                }

                return;
            }

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

        private void CreateAuthoredLandmarks(string landmarkProfileId)
        {
            if (landmarkProfileId == BiomeCatalog.CanopyId)
            {
                CreateCanopyLandmarks();
                return;
            }

            if (landmarkProfileId == BiomeCatalog.RelayId)
            {
                CreateRelayLandmarks();
                return;
            }

            CreateFrostboundLandmarks();
        }

        private void CreateFrostboundLandmarks()
        {
            CreateDriftwoodWreck(new Vector3(-12f, -8f, 0f));
            CreateRuneCircle(new Vector3(8f, 6f, 0f));
            CreateBossApproachMarkers();
            _extractionBeaconPlaceholder = CreateLandmark(
                "Extraction Beacon Placeholder",
                new Vector3(SelectedMap.ExtractionBeaconX, SelectedMap.ExtractionBeaconY, 0f),
                RuntimeAssets.Diamond,
                ExpeditionPhase.Extraction,
                new Vector3(1.1f, 1.45f, 1f),
                -4);
        }

        private void CreateCanopyLandmarks()
        {
            CreateCanopyRoot(new Vector3(-10f, -7f, 0f));
            CreateMossCircle(new Vector3(7f, 5f, 0f));
            CreateBossApproachMarkers();
            _extractionBeaconPlaceholder = CreateLandmark(
                "Canopy Beacon Placeholder",
                new Vector3(SelectedMap.ExtractionBeaconX, SelectedMap.ExtractionBeaconY, 0f),
                RuntimeAssets.Diamond,
                ExpeditionPhase.Extraction,
                new Vector3(1.1f, 1.45f, 1f),
                -4);
        }

        private void CreateRelayLandmarks()
        {
            CreateSignalTower(new Vector3(-11f, -6f, 0f));
            CreateSupplyDepot(new Vector3(9f, 4f, 0f));
            CreateBossApproachMarkers();
            _extractionBeaconPlaceholder = CreateLandmark(
                "Extraction Point Placeholder",
                new Vector3(SelectedMap.ExtractionBeaconX, SelectedMap.ExtractionBeaconY, 0f),
                RuntimeAssets.Diamond,
                ExpeditionPhase.Extraction,
                new Vector3(1.1f, 1.45f, 1f),
                -4);
        }

        private void CreateCanopyRoot(Vector3 position)
        {
            var root = new GameObject("Ancestral Root");
            root.transform.SetParent(_runRoot, false);
            root.transform.position = position;
            AddLandmarkPart(root.transform, "Root Heart", RuntimeAssets.Circle,
                ExpeditionPhase.Driftwood, Vector3.zero, new Vector3(2.2f, 0.68f, 1f), -8);
            AddLandmarkPart(root.transform, "Thorn Branch", RuntimeAssets.Diamond,
                ExpeditionPhase.Driftwood, new Vector3(0.35f, 0.45f, 0f), new Vector3(0.24f, 0.95f, 1f), -7);
        }

        private void CreateMossCircle(Vector3 center)
        {
            var circleRoot = new GameObject("Moss Circle");
            circleRoot.transform.SetParent(_runRoot, false);
            circleRoot.transform.position = center;
            AddLandmarkPart(circleRoot.transform, "Moss Heart", RuntimeAssets.Circle,
                ExpeditionPhase.Shoreline, Vector3.zero, new Vector3(0.72f, 0.72f, 1f), -6);

            for (var i = 0; i < 6; i++)
            {
                var angle = i * (Mathf.PI * 2f / 6f);
                var offset = new Vector3(Mathf.Cos(angle) * 1.05f, Mathf.Sin(angle) * 1.05f, 0f);
                AddLandmarkPart(circleRoot.transform, $"Moss Stone {i + 1}", RuntimeAssets.Diamond,
                    ExpeditionPhase.Shoreline, offset, Vector3.one * 0.28f, -5);
            }
        }

        private void CreateSignalTower(Vector3 position)
        {
            var towerRoot = new GameObject("Broken Signal Tower");
            towerRoot.transform.SetParent(_runRoot, false);
            towerRoot.transform.position = position;
            AddLandmarkPart(towerRoot.transform, "Tower Base", RuntimeAssets.Circle,
                ExpeditionPhase.Driftwood, Vector3.zero, new Vector3(1.4f, 0.55f, 1f), -8);
            AddLandmarkPart(towerRoot.transform, "Broken Antenna", RuntimeAssets.Diamond,
                ExpeditionPhase.Driftwood, new Vector3(0.1f, 0.65f, 0f), new Vector3(0.18f, 1.1f, 1f), -7);
        }

        private void CreateSupplyDepot(Vector3 center)
        {
            var depotRoot = new GameObject("Supply Depot");
            depotRoot.transform.SetParent(_runRoot, false);
            depotRoot.transform.position = center;
            AddLandmarkPart(depotRoot.transform, "Crate Stack", RuntimeAssets.Circle,
                ExpeditionPhase.Shoreline, Vector3.zero, new Vector3(1.2f, 0.8f, 1f), -6);
            AddLandmarkPart(depotRoot.transform, "Signal Flag", RuntimeAssets.Diamond,
                ExpeditionPhase.Shoreline, new Vector3(0.55f, 0.35f, 0f), new Vector3(0.22f, 0.55f, 1f), -5);
        }

        private void CreateDriftwoodWreck(Vector3 position)
        {
            var wreckRoot = new GameObject("Driftwood Wreck");
            wreckRoot.transform.SetParent(_runRoot, false);
            wreckRoot.transform.position = position;
            wreckRoot.transform.rotation = Quaternion.Euler(0f, 0f, 18f);

            AddLandmarkPart(wreckRoot.transform, "Hull Spine", RuntimeAssets.Circle,
                ExpeditionPhase.Driftwood, new Vector3(0f, 0f, 0f), new Vector3(2.4f, 0.72f, 1f), -8);
            AddLandmarkPart(wreckRoot.transform, "Broken Mast", RuntimeAssets.Diamond,
                ExpeditionPhase.Driftwood, new Vector3(0.42f, 0.55f, 0f), new Vector3(0.22f, 1.05f, 1f), -7)
                .transform.rotation = Quaternion.Euler(0f, 0f, -12f);
            AddLandmarkPart(wreckRoot.transform, "Rib Timbers", RuntimeAssets.Diamond,
                ExpeditionPhase.Driftwood, new Vector3(-0.55f, -0.12f, 0f), new Vector3(0.95f, 0.38f, 1f), -7);
        }

        private void CreateRuneCircle(Vector3 center)
        {
            var circleRoot = new GameObject("Rune Circle");
            circleRoot.transform.SetParent(_runRoot, false);
            circleRoot.transform.position = center;

            AddLandmarkPart(circleRoot.transform, "Rune Heart", RuntimeAssets.Circle,
                ExpeditionPhase.Shoreline, Vector3.zero, new Vector3(0.72f, 0.72f, 1f), -6);

            for (var i = 0; i < 6; i++)
            {
                var angle = i * (Mathf.PI * 2f / 6f);
                var offset = new Vector3(Mathf.Cos(angle) * 1.05f, Mathf.Sin(angle) * 1.05f, 0f);
                AddLandmarkPart(circleRoot.transform, $"Rune Stone {i + 1}", RuntimeAssets.Diamond,
                    ExpeditionPhase.Shoreline, offset, Vector3.one * 0.28f, -5);
            }
        }

        private void CreateBossApproachMarkers()
        {
            var markerPositions = new[]
            {
                new Vector3(-6f, 9f, 0f),
                new Vector3(0f, 11f, 0f),
                new Vector3(6f, 9f, 0f)
            };

            for (var i = 0; i < markerPositions.Length; i++)
            {
                var marker = CreateLandmark(
                    $"Boss Approach Marker {i + 1}",
                    markerPositions[i],
                    RuntimeAssets.Diamond,
                    ExpeditionPhase.WarlordApproach,
                    new Vector3(0.34f, 0.62f, 1f),
                    -3);
                marker.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            }
        }

        private GameObject CreateLandmark(string landmarkName, Vector3 position, Sprite sprite,
            ExpeditionPhase homePhase, Vector3 scale, int sortingOrder)
        {
            var landmark = new GameObject(landmarkName);
            landmark.transform.SetParent(_runRoot, false);
            landmark.transform.position = position;
            landmark.transform.localScale = scale;
            AddPhaseTint(landmark, sprite, homePhase, sortingOrder);
            return landmark;
        }

        private GameObject AddLandmarkPart(Transform parent, string partName, Sprite sprite,
            ExpeditionPhase homePhase, Vector3 localPosition, Vector3 localScale, int sortingOrder)
        {
            var part = new GameObject(partName);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            AddPhaseTint(part, sprite, homePhase, sortingOrder);
            return part;
        }

        private static void AddPhaseTint(GameObject target, Sprite sprite, ExpeditionPhase homePhase, int sortingOrder)
        {
            var renderer = target.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            target.AddComponent<FrostboundLandmarkTint>().Initialize(renderer, homePhase);
        }

        private void SpawnEnemy(bool boss, float timeDifficulty, bool elite = false)
        {
            if (Players.Count == 0)
            {
                return;
            }

            var enemyLevel = BalanceRules.ComputeEnemyLevel(_runModel.Level, boss, elite);
            var effectiveDifficulty = BalanceRules.ResolveEffectiveDifficulty(timeDifficulty, enemyLevel);
            var position = _obstacleLayout.ResolveSpawnPosition(GroupCenter, Rng,
                SharedSpawnModel.MinimumSpawnDistance, SharedSpawnModel.MaximumSpawnDistance, 0.36f);
            var enemy = _enemyPool.Get(position);
            enemy.Initialize(this, effectiveDifficulty, enemyLevel, boss, elite);
            Enemies.Add(enemy);
            _enemyGrid.Add(enemy);
        }

        private void SpawnBossWaveWithEntrance(float timeDifficulty, int bossCount)
        {
            SpawnBossWithEntrance(timeDifficulty, true);
            if (bossCount <= 1)
            {
                return;
            }

            SpawnBossWithEntrance(timeDifficulty, false);
            Present(PresentationCue.BossSpawn, GroupCenter, new Color(1f, 0.18f, 0.08f), 4.4f);
            _cameraFollow.AddTrauma(0.42f);
            _hud.SetAnnouncement(BiomeCatalog.ResolveTwinBossEntranceAnnouncement(SelectedMap.BiomeId), 5.4f);
        }

        private void SpawnBossWithEntrance(float timeDifficulty, bool announceEntrance)
        {
            if (Players.Count == 0)
            {
                return;
            }

            var enemyLevel = BalanceRules.ComputeEnemyLevel(_runModel.Level, true, false);
            var effectiveDifficulty = BalanceRules.ResolveEffectiveDifficulty(timeDifficulty, enemyLevel);
            var position = _obstacleLayout.ResolveSpawnPosition(GroupCenter, Rng, 13.5f, 16.5f, 0.85f);
            var enemy = _enemyPool.Get(position);
            enemy.Initialize(this, effectiveDifficulty, enemyLevel, true, false);
            Enemies.Add(enemy);
            _enemyGrid.Add(enemy);

            Present(PresentationCue.BossSpawn, position, new Color(1f, 0.34f, 0.16f), 3.2f);
            _cameraFollow.AddTrauma(announceEntrance ? 0.62f : 0.38f);

            if (announceEntrance)
            {
                _hud.SetAnnouncement(SelectedMap.BossEntranceAnnouncement, 4.8f);
            }
        }

        private void TrySpawnWarlordElite(float timeDifficulty)
        {
            if (_warlordEliteSpawned || _routeModel.BossSpawned || _routeModel.BossKilled)
                return;

            if (_routeModel.CurrentPhase != ExpeditionPhase.WarlordApproach)
                return;

            var halfObjective = _routeModel.RequiredKillObjective / 2;
            if (_routeModel.DraugrKills < halfObjective)
                return;

            _warlordEliteSpawned = true;
            SpawnEnemy(false, timeDifficulty, true);
            _hud.SetAnnouncement(SelectedMap.EliteSpawnAnnouncement, 3.2f);
        }

        private void SpawnRuneShards()
        {
            _shardPool?.ReleaseAll();
            var shardCount = SelectedMap.OptionalShardObjective;
            if (shardCount <= 0)
                return;

            for (var i = 0; i < shardCount; i++)
            {
                var angle = Rng.Range(0f, Mathf.PI * 2f);
                var distance = Rng.Range(7f, 30f);
                var position = new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, 0f);
                _shardPool.Get(position).Initialize(this);
            }
        }

        private void SpawnExtractionBeacon()
        {
            ClearExtractionBeacon();
            if (_extractionBeaconPlaceholder != null)
                _extractionBeaconPlaceholder.SetActive(false);

            _extractionBeacon = new GameObject("Extraction Beacon");
            _extractionBeacon.transform.SetParent(_runRoot, false);
            _extractionBeacon.transform.position = new Vector3(
                SelectedMap.ExtractionBeaconX, SelectedMap.ExtractionBeaconY, 0f);
            _extractionBeacon.transform.localScale = Vector3.one * 1.6f;
            var renderer = _extractionBeacon.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeAssets.Diamond;
            renderer.color = new Color(0.96f, 0.78f, 0.22f, 0.82f);
            renderer.sortingOrder = 3;
        }

        private void ClearExtractionBeacon()
        {
            if (_extractionBeacon == null)
                return;

            Destroy(_extractionBeacon);
            _extractionBeacon = null;

            if (_extractionBeaconPlaceholder != null)
                _extractionBeaconPlaceholder.SetActive(true);
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

        public int ResolveAreaEffect(Vector2 center, SharedEffectRequest effect, bool repelExperienceGems = false,
            int ownerPlayerIndex = -1)
        {
            if (effect.Kind != SharedEffectKind.AreaDamage &&
                effect.Kind != SharedEffectKind.PersistentZone)
            {
                return 0;
            }

            if (effect.Target != SharedEffectTarget.Enemies)
            {
                return 0;
            }

            if (repelExperienceGems)
            {
                _gemRepulsionOrigin = center;
                _gemRepulsionRadius = effect.Radius;
                _gemRepulsionUntil = Time.time + UltimateGemBurstDuration;
            }

            var hitCount = 0;
            GetEnemiesInRadius(center, effect.Radius + 0.9f, _spatialScratch);
            for (var i = _spatialScratch.Count - 1; i >= 0; i--)
            {
                var enemy = _spatialScratch[i];
                if (enemy == null || !enemy.Alive)
                {
                    continue;
                }

                if ((enemy.Position - center).sqrMagnitude <=
                    (effect.Radius + enemy.Radius) * (effect.Radius + enemy.Radius))
                {
                    enemy.TakeDamage(ApplyWeaponDamage(effect.Damage, ownerPlayerIndex), effect.Knockback, center);
                    hitCount++;
                }
            }

            return hitCount;
        }

        public void SpawnPersistentZone(
            Vector2 position,
            float radius,
            float damagePerTick,
            float knockback,
            float duration,
            float tickInterval,
            bool followOwner,
            int ownerPlayerIndex)
        {
            _persistentZones.Spawn(
                position,
                radius,
                damagePerTick,
                knockback,
                duration,
                tickInterval,
                followOwner,
                ownerPlayerIndex);
        }

        private void AdvancePersistentZones(float deltaTime)
        {
            _persistentZones.Advance(deltaTime, ResolvePlayerPosition);

            for (var i = 0; i < _persistentZones.ActiveZones.Count; i++)
            {
                if (!_persistentZones.TryConsumeTick(i, out var effect, out var center))
                {
                    continue;
                }

                var ownerPlayerIndex = _persistentZones.ActiveZones[i].OwnerPlayerIndex;
                ResolveAreaEffect(center, effect, false, ownerPlayerIndex);
                Present(PresentationCue.ProjectileTrail, center, new Color(0.55f, 0.82f, 0.42f), 0.22f);
            }
        }

        private Vector2 ResolvePlayerPosition(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= Players.Count || Players[playerIndex] == null)
            {
                return Vector2.zero;
            }

            return Players[playerIndex].transform.position;
        }

        public void OnEnemyKilled(Enemy enemy, int experienceValue, bool boss, bool elite)
        {
            if (enemy == null) return;
            Kills++;
            if (Rng.Chance(SharedMetaProgressionModel.RenownKillPickupChance)) RunRenown++;
            var position = enemy.transform.position;
            var gem = _gemPool.Get(position);
            gem.Initialize(this, experienceValue);

            if (Time.time <= _gemRepulsionUntil)
            {
                gem.ApplyUltimateRepulsion(
                    _gemRepulsionOrigin,
                    _gemRepulsionRadius,
                    _gemRepulsionUntil - Time.time);
            }

            Present(PresentationCue.EnemyDefeated, position,
                boss ? new Color(1f, 0.45f, 0.22f) : new Color(0.5f, 0.82f, 0.9f),
                boss ? 2.4f : 0.7f);

            if (!boss && _lootProgress.TryRollDrop(Level, Kills, Players.Count, Rng, out var droppedDefinition))
            {
                var pickup = _lootPool.Get(position);
                pickup.Initialize(this, droppedDefinition);
            }

            ReleaseEnemy(enemy);
            _routeModel.OnEnemyKilled(boss, elite);

            var routeAnnouncement = _routeModel.ConsumeAnnouncement();
            if (!string.IsNullOrEmpty(routeAnnouncement))
                _hud.SetAnnouncement(routeAnnouncement, 3.6f);

            if (boss && _routeModel.BossKilled)
            {
                SpawnExtractionBeacon();
            }
        }

        public void OnLootCollected(LootEffectDefinition definition, int collectorPlayerIndex)
        {
            var tracked = definition ?? LootEffectCatalog.DefaultRunLoot;
            var result = _lootProgress.OnCollected(tracked, _temporaryEffect.IsActive);
            Present(PresentationCue.LootCollected, ResolvePlayerPosition(collectorPlayerIndex),
                tracked.ThemeColor, 0.45f);

            if (result == LootCollectResult.DiscardedWhileActive)
            {
                _hud.SetAnnouncement($"{tracked.DisplayName.ToUpperInvariant()} — ALREADY ACTIVE", 1.4f);
                return;
            }

            if (result == LootCollectResult.Incremented)
            {
                var currentCount = _lootProgress.GetCount(tracked);
                var requiredCount = _lootProgress.GetRequiredCount(tracked);
                _hud.SetAnnouncement(
                    $"+1 {tracked.DisplayName.ToUpperInvariant()}  ({currentCount}/{requiredCount})",
                    1.8f);
                return;
            }

            if (result != LootCollectResult.Activated)
            {
                return;
            }

            _temporaryEffect.Activate(tracked, collectorPlayerIndex, Players);
            Present(PresentationCue.LootActivated, GroupCenter, tracked.ThemeColor, 1.6f);
            _hud.SetAnnouncement($"{tracked.DisplayName.ToUpperInvariant()} ACTIVATED", 2.4f);
        }

        private void HandleTemporaryEffectPresentation()
        {
            if (_temporaryEffect.JustExpired)
            {
                Present(PresentationCue.TemporaryEffectExpired, GroupCenter,
                    _temporaryEffect.LastExpiredThemeColor, 0.8f);
            }

            for (var i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if (player == null)
                {
                    continue;
                }

                player.UpdateTemporaryEffectPresentation(_temporaryEffect);
            }
        }

        public void ReleaseLootPickup(LootPickup pickup) => _lootPool.Release(pickup);

        public void OnRuneShardCollected(RuneShardPickup shard)
        {
            _routeModel.OnShardCollected();
            ReleaseRuneShard(shard);
        }

        public void OnPlayerDowned(PlayerController player)
        {
            Present(PresentationCue.PlayerDowned, player.transform.position, player.Definition.Color, 1.4f);
            var living = 0;
            for (var i = 0; i < Players.Count; i++)
                if (Players[i] != null && Players[i].IsAlive) living++;
            if (living == 0) EndRun(false);
            else _hud.SetAnnouncement($"{player.HeroName.ToUpperInvariant()} IS DOWN — STAY CLOSE TO REVIVE", 3f);
        }

        public void OnPlayerRevived(PlayerController player)
        {
            Present(PresentationCue.PlayerRevived, player.transform.position, player.Definition.Color, 1.1f);
            _hud.SetAnnouncement($"{player.HeroName.ToUpperInvariant()} RETURNS TO THE SAGA", 2.2f);
        }

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
            _currentRewards.AddRange(RewardFactory.Generate(builds, RewardTurnPlayerIndex, Players.Count, Rng,
                SharedChallengeProfileModel.AllowsHealingRewards(SelectedChallenge)));
            DiscoverLevelUpOffers(_currentRewards);
            State = RunState.LevelUp;
            Time.timeScale = 0f;
            Present(PresentationCue.LevelUp, GroupCenter, PresentationTheme.Accent);
        }

        private static void DiscoverLevelUpOffers(IReadOnlyList<RewardOption> rewards)
        {
            for (var i = 0; i < rewards.Count; i++)
            {
                var item = rewards[i].Item;
                if (item == null)
                {
                    continue;
                }

                DiscoverRewardItemCodex(item);
            }
        }

        private static void DiscoverRewardItemCodex(ItemDefinition item)
        {
            if (item == null)
            {
                return;
            }

            SaveService.DiscoverCodex(item.Id);
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
            DiscoverAppliedReward(option);
            _hud.SetAnnouncement($"{chooser.ToUpperInvariant()} CHOSE {option.Item.Name.ToUpperInvariant()} FOR {destination}", 3.2f);
            Present(PresentationCue.Confirm, GroupCenter, option.Item.Color);
            _runModel.CompleteReward();
            State = RunState.Playing;
            Time.timeScale = 1f;
        }

        public void EndRun(bool victory)
        {
            if (!_runModel.Complete(victory)) return;
            State = _runModel.Outcome == RunOutcome.Victory ? RunState.Victory : RunState.GameOver;
            Time.timeScale = 0f;

            if (victory)
            {
                EndCause = _routeModel.ExtractionCompletion == ExtractionCompletionKind.Timeout
                    ? RunEndCause.VictoryTimeout
                    : RunEndCause.VictoryExtraction;
            }
            else
            {
                EndCause = RunEndCause.DefeatPartyWiped;
            }

            EndPresentationPhase = RunEndPresentationPhase.Beat;
            EndBeatRemaining = 2.8f;

            Present(victory ? PresentationCue.Victory : PresentationCue.Defeat, GroupCenter,
                victory ? PresentationTheme.Accent : new Color(0.65f, 0.2f, 0.22f), 2f);
            if (!_runRecorded)
            {
                var characterIds = new string[Mathf.Max(1, Players.Count)];
                for (var i = 0; i < characterIds.Length; i++)
                {
                    characterIds[i] = SelectedCharacters[Mathf.Clamp(i, 0, SelectedCharacters.Length - 1)].Id;
                }

                var renownMultiplier = SharedChallengeProfileModel.ResolveRenownMultiplier(SelectedChallenge);
                LastRunRenownEarned = SharedMetaProgressionModel.CalculateRunRenownEarned(
                    RunRenown, Kills, victory, renownMultiplier);
                SaveService.RecordRun(Kills, RunRenown, Elapsed, victory, renownMultiplier, characterIds);
                if (victory)
                {
                    SaveService.RecordVictoryChallengeUnlocks(SelectedMap.Id);

                    var relicId = _routeModel.ResolveVictoryRelicId();
                    if (!string.IsNullOrEmpty(relicId))
                        SaveService.RecordRelicCollected(relicId);
                }

                _runRecorded = true;
            }
        }

        public void AdvanceEndBeat(float deltaTime)
        {
            if (EndPresentationPhase != RunEndPresentationPhase.Beat)
            {
                return;
            }

            EndBeatRemaining = Mathf.Max(0f, EndBeatRemaining - deltaTime);
            if (EndBeatRemaining <= 0f)
            {
                EndPresentationPhase = RunEndPresentationPhase.Summary;
            }
        }

        public void SkipEndBeat()
        {
            if (EndPresentationPhase != RunEndPresentationPhase.Beat)
            {
                return;
            }

            EndPresentationPhase = RunEndPresentationPhase.Summary;
            EndBeatRemaining = 0f;
        }

        private void DiscoverRunStartCodex(int playerCount)
        {
            SaveService.DiscoverCodex(SelectedMap.Id);

            for (var i = 0; i < playerCount; i++)
            {
                var character = SelectedCharacters[Mathf.Clamp(i, 0, SelectedCharacters.Length - 1)];
                SaveService.DiscoverCodex(character.Id);

                for (var w = 0; w < character.StarterWeaponIds.Length; w++)
                {
                    SaveService.DiscoverCodex(character.StarterWeaponIds[w]);
                }
            }
        }

        private static void DiscoverAppliedReward(RewardOption option)
        {
            if (option?.Item == null)
            {
                return;
            }

            DiscoverRewardItemCodex(option.Item);

            if (option.Item.IsEvolution)
            {
                SaveService.DiscoverCodex(option.Item.Id);
            }
        }

        public void EnterCamp()
        {
            State = RunState.MainMenu;
            Time.timeScale = 1f;
        }

        public void ReturnToTitleScreen()
        {
            Time.timeScale = 1f;
            State = RunState.TitleScreen;
        }

        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            _runModel.Reset();
            State = RunState.MainMenu;
            EndPresentationPhase = RunEndPresentationPhase.Summary;
            EndBeatRemaining = 0f;
            LocalInputRouter.BeginSession(1);
            ReleasePooledSimulation();
            ClearExtractionBeacon();
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

        public void OpenSettings()
        {
            if (State != RunState.MainMenu && State != RunState.Paused && State != RunState.TitleScreen) return;
            _settingsReturnState = State;
            State = RunState.Settings;
            Time.timeScale = 0f;
        }

        public void CloseSettings()
        {
            if (State != RunState.Settings) return;
            LocalInputRouter.CancelRebind();
            State = _settingsReturnState;
            Time.timeScale = State == RunState.MainMenu || State == RunState.TitleScreen ? 1f : 0f;
        }

        public void ToggleDevelopmentTuning()
        {
            if (ShowDevelopmentTuning)
            {
                ShowDevelopmentTuning = false;
                Time.timeScale = _developmentTuningPreviousTimeScale;
                return;
            }

            _developmentTuningPreviousTimeScale = Time.timeScale;
            ShowDevelopmentTuning = true;
            Time.timeScale = 0f;
        }

        public void CloseDevelopmentTuning()
        {
            if (!ShowDevelopmentTuning)
            {
                return;
            }

            ShowDevelopmentTuning = false;
            Time.timeScale = _developmentTuningPreviousTimeScale;
        }

        public void ApplyDevelopmentTuningToActiveRun()
        {
            if (State != RunState.Playing)
            {
                return;
            }

            _lootProgress.Begin();

            for (var i = 0; i < Players.Count; i++)
            {
                var resolved = DevelopmentTuningResolver.ResolveCharacter(Players[i].Definition);
                Players[i].ReinitializeFromDefinition(resolved);
            }
        }

        public void ShowPulse(Vector2 position, int playerIndex)
        {
            _pulsePool.Get(position).Initialize(this, playerIndex);
            Present(PresentationCue.RavenGuard, position,
                playerIndex == 0 ? PresentationTheme.Frost : new Color(1f, 0.58f, 0.22f), 1f);
        }

        public void Announce(string message, float duration) => _hud.SetAnnouncement(message, duration);

        public void ShowUltimate(Vector2 position, int playerIndex, float radius)
        {
            _ultimatePulsePool.Get(position).Initialize(this, playerIndex, radius);
            Present(PresentationCue.Ultimate, position,
                playerIndex == 0 ? PresentationTheme.Frost : new Color(1f, 0.58f, 0.22f),
                Mathf.Max(1f, radius / 4f));
        }

        public void Present(PresentationCue cue, Vector2 position, Color color, float scale = 1f) =>
            _presentation?.Notify(cue, position, color, scale);

        public void PresentPlayerHurt(
            PlayerController player,
            float damage,
            Vector2 source,
            PlayerHurtHitKind hitKind,
            float healthFractionBeforeDamage)
        {
            if (player == null)
            {
                return;
            }

            var damageRatio = damage / Mathf.Max(1f, player.MaxHealth);
            var severity = PlayerHurtSeverity.Resolve(damageRatio, hitKind);
            player.PresentDamageTaken(source);
            _playerHurtFeedback.RegisterHit(player.PlayerIndex, healthFractionBeforeDamage, damageRatio, severity);
            _presentation?.NotifyPlayerHurt(
                player.transform.position,
                severity.VfxScale,
                severity.Trauma,
                severity.Pitch,
                player.PlayerIndex,
                Players.Count,
                severity);
        }

        public void AddCameraTrauma(float amount) => _cameraFollow?.AddTrauma(amount);

        public float DistanceToNearestLivingPlayer(Vector2 position)
        {
            var nearestDistance = float.MaxValue;
            for (var i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if (player == null || !player.IsAlive)
                {
                    continue;
                }

                var distance = Vector2.Distance(position, player.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                }
            }

            return nearestDistance == float.MaxValue ? 999f : nearestDistance;
        }

        public float ResolveBossProximityPressure()
        {
            if (Enemies.Count == 0)
            {
                return 0f;
            }

            var partyCenter = GroupCenter;
            var nearestBossDistance = float.MaxValue;
            for (var i = 0; i < Enemies.Count; i++)
            {
                var enemy = Enemies[i];
                if (enemy == null || !enemy.Alive || !enemy.Boss)
                {
                    continue;
                }

                nearestBossDistance = Mathf.Min(nearestBossDistance,
                    Vector2.Distance(partyCenter, enemy.Position));
            }

            if (nearestBossDistance > 10f)
            {
                return 0f;
            }

            return 1f - nearestBossDistance / 10f;
        }

        public void ResolveBossSlam(Vector2 center, float radius, float damage, float visualScale)
        {
            Present(PresentationCue.BossSpawn, center, new Color(1f, 0.22f, 0.1f), visualScale * 1.35f);
            AddCameraTrauma(0.3f);

            for (var i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if (player == null || !player.IsAlive)
                {
                    continue;
                }

                var playerPosition = (Vector2)player.transform.position;
                var hitRadius = radius + BalanceRules.PlayerCollisionRadius;
                if ((playerPosition - center).sqrMagnitude > hitRadius * hitRadius)
                {
                    continue;
                }

                player.TakeDamage(
                    damage,
                    DevelopmentTuningResolver.PlayerBossSlamKnockback,
                    center,
                    PlayerHurtHitKind.BossSlam);
            }
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

        public void ReleaseRuneShard(RuneShardPickup shard) => _shardPool.Release(shard);

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
            _shardPool = new ComponentPool<RuneShardPickup>(() => CreatePooled<RuneShardPickup>("Pooled Rune Shard"), _poolRoot, 16);
            _lootPool = new ComponentPool<LootPickup>(() => CreatePooled<LootPickup>("Pooled Loot"), _poolRoot, 32);
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
            _shardPool?.ReleaseAll();
            _lootPool?.ReleaseAll();
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
        private float _trauma;

        public void AddTrauma(float amount) => _trauma = Mathf.Clamp01(_trauma + amount);

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

            var bossPressure = ResolveBossMenacePressure(center);
            var desired = new Vector3(center.x, center.y, -10f);
            var basePosition = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            _trauma = Mathf.Max(0f, _trauma - Time.unscaledDeltaTime * 1.8f);
            var shake = _trauma * _trauma * PresentationPreferences.Data.ScreenShake;
            var offset = new Vector3(Mathf.Sin(Time.unscaledTime * 73f), Mathf.Cos(Time.unscaledTime * 61f), 0f) * shake * 0.22f;
            transform.position = basePosition + offset;
            var camera = GetComponent<Camera>();
            var targetSize = Mathf.Clamp(6f + maximumDistance * 0.62f - bossPressure * 1.35f, 5.4f, 9.5f);
            camera.orthographicSize = Mathf.Lerp(camera.orthographicSize, targetSize, 1f - Mathf.Exp(-5f * Time.unscaledDeltaTime));
        }

        private float ResolveBossMenacePressure(Vector2 center)
        {
            if (Director == null)
            {
                return 0f;
            }

            var nearestBossDistance = float.MaxValue;
            for (var i = 0; i < Director.Enemies.Count; i++)
            {
                var enemy = Director.Enemies[i];
                if (enemy == null || !enemy.Alive || !enemy.Boss)
                {
                    continue;
                }

                nearestBossDistance = Mathf.Min(nearestBossDistance,
                    Vector2.Distance(center, enemy.Position));
            }

            if (nearestBossDistance > 11f)
            {
                return 0f;
            }

            return 1f - nearestBossDistance / 11f;
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

    public sealed class FrostboundLandmarkTint : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private ExpeditionPhase _homePhase;
        private GameDirector _director;

        public void Initialize(SpriteRenderer renderer, ExpeditionPhase homePhase)
        {
            _renderer = renderer;
            _homePhase = homePhase;
            _renderer.color = DimmedPhaseColor(homePhase);
        }

        private void Update()
        {
            if (_renderer == null)
                return;

            if (_director == null)
            {
                var directors = Object.FindObjectsByType<GameDirector>();
                _director = directors.Length > 0 ? directors[0] : null;
            }

            if (_director == null || _director.State != RunState.Playing)
            {
                _renderer.color = DimmedPhaseColor(_homePhase);
                return;
            }

            var currentPhase = _director.Route.CurrentPhase;
            var phaseActive = IsPhaseActive(currentPhase);
            var phaseColor = PhaseColor(_homePhase);
            var alpha = phaseActive ? 0.84f : 0.28f;

            if (_homePhase == ExpeditionPhase.Extraction &&
                (currentPhase == ExpeditionPhase.Extraction || currentPhase == ExpeditionPhase.Completed))
            {
                alpha = 0.92f;
            }

            _renderer.color = new Color(phaseColor.r, phaseColor.g, phaseColor.b, alpha);
        }

        private bool IsPhaseActive(ExpeditionPhase currentPhase)
        {
            if (currentPhase == ExpeditionPhase.Completed)
                return _homePhase == ExpeditionPhase.Extraction;

            return (int)currentPhase >= (int)_homePhase;
        }

        private static Color PhaseColor(ExpeditionPhase phase)
        {
            switch (phase)
            {
                case ExpeditionPhase.Shoreline:
                    return new Color(0.22f, 0.45f, 0.49f);
                case ExpeditionPhase.Driftwood:
                    return new Color(0.42f, 0.32f, 0.22f);
                case ExpeditionPhase.WarlordApproach:
                    return new Color(0.55f, 0.38f, 0.42f);
                case ExpeditionPhase.Boss:
                    return new Color(0.72f, 0.22f, 0.28f);
                case ExpeditionPhase.Extraction:
                    return new Color(0.96f, 0.78f, 0.22f);
                default:
                    return new Color(0.17f, 0.25f, 0.28f);
            }
        }

        private static Color DimmedPhaseColor(ExpeditionPhase phase)
        {
            var color = PhaseColor(phase);
            return new Color(color.r, color.g, color.b, 0.24f);
        }
    }
}
