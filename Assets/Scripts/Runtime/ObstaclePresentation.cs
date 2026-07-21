using UnityEngine;

namespace ProjectExpedition
{
    public static class ObstaclePresentation
    {
        private static readonly Color BlockFill = new Color(0.48f, 0.34f, 0.24f, 0.98f);
        private static readonly Color BlockOutline = new Color(0.96f, 0.74f, 0.28f, 0.95f);
        private static readonly Color BoulderFill = new Color(0.42f, 0.4f, 0.38f, 0.98f);
        private static readonly Color BoulderOutline = new Color(0.88f, 0.62f, 0.22f, 0.95f);

        public static void Attach(GameObject root, ObstacleDefinition obstacle)
        {
            if (root == null)
            {
                return;
            }

            if (obstacle.Shape == ObstacleShapeKind.Circle)
            {
                AttachCircleObstacle(root, obstacle.Radius);
                return;
            }

            AttachBoxObstacle(root, obstacle.HalfExtents);
        }

        private static void AttachCircleObstacle(GameObject root, float radius)
        {
            var outlineScale = Vector3.one * (radius * 2.25f);
            CreateSpriteChild(root, "Obstacle Outline", RuntimeAssets.Circle, outlineScale, BoulderOutline, 0);
            CreateSpriteChild(root, "Obstacle Body", RuntimeAssets.Circle, Vector3.one * (radius * 2f),
                BoulderFill, 1);
        }

        private static void AttachBoxObstacle(GameObject root, Vector2 halfExtents)
        {
            var outlineScale = new Vector3(halfExtents.x * 2.18f, halfExtents.y * 2.18f, 1f);
            CreateSpriteChild(root, "Obstacle Outline", RuntimeAssets.Square, outlineScale, BlockOutline, 0);
            CreateSpriteChild(root, "Obstacle Body", RuntimeAssets.Square,
                new Vector3(halfExtents.x * 2f, halfExtents.y * 2f, 1f), BlockFill, 1);
        }

        private static void CreateSpriteChild(GameObject parent, string childName, Sprite sprite,
            Vector3 localScale, Color color, int sortingOrder)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(parent.transform, false);
            child.transform.localScale = localScale;
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }
    }
}
