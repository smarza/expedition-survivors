using NUnit.Framework;

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
            progress.Begin();

            for (var i = 0; i < 9; i++)
            {
                Assert.That(progress.OnCollected(LootEffectCatalog.HealingEmbers, _ => false),
                    Is.EqualTo(LootCollectResult.Incremented));
            }

            Assert.That(progress.OnCollected(LootEffectCatalog.HealingEmbers, _ => false),
                Is.EqualTo(LootCollectResult.Activated));
            Assert.That(progress.GetCount(LootEffectCatalog.HealingEmbers), Is.Zero);
        }

        [Test]
        public void OnCollected_DiscardsWhileActiveWhenConfigured()
        {
            var discardLoot = new LootEffectDefinition(
                "test.discard_loot",
                "Discard Loot",
                UnityEngine.Color.white,
                0.04f,
                0.005f,
                0.08f,
                10,
                8f,
                8f,
                TemporaryEffectType.Regeneration,
                LootCollectWhileActive.Discard);

            var progress = new SharedLootProgressModel();
            progress.Begin(new[] { discardLoot });

            for (var i = 0; i < 10; i++)
            {
                progress.OnCollected(discardLoot, _ => false);
            }

            Assert.That(progress.OnCollected(discardLoot, id => id == discardLoot.Id),
                Is.EqualTo(LootCollectResult.DiscardedWhileActive));
            Assert.That(progress.GetCount(discardLoot), Is.Zero);
        }

        [Test]
        public void OnCollected_BanksProgressWhileActiveWithoutImmediateActivation()
        {
            var progress = new SharedLootProgressModel();
            progress.Begin();

            for (var i = 0; i < 10; i++)
            {
                progress.OnCollected(LootEffectCatalog.HealingEmbers, _ => false);
            }

            Assert.That(progress.OnCollected(LootEffectCatalog.HealingEmbers,
                    id => id == LootEffectCatalog.HealingEmbers.Id),
                Is.EqualTo(LootCollectResult.Incremented));
            Assert.That(progress.GetCount(LootEffectCatalog.HealingEmbers), Is.EqualTo(10));
        }

        [Test]
        public void TryConsumeBankedActivation_ConsumesFullProgressWhenEffectIsInactive()
        {
            var progress = new SharedLootProgressModel();
            progress.Begin();

            for (var i = 0; i < 10; i++)
            {
                progress.OnCollected(LootEffectCatalog.HealingEmbers,
                    id => id == LootEffectCatalog.HealingEmbers.Id);
            }

            Assert.That(progress.TryConsumeBankedActivation(LootEffectCatalog.HealingEmbers), Is.True);
            Assert.That(progress.GetCount(LootEffectCatalog.HealingEmbers), Is.Zero);
            Assert.That(progress.TryConsumeBankedActivation(LootEffectCatalog.HealingEmbers), Is.False);
        }

        [Test]
        public void OnCollected_TracksIndependentCountersPerColor()
        {
            var progress = new SharedLootProgressModel();
            progress.Begin();

            progress.OnCollected(LootEffectCatalog.HealingEmbers, _ => false);
            progress.OnCollected(LootEffectCatalog.CriticalFlare, _ => false);
            progress.OnCollected(LootEffectCatalog.CriticalFlare, _ => false);

            Assert.That(progress.GetCount(LootEffectCatalog.HealingEmbers), Is.EqualTo(1));
            Assert.That(progress.GetCount(LootEffectCatalog.CriticalFlare), Is.EqualTo(2));
        }

        [Test]
        public void OnCollected_DoesNotBlockDifferentColorWhileAnotherIsActive()
        {
            var progress = new SharedLootProgressModel();
            progress.Begin();

            Assert.That(progress.OnCollected(LootEffectCatalog.CriticalFlare,
                    id => id == LootEffectCatalog.HealingEmbers.Id),
                Is.EqualTo(LootCollectResult.Incremented));
            Assert.That(progress.GetCount(LootEffectCatalog.CriticalFlare), Is.EqualTo(1));
        }

        [Test]
        public void TryRollDrop_UsesDeterministicRandom()
        {
            var progress = new SharedLootProgressModel();
            progress.Begin();
            var random = new RunRandom(991);

            var drops = 0;
            for (var i = 0; i < 500; i++)
            {
                if (progress.TryRollDrop(1, i, 1, random, out _))
                {
                    drops++;
                }
            }

            Assert.That(drops, Is.GreaterThan(0));
            Assert.That(drops, Is.LessThan(500));
        }
    }
}
