using NUnit.Framework;
using UnityEngine;

namespace ProjectExpedition.Tests
{
    public sealed class SharedPickupCollectionTests
    {
        [Test]
        public void FindCollectingPlayerIndex_UsesAnyPlayerWithinCollectionRadius()
        {
            var positions = new[]
            {
                new Vector2(0.55f, 0f),
                new Vector2(0.35f, 0f)
            };
            var alive = new[] { true, true };

            var collectorIndex = SharedPickupCollectionModel.FindCollectingPlayerIndex(
                Vector2.zero,
                positions,
                alive);

            Assert.That(collectorIndex, Is.EqualTo(1));
        }

        [Test]
        public void FindMagnetTargetIndex_UsesNearestPlayerWithinMagnetRange()
        {
            var positions = new[]
            {
                new Vector2(2.5f, 0f),
                new Vector2(1.2f, 0f)
            };
            var alive = new[] { true, true };
            var magnetRadii = new[] { 3f, 1.7f };

            var targetIndex = SharedPickupCollectionModel.FindMagnetTargetIndex(
                Vector2.zero,
                positions,
                alive,
                magnetRadii);

            Assert.That(targetIndex, Is.EqualTo(1));
        }

        [Test]
        public void FindMagnetTargetIndex_IgnoresPlayerOutsideMagnetRangeEvenIfGloballyNearest()
        {
            var positions = new[]
            {
                new Vector2(0.8f, 0f),
                new Vector2(1.4f, 0f)
            };
            var alive = new[] { true, true };
            var magnetRadii = new[] { 0.7f, 1.7f };

            var targetIndex = SharedPickupCollectionModel.FindMagnetTargetIndex(
                Vector2.zero,
                positions,
                alive,
                magnetRadii);

            Assert.That(targetIndex, Is.EqualTo(1));
        }
    }
}
