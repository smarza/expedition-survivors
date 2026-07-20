using System;
using System.IO;
using UnityEngine;

namespace ProjectExpedition
{
    [Serializable]
    public sealed class SaveEnvelope
    {
        public int Version;
        public MetaProgress Progress;
    }

    public static class SaveMigration
    {
        public const int CurrentVersion = 3;

        public static MetaProgress Deserialize(string json, out int sourceVersion)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                sourceVersion = CurrentVersion;
                return new MetaProgress();
            }

            var envelope = JsonUtility.FromJson<SaveEnvelope>(json);
            if (envelope != null && envelope.Version > 0)
            {
                if (envelope.Version > CurrentVersion)
                    throw new InvalidOperationException($"Save version {envelope.Version} is newer than supported version {CurrentVersion}.");
                sourceVersion = envelope.Version;
                return Migrate(envelope.Progress ?? new MetaProgress(), sourceVersion);
            }

            sourceVersion = 1;
            return Migrate(JsonUtility.FromJson<MetaProgress>(json) ?? new MetaProgress(), sourceVersion);
        }

        private static MetaProgress Migrate(MetaProgress progress, int sourceVersion)
        {
            if (progress == null)
                progress = new MetaProgress();

            if (sourceVersion < 3 && progress.RelicsCollected == null)
                progress.RelicsCollected = new string[0];

            return progress;
        }

        public static string Serialize(MetaProgress progress) => JsonUtility.ToJson(new SaveEnvelope
        {
            Version = CurrentVersion,
            Progress = progress ?? new MetaProgress()
        }, true);
    }

    public static class SaveService
    {
        private const string FileName = "project-expedition-save-v1.json";
        public static MetaProgress Data { get; private set; } = new MetaProgress();
        internal static bool PersistenceEnabled { get; set; } = true;

        internal static void AssignDataForTests(MetaProgress progress)
        {
            Data = progress ?? new MetaProgress();
        }
        private static string PathName => Path.Combine(Application.persistentDataPath, FileName);

        public static void Load()
        {
            if (!PersistenceEnabled)
            {
                Data = new MetaProgress();
                return;
            }
            try
            {
                Data = File.Exists(PathName)
                    ? SaveMigration.Deserialize(File.ReadAllText(PathName), out _)
                    : new MetaProgress();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Save could not be loaded: {exception.Message}");
                Data = new MetaProgress();
            }
        }

        public static void RecordRun(int kills, int recoveredRenown, float time, bool victory)
        {
            Data.RunsCompleted++;
            Data.BestKills = Mathf.Max(Data.BestKills, kills);
            Data.BestTime = Mathf.Max(Data.BestTime, time);
            Data.TotalRenown += recoveredRenown + Mathf.Max(1, kills / 10) + (victory ? 50 : 0);
            Data.HaldorMastery += Mathf.Max(1, kills / 25) + (victory ? 3 : 0);
            Save();
        }

        public static void RecordRelicCollected(string relicId)
        {
            if (string.IsNullOrWhiteSpace(relicId))
                return;

            var relics = Data.RelicsCollected ?? new string[0];
            for (var i = 0; i < relics.Length; i++)
            {
                if (relics[i] == relicId)
                    return;
            }

            var updated = new string[relics.Length + 1];
            for (var i = 0; i < relics.Length; i++)
                updated[i] = relics[i];

            updated[relics.Length] = relicId;
            Data.RelicsCollected = updated;
            Save();
        }

        public static int RelicCollectionCount()
        {
            return Data.RelicsCollected?.Length ?? 0;
        }

        public static bool HasRelic(string relicId)
        {
            if (string.IsNullOrWhiteSpace(relicId))
            {
                return false;
            }

            var relics = Data.RelicsCollected;
            if (relics == null)
            {
                return false;
            }

            for (var i = 0; i < relics.Length; i++)
            {
                if (relics[i] == relicId)
                {
                    return true;
                }
            }

            return false;
        }

        public static void RecordCampLeader(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return;
            }

            Data.LastCampLeaderId = characterId;
            Save();
        }

        public static void RecordLastRunCharacters(string primaryCharacterId, string secondaryCharacterId)
        {
            if (!string.IsNullOrWhiteSpace(primaryCharacterId))
            {
                Data.LastCampLeaderId = primaryCharacterId;
            }

            if (!string.IsNullOrWhiteSpace(secondaryCharacterId))
            {
                Data.LastCoopPartnerId = secondaryCharacterId;
            }

            Save();
        }

        public static int ResolveLastCharacterSelectionIndex(int playerSlot)
        {
            var characterId = playerSlot == 0 ? Data.LastCampLeaderId : Data.LastCoopPartnerId;
            var index = ContentCatalog.CharacterIndex(characterId);
            if (index >= 0)
            {
                return index;
            }

            if (playerSlot == 0)
            {
                return 0;
            }

            return ContentCatalog.Characters.Length > 1 ? 1 : 0;
        }

        public static CharacterDefinition ResolveCampLeader()
        {
            var leader = ContentCatalog.FindCharacter(Data.LastCampLeaderId);
            if (leader != null)
            {
                return leader;
            }

            return ContentCatalog.Characters[0];
        }

        public static void Save()
        {
            if (!PersistenceEnabled) return;
            try
            {
                var temp = PathName + ".tmp";
                File.WriteAllText(temp, SaveMigration.Serialize(Data));
                if (File.Exists(PathName)) File.Delete(PathName);
                File.Move(temp, PathName);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Save could not be written: {exception.Message}");
            }
        }

        internal static void ResetForTests() => Data = new MetaProgress();
    }
}
