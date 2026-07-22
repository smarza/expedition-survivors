using UnityEngine;

namespace ProjectExpedition
{
    public sealed class RuneShardPickup : MonoBehaviour, IPoolableComponent
    {
        private static readonly Color ShardColor = new Color(0.96f, 0.78f, 0.22f);

        private GameDirector _director;
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

            _renderer.sprite = RuntimeAssets.Square;
            _renderer.sortingOrder = 7;
        }

        public void Initialize(GameDirector director)
        {
            _director = director;
            _age = 0f;
            _collected = false;
            gameObject.name = "Rune Shard";
            _renderer.color = ShardColor;
            transform.localScale = Vector3.one * 0.28f;
        }

        private void Update()
        {
            if (_director == null || _director.State != RunState.Playing || _collected)
            {
                return;
            }

            _age += Time.deltaTime;
            transform.Rotate(0f, 0f, 45f * Time.deltaTime);
            var bob = 1f + Mathf.Sin(Time.time * 5f) * 0.06f;
            transform.localScale = Vector3.one * 0.28f * bob;

            var pickupPosition = (Vector2)transform.position;
            if (_director.TryResolvePickupCollection(
                    pickupPosition,
                    SharedPickupCollectionModel.DefaultCollectionRadiusSqr,
                    out var collector))
            {
                _collected = true;
                _director.OnRuneShardCollected(this, collector.PlayerIndex);
                return;
            }

            if (_director.TryResolvePickupMagnetTarget(pickupPosition, out _, out var magnetDelta))
            {
                transform.position += (Vector3)(magnetDelta.normalized *
                    (4.5f + 10f / Mathf.Max(0.5f, magnetDelta.magnitude)) * Time.deltaTime);
            }

            if (_age > 120f)
            {
                _director.ReleaseRuneShard(this);
            }
        }

        public void OnReleasedToPool()
        {
            _director = null;
            _age = 0f;
            _collected = false;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
