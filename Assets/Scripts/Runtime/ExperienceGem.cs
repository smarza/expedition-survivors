using UnityEngine;

namespace ProjectExpedition
{
    public sealed class ExperienceGem : MonoBehaviour, IPoolableComponent
    {
        private GameDirector _director;
        private int _value;
        private float _age;
        private Vector2 _repulsionOrigin;
        private float _repulsionRemaining;
        private float _repulsionStrength;
        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = gameObject.GetComponent<SpriteRenderer>();
            if (_renderer == null)
            {
                _renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            _renderer.sprite = RuntimeAssets.Diamond;
            _renderer.sortingOrder = 4;
        }

        public void Initialize(GameDirector director, int value)
        {
            _director = director;
            _value = value;
            _age = 0f;
            _repulsionOrigin = Vector2.zero;
            _repulsionRemaining = 0f;
            _repulsionStrength = 0f;
            gameObject.name = value >= 10 ? "Jotunn Ember" : "Frozen Echo";
            _renderer.color = value >= 10 ? new Color(1f, 0.72f, 0.18f) : new Color(0.96f, 0.98f, 1f);
            transform.localScale = Vector3.one * (value >= 10 ? 0.27f : 0.18f);
        }

        public void ApplyUltimateRepulsion(Vector2 origin, float strength, float duration)
        {
            _repulsionOrigin = origin;
            _repulsionStrength = strength;
            _repulsionRemaining = Mathf.Max(0f, duration);
        }

        private void Update()
        {
            if (_director == null || _director.State != RunState.Playing)
            {
                return;
            }

            _age += Time.deltaTime;
            transform.Rotate(0f, 0f, 90f * Time.deltaTime);

            if (_repulsionRemaining > 0f)
            {
                _repulsionRemaining -= Time.deltaTime;
                var away = (Vector2)transform.position - _repulsionOrigin;
                if (away.sqrMagnitude > 0.05f)
                {
                    var fade = Mathf.Clamp01(_repulsionRemaining / 0.55f);
                    var distanceFalloff = Mathf.Clamp01(away.magnitude / 6f);
                    transform.position += (Vector3)(away.normalized * _repulsionStrength * fade * distanceFalloff * Time.deltaTime);
                }
            }

            var collector = _director.GetNearestLivingPlayer(transform.position);
            if (collector == null)
            {
                return;
            }

            var delta = (Vector2)collector.transform.position - (Vector2)transform.position;
            var magnet = collector.MagnetRadius;
            if (delta.sqrMagnitude < magnet * magnet)
            {
                transform.position += (Vector3)(delta.normalized * (4.5f + 10f / Mathf.Max(0.5f, delta.magnitude)) * Time.deltaTime);
            }

            if (delta.sqrMagnitude < 0.24f)
            {
                _director.AddExperience(_value);
                _director.Present(PresentationCue.ExperiencePickup, transform.position, _renderer.color,
                    _value >= 10 ? 0.7f : 0.25f);
                _director.ReleaseExperienceGem(this);
            }
            else if (_age > 40f)
            {
                _director.ReleaseExperienceGem(this);
            }
        }

        public void OnReleasedToPool()
        {
            _director = null;
            _value = 0;
            _age = 0f;
            _repulsionOrigin = Vector2.zero;
            _repulsionRemaining = 0f;
            _repulsionStrength = 0f;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
