using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    [Flags]
    public enum EnemyAdvanceResult : byte
    {
        None = 0,
        Moved = 1,
        ContactTriggered = 2
    }

    [Flags]
    public enum EnemyDamageResult : byte
    {
        Ignored = 0,
        Damaged = 1,
        Moved = 2,
        Killed = 4
    }

    /// <summary>
    /// Presentation-free enemy state shared by every simulation adapter. Spawn
    /// randomness is supplied explicitly so seeded random ownership remains
    /// outside the model while all derived statistics and combat rules stay here.
    /// </summary>
    public sealed class SharedEnemyModel
    {
        private const float MinimumMovementDistanceSquared = 0.001f;
        private const float ContactInterval = 0.7f;

        public Vector2 Position { get; private set; }
        public float Health { get; private set; }
        public float Speed { get; private set; }
        public float ContactDamage { get; private set; }
        public float ContactCooldownRemaining { get; private set; }
        public float Radius { get; private set; }
        public int ExperienceValue { get; private set; }
        public int EnemyLevel { get; private set; }
        public bool Boss { get; private set; }
        public bool Alive { get; private set; }

        public void Begin(Vector2 position, EnemyDefinition definition, float difficulty,
            int enemyLevel, float rolledBaseSpeed, float rolledRadius, int rolledExperience,
            ChallengeProfile challenge = default)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            Position = position;
            EnemyLevel = Mathf.Max(1, enemyLevel);
            Health = SharedChallengeProfileModel.ApplyEnemyHealthMultiplier(
                definition.BaseHealth + difficulty * definition.HealthPerDifficulty, challenge);
            Speed = rolledBaseSpeed + difficulty * definition.SpeedPerDifficulty;
            ContactDamage = definition.BaseContactDamage +
                difficulty * definition.ContactDamagePerDifficulty;
            Radius = rolledRadius;
            ExperienceValue = rolledExperience;
            Boss = definition.Boss;
            ContactCooldownRemaining = 0f;
            Alive = true;
        }

        public EnemyAdvanceResult AdvanceTowards(Vector2 targetPosition, float targetRadius,
            float deltaTime, IReadOnlyList<ObstacleDefinition> obstacles = null, float speedMultiplier = 1f)
        {
            if (!Alive || deltaTime <= 0f) return EnemyAdvanceResult.None;

            var result = EnemyAdvanceResult.None;
            var delta = targetPosition - Position;
            if (delta.sqrMagnitude > MinimumMovementDistanceSquared)
            {
                var maxStep = Speed * deltaTime * Mathf.Max(0f, speedMultiplier);
                Position = SharedMovementCollision.AdvanceCircleTowardsTarget(
                    Position, Radius, targetPosition, maxStep, obstacles);
                result |= EnemyAdvanceResult.Moved;
            }

            ContactCooldownRemaining -= deltaTime;
            var hitRange = Radius + targetRadius;
            if (delta.sqrMagnitude <= hitRange * hitRange && ContactCooldownRemaining <= 0f)
            {
                ContactCooldownRemaining = ContactInterval;
                result |= EnemyAdvanceResult.ContactTriggered;
            }
            return result;
        }

        public EnemyDamageResult TakeDamage(float amount, float knockback, Vector2 source,
            IReadOnlyList<ObstacleDefinition> obstacles = null)
        {
            if (!Alive) return EnemyDamageResult.Ignored;

            var result = EnemyDamageResult.Damaged;
            Health -= amount;
            if (knockback > 0f)
            {
                var away = Position - source;
                if (away.sqrMagnitude > MinimumMovementDistanceSquared)
                {
                    var knockbackDelta = away.normalized * knockback;
                    Position = SharedMovementCollision.ResolveCircleKnockback(Position, Radius,
                        knockbackDelta, obstacles);
                    result |= EnemyDamageResult.Moved;
                }
            }

            if (Health > 0f) return result;
            Health = 0f;
            Alive = false;
            return result | EnemyDamageResult.Killed;
        }

        public void Stop()
        {
            Alive = false;
            Health = 0f;
            Speed = 0f;
            ContactDamage = 0f;
            ContactCooldownRemaining = 0f;
            Radius = 0f;
            ExperienceValue = 0;
            EnemyLevel = 0;
            Boss = false;
        }
    }
}
