using NUnit.Framework;

namespace ProjectExpedition.Tests
{
    public sealed class SaveMigrationTests
    {
        [Test]
        public void LegacyV1Save_MigratesWithoutProgressLoss()
        {
            const string legacy = "{\"TotalRenown\":42,\"RunsCompleted\":3,\"BestKills\":81,\"BestTime\":245.5,\"HaldorMastery\":7}";

            var progress = SaveMigration.Deserialize(legacy, out var sourceVersion);

            Assert.That(sourceVersion, Is.EqualTo(1));
            Assert.That(progress.TotalRenown, Is.EqualTo(42));
            Assert.That(progress.RunsCompleted, Is.EqualTo(3));
            Assert.That(progress.BestKills, Is.EqualTo(81));
            Assert.That(progress.BestTime, Is.EqualTo(245.5f));
            Assert.That(progress.HaldorMastery, Is.EqualTo(7));
        }

        [Test]
        public void CurrentSave_RoundTripsThroughVersionedEnvelope()
        {
            var original = new MetaProgress
            {
                TotalRenown = 120,
                RunsCompleted = 6,
                BestKills = 144,
                BestTime = 612.25f,
                HaldorMastery = 19
            };

            var json = SaveMigration.Serialize(original);
            var restored = SaveMigration.Deserialize(json, out var sourceVersion);

            Assert.That(sourceVersion, Is.EqualTo(SaveMigration.CurrentVersion));
            Assert.That(restored.TotalRenown, Is.EqualTo(original.TotalRenown));
            Assert.That(restored.RunsCompleted, Is.EqualTo(original.RunsCompleted));
            Assert.That(restored.BestKills, Is.EqualTo(original.BestKills));
            Assert.That(restored.BestTime, Is.EqualTo(original.BestTime));
            Assert.That(restored.HaldorMastery, Is.EqualTo(original.HaldorMastery));
        }
    }
}
