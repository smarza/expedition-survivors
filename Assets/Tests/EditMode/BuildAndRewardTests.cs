using NUnit.Framework;

namespace ProjectExpedition.Tests
{
    public sealed class BuildAndRewardTests
    {
        [Test]
        public void PlayerBuild_RespectsSlotsWhileAllowingExistingItemLevels()
        {
            var build = new PlayerBuild();
            build.Initialize(2, 1);

            Assert.That(build.CountCategory(ItemCategory.Weapon), Is.EqualTo(2));
            Assert.That(build.CanAcquire(ItemCatalog.FrostAxe), Is.True);
            Assert.That(build.Acquire(ItemCatalog.LongshipBoots), Is.Not.Null);
            Assert.That(build.CanAcquire(ItemCatalog.BearBlooded), Is.False);
        }

        [Test]
        public void PlayerBuild_EvolutionRequiresMaximumLevelAndCatalyst()
        {
            var build = new PlayerBuild();
            build.Initialize(4, 4);

            for (var level = 2; level <= ItemCatalog.FrostAxe.MaxLevel; level++)
                Assert.That(build.Acquire(ItemCatalog.FrostAxe), Is.Not.Null);

            Assert.That(build.Find(ItemCatalog.FrostAxe.Id).Level, Is.EqualTo(ItemCatalog.FrostAxe.MaxLevel));
            Assert.That(build.CanEvolve(ItemCatalog.JotunnCleaver), Is.False);
            Assert.That(build.Acquire(ItemCatalog.JotunnRune), Is.Not.Null);
            Assert.That(build.CanEvolve(ItemCatalog.JotunnCleaver), Is.True);

            var result = build.Acquire(ItemCatalog.JotunnCleaver);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Evolution, Is.True);
            Assert.That(build.Find(ItemCatalog.FrostAxe.Id).EvolutionId, Is.EqualTo(ItemCatalog.JotunnCleaver.Id));
            Assert.That(build.CanEvolve(ItemCatalog.JotunnCleaver), Is.False);
        }

        [Test]
        public void RewardFactory_SameSeedProducesSameRecipientsAndItems()
        {
            var first = GeneratePair(7001);
            var second = GeneratePair(7001);

            Assert.That(first.Length, Is.EqualTo(4));
            for (var i = 0; i < first.Length; i++)
            {
                Assert.That(first[i].Item.Id, Is.EqualTo(second[i].Item.Id));
                Assert.That(first[i].TargetPlayerIndex, Is.EqualTo(second[i].TargetPlayerIndex));
                Assert.That(first[i].Shared, Is.EqualTo(second[i].Shared));
                Assert.That(first[i].TargetPlayerIndex, Is.InRange(0, 1));
            }
            Assert.That(first[0].TargetPlayerIndex, Is.EqualTo(1));
            Assert.That(first[1].TargetPlayerIndex, Is.EqualTo(1));
        }

        [Test]
        public void BalanceRules_ProgressionAndUltimateCooldownRemainBounded()
        {
            Assert.That(BalanceRules.ExperienceToNext(2, 1), Is.GreaterThan(BalanceRules.ExperienceToNext(1, 1)));
            Assert.That(BalanceRules.ExperienceToNext(1, 2), Is.GreaterThan(BalanceRules.ExperienceToNext(1, 1)));
            Assert.That(BalanceRules.UltimateCooldown(60f, 1), Is.LessThan(60f));
            Assert.That(BalanceRules.UltimateCooldown(60f, 100), Is.EqualTo(28f));
        }

        [Test]
        public void UpgradeDescriptions_MatchExactWeaponLevelEffects()
        {
            Assert.That(ItemCatalog.FrostAxe.EffectDescriptionAtLevel(6),
                Is.EqualTo("+1 projectile per Frost Axe volley"));
            Assert.That(ItemCatalog.RavenGuard.EffectDescriptionAtLevel(5),
                Is.EqualTo("+42% Raven Guard damage and -14% interval"));
            Assert.That(UpgradeDescriptions.Progression(ItemCatalog.FrostAxe),
                Does.Contain("L8: +26% Frost Axe damage"));
            Assert.That(ItemCatalog.Find("gear.flare_core").EffectDescriptionAtLevel(1),
                Is.EqualTo("+1 Signal Flare projectile per volley"));
        }

        private static RewardOption[] GeneratePair(int seed)
        {
            var builds = new[] { new PlayerBuild(), new PlayerBuild() };
            builds[0].Initialize(4, 4);
            builds[1].Initialize(4, 4);
            return RewardFactory.Generate(builds, 1, 2, new RunRandom(seed)).ToArray();
        }
    }
}
