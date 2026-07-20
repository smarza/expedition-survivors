using System;
using UnityEngine;

namespace ProjectExpedition
{
    public enum ExpeditionPhase
    {
        Shoreline,
        Driftwood,
        WarlordApproach,
        Boss,
        Extraction,
        Completed
    }

    /// <summary>
    /// Deterministic expedition route state shared by every run adapter. Tracks
    /// phase pacing, objective progress, boss eligibility and extraction rules.
    /// </summary>
    public sealed class SharedExpeditionRouteModel
    {
        private const float ExtractionBeaconRadius = 3.5f;
        private const float ShorelineSecondsBeforeBoss = 150f;
        private const float DriftwoodSecondsBeforeBoss = 60f;

        public ExpeditionPhase CurrentPhase { get; private set; } = ExpeditionPhase.Shoreline;
        public string MapId { get; private set; } = string.Empty;
        public int DraugrKills { get; private set; }
        public int RuneShardsCollected { get; private set; }
        public int RequiredKillObjective { get; private set; }
        public int OptionalShardObjective { get; private set; }
        public float ExtractionDuration { get; private set; }
        public float ExtractionBeaconX { get; private set; }
        public float ExtractionBeaconY { get; private set; }
        public float BossSpawnTime { get; private set; }
        public float ExtractionElapsed { get; private set; }
        public bool BossSpawned { get; private set; }
        public bool BossKilled { get; private set; }
        public string PendingAnnouncement { get; private set; }

        private float _elapsed;

        public void Begin(string mapId)
        {
            var map = ResolveMap(mapId);
            MapId = map.Id;
            RequiredKillObjective = Mathf.Max(1, map.RequiredKillObjective);
            OptionalShardObjective = Mathf.Max(0, map.OptionalShardObjective);
            ExtractionDuration = Mathf.Max(1f, map.ExtractionDuration);
            ExtractionBeaconX = map.ExtractionBeaconX;
            ExtractionBeaconY = map.ExtractionBeaconY;
            BossSpawnTime = Mathf.Max(1f, map.BossSpawnTime);
            DraugrKills = 0;
            RuneShardsCollected = 0;
            ExtractionElapsed = 0f;
            BossSpawned = false;
            BossKilled = false;
            _elapsed = 0f;
            CurrentPhase = ExpeditionPhase.Shoreline;
            QueueAnnouncement(PhaseAnnouncement(ExpeditionPhase.Shoreline));
        }

        public void Advance(float elapsed, Vector2 partyCenter)
        {
            if (CurrentPhase == ExpeditionPhase.Completed)
                return;

            var deltaTime = Mathf.Max(0f, elapsed - _elapsed);
            _elapsed = Mathf.Max(0f, elapsed);

            if (BossKilled)
            {
                if (CurrentPhase != ExpeditionPhase.Extraction)
                {
                    CurrentPhase = ExpeditionPhase.Extraction;
                    ExtractionElapsed = 0f;
                    QueueAnnouncement(PhaseAnnouncement(ExpeditionPhase.Extraction));
                }
                else
                {
                    ExtractionElapsed += deltaTime;
                }

                var atBeacon = Vector2.Distance(partyCenter,
                    new Vector2(ExtractionBeaconX, ExtractionBeaconY)) <= ExtractionBeaconRadius;
                if (atBeacon || ExtractionElapsed >= ExtractionDuration)
                    CurrentPhase = ExpeditionPhase.Completed;

                return;
            }

            var previousPhase = CurrentPhase;
            CurrentPhase = ResolveTimedPhase(_elapsed);
            if (CurrentPhase != previousPhase)
                QueueAnnouncement(PhaseAnnouncement(CurrentPhase));
        }

        public void OnEnemyKilled(bool isBoss, bool isElite)
        {
            if (isBoss)
            {
                BossKilled = true;
                CurrentPhase = ExpeditionPhase.Extraction;
                ExtractionElapsed = 0f;
                QueueAnnouncement(PhaseAnnouncement(ExpeditionPhase.Extraction));
                return;
            }

            if (!isElite)
                DraugrKills++;

            if (!BossSpawned && CanSpawnBoss())
            {
                CurrentPhase = ExpeditionPhase.Boss;
                QueueAnnouncement(PhaseAnnouncement(ExpeditionPhase.Boss));
            }
        }

        public void OnShardCollected()
        {
            if (OptionalShardObjective <= 0)
                return;

            RuneShardsCollected = Mathf.Min(OptionalShardObjective, RuneShardsCollected + 1);
        }

        public void MarkBossSpawned()
        {
            BossSpawned = true;
            CurrentPhase = ExpeditionPhase.Boss;
        }

        public bool CanSpawnBoss()
        {
            if (BossSpawned || BossKilled)
                return false;

            return DraugrKills >= RequiredKillObjective || _elapsed >= BossSpawnTime;
        }

        public bool IsExtractionComplete() => CurrentPhase == ExpeditionPhase.Completed;

        public string ConsumeAnnouncement()
        {
            var message = PendingAnnouncement;
            PendingAnnouncement = null;
            return message;
        }

        public string ResolveVictoryRelicId()
        {
            if (MapId != "frostbound.scout" && !MapId.StartsWith("frostbound.", StringComparison.Ordinal))
                return string.Empty;

            if (OptionalShardObjective > 0 && RuneShardsCollected >= OptionalShardObjective)
                return "relic.jotunn_echo_warden";

            return "relic.jotunn_echo";
        }

        public float ObjectiveProgress =>
            RequiredKillObjective > 0 ? DraugrKills / (float)RequiredKillObjective : 0f;

        private ExpeditionPhase ResolveTimedPhase(float elapsed)
        {
            if (CanSpawnBoss())
                return ExpeditionPhase.Boss;

            if (elapsed >= BossSpawnTime - DriftwoodSecondsBeforeBoss)
                return ExpeditionPhase.WarlordApproach;

            if (elapsed >= BossSpawnTime - ShorelineSecondsBeforeBoss)
                return ExpeditionPhase.Driftwood;

            return ExpeditionPhase.Shoreline;
        }

        private static MapDefinition ResolveMap(string mapId)
        {
            if (!string.IsNullOrWhiteSpace(mapId))
            {
                for (var i = 0; i < ContentCatalog.Maps.Length; i++)
                {
                    if (ContentCatalog.Maps[i].Id == mapId)
                        return ContentCatalog.Maps[i];
                }
            }

            return ContentCatalog.Maps[0];
        }

        private static string PhaseAnnouncement(ExpeditionPhase phase)
        {
            switch (phase)
            {
                case ExpeditionPhase.Shoreline:
                    return "THE SHORE AWAKENS";
                case ExpeditionPhase.Driftwood:
                    return "DRIFTWOOD RUN";
                case ExpeditionPhase.WarlordApproach:
                    return "THE COAST TIGHTENS";
                case ExpeditionPhase.Boss:
                    return "THE JOTUNN HAS FOUND YOU";
                case ExpeditionPhase.Extraction:
                    return "REACH THE BEACON";
                default:
                    return string.Empty;
            }
        }

        private void QueueAnnouncement(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            PendingAnnouncement = message;
        }
    }
}
