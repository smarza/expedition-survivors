using NUnit.Framework;

namespace ProjectExpedition.Tests
{
    public sealed class ContentAuthoringTests
    {
        [SetUp]
        public void SetUp()
        {
            ProductionContentRuntime.Load();
        }

        [Test]
        public void ProductionContentRuntime_ValidatePassesAfterLoad()
        {
            Assert.That(ProductionContentRuntime.Validate(out var report), Is.True, report);
            Assert.That(report, Does.Contain("6 characters"));
            Assert.That(report, Does.Contain("6 maps"));
            Assert.That(report, Does.Contain("9 enemies"));
        }

        [Test]
        public void ContentCatalog_ContainsSixHeroesAndSixMaps()
        {
            Assert.That(ContentCatalog.Characters.Length, Is.EqualTo(6));
            Assert.That(ContentCatalog.Maps.Length, Is.EqualTo(6));
            Assert.That(ContentCatalog.FindCharacter("oathbound.bren"), Is.Not.Null);
            Assert.That(ContentCatalog.FindCharacter("ironway.rex"), Is.Not.Null);
            Assert.That(ContentCatalog.FindMap("oathbound.scout"), Is.Not.Null);
            Assert.That(ContentCatalog.FindMap("ironway.saga"), Is.Not.Null);
        }

        [Test]
        public void LootEffectCatalog_DefaultHealingEmbers_IsConfiguredForPartyActivation()
        {
            var loot = LootEffectCatalog.HealingEmbers;

            Assert.That(loot.RequiredCount, Is.EqualTo(10));
            Assert.That(loot.EffectType, Is.EqualTo(TemporaryEffectType.Regeneration));
            Assert.That(loot.EffectTarget, Is.EqualTo(TemporaryEffectTarget.WholeParty));
            Assert.That(loot.EvaluateDropChance(1, 0, 1), Is.GreaterThan(loot.MinimumDropChance));
        }

        [Test]
        public void ItemCatalog_ContainsTwelveEvolutionsAndThirtySlotItems()
        {
            var evolutionCount = 0;
            var slotItemCount = 0;

            for (var i = 0; i < ItemCatalog.All.Length; i++)
            {
                var item = ItemCatalog.All[i];

                if (item.IsEvolution)
                {
                    evolutionCount++;
                    continue;
                }

                if (item.Category == ItemCategory.Weapon || item.Category == ItemCategory.Gear)
                {
                    slotItemCount++;
                }
            }

            Assert.That(evolutionCount, Is.EqualTo(12));
            Assert.That(slotItemCount, Is.EqualTo(30));
            Assert.That(ItemCatalog.Find("weapon.root_lance"), Is.Not.Null);
            Assert.That(ItemCatalog.Find("gear.sap_vial"), Is.Not.Null);
            Assert.That(ItemCatalog.Find("gear.siege_plating"), Is.Not.Null);
        }

        [Test]
        public void EnemyCatalog_ContainsNineBiomeEnemies()
        {
            Assert.That(EnemyCatalog.All.Length, Is.EqualTo(9));
            Assert.That(EnemyCatalog.FindById("enemy.bramble_stalker").Id, Is.EqualTo("enemy.bramble_stalker"));
            Assert.That(EnemyCatalog.FindById("enemy.siege_automaton").Boss, Is.True);
        }

        [Test]
        public void EvolutionRecipes_ReferenceExistingBaseAndCatalyst()
        {
            for (var i = 0; i < ItemCatalog.All.Length; i++)
            {
                var item = ItemCatalog.All[i];

                if (!item.IsEvolution)
                {
                    continue;
                }

                Assert.That(ItemCatalog.Find(item.EvolutionOf), Is.Not.Null, item.Id);
                Assert.That(ItemCatalog.Find(item.CatalystId), Is.Not.Null, item.Id);
            }
        }
    }
}
