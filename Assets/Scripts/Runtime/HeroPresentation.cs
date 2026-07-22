using UnityEngine;

namespace ProjectExpedition
{
    public sealed class HeroPresentation : MonoBehaviour
    {
        private static readonly Color HurtFlashColor = new Color(1f, 0.28f, 0.16f);
        private static readonly Color InvulnerabilityFlashColor = new Color(0.42f, 0.82f, 1f);
        private static readonly Color ReviveFlashColor = new Color(0.52f, 0.92f, 0.82f);

        private SpriteRenderer _body;
        private SpriteRenderer _rune;
        private Color _baseColor;
        private float _hurtTimer;
        private float _reviveTimer;
        private float _attackTimer;
        private float _ultimateTimer;
        private float _staggerTimer;
        private Vector2 _facing = Vector2.right;
        private Vector2 _staggerDirection = Vector2.right;
        private int _playerIndex;

        public void Initialize(SpriteRenderer body, SpriteRenderer rune, Color baseColor,
            CharacterDefinition definition, int playerIndex)
        {
            _body = body;
            _rune = rune;
            _baseColor = baseColor;
            _playerIndex = playerIndex;
            if (definition != null && definition.Id == "ravenbound.haldor")
                BuildHaldorSilhouette();
            else if (definition != null && definition.Id == "oathbound.sylva")
                BuildSylvaSilhouette();
            else if (definition != null && definition.Id == "ironway.mara")
                BuildMaraSilhouette();
            else if (definition != null && definition.Id == "oathbound.bren")
                BuildBrenSilhouette();
            else if (definition != null && definition.Id == "ironway.rex")
                BuildRexSilhouette();
            else
                BuildEiraSilhouette();
        }

        public void Tick(Vector2 movement, bool invulnerable, bool downed, float deltaTime)
        {
            _hurtTimer = Mathf.Max(0f, _hurtTimer - deltaTime);
            _reviveTimer = Mathf.Max(0f, _reviveTimer - deltaTime);
            _attackTimer = Mathf.Max(0f, _attackTimer - deltaTime);
            _staggerTimer = Mathf.Max(0f, _staggerTimer - deltaTime);
            _ultimateTimer = Mathf.Max(0f, _ultimateTimer - Time.unscaledDeltaTime);
            if (movement.sqrMagnitude > 0.02f)
            {
                _facing = movement.normalized;
            }

            var breathing = downed ? 0f : Mathf.Sin(Time.time * 4.2f + _playerIndex) * 0.018f;
            var attack = _attackTimer > 0f ? Mathf.Sin((_attackTimer / 0.18f) * Mathf.PI) * 0.12f : 0f;
            transform.localScale = new Vector3(0.78f + breathing + attack, 0.78f - breathing + attack, 1f);

            var lean = downed ? -82f : Mathf.Clamp(-_facing.x * 7f, -7f, 7f);
            if (_staggerTimer > 0f && !downed)
            {
                var staggerStrength = _staggerTimer / 0.12f;
                lean += -_staggerDirection.x * 14f * staggerStrength;
            }

            transform.rotation = Quaternion.Euler(0f, 0f, lean);
            _body.color = ResolveBodyColor(invulnerable, downed);

            if (_rune != null)
            {
                var pulse = 0.18f + Mathf.Sin(Time.unscaledTime * 6f) * 0.035f;
                _rune.transform.localScale = Vector3.one * (_ultimateTimer > 0f ? pulse * 1.7f : pulse);
                _rune.transform.Rotate(0f, 0f, 45f * Time.unscaledDeltaTime);
            }
        }

        public void ShowDamageTaken(Vector2 source)
        {
            var reduced = PresentationPreferences.Data.ReducedFlashes;
            _hurtTimer = reduced ? 0.05f : 0.12f;
            _staggerTimer = reduced ? 0.06f : 0.12f;

            var away = ((Vector2)transform.position - source);
            if (away.sqrMagnitude > 0.01f)
            {
                _staggerDirection = away.normalized;
            }
            else
            {
                _staggerDirection = -_facing;
            }
        }

        public void ShowReviveSuccess()
        {
            _reviveTimer = PresentationPreferences.Data.ReducedFlashes ? 0.04f : 0.1f;
        }

        public void ShowHit() => ShowReviveSuccess();

        public void ShowAttack(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.01f)
            {
                _facing = direction.normalized;
            }

            _attackTimer = 0.18f;
        }

        public void ShowUltimate() => _ultimateTimer = 0.75f;

        private Color ResolveBodyColor(bool invulnerable, bool downed)
        {
            if (downed)
            {
                return Color.Lerp(new Color(0.12f, 0.15f, 0.17f), _baseColor, 0.35f);
            }

            if (_hurtTimer > 0f)
            {
                var strength = PresentationPreferences.Data.ReducedFlashes ? 0.55f : 0.82f;
                return Color.Lerp(_baseColor, HurtFlashColor, strength * (_hurtTimer / 0.12f));
            }

            if (_reviveTimer > 0f)
            {
                var strength = PresentationPreferences.Data.ReducedFlashes ? 0.45f : 0.72f;
                return Color.Lerp(_baseColor, ReviveFlashColor, strength * (_reviveTimer / 0.1f));
            }

            if (_ultimateTimer > 0f)
            {
                return Color.Lerp(_baseColor, Color.white, 0.35f);
            }

            if (invulnerable)
            {
                var pulse = 0.45f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f)) * 0.25f;
                if (PresentationPreferences.Data.ReducedFlashes)
                {
                    pulse = 0.55f;
                }

                return Color.Lerp(_baseColor, InvulnerabilityFlashColor, pulse * 0.42f);
            }

            return _baseColor;
        }

        private void BuildHaldorSilhouette()
        {
            AddPart("Fur Mantle", RuntimeAssets.Circle, new Color(0.16f, 0.2f, 0.24f),
                new Vector3(0f, 0.22f, 0f), new Vector3(1.16f, 0.72f, 1f), 9);
            AddPart("Copper Beard", RuntimeAssets.Diamond, new Color(0.7f, 0.34f, 0.12f),
                new Vector3(0.12f, -0.22f, 0f), new Vector3(0.38f, 0.55f, 1f), 12);
            AddPart("Raven Brooch", RuntimeAssets.Diamond, new Color(0.94f, 0.67f, 0.2f),
                new Vector3(-0.08f, 0.05f, 0f), Vector3.one * 0.13f, 13);
            var axe = AddPart("Readied Frost Axe", RuntimeAssets.Diamond, new Color(0.42f, 0.91f, 1f),
                new Vector3(0.56f, 0.1f, 0f), new Vector3(0.18f, 0.42f, 1f), 11);
            axe.transform.rotation = Quaternion.Euler(0f, 0f, -24f);
        }

        private void BuildSylvaSilhouette()
        {
            AddPart("Leaf Cloak", RuntimeAssets.Diamond, new Color(0.14f, 0.34f, 0.22f),
                new Vector3(0f, 0.12f, 0f), new Vector3(0.92f, 1.12f, 1f), 9);
            AddPart("Canopy Layer", RuntimeAssets.Circle, new Color(0.22f, 0.48f, 0.28f),
                new Vector3(-0.08f, 0.28f, 0f), new Vector3(0.78f, 0.52f, 1f), 10);
            AddPart("Antler Brooch", RuntimeAssets.Diamond, new Color(0.78f, 0.62f, 0.36f),
                new Vector3(0.02f, 0.18f, 0f), Vector3.one * 0.16f, 13);
            var staff = AddPart("Thorn Staff", RuntimeAssets.Diamond, new Color(0.36f, 0.58f, 0.32f),
                new Vector3(0.52f, -0.04f, 0f), new Vector3(0.14f, 0.72f, 1f), 11);
            staff.transform.rotation = Quaternion.Euler(0f, 0f, 14f);
        }

        private void BuildMaraSilhouette()
        {
            AddPart("Field Pack", RuntimeAssets.Circle, new Color(0.24f, 0.28f, 0.26f),
                new Vector3(-0.18f, 0.16f, 0f), new Vector3(0.62f, 0.82f, 1f), 9);
            AddPart("Shell Plate", RuntimeAssets.Diamond, new Color(0.34f, 0.38f, 0.4f),
                new Vector3(0.04f, 0.08f, 0f), new Vector3(0.72f, 0.96f, 1f), 10);
            AddPart("Visor Band", RuntimeAssets.Diamond, new Color(0.42f, 0.82f, 0.92f),
                new Vector3(0.1f, 0.34f, 0f), new Vector3(0.46f, 0.12f, 1f), 12);
            var launcher = AddPart("Flare Launcher", RuntimeAssets.Diamond, new Color(0.92f, 0.48f, 0.18f),
                new Vector3(0.58f, 0.06f, 0f), new Vector3(0.2f, 0.34f, 1f), 11);
            launcher.transform.rotation = Quaternion.Euler(0f, 0f, -18f);
        }

        private void BuildBrenSilhouette()
        {
            AddPart("Bark Mantle", RuntimeAssets.Circle, new Color(0.22f, 0.34f, 0.24f),
                new Vector3(0f, 0.18f, 0f), new Vector3(1.08f, 0.78f, 1f), 9);
            AddPart("Root Wrap", RuntimeAssets.Diamond, new Color(0.34f, 0.24f, 0.16f),
                new Vector3(-0.06f, -0.12f, 0f), new Vector3(0.72f, 0.42f, 1f), 10);
            AddPart("Acorn Brooch", RuntimeAssets.Diamond, new Color(0.72f, 0.52f, 0.24f),
                new Vector3(0.04f, 0.12f, 0f), Vector3.one * 0.14f, 13);
            var staff = AddPart("Driftwood Staff", RuntimeAssets.Diamond, new Color(0.48f, 0.38f, 0.28f),
                new Vector3(0.5f, -0.02f, 0f), new Vector3(0.16f, 0.78f, 1f), 11);
            staff.transform.rotation = Quaternion.Euler(0f, 0f, 10f);
        }

        private void BuildRexSilhouette()
        {
            AddPart("Breacher Rig", RuntimeAssets.Circle, new Color(0.28f, 0.24f, 0.22f),
                new Vector3(-0.12f, 0.14f, 0f), new Vector3(0.78f, 0.88f, 1f), 9);
            AddPart("Blast Plate", RuntimeAssets.Diamond, new Color(0.46f, 0.36f, 0.28f),
                new Vector3(0.06f, 0.1f, 0f), new Vector3(0.8f, 1.02f, 1f), 10);
            AddPart("Charge Band", RuntimeAssets.Diamond, new Color(0.92f, 0.58f, 0.22f),
                new Vector3(0.08f, 0.32f, 0f), new Vector3(0.5f, 0.14f, 1f), 12);
            var beacon = AddPart("Iron Beacon", RuntimeAssets.Diamond, new Color(0.68f, 0.62f, 0.52f),
                new Vector3(0.56f, 0.04f, 0f), new Vector3(0.24f, 0.38f, 1f), 11);
            beacon.transform.rotation = Quaternion.Euler(0f, 0f, -22f);
        }

        private void BuildEiraSilhouette()
        {
            AddPart("Raven Cloak", RuntimeAssets.Diamond, new Color(0.12f, 0.16f, 0.24f),
                new Vector3(0f, 0.08f, 0f), new Vector3(0.86f, 1.08f, 1f), 9);
            AddPart("Raven Feather", RuntimeAssets.Diamond, new Color(0.58f, 0.72f, 0.92f),
                new Vector3(0.44f, 0.35f, 0f), new Vector3(0.12f, 0.42f, 1f), 12);
        }

        private GameObject AddPart(string partName, Sprite sprite, Color color, Vector3 position,
            Vector3 scale, int order)
        {
            var part = new GameObject(partName);
            part.transform.SetParent(transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return part;
        }
    }
}
