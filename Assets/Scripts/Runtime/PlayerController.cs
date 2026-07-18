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
        public float MaxHealth { get; private set; } = 120f;
        public float Health { get; private set; }
        public float MoveSpeed { get; private set; } = 4.6f;
        public float Armor { get; private set; } = 1f;
        public float MagnetRadius { get; private set; } = 1.7f;
        public bool IsAlive => Health > 0f && !IsDowned;
        public bool IsDowned { get; private set; }
        public float ReviveProgress { get; private set; }
        public string UltimateName => Definition != null ? Definition.UltimateName : "Ultimate";
        public float UltimateRemaining { get; private set; }
        public float UltimateCooldown { get; private set; }
        public bool UltimateReady => UltimateRemaining <= 0f;
        public float UltimateDamage => Definition != null ? Definition.UltimateDamage * _ultimateDamageMultiplier : 0f;

        private GameDirector _director;
        private SpriteRenderer _body;
        private float _hitFlash;
        private int _ultimateCooldownUpgrades;
        private float _ultimateDamageMultiplier = 1f;
        private Color _heroColor;

        public void Initialize(GameDirector director, int playerIndex)
        {
            Initialize(director, playerIndex, ContentCatalog.Character(playerIndex));
        }

        public void Initialize(GameDirector director, int playerIndex, CharacterDefinition definition)
        {
            _director = director;
            PlayerIndex = playerIndex;
            Definition = definition ?? ContentCatalog.Character(playerIndex);
            HeroName = Definition.Name;
            _heroColor = Definition.Color;
            MaxHealth = Definition.MaxHealth;
            MoveSpeed = Definition.MoveSpeed;
            Armor = Definition.Armor;
            Health = MaxHealth;
            UltimateCooldown = Definition.UltimateCooldown;
            UltimateRemaining = UltimateCooldown * 0.3f;
            Build.Initialize(director.SelectedMap.WeaponSlots, director.SelectedMap.GearSlots);
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

            Weapons = new WeaponSystem(director, this);
            if (Definition.Id == "ravenbound.haldor") Weapons.ApplyMastery(SaveService.Data.HaldorMastery);
        }

        private void Update()
        {
            if (_director == null || _director.State != RunState.Playing) return;
            if (IsDowned)
            {
                UpdateRevival();
                return;
            }
            _hitFlash = Mathf.Max(0f, _hitFlash - Time.deltaTime);
            UltimateRemaining = Mathf.Max(0f, UltimateRemaining - Time.deltaTime);
            _body.color = _hitFlash > 0f ? Color.white : _heroColor;

            var move = LocalInputRouter.ReadMovement(PlayerIndex, _director.Players.Count);
            var nextPosition = (Vector2)transform.position + move * MoveSpeed * Time.deltaTime;
            transform.position = _director.ConstrainToCoopRange(this, nextPosition);

            if (LocalInputRouter.UltimatePressed(PlayerIndex, _director.Players.Count) && UltimateReady) ActivateUltimate();
            Weapons.Tick(Time.deltaTime);
        }

        private void ActivateUltimate()
        {
            if (Definition == null || !UltimateReady) return;
            UltimateRemaining = UltimateCooldown;
            _hitFlash = 1.25f;
            var radius = Definition.UltimateRadius * (1f + (_ultimateDamageMultiplier - 1f) * 0.35f);
            _director.DamageEnemiesInRadius(transform.position, radius,
                Definition.UltimateDamage * _ultimateDamageMultiplier, 3.2f);
            _director.ShowUltimate(transform.position, PlayerIndex, radius);
            _director.Announce($"{HeroName.ToUpperInvariant()} — {UltimateName.ToUpperInvariant()}", 2.2f);
        }

        private void UpdateRevival()
        {
            var rescuerNearby = false;
            for (var i = 0; i < _director.Players.Count; i++)
            {
                var other = _director.Players[i];
                if (other == null || other == this || !other.IsAlive) continue;
                if (((Vector2)other.transform.position - (Vector2)transform.position).sqrMagnitude <= 1.8f * 1.8f)
                {
                    rescuerNearby = true;
                    break;
                }
            }
            ReviveProgress = Mathf.Clamp01(ReviveProgress + (rescuerNearby ? 1f : -0.35f) * Time.deltaTime / 2.5f);
            _body.color = Color.Lerp(new Color(0.12f, 0.15f, 0.17f), _heroColor, ReviveProgress * 0.65f);
            if (ReviveProgress >= 1f) Revive();
        }

        public void TakeDamage(float rawDamage)
        {
            if (!IsAlive || IsDowned || _hitFlash > 0f) return;
            Health -= Mathf.Max(1f, rawDamage - Armor);
            _hitFlash = 0.22f;
            if (Health <= 0f)
            {
                Health = 0f;
                IsDowned = true;
                ReviveProgress = 0f;
                _director.OnPlayerDowned(this);
            }
        }

        private void Revive()
        {
            IsDowned = false;
            ReviveProgress = 0f;
            Health = MaxHealth * 0.42f;
            _hitFlash = 1f;
            _body.color = Color.white;
            _director.OnPlayerRevived(this);
        }

        public void Heal(float amount) => Health = Mathf.Min(MaxHealth, Health + amount);
        public void AddMoveSpeed(float amount) => MoveSpeed += amount;
        public void AddArmor(float amount) => Armor += amount;
        public void AddMagnet(float amount) => MagnetRadius += amount;

        public void ImproveUltimateCooldown()
        {
            _ultimateCooldownUpgrades++;
            var previous = UltimateCooldown;
            UltimateCooldown = BalanceRules.UltimateCooldown(Definition.UltimateCooldown, _ultimateCooldownUpgrades);
            if (previous > 0f) UltimateRemaining = Mathf.Min(UltimateCooldown, UltimateRemaining * UltimateCooldown / previous);
        }

        public void ImproveUltimateDamage() => _ultimateDamageMultiplier *= 1.3f;

        public void AddMaxHealth(float amount)
        {
            MaxHealth += amount;
            Health += amount;
        }
    }
}
