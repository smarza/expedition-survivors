using NUnit.Framework;

namespace ProjectExpedition.Tests
{
    public sealed class SharedChallengeProfileModelTests
    {
        [Test]
        public void ResolveRenownMultiplier_StandardTierReturnsOne()
        {
            var profile = new ChallengeProfile(ChallengeTier.Standard, ChallengeMutator.None, ChallengeMutator.None);

            Assert.That(SharedChallengeProfileModel.ResolveRenownMultiplier(profile), Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void ResolveRenownMultiplier_VeteranTierIncreasesBase()
        {
            var profile = new ChallengeProfile(ChallengeTier.Veteran, ChallengeMutator.None, ChallengeMutator.None);

            Assert.That(SharedChallengeProfileModel.ResolveRenownMultiplier(profile), Is.EqualTo(1.25f).Within(0.001f));
        }

        [Test]
        public void ApplyEnemyHealthMultiplier_VeteranIncreasesHealth()
        {
            var profile = new ChallengeProfile(ChallengeTier.Veteran, ChallengeMutator.None, ChallengeMutator.None);

            Assert.That(
                SharedChallengeProfileModel.ApplyEnemyHealthMultiplier(100f, profile),
                Is.EqualTo(125f).Within(0.001f));
        }

        [Test]
        public void ApplySpawnInterval_VeteranReducesInterval()
        {
            var profile = new ChallengeProfile(ChallengeTier.Veteran, ChallengeMutator.None, ChallengeMutator.None);

            Assert.That(
                SharedChallengeProfileModel.ApplySpawnInterval(1f, profile),
                Is.EqualTo(0.85f).Within(0.001f));
        }

        [Test]
        public void ApplyGroupSize_SwarmSurgeAddsOneAndClamps()
        {
            var profile = new ChallengeProfile(
                ChallengeTier.Standard,
                ChallengeMutator.SwarmSurge,
                ChallengeMutator.None);

            Assert.That(SharedChallengeProfileModel.ApplyGroupSize(7, profile), Is.EqualTo(8));
            Assert.That(SharedChallengeProfileModel.ApplyGroupSize(1, profile), Is.EqualTo(2));
        }

        [Test]
        public void ApplyPlayerDamageTakenMultiplier_GlassCannonIncreasesDamage()
        {
            var profile = new ChallengeProfile(
                ChallengeTier.Standard,
                ChallengeMutator.GlassCannon,
                ChallengeMutator.None);

            Assert.That(
                SharedChallengeProfileModel.ApplyPlayerDamageTakenMultiplier(10f, profile),
                Is.EqualTo(14f).Within(0.001f));
        }

        [Test]
        public void ApplyWeaponDamageMultiplier_GlassCannonIncreasesDamage()
        {
            var profile = new ChallengeProfile(
                ChallengeTier.Standard,
                ChallengeMutator.GlassCannon,
                ChallengeMutator.None);

            Assert.That(
                SharedChallengeProfileModel.ApplyWeaponDamageMultiplier(20f, profile),
                Is.EqualTo(25f).Within(0.001f));
        }

        [Test]
        public void AllowsHealingRewards_IronResolveBlocksHealing()
        {
            var standard = new ChallengeProfile(ChallengeTier.Standard, ChallengeMutator.None, ChallengeMutator.None);
            var ironResolve = new ChallengeProfile(
                ChallengeTier.Standard,
                ChallengeMutator.IronResolve,
                ChallengeMutator.None);

            Assert.That(SharedChallengeProfileModel.AllowsHealingRewards(standard), Is.True);
            Assert.That(SharedChallengeProfileModel.AllowsHealingRewards(ironResolve), Is.False);
        }
    }
}
