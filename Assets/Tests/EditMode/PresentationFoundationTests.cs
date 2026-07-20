using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectExpedition.Tests
{
    public sealed class PresentationFoundationTests
    {
        [Test]
        public void Preferences_RoundTripAccessibilityAudioAndBindings()
        {
            var source = new PresentationPreferencesData
            {
                UiScale = 1.15f,
                HighContrast = true,
                ReducedFlashes = true,
                ScreenShake = 0.3f,
                MasterVolume = 0.7f,
                MusicVolume = 0.4f,
                SfxVolume = 0.9f,
                FirstRunHintsSeen = true
            };
            source.Keyboard.Set(BindingAction.Ultimate, Key.Q);

            var restored = PresentationPreferences.Deserialize(PresentationPreferences.Serialize(source));

            Assert.That(restored.UiScale, Is.EqualTo(1.15f).Within(0.0001f));
            Assert.That(restored.HighContrast, Is.True);
            Assert.That(restored.ReducedFlashes, Is.True);
            Assert.That(restored.ScreenShake, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(restored.MasterVolume, Is.EqualTo(0.7f).Within(0.0001f));
            Assert.That(restored.MusicVolume, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(restored.SfxVolume, Is.EqualTo(0.9f).Within(0.0001f));
            Assert.That(restored.FirstRunHintsSeen, Is.True);
            Assert.That(restored.Keyboard.Get(BindingAction.Ultimate), Is.EqualTo(Key.Q));
        }

        [Test]
        public void Preferences_ClampUnsafePresentationValues()
        {
            var json = "{\"UiScale\":9,\"ScreenShake\":-2,\"MasterVolume\":3,\"MusicVolume\":-1,\"SfxVolume\":2}";
            var restored = PresentationPreferences.Deserialize(json);

            Assert.That(restored.UiScale, Is.EqualTo(1.2f));
            Assert.That(restored.ScreenShake, Is.Zero);
            Assert.That(restored.MasterVolume, Is.EqualTo(1f));
            Assert.That(restored.MusicVolume, Is.Zero);
            Assert.That(restored.SfxVolume, Is.EqualTo(1f));
            Assert.That(restored.Keyboard, Is.Not.Null);
        }

        [Test]
        public void Layout_PreservesReferenceAspectInsideDesktopAndSteamDeckSafeAreas()
        {
            var desktop = PresentationLayout.Calculate(1920f, 1080f, new Rect(0f, 0f, 1920f, 1080f));
            Assert.That(desktop.Scale, Is.EqualTo(1f));
            Assert.That(desktop.Offset, Is.EqualTo(Vector3.zero));

            var deck = PresentationLayout.Calculate(1280f, 800f, new Rect(0f, 0f, 1280f, 800f));
            Assert.That(deck.Scale, Is.EqualTo(2f / 3f).Within(0.0001f));
            Assert.That(deck.Offset.x, Is.Zero.Within(0.0001f));
            Assert.That(deck.Offset.y, Is.EqualTo(40f).Within(0.0001f));

            var inset = PresentationLayout.Calculate(1280f, 800f, new Rect(40f, 20f, 1200f, 740f));
            Assert.That(inset.Offset.x, Is.GreaterThanOrEqualTo(40f));
            Assert.That(inset.Offset.y, Is.GreaterThanOrEqualTo(40f));
        }

        [Test]
        public void Glyphs_ExposeKeyboardAndControllerSpecificPrompts()
        {
            Assert.That(InputGlyphs.Prompt(BindingAction.Ultimate, InputPromptDevice.Keyboard), Does.StartWith("["));
            Assert.That(InputGlyphs.Prompt(BindingAction.Submit, InputPromptDevice.Xbox), Is.EqualTo("[A]"));
            Assert.That(InputGlyphs.Prompt(BindingAction.Submit, InputPromptDevice.PlayStation), Is.EqualTo("[CROSS]"));
            Assert.That(InputGlyphs.Prompt(BindingAction.Back, InputPromptDevice.PlayStation), Is.EqualTo("[CIRCLE]"));
            Assert.That(InputGlyphs.Prompt(BindingAction.Pause, InputPromptDevice.SteamDeck), Is.EqualTo("[MENU]"));
        }

        [Test]
        public void AudioMix_UsesMasterBusAndProtectsImportantVoices()
        {
            Assert.That(PresentationMix.LinearBusVolume(0.5f, 0.8f), Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(PresentationMix.LinearBusVolume(2f, -1f), Is.Zero);
            Assert.That(PresentationMix.Priority(PresentationCue.Ultimate),
                Is.LessThan(PresentationMix.Priority(PresentationCue.AxeThrow)));
            Assert.That(PresentationMix.Priority(PresentationCue.PlayerDowned),
                Is.LessThan(PresentationMix.Priority(PresentationCue.Navigate)));
        }

        [Test]
        public void MusicRouting_FollowsMenuRunBossRewardAndResultStates()
        {
            Assert.That(PresentationDirector.MusicFor(RunState.MainMenu, false), Is.EqualTo(PresentationMusicState.Menu));
            Assert.That(PresentationDirector.MusicFor(RunState.Playing, false), Is.EqualTo(PresentationMusicState.Expedition));
            Assert.That(PresentationDirector.MusicFor(RunState.Playing, true), Is.EqualTo(PresentationMusicState.Boss));
            Assert.That(PresentationDirector.MusicFor(RunState.LevelUp, false), Is.EqualTo(PresentationMusicState.Reward));
            Assert.That(PresentationDirector.MusicFor(RunState.Victory, true), Is.EqualTo(PresentationMusicState.Result));
        }

        [Test]
        public void PresentationAudioAssets_AreImportedForEveryMusicStateAndCue()
        {
            foreach (PresentationMusicState state in System.Enum.GetValues(typeof(PresentationMusicState)))
                Assert.That(Resources.Load<AudioClip>(PresentationAudioMixer.MusicResourcePath(state)), Is.Not.Null,
                    $"Missing imported music asset for {state}.");

            foreach (PresentationCue cue in System.Enum.GetValues(typeof(PresentationCue)))
                Assert.That(Resources.Load<AudioClip>(PresentationAudioMixer.SfxResourcePath(cue)), Is.Not.Null,
                    $"Missing imported SFX asset for {cue}.");
        }
    }
}
