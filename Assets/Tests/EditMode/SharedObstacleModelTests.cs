using NUnit.Framework;
using UnityEngine;

namespace ProjectExpedition.Tests
{
    public sealed class SharedObstacleModelTests
    {
        [Test]
        public void ResolveCircleMovement_BlocksEntryIntoAxisAlignedBox()
        {
            var obstacles = new[]
            {
                ObstacleDefinition.Box(Vector2.zero, new Vector2(2f, 2f))
            };
            var resolved = SharedMovementCollision.AdvanceCircleTowardsTarget(
                new Vector2(-6f, 0f), 0.4f, new Vector2(6f, 0f), 12f, obstacles);

            Assert.That(Mathf.Abs(resolved.x), Is.LessThan(6f));
            Assert.That(SharedMovementCollision.OverlapsCircle(obstacles[0], resolved, 0.4f), Is.False);
        }

        [Test]
        public void ResolveSpawnPosition_AvoidsObstacleInterior()
        {
            var layout = new SharedObstacleLayoutModel();
            layout.Load(new[]
            {
                ObstacleDefinition.Box(Vector2.zero, new Vector2(3f, 3f))
            });
            var random = new RunRandom(42);

            for (var i = 0; i < 20; i++)
            {
                var position = layout.ResolveSpawnPosition(Vector2.zero, random, 8.5f, 11.8f, 0.36f);
                Assert.That(layout.OverlapsCircle(position, 0.36f), Is.False);
            }
        }

        [Test]
        public void AdvanceTowards_SteersAroundSimpleObstacle()
        {
            var obstacles = new[]
            {
                ObstacleDefinition.Box(new Vector2(2f, 0f), new Vector2(0.5f, 2f))
            };
            var model = new SharedEnemyModel();
            model.Begin(Vector2.zero, EnemyCatalog.Draugr, 1f, 2, 2f, 0.36f, 3);

            model.AdvanceTowards(new Vector2(6f, 0f), BalanceRules.PlayerCollisionRadius, 1f, obstacles);

            Assert.That(SharedMovementCollision.OverlapsCircle(obstacles[0], model.Position, model.Radius),
                Is.False);
            Assert.That(model.Position.x, Is.GreaterThan(0.1f));
        }

        [Test]
        public void AdvanceTowards_ReachesTargetAcrossMultipleStepsWithoutGettingStuck()
        {
            var obstacles = new[]
            {
                ObstacleDefinition.Box(new Vector2(0f, 0f), new Vector2(0.45f, 1.8f))
            };
            var model = new SharedEnemyModel();
            model.Begin(new Vector2(-4f, 0f), EnemyCatalog.Draugr, 1f, 2, 2.5f, 0.36f, 3);

            for (var step = 0; step < 120; step++)
            {
                model.AdvanceTowards(new Vector2(6f, 0f), BalanceRules.PlayerCollisionRadius, 0.05f, obstacles);
            }

            Assert.That(model.Position.x, Is.GreaterThan(2.5f));
            Assert.That(SharedMovementCollision.OverlapsCircle(obstacles[0], model.Position, model.Radius),
                Is.False);
        }

        [Test]
        public void SegmentBlockedByObstacles_StopsProjectilesBeforeObstacleInterior()
        {
            var obstacles = new[]
            {
                ObstacleDefinition.Box(Vector2.zero, new Vector2(0.5f, 2f))
            };
            var blocked = SharedMovementCollision.SegmentBlockedByObstacles(
                new Vector2(-2f, 0f), new Vector2(2f, 0f), 0.2f, obstacles);

            Assert.That(blocked, Is.True);
        }

        [Test]
        public void ProjectileAdvance_StopsWhenSegmentCrossesObstacle()
        {
            var projectile = new SharedProjectileModel();
            projectile.Begin(new Vector2(-2f, 0f), Vector2.right, 10f, 12f, 1, false, false, 2f, 0.2f);
            var obstacles = new[]
            {
                ObstacleDefinition.Box(new Vector2(0f, 0f), new Vector2(0.5f, 2f))
            };

            projectile.Advance(0.25f, obstacles);

            Assert.That(projectile.Active, Is.False);
            Assert.That(projectile.Position.x, Is.LessThan(0f));
        }
    }
}
