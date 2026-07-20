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
        public void Version2Save_MigratesRelicsCollected()
        {
            const string versionTwo = "{\"Version\":2,\"Progress\":{\"TotalRenown\":10,\"RunsCompleted\":2,\"BestKills\":40,\"BestTime\":180,\"HaldorMastery\":4}}";

            var progress = SaveMigration.Deserialize(versionTwo, out var sourceVersion);

            Assert.That(sourceVersion, Is.EqualTo(2));
            Assert.That(progress.RelicsCollected, Is.Not.Null);
            Assert.That(progress.RelicsCollected.Length, Is.Zero);
        }

        [Test]
        public void CurrentSave_RoundTripsThroughVersionedEnvelope()
        {
            var original = new MetaProgress
            {
                TotalRenown = 120,
                SpentRenown = 50,
                RunsCompleted = 6,
                BestKills = 144,
                BestTime = 612.25f,
                HaldorMastery = 19,
                SylvaMastery = 8,
                MaraMastery = 3,
                EiraMastery = 5,
                RelicsCollected = new[] { "relic.jotunn_echo" },
                UnlockedContentIds = new[] { SharedMetaProgressionModel.HaldorId, SharedMetaProgressionModel.SylvaId },
                DiscoveredCodexIds = new[] { "weapon.frost_axe" },
                CampOnboardingComplete = true
            };

            var json = SaveMigration.Serialize(original);
            var restored = SaveMigration.Deserialize(json, out var sourceVersion);

            Assert.That(sourceVersion, Is.EqualTo(SaveMigration.CurrentVersion));
            Assert.That(restored.TotalRenown, Is.EqualTo(original.TotalRenown));
            Assert.That(restored.SpentRenown, Is.EqualTo(original.SpentRenown));
            Assert.That(restored.RunsCompleted, Is.EqualTo(original.RunsCompleted));
            Assert.That(restored.BestKills, Is.EqualTo(original.BestKills));
            Assert.That(restored.BestTime, Is.EqualTo(original.BestTime));
            Assert.That(restored.HaldorMastery, Is.EqualTo(original.HaldorMastery));
            Assert.That(restored.SylvaMastery, Is.EqualTo(original.SylvaMastery));
            Assert.That(restored.MaraMastery, Is.EqualTo(original.MaraMastery));
            Assert.That(restored.EiraMastery, Is.EqualTo(original.EiraMastery));
            Assert.That(restored.RelicsCollected, Is.EqualTo(original.RelicsCollected));
            Assert.That(restored.UnlockedContentIds, Is.EqualTo(original.UnlockedContentIds));
            Assert.That(restored.DiscoveredCodexIds, Is.EqualTo(original.DiscoveredCodexIds));
            Assert.That(restored.CampOnboardingComplete, Is.True);
        }

        [Test]
        public void VersionThreeSave_MigratesToVersionFourEnvelope()
        {
            const string versionThree =
                "{\"Version\":3,\"Progress\":{\"TotalRenown\":80,\"RunsCompleted\":2,\"BestKills\":160,\"BestTime\":280,\"HaldorMastery\":6,\"RelicsCollected\":[\"relic.jotunn_echo\"]}}";

            var progress = SaveMigration.Deserialize(versionThree, out var sourceVersion);

            Assert.That(sourceVersion, Is.EqualTo(3));
            Assert.That(SaveMigration.CurrentVersion, Is.EqualTo(4));
            Assert.That(SharedMetaProgressionModel.IsUnlocked(progress, SharedMetaProgressionModel.SylvaId), Is.True);
            Assert.That(SharedMetaProgressionModel.IsUnlocked(progress, SharedMetaProgressionModel.EiraId), Is.True);
            Assert.That(SharedMetaProgressionModel.IsUnlocked(progress, SharedMetaProgressionModel.MaraId), Is.True);
            Assert.That(SharedMetaProgressionModel.IsUnlocked(progress, SharedMetaProgressionModel.SagaMapId), Is.True);
        }

        [Test]
        public void ResolveCampLeader_UsesLastPlayedHero()
        {
            SaveService.AssignDataForTests(new MetaProgress
            {
                LastCampLeaderId = "ironway.mara"
            });

            Assert.That(SaveService.ResolveCampLeader().Id, Is.EqualTo("ironway.mara"));
            Assert.That(SaveService.ResolveCampLeader().Name, Is.EqualTo("Captain Mara Voss"));
        }

        [Test]
        public void ResolveCampLeader_FallsBackToHaldorWhenUnknown()
        {
            SaveService.AssignDataForTests(new MetaProgress
            {
                LastCampLeaderId = "missing.hero"
            });

            Assert.That(SaveService.ResolveCampLeader().Id, Is.EqualTo("ravenbound.haldor"));
        }

        [Test]
        public void ResolveLastCharacterSelectionIndex_UsesSavedIds()
        {
            SaveService.AssignDataForTests(new MetaProgress
            {
                LastCampLeaderId = "ironway.mara",
                LastCoopPartnerId = "oathbound.sylva"
            });

            Assert.That(SaveService.ResolveLastCharacterSelectionIndex(0),
                Is.EqualTo(ContentCatalog.CharacterIndex("ironway.mara")));
            Assert.That(SaveService.ResolveLastCharacterSelectionIndex(1),
                Is.EqualTo(ContentCatalog.CharacterIndex("oathbound.sylva")));
        }

        [Test]
        public void SerializeRoundTrip_PreservesLastCampLeaderId()
        {
            var original = new MetaProgress
            {
                LastCampLeaderId = "oathbound.sylva"
            };

            var json = SaveMigration.Serialize(original);
            var restored = SaveMigration.Deserialize(json, out _);

            Assert.That(restored.LastCampLeaderId, Is.EqualTo("oathbound.sylva"));
        }

        [Test]
        public void SerializeRoundTrip_PreservesLastCoopPartnerId()
        {
            var original = new MetaProgress
            {
                LastCampLeaderId = "ironway.mara",
                LastCoopPartnerId = "ravenbound.eira"
            };

            var json = SaveMigration.Serialize(original);
            var restored = SaveMigration.Deserialize(json, out _);

            Assert.That(restored.LastCoopPartnerId, Is.EqualTo("ravenbound.eira"));
        }
    }
}
