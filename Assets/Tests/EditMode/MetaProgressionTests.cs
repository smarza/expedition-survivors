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
            var progress = new MetaProgress { TotalRenown = 100 };
            SharedMetaProgressionModel.EnsureStarterUnlocks(progress);

            var result = SharedMetaProgressionModel.TryPurchaseUnlock(progress, SharedMetaProgressionModel.SylvaId);

            Assert.That(result.Success, Is.True);
            Assert.That(progress.SpentRenown, Is.EqualTo(75));
            Assert.That(SharedMetaProgressionModel.AvailableRenown(progress), Is.EqualTo(25));
            Assert.That(SharedMetaProgressionModel.IsUnlocked(progress, SharedMetaProgressionModel.SylvaId), Is.True);
        }

        [Test]
        public void PurchaseUnlock_RejectsInsufficientRenown()
        {
            var progress = new MetaProgress { TotalRenown = 50 };
            SharedMetaProgressionModel.EnsureStarterUnlocks(progress);

            var result = SharedMetaProgressionModel.TryPurchaseUnlock(progress, SharedMetaProgressionModel.SylvaId);

            Assert.That(result.Success, Is.False);
            Assert.That(progress.SpentRenown, Is.Zero);
            Assert.That(SharedMetaProgressionModel.IsUnlocked(progress, SharedMetaProgressionModel.SylvaId), Is.False);
        }

        [Test]
        public void PurchaseUnlock_RejectsDoublePurchase()
        {
            var progress = new MetaProgress { TotalRenown = 120 };
            SharedMetaProgressionModel.EnsureStarterUnlocks(progress);
            SharedMetaProgressionModel.TryPurchaseUnlock(progress, SharedMetaProgressionModel.SylvaId);

            var result = SharedMetaProgressionModel.TryPurchaseUnlock(progress, SharedMetaProgressionModel.SylvaId);

            Assert.That(result.Success, Is.False);
            Assert.That(progress.SpentRenown, Is.EqualTo(75));
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

        public void RunRenownEarned_MatchesBalancedFormula()
        {
            Assert.That(SharedMetaProgressionModel.CalculateRunRenownEarned(0, 0, false), Is.EqualTo(1));
            Assert.That(SharedMetaProgressionModel.CalculateRunRenownEarned(20, 200, true), Is.EqualTo(58));
            Assert.That(SharedMetaProgressionModel.CalculateRunRenownEarned(0, 150, false), Is.EqualTo(10));
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
        public void NextCharacterIndex_WrapsAllFourHeroes()
        {
            Assert.That(SharedMetaProgressionModel.NextCharacterIndex(0, 1, 0), Is.EqualTo(1));
            Assert.That(SharedMetaProgressionModel.NextCharacterIndex(1, 1, 0), Is.EqualTo(2));
            Assert.That(SharedMetaProgressionModel.NextCharacterIndex(2, 1, 0), Is.EqualTo(3));
            Assert.That(SharedMetaProgressionModel.NextCharacterIndex(3, 1, 0), Is.EqualTo(0));
            Assert.That(SharedMetaProgressionModel.NextCharacterIndex(0, 0, 1), Is.EqualTo(2));
            Assert.That(SharedMetaProgressionModel.NextCharacterIndex(2, 0, -1), Is.EqualTo(0));
        }

        [Test]
        public void NextCharacterIndex_IncludesLockedHeroes()
        {
            var progress = new MetaProgress();
            SharedMetaProgressionModel.EnsureStarterUnlocks(progress);

            var next = SharedMetaProgressionModel.NextCharacterIndex(0, 1, 0);

            Assert.That(next, Is.EqualTo(1));
            Assert.That(SharedMetaProgressionModel.IsCharacterUnlocked(progress, next), Is.False);
            Assert.That(ContentCatalog.Character(next).Id, Is.EqualTo(SharedMetaProgressionModel.EiraId));
        }

        [Test]
        public void NextMapIndex_WrapsBothMaps()
        {
            Assert.That(SharedMetaProgressionModel.NextMapIndex(0, 1, 0), Is.EqualTo(1));
            Assert.That(SharedMetaProgressionModel.NextMapIndex(1, 1, 0), Is.EqualTo(0));
        }

        [Test]
        public void NextMapIndex_IncludesLockedMaps()
        {
            var progress = new MetaProgress();
            SharedMetaProgressionModel.EnsureStarterUnlocks(progress);

            var next = SharedMetaProgressionModel.NextMapIndex(0, 1, 0);

            Assert.That(next, Is.EqualTo(1));
            Assert.That(SharedMetaProgressionModel.IsMapUnlocked(progress, next), Is.False);
            Assert.That(ContentCatalog.Map(next).Id, Is.EqualTo(SharedMetaProgressionModel.SagaMapId));
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
            var progress = new MetaProgress { TotalRenown = 80 };
            SharedMetaProgressionModel.EnsureStarterUnlocks(progress);

            var unlockId = SharedMetaProgressionModel.FindCheapestAffordableUnlockId(progress);

            Assert.That(unlockId, Is.EqualTo(SharedMetaProgressionModel.SylvaId));
        }

        [Test]
        public void VersionFourSave_MigratesToVersionFiveWithCanopyAndRelayUnlocks()
        {
            const string versionFour =
                "{\"Version\":4,\"Progress\":{\"TotalRenown\":130,\"RunsCompleted\":2,\"BestKills\":160,\"BestTime\":280,\"HaldorMastery\":6,\"SylvaMastery\":2,\"MaraMastery\":1,\"EiraMastery\":3,\"RelicsCollected\":[\"relic.jotunn_echo\"],\"UnlockedContentIds\":[\"ravenbound.haldor\",\"frostbound.scout\",\"oathbound.sylva\",\"ravenbound.eira\",\"ironway.mara\",\"frostbound.saga\"]}}";

            var progress = SaveMigration.Deserialize(versionFour, out var sourceVersion);

            Assert.That(sourceVersion, Is.EqualTo(4));
            Assert.That(SaveMigration.CurrentVersion, Is.EqualTo(5));
            Assert.That(SharedMetaProgressionModel.IsUnlocked(progress, SharedMetaProgressionModel.CanopyScoutMapId), Is.True);
            Assert.That(SharedMetaProgressionModel.IsUnlocked(progress, SharedMetaProgressionModel.RelayScoutMapId), Is.True);
            Assert.That(SharedMetaProgressionModel.IsUnlocked(progress, SharedMetaProgressionModel.BrenId), Is.True);
            Assert.That(SharedMetaProgressionModel.IsUnlocked(progress, SharedMetaProgressionModel.RexId), Is.True);
            Assert.That(SharedMetaProgressionModel.IsVeteranUnlocked(progress), Is.True);
        }

        [Test]
        public void PurchaseUnlock_BrenAndRexMatchUnlockCatalogCosts()
        {
            var progress = new MetaProgress { TotalRenown = 400 };
            SharedMetaProgressionModel.EnsureStarterUnlocks(progress);

            var bren = SharedMetaProgressionModel.TryPurchaseUnlock(progress, SharedMetaProgressionModel.BrenId);
            var rex = SharedMetaProgressionModel.TryPurchaseUnlock(progress, SharedMetaProgressionModel.RexId);

            Assert.That(bren.Success, Is.True);
            Assert.That(rex.Success, Is.True);
            Assert.That(progress.SpentRenown, Is.EqualTo(175 + 210));
            Assert.That(SharedMetaProgressionModel.IsUnlocked(progress, SharedMetaProgressionModel.BrenId), Is.True);
            Assert.That(SharedMetaProgressionModel.IsUnlocked(progress, SharedMetaProgressionModel.RexId), Is.True);
        }
    }
}
