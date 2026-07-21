using NUnit.Framework;
using UnityEngine;

namespace ProjectExpedition.Tests
{
    public sealed class SharedEnemyModelTests
    {
        [Test]
        public void Begin_DerivesStatisticsFromDefinitionDifficultyAndExplicitRolls()
        {
            var model = StartedDraugr(2f);

            Assert.That(model.Position, Is.EqualTo(new Vector2(1f, 2f)));
            Assert.That(model.Health, Is.EqualTo(31f));
            Assert.That(model.Speed, Is.EqualTo(1.8f).Within(0.0001f));
            Assert.That(model.ContactDamage, Is.EqualTo(10.2f).Within(0.0001f));
            Assert.That(model.Radius, Is.EqualTo(0.36f));
            Assert.That(model.ExperienceValue, Is.EqualTo(3));
            Assert.That(model.EnemyLevel, Is.EqualTo(2));
            Assert.That(model.Boss, Is.False);
            Assert.That(model.Alive, Is.True);
        }

        [Test]
        public void AdvanceTowards_MovesAndAppliesTheSharedContactInterval()
        {
            var model = StartedDraugr(0f, Vector2.zero, 2f, 0.4f);

            var result = model.AdvanceTowards(new Vector2(4f, 0f), 0.38f, 1f);
            Assert.That(result, Is.EqualTo(EnemyAdvanceResult.Moved));
            Assert.That(model.Position.x, Is.EqualTo(2f).Within(0.0001f));

            result = model.AdvanceTowards(new Vector2(2.2f, 0f), 0.38f, 0.1f);
            Assert.That((result & EnemyAdvanceResult.Moved) != 0, Is.True);
            Assert.That((result & EnemyAdvanceResult.ContactTriggered) != 0, Is.True);
            Assert.That(model.ContactCooldownRemaining, Is.EqualTo(0.7f));

            result = model.AdvanceTowards(new Vector2(2.2f, 0f), 0.38f, 0.6f);
            Assert.That((result & EnemyAdvanceResult.ContactTriggered) != 0, Is.False);
            result = model.AdvanceTowards(new Vector2(2.2f, 0f), 0.38f, 0.11f);
            Assert.That((result & EnemyAdvanceResult.ContactTriggered) != 0, Is.True);
        }

        [Test]
        public void TakeDamage_AppliesKnockbackAndReportsDeathExactlyOnce()
        {
            var model = StartedDraugr(0f, Vector2.right, 1.5f, 0.35f);

            var result = model.TakeDamage(5f, 2f, Vector2.zero);
            Assert.That((result & EnemyDamageResult.Damaged) != 0, Is.True);
            Assert.That((result & EnemyDamageResult.Moved) != 0, Is.True);
            Assert.That((result & EnemyDamageResult.Killed) != 0, Is.False);
            Assert.That(model.Health, Is.EqualTo(15f));
            Assert.That(model.Position, Is.EqualTo(new Vector2(3f, 0f)));

            result = model.TakeDamage(100f, 0f, Vector2.zero);
            Assert.That((result & EnemyDamageResult.Killed) != 0, Is.True);
            Assert.That(model.Health, Is.Zero);
            Assert.That(model.Alive, Is.False);
            Assert.That(model.TakeDamage(1f, 1f, Vector2.zero), Is.EqualTo(EnemyDamageResult.Ignored));
        }

        [Test]
        public void Boss_UsesTheSameDifficultyScalingAndStateBoundary()
        {
            var model = new SharedEnemyModel();
            model.Begin(new Vector2(-2f, 4f), EnemyCatalog.Jotunn, 3f, 6, 1.35f, 0.85f, 60);

            Assert.That(model.Boss, Is.True);
            Assert.That(model.Health, Is.EqualTo(935f));
            Assert.That(model.Speed, Is.EqualTo(1.41f));
            Assert.That(model.ContactDamage, Is.EqualTo(27.05f));
            Assert.That(model.ExperienceValue, Is.EqualTo(60));

            model.Stop();
            Assert.That(model.Alive, Is.False);
            Assert.That(model.AdvanceTowards(Vector2.zero, 0.38f, 1f), Is.EqualTo(EnemyAdvanceResult.None));
            Assert.That(model.TakeDamage(1f, 0f, Vector2.zero), Is.EqualTo(EnemyDamageResult.Ignored));
        }

        [Test]
        public void SameCommands_ProduceIdenticalEnemyStateForEveryAdapter()
        {
            var first = StartedDraugr(4f);
            var second = StartedDraugr(4f);

            first.AdvanceTowards(new Vector2(6f, -2f), 0.38f, 0.16f);
            second.AdvanceTowards(new Vector2(6f, -2f), 0.38f, 0.16f);
            first.TakeDamage(7f, 0.4f, new Vector2(-3f, 1f));
            second.TakeDamage(7f, 0.4f, new Vector2(-3f, 1f));

            Assert.That(first.Position, Is.EqualTo(second.Position));
            Assert.That(first.Health, Is.EqualTo(second.Health));
            Assert.That(first.ContactCooldownRemaining, Is.EqualTo(second.ContactCooldownRemaining));
            Assert.That(first.Alive, Is.EqualTo(second.Alive));
        }

        private static SharedEnemyModel StartedDraugr(float difficulty,
            Vector2? position = null, float rolledSpeed = 1.75f, float rolledRadius = 0.36f)
        {
            var model = new SharedEnemyModel();
            model.Begin(position ?? new Vector2(1f, 2f), EnemyCatalog.Draugr, difficulty, 2,
                rolledSpeed, rolledRadius, 3);
            return model;
        }
    }
}
