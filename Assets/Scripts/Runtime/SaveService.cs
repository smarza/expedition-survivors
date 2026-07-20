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
        public const int CurrentVersion = 2;

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
                return envelope.Progress ?? new MetaProgress();
            }

            sourceVersion = 1;
            return JsonUtility.FromJson<MetaProgress>(json) ?? new MetaProgress();
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
