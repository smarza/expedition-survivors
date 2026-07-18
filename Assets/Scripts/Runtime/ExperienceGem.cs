using UnityEngine;

namespace ProjectExpedition
{
    public sealed class ExperienceGem : MonoBehaviour, IPoolableComponent
    {
        private GameDirector _director;
        private int _value;
        private float _age;
        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = gameObject.GetComponent<SpriteRenderer>();
            if (_renderer == null) _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = RuntimeAssets.Diamond;
            _renderer.sortingOrder = 4;
        }

        public void Initialize(GameDirector director, int value)
        {
            _director = director;
            _value = value;
            _age = 0f;
            gameObject.name = value >= 10 ? "Jotunn Ember" : "Frozen Echo";
            _renderer.color = value >= 10 ? new Color(1f, 0.72f, 0.18f) : new Color(0.28f, 0.92f, 0.96f);
            transform.localScale = Vector3.one * (value >= 10 ? 0.27f : 0.18f);
        }

        private void Update()
        {
            if (_director == null || _director.State != RunState.Playing) return;
            var collector = _director.GetNearestLivingPlayer(transform.position);
            if (collector == null) return;
            _age += Time.deltaTime;
            transform.Rotate(0f, 0f, 90f * Time.deltaTime);
            var delta = (Vector2)collector.transform.position - (Vector2)transform.position;
            var magnet = collector.MagnetRadius;
            if (delta.sqrMagnitude < magnet * magnet)
                transform.position += (Vector3)(delta.normalized * (4.5f + 10f / Mathf.Max(0.5f, delta.magnitude)) * Time.deltaTime);
            if (delta.sqrMagnitude < 0.24f)
            {
                _director.AddExperience(_value);
                _director.ReleaseExperienceGem(this);
            }
            else if (_age > 40f) _director.ReleaseExperienceGem(this);
        }

        public void OnReleasedToPool()
        {
            _director = null;
            _value = 0;
            _age = 0f;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
