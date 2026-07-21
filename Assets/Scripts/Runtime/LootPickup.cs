using UnityEngine;

namespace ProjectExpedition
{
    public sealed class LootPickup : MonoBehaviour, IPoolableComponent
    {
        private GameDirector _director;
        private LootEffectDefinition _definition;
        private float _age;
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
            gameObject.name = _definition.DisplayName;
            _renderer.color = _definition.ThemeColor;
            transform.localScale = Vector3.one * 0.22f;
        }

        private void Update()
        {
            if (_director == null || _director.State != RunState.Playing)
            {
                return;
            }

            _age += Time.deltaTime;
            transform.Rotate(0f, 0f, 120f * Time.deltaTime);
            var pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.08f;
            transform.localScale = Vector3.one * 0.22f * pulse;

            var collector = _director.GetNearestLivingPlayer(transform.position);
            if (collector == null)
            {
                return;
            }

            var delta = (Vector2)collector.transform.position - (Vector2)transform.position;
            var magnet = collector.MagnetRadius;

            if (delta.sqrMagnitude < magnet * magnet)
            {
                transform.position += (Vector3)(delta.normalized *
                    (4f + 8f / Mathf.Max(0.5f, delta.magnitude)) * Time.deltaTime);
            }

            if (delta.sqrMagnitude < 0.24f)
            {
                _director.OnLootCollected(_definition, collector.PlayerIndex);
                _director.ReleaseLootPickup(this);
            }
            else if (_age > 40f)
            {
                _director.ReleaseLootPickup(this);
            }
        }

        public void OnReleasedToPool()
        {
            _director = null;
            _definition = null;
            _age = 0f;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
