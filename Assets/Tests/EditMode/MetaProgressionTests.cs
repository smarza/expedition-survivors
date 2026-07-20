using NUnit.Framework;

namespace ProjectExpedition.Tests
{
    public sealed class MetaProgressionTests
    {
        [Test]
        public void FreshSave_StartsWithHaldorAndScoutOnly()
        {
            var progress = new MetaProgress();
            SharedMetaProgressionModel.EnsureStarterUnlocks(progress);

            Assert.That(SharedMetaProgressionModel.IsUnlocked(progress, SharedMetaProgressionModel.HaldorId), Is.True);
            Assert.That(SharedMetaProgressionModel.IsUnlocked(progress, SharedMetaProgressionModel.ScoutMapId), Is.True);
            Assert.That(SharedMetaProgressionModel.IsUnlocked(progress, SharedMetaProgressionModel.SylvaId), Is.False);
            Assert.That(SharedMetaProgressionModel.IsUnlocked(progress, SharedMetaProgressionModel.SagaMapId), Is.False);
        }

        [Test]
        public void PurchaseUnlock_DeductsRenownAndUnlocksContent()
        {
            var progress = new MetaProgress { TotalRenown = 80 };
            SharedMetaProgressionModel.EnsureStarterUnlocks(progress);

            var result = SharedMetaProgressionModel.TryPurchaseUnlock(progress, SharedMetaProgressionModel.SylvaId);

            Assert.That(result.Success, Is.True);
            Assert.That(progress.SpentRenown, Is.EqualTo(50));
            Assert.That(SharedMetaProgressionModel.AvailableRenown(progress), Is.EqualTo(30));
            Assert.That(SharedMetaProgressionModel.IsUnlocked(progress, SharedMetaProgressionModel.SylvaId), Is.True);
        }

        [Test]
        public void PurchaseUnlock_RejectsInsufficientRenown()
        {
            var progress = new MetaProgress { TotalRenown = 30 };
            SharedMetaProgressionModel.EnsureStarterUnlocks(progress);

            var result = SharedMetaProgressionModel.TryPurchaseUnlock(progress, SharedMetaProgressionModel.SylvaId);

            Assert.That(result.Success, Is.False);
            Assert.That(progress.SpentRenown, Is.Zero);
            Assert.That(SharedMetaProgressionModel.IsUnlocked(progress, SharedMetaProgressionModel.SylvaId), Is.False);
        }

        [Test]
        public void PurchaseUnlock_RejectsDoublePurchase()
        {
            var progress = new MetaProgress { TotalRenown = 120, SpentRenown = 50 };
            SharedMetaProgressionModel.EnsureStarterUnlocks(progress);
            SharedMetaProgressionModel.TryPurchaseUnlock(progress, SharedMetaProgressionModel.SylvaId);

            var result = SharedMetaProgressionModel.TryPurchaseUnlock(progress, SharedMetaProgressionModel.SylvaId);

            Assert.That(result.Success, Is.False);
            Assert.That(progress.SpentRenown, Is.EqualTo(50));
        }

        [Test]
        public void VersionThreeSave_MigratesToVersionFourWithRetroactiveSylva()
        {
            const string versionThree =
                "{\"Version\":3,\"Progress\":{\"TotalRenown\":55,\"RunsCompleted\":1,\"BestKills\":90,\"BestTime\":200,\"HaldorMastery\":4,\"RelicsCollected\":[]}}";

            var progress = SaveMigration.Deserialize(versionThree, out var sourceVersion);

            Assert.That(sourceVersion, Is.EqualTo(3));
            Assert.That(SharedMetaProgressionModel.IsUnlocked(progress, SharedMetaProgressionModel.SylvaId), Is.True);
            Assert.That(progress.SpentRenown, Is.Zero);
            Assert.That(progress.DiscoveredCodexIds, Is.Not.Null);
        }

        [Test]
        public void MasteryGain_MatchesRunFormula()
        {
            Assert.That(SharedMetaProgressionModel.CalculateMasteryGain(50, false), Is.EqualTo(2));
            Assert.That(SharedMetaProgressionModel.CalculateMasteryGain(100, true), Is.EqualTo(7));
        }

        [Test]
        public void MasteryDamageMultiplier_CapsAtFifteenPercent()
        {
            Assert.That(SharedMetaProgressionModel.MasteryDamageMultiplier(0), Is.EqualTo(1f).Within(0.001f));
            Assert.That(SharedMetaProgressionModel.MasteryDamageMultiplier(30), Is.EqualTo(1.15f).Within(0.001f));
            Assert.That(SharedMetaProgressionModel.MasteryDamageMultiplier(100), Is.EqualTo(1.15f).Within(0.001f));
        }

        [Test]
        public void ApplyMasteryToProgress_UpdatesHeroSpecificField()
        {
            var progress = new MetaProgress();
            SharedMetaProgressionModel.ApplyMasteryToProgress(progress, SharedMetaProgressionModel.MaraId, 75, true);

            Assert.That(progress.MaraMastery, Is.EqualTo(6));
            Assert.That(progress.HaldorMastery, Is.Zero);
        }

        [Test]
        public void DiscoverCodex_IsIdempotent()
        {
            var progress = new MetaProgress();
            SharedMetaProgressionModel.EnsureStarterUnlocks(progress);

            Assert.That(SharedMetaProgressionModel.DiscoverCodex(progress, "weapon.frost_axe"), Is.True);
            Assert.That(SharedMetaProgressionModel.DiscoverCodex(progress, "weapon.frost_axe"), Is.False);
            Assert.That(progress.DiscoveredCodexIds.Length, Is.EqualTo(1));
        }

        [Test]
        public void EvolutionCodex_ShowsHintWhenBaseAndCatalystDiscovered()
        {
            var progress = new MetaProgress();
            SharedMetaProgressionModel.EnsureStarterUnlocks(progress);
            SharedMetaProgressionModel.DiscoverCodex(progress, "weapon.frost_axe");
            SharedMetaProgressionModel.DiscoverCodex(progress, "gear.jotunn_rune");

            var entry = SharedMetaProgressionModel.FindCodexEntry("evolution.jotunn_cleaver");
            Assert.That(entry.HasValue, Is.True);

            var visibility = SharedMetaProgressionModel.ResolveCodexVisibility(progress, entry.Value);
            Assert.That(visibility, Is.EqualTo(CodexVisibility.Hint));
        }

        [Test]
        public void NextUnlockedCharacterIndex_SkipsLockedHeroes()
        {
            var progress = new MetaProgress();
            SharedMetaProgressionModel.EnsureStarterUnlocks(progress);

            var next = SharedMetaProgressionModel.NextUnlockedCharacterIndex(progress, 0, 1);

            Assert.That(ContentCatalog.Character(next).Id, Is.EqualTo(SharedMetaProgressionModel.HaldorId));
        }

        [Test]
        public void FindCheapestAffordableUnlock_PrefersLowestCost()
        {
            var progress = new MetaProgress { TotalRenown = 65 };
            SharedMetaProgressionModel.EnsureStarterUnlocks(progress);

            var unlockId = SharedMetaProgressionModel.FindCheapestAffordableUnlockId(progress);

            Assert.That(unlockId, Is.EqualTo(SharedMetaProgressionModel.SylvaId));
        }
    }
}
