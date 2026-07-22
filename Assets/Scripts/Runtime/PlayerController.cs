using UnityEngine;

namespace ProjectExpedition
{
    public sealed class PlayerController : MonoBehaviour
    {
        public int PlayerIndex { get; private set; }
        public string HeroName { get; private set; }
        public CharacterDefinition Definition { get; private set; }
        public WeaponSystem Weapons { get; private set; }
        public PlayerBuild Build { get; } = new PlayerBuild();
        public float MaxHealth => _model.MaxHealth;
        public float Health => _model.Health;
        public float MoveSpeed => _model.MoveSpeed;
        public float Armor => _model.Armor;
        public float MagnetRadius => _model.MagnetRadius;
        public float EffectiveMagnetRadius => _model.EffectiveMagnetRadius;
        public bool IsAlive => _model.IsAlive;
        public bool IsDowned => _model.IsDowned;
        public float ReviveProgress => _model.ReviveProgress;
        public string UltimateName => Definition != null ? Definition.UltimateName : "Ultimate";
        public float UltimateRemaining => _model.UltimateRemaining;
        public float UltimateCooldown => _model.UltimateCooldown;
        public bool UltimateReady => _model.UltimateReady;
        public float UltimateDamage => _model.UltimateDamage;
        public float UltimateRadius => _model.UltimateRadius;

        private GameDirector _director;
        private SpriteRenderer _body;
        private readonly SharedPlayerModel _model = new SharedPlayerModel();
        private Color _heroColor;
        private HeroPresentation _presentation;
        private TemporaryEffectGlowPresentation _effectGlow;

        public void Initialize(GameDirector director, int playerIndex)
        {
            Initialize(director, playerIndex, ContentCatalog.Character(playerIndex));
        }

        public void Initialize(GameDirector director, int playerIndex, CharacterDefinition definition)
        {
            _director = director;
            PlayerIndex = playerIndex;
            Definition = DevelopmentTuningResolver.ResolveCharacter(definition ?? ContentCatalog.Character(playerIndex));
            HeroName = Definition.Name;
            _heroColor = Definition.Color;
            _model.Begin(Definition.MaxHealth, Definition.MoveSpeed, Definition.Armor,
                Definition.UltimateCooldown, Definition.UltimateDamage, Definition.UltimateRadius,
                BalanceRules.UltimateCooldown, DevelopmentTuningResolver.DefaultMagnetRadius);
            var resolvedMap = DevelopmentTuningResolver.ResolveMap(director.SelectedMap);
            Build.Initialize(resolvedMap.WeaponSlots, resolvedMap.GearSlots,
                Definition.StarterWeaponIds);
            _body = gameObject.AddComponent<SpriteRenderer>();
            _body.sprite = RuntimeAssets.Circle;
            _body.color = _heroColor;
            _body.sortingOrder = 10;
            transform.localScale = Vector3.one * 0.78f;

            var shield = new GameObject("Raven Shield");
            shield.transform.SetParent(transform, false);
            shield.transform.localPosition = new Vector3(-0.48f, 0.05f, 0f);
            shield.transform.localScale = new Vector3(0.56f, 0.72f, 1f);
            var shieldRenderer = shield.AddComponent<SpriteRenderer>();
            shieldRenderer.sprite = RuntimeAssets.Circle;
            shieldRenderer.color = new Color(0.13f, 0.22f, 0.31f);
            shieldRenderer.sortingOrder = 11;

            var rune = new GameObject("Frost Rune");
            rune.transform.SetParent(transform, false);
            rune.transform.localPosition = new Vector3(0.08f, 0.08f, 0f);
            rune.transform.localScale = Vector3.one * 0.22f;
            var runeRenderer = rune.AddComponent<SpriteRenderer>();
            runeRenderer.sprite = RuntimeAssets.Diamond;
            runeRenderer.color = new Color(0.36f, 0.95f, 1f);
            runeRenderer.sortingOrder = 12;

            var marker = new GameObject($"P{playerIndex + 1} Marker");
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = Vector3.up * 0.72f;
            marker.transform.localScale = new Vector3(0.32f, 0.18f, 1f);
            var markerRenderer = marker.AddComponent<SpriteRenderer>();
            markerRenderer.sprite = RuntimeAssets.Diamond;
            markerRenderer.color = playerIndex == 0 ? new Color(0.35f, 0.92f, 1f) : new Color(1f, 0.63f, 0.24f);
            markerRenderer.sortingOrder = 13;

            _presentation = gameObject.AddComponent<HeroPresentation>();
            _presentation.Initialize(_body, runeRenderer, _heroColor, Definition, PlayerIndex);
            _effectGlow = gameObject.AddComponent<TemporaryEffectGlowPresentation>();
            _effectGlow.Initialize(_body);

            Weapons = new WeaponSystem(director, this);
            Weapons.SyncFromBuild(Build);
            var mastery = SharedMetaProgressionModel.ResolveMastery(SaveService.Data, Definition.Id);
            Weapons.ApplyHeroMastery(Definition.Id, mastery);
        }

        public void ReinitializeFromDefinition(CharacterDefinition definition)
        {
            if (definition == null || _director == null)
            {
                return;
            }

            Definition = definition;
            HeroName = Definition.Name;
            _model.Begin(Definition.MaxHealth, Definition.MoveSpeed, Definition.Armor,
                Definition.UltimateCooldown, Definition.UltimateDamage, Definition.UltimateRadius,
                BalanceRules.UltimateCooldown, DevelopmentTuningResolver.DefaultMagnetRadius);
        }

        private void Update()
        {
            if (_director == null || _director.State != RunState.Playing)
            {
                return;
            }

            if (IsDowned)
            {
                UpdateRevival();
                return;
            }

            _model.Advance(Time.deltaTime);

            if (!_director.IsExpeditionCombatActive)
            {
                _presentation.Tick(Vector2.zero, _model.InvulnerabilityRemaining > 0f, false, Time.deltaTime);
                return;
            }

            var move = LocalInputRouter.ReadMovement(PlayerIndex, _director.Players.Count);
            var nextPosition = _model.CalculateRequestedPosition((Vector2)transform.position, move,
                Time.deltaTime, _director.ObstacleLayout.Obstacles);
            transform.position = _director.ConstrainToCoopRange(this, nextPosition);
            _presentation.Tick(move, _model.InvulnerabilityRemaining > 0f, false, Time.deltaTime);

            if (LocalInputRouter.UltimatePressed(PlayerIndex, _director.Players.Count))
            {
                ActivateUltimate();
            }

            Weapons.Tick(Time.deltaTime);
        }

        private void ActivateUltimate()
        {
            if (Definition == null || !_model.TryActivateUltimate()) return;
            _presentation.ShowUltimate();
            var effect = SharedEffectPipeline.CreateUltimate(_model);
            _director.ResolveAreaEffect(transform.position, effect, true, PlayerIndex);
            _director.ShowUltimate(transform.position, PlayerIndex, effect.Radius);
            _director.Announce($"{HeroName.ToUpperInvariant()} — {UltimateName.ToUpperInvariant()}", 2.2f);
        }

        private void UpdateRevival()
        {
            _presentation.Tick(Vector2.zero, false, true, Time.deltaTime);
            var rescuerNearby = false;
            for (var i = 0; i < _director.Players.Count; i++)
            {
                var other = _director.Players[i];
                if (other == null || other == this || !other.IsAlive) continue;
                if (((Vector2)other.transform.position - (Vector2)transform.position).sqrMagnitude <=
                    SharedPlayerModel.DefaultReviveRadius * SharedPlayerModel.DefaultReviveRadius)
                {
                    rescuerNearby = true;
                    break;
                }
            }
            if (_model.AdvanceRevival(rescuerNearby, Time.deltaTime))
            {
                _presentation.ShowReviveSuccess();
                _director.OnPlayerRevived(this);
                return;
            }
        }

        public void TakeDamage(float rawDamage)
        {
            TakeDamage(rawDamage, DevelopmentTuningResolver.PlayerContactKnockback, transform.position,
                PlayerHurtHitKind.Contact);
        }

        public void TakeDamage(float rawDamage, float knockback, Vector2 source, PlayerHurtHitKind hitKind)
        {
            var damage = SharedChallengeProfileModel.ApplyPlayerDamageTakenMultiplier(
                rawDamage, _director.SelectedChallenge);
            var currentPosition = (Vector2)transform.position;
            var healthBefore = _model.Health;
            var result = _model.TakeDamage(
                damage,
                knockback,
                source,
                currentPosition,
                _director.ObstacleLayout.Obstacles,
                out var resolvedPosition);

            if (result == PlayerDamageResult.Ignored)
            {
                return;
            }

            transform.position = _director.ConstrainToCoopRange(this, resolvedPosition);
            var healthFractionBefore = healthBefore / Mathf.Max(1f, _model.MaxHealth);
            _director.PresentPlayerHurt(this, damage, source, hitKind, healthFractionBefore);
            if (result == PlayerDamageResult.Downed)
            {
                _director.OnPlayerDowned(this);
            }
        }

        public void PresentDamageTaken(Vector2 source) => _presentation?.ShowDamageTaken(source);

        public void PresentAttack(Vector2 direction) => _presentation?.ShowAttack(direction);

        public void Heal(float amount) => _model.Heal(amount);
        public void AddMoveSpeed(float amount) => _model.AddMoveSpeed(amount);
        public void AddArmor(float amount) => _model.AddArmor(amount);
        public void AddMagnet(float amount) => _model.AddMagnet(amount);

        public void ImproveUltimateCooldown() => _model.ImproveUltimateCooldown();

        public void ImproveUltimateDamage() => _model.ImproveUltimateDamage();

        public void AddMaxHealth(float amount) => _model.AddMaxHealth(amount);

        public void ApplyTemporaryMagnetBoost(float amount, float durationSeconds) =>
            _model.ApplyTemporaryMagnetBoost(amount, durationSeconds);

        public void ApplyTemporaryArmorAura(float amount, float durationSeconds) =>
            _model.ApplyTemporaryArmorAura(amount, durationSeconds);

        public void ApplyTemporaryLootMoveSpeed(string instanceKey, float amount) =>
            _model.ApplyTemporaryLootMoveSpeed(instanceKey, amount);

        public void ApplyTemporaryLootCriticalBonus(string instanceKey, float amount) =>
            _model.ApplyTemporaryLootCriticalBonus(instanceKey, amount);

        public void ApplyTemporaryLootDamageMultiplier(string instanceKey, float multiplier) =>
            _model.ApplyTemporaryLootDamageMultiplier(instanceKey, multiplier);

        public void RemoveTemporaryLootBonus(string instanceKey) =>
            _model.RemoveTemporaryLootBonus(instanceKey);

        public void RefreshTemporaryInvulnerability(float remaining) =>
            _model.RefreshTemporaryInvulnerability(remaining);

        public float TemporaryCriticalBonus => _model.TemporaryCriticalBonus;

        public float TemporaryDamageMultiplier => _model.TemporaryDamageMultiplier;

        public void ApplyBuildResult(BuildApplyResult result)
        {
            if (result == null || result.Item == null) return;
            if (result.Evolution)
            {
                Weapons.Model.ApplyEvolution(result.Item.Id);
                Weapons.SyncFromBuild(Build);
                return;
            }

            var upgradeId = result.Item.EffectAtLevel(result.NewLevel);
            if (result.Item.Category == ItemCategory.Weapon)
            {
                SharedEffectPipeline.ApplyUpgrade(_model, Weapons.Model, upgradeId, result.Item.Id);
            }
            else
            {
                SharedEffectPipeline.ApplyUpgrade(_model, Weapons.Model, upgradeId);
            }

            Weapons.SyncFromBuild(Build);
        }

        public void UpdateTemporaryEffectPresentation(SharedTemporaryEffectModel effect)
        {
            if (_effectGlow == null || effect == null)
            {
                return;
            }

            var recent = effect.GetMostRecentActiveEffect();
            if (recent == null || recent.Definition == null)
            {
                _effectGlow.SetEffect(Color.clear, false);
                return;
            }

            var appliesToPlayer = recent.Definition.EffectTarget == TemporaryEffectTarget.WholeParty ||
                recent.ActivatorPlayerIndex == PlayerIndex;
            _effectGlow.SetEffect(recent.Definition.ThemeColor, appliesToPlayer);
        }
    }
}
