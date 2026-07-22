using UnityEngine;

namespace ProjectExpedition
{
    public sealed class ExperienceGem : MonoBehaviour, IPoolableComponent
    {
        private GameDirector _director;
        private int _value;
        private float _age;
        private float _baseScale;
        private bool _burstActive;
        private Vector2 _burstStart;
        private Vector2 _burstEnd;
        private Vector2 _burstArcOffset;
        private float _burstDuration;
        private float _burstElapsed;
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
            _burstActive = false;
            _burstStart = Vector2.zero;
            _burstEnd = Vector2.zero;
            _burstArcOffset = Vector2.zero;
            _burstDuration = 0f;
            _burstElapsed = 0f;
            gameObject.name = value >= 10 ? "Jotunn Ember" : "Frozen Echo";
            _renderer.color = value >= 10 ? new Color(1f, 0.72f, 0.18f) : new Color(0.96f, 0.98f, 1f);
            _baseScale = value >= 10 ? 0.27f : 0.18f;
            transform.localScale = Vector3.one * _baseScale;
        }

        public void ApplyUltimateRepulsion(Vector2 ultimateOrigin, float ultimateRadius, float duration)
        {
            var start = (Vector2)transform.position;
            var offsetFromUltimate = start - ultimateOrigin;
            Vector2 direction;

            if (offsetFromUltimate.sqrMagnitude < 0.04f)
            {
                var hashAngle = Mathf.Repeat(start.x * 12.9898f + start.y * 78.233f, Mathf.PI * 2f);
                direction = new Vector2(Mathf.Cos(hashAngle), Mathf.Sin(hashAngle));
            }
            else
            {
                direction = offsetFromUltimate.normalized;
            }

            var distanceFromCenter = offsetFromUltimate.magnitude;
            var normalizedDistance = Mathf.Clamp01(distanceFromCenter / Mathf.Max(0.5f, ultimateRadius));
            var propagationDistance = Mathf.Lerp(
                ultimateRadius * 0.45f,
                ultimateRadius * 0.95f,
                normalizedDistance);
            propagationDistance = Mathf.Max(propagationDistance, 2.2f);

            _burstStart = start;
            _burstEnd = start + direction * propagationDistance;
            _burstArcOffset = new Vector2(-direction.y, direction.x);
            _burstDuration = Mathf.Max(0.05f, duration);
            _burstElapsed = 0f;
            _burstActive = true;
        }

        private void Update()
        {
            if (_director == null || _director.State != RunState.Playing)
            {
                return;
            }

            _age += Time.deltaTime;

            if (_burstActive)
            {
                AdvanceUltimateBurst();
                return;
            }

            transform.Rotate(0f, 0f, 90f * Time.deltaTime);

            var pickupPosition = (Vector2)transform.position;
            if (_director.TryResolvePickupCollection(
                    pickupPosition,
                    SharedPickupCollectionModel.DefaultCollectionRadiusSqr,
                    out _))
            {
                _director.AddExperience(_value);
                _director.Present(PresentationCue.ExperiencePickup, transform.position, _renderer.color,
                    _value >= 10 ? 0.7f : 0.25f);
                _director.ReleaseExperienceGem(this);
                return;
            }

            if (_director.TryResolvePickupMagnetTarget(pickupPosition, out _, out var magnetDelta))
            {
                transform.position += (Vector3)(magnetDelta.normalized *
                    (4.5f + 10f / Mathf.Max(0.5f, magnetDelta.magnitude)) * Time.deltaTime);
            }

            if (_age > 40f)
            {
                _director.ReleaseExperienceGem(this);
            }
        }

        private void AdvanceUltimateBurst()
        {
            _burstElapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(_burstElapsed / _burstDuration);
            var eased = 1f - Mathf.Pow(1f - progress, 3f);
            var arcLift = Mathf.Sin(progress * Mathf.PI) * 0.55f;
            var burstPosition = Vector2.Lerp(_burstStart, _burstEnd, eased) + _burstArcOffset * arcLift * 0.25f;

            transform.position = burstPosition;
            transform.Rotate(0f, 0f, 180f * Time.deltaTime);

            var scalePulse = 1f + Mathf.Sin(progress * Mathf.PI) * 0.45f;
            transform.localScale = Vector3.one * _baseScale * scalePulse;

            if (progress >= 1f)
            {
                _burstActive = false;
                transform.localScale = Vector3.one * _baseScale;
            }
        }

        public void OnReleasedToPool()
        {
            _director = null;
            _value = 0;
            _age = 0f;
            _baseScale = 1f;
            _burstActive = false;
            _burstStart = Vector2.zero;
            _burstEnd = Vector2.zero;
            _burstArcOffset = Vector2.zero;
            _burstDuration = 0f;
            _burstElapsed = 0f;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
