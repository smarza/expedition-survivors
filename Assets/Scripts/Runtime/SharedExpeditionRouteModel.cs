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
        public int PrimaryKillCount { get; private set; }
        public int OptionalPickupsCollected { get; private set; }
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

        public int DraugrKills => PrimaryKillCount;

        public int RuneShardsCollected => OptionalPickupsCollected;

        private MapDefinition _activeMap;
        private float _elapsed;

        public void Begin(string mapId)
        {
            Begin(mapId, new ChallengeProfile(ChallengeTier.Standard, ChallengeMutator.None, ChallengeMutator.None));
        }

        public void Begin(string mapId, ChallengeProfile challenge)
        {
            _activeMap = ResolveMap(mapId);
            MapId = _activeMap.Id;
            RequiredKillObjective = SharedChallengeProfileModel.ApplyRequiredKillObjective(
                Mathf.Max(1, _activeMap.RequiredKillObjective), challenge);
            OptionalShardObjective = Mathf.Max(0, _activeMap.OptionalShardObjective);
            ExtractionDuration = Mathf.Max(1f, _activeMap.ExtractionDuration);
            ExtractionBeaconX = _activeMap.ExtractionBeaconX;
            ExtractionBeaconY = _activeMap.ExtractionBeaconY;
            BossSpawnTime = SharedChallengeProfileModel.ApplyBossSpawnTime(
                Mathf.Max(1f, _activeMap.BossSpawnTime), challenge);
            PrimaryKillCount = 0;
            OptionalPickupsCollected = 0;
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
            {
                return;
            }

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
                {
                    CurrentPhase = ExpeditionPhase.Completed;
                }

                return;
            }

            var previousPhase = CurrentPhase;
            CurrentPhase = ResolveTimedPhase(_elapsed);

            if (CurrentPhase != previousPhase)
            {
                QueueAnnouncement(PhaseAnnouncement(CurrentPhase));
            }
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
            {
                PrimaryKillCount++;
            }

            if (!BossSpawned && CanSpawnBoss())
            {
                CurrentPhase = ExpeditionPhase.Boss;
                QueueAnnouncement(PhaseAnnouncement(ExpeditionPhase.Boss));
            }
        }

        public void OnShardCollected()
        {
            OnOptionalPickupCollected();
        }

        public void OnOptionalPickupCollected()
        {
            if (OptionalShardObjective <= 0)
            {
                return;
            }

            OptionalPickupsCollected = Mathf.Min(OptionalShardObjective, OptionalPickupsCollected + 1);
        }

        public void MarkBossSpawned()
        {
            BossSpawned = true;
            CurrentPhase = ExpeditionPhase.Boss;
        }

        public bool CanSpawnBoss()
        {
            if (BossSpawned || BossKilled)
            {
                return false;
            }

            return PrimaryKillCount >= RequiredKillObjective || _elapsed >= BossSpawnTime;
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
            if (_activeMap == null || string.IsNullOrWhiteSpace(_activeMap.VictoryRelicStandardId))
            {
                return string.Empty;
            }

            if (OptionalShardObjective > 0 &&
                OptionalPickupsCollected >= OptionalShardObjective &&
                !string.IsNullOrWhiteSpace(_activeMap.VictoryRelicBonusId))
            {
                return _activeMap.VictoryRelicBonusId;
            }

            return _activeMap.VictoryRelicStandardId;
        }

        public float ObjectiveProgress =>
            RequiredKillObjective > 0 ? PrimaryKillCount / (float)RequiredKillObjective : 0f;

        private ExpeditionPhase ResolveTimedPhase(float elapsed)
        {
            if (CanSpawnBoss())
            {
                return ExpeditionPhase.Boss;
            }

            if (elapsed >= BossSpawnTime - DriftwoodSecondsBeforeBoss)
            {
                return ExpeditionPhase.WarlordApproach;
            }

            if (elapsed >= BossSpawnTime - ShorelineSecondsBeforeBoss)
            {
                return ExpeditionPhase.Driftwood;
            }

            return ExpeditionPhase.Shoreline;
        }

        private static MapDefinition ResolveMap(string mapId)
        {
            if (!string.IsNullOrWhiteSpace(mapId))
            {
                for (var i = 0; i < ContentCatalog.Maps.Length; i++)
                {
                    if (ContentCatalog.Maps[i].Id == mapId)
                    {
                        return ContentCatalog.Maps[i];
                    }
                }
            }

            return ContentCatalog.Maps[0];
        }

        private string PhaseAnnouncement(ExpeditionPhase phase)
        {
            if (_activeMap == null)
            {
                return string.Empty;
            }

            var announcements = _activeMap.PhaseAnnouncements;
            if (announcements == null || announcements.Length == 0)
            {
                return string.Empty;
            }

            switch (phase)
            {
                case ExpeditionPhase.Shoreline:
                    return announcements[0];
                case ExpeditionPhase.Driftwood:
                    return announcements.Length > 1 ? announcements[1] : announcements[0];
                case ExpeditionPhase.WarlordApproach:
                    return announcements.Length > 2 ? announcements[2] : announcements[0];
                case ExpeditionPhase.Boss:
                    return announcements.Length > 3 ? announcements[3] : announcements[0];
                case ExpeditionPhase.Extraction:
                    return announcements.Length > 4 ? announcements[4] : announcements[0];
                default:
                    return string.Empty;
            }
        }

        private void QueueAnnouncement(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            PendingAnnouncement = message;
        }
    }
}
