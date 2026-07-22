using System;
using UnityEngine;

namespace ProjectExpedition
{
    public static class DevelopmentTuningService
    {
        private const string PlayerPrefsKey = "project-expedition-dev-tuning-v1";
        private static DevelopmentTuningProfileData _active;

        public static DevelopmentTuningProfileData Active
        {
            get
            {
                if (_active == null)
                {
                    Load();
                }

                return _active;
            }
        }

        public static int Revision { get; private set; }

        public static void Load()
        {
            try
            {
                var json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
                _active = string.IsNullOrEmpty(json)
                    ? DevelopmentTuningDefaults.Create()
                    : JsonUtility.FromJson<DevelopmentTuningProfileData>(json) ??
                      DevelopmentTuningDefaults.Create();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Development tuning settings could not be loaded: {exception.Message}");
                _active = DevelopmentTuningDefaults.Create();
            }

            DevelopmentTuningDefaults.Sanitize(_active);
            Revision++;
        }

        public static void Save()
        {
            if (_active == null)
            {
                _active = DevelopmentTuningDefaults.Create();
            }

            DevelopmentTuningDefaults.Sanitize(_active);
            try
            {
                PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(_active));
                PlayerPrefs.Save();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Development tuning settings could not be saved: {exception.Message}");
            }

            Revision++;
        }

        public static void ResetToDefaults()
        {
            _active = DevelopmentTuningDefaults.Create();
            Save();
        }

        public static void NotifyChanged()
        {
            DevelopmentTuningDefaults.Sanitize(_active ?? DevelopmentTuningDefaults.Create());
            Save();
        }

        public static string SerializeActiveProfile()
        {
            DevelopmentTuningDefaults.Sanitize(Active);
            return JsonUtility.ToJson(Active, true);
        }

        public static bool ExportToClipboard()
        {
            try
            {
                GUIUtility.systemCopyBuffer = SerializeActiveProfile();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Development tuning settings could not be exported: {exception.Message}");
                return false;
            }
        }

        public static void UseProfileForTests(DevelopmentTuningProfileData profile)
        {
            _active = DevelopmentTuningDefaults.Clone(profile);
            DevelopmentTuningDefaults.Sanitize(_active);
            Revision++;
        }
    }
}
