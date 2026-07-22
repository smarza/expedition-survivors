using UnityEngine;

namespace ProjectExpedition
{
    /// <summary>
    /// Temporary world-space level marker for geometric enemy placeholders.
    /// </summary>
    public sealed class EnemyLevelLabelPresentation : MonoBehaviour
    {
        private TextMesh _textMesh;
        private MeshRenderer _meshRenderer;

        public void Initialize(float enemyRadius)
        {
            if (_textMesh == null)
            {
                _textMesh = gameObject.AddComponent<TextMesh>();
                _textMesh.anchor = TextAnchor.MiddleCenter;
                _textMesh.alignment = TextAlignment.Center;
                _textMesh.fontSize = 48;
                _textMesh.fontStyle = FontStyle.Bold;
                _meshRenderer = gameObject.GetComponent<MeshRenderer>();
            }

            ApplyLayout(enemyRadius);
        }

        public void SetLevel(int enemyLevel, int playerLevel, bool visible)
        {
            if (_textMesh == null)
            {
                return;
            }

            gameObject.SetActive(visible && PresentationPreferences.Data.ShowEnemyLevelLabels);
            _textMesh.text = enemyLevel.ToString();
            _textMesh.color = ResolveLabelColor(enemyLevel, playerLevel);
        }

        public void RefreshForEnemyRadius(float enemyRadius)
        {
            ApplyLayout(enemyRadius);
        }

        private void ApplyLayout(float enemyRadius)
        {
            transform.localPosition = Vector3.zero;
            _textMesh.characterSize = Mathf.Clamp(enemyRadius * 0.17f, 0.065f, 0.15f);

            if (_meshRenderer != null)
            {
                _meshRenderer.sortingOrder = 12;
            }
        }

        private static Color ResolveLabelColor(int enemyLevel, int playerLevel)
        {
            if (enemyLevel < playerLevel)
            {
                return new Color(0.72f, 0.78f, 0.84f, 0.82f);
            }

            if (enemyLevel == playerLevel)
            {
                return new Color(0.94f, 0.94f, 0.94f, 0.95f);
            }

            return new Color(1f, 0.58f, 0.42f, 0.98f);
        }
    }
}
