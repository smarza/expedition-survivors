using NUnit.Framework;
using UnityEngine;

namespace ProjectExpedition.Tests
{
    public sealed class SharedEnemyLevelTests
    {
        [Test]
        public void ComputeEnemyLevel_UsesPlayerLevelPlusConfiguredOffsets()
        {
            Assert.That(BalanceRules.ComputeEnemyLevel(1, false, false), Is.EqualTo(2));
            Assert.That(BalanceRules.ComputeEnemyLevel(5, false, false), Is.EqualTo(6));
            Assert.That(BalanceRules.ComputeEnemyLevel(5, false, true), Is.EqualTo(7));
            Assert.That(BalanceRules.ComputeEnemyLevel(5, true, false), Is.EqualTo(8));
        }

        [Test]
        public void ResolveEffectiveDifficulty_UsesMaxOfTimeAndLevelDifficulty()
        {
            Assert.That(BalanceRules.ResolveEffectiveDifficulty(1.5f, 2), Is.EqualTo(2f).Within(0.0001f));
            Assert.That(BalanceRules.ResolveEffectiveDifficulty(6f, 4), Is.EqualTo(6f).Within(0.0001f));
        }

        [Test]
        public void ExperienceForEnemy_ScalesWithEnemyLevel()
        {
            var lowLevelExperience = BalanceRules.ExperienceForEnemy(3, 2, false, false);
            var highLevelExperience = BalanceRules.ExperienceForEnemy(3, 8, false, false);

            Assert.That(highLevelExperience, Is.GreaterThan(lowLevelExperience));
            Assert.That(BalanceRules.ExperienceForEnemy(3, 2, true, false),
                Is.GreaterThan(lowLevelExperience));
        }

        [Test]
        public void Begin_LocksEnemyLevelAndStatsAfterSpawn()
        {
            var model = new SharedEnemyModel();
            model.Begin(new Vector2(0f, 0f), EnemyCatalog.Draugr, 4f, 3, 1.75f, 0.36f, 4);

            Assert.That(model.EnemyLevel, Is.EqualTo(3));
            Assert.That(model.Health, Is.EqualTo(42f).Within(0.0001f));
            Assert.That(model.ExperienceValue, Is.EqualTo(4));
        }
    }
}
