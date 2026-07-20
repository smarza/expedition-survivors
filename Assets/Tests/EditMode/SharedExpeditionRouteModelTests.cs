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
            Assert.That(route.RequiredKillObjective, Is.EqualTo(150));
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

            for (var i = 0; i < 149; i++)
                route.OnEnemyKilled(false, false);

            Assert.That(route.CanSpawnBoss(), Is.False);
            Assert.That(route.CurrentPhase, Is.EqualTo(ExpeditionPhase.Shoreline));

            route.OnEnemyKilled(false, false);

            Assert.That(route.DraugrKills, Is.EqualTo(150));
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
            var beacon = new Vector2(route.ExtractionBeaconX, route.ExtractionBeaconY);

            route.Advance(1f, beacon);

            Assert.That(route.IsExtractionComplete(), Is.False);
            Assert.That(route.PartyAtExtractionBeacon, Is.True);
            Assert.That(route.ExtractionHoldElapsed, Is.EqualTo(1f).Within(0.0001f));

            route.Advance(2f, beacon);

            Assert.That(route.IsExtractionComplete(), Is.True);
            Assert.That(route.CurrentPhase, Is.EqualTo(ExpeditionPhase.Completed));
            Assert.That(route.ExtractionCompletion, Is.EqualTo(ExtractionCompletionKind.BeaconHold));

            route = new SharedExpeditionRouteModel();
            route.Begin("frostbound.scout");
            route.OnEnemyKilled(true, false);
            route.Advance(16f, Vector2.zero);

            Assert.That(route.IsExtractionComplete(), Is.True);
            Assert.That(route.ExtractionCompletion, Is.EqualTo(ExtractionCompletionKind.Timeout));
        }

        [Test]
        public void Extraction_HoldResetsWhenLeavingBeacon()
        {
            var route = new SharedExpeditionRouteModel();
            route.Begin("frostbound.scout");
            route.OnEnemyKilled(true, false);
            var beacon = new Vector2(route.ExtractionBeaconX, route.ExtractionBeaconY);

            route.Advance(1.5f, beacon);
            Assert.That(route.ExtractionHoldElapsed, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(route.IsExtractionComplete(), Is.False);

            route.Advance(2f, Vector2.zero);
            Assert.That(route.ExtractionHoldElapsed, Is.Zero.Within(0.0001f));
            Assert.That(route.PartyAtExtractionBeacon, Is.False);
            Assert.That(route.IsExtractionComplete(), Is.False);

            route.Advance(4f, beacon);
            Assert.That(route.IsExtractionComplete(), Is.True);
            Assert.That(route.ExtractionCompletion, Is.EqualTo(ExtractionCompletionKind.BeaconHold));
        }

        [Test]
        public void Extraction_QueuesUnderwayAnnouncementWhenEnteringBeacon()
        {
            var route = new SharedExpeditionRouteModel();
            route.Begin("frostbound.scout");
            route.OnEnemyKilled(true, false);
            route.ConsumeAnnouncement();

            route.Advance(0.5f, new Vector2(route.ExtractionBeaconX, route.ExtractionBeaconY));

            Assert.That(route.ConsumeAnnouncement(), Is.EqualTo("EXTRACTION UNDERWAY"));
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

        [Test]
        public void Begin_InitializesCanopyScoutObjectivesAndAnnouncements()
        {
            var route = new SharedExpeditionRouteModel();
            route.Begin("oathbound.scout");

            Assert.That(route.MapId, Is.EqualTo("oathbound.scout"));
            Assert.That(route.RequiredKillObjective, Is.EqualTo(140));
            Assert.That(route.OptionalShardObjective, Is.EqualTo(5));
            Assert.That(route.ConsumeAnnouncement(), Is.EqualTo("THE CANOPY STIRS"));
        }

        [Test]
        public void Begin_InitializesRelayScoutObjectivesAndAnnouncements()
        {
            var route = new SharedExpeditionRouteModel();
            route.Begin("ironway.scout");

            Assert.That(route.MapId, Is.EqualTo("ironway.scout"));
            Assert.That(route.RequiredKillObjective, Is.EqualTo(155));
            Assert.That(route.OptionalShardObjective, Is.EqualTo(4));
            Assert.That(route.ConsumeAnnouncement(), Is.EqualTo("THE RELAY STIRS"));
        }

        [Test]
        public void CanopyScoutKills_UnlockBossWithHeartwoodAnnouncement()
        {
            var route = new SharedExpeditionRouteModel();
            route.Begin("oathbound.scout");

            for (var i = 0; i < 139; i++)
            {
                route.OnEnemyKilled(false, false);
            }

            Assert.That(route.CanSpawnBoss(), Is.False);

            route.OnEnemyKilled(false, false);

            Assert.That(route.CanSpawnBoss(), Is.True);
            Assert.That(route.ConsumeAnnouncement(), Is.EqualTo("THE HEARTWOOD AWAKENS"));
        }

        [Test]
        public void ResolveVictoryRelicId_GrantsHeartwoodWardenWhenSapCollected()
        {
            var route = new SharedExpeditionRouteModel();
            route.Begin("oathbound.scout");

            Assert.That(route.ResolveVictoryRelicId(), Is.EqualTo("relic.heartwood_echo"));

            for (var i = 0; i < route.OptionalShardObjective; i++)
            {
                route.OnOptionalPickupCollected();
            }

            Assert.That(route.ResolveVictoryRelicId(), Is.EqualTo("relic.heartwood_echo_warden"));
        }
    }
}
