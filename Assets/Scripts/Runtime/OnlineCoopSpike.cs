using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace ProjectExpedition
{
    /// <summary>
    /// Host-authoritative two-player expedition. Player commands travel to the
    /// host while compact snapshots replicate players, enemies and run state.
    /// Swarm enemies intentionally remain plain simulation records instead of
    /// one NetworkObject per enemy.
    /// </summary>
    public sealed class OnlineCoopSpike : MonoBehaviour
    {
        private const string InputMessage = "expedition.input.v2";
        private const string SnapshotMessage = "expedition.snapshot.v2";
        private const string UpgradeMessage = "expedition.upgrade.v1";
        private const string AttackMessage = "expedition.attack.v1";
        private const string PulseMessage = "expedition.pulse.v1";
        private const ushort Port = 7777;
        private const float SnapshotRate = 15f;
        private const int MaximumEnemies = 96;

        internal enum OnlinePhase : byte
        {
            Lobby,
            Playing,
            LevelUp,
            Victory,
            Defeat
        }

        private sealed class NetPlayer
        {
            public ulong Id;
            public Vector2 Position;
            public Vector2 Input;
            public float MaxHealth;
            public float Health;
            public float MoveSpeed;
            public float Armor;
            public float AttackTimer = 0.25f;
            public float PulseTimer = 4.5f;
            public float UltimateRemaining;
            public float Invulnerability;
            public float ReviveProgress;
            public bool Downed;
            public bool UltimateQueued;
            public readonly PlayerBuild Build = new PlayerBuild();
            public float AxeDamage = 24f;
            public float AxeCooldown = 0.82f;
            public int AxeCount = 1;
            public int AxePierce = 1;
            public float CriticalChance = 0.08f;
            public float ShieldDamage = 20f;
            public int UltimateCooldownUpgrades;
            public float UltimateDamageMultiplier = 1f;
            public bool FrostAxeEvolved;
            public bool RavenGuardEvolved;
        }

        private sealed class NetEnemy
        {
            public int Id;
            public Vector2 Position;
            public float MaxHealth;
            public float Health;
            public float Speed;
            public float Radius;
            public float AttackCooldown;
            public bool Boss;
        }

        public bool Visible { get; private set; }
        public bool SessionActive => _networkManager != null && _networkManager.IsListening;

        private GameDirector _director;
        private NetworkManager _networkManager;
        private UnityTransport _transport;
        private GameObject _networkObject;
        private Transform _visualRoot;
        private Transform _arenaRoot;
        private Camera _camera;
        private string _address = "127.0.0.1";
        private string _status = "Ready for a two-player expedition";
        private OnlinePhase _phase = OnlinePhase.Lobby;
        private float _sessionRestartAllowedAt;
        private float _sendInputTimer;
        private float _snapshotTimer;
        private float _spawnTimer;
        private float _elapsed;
        private int _level = 1;
        private int _experience;
        private int _experienceToNext = 78;
        private int _kills;
        private int _renown;
        private bool _bossSpawned;
        private bool _runRecorded;
        private int _nextEnemyId = 1;
        private int _snapshotsSent;
        private int _snapshotsReceived;
        private int _lastSnapshotBytes;
        private bool _localUltimateQueued;
        private int _mapIndex;
        private int _upgradeSelection;
        private int _lobbySelection;
        private int _resultSelection;
        private float _inputReadyAt;
        private bool _showBuildDetails;
        private int _lastRewardItemIndex = -1;
        private int _lastRewardTargetPlayerIndex;
        private int _lastRewardChooserPlayerIndex;
        private bool _lastRewardShared;

        private readonly Dictionary<ulong, NetPlayer> _players = new Dictionary<ulong, NetPlayer>();
        private readonly Dictionary<int, NetEnemy> _enemies = new Dictionary<int, NetEnemy>();
        private readonly Dictionary<ulong, Vector2> _playerTargets = new Dictionary<ulong, Vector2>();
        private readonly Dictionary<int, Vector2> _enemyTargets = new Dictionary<int, Vector2>();
        private readonly Dictionary<ulong, GameObject> _playerViews = new Dictionary<ulong, GameObject>();
        private readonly Dictionary<int, OnlineEnemyView> _enemyViews = new Dictionary<int, OnlineEnemyView>();
        private readonly List<RewardOption> _onlineRewards = new List<RewardOption>(4);
        private readonly List<int> _enemyIdScratch = new List<int>(MaximumEnemies);
        private readonly List<NetEnemy> _onlineSpatialScratch = new List<NetEnemy>(MaximumEnemies);
        private readonly SpatialHashGrid<NetEnemy> _onlineEnemyGrid = new SpatialHashGrid<NetEnemy>(2.5f, enemy => enemy.Position);
        private readonly SharedRunModel _onlineRunModel = new SharedRunModel();
        private ComponentPool<OnlineEnemyView> _onlineEnemyViewPool;
        private ComponentPool<OnlineAttackVisual> _onlineAttackPool;
        private ComponentPool<OnlinePulseVisual> _onlinePulsePool;
        private int _rewardTurnPlayerIndex;

        private MapDefinition OnlineMap => ContentCatalog.Map(_mapIndex);

        internal static OnlinePhase ProjectOnlinePhase(RunSimulationPhase phase, RunOutcome outcome)
        {
            switch (phase)
            {
                case RunSimulationPhase.Playing: return OnlinePhase.Playing;
                case RunSimulationPhase.Reward: return OnlinePhase.LevelUp;
                case RunSimulationPhase.Completed:
                    return outcome == RunOutcome.Victory ? OnlinePhase.Victory : OnlinePhase.Defeat;
                default: return OnlinePhase.Lobby;
            }
        }

        private void SyncHostRunProjection()
        {
            _phase = ProjectOnlinePhase(_onlineRunModel.Phase, _onlineRunModel.Outcome);
            _elapsed = _onlineRunModel.Elapsed;
            _level = _onlineRunModel.Level;
            _experience = _onlineRunModel.Experience;
            _experienceToNext = _onlineRunModel.ExperienceToNext;
            _bossSpawned = _onlineRunModel.BossTriggered;
            _rewardTurnPlayerIndex = _onlineRunModel.RewardTurnPlayerIndex;
        }

        public void Initialize(GameDirector director) => _director = director;

        public void Show()
        {
            Visible = true;
            _onlineRunModel.Reset();
            _phase = OnlinePhase.Lobby;
            _status = "Start a Host, then Join from the second instance";
            _inputReadyAt = Time.unscaledTime + 0.25f;
            _camera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            EnsureVisualRoot();
        }

        public void Hide()
        {
            Visible = false;
            ShutdownSession();
        }

        private void Update()
        {
            if (!Visible) return;
            if (!SessionActive)
            {
                UpdateOfflineLobbyInput();
                return;
            }
            if (_phase == OnlinePhase.Lobby) UpdateConnectedLobbyInput();
            if (!SessionActive) return;
            if (_phase == OnlinePhase.Playing && LocalInputRouter.DetailsPressed())
                _showBuildDetails = !_showBuildDetails;
            if (_networkManager.IsServer) UpdateHost();
            else if (_networkManager.IsConnectedClient) UpdateClient();
            UpdateVisuals();
        }

        private void UpdateHost()
        {
            if (_players.TryGetValue(NetworkManager.ServerClientId, out var host))
            {
                host.Input = _phase == OnlinePhase.Playing && !_showBuildDetails
                    ? LocalInputRouter.ReadMovement(0, 1)
                    : Vector2.zero;
                if (_phase == OnlinePhase.Playing && !_showBuildDetails && LocalInputRouter.UltimatePressed(0, 1))
                    host.UltimateQueued = true;
            }

            if (_phase == OnlinePhase.LevelUp)
            {
                if (_rewardTurnPlayerIndex == 0)
                {
                    var choice = OnlineUpgradeInput();
                    if (choice >= 0) ResolveUpgrade(choice);
                }
            }
            else if (_phase == OnlinePhase.Victory || _phase == OnlinePhase.Defeat)
            {
                UpdateOnlineResultsInput();
                if (!SessionActive) return;
            }
            else if (_phase == OnlinePhase.Playing)
            {
                SimulateRun(Time.unscaledDeltaTime);
            }

            _snapshotTimer -= Time.unscaledDeltaTime;
            if (_snapshotTimer <= 0f)
            {
                SendSnapshot();
                _snapshotTimer = 1f / SnapshotRate;
            }
        }

        private void UpdateClient()
        {
            if (_phase == OnlinePhase.Playing && !_showBuildDetails && LocalInputRouter.UltimatePressed(0, 1))
                _localUltimateQueued = true;
            if (_phase == OnlinePhase.LevelUp)
            {
                if (_rewardTurnPlayerIndex == 1)
                {
                    var choice = OnlineUpgradeInput();
                    if (choice >= 0) SendUpgradeChoice(choice);
                }
            }
            else if (_phase == OnlinePhase.Victory || _phase == OnlinePhase.Defeat)
            {
                UpdateOnlineResultsInput();
                if (!SessionActive) return;
            }

            _sendInputTimer -= Time.unscaledDeltaTime;
            if (_sendInputTimer > 0f) return;
            _sendInputTimer = 1f / 30f;
            var move = _phase == OnlinePhase.Playing && !_showBuildDetails
                ? LocalInputRouter.ReadMovement(0, 1)
                : Vector2.zero;
            using (var writer = new FastBufferWriter(16, Allocator.Temp))
            {
                writer.WriteValueSafe(move.x);
                writer.WriteValueSafe(move.y);
                writer.WriteValueSafe(_localUltimateQueued);
                _networkManager.CustomMessagingManager.SendNamedMessage(
                    InputMessage, NetworkManager.ServerClientId, writer, NetworkDelivery.UnreliableSequenced);
            }
            _localUltimateQueued = false;
        }

        private void SimulateRun(float deltaTime)
        {
            _onlineRunModel.Advance(deltaTime);
            SyncHostRunProjection();
            UpdatePlayers(deltaTime);
            UpdateEnemies(deltaTime);

            _spawnTimer -= deltaTime;
            if (_spawnTimer <= 0f && _enemies.Count < MaximumEnemies)
            {
                var groupSize = Mathf.Clamp(1 + Mathf.FloorToInt(_elapsed / 28f), 1, 5);
                var difficulty = 1f + _elapsed / OnlineMap.DifficultyRamp;
                for (var i = 0; i < groupSize && _enemies.Count < MaximumEnemies; i++) SpawnEnemy(false, difficulty);
                _spawnTimer = Mathf.Max(OnlineMap.MinimumSpawnInterval,
                    OnlineMap.BaseSpawnInterval - _elapsed * 0.0014f);
            }

            if (_onlineRunModel.TryTriggerBoss(OnlineMap.BossSpawnTime))
            {
                SyncHostRunProjection();
                SpawnEnemy(true, 1f + _elapsed / OnlineMap.DifficultyRamp);
                _status = "THE JOTUNN HAS ENTERED THE EXPEDITION";
            }
            // The target duration becomes overtime; victory still requires
            // killing the host-authoritative Jotunn.
        }

        private void UpdatePlayers(float deltaTime)
        {
            var ids = new List<ulong>(_players.Keys);
            for (var i = 0; i < ids.Count; i++)
            {
                var player = _players[ids[i]];
                player.Invulnerability = Mathf.Max(0f, player.Invulnerability - deltaTime);
                player.UltimateRemaining = Mathf.Max(0f, player.UltimateRemaining - deltaTime);
                if (player.Downed) continue;

                var movement = Vector2.ClampMagnitude(player.Input, 1f);
                var requested = player.Position + movement * player.MoveSpeed * deltaTime;
                player.Position = ConstrainToParty(player.Id, requested);

                if (player.UltimateQueued && player.UltimateRemaining <= 0f)
                {
                    var definition = OnlineCharacter(player.Id);
                    var radius = definition.UltimateRadius * (1f + (player.UltimateDamageMultiplier - 1f) * 0.35f);
                    DamageEnemiesInRadius(player.Position, radius, definition.UltimateDamage * player.UltimateDamageMultiplier);
                    player.UltimateRemaining = BalanceRules.UltimateCooldown(definition.UltimateCooldown, player.UltimateCooldownUpgrades);
                    player.Invulnerability = 1.25f;
                    BroadcastPulse(player.Position, PlayerIndex(player.Id), true);
                    _status = $"{definition.Name.ToUpperInvariant()} — {definition.UltimateName.ToUpperInvariant()}";
                }
                player.UltimateQueued = false;

                player.AttackTimer -= deltaTime;
                if (player.AttackTimer <= 0f)
                {
                    FireAxes(player);
                    player.AttackTimer = player.AxeCooldown;
                }
                player.PulseTimer -= deltaTime;
                if (player.PulseTimer <= 0f)
                {
                    var shieldRadius = player.RavenGuardEvolved ? 3.35f : 2.55f;
                    var hits = DamageEnemiesInRadius(player.Position, shieldRadius, player.ShieldDamage);
                    if (player.RavenGuardEvolved && hits > 0) player.Health = Mathf.Min(player.MaxHealth, player.Health + Mathf.Min(9f, hits * 0.65f));
                    BroadcastPulse(player.Position, PlayerIndex(player.Id), false);
                    player.PulseTimer = 5.2f;
                }
                _playerTargets[player.Id] = player.Position;
            }
            UpdateRevival(deltaTime);
        }

        private void UpdateRevival(float deltaTime)
        {
            var living = 0;
            foreach (var pair in _players)
                if (!pair.Value.Downed && pair.Value.Health > 0f) living++;
            if (living == 0)
            {
                EndRun(false);
                return;
            }

            foreach (var pair in _players)
            {
                var downed = pair.Value;
                if (!downed.Downed) continue;
                var rescuerNearby = false;
                foreach (var otherPair in _players)
                {
                    var other = otherPair.Value;
                    if (other.Id == downed.Id || other.Downed || other.Health <= 0f) continue;
                    if ((other.Position - downed.Position).sqrMagnitude <= 1.8f * 1.8f)
                    {
                        rescuerNearby = true;
                        break;
                    }
                }
                downed.ReviveProgress = Mathf.Clamp01(downed.ReviveProgress +
                    (rescuerNearby ? 1f : -0.35f) * deltaTime / 2.5f);
                if (downed.ReviveProgress >= 1f)
                {
                    downed.Downed = false;
                    downed.Health = downed.MaxHealth * 0.42f;
                    downed.ReviveProgress = 0f;
                    downed.Invulnerability = 1f;
                    _status = $"{PlayerName(downed.Id).ToUpperInvariant()} RETURNS TO THE SAGA";
                }
            }
        }

        private void FireAxes(NetPlayer player)
        {
            var alreadyTargeted = new HashSet<int>();
            for (var axe = 0; axe < player.AxeCount; axe++)
            {
                var target = FindNearestEnemy(player.Position, alreadyTargeted);
                if (target == null) target = FindNearestEnemy(player.Position, null);
                if (target == null) return;
                alreadyTargeted.Add(target.Id);
                var critical = Random.value < player.CriticalChance;
                DamageEnemy(target, player.AxeDamage * (critical ? 2f : 1f));
                BroadcastAttack(player.Position, target.Position, PlayerIndex(player.Id), critical);
                if (player.FrostAxeEvolved) DamageEnemiesInRadius(target.Position, 1.25f, player.AxeDamage * 0.42f);

                if (player.AxePierce > 1)
                {
                    var secondary = FindNearestEnemy(target.Position, alreadyTargeted);
                    if (secondary != null && (secondary.Position - target.Position).sqrMagnitude < 3.5f * 3.5f)
                    {
                        alreadyTargeted.Add(secondary.Id);
                        DamageEnemy(secondary, player.AxeDamage * 0.55f);
                    }
                }
            }
        }

        private NetEnemy FindNearestEnemy(Vector2 origin, HashSet<int> excluded)
        {
            return _onlineEnemyGrid.FindNearest(origin, 32f,
                enemy => enemy != null && (excluded == null || !excluded.Contains(enemy.Id)));
        }

        private void UpdateEnemies(float deltaTime)
        {
            _enemyIdScratch.Clear();
            foreach (var id in _enemies.Keys) _enemyIdScratch.Add(id);
            for (var i = 0; i < _enemyIdScratch.Count; i++)
            {
                if (!_enemies.TryGetValue(_enemyIdScratch[i], out var enemy)) continue;
                enemy.AttackCooldown = Mathf.Max(0f, enemy.AttackCooldown - deltaTime);
                var target = FindNearestLivingPlayer(enemy.Position);
                if (target == null) continue;
                var offset = target.Position - enemy.Position;
                var distance = offset.magnitude;
                if (distance > enemy.Radius + 0.48f)
                {
                    enemy.Position += offset.normalized * enemy.Speed * deltaTime;
                    _onlineEnemyGrid.Update(enemy);
                }
                else if (enemy.AttackCooldown <= 0f)
                {
                    DamagePlayer(target, enemy.Boss ? 28f : 12f);
                    enemy.AttackCooldown = enemy.Boss ? 0.75f : 1.05f;
                }
                _enemyTargets[enemy.Id] = enemy.Position;
            }
        }

        private NetPlayer FindNearestLivingPlayer(Vector2 origin)
        {
            NetPlayer nearest = null;
            var best = float.MaxValue;
            foreach (var pair in _players)
            {
                var player = pair.Value;
                if (player.Downed || player.Health <= 0f) continue;
                var distance = (player.Position - origin).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                nearest = player;
            }
            return nearest;
        }

        private void DamagePlayer(NetPlayer player, float rawDamage)
        {
            if (player.Downed || player.Invulnerability > 0f) return;
            player.Health -= Mathf.Max(1f, rawDamage - player.Armor);
            player.Invulnerability = 0.28f;
            if (player.Health > 0f) return;
            player.Health = 0f;
            player.Downed = true;
            player.ReviveProgress = 0f;
            _status = $"{PlayerName(player.Id).ToUpperInvariant()} IS DOWN — STAY CLOSE TO REVIVE";
        }

        private void DamageEnemy(NetEnemy enemy, float damage)
        {
            if (enemy == null || !_enemies.ContainsKey(enemy.Id)) return;
            enemy.Health -= damage;
            if (enemy.Health > 0f) return;
            _enemies.Remove(enemy.Id);
            _enemyTargets.Remove(enemy.Id);
            _onlineEnemyGrid.Remove(enemy);
            if (_enemyViews.TryGetValue(enemy.Id, out var view)) _onlineEnemyViewPool.Release(view);
            _enemyViews.Remove(enemy.Id);
            _kills++;
            if (Random.value < 0.18f) _renown++;
            AddExperience(enemy.Boss ? 60 : Random.Range(2, 5));
            if (enemy.Boss) EndRun(true);
        }

        private int DamageEnemiesInRadius(Vector2 center, float radius, float damage)
        {
            var hits = 0;
            _onlineEnemyGrid.QueryRadius(center, radius + 0.9f, _onlineSpatialScratch);
            for (var i = 0; i < _onlineSpatialScratch.Count; i++)
            {
                var enemy = _onlineSpatialScratch[i];
                if (enemy == null || !_enemies.ContainsKey(enemy.Id)) continue;
                var range = radius + enemy.Radius;
                if ((enemy.Position - center).sqrMagnitude <= range * range)
                {
                    DamageEnemy(enemy, damage);
                    hits++;
                }
            }
            return hits;
        }

        private void SpawnEnemy(bool boss, float difficulty)
        {
            var center = PartyCenter();
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var distance = Random.Range(8.5f, 11.8f);
            var enemy = new NetEnemy
            {
                Id = _nextEnemyId++,
                Position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance,
                MaxHealth = boss ? 920f * difficulty : 34f * difficulty,
                Speed = boss ? 1.25f : Random.Range(1.45f, 2.15f) + difficulty * 0.05f,
                Radius = boss ? 0.85f : 0.34f,
                Boss = boss
            };
            enemy.Health = enemy.MaxHealth;
            _enemies[enemy.Id] = enemy;
            _enemyTargets[enemy.Id] = enemy.Position;
            _onlineEnemyGrid.Add(enemy);
            EnsureEnemyView(enemy.Id, boss);
        }

        private Vector2 ConstrainToParty(ulong movingId, Vector2 requested)
        {
            if (_players.Count <= 1) return requested;
            foreach (var pair in _players)
            {
                if (pair.Key == movingId) continue;
                var offset = requested - pair.Value.Position;
                const float maximumSeparation = 11.5f;
                return offset.sqrMagnitude <= maximumSeparation * maximumSeparation
                    ? requested
                    : pair.Value.Position + offset.normalized * maximumSeparation;
            }
            return requested;
        }

        private Vector2 PartyCenter()
        {
            if (_players.Count == 0) return Vector2.zero;
            var center = Vector2.zero;
            foreach (var pair in _players) center += pair.Value.Position;
            return center / _players.Count;
        }

        private void AddExperience(int amount)
        {
            if (_onlineRunModel.Phase != RunSimulationPhase.Playing) return;
            var levelUp = _onlineRunModel.AddExperience(amount);
            SyncHostRunProjection();
            if (levelUp) OfferLevelUp();
        }

        private void OfferLevelUp()
        {
            _onlineRewards.Clear();
            var builds = new[] { PlayerByIndex(0).Build, PlayerByIndex(1).Build };
            _onlineRewards.AddRange(RewardFactory.Generate(builds, _rewardTurnPlayerIndex, 2));
            _upgradeSelection = 0;
            _status = $"P{_rewardTurnPlayerIndex + 1} CHOOSES THE NEXT REWARD";
            SendSnapshot();
        }

        private void ResolveUpgrade(int index)
        {
            if (_phase != OnlinePhase.LevelUp || index < 0 || index >= _onlineRewards.Count) return;
            var option = _onlineRewards[index];
            if (!_onlineRunModel.CompleteReward()) return;
            if (option.Shared)
            {
                ApplyOnlineReward(PlayerByIndex(0), option.Item);
                ApplyOnlineReward(PlayerByIndex(1), option.Item);
            }
            else ApplyOnlineReward(PlayerByIndex(option.TargetPlayerIndex), option.Item);
            var chooser = OnlineCharacter(PlayerByIndex(_rewardTurnPlayerIndex).Id).Name;
            var destination = option.Shared ? "THE TEAM" : OnlineCharacter(PlayerByIndex(option.TargetPlayerIndex).Id).Name.ToUpperInvariant();
            _status = $"{chooser.ToUpperInvariant()} CHOSE {option.Item.Name.ToUpperInvariant()} FOR {destination}";
            _lastRewardItemIndex = ItemCatalog.IndexOf(option.Item.Id);
            _lastRewardTargetPlayerIndex = option.TargetPlayerIndex;
            _lastRewardChooserPlayerIndex = _rewardTurnPlayerIndex;
            _lastRewardShared = option.Shared;
            SyncHostRunProjection();
            SendSnapshot();
        }

        private void ApplyOnlineReward(NetPlayer player, ItemDefinition item)
        {
            if (player == null || item == null) return;
            var result = player.Build.Acquire(item);
            if (result == null) return;
            if (result.Evolution)
            {
                if (item == ItemCatalog.JotunnCleaver)
                {
                    player.FrostAxeEvolved = true;
                    player.AxeDamage *= 1.55f;
                    player.AxePierce += 2;
                }
                else if (item == ItemCatalog.StormAegis)
                {
                    player.RavenGuardEvolved = true;
                    player.ShieldDamage *= 1.35f;
                }
                return;
            }
            var effect = item.EffectAtLevel(result.NewLevel);
            switch (effect)
            {
                case UpgradeId.AxeDamage: player.AxeDamage *= 1.26f; break;
                case UpgradeId.AxeSpeed: player.AxeCooldown = Mathf.Max(0.24f, player.AxeCooldown * 0.86f); break;
                case UpgradeId.ExtraAxe: player.AxeCount = Mathf.Min(5, player.AxeCount + 1); break;
                case UpgradeId.AxePierce: player.AxePierce++; break;
                case UpgradeId.ShieldDamage: player.ShieldDamage *= 1.42f; break;
                case UpgradeId.CriticalRunes: player.CriticalChance = Mathf.Min(0.55f, player.CriticalChance + 0.09f); break;
                case UpgradeId.MoveSpeed: player.MoveSpeed += 0.46f; break;
                case UpgradeId.MaxHealth: player.MaxHealth += 24f; player.Health += 24f; break;
                case UpgradeId.Armor: player.Armor += 1f; break;
                case UpgradeId.UltimateDamage: player.UltimateDamageMultiplier *= 1.3f; break;
                case UpgradeId.UltimateCooldown:
                    var definition = OnlineCharacter(player.Id);
                    var previous = BalanceRules.UltimateCooldown(definition.UltimateCooldown, player.UltimateCooldownUpgrades);
                    player.UltimateCooldownUpgrades++;
                    var current = BalanceRules.UltimateCooldown(definition.UltimateCooldown, player.UltimateCooldownUpgrades);
                    player.UltimateRemaining = Mathf.Min(current, player.UltimateRemaining * current / previous);
                    break;
                case UpgradeId.Heal: player.Health = Mathf.Min(player.MaxHealth, player.Health + 24f); break;
            }
        }

        private void EndRun(bool victory)
        {
            if (!_onlineRunModel.Complete(victory)) return;
            SyncHostRunProjection();
            _resultSelection = _networkManager != null && _networkManager.IsHost ? 0 : 1;
            _status = victory ? "A SAGA IS BORN" : "THE ICE CLAIMS THE EXPEDITION";
            if (!_runRecorded && _networkManager != null && _networkManager.IsHost)
            {
                SaveService.RecordRun(_kills, _renown, _elapsed, victory);
                _runRecorded = true;
            }
            SendSnapshot();
        }

        private void BeginExpedition()
        {
            ClearGameplayVisuals();
            _players.Clear();
            _enemies.Clear();
            _onlineEnemyGrid.Clear();
            _playerTargets.Clear();
            _enemyTargets.Clear();
            var ids = _networkManager.ConnectedClientsIds;
            for (var i = 0; i < ids.Count; i++)
            {
                var player = CreateNetPlayer(ids[i]);
                player.Position = new Vector2(i == 0 ? -1.1f : 1.1f, 0f);
                _players[player.Id] = player;
                _playerTargets[player.Id] = player.Position;
                EnsurePlayerView(player.Id);
            }
            _onlineRunModel.Begin(2, BalanceRules.ExperienceToNext);
            SyncHostRunProjection();
            _kills = 0;
            _renown = 0;
            _runRecorded = false;
            _nextEnemyId = 1;
            _lastRewardItemIndex = -1;
            _lastRewardTargetPlayerIndex = 0;
            _lastRewardChooserPlayerIndex = 0;
            _lastRewardShared = false;
            _showBuildDetails = false;
            _spawnTimer = 0.2f;
            CreateArenaVisuals();
            _status = "RAVENBOUND DUO — THE EXPEDITION BEGINS";
            SendSnapshot();
        }

        public void StartHost()
        {
            if (SessionActive || !CreateNetworkManager()) return;
            _transport.SetConnectionData("127.0.0.1", Port, "0.0.0.0");
            if (!_networkManager.StartHost())
            {
                _status = "Host failed — check Console and UDP port 7777";
                return;
            }
            RegisterMessages();
            _networkManager.OnClientConnectedCallback += OnClientConnected;
            _networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            _players[NetworkManager.ServerClientId] = CreateNetPlayer(NetworkManager.ServerClientId);
            _status = "HOST READY — HALDOR IS WAITING FOR PLAYER 2";
        }

        public void StartClient()
        {
            if (SessionActive || !CreateNetworkManager()) return;
            var address = string.IsNullOrWhiteSpace(_address) ? "127.0.0.1" : _address.Trim();
            _transport.SetConnectionData(address, Port);
            if (!_networkManager.StartClient())
            {
                _status = "Client failed — check the host address";
                return;
            }
            RegisterMessages();
            _networkManager.OnClientConnectedCallback += OnClientConnected;
            _networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            _status = $"CONNECTING TO {address}:{Port}...";
        }

        public void ShutdownSession()
        {
            var managerObject = _networkObject;
            if (_networkManager != null)
            {
                if (_networkManager.CustomMessagingManager != null)
                {
                    _networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(InputMessage);
                    _networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(SnapshotMessage);
                    _networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(UpgradeMessage);
                    _networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(AttackMessage);
                    _networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(PulseMessage);
                }
                _networkManager.OnClientConnectedCallback -= OnClientConnected;
                _networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
                if (_networkManager.IsListening) _networkManager.Shutdown();
            }
            if (managerObject != null) Destroy(managerObject, 0.5f);
            _sessionRestartAllowedAt = Time.unscaledTime + 0.65f;
            _networkManager = null;
            _transport = null;
            _networkObject = null;
            ClearAllVisuals();
            _onlineRunModel.Reset();
            _phase = OnlinePhase.Lobby;
            _status = "Session closed";
            _inputReadyAt = Time.unscaledTime + 0.25f;
        }

        private bool CreateNetworkManager()
        {
            if (Time.unscaledTime < _sessionRestartAllowedAt || NetworkManager.Singleton != null)
            {
                _status = "Previous session is still closing — wait one second";
                return false;
            }
            _networkObject = new GameObject("Online Expedition — NetworkManager");
            _transport = _networkObject.AddComponent<UnityTransport>();
            _networkManager = _networkObject.AddComponent<NetworkManager>();
            if (_networkManager.NetworkConfig == null) _networkManager.NetworkConfig = new NetworkConfig();
            _networkManager.NetworkConfig.NetworkTransport = _transport;
            _networkManager.NetworkConfig.EnableSceneManagement = false;
            _networkManager.NetworkConfig.ConnectionApproval = true;
            _networkManager.NetworkConfig.TickRate = 30;
            _networkManager.ConnectionApprovalCallback = ApproveConnection;
            DontDestroyOnLoad(_networkObject);
            return true;
        }

        private void ApproveConnection(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            var hasRoom = _networkManager != null && _networkManager.ConnectedClientsIds.Count < 2;
            response.Approved = hasRoom;
            response.CreatePlayerObject = false;
            response.Pending = false;
            response.Reason = hasRoom ? string.Empty : "The expedition already has two survivors.";
        }

        private void RegisterMessages()
        {
            _networkManager.CustomMessagingManager.RegisterNamedMessageHandler(InputMessage, ReceiveInput);
            _networkManager.CustomMessagingManager.RegisterNamedMessageHandler(SnapshotMessage, ReceiveSnapshot);
            _networkManager.CustomMessagingManager.RegisterNamedMessageHandler(UpgradeMessage, ReceiveUpgradeChoice);
            _networkManager.CustomMessagingManager.RegisterNamedMessageHandler(AttackMessage, ReceiveAttack);
            _networkManager.CustomMessagingManager.RegisterNamedMessageHandler(PulseMessage, ReceivePulse);
        }

        private void OnClientConnected(ulong clientId)
        {
            if (_networkManager.IsServer)
            {
                if (!_players.ContainsKey(clientId)) _players[clientId] = CreateNetPlayer(clientId);
                _status = $"HOST ACTIVE — {_networkManager.ConnectedClientsIds.Count}/2 PLAYERS";
                if (_networkManager.ConnectedClientsIds.Count == 2) BeginExpedition();
            }
            else if (clientId == _networkManager.LocalClientId)
            {
                _status = "CONNECTED — WAITING FOR THE HOST SNAPSHOT";
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            _players.Remove(clientId);
            _playerTargets.Remove(clientId);
            if (_playerViews.TryGetValue(clientId, out var view)) Destroy(view);
            _playerViews.Remove(clientId);
            if (_networkManager != null && _networkManager.IsServer)
            {
                _onlineRunModel.Reset();
                _phase = OnlinePhase.Lobby;
                _enemies.Clear();
                _onlineEnemyGrid.Clear();
                _enemyTargets.Clear();
                ClearGameplayVisuals();
                _status = "PLAYER 2 DISCONNECTED — EXPEDITION RESET";
            }
            else
            {
                _status = "Disconnected from host";
            }
        }

        private void ReceiveInput(ulong senderId, FastBufferReader reader)
        {
            if (_networkManager == null || !_networkManager.IsServer || !_players.TryGetValue(senderId, out var player)) return;
            reader.ReadValueSafe(out float x);
            reader.ReadValueSafe(out float y);
            reader.ReadValueSafe(out bool ultimate);
            player.Input = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
            if (ultimate) player.UltimateQueued = true;
        }

        private void SendUpgradeChoice(int index)
        {
            if (_networkManager == null || !_networkManager.IsConnectedClient) return;
            using (var writer = new FastBufferWriter(4, Allocator.Temp))
            {
                writer.WriteValueSafe((byte)index);
                _networkManager.CustomMessagingManager.SendNamedMessage(
                    UpgradeMessage, NetworkManager.ServerClientId, writer, NetworkDelivery.ReliableSequenced);
            }
        }

        private void ReceiveUpgradeChoice(ulong senderId, FastBufferReader reader)
        {
            if (_networkManager == null || !_networkManager.IsServer || _phase != OnlinePhase.LevelUp) return;
            reader.ReadValueSafe(out byte index);
            if (_rewardTurnPlayerIndex != 1 || PlayerIndex(senderId) != 1) return;
            ResolveUpgrade(index);
        }

        private void SendSnapshot()
        {
            if (_networkManager == null || !_networkManager.IsServer) return;
            SyncHostRunProjection();
            var capacity = 2048 + _players.Count * 160 + _enemies.Count * 16;
            using (var writer = new FastBufferWriter(capacity, Allocator.Temp))
            {
                writer.WriteValueSafe((byte)_phase);
                writer.WriteValueSafe((byte)_mapIndex);
                writer.WriteValueSafe(_elapsed);
                writer.WriteValueSafe(_level);
                writer.WriteValueSafe(_experience);
                writer.WriteValueSafe(_experienceToNext);
                writer.WriteValueSafe(_kills);
                writer.WriteValueSafe(_renown);
                writer.WriteValueSafe(_bossSpawned);
                writer.WriteValueSafe((byte)_rewardTurnPlayerIndex);
                writer.WriteValueSafe((byte)_onlineRewards.Count);
                for (var i = 0; i < _onlineRewards.Count; i++)
                {
                    writer.WriteValueSafe((byte)ItemCatalog.IndexOf(_onlineRewards[i].Item.Id));
                    writer.WriteValueSafe((byte)_onlineRewards[i].TargetPlayerIndex);
                    writer.WriteValueSafe(_onlineRewards[i].Shared);
                }
                writer.WriteValueSafe((short)_lastRewardItemIndex);
                writer.WriteValueSafe((byte)_lastRewardTargetPlayerIndex);
                writer.WriteValueSafe((byte)_lastRewardChooserPlayerIndex);
                writer.WriteValueSafe(_lastRewardShared);

                writer.WriteValueSafe((byte)_players.Count);
                foreach (var pair in _players)
                {
                    var player = pair.Value;
                    writer.WriteValueSafe(player.Id);
                    writer.WriteValueSafe(player.Position.x);
                    writer.WriteValueSafe(player.Position.y);
                    writer.WriteValueSafe(player.Health);
                    writer.WriteValueSafe(player.MaxHealth);
                    writer.WriteValueSafe(player.Downed);
                    writer.WriteValueSafe(player.ReviveProgress);
                    writer.WriteValueSafe(player.UltimateRemaining);
                    writer.WriteValueSafe(player.MoveSpeed);
                    writer.WriteValueSafe(player.Armor);
                    writer.WriteValueSafe(player.AxeDamage);
                    writer.WriteValueSafe(player.AxeCooldown);
                    writer.WriteValueSafe((byte)player.AxeCount);
                    writer.WriteValueSafe((byte)player.AxePierce);
                    writer.WriteValueSafe(player.CriticalChance);
                    writer.WriteValueSafe(player.ShieldDamage);
                    writer.WriteValueSafe((byte)player.UltimateCooldownUpgrades);
                    writer.WriteValueSafe(player.UltimateDamageMultiplier);
                    writer.WriteValueSafe(player.FrostAxeEvolved);
                    writer.WriteValueSafe(player.RavenGuardEvolved);
                    writer.WriteValueSafe((byte)player.Build.Items.Count);
                    for (var itemIndex = 0; itemIndex < player.Build.Items.Count; itemIndex++)
                    {
                        var state = player.Build.Items[itemIndex];
                        writer.WriteValueSafe((byte)ItemCatalog.IndexOf(state.ItemId));
                        writer.WriteValueSafe((byte)state.Level);
                        writer.WriteValueSafe((short)ItemCatalog.IndexOf(state.EvolutionId));
                    }
                }

                writer.WriteValueSafe((ushort)_enemies.Count);
                foreach (var pair in _enemies)
                {
                    var enemy = pair.Value;
                    var x = (short)Mathf.Clamp(Mathf.RoundToInt(enemy.Position.x * 100f), short.MinValue, short.MaxValue);
                    var y = (short)Mathf.Clamp(Mathf.RoundToInt(enemy.Position.y * 100f), short.MinValue, short.MaxValue);
                    var health = (ushort)Mathf.Clamp(Mathf.RoundToInt(enemy.Health / Mathf.Max(1f, enemy.MaxHealth) * 65535f), 0, 65535);
                    writer.WriteValueSafe((ushort)enemy.Id);
                    writer.WriteValueSafe(x);
                    writer.WriteValueSafe(y);
                    writer.WriteValueSafe(health);
                    writer.WriteValueSafe(enemy.Boss);
                }
                _lastSnapshotBytes = writer.Length;
                for (var i = 0; i < _networkManager.ConnectedClientsIds.Count; i++)
                {
                    var id = _networkManager.ConnectedClientsIds[i];
                    if (id == NetworkManager.ServerClientId) continue;
                    _networkManager.CustomMessagingManager.SendNamedMessage(
                        SnapshotMessage, id, writer, NetworkDelivery.UnreliableSequenced);
                }
            }
            _snapshotsSent++;
        }

        private void ReceiveSnapshot(ulong senderId, FastBufferReader reader)
        {
            if (_networkManager == null || _networkManager.IsServer) return;
            var previousPhase = _phase;
            reader.ReadValueSafe(out byte phase);
            _phase = (OnlinePhase)phase;
            reader.ReadValueSafe(out byte mapIndex);
            _mapIndex = Mathf.Clamp(mapIndex, 0, ContentCatalog.Maps.Length - 1);
            if (_phase == OnlinePhase.Playing &&
                (previousPhase == OnlinePhase.Lobby || previousPhase == OnlinePhase.Victory || previousPhase == OnlinePhase.Defeat))
            {
                ClearGameplayVisuals();
                _players.Clear();
                _enemies.Clear();
                _playerTargets.Clear();
                _enemyTargets.Clear();
                _onlineEnemyGrid.Clear();
            }
            reader.ReadValueSafe(out _elapsed);
            reader.ReadValueSafe(out _level);
            reader.ReadValueSafe(out _experience);
            reader.ReadValueSafe(out _experienceToNext);
            reader.ReadValueSafe(out _kills);
            reader.ReadValueSafe(out _renown);
            reader.ReadValueSafe(out _bossSpawned);
            reader.ReadValueSafe(out byte rewardOwner);
            _rewardTurnPlayerIndex = rewardOwner;
            _onlineRewards.Clear();
            reader.ReadValueSafe(out byte rewardCount);
            for (var i = 0; i < rewardCount; i++)
            {
                reader.ReadValueSafe(out byte itemIndex);
                reader.ReadValueSafe(out byte targetIndex);
                reader.ReadValueSafe(out bool shared);
                _onlineRewards.Add(new RewardOption
                {
                    Item = ItemCatalog.At(itemIndex),
                    TargetPlayerIndex = targetIndex,
                    Shared = shared
                });
            }
            reader.ReadValueSafe(out short lastRewardItemIndex);
            reader.ReadValueSafe(out byte lastRewardTarget);
            reader.ReadValueSafe(out byte lastRewardChooser);
            reader.ReadValueSafe(out bool lastRewardShared);
            _lastRewardItemIndex = lastRewardItemIndex;
            _lastRewardTargetPlayerIndex = lastRewardTarget;
            _lastRewardChooserPlayerIndex = lastRewardChooser;
            _lastRewardShared = lastRewardShared;

            var seenPlayers = new HashSet<ulong>();
            reader.ReadValueSafe(out byte playerCount);
            for (var i = 0; i < playerCount; i++)
            {
                reader.ReadValueSafe(out ulong id);
                reader.ReadValueSafe(out float x);
                reader.ReadValueSafe(out float y);
                reader.ReadValueSafe(out float health);
                reader.ReadValueSafe(out float maxHealth);
                reader.ReadValueSafe(out bool downed);
                reader.ReadValueSafe(out float revive);
                reader.ReadValueSafe(out float ultimateRemaining);
                reader.ReadValueSafe(out float moveSpeed);
                reader.ReadValueSafe(out float armor);
                reader.ReadValueSafe(out float axeDamage);
                reader.ReadValueSafe(out float axeCooldown);
                reader.ReadValueSafe(out byte axeCount);
                reader.ReadValueSafe(out byte axePierce);
                reader.ReadValueSafe(out float criticalChance);
                reader.ReadValueSafe(out float shieldDamage);
                reader.ReadValueSafe(out byte ultimateCooldownUpgrades);
                reader.ReadValueSafe(out float ultimateDamageMultiplier);
                reader.ReadValueSafe(out bool frostAxeEvolved);
                reader.ReadValueSafe(out bool ravenGuardEvolved);
                if (!_players.TryGetValue(id, out var player))
                {
                    player = CreateNetPlayer(id);
                    _players[id] = player;
                }
                player.Position = new Vector2(x, y);
                player.Health = health;
                player.MaxHealth = maxHealth;
                player.Downed = downed;
                player.ReviveProgress = revive;
                player.UltimateRemaining = ultimateRemaining;
                player.MoveSpeed = moveSpeed;
                player.Armor = armor;
                player.AxeDamage = axeDamage;
                player.AxeCooldown = axeCooldown;
                player.AxeCount = axeCount;
                player.AxePierce = axePierce;
                player.CriticalChance = criticalChance;
                player.ShieldDamage = shieldDamage;
                player.UltimateCooldownUpgrades = ultimateCooldownUpgrades;
                player.UltimateDamageMultiplier = ultimateDamageMultiplier;
                player.FrostAxeEvolved = frostAxeEvolved;
                player.RavenGuardEvolved = ravenGuardEvolved;
                var buildStates = new List<ItemState>();
                reader.ReadValueSafe(out byte buildItemCount);
                for (var itemIndex = 0; itemIndex < buildItemCount; itemIndex++)
                {
                    reader.ReadValueSafe(out byte definitionIndex);
                    reader.ReadValueSafe(out byte itemLevel);
                    reader.ReadValueSafe(out short evolutionIndex);
                    var definition = ItemCatalog.At(definitionIndex);
                    var evolution = ItemCatalog.At(evolutionIndex);
                    if (definition != null)
                    {
                        buildStates.Add(new ItemState
                        {
                            ItemId = definition.Id,
                            Level = itemLevel,
                            EvolutionId = evolution != null ? evolution.Id : null
                        });
                    }
                }
                player.Build.LoadSnapshot(OnlineMap.WeaponSlots, OnlineMap.GearSlots, buildStates);
                _playerTargets[id] = player.Position;
                seenPlayers.Add(id);
                EnsurePlayerView(id);
            }
            RemoveMissingPlayers(seenPlayers);

            var seenEnemies = new HashSet<int>();
            reader.ReadValueSafe(out ushort enemyCount);
            for (var i = 0; i < enemyCount; i++)
            {
                reader.ReadValueSafe(out ushort compactId);
                reader.ReadValueSafe(out short compactX);
                reader.ReadValueSafe(out short compactY);
                reader.ReadValueSafe(out ushort compactHealth);
                reader.ReadValueSafe(out bool boss);
                var id = (int)compactId;
                var x = compactX / 100f;
                var y = compactY / 100f;
                var added = false;
                if (!_enemies.TryGetValue(id, out var enemy))
                {
                    enemy = new NetEnemy { Id = id, Boss = boss };
                    _enemies[id] = enemy;
                    added = true;
                }
                enemy.Position = new Vector2(x, y);
                enemy.MaxHealth = boss ? 1000f : 100f;
                enemy.Health = compactHealth / 65535f * enemy.MaxHealth;
                enemy.Boss = boss;
                _enemyTargets[id] = enemy.Position;
                if (added) _onlineEnemyGrid.Add(enemy);
                else _onlineEnemyGrid.Update(enemy);
                seenEnemies.Add(id);
                EnsureEnemyView(id, boss);
            }
            RemoveMissingEnemies(seenEnemies);
            if (_phase != OnlinePhase.Lobby) CreateArenaVisuals();
            if (previousPhase != _phase)
            {
                if (_phase == OnlinePhase.Playing)
                    _status = previousPhase == OnlinePhase.LevelUp ? LastRewardAnnouncement() : "ONLINE EXPEDITION SYNCHRONIZED";
                else if (_phase == OnlinePhase.LevelUp) _status = $"P{_rewardTurnPlayerIndex + 1} CHOOSES THE NEXT REWARD";
                else if (_phase == OnlinePhase.Victory) _status = "A SAGA IS BORN";
                else if (_phase == OnlinePhase.Defeat) _status = "THE ICE CLAIMS THE EXPEDITION";
            }
            _lastSnapshotBytes = reader.Length;
            _snapshotsReceived++;
        }

        private void RemoveMissingPlayers(HashSet<ulong> seen)
        {
            var ids = new List<ulong>(_players.Keys);
            for (var i = 0; i < ids.Count; i++)
            {
                if (seen.Contains(ids[i])) continue;
                _players.Remove(ids[i]);
                _playerTargets.Remove(ids[i]);
                if (_playerViews.TryGetValue(ids[i], out var view)) Destroy(view);
                _playerViews.Remove(ids[i]);
            }
        }

        private void RemoveMissingEnemies(HashSet<int> seen)
        {
            _enemyIdScratch.Clear();
            foreach (var id in _enemies.Keys) _enemyIdScratch.Add(id);
            for (var i = 0; i < _enemyIdScratch.Count; i++)
            {
                var id = _enemyIdScratch[i];
                if (seen.Contains(id)) continue;
                if (_enemies.TryGetValue(id, out var enemy)) _onlineEnemyGrid.Remove(enemy);
                _enemies.Remove(id);
                _enemyTargets.Remove(id);
                if (_enemyViews.TryGetValue(id, out var view)) _onlineEnemyViewPool.Release(view);
                _enemyViews.Remove(id);
            }
        }

        private void BroadcastAttack(Vector2 origin, Vector2 target, int playerIndex, bool critical)
        {
            CreateAttackVisual(origin, target, playerIndex, critical);
            if (_networkManager == null || !_networkManager.IsServer) return;
            using (var writer = new FastBufferWriter(32, Allocator.Temp))
            {
                writer.WriteValueSafe(origin.x);
                writer.WriteValueSafe(origin.y);
                writer.WriteValueSafe(target.x);
                writer.WriteValueSafe(target.y);
                writer.WriteValueSafe((byte)playerIndex);
                writer.WriteValueSafe(critical);
                for (var i = 0; i < _networkManager.ConnectedClientsIds.Count; i++)
                {
                    var id = _networkManager.ConnectedClientsIds[i];
                    if (id == NetworkManager.ServerClientId) continue;
                    _networkManager.CustomMessagingManager.SendNamedMessage(
                        AttackMessage, id, writer, NetworkDelivery.UnreliableSequenced);
                }
            }
        }

        private void ReceiveAttack(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out float originX);
            reader.ReadValueSafe(out float originY);
            reader.ReadValueSafe(out float targetX);
            reader.ReadValueSafe(out float targetY);
            reader.ReadValueSafe(out byte playerIndex);
            reader.ReadValueSafe(out bool critical);
            CreateAttackVisual(new Vector2(originX, originY), new Vector2(targetX, targetY), playerIndex, critical);
        }

        private void CreateAttackVisual(Vector2 origin, Vector2 target, int playerIndex, bool critical)
        {
            EnsureVisualRoot();
            _onlineAttackPool.Get(origin).Initialize(this, target, playerIndex, critical);
        }

        private void BroadcastPulse(Vector2 position, int playerIndex, bool rush)
        {
            EnsureVisualRoot();
            _onlinePulsePool.Get(position).Initialize(this, playerIndex, rush);
            if (_networkManager == null || !_networkManager.IsServer) return;
            using (var writer = new FastBufferWriter(20, Allocator.Temp))
            {
                writer.WriteValueSafe(position.x);
                writer.WriteValueSafe(position.y);
                writer.WriteValueSafe((byte)playerIndex);
                writer.WriteValueSafe(rush);
                for (var i = 0; i < _networkManager.ConnectedClientsIds.Count; i++)
                {
                    var id = _networkManager.ConnectedClientsIds[i];
                    if (id == NetworkManager.ServerClientId) continue;
                    _networkManager.CustomMessagingManager.SendNamedMessage(
                        PulseMessage, id, writer, NetworkDelivery.UnreliableSequenced);
                }
            }
        }

        private void ReceivePulse(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out float x);
            reader.ReadValueSafe(out float y);
            reader.ReadValueSafe(out byte playerIndex);
            reader.ReadValueSafe(out bool rush);
            EnsureVisualRoot();
            _onlinePulsePool.Get(new Vector2(x, y)).Initialize(this, playerIndex, rush);
        }

        private void UpdateVisuals()
        {
            var delta = Time.unscaledDeltaTime;
            foreach (var pair in _playerTargets)
            {
                EnsurePlayerView(pair.Key);
                var view = _playerViews[pair.Key];
                view.transform.position = Vector3.Lerp(view.transform.position, pair.Value, 1f - Mathf.Exp(-18f * delta));
                if (_players.TryGetValue(pair.Key, out var player))
                {
                    var renderer = view.GetComponent<SpriteRenderer>();
                    var baseColor = PlayerIndex(pair.Key) == 0 ? new Color(0.29f, 0.65f, 0.82f) : new Color(0.88f, 0.52f, 0.24f);
                    renderer.color = player.Downed ? new Color(0.16f, 0.18f, 0.2f) : baseColor;
                }
            }
            foreach (var pair in _enemyTargets)
            {
                if (!_enemies.TryGetValue(pair.Key, out var enemy)) continue;
                EnsureEnemyView(pair.Key, enemy.Boss);
                _enemyViews[pair.Key].transform.position = Vector3.Lerp(
                    _enemyViews[pair.Key].transform.position, pair.Value, 1f - Mathf.Exp(-14f * delta));
            }
            UpdateCamera(delta);
        }

        private void UpdateCamera(float delta)
        {
            if (_camera == null || _playerTargets.Count == 0) return;
            var center = Vector2.zero;
            foreach (var pair in _playerTargets) center += pair.Value;
            center /= _playerTargets.Count;
            var maxDistance = 0f;
            foreach (var pair in _playerTargets) maxDistance = Mathf.Max(maxDistance, Vector2.Distance(center, pair.Value));
            _camera.transform.position = Vector3.Lerp(_camera.transform.position,
                new Vector3(center.x, center.y, -10f), 1f - Mathf.Exp(-10f * delta));
            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize,
                Mathf.Clamp(6f + maxDistance * 0.62f, 6f, 9.5f), 1f - Mathf.Exp(-5f * delta));
        }

        private void EnsureVisualRoot()
        {
            if (_visualRoot != null) return;
            _visualRoot = new GameObject("Online Expedition Visuals").transform;
            _visualRoot.SetParent(transform, false);
            _onlineEnemyViewPool = new ComponentPool<OnlineEnemyView>(() => CreateOnlinePooled<OnlineEnemyView>("Pooled Online Enemy"), _visualRoot, MaximumEnemies);
            _onlineAttackPool = new ComponentPool<OnlineAttackVisual>(() => CreateOnlinePooled<OnlineAttackVisual>("Pooled Network Axe"), _visualRoot, 64);
            _onlinePulsePool = new ComponentPool<OnlinePulseVisual>(() => CreateOnlinePooled<OnlinePulseVisual>("Pooled Network Pulse"), _visualRoot, 16);
        }

        private void CreateArenaVisuals()
        {
            if (_arenaRoot != null) return;
            EnsureVisualRoot();
            _arenaRoot = new GameObject("Online Frozen Shore").transform;
            _arenaRoot.SetParent(_visualRoot, false);
            var ground = new GameObject("Frozen Ground");
            ground.transform.SetParent(_arenaRoot, false);
            ground.transform.localScale = new Vector3(80f, 80f, 1f);
            var groundRenderer = ground.AddComponent<SpriteRenderer>();
            groundRenderer.sprite = RuntimeAssets.Circle;
            groundRenderer.color = OnlineMap.GroundColor;
            groundRenderer.sortingOrder = -20;

            var previousState = Random.state;
            Random.InitState(1908);
            for (var i = 0; i < 90; i++)
            {
                var marker = new GameObject(i % 5 == 0 ? "Rune Stone" : "Ice Shard");
                marker.transform.SetParent(_arenaRoot, false);
                marker.transform.position = new Vector3(Random.Range(-38f, 38f), Random.Range(-38f, 38f), 0f);
                marker.transform.localScale = Vector3.one * Random.Range(0.08f, 0.28f);
                var renderer = marker.AddComponent<SpriteRenderer>();
                renderer.sprite = i % 5 == 0 ? RuntimeAssets.Diamond : RuntimeAssets.Circle;
                renderer.color = i % 5 == 0 ? new Color(0.22f, 0.45f, 0.49f) : new Color(0.17f, 0.25f, 0.28f);
                renderer.sortingOrder = -10;
            }
            Random.state = previousState;
        }

        private void EnsurePlayerView(ulong id)
        {
            if (_playerViews.ContainsKey(id)) return;
            EnsureVisualRoot();
            var index = PlayerIndex(id);
            var player = new GameObject(index == 0 ? "Haldor Stormborn — Online" : "Eira Raven-Sworn — Online");
            player.transform.SetParent(_visualRoot, false);
            player.transform.localScale = Vector3.one * 0.78f;
            var renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeAssets.Circle;
            renderer.color = index == 0 ? new Color(0.29f, 0.65f, 0.82f) : new Color(0.88f, 0.52f, 0.24f);
            renderer.sortingOrder = 10;

            var shield = new GameObject("Raven Shield");
            shield.transform.SetParent(player.transform, false);
            shield.transform.localPosition = new Vector3(-0.48f, 0.05f, 0f);
            shield.transform.localScale = new Vector3(0.56f, 0.72f, 1f);
            var shieldRenderer = shield.AddComponent<SpriteRenderer>();
            shieldRenderer.sprite = RuntimeAssets.Circle;
            shieldRenderer.color = new Color(0.13f, 0.22f, 0.31f);
            shieldRenderer.sortingOrder = 11;

            var rune = new GameObject("Frost Rune");
            rune.transform.SetParent(player.transform, false);
            rune.transform.localPosition = new Vector3(0.08f, 0.08f, 0f);
            rune.transform.localScale = Vector3.one * 0.22f;
            var runeRenderer = rune.AddComponent<SpriteRenderer>();
            runeRenderer.sprite = RuntimeAssets.Diamond;
            runeRenderer.color = index == 0 ? new Color(0.36f, 0.95f, 1f) : new Color(1f, 0.72f, 0.28f);
            runeRenderer.sortingOrder = 12;
            _playerViews[id] = player;
        }

        private void EnsureEnemyView(int id, bool boss)
        {
            if (_enemyViews.ContainsKey(id)) return;
            EnsureVisualRoot();
            var enemy = _onlineEnemyViewPool.Get(Vector3.zero);
            enemy.Configure(id, boss);
            _enemyViews[id] = enemy;
        }

        private void ClearGameplayVisuals()
        {
            foreach (var pair in _playerViews) if (pair.Value != null) Destroy(pair.Value);
            foreach (var pair in _enemyViews) if (pair.Value != null) _onlineEnemyViewPool.Release(pair.Value);
            _playerViews.Clear();
            _enemyViews.Clear();
            _onlineAttackPool?.ReleaseAll();
            _onlinePulsePool?.ReleaseAll();
            _onlineEnemyGrid.Clear();
            if (_arenaRoot != null) Destroy(_arenaRoot.gameObject);
            _arenaRoot = null;
        }

        private void ClearAllVisuals()
        {
            _onlineEnemyViewPool?.ReleaseAll();
            _onlineAttackPool?.ReleaseAll();
            _onlinePulsePool?.ReleaseAll();
            if (_visualRoot != null) Destroy(_visualRoot.gameObject);
            _visualRoot = null;
            _onlineEnemyViewPool = null;
            _onlineAttackPool = null;
            _onlinePulsePool = null;
            _arenaRoot = null;
            _playerViews.Clear();
            _enemyViews.Clear();
            _players.Clear();
            _enemies.Clear();
            _onlineEnemyGrid.Clear();
            _playerTargets.Clear();
            _enemyTargets.Clear();
            if (_camera != null)
            {
                _camera.transform.position = new Vector3(0f, 0f, -10f);
                _camera.orthographicSize = 6f;
            }
        }

        private T CreateOnlinePooled<T>(string objectName) where T : Component
        {
            var pooledObject = new GameObject(objectName);
            pooledObject.transform.SetParent(_visualRoot, false);
            return pooledObject.AddComponent<T>();
        }

        public void ReleaseOnlineAttack(OnlineAttackVisual visual) => _onlineAttackPool?.Release(visual);

        public void ReleaseOnlinePulse(OnlinePulseVisual visual) => _onlinePulsePool?.Release(visual);

        private NetPlayer CreateNetPlayer(ulong id)
        {
            var definition = OnlineCharacter(id);
            var player = new NetPlayer
            {
                Id = id,
                MaxHealth = definition.MaxHealth,
                Health = definition.MaxHealth,
                MoveSpeed = definition.MoveSpeed,
                Armor = definition.Armor,
                UltimateRemaining = definition.UltimateCooldown * 0.3f
            };
            player.Build.Initialize(OnlineMap.WeaponSlots, OnlineMap.GearSlots);
            if (PlayerIndex(id) == 0)
                player.AxeDamage *= 1f + Mathf.Min(0.15f, SaveService.Data.HaldorMastery * 0.005f);
            return player;
        }

        private NetPlayer PlayerByIndex(int playerIndex)
        {
            foreach (var pair in _players)
                if (PlayerIndex(pair.Key) == playerIndex) return pair.Value;
            return null;
        }

        private int PlayerIndex(ulong id) => id == NetworkManager.ServerClientId ? 0 : 1;
        private CharacterDefinition OnlineCharacter(ulong id) => ContentCatalog.Character(PlayerIndex(id));
        private string PlayerName(ulong id) => OnlineCharacter(id).Name;

        private static string UpgradeName(UpgradeId id)
        {
            switch (id)
            {
                case UpgradeId.AxeDamage: return "Jotunn Splitter";
                case UpgradeId.AxeSpeed: return "Storm Tempo";
                case UpgradeId.ExtraAxe: return "Twin Tempest";
                case UpgradeId.AxePierce: return "Ironbreaker";
                case UpgradeId.MoveSpeed: return "Longship Legs";
                case UpgradeId.MaxHealth: return "Bear-Blooded";
                case UpgradeId.Armor: return "Raven Guard";
                case UpgradeId.ShieldDamage: return "Thunder Rim";
                case UpgradeId.CriticalRunes: return "Saga Carver";
                case UpgradeId.UltimateCooldown: return "Raven Hourglass";
                case UpgradeId.UltimateDamage: return "Final Verse";
                default: return id.ToString();
            }
        }

        private static string UpgradeDescription(UpgradeId id)
        {
            switch (id)
            {
                case UpgradeId.AxeDamage: return "+26% frost-axe damage";
                case UpgradeId.AxeSpeed: return "Throw axes 14% faster";
                case UpgradeId.ExtraAxe: return "Throw one additional axe";
                case UpgradeId.AxePierce: return "Axes strike a nearby second target";
                case UpgradeId.MoveSpeed: return "+10% movement speed for the team";
                case UpgradeId.MaxHealth: return "+24 maximum health and heal 24";
                case UpgradeId.Armor: return "+1 armor for both survivors";
                case UpgradeId.ShieldDamage: return "+42% Raven Guard damage";
                case UpgradeId.CriticalRunes: return "+9% critical chance";
                case UpgradeId.UltimateCooldown: return "Ultimate recharges 10% faster";
                case UpgradeId.UltimateDamage: return "+30% Ultimate damage and area";
                default: return string.Empty;
            }
        }

        private void UpdateOfflineLobbyInput()
        {
            if (Time.unscaledTime < _inputReadyAt) return;
            var horizontal = LocalInputRouter.AnyMenuHorizontalPressed();
            if (horizontal != 0) _mapIndex = Wrap(_mapIndex + horizontal, ContentCatalog.Maps.Length);
            var vertical = LocalInputRouter.AnyMenuVerticalPressed();
            if (vertical != 0) _lobbySelection = Wrap(_lobbySelection + vertical, 3);
            if (LocalInputRouter.MenuBackPressed())
            {
                _director.ReturnToMenu();
                return;
            }
            if (!LocalInputRouter.AnyMenuSubmitPressed()) return;
            if (_lobbySelection == 0) StartHost();
            else if (_lobbySelection == 1) StartClient();
            else _director.ReturnToMenu();
        }

        private void UpdateConnectedLobbyInput()
        {
            if (Time.unscaledTime < _inputReadyAt) return;
            var vertical = LocalInputRouter.AnyMenuVerticalPressed();
            if (vertical != 0) _lobbySelection = Wrap(_lobbySelection + vertical, 2);
            if (LocalInputRouter.MenuBackPressed())
            {
                _director.ReturnToMenu();
                return;
            }
            if (!LocalInputRouter.AnyMenuSubmitPressed()) return;
            if (_lobbySelection == 0) ShutdownSession();
            else _director.ReturnToMenu();
        }

        private int OnlineUpgradeInput()
        {
            var direct = LocalInputRouter.LevelChoicePressed(0, 1);
            if (direct >= 0 && direct < _onlineRewards.Count) return direct;
            var horizontal = LocalInputRouter.MenuHorizontalPressed(0, 1);
            if (horizontal != 0) _upgradeSelection = Wrap(_upgradeSelection + horizontal, _onlineRewards.Count);
            return LocalInputRouter.MenuSubmitPressed(0, 1) ? _upgradeSelection : -1;
        }

        private void UpdateOnlineResultsInput()
        {
            var isHost = _networkManager != null && _networkManager.IsHost;
            if (isHost)
            {
                var horizontal = LocalInputRouter.AnyMenuHorizontalPressed();
                if (horizontal != 0) _resultSelection = Wrap(_resultSelection + horizontal, 2);
            }
            else
            {
                _resultSelection = 1;
            }
            if (!LocalInputRouter.AnyMenuSubmitPressed()) return;
            if (isHost && _resultSelection == 0) BeginExpedition();
            else _director.ReturnToMenu();
        }

        private static int Wrap(int value, int count)
        {
            if (count <= 0) return 0;
            if (value < 0) return count - 1;
            if (value >= count) return 0;
            return value;
        }

        private void OnGUI()
        {
            if (!Visible) return;
            var previous = GUI.matrix;
            var canvasScale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            var canvasOffset = new Vector3(
                (Screen.width - 1920f * canvasScale) * 0.5f,
                (Screen.height - 1080f * canvasScale) * 0.5f,
                0f);
            DrawLetterbox(canvasOffset, canvasScale);
            GUI.matrix = Matrix4x4.TRS(canvasOffset, Quaternion.identity, new Vector3(canvasScale, canvasScale, 1f));
            var title = MakeStyle(42, FontStyle.Bold, new Color(0.76f, 0.91f, 0.96f), TextAnchor.MiddleCenter);
            var heading = MakeStyle(27, FontStyle.Bold, new Color(0.93f, 0.76f, 0.32f), TextAnchor.MiddleCenter);
            var text = MakeStyle(20, FontStyle.Normal, new Color(0.82f, 0.88f, 0.9f), TextAnchor.MiddleCenter);
            var body = MakeStyle(20, FontStyle.Normal, new Color(0.82f, 0.88f, 0.9f), TextAnchor.UpperLeft);
            var small = MakeStyle(16, FontStyle.Bold, new Color(0.62f, 0.72f, 0.76f), TextAnchor.MiddleCenter);
            var micro = MakeStyle(13, FontStyle.Bold, new Color(0.72f, 0.82f, 0.86f), TextAnchor.MiddleCenter);
            var badge = MakeStyle(14, FontStyle.Bold, new Color(0.025f, 0.065f, 0.085f), TextAnchor.MiddleCenter);
            var itemTitle = MakeStyle(15, FontStyle.Bold, new Color(0.96f, 0.78f, 0.34f), TextAnchor.UpperLeft);
            var statSection = MakeStyle(14, FontStyle.Bold, new Color(0.93f, 0.76f, 0.32f), TextAnchor.MiddleLeft);
            var statLabel = MakeStyle(14, FontStyle.Bold, new Color(0.62f, 0.72f, 0.76f), TextAnchor.MiddleLeft);
            var statValue = MakeStyle(16, FontStyle.Bold, new Color(0.86f, 0.92f, 0.94f), TextAnchor.MiddleRight);
            var button = MakeButtonStyle(21);

            if (!SessionActive || _phase == OnlinePhase.Lobby)
            {
                DrawPanel(new Rect(0, 0, 1920, 1080), new Color(0.025f, 0.055f, 0.075f, 0.97f));
                GUI.Label(new Rect(350, 55, 1220, 80), "ONLINE EXPEDITION — PRODUCTION CORE", title);
                GUI.Label(new Rect(390, 145, 1140, 70), "Host-authoritative two-player run. The expedition starts automatically when Player 2 connects.", text);
                GUI.Label(new Rect(410, 245, 1100, 60), _status, heading);

                if (!SessionActive)
                {
                    var map = OnlineMap;
                    GUI.Label(new Rect(470, 320, 980, 38), "◀   ONLINE MAP   ▶", small);
                    if (GUI.Button(new Rect(360, 365, 80, 54), "◀", button)) _mapIndex = Wrap(_mapIndex - 1, ContentCatalog.Maps.Length);
                    GUI.Label(new Rect(450, 360, 1020, 65), $"{map.Name.ToUpperInvariant()} — {map.DurationLabel}", text);
                    if (GUI.Button(new Rect(1480, 365, 80, 54), "▶", button)) _mapIndex = Wrap(_mapIndex + 1, ContentCatalog.Maps.Length);
                    GUI.Label(new Rect(570, 440, 780, 32), "HOST IP / LOCALHOST", small);
                    _address = GUI.TextField(new Rect(700, 478, 520, 52), _address, 64);
                    var hostLabel = _lobbySelection == 0 ? "▶ START HOST — HALDOR" : "START HOST — HALDOR";
                    var joinLabel = _lobbySelection == 1 ? "▶ JOIN — EIRA" : "JOIN — EIRA";
                    var hostRect = new Rect(480, 570, 430, 72);
                    var joinRect = new Rect(1010, 570, 430, 72);
                    if (_lobbySelection == 0) DrawBorder(new Rect(hostRect.x - 5, hostRect.y - 5, hostRect.width + 10, hostRect.height + 10), new Color(1f, 0.76f, 0.26f), 4f);
                    if (_lobbySelection == 1) DrawBorder(new Rect(joinRect.x - 5, joinRect.y - 5, joinRect.width + 10, joinRect.height + 10), new Color(1f, 0.76f, 0.26f), 4f);
                    if (GUI.Button(hostRect, hostLabel, button)) { _lobbySelection = 0; StartHost(); }
                    if (GUI.Button(joinRect, joinLabel, button)) { _lobbySelection = 1; StartClient(); }
                    GUI.Label(new Rect(520, 660, 880, 75), "Same PC: 127.0.0.1. LAN: enter the host IPv4 address. UDP port 7777 must be allowed.", text);
                }
                else
                {
                    var peers = _networkManager.IsServer ? _networkManager.ConnectedClientsIds.Count : 1;
                    GUI.Label(new Rect(620, 375, 680, 150), $"{OnlineMap.Name.ToUpperInvariant()}\nPLAYERS {peers}/2\nWaiting for the second survivor...", text);
                    var disconnectLabel = _lobbySelection == 0 ? "▶ DISCONNECT" : "DISCONNECT";
                    var disconnectRect = new Rect(735, 590, 450, 76);
                    if (_lobbySelection == 0) DrawBorder(new Rect(disconnectRect.x - 5, disconnectRect.y - 5, disconnectRect.width + 10, disconnectRect.height + 10), new Color(1f, 0.76f, 0.26f), 4f);
                    if (GUI.Button(disconnectRect, disconnectLabel, button)) { _lobbySelection = 0; ShutdownSession(); }
                }
                var backIndex = SessionActive ? 1 : 2;
                var backLabel = _lobbySelection == backIndex ? "▶ BACK TO CAMP" : "BACK TO CAMP";
                var backRect = new Rect(735, 870, 450, 76);
                if (_lobbySelection == backIndex) DrawBorder(new Rect(backRect.x - 5, backRect.y - 5, backRect.width + 10, backRect.height + 10), new Color(1f, 0.76f, 0.26f), 4f);
                if (GUI.Button(backRect, backLabel, button)) { _lobbySelection = backIndex; _director.ReturnToMenu(); }
                GUI.Label(new Rect(560, 965, 800, 35), "D-PAD/STICK NAVIGATES   •   A CONFIRMS   •   B RETURNS", small);
            }
            else
            {
                if (_showBuildDetails) DrawOnlineBuildDetails(title, heading, body, small, micro, itemTitle, statSection, statLabel, statValue);
                else if (_phase == OnlinePhase.LevelUp) DrawLevelUp(title, heading, body, small, badge, button);
                else if (_phase == OnlinePhase.Victory || _phase == OnlinePhase.Defeat) DrawResults(title, text, button);
                else DrawRunHud(text, small, micro, button, heading);
            }
            GUI.matrix = previous;
            GUI.color = Color.white;
        }

        private void DrawRunHud(GUIStyle text, GUIStyle small, GUIStyle micro, GUIStyle button, GUIStyle heading)
        {
            DrawPanel(new Rect(25, 22, 640, 250), new Color(0.02f, 0.05f, 0.07f, 0.91f));
            var ordered = new List<ulong>(_players.Keys);
            ordered.Sort();
            for (var i = 0; i < ordered.Count; i++)
            {
                var player = _players[ordered[i]];
                var y = 36f + i * 88f;
                GUI.Label(new Rect(45, y, 320, 28), $"P{i + 1}  {PlayerName(player.Id).ToUpperInvariant()}", small);
                var healthText = player.Downed
                    ? $"DOWN — REVIVE {Mathf.RoundToInt(player.ReviveProgress * 100f)}%"
                    : $"{Mathf.CeilToInt(player.Health)} / {Mathf.CeilToInt(player.MaxHealth)}";
                DrawBar(new Rect(360, y + 3, 280, 22), player.Health / Mathf.Max(1f, player.MaxHealth),
                    i == 0 ? new Color(0.28f, 0.68f, 0.88f) : new Color(0.9f, 0.48f, 0.2f), healthText, small);
                var definition = OnlineCharacter(player.Id);
                var cooldown = BalanceRules.UltimateCooldown(definition.UltimateCooldown, player.UltimateCooldownUpgrades);
                var ultimateFill = player.UltimateRemaining <= 0f ? 1f : 1f - player.UltimateRemaining / Mathf.Max(1f, cooldown);
                var ultimateText = player.UltimateRemaining <= 0f ? $"{definition.UltimateName.ToUpperInvariant()} — READY" : $"{definition.UltimateName.ToUpperInvariant()} — {player.UltimateRemaining:0}s";
                DrawBar(new Rect(45, y + 34, 595, 17), ultimateFill, new Color(0.7f, 0.38f, 0.9f), ultimateText, small);
            }
            DrawBar(new Rect(45, 235, 595, 16), _experience / (float)Mathf.Max(1, _experienceToNext),
                new Color(0.22f, 0.72f, 0.9f), $"LEVEL {_level}", small);

            DrawPanel(new Rect(725, 22, 470, 105), new Color(0.02f, 0.05f, 0.07f, 0.91f));
            var timerState = _bossSpawned ? "DEFEAT THE JOTUNN" : $"JOTUNN IN {FormatTime(OnlineMap.BossSpawnTime - _elapsed)}";
            GUI.Label(new Rect(745, 34, 430, 40), $"{FormatTime(_elapsed)} / {FormatTime(OnlineMap.Duration)}", heading);
            GUI.Label(new Rect(745, 78, 430, 28), timerState, small);

            DrawPanel(new Rect(1240, 22, 650, 105), new Color(0.02f, 0.05f, 0.07f, 0.91f));
            GUI.Label(new Rect(1260, 35, 610, 38), $"KILLS {_kills}   RENOWN {_renown}   ENEMIES {_enemies.Count}", text);
            var host = PlayerByIndex(0);
            var client = PlayerByIndex(1);
            var combatSummary = host == null || client == null ? "WAITING FOR BOTH BUILDS" :
                $"P1 AXE {host.AxeCount} × {host.AxeDamage:0}   •   P2 AXE {client.AxeCount} × {client.AxeDamage:0}";
            GUI.Label(new Rect(1260, 80, 610, 25), combatSummary, small);

            DrawOnlineBuildTray(small, micro);

            DrawPanel(new Rect(25, 1010, 1870, 48), new Color(0.02f, 0.05f, 0.07f, 0.86f));
            var rtt = 0UL;
            if (_transport != null && _networkManager.IsConnectedClient && !_networkManager.IsHost)
                rtt = _transport.GetCurrentRtt(NetworkManager.ServerClientId);
            GUI.Label(new Rect(45, 1017, 1500, 28), $"{_status}   •   Snapshot {_lastSnapshotBytes} B @ {SnapshotRate:0} Hz   •   RTT {rtt} ms   •   ULTIMATE: SPACE / RIGHT TRIGGER", small);
            if (GUI.Button(new Rect(1680, 1015, 190, 34), "DISCONNECT", button)) ShutdownSession();

        }

        private void DrawLevelUp(GUIStyle title, GUIStyle heading, GUIStyle text, GUIStyle small, GUIStyle badge, GUIStyle button)
        {
            DrawPanel(new Rect(0, 0, 1920, 1080), new Color(0.008f, 0.022f, 0.032f, 1f));
            GUI.Label(new Rect(500, 85, 920, 75), "CHOOSE THE NEXT VERSE", title);
            var chooser = PlayerByIndex(_rewardTurnPlayerIndex);
            var chooserName = chooser == null ? $"P{_rewardTurnPlayerIndex + 1}" : PlayerName(chooser.Id).ToUpperInvariant();
            GUI.Label(new Rect(460, 165, 1000, 44), $"{chooserName} CHOOSES — ONLY P{_rewardTurnPlayerIndex + 1}'S DEVICE IS ACTIVE", small);
            var localPlayerIndex = _networkManager != null && _networkManager.IsHost ? 0 : 1;
            var canChoose = localPlayerIndex == _rewardTurnPlayerIndex;
            for (var i = 0; i < _onlineRewards.Count; i++)
            {
                var option = _onlineRewards[i];
                if (option.Item == null) continue;
                var rect = new Rect(90 + i * 435, 255, 400, 485);
                var hovered = rect.Contains(Event.current.mousePosition);
                if (hovered && canChoose) _upgradeSelection = i;
                DrawPanel(rect, hovered && canChoose ? new Color(0.075f, 0.145f, 0.19f, 1f) : new Color(0.045f, 0.095f, 0.13f, 1f));
                DrawPanel(new Rect(rect.x, rect.y, rect.width, 13), option.Item.Color);
                if (i == _upgradeSelection && canChoose)
                    DrawBorder(rect, new Color(0.96f, 0.72f, 0.22f), 7f);
                var targetColor = option.Shared ? new Color(0.72f, 0.48f, 0.92f) : OnlineCharacter(PlayerByIndex(option.TargetPlayerIndex).Id).Color;
                DrawOnlineRewardTargetIcons(option, new Rect(rect.x + 180, rect.y + 26, 44, 44), badge);
                DrawPanel(new Rect(rect.x + 229, rect.y + 28, 141, 40), targetColor);
                GUI.Label(new Rect(rect.x + 229, rect.y + 28, 141, 40), OnlineRewardTargetLabel(option), badge);
                GUI.Label(new Rect(rect.x + 25, rect.y + 82, 350, 65), $"{i + 1}. {option.Item.Name}", heading);
                var targetBuild = PlayerByIndex(option.TargetPlayerIndex)?.Build;
                var nextLabel = option.Shared ? "TEAM UPGRADE" : targetBuild?.NextLabel(option.Item) ?? string.Empty;
                GUI.Label(new Rect(rect.x + 30, rect.y + 150, 340, 38), nextLabel, small);
                GUI.Label(new Rect(rect.x + 28, rect.y + 205, 344, 115), option.Item.Description, text);
                GUI.Label(new Rect(rect.x + 25, rect.y + 320, 350, 38), OnlineEvolutionHint(option.Item), small);
                var previousEnabled = GUI.enabled;
                GUI.enabled = canChoose;
                if (GUI.Button(new Rect(rect.x + 28, rect.y + 385, 344, 64), canChoose ? "CHOOSE REWARD" : $"WAITING FOR P{_rewardTurnPlayerIndex + 1}", button))
                {
                    if (_networkManager.IsServer) ResolveUpgrade(i);
                    else SendUpgradeChoice(i);
                }
                if (canChoose && GUI.Button(rect, GUIContent.none, GUIStyle.none))
                {
                    if (_networkManager.IsServer) ResolveUpgrade(i);
                    else SendUpgradeChoice(i);
                }
                GUI.enabled = previousEnabled;
            }
            GUI.Label(new Rect(420, 790, 1080, 55), "CLICK A CARD   •   D-PAD / STICK TO CHOOSE   •   A TO CONFIRM   •   1–4 OR X/Y/B/RB", small);
        }

        private void DrawOnlineBuildTray(GUIStyle small, GUIStyle micro)
        {
            const float panelY = 300f;
            const float panelHeight = 235f;
            DrawPanel(new Rect(25, panelY, 640, panelHeight), new Color(0.018f, 0.045f, 0.062f, 0.94f));
            DrawPanel(new Rect(25, panelY, 640, 4), new Color(0.32f, 0.57f, 0.66f));
            for (var playerIndex = 0; playerIndex < 2; playerIndex++)
            {
                var player = PlayerByIndex(playerIndex);
                if (player == null) continue;
                var y = 318f + playerIndex * 95f;
                GUI.Label(new Rect(39, y, 92, 28), $"P{playerIndex + 1} BUILD", micro);
                var visibleIndex = 0;
                for (var i = 0; i < player.Build.Items.Count && visibleIndex < 12; i++)
                {
                    var state = player.Build.Items[i];
                    var item = ItemCatalog.Find(state.ItemId);
                    if (item == null || item.Category == ItemCategory.Boon) continue;
                    var rect = new Rect(140 + visibleIndex * 41, y, 38, 50);
                    var accent = state.IsEvolved ? new Color(0.98f, 0.68f, 0.22f) : item.Color;
                    DrawPanel(rect, new Color(0.025f, 0.065f, 0.085f, 1f));
                    DrawBorder(rect, accent, 3f);
                    var shortName = item.ShortName.Substring(0, Mathf.Min(3, item.ShortName.Length));
                    GUI.Label(new Rect(rect.x + 2, rect.y + 3, rect.width - 4, 22), shortName, micro);
                    GUI.Label(new Rect(rect.x + 2, rect.y + 25, rect.width - 4, 20), state.IsEvolved ? "E" : $"L{state.Level}", micro);
                    visibleIndex++;
                }
                GUI.Label(new Rect(140, y + 54, 465, 24),
                    $"WEAPONS {player.Build.CountCategory(ItemCategory.Weapon)}/{player.Build.WeaponSlots}   •   GEAR {player.Build.CountCategory(ItemCategory.Gear)}/{player.Build.GearSlots}", small);
            }
            GUI.Label(new Rect(42, panelY + panelHeight - 29, 575, 24), "TAB / GAMEPAD VIEW — DETAILS (RUN CONTINUES ONLINE)", micro);
        }

        private void DrawOnlineBuildDetails(
            GUIStyle title, GUIStyle heading, GUIStyle text, GUIStyle small, GUIStyle micro, GUIStyle itemTitle,
            GUIStyle statSection, GUIStyle statLabel, GUIStyle statValue)
        {
            DrawPanel(new Rect(0, 0, 1920, 1080), new Color(0.008f, 0.022f, 0.032f, 1f));
            GUI.Label(new Rect(480, 30, 960, 75), "ONLINE EXPEDITION BUILD", title);
            GUI.Label(new Rect(460, 101, 1000, 38), "LIVE STATISTICS — THE ONLINE SIMULATION CONTINUES WHILE THIS REFERENCE IS OPEN", small);
            for (var playerIndex = 0; playerIndex < 2; playerIndex++)
            {
                var player = PlayerByIndex(playerIndex);
                if (player == null) continue;
                var definition = OnlineCharacter(player.Id);
                var rect = new Rect(75 + playerIndex * 885, 155, 850, 810);
                DrawPanel(rect, new Color(0.035f, 0.08f, 0.108f, 1f));
                DrawPanel(new Rect(rect.x, rect.y, rect.width, 14), definition.Color);
                GUI.Label(new Rect(rect.x + 30, rect.y + 31, rect.width - 60, 55), $"P{playerIndex + 1} — {definition.Name.ToUpperInvariant()}", heading);
                var ultimateCooldown = BalanceRules.UltimateCooldown(definition.UltimateCooldown, player.UltimateCooldownUpgrades);
                var statGap = 16f;
                var statWidth = (rect.width - 76f - statGap) * 0.5f;
                var survivorStats = new Rect(rect.x + 30, rect.y + 95, statWidth, 225);
                var combatStats = new Rect(survivorStats.xMax + statGap, rect.y + 95, statWidth, 225);
                DrawOnlineStatColumn(survivorStats, "SURVIVOR",
                    new[] { "HEALTH", "ARMOR", "MOVE SPEED", "STATE", "ULTIMATE", "DAMAGE", "COOLDOWN" },
                    new[] { $"{player.Health:0} / {player.MaxHealth:0}", $"{player.Armor:0.0}", $"{player.MoveSpeed:0.00}", player.Downed ? "DOWN" : "ACTIVE", definition.UltimateName, $"{definition.UltimateDamage * player.UltimateDamageMultiplier:0}", $"{ultimateCooldown:0.0}s" },
                    definition.Color, statSection, statLabel, statValue);
                DrawOnlineStatColumn(combatStats, "COMBAT",
                    new[] { "WEAPON", "DAMAGE", "RATE", "PROJECTILES", "PIERCE", "CRITICAL", "RAVEN GUARD" },
                    new[] { "FROST AXE", $"{player.AxeDamage:0.0}", $"{1f / Mathf.Max(0.01f, player.AxeCooldown):0.00}/s", $"{player.AxeCount}", $"{player.AxePierce}", $"{player.CriticalChance * 100f:0}%", $"{player.ShieldDamage:0.0}" },
                    new Color(0.54f, 0.72f, 0.9f), statSection, statLabel, statValue);
                GUI.Label(new Rect(rect.x + 35, rect.y + 338, rect.width - 340, 38), "ITEMS AND SOURCES", small);
                GUI.Label(new Rect(rect.xMax - 310, rect.y + 342, 270, 30),
                    $"WEAPONS {player.Build.CountCategory(ItemCategory.Weapon)}/{player.Build.WeaponSlots}   •   GEAR {player.Build.CountCategory(ItemCategory.Gear)}/{player.Build.GearSlots}", micro);
                var visibleIndex = 0;
                for (var i = 0; i < player.Build.Items.Count && visibleIndex < 12; i++)
                {
                    var state = player.Build.Items[i];
                    var item = ItemCatalog.Find(state.ItemId);
                    if (item == null || item.Category == ItemCategory.Boon) continue;
                    var column = visibleIndex % 3;
                    var row = visibleIndex / 3;
                    var gap = 12f;
                    var cardWidth = (rect.width - 84f - gap * 2f) / 3f;
                    var itemRect = new Rect(rect.x + 30 + column * (cardWidth + gap), rect.y + 385 + row * 94, cardWidth, 90);
                    DrawPanel(itemRect, new Color(0.018f, 0.05f, 0.07f, 1f));
                    DrawBorder(itemRect, state.IsEvolved ? new Color(0.98f, 0.68f, 0.22f) : item.Color, 3f);
                    var evolved = state.IsEvolved ? ItemCatalog.Find(state.EvolutionId) : null;
                    GUI.Label(new Rect(itemRect.x + 10, itemRect.y + 5, itemRect.width - 20, 36),
                        evolved != null ? $"{evolved.Name} — EVOLVED" : $"{item.Name} — LEVEL {state.Level}/{item.MaxLevel}", itemTitle);
                    GUI.Label(new Rect(itemRect.x + 10, itemRect.y + 40, itemRect.width - 20, 46), item.Description, micro);
                    visibleIndex++;
                }
            }
            GUI.Label(new Rect(560, 990, 800, 42), "TAB / GAMEPAD VIEW — RETURN TO EXPEDITION", small);
        }

        private static void DrawOnlineStatColumn(
            Rect rect, string title, string[] labels, string[] values, Color accent,
            GUIStyle sectionStyle, GUIStyle labelStyle, GUIStyle valueStyle)
        {
            DrawPanel(rect, new Color(0.018f, 0.048f, 0.068f, 1f));
            DrawPanel(new Rect(rect.x, rect.y, rect.width, 4), accent);
            GUI.Label(new Rect(rect.x + 14, rect.y + 7, rect.width - 28, 28), title, sectionStyle);
            var rowY = rect.y + 38f;
            const float rowHeight = 25f;
            var count = Mathf.Min(labels.Length, values.Length);
            for (var i = 0; i < count; i++)
            {
                if ((i & 1) == 1)
                    DrawPanel(new Rect(rect.x + 10, rowY, rect.width - 20, rowHeight), new Color(0.04f, 0.08f, 0.1f, 0.75f));
                GUI.Label(new Rect(rect.x + 14, rowY, rect.width * 0.54f - 14, rowHeight), labels[i], labelStyle);
                GUI.Label(new Rect(rect.x + rect.width * 0.54f, rowY, rect.width * 0.46f - 14, rowHeight), values[i], valueStyle);
                rowY += rowHeight;
            }
        }

        private string OnlineRewardTargetLabel(RewardOption option)
        {
            if (option.Shared) return "P1 + P2";
            var target = PlayerByIndex(option.TargetPlayerIndex);
            if (target == null) return $"P{option.TargetPlayerIndex + 1}";
            return $"P{option.TargetPlayerIndex + 1}  {PlayerName(target.Id).Split(' ')[0].ToUpperInvariant()}";
        }

        private void DrawOnlineRewardTargetIcons(RewardOption option, Rect rect, GUIStyle small)
        {
            if (option.Shared)
            {
                DrawOnlinePlayerToken(0, new Rect(rect.x, rect.y + 3, 34, 34), small);
                DrawOnlinePlayerToken(1, new Rect(rect.x + 17, rect.y + 3, 34, 34), small);
            }
            else DrawOnlinePlayerToken(option.TargetPlayerIndex, rect, small);
        }

        private void DrawOnlinePlayerToken(int playerIndex, Rect rect, GUIStyle small)
        {
            var target = PlayerByIndex(playerIndex);
            if (target == null) return;
            var previous = GUI.color;
            GUI.color = OnlineCharacter(target.Id).Color;
            GUI.DrawTexture(rect, RuntimeAssets.Circle.texture, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
            GUI.Label(rect, $"P{playerIndex + 1}", small);
        }

        private static string OnlineEvolutionHint(ItemDefinition item)
        {
            if (!item.IsEvolution) return item.Category.ToString().ToUpperInvariant();
            var baseItem = ItemCatalog.Find(item.EvolutionOf);
            var catalyst = ItemCatalog.Find(item.CatalystId);
            return baseItem == null || catalyst == null ? "EVOLUTION" : $"{baseItem.Name} MAX + {catalyst.Name}";
        }

        private string LastRewardAnnouncement()
        {
            var item = ItemCatalog.At(_lastRewardItemIndex);
            if (item == null) return "ONLINE EXPEDITION SYNCHRONIZED";
            var chooser = PlayerByIndex(_lastRewardChooserPlayerIndex);
            var target = PlayerByIndex(_lastRewardTargetPlayerIndex);
            var chooserName = chooser == null ? $"P{_lastRewardChooserPlayerIndex + 1}" : PlayerName(chooser.Id).ToUpperInvariant();
            var destination = _lastRewardShared ? "THE TEAM" : target == null ? $"P{_lastRewardTargetPlayerIndex + 1}" : PlayerName(target.Id).ToUpperInvariant();
            return $"{chooserName} CHOSE {item.Name.ToUpperInvariant()} FOR {destination}";
        }

        private void DrawResults(GUIStyle title, GUIStyle text, GUIStyle button)
        {
            DrawPanel(new Rect(0, 0, 1920, 1080), new Color(0.01f, 0.025f, 0.04f, 0.84f));
            DrawPanel(new Rect(500, 210, 920, 650), new Color(0.055f, 0.105f, 0.14f, 1f));
            GUI.Label(new Rect(555, 275, 810, 82), _phase == OnlinePhase.Victory ? "A SAGA IS BORN" : "THE ICE CLAIMS THE DUO", title);
            GUI.Label(new Rect(610, 405, 700, 180), $"Time survived: {FormatTime(_elapsed)}\nEnemies defeated: {_kills}\nRenown recovered: {_renown}\nTeam level: {_level}", text);
            var runAgain = _resultSelection == 0 ? "▶ RUN AGAIN" : "RUN AGAIN";
            var returnCamp = _resultSelection == 1 ? "▶ RETURN TO CAMP" : "RETURN TO CAMP";
            if (_networkManager.IsHost && GUI.Button(new Rect(570, 690, 360, 75), runAgain, button)) { _resultSelection = 0; BeginExpedition(); }
            if (GUI.Button(new Rect(990, 690, 360, 75), returnCamp, button)) { _resultSelection = 1; _director.ReturnToMenu(); }
        }

        private static GUIStyle MakeStyle(int size, FontStyle font, Color color, TextAnchor alignment)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = font,
                alignment = alignment,
                wordWrap = true
            };
            SetAllTextColors(style, color);
            return style;
        }

        private static GUIStyle MakeButtonStyle(int size)
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = size,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 6, 6)
            };
            var readable = new Color(0.88f, 0.94f, 0.96f);
            var focused = new Color(1f, 0.76f, 0.26f);
            SetAllTextColors(style, readable);
            style.hover.textColor = focused;
            style.focused.textColor = focused;
            style.onHover.textColor = focused;
            style.onFocused.textColor = focused;
            return style;
        }

        private static void SetAllTextColors(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
        }

        private static void DrawPanel(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, RuntimeAssets.White);
            GUI.color = previous;
        }

        private static void DrawLetterbox(Vector3 offset, float scale)
        {
            var color = GUI.color;
            GUI.color = new Color(0.006f, 0.016f, 0.024f, 1f);
            if (offset.x > 0f)
            {
                GUI.DrawTexture(new Rect(0f, 0f, offset.x, Screen.height), RuntimeAssets.White);
                GUI.DrawTexture(new Rect(offset.x + 1920f * scale, 0f, offset.x, Screen.height), RuntimeAssets.White);
            }
            if (offset.y > 0f)
            {
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, offset.y), RuntimeAssets.White);
                GUI.DrawTexture(new Rect(0f, offset.y + 1080f * scale, Screen.width, offset.y), RuntimeAssets.White);
            }
            GUI.color = color;
        }

        private static void DrawBorder(Rect rect, Color color, float thickness)
        {
            DrawPanel(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawPanel(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawPanel(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawPanel(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static void DrawBar(Rect rect, float fill, Color color, string label, GUIStyle style)
        {
            DrawPanel(rect, new Color(0.02f, 0.035f, 0.045f, 1f));
            DrawPanel(new Rect(rect.x + 2, rect.y + 2, (rect.width - 4) * Mathf.Clamp01(fill), rect.height - 4), color);
            if (!string.IsNullOrEmpty(label)) GUI.Label(rect, label, style);
        }

        private static string FormatTime(float seconds)
        {
            var total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }

    public sealed class OnlineEnemyView : MonoBehaviour, IPoolableComponent
    {
        private SpriteRenderer _renderer;
        private GameObject _rune;

        private void Awake()
        {
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = RuntimeAssets.Circle;
            _rune = new GameObject("Jotunn Rune");
            _rune.transform.SetParent(transform, false);
            _rune.transform.localScale = Vector3.one * 0.3f;
            var runeRenderer = _rune.AddComponent<SpriteRenderer>();
            runeRenderer.sprite = RuntimeAssets.Diamond;
            runeRenderer.color = new Color(1f, 0.42f, 0.2f);
            runeRenderer.sortingOrder = 10;
            _rune.SetActive(false);
        }

        public void Configure(int id, bool boss)
        {
            gameObject.name = boss ? "Online Jotunn Warlord" : $"Online Draugr {id:000}";
            transform.localScale = Vector3.one * (boss ? 1.75f : 0.66f);
            _renderer.color = boss ? new Color(0.55f, 0.2f, 0.68f) : new Color(0.46f, 0.28f, 0.38f);
            _renderer.sortingOrder = boss ? 9 : 5;
            _rune.SetActive(boss);
        }

        public void OnReleasedToPool()
        {
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
            if (_rune != null) _rune.SetActive(false);
        }
    }

    public sealed class OnlineAttackVisual : MonoBehaviour, IPoolableComponent
    {
        private Vector2 _target;
        private float _life;
        private SpriteRenderer _renderer;
        private OnlineCoopSpike _owner;

        private void Awake()
        {
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = RuntimeAssets.Diamond;
            _renderer.sortingOrder = 15;
        }

        public void Initialize(OnlineCoopSpike owner, Vector2 target, int playerIndex, bool critical)
        {
            _owner = owner;
            _target = target;
            _life = 0.3f;
            gameObject.name = critical ? "Critical Network Axe" : "Network Frost Axe";
            _renderer.color = critical ? new Color(1f, 0.82f, 0.25f) :
                (playerIndex == 0 ? new Color(0.42f, 0.91f, 1f) : new Color(1f, 0.62f, 0.25f));
            transform.localScale = Vector3.one * (critical ? 0.34f : 0.24f);
        }

        private void Update()
        {
            _life -= Time.unscaledDeltaTime;
            transform.position = Vector3.MoveTowards(transform.position, _target, 20f * Time.unscaledDeltaTime);
            transform.Rotate(0f, 0f, 900f * Time.unscaledDeltaTime);
            if (_life <= 0f || ((Vector2)transform.position - _target).sqrMagnitude < 0.05f) _owner.ReleaseOnlineAttack(this);
        }

        public void OnReleasedToPool()
        {
            _owner = null;
            _life = 0f;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
        }
    }

    public sealed class OnlinePulseVisual : MonoBehaviour, IPoolableComponent
    {
        private SpriteRenderer _renderer;
        private float _time;
        private int _playerIndex;
        private bool _rush;
        private OnlineCoopSpike _owner;

        private void Awake()
        {
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = RuntimeAssets.Circle;
            _renderer.sortingOrder = 14;
        }

        public void Initialize(OnlineCoopSpike owner, int playerIndex, bool rush)
        {
            _owner = owner;
            _playerIndex = playerIndex;
            _rush = rush;
            _time = 0f;
            gameObject.name = rush ? "Network Ultimate" : "Network Shield Pulse";
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            _time += Time.unscaledDeltaTime;
            var duration = _rush ? 0.85f : 0.35f;
            var size = _rush ? 13f : 5.2f;
            transform.localScale = Vector3.one * Mathf.Lerp(0.2f, size, _time / duration);
            var color = _playerIndex == 0 ? new Color(0.35f, 0.9f, 1f) : new Color(1f, 0.58f, 0.22f);
            _renderer.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.35f, 0f, _time / duration));
            if (_time >= duration) _owner.ReleaseOnlinePulse(this);
        }

        public void OnReleasedToPool()
        {
            _owner = null;
            _time = 0f;
            _rush = false;
            transform.localScale = Vector3.zero;
        }
    }
}
