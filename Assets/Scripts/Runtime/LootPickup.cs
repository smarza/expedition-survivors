using UnityEngine;

namespace ProjectExpedition
{
    public sealed class LootPickup : MonoBehaviour, IPoolableComponent
    {
        private GameDirector _director;
        private LootEffectDefinition _definition;
        private float _age;
        private bool _collected;
        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = gameObject.GetComponent<SpriteRenderer>();
            if (_renderer == null)
            {
                _renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            _renderer.sprite = RuntimeAssets.Diamond;
            _renderer.sortingOrder = 6;
        }

        public void Initialize(GameDirector director, LootEffectDefinition definition)
        {
            _director = director;
            _definition = definition ?? LootEffectCatalog.DefaultRunLoot;
            _age = 0f;
            _collected = false;
            gameObject.name = $"Loot — {_definition.DisplayName}";
            _renderer.color = _definition.ThemeColor;
            transform.localScale = Vector3.one * 0.22f;
        }

        private void Update()
        {
            if (_director == null || _director.State != RunState.Playing || _collected)
            {
                return;
            }

            _age += Time.deltaTime;
            transform.Rotate(0f, 0f, 120f * Time.deltaTime);
            var pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.08f;
            transform.localScale = Vector3.one * 0.22f * pulse;

            var pickupPosition = (Vector2)transform.position;
            if (_director.TryResolvePickupCollection(
                    pickupPosition,
                    SharedPickupCollectionModel.DefaultCollectionRadiusSqr,
                    out var collector))
            {
                _collected = true;
                _director.OnLootCollected(_definition, collector.PlayerIndex);
                _director.ReleaseLootPickup(this);
                return;
            }

            if (_director.TryResolvePickupMagnetTarget(pickupPosition, out _, out var magnetDelta))
            {
                transform.position += (Vector3)(magnetDelta.normalized *
                    (4f + 8f / Mathf.Max(0.5f, magnetDelta.magnitude)) * Time.deltaTime);
            }

            if (_age > 40f)
            {
                _director.ReleaseLootPickup(this);
            }
        }

        public void OnReleasedToPool()
        {
            _director = null;
            _definition = null;
            _age = 0f;
            _collected = false;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
