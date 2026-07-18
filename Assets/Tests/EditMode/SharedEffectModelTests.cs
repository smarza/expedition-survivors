using NUnit.Framework;
using UnityEngine;

namespace ProjectExpedition.Tests
{
    public sealed class SharedEffectModelTests
    {
        [Test]
        public void Advance_PreservesAutomaticWeaponCadence()
        {
            var model = new SharedWeaponModel();

            var first = model.Advance(0.25f);
            var pulse = model.Advance(4.25f);

            Assert.That(first, Is.EqualTo(WeaponAdvanceResult.FireAxeVolley));
            Assert.That((pulse & WeaponAdvanceResult.TriggerRavenGuard) != 0, Is.True);
        }

        [Test]
        public void UpgradesAndEvolutions_PreserveExistingBalanceValues()
        {
            var model = new SharedWeaponModel();
            model.ApplyUpgrade(UpgradeId.AxeDamage);
            model.ApplyUpgrade(UpgradeId.AxeSpeed);
            model.ApplyUpgrade(UpgradeId.ExtraAxe);
            model.ApplyUpgrade(UpgradeId.AxePierce);
            model.ApplyUpgrade(UpgradeId.ShieldDamage);
            model.ApplyUpgrade(UpgradeId.CriticalRunes);
            model.ApplyEvolution(ItemCatalog.JotunnCleaver.Id);
            model.ApplyEvolution(ItemCatalog.StormAegis.Id);

            Assert.That(model.AxeDamage, Is.EqualTo(24f * 1.26f * 1.55f).Within(0.0001f));
            Assert.That(model.AxeCooldown, Is.EqualTo(0.82f * 0.86f).Within(0.0001f));
            Assert.That(model.AxeCount, Is.EqualTo(2));
            Assert.That(model.AxePierce, Is.EqualTo(4));
            Assert.That(model.ShieldDamage, Is.EqualTo(20f * 1.42f * 1.35f).Within(0.0001f));
            Assert.That(model.CriticalChance, Is.EqualTo(0.17f).Within(0.0001f));
        }

        [Test]
        public void FrostAxeLevelTable_ReachesTwoProjectilesAtMaximumLevel()
        {
            var model = new SharedWeaponModel();

            for (var level = 2; level <= ItemCatalog.FrostAxe.MaxLevel; level++)
                Assert.That(model.ApplyUpgrade(ItemCatalog.FrostAxe.EffectAtLevel(level)), Is.True);

            Assert.That(ItemCatalog.FrostAxe.EffectAtLevel(6), Is.EqualTo(UpgradeId.ExtraAxe));
            Assert.That(model.AxeDamage, Is.EqualTo(24f * Mathf.Pow(1.26f, 3f)).Within(0.0001f));
            Assert.That(model.AxeCooldown, Is.EqualTo(0.82f * Mathf.Pow(0.86f, 2f)).Within(0.0001f));
            Assert.That(model.AxeCount, Is.EqualTo(2));
            Assert.That(model.AxePierce, Is.EqualTo(2));
        }

        [Test]
        public void RavenGuardLevelTable_PreservesDamageAndAddsPromisedFrequency()
        {
            var player = StartedPlayer();
            var weapons = new SharedWeaponModel();

            for (var level = 2; level <= ItemCatalog.RavenGuard.MaxLevel; level++)
                Assert.That(SharedEffectPipeline.ApplyUpgrade(player, weapons,
                    ItemCatalog.RavenGuard.EffectAtLevel(level)), Is.True);

            Assert.That(ItemCatalog.RavenGuard.EffectAtLevel(5),
                Is.EqualTo(UpgradeId.ShieldDamageAndSpeed));
            Assert.That(ItemCatalog.RavenGuard.EffectAtLevel(8),
                Is.EqualTo(UpgradeId.ShieldDamageAndSpeed));
            Assert.That(weapons.ShieldDamage,
                Is.EqualTo(20f * Mathf.Pow(1.42f, 5f)).Within(0.0001f));
            Assert.That(weapons.ShieldCooldown,
                Is.EqualTo(5.2f * Mathf.Pow(0.86f, 2f)).Within(0.0001f));
            Assert.That(player.Armor, Is.EqualTo(4f));
        }

        [Test]
        public void AxeVolley_ProducesSharedProjectileEffectsAndDirections()
        {
            var model = new SharedWeaponModel();
            model.ApplyUpgrade(UpgradeId.ExtraAxe);
            model.ApplyEvolution(ItemCatalog.JotunnCleaver.Id);

            var left = model.CalculateAxeDirection(Vector2.up, 0);
            var right = model.CalculateAxeDirection(Vector2.up, 1);
            var effect = model.CreateAxeEffect(true);

            Assert.That(left.x, Is.GreaterThan(0f));
            Assert.That(right.x, Is.LessThan(0f));
            Assert.That(effect.Kind, Is.EqualTo(SharedEffectKind.Projectile));
            Assert.That(effect.Damage, Is.EqualTo(model.AxeDamage * 2f).Within(0.0001f));
            Assert.That(effect.Pierce, Is.EqualTo(model.AxePierce));
            Assert.That(effect.Duration, Is.EqualTo(3.2f));
            Assert.That(effect.Critical, Is.True);
            Assert.That(effect.Evolved, Is.True);
        }

        [Test]
        public void RavenGuard_ProducesSharedAreaEffectAndBoundedHealing()
        {
            var model = new SharedWeaponModel();
            model.ApplyEvolution(ItemCatalog.StormAegis.Id);

            var effect = model.CreateRavenGuardEffect();

            Assert.That(effect.Kind, Is.EqualTo(SharedEffectKind.AreaDamage));
            Assert.That(effect.Radius, Is.EqualTo(3.35f));
            Assert.That(effect.Knockback, Is.EqualTo(0.72f));
            Assert.That(model.CalculateRavenGuardHealing(4), Is.EqualTo(2.6f).Within(0.0001f));
            Assert.That(model.CalculateRavenGuardHealing(100), Is.EqualTo(9f));
        }

        [Test]
        public void EffectPipeline_AppliesPlayerWeaponAndEvolutionState()
        {
            var player = StartedPlayer();
            var weapons = new SharedWeaponModel();

            SharedEffectPipeline.ApplyUpgrade(player, weapons, UpgradeId.MoveSpeed);
            SharedEffectPipeline.ApplyUpgrade(player, weapons, UpgradeId.MaxHealth);
            SharedEffectPipeline.ApplyUpgrade(player, weapons, UpgradeId.AxeDamage);
            weapons.ApplyEvolution(ItemCatalog.JotunnCleaver.Id);

            Assert.That(player.MoveSpeed, Is.EqualTo(4.91f).Within(0.0001f));
            Assert.That(player.MaxHealth, Is.EqualTo(174f));
            Assert.That(weapons.AxeDamage, Is.EqualTo(24f * 1.26f * 1.55f).Within(0.0001f));
            Assert.That(weapons.FrostAxeEvolved, Is.True);
        }

        [Test]
        public void UltimateAndEvolutionExplosion_UseSharedAreaRequests()
        {
            var player = StartedPlayer();
            var ultimate = SharedEffectPipeline.CreateUltimate(player);
            var explosion = SharedEffectPipeline.CreateJotunnCleaverExplosion(100f);

            Assert.That(ultimate.Kind, Is.EqualTo(SharedEffectKind.AreaDamage));
            Assert.That(ultimate.Damage, Is.EqualTo(145f));
            Assert.That(ultimate.Radius, Is.EqualTo(6.8f));
            Assert.That(explosion.Damage, Is.EqualTo(42f));
            Assert.That(explosion.Radius, Is.EqualTo(1.25f));
        }

        private static SharedPlayerModel StartedPlayer()
        {
            var model = new SharedPlayerModel();
            model.Begin(150f, 4.45f, 2f, 60f, 145f, 6.8f,
                BalanceRules.UltimateCooldown);
            return model;
        }
    }
}
