using NUnit.Framework;
using UnityEngine;

namespace ProjectExpedition.Tests
{
    public sealed class SharedExpeditionRouteModelTests
    {
        [Test]
        public void Begin_InitializesScoutObjectivesAndOpeningPhase()
        {
            var route = new SharedExpeditionRouteModel();
            route.Begin("frostbound.scout");

            Assert.That(route.MapId, Is.EqualTo("frostbound.scout"));
            Assert.That(route.CurrentPhase, Is.EqualTo(ExpeditionPhase.Shoreline));
            Assert.That(route.RequiredKillObjective, Is.EqualTo(120));
            Assert.That(route.OptionalShardObjective, Is.EqualTo(5));
            Assert.That(route.ExtractionBeaconX, Is.Zero.Within(0.0001f));
            Assert.That(route.ExtractionBeaconY, Is.EqualTo(14f).Within(0.0001f));
            Assert.That(route.ConsumeAnnouncement(), Is.EqualTo("THE SHORE AWAKENS"));
        }

        [Test]
        public void DraugrKills_UnlockBossBeforeTimer()
        {
            var route = new SharedExpeditionRouteModel();
            route.Begin("frostbound.scout");

            for (var i = 0; i < 119; i++)
                route.OnEnemyKilled(false, false);

            Assert.That(route.CanSpawnBoss(), Is.False);
            Assert.That(route.CurrentPhase, Is.EqualTo(ExpeditionPhase.Shoreline));

            route.OnEnemyKilled(false, false);

            Assert.That(route.DraugrKills, Is.EqualTo(120));
            Assert.That(route.CanSpawnBoss(), Is.True);
            Assert.That(route.ConsumeAnnouncement(), Is.EqualTo("THE JOTUNN HAS FOUND YOU"));
        }

        [Test]
        public void Advance_ReachesExtractionAfterBossKill()
        {
            var route = new SharedExpeditionRouteModel();
            route.Begin("frostbound.scout");
            route.OnEnemyKilled(true, false);

            Assert.That(route.CurrentPhase, Is.EqualTo(ExpeditionPhase.Extraction));
            Assert.That(route.BossKilled, Is.True);
            Assert.That(route.ConsumeAnnouncement(), Is.EqualTo("REACH THE BEACON"));
            Assert.That(route.IsExtractionComplete(), Is.False);
        }

        [Test]
        public void Extraction_CompletesAtBeaconOrAfterDuration()
        {
            var route = new SharedExpeditionRouteModel();
            route.Begin("frostbound.scout");
            route.OnEnemyKilled(true, false);

            route.Advance(1f, new Vector2(route.ExtractionBeaconX, route.ExtractionBeaconY));

            Assert.That(route.IsExtractionComplete(), Is.True);
            Assert.That(route.CurrentPhase, Is.EqualTo(ExpeditionPhase.Completed));

            route = new SharedExpeditionRouteModel();
            route.Begin("frostbound.scout");
            route.OnEnemyKilled(true, false);
            route.Advance(16f, Vector2.zero);

            Assert.That(route.IsExtractionComplete(), Is.True);
        }

        [Test]
        public void ResolveVictoryRelicId_GrantsWardenWhenOptionalObjectiveComplete()
        {
            var route = new SharedExpeditionRouteModel();
            route.Begin("frostbound.scout");

            Assert.That(route.ResolveVictoryRelicId(), Is.EqualTo("relic.jotunn_echo"));

            for (var i = 0; i < route.OptionalShardObjective; i++)
                route.OnShardCollected();

            Assert.That(route.ResolveVictoryRelicId(), Is.EqualTo("relic.jotunn_echo_warden"));
        }
    }
}
