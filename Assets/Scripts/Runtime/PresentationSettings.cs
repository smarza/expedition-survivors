using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectExpedition
{
    public enum BindingAction
    {
        MoveUp,
        MoveDown,
        MoveLeft,
        MoveRight,
        Ultimate,
        Submit,
        Back,
        Pause,
        BuildDetails
    }

    public enum InputPromptDevice
    {
        Keyboard,
        Xbox,
        PlayStation,
        Switch,
        SteamDeck,
        GenericGamepad,
        Touch
    }

    public enum TouchControlsMode
    {
        Auto,
        On,
        Off
    }

    [Serializable]
    public sealed class InputBindingProfile
    {
        public Key MoveUp = Key.W;
        public Key MoveDown = Key.S;
        public Key MoveLeft = Key.A;
        public Key MoveRight = Key.D;
        public Key Ultimate = Key.Space;
        public Key Submit = Key.Space;
        public Key Back = Key.Escape;
        public Key Pause = Key.Escape;
        public Key BuildDetails = Key.Tab;

        public Key Get(BindingAction action)
        {
            switch (action)
            {
                case BindingAction.MoveUp: return MoveUp;
                case BindingAction.MoveDown: return MoveDown;
                case BindingAction.MoveLeft: return MoveLeft;
                case BindingAction.MoveRight: return MoveRight;
                case BindingAction.Ultimate: return Ultimate;
                case BindingAction.Submit: return Submit;
                case BindingAction.Back: return Back;
                case BindingAction.Pause: return Pause;
                case BindingAction.BuildDetails: return BuildDetails;
                default: return Key.None;
            }
        }

        public void Set(BindingAction action, Key key)
        {
            if (key == Key.None) return;
            switch (action)
            {
                case BindingAction.MoveUp: MoveUp = key; break;
                case BindingAction.MoveDown: MoveDown = key; break;
                case BindingAction.MoveLeft: MoveLeft = key; break;
                case BindingAction.MoveRight: MoveRight = key; break;
                case BindingAction.Ultimate: Ultimate = key; break;
                case BindingAction.Submit: Submit = key; break;
                case BindingAction.Back: Back = key; break;
                case BindingAction.Pause: Pause = key; break;
                case BindingAction.BuildDetails: BuildDetails = key; break;
            }
        }

        public static string Display(Key key)
        {
            switch (key)
            {
                case Key.UpArrow: return "UP";
                case Key.DownArrow: return "DOWN";
                case Key.LeftArrow: return "LEFT";
                case Key.RightArrow: return "RIGHT";
                case Key.Space: return "SPACE";
                case Key.Enter: return "ENTER";
                case Key.Escape: return "ESC";
                case Key.LeftCtrl: return "L CTRL";
                case Key.RightCtrl: return "R CTRL";
                case Key.LeftShift: return "L SHIFT";
                case Key.RightShift: return "R SHIFT";
                default: return key.ToString().ToUpperInvariant();
            }
        }
    }

    [Serializable]
    public sealed class PresentationPreferencesData
    {
        public int Version = 1;
        public float UiScale = 1f;
        public bool HighContrast;
        public bool ReducedFlashes;
        public float ScreenShake = 0.7f;
        public float MasterVolume = 0.8f;
        public float MusicVolume = 0.55f;
        public float SfxVolume = 0.8f;
        public bool FirstRunHintsSeen;
        public int ChallengeTier;
        public int ChallengeMutatorA;
        public int ChallengeMutatorB;
        public TouchControlsMode TouchControls = TouchControlsMode.Auto;
        public InputBindingProfile Keyboard = new InputBindingProfile();
    }

    public static class PresentationPreferences
    {
        private const string PlayerPrefsKey = "project-expedition-presentation-v1";
        private static PresentationPreferencesData _data;

        public static PresentationPreferencesData Data
        {
            get
            {
                if (_data == null) Load();
                return _data;
            }
        }

        public static int Revision { get; private set; }

        public static void Load()
        {
            try
            {
                var json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
                _data = string.IsNullOrEmpty(json)
                    ? new PresentationPreferencesData()
                    : JsonUtility.FromJson<PresentationPreferencesData>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Presentation settings could not be loaded: {exception.Message}");
                _data = new PresentationPreferencesData();
            }
            Sanitize();
            Revision++;
        }

        public static void Save()
        {
            Sanitize();
            try
            {
                PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(_data));
                PlayerPrefs.Save();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Presentation settings could not be saved: {exception.Message}");
            }
            Revision++;
        }

        public static void ResetToDefaults()
        {
            _data = new PresentationPreferencesData();
            Save();
        }

        public static string Serialize(PresentationPreferencesData data) =>
            JsonUtility.ToJson(data ?? new PresentationPreferencesData());

        public static PresentationPreferencesData Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return new PresentationPreferencesData();
            var data = JsonUtility.FromJson<PresentationPreferencesData>(json) ??
                       new PresentationPreferencesData();
            if (data.Keyboard == null) data.Keyboard = new InputBindingProfile();
            data.UiScale = Mathf.Clamp(data.UiScale, 0.9f, 1.2f);
            data.ScreenShake = Mathf.Clamp01(data.ScreenShake);
            data.MasterVolume = Mathf.Clamp01(data.MasterVolume);
            data.MusicVolume = Mathf.Clamp01(data.MusicVolume);
            data.SfxVolume = Mathf.Clamp01(data.SfxVolume);
            data.ChallengeTier = Mathf.Clamp(data.ChallengeTier, 0, 1);
            data.ChallengeMutatorA = Mathf.Clamp(data.ChallengeMutatorA, 0, 4);
            data.ChallengeMutatorB = Mathf.Clamp(data.ChallengeMutatorB, 0, 4);
            return data;
        }

        private static void Sanitize()
        {
            if (_data == null) _data = new PresentationPreferencesData();
            if (_data.Keyboard == null) _data.Keyboard = new InputBindingProfile();
            _data.Version = 1;
            _data.UiScale = Mathf.Clamp(_data.UiScale, 0.9f, 1.2f);
            _data.ScreenShake = Mathf.Clamp01(_data.ScreenShake);
            _data.MasterVolume = Mathf.Clamp01(_data.MasterVolume);
            _data.MusicVolume = Mathf.Clamp01(_data.MusicVolume);
            _data.SfxVolume = Mathf.Clamp01(_data.SfxVolume);
            _data.ChallengeTier = Mathf.Clamp(_data.ChallengeTier, 0, 1);
            _data.ChallengeMutatorA = Mathf.Clamp(_data.ChallengeMutatorA, 0, 4);
            _data.ChallengeMutatorB = Mathf.Clamp(_data.ChallengeMutatorB, 0, 4);
        }
    }

    public readonly struct PresentationViewport
    {
        public readonly float Scale;
        public readonly Vector3 Offset;

        public PresentationViewport(float scale, Vector3 offset)
        {
            Scale = scale;
            Offset = offset;
        }
    }

    public static class PresentationLayout
    {
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;

        public static PresentationViewport Calculate(float screenWidth, float screenHeight, Rect safeArea)
        {
            if (safeArea.width <= 0f || safeArea.height <= 0f)
                safeArea = new Rect(0f, 0f, screenWidth, screenHeight);
            var scale = Mathf.Max(0.01f, Mathf.Min(safeArea.width / ReferenceWidth,
                safeArea.height / ReferenceHeight));
            var x = safeArea.x + (safeArea.width - ReferenceWidth * scale) * 0.5f;
            var y = screenHeight - safeArea.yMax +
                    (safeArea.height - ReferenceHeight * scale) * 0.5f;
            return new PresentationViewport(scale, new Vector3(x, y, 0f));
        }
    }

    public static class PresentationTheme
    {
        public static Color TextPrimary => PresentationPreferences.Data.HighContrast
            ? Color.white : new Color(0.82f, 0.9f, 0.93f);
        public static Color TextSecondary => PresentationPreferences.Data.HighContrast
            ? new Color(0.82f, 0.93f, 1f) : new Color(0.62f, 0.72f, 0.76f);
        public static Color Accent => PresentationPreferences.Data.HighContrast
            ? new Color(1f, 0.82f, 0.18f) : new Color(0.93f, 0.7f, 0.24f);
        public static Color Frost => new Color(0.35f, 0.9f, 1f);
        public static Color Raven => new Color(0.5f, 0.78f, 0.92f);
        public static int FontSize(int baseSize) =>
            Mathf.RoundToInt(baseSize * PresentationPreferences.Data.UiScale);
    }

    public static class InputGlyphs
    {
        public static InputPromptDevice Detect(Gamepad gamepad)
        {
            if (gamepad == null) return InputPromptDevice.Keyboard;
            var label = $"{gamepad.displayName} {gamepad.description.product} {gamepad.layout}".ToLowerInvariant();
            if (label.Contains("dualsense") || label.Contains("dualshock") || label.Contains("playstation"))
                return InputPromptDevice.PlayStation;
            if (label.Contains("switch") || label.Contains("nintendo")) return InputPromptDevice.Switch;
            if (label.Contains("steam") || label.Contains("deck")) return InputPromptDevice.SteamDeck;
            if (label.Contains("xbox") || label.Contains("xinput")) return InputPromptDevice.Xbox;
            return InputPromptDevice.GenericGamepad;
        }

        public static string Prompt(BindingAction action, InputPromptDevice device)
        {
            if (device == InputPromptDevice.Touch)
            {
                switch (action)
                {
                    case BindingAction.Submit: return "[TAP]";
                    case BindingAction.Back: return "[TAP BACK]";
                    case BindingAction.Ultimate: return "[ULT]";
                    case BindingAction.Pause: return "[PAUSE]";
                    case BindingAction.BuildDetails: return "[DETAILS]";
                    case BindingAction.MoveUp:
                    case BindingAction.MoveDown:
                    case BindingAction.MoveLeft:
                    case BindingAction.MoveRight: return "[DRAG]";
                    default: return "[TAP]";
                }
            }

            if (device == InputPromptDevice.Keyboard)
                return $"[{InputBindingProfile.Display(PresentationPreferences.Data.Keyboard.Get(action))}]";
            switch (action)
            {
                case BindingAction.Submit: return device == InputPromptDevice.PlayStation ? "[CROSS]" : "[A]";
                case BindingAction.Back: return device == InputPromptDevice.PlayStation ? "[CIRCLE]" : "[B]";
                case BindingAction.Ultimate: return "[RT]";
                case BindingAction.Pause: return device == InputPromptDevice.PlayStation ? "[OPTIONS]" : "[MENU]";
                case BindingAction.BuildDetails: return device == InputPromptDevice.PlayStation ? "[SHARE]" : "[VIEW]";
                case BindingAction.MoveUp:
                case BindingAction.MoveDown:
                case BindingAction.MoveLeft:
                case BindingAction.MoveRight: return "[L STICK / D-PAD]";
                default: return "[GAMEPAD]";
            }
        }
    }
}
