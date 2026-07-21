using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public enum ObstacleShapeKind : byte
    {
        Circle,
        AxisAlignedBox
    }

    public readonly struct ObstacleDefinition
    {
        public readonly ObstacleShapeKind Shape;
        public readonly Vector2 Center;
        public readonly Vector2 HalfExtents;
        public readonly float Radius;

        public ObstacleDefinition(ObstacleShapeKind shape, Vector2 center, Vector2 halfExtents, float radius)
        {
            Shape = shape;
            Center = center;
            HalfExtents = halfExtents;
            Radius = radius;
        }

        public static ObstacleDefinition Circle(Vector2 center, float radius)
        {
            return new ObstacleDefinition(ObstacleShapeKind.Circle, center, Vector2.zero, radius);
        }

        public static ObstacleDefinition Box(Vector2 center, Vector2 halfExtents)
        {
            return new ObstacleDefinition(ObstacleShapeKind.AxisAlignedBox, center, halfExtents, 0f);
        }
    }

    public sealed class SharedObstacleLayoutModel
    {
        private readonly List<ObstacleDefinition> _obstacles = new List<ObstacleDefinition>(16);

        public IReadOnlyList<ObstacleDefinition> Obstacles => _obstacles;

        public void Load(IReadOnlyList<ObstacleDefinition> obstacles)
        {
            _obstacles.Clear();
            if (obstacles == null)
            {
                return;
            }

            for (var i = 0; i < obstacles.Count; i++)
            {
                _obstacles.Add(obstacles[i]);
            }
        }

        public void Clear() => _obstacles.Clear();

        public bool OverlapsCircle(Vector2 position, float radius, float padding = 0f)
        {
            var expandedRadius = radius + padding;

            for (var i = 0; i < _obstacles.Count; i++)
            {
                if (SharedMovementCollision.OverlapsCircle(_obstacles[i], position, expandedRadius))
                {
                    return true;
                }
            }

            return false;
        }

        public Vector2 ResolveSpawnPosition(Vector2 groupCenter, RunRandom random, float minimumDistance,
            float maximumDistance, float entityRadius, int maximumAttempts = 12)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            for (var attempt = 0; attempt < maximumAttempts; attempt++)
            {
                var angle = random.Range(0f, Mathf.PI * 2f);
                var distance = random.Range(minimumDistance, maximumDistance);
                var candidate = SharedSpawnModel.CalculateSpawnPosition(groupCenter, angle, distance);

                if (!OverlapsCircle(candidate, entityRadius))
                {
                    return candidate;
                }
            }

            return SharedSpawnModel.CalculateSpawnPosition(groupCenter, random.Range(0f, Mathf.PI * 2f),
                minimumDistance);
        }
    }

    public static class ObstacleLayoutCatalog
    {
        private static readonly ObstacleDefinition[] SharedTestLayout =
        {
            ObstacleDefinition.Box(new Vector2(-5.5f, 0f), new Vector2(0.45f, 1.8f)),
            ObstacleDefinition.Box(new Vector2(5.5f, 0f), new Vector2(0.45f, 1.8f)),
            ObstacleDefinition.Box(new Vector2(0f, 4.5f), new Vector2(2f, 0.45f)),
            ObstacleDefinition.Box(new Vector2(0f, -4.5f), new Vector2(2f, 0.45f)),
            ObstacleDefinition.Circle(new Vector2(-2.8f, 2.8f), 0.65f),
            ObstacleDefinition.Circle(new Vector2(2.8f, -2.8f), 0.65f)
        };

        public static IReadOnlyList<ObstacleDefinition> ForMap(MapDefinition map)
        {
            if (map == null)
            {
                return SharedTestLayout;
            }

            return SharedTestLayout;
        }
    }
}
