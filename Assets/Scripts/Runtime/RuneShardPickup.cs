using UnityEngine;

namespace ProjectExpedition
{
    public sealed class RuneShardPickup : MonoBehaviour, IPoolableComponent
    {
        private GameDirector _director;
        private float _age;
        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = gameObject.GetComponent<SpriteRenderer>();
            if (_renderer == null)
                _renderer = gameObject.AddComponent<SpriteRenderer>();

            _renderer.sprite = RuntimeAssets.Diamond;
            _renderer.sortingOrder = 6;
        }

        public void Initialize(GameDirector director)
        {
            _director = director;
            _age = 0f;
            gameObject.name = "Rune Shard";
            _renderer.color = new Color(0.42f, 0.78f, 0.96f);
            transform.localScale = Vector3.one * 0.24f;
        }

        private void Update()
        {
            if (_director == null || _director.State != RunState.Playing)
                return;

            var collector = _director.GetNearestLivingPlayer(transform.position);
            if (collector == null)
                return;

            _age += Time.deltaTime;
            transform.Rotate(0f, 0f, 120f * Time.deltaTime);
            var delta = (Vector2)collector.transform.position - (Vector2)transform.position;
            var magnet = collector.MagnetRadius;
            if (delta.sqrMagnitude < magnet * magnet)
            {
                transform.position += (Vector3)(delta.normalized *
                    (4.5f + 10f / Mathf.Max(0.5f, delta.magnitude)) * Time.deltaTime);
            }

            if (delta.sqrMagnitude < 0.24f)
            {
                _director.Present(PresentationCue.ExperiencePickup, transform.position,
                    _renderer.color, 0.45f);
                _director.OnRuneShardCollected(this);
            }
            else if (_age > 120f)
            {
                _director.ReleaseRuneShard(this);
            }
        }

        public void OnReleasedToPool()
        {
            _director = null;
            _age = 0f;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
