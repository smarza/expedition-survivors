using System;
using UnityEngine;

namespace ProjectExpedition
{
    public readonly struct SpawnAdvanceResult
    {
        public readonly int RegularEnemyCount;
        public readonly bool SpawnBoss;
        public readonly float Difficulty;

        public SpawnAdvanceResult(int regularEnemyCount, bool spawnBoss, float difficulty)
        {
            RegularEnemyCount = regularEnemyCount;
            SpawnBoss = spawnBoss;
            Difficulty = difficulty;
        }
    }

    /// <summary>
    /// Presentation-free wave scheduler shared by every local simulation adapter.
    /// Random rolls are supplied by the caller so the run owns one explicit seed.
    /// </summary>
    public sealed class SharedSpawnModel
    {
        public const int MaximumActiveEnemies = 260;
        public const float MinimumSpawnDistance = 8.5f;
        public const float MaximumSpawnDistance = 11.8f;

        private const float InitialSpawnDelay = 0.2f;
        private const float GroupGrowthSeconds = 35f;
        private const int MaximumGroupSize = 7;
        private const float IntervalAccelerationPerSecond = 0.0014f;

        public float SpawnTimer { get; private set; }

        public void Begin() => SpawnTimer = InitialSpawnDelay;

        public SpawnAdvanceResult Advance(float deltaTime, float elapsed, MapDefinition map,
            int activeEnemyCount, bool bossRequested, ChallengeProfile challenge = default)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));

            SpawnTimer -= Mathf.Max(0f, deltaTime);
            var safeElapsed = Mathf.Max(0f, elapsed);
            var difficulty = 1f + safeElapsed / Mathf.Max(0.001f, map.DifficultyRamp);
            var regularEnemyCount = 0;

            if (SpawnTimer <= 0f && activeEnemyCount < MaximumActiveEnemies)
            {
                regularEnemyCount = SharedChallengeProfileModel.ApplyGroupSize(Mathf.Clamp(
                    1 + Mathf.FloorToInt(safeElapsed / GroupGrowthSeconds), 1, MaximumGroupSize), challenge);
                SpawnTimer = SharedChallengeProfileModel.ApplySpawnInterval(Mathf.Max(map.MinimumSpawnInterval,
                    map.BaseSpawnInterval - safeElapsed * IntervalAccelerationPerSecond), challenge);
            }

            return new SpawnAdvanceResult(regularEnemyCount, bossRequested, difficulty);
        }

        public static Vector2 CalculateSpawnPosition(Vector2 groupCenter, float angleRadians,
            float distance)
        {
            var direction = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
            return groupCenter + direction * Mathf.Clamp(distance, MinimumSpawnDistance,
                MaximumSpawnDistance);
        }
    }
}
