using NUnit.Framework;
using UnityEngine;

namespace ProjectExpedition.Tests
{
    public sealed class SharedSpawnModelTests
    {
        [Test]
        public void Advance_UsesInitialDelayAndMapDifficultyRamp()
        {
            var model = new SharedSpawnModel();
            model.Begin();

            var waiting = model.Advance(0.19f, 46f, ContentCatalog.Maps[0], 0, false);
            var spawning = model.Advance(0.02f, 46f, ContentCatalog.Maps[0], 0, false);

            Assert.That(waiting.RegularEnemyCount, Is.Zero);
            Assert.That(spawning.RegularEnemyCount, Is.EqualTo(2));
            Assert.That(spawning.Difficulty, Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void Advance_GrowsGroupsAndClampsSpawnInterval()
        {
            var model = new SharedSpawnModel();
            model.Begin();

            var result = model.Advance(1f, 1000f, ContentCatalog.Maps[0], 0, false);

            Assert.That(result.RegularEnemyCount, Is.EqualTo(7));
            Assert.That(model.SpawnTimer,
                Is.EqualTo(ContentCatalog.Maps[0].MinimumSpawnInterval).Within(0.0001f));
        }

        [Test]
        public void Advance_RespectsActiveCapButNeverSuppressesBoss()
        {
            var model = new SharedSpawnModel();
            model.Begin();

            var result = model.Advance(1f, 240f, ContentCatalog.Maps[0],
                SharedSpawnModel.MaximumActiveEnemies, true);

            Assert.That(result.RegularEnemyCount, Is.Zero);
            Assert.That(result.SpawnBoss, Is.True);
        }

        [Test]
        public void CalculateSpawnPosition_UsesSharedRingBounds()
        {
            var center = new Vector2(2f, -3f);

            var position = SharedSpawnModel.CalculateSpawnPosition(center, Mathf.PI * 0.5f, 99f);

            Assert.That(position.x, Is.EqualTo(center.x).Within(0.0001f));
            Assert.That(position.y, Is.EqualTo(center.y + SharedSpawnModel.MaximumSpawnDistance)
                .Within(0.0001f));
        }
    }
}
