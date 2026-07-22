using System.Collections.Generic;
using NUnit.Framework;

namespace ProjectExpedition.Tests
{
    public sealed class SharedTemporaryEffectTests
    {
        [Test]
        public void Activate_AllowsMultipleDifferentEffectsSimultaneously()
        {
            var model = new SharedTemporaryEffectModel();
            var players = new List<PlayerController>();

            model.Activate(LootEffectCatalog.HealingEmbers, 0, players);
            model.Activate(LootEffectCatalog.CriticalFlare, 0, players);

            Assert.That(model.HasActiveEffect, Is.True);
            Assert.That(model.ActiveEffects.Count, Is.EqualTo(2));
            Assert.That(model.IsActive(LootEffectCatalog.HealingEmbers.Id), Is.True);
            Assert.That(model.IsActive(LootEffectCatalog.CriticalFlare.Id), Is.True);
        }

        [Test]
        public void Advance_ExpiresEffectsIndependently()
        {
            var model = new SharedTemporaryEffectModel();
            var players = new List<PlayerController>();

            model.Activate(LootEffectCatalog.AegisVeil, 0, players);
            model.Activate(LootEffectCatalog.WrathEmbers, 0, players);

            model.Advance(7f, players);

            Assert.That(model.IsActive(LootEffectCatalog.AegisVeil.Id), Is.False);
            Assert.That(model.IsActive(LootEffectCatalog.WrathEmbers.Id), Is.True);
            Assert.That(model.ActiveEffects.Count, Is.EqualTo(1));
        }

        [Test]
        public void IsActive_ReturnsTrueOnlyForMatchingDefinition()
        {
            var model = new SharedTemporaryEffectModel();
            var players = new List<PlayerController>();

            model.Activate(LootEffectCatalog.SwiftTrail, 0, players);

            Assert.That(model.IsActive(LootEffectCatalog.SwiftTrail.Id), Is.True);
            Assert.That(model.IsActive(LootEffectCatalog.HealingEmbers.Id), Is.False);
        }
    }
}
