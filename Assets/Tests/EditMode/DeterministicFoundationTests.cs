using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectExpedition.Tests
{
    public sealed class DeterministicFoundationTests
    {
        private sealed class Point
        {
            public Vector2 Position;
        }

        [Test]
        public void RunRandom_SameSeedProducesSameSequence()
        {
            var first = new RunRandom(224102942);
            var second = new RunRandom(224102942);

            for (var i = 0; i < 64; i++)
            {
                Assert.That(first.Range(-5000, 5000), Is.EqualTo(second.Range(-5000, 5000)));
                Assert.That(first.Range(-40f, 40f), Is.EqualTo(second.Range(-40f, 40f)));
                Assert.That(first.Chance(0.37f), Is.EqualTo(second.Chance(0.37f)));
            }
        }

        [Test]
        public void RunRandom_NormalizesZeroSeedAndKeepsRangeBounds()
        {
            var random = new RunRandom(0);

            Assert.That(random.Seed, Is.EqualTo(1));
            for (var i = 0; i < 128; i++)
                Assert.That(random.Range(3, 9), Is.InRange(3, 8));
        }

        [Test]
        public void SpatialGrid_UpdatesAndRemovesMembership()
        {
            var grid = new SpatialHashGrid<Point>(2f, point => point.Position);
            var near = new Point { Position = new Vector2(1f, 1f) };
            var far = new Point { Position = new Vector2(20f, 20f) };
            grid.Add(near);
            grid.Add(far);

            Assert.That(grid.ItemCount, Is.EqualTo(2));
            Assert.That(grid.FindNearest(Vector2.zero, 5f), Is.SameAs(near));

            near.Position = new Vector2(30f, 30f);
            grid.Update(near);
            Assert.That(grid.FindNearest(Vector2.zero, 5f), Is.Null);

            grid.Remove(far);
            Assert.That(grid.ItemCount, Is.EqualTo(1));
            grid.Clear();
            Assert.That(grid.ItemCount, Is.Zero);
            Assert.That(grid.OccupiedCellCount, Is.Zero);
        }

        [Test]
        public void ComponentPool_ReusesReleasedInstances()
        {
            var root = new GameObject("Pool Test Root");
            try
            {
                var pool = new ComponentPool<PoolProbe>(
                    () => new GameObject("Pool Probe").AddComponent<PoolProbe>(), root.transform, 1);

                var first = pool.Get(Vector3.zero);
                pool.Release(first);
                var second = pool.Get(Vector3.one);

                Assert.That(second, Is.SameAs(first));
                Assert.That(pool.Created, Is.EqualTo(1));
                Assert.That(pool.Reused, Is.EqualTo(2));
                Assert.That(pool.ActiveCount, Is.EqualTo(1));
                Assert.That(second.ReleaseCount, Is.EqualTo(2));
                pool.Release(second);
                Assert.That(pool.AvailableCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ProductionContent_HasStableUniqueIdsAndValidRecipes()
        {
            Assert.That(ProductionContentRuntime.Validate(out var report), Is.True, report);
            Assert.That(ContentCatalog.Character(0).Id, Is.EqualTo("ravenbound.haldor"));
            Assert.That(ItemCatalog.Find("evolution.jotunn_cleaver"), Is.Not.Null);
            Assert.That(EnemyCatalog.Draugr.Id, Is.EqualTo("enemy.draugr_raider"));
        }
    }
}
