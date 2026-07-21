using UnityEngine;

namespace ProjectExpedition
{
    public sealed class TemporaryEffectGlowPresentation : MonoBehaviour
    {
        private SpriteRenderer _glowRenderer;
        private Transform _glowTransform;
        private Color _themeColor = Color.clear;
        private bool _active;

        public void Initialize(SpriteRenderer bodyRenderer)
        {
            var glowObject = new GameObject("Temporary Effect Glow");
            glowObject.transform.SetParent(transform, false);
            _glowTransform = glowObject.transform;
            _glowTransform.localScale = Vector3.one * 1.35f;
            _glowRenderer = glowObject.AddComponent<SpriteRenderer>();
            _glowRenderer.sprite = RuntimeAssets.Circle;
            _glowRenderer.sortingOrder = bodyRenderer != null ? bodyRenderer.sortingOrder - 1 : 9;
            _glowRenderer.enabled = false;
        }

        public void SetEffect(Color themeColor, bool active)
        {
            _themeColor = themeColor;
            _active = active && PresentationPreferences.Data.ShowTemporaryEffectGlow;

            if (_glowRenderer == null)
            {
                return;
            }

            _glowRenderer.enabled = _active;
        }

        private void Update()
        {
            if (!_active || _glowRenderer == null)
            {
                return;
            }

            var pulse = 0.22f + Mathf.Abs(Mathf.Sin(Time.time * 4.2f)) * 0.18f;
            if (PresentationPreferences.Data.ReducedFlashes)
            {
                pulse *= 0.65f;
            }

            _glowRenderer.color = new Color(_themeColor.r, _themeColor.g, _themeColor.b, pulse);

            if (_glowTransform != null)
            {
                _glowTransform.localScale = Vector3.one * (1.35f + pulse * 0.25f);
            }
        }
    }
}
