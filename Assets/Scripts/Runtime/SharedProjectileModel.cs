using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    /// <summary>
    /// Presentation-free projectile flight shared by local GameObjects and the
    /// host-authoritative Online adapter. Damage application and visual effects
    /// remain adapter responsibilities.
    /// </summary>
    public sealed class SharedProjectileModel
    {
        public Vector2 Position { get; private set; }
        public Vector2 Direction { get; private set; }
        public float Damage { get; private set; }
        public float Speed { get; private set; }
        public float Radius { get; private set; }
        public float RemainingLifetime { get; private set; }
        public int RemainingPierce { get; private set; }
        public bool Critical { get; private set; }
        public bool Evolved { get; private set; }
        public bool Active { get; private set; }
        public float TotalLifetime { get; private set; }

        public void Begin(Vector2 position, Vector2 direction, float damage, float speed,
            int pierce, bool critical, bool evolved, float lifetime = 3.2f, float radius = 0f)
        {
            Position = position;
            Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            Damage = Mathf.Max(0f, damage);
            Speed = Mathf.Max(0f, speed);
            Radius = radius > 0f ? radius : critical ? 0.32f : 0.25f;
            TotalLifetime = Mathf.Max(0.01f, lifetime);
            RemainingLifetime = TotalLifetime;
            RemainingPierce = Mathf.Max(0, pierce);
            Critical = critical;
            Evolved = evolved;
            Active = true;
        }

        public void SetDirection(Vector2 direction)
        {
            Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Direction;
        }

        public void Advance(float deltaTime, IReadOnlyList<ObstacleDefinition> obstacles = null)
        {
            if (!Active || deltaTime <= 0f) return;

            var step = Direction * Speed * deltaTime;
            var nextPosition = Position + step;

            if (obstacles != null && obstacles.Count > 0 &&
                SharedMovementCollision.SegmentBlockedByObstacles(Position, nextPosition, Radius, obstacles))
            {
                Stop();
                return;
            }

            Position = nextPosition;
            RemainingLifetime -= deltaTime;
            if (RemainingLifetime <= 0f) Stop();
        }

        public bool Overlaps(Vector2 targetPosition, float targetRadius)
        {
            if (!Active) return false;
            var range = Radius + Mathf.Max(0f, targetRadius);
            return (Position - targetPosition).sqrMagnitude <= range * range;
        }

        public void RegisterHit()
        {
            if (!Active)
            {
                return;
            }

            RemainingPierce--;
            if (RemainingPierce < 0)
            {
                Stop();
            }
        }

        public void ReactivateForReturn(float lifetime = 1.4f)
        {
            Active = true;
            RemainingLifetime = lifetime;
            RemainingPierce = 0;
        }

        public void Stop()
        {
            Active = false;
            RemainingLifetime = 0f;
        }
    }
}
