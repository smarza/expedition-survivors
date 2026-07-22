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
        public const int DefaultMaximumActiveEnemies = 260;
        public const float DefaultMinimumSpawnDistance = 8.5f;
        public const float DefaultMaximumSpawnDistance = 11.8f;
        public const float DefaultInitialSpawnDelay = 0.2f;
        public const float DefaultGroupGrowthSeconds = 35f;
        public const int DefaultMaximumGroupSize = 7;
        public const float DefaultIntervalAccelerationPerSecond = 0.0014f;

        public static int MaximumActiveEnemies => DevelopmentTuningResolver.MaximumActiveEnemies;

        public static float MinimumSpawnDistance => DevelopmentTuningResolver.MinimumSpawnDistance;

        public static float MaximumSpawnDistance => DevelopmentTuningResolver.MaximumSpawnDistance;

        public float SpawnTimer { get; private set; }

        public void Begin() => SpawnTimer = DevelopmentTuningResolver.InitialSpawnDelay;

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
                    1 + Mathf.FloorToInt(safeElapsed / DevelopmentTuningResolver.GroupGrowthSeconds), 1,
                    DevelopmentTuningResolver.MaximumGroupSize), challenge);
                SpawnTimer = SharedChallengeProfileModel.ApplySpawnInterval(Mathf.Max(map.MinimumSpawnInterval,
                    map.BaseSpawnInterval - safeElapsed * DevelopmentTuningResolver.IntervalAccelerationPerSecond),
                    challenge);
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
