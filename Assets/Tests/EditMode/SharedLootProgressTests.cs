using NUnit.Framework;
using UnityEngine;

namespace ProjectExpedition.Tests
{
    public sealed class SharedLootProgressTests
    {
        [Test]
        public void EvaluateDropChance_ReducesWithPlayerLevelButRespectsFloor()
        {
            var definition = LootEffectCatalog.HealingEmbers;
            var lowLevelChance = definition.EvaluateDropChance(1, 0, 1);
            var highLevelChance = definition.EvaluateDropChance(12, 200, 1);

            Assert.That(highLevelChance, Is.LessThan(lowLevelChance));
            Assert.That(highLevelChance, Is.GreaterThanOrEqualTo(definition.MinimumDropChance));
        }

        [Test]
        public void OnCollected_ActivatesAfterRequiredCount()
        {
            var progress = new SharedLootProgressModel();
            progress.Begin(LootEffectCatalog.HealingEmbers);

            for (var i = 0; i < 9; i++)
            {
                Assert.That(progress.OnCollected(false), Is.EqualTo(LootCollectResult.Incremented));
            }

            Assert.That(progress.OnCollected(false), Is.EqualTo(LootCollectResult.Activated));
            Assert.That(progress.CurrentCount, Is.Zero);
        }

        [Test]
        public void OnCollected_DiscardsWhileActiveWhenConfigured()
        {
            var progress = new SharedLootProgressModel();
            progress.Begin(LootEffectCatalog.HealingEmbers);

            for (var i = 0; i < 10; i++)
            {
                progress.OnCollected(false);
            }

            Assert.That(progress.OnCollected(true), Is.EqualTo(LootCollectResult.DiscardedWhileActive));
            Assert.That(progress.CurrentCount, Is.Zero);
        }

        [Test]
        public void TryRollDrop_UsesDeterministicRandom()
        {
            var progress = new SharedLootProgressModel();
            progress.Begin(LootEffectCatalog.HealingEmbers);
            var random = new RunRandom(991);

            var drops = 0;
            for (var i = 0; i < 500; i++)
            {
                if (progress.TryRollDrop(1, i, 1, random))
                {
                    drops++;
                }
            }

            Assert.That(drops, Is.GreaterThan(0));
            Assert.That(drops, Is.LessThan(500));
        }
    }
}
