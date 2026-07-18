using System;
using System.IO;
using UnityEngine;

namespace ProjectExpedition
{
    public static class SaveService
    {
        private const string FileName = "project-expedition-save-v1.json";
        public static MetaProgress Data { get; private set; } = new MetaProgress();
        private static string PathName => Path.Combine(Application.persistentDataPath, FileName);

        public static void Load()
        {
            try
            {
                Data = File.Exists(PathName)
                    ? JsonUtility.FromJson<MetaProgress>(File.ReadAllText(PathName)) ?? new MetaProgress()
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
            try
            {
                var temp = PathName + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(Data, true));
                if (File.Exists(PathName)) File.Delete(PathName);
                File.Move(temp, PathName);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Save could not be written: {exception.Message}");
            }
        }
    }
}
