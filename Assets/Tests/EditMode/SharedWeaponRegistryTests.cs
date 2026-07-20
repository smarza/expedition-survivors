using NUnit.Framework;
using UnityEngine;

namespace ProjectExpedition.Tests
{
    public sealed class SharedWeaponRegistryTests
    {
        [Test]
        public void FrostAxeLevelTable_MatchesLegacySharedWeaponModelParity()
        {
            var registry = new SharedWeaponRegistry();
            registry.Begin();

            for (var level = 2; level <= ItemCatalog.FrostAxe.MaxLevel; level++)
                Assert.That(registry.ApplyUpgrade(ItemCatalog.FrostAxe.EffectAtLevel(level)), Is.True);

            Assert.That(ItemCatalog.FrostAxe.EffectAtLevel(6), Is.EqualTo(UpgradeId.ExtraAxe));
            Assert.That(registry.AxeDamage, Is.EqualTo(24f * Mathf.Pow(1.26f, 3f)).Within(0.0001f));
            Assert.That(registry.AxeCooldown, Is.EqualTo(0.82f * Mathf.Pow(0.86f, 2f)).Within(0.0001f));
            Assert.That(registry.AxeCount, Is.EqualTo(2));
            Assert.That(registry.AxePierce, Is.EqualTo(2));
        }

        [Test]
        public void Begin_EquipsDefaultRavenboundStarterWeapons()
        {
            var registry = new SharedWeaponRegistry();
            registry.Begin();

            Assert.That(registry.FindInstance("weapon.frost_axe"), Is.Not.Null);
            Assert.That(registry.FindInstance("weapon.raven_guard"), Is.Not.Null);
            Assert.That(registry.AxeDamage, Is.EqualTo(24f).Within(0.0001f));
            Assert.That(registry.ShieldDamage, Is.EqualTo(20f).Within(0.0001f));
        }
    }
}
