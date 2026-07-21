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

        [Test]
        public void LayoutZone_DividesHorizontalColumnsFromLeftEdge()
        {
            var parent = new LayoutZone(24f, 92f, 1872f, 760f);
            var inner = parent.Inset(12f);
            var gridWidth = CharacterSelectLayoutMetrics.SoloGridWidth(inner.Rect.width);
            var columns = inner.DivideHorizontal(
                CharacterSelectLayoutMetrics.ColumnGap,
                CharacterSelectLayoutMetrics.StatsColumnWidth,
                gridWidth,
                CharacterSelectLayoutMetrics.FilterColumnWidth);

            Assert.That(columns.Length, Is.EqualTo(3));
            Assert.That(columns[0].Rect.width, Is.EqualTo(CharacterSelectLayoutMetrics.StatsColumnWidth));
            Assert.That(columns[1].Rect.width, Is.EqualTo(gridWidth).Within(0.01f));
            Assert.That(columns[2].Rect.width, Is.EqualTo(CharacterSelectLayoutMetrics.FilterColumnWidth));
            Assert.That(columns[1].Rect.x, Is.EqualTo(columns[0].Rect.xMax + CharacterSelectLayoutMetrics.ColumnGap).Within(0.01f));
        }

        [Test]
        public void LayoutZone_DividesVerticalRowsFromTopEdge()
        {
            var parent = new LayoutZone(0f, 0f, 1920f, 1080f);
            var rows = parent.DivideVertical(16f, 72f, 772f, 168f);

            Assert.That(rows.Length, Is.EqualTo(3));
            Assert.That(rows[0].Rect.height, Is.EqualTo(72f));
            Assert.That(rows[1].Rect.y, Is.EqualTo(88f).Within(0.01f));
            Assert.That(rows[2].Rect.height, Is.EqualTo(168f));
        }

        [Test]
        public void CharacterSelectGrid_MeetsMinimumTileSizeAt1080p()
        {
            var innerBodyWidth = 1872f - 24f;
            var gridWidth = CharacterSelectLayoutMetrics.SoloGridWidth(innerBodyWidth);
            var gridHeight = 760f;
            var characterCount = ContentCatalog.Characters.Length;
            var columns = CharacterSelectLayoutMetrics.SoloGridColumns;

            Assert.That(
                CharacterSelectLayoutMetrics.MeetsMinimumTileSize(
                    gridWidth,
                    gridHeight,
                    characterCount,
                    columns,
                    PresentationSpacing.Space12),
                Is.True);
        }

        [Test]
        public void CharacterSelectGrid_MeetsMinimumTileSizeAtSteamDeckScale()
        {
            var deck = PresentationLayout.Calculate(1280f, 800f, new Rect(0f, 0f, 1280f, 800f));
            var innerBodyWidth = (1872f - 24f) * deck.Scale;
            var scaledGridWidth = CharacterSelectLayoutMetrics.SoloGridWidth(innerBodyWidth);
            var scaledGridHeight = 760f * deck.Scale;
            var characterCount = ContentCatalog.Characters.Length;
            var columns = CharacterSelectLayoutMetrics.SoloGridColumns;

            Assert.That(
                CharacterSelectLayoutMetrics.MeetsMinimumTileSize(
                    scaledGridWidth,
                    scaledGridHeight,
                    characterCount,
                    columns,
                    PresentationSpacing.Space12 * deck.Scale),
                Is.True);
        }

        [Test]
        public void PresentationTypography_UsesCompactTokenSizes()
        {
            Assert.That(PresentationTypography.BaseSize(CompactFontToken.Display), Is.EqualTo(36));
            Assert.That(PresentationTypography.BaseSize(CompactFontToken.Heading), Is.EqualTo(22));
            Assert.That(PresentationTypography.BaseSize(CompactFontToken.Body), Is.EqualTo(16));
            Assert.That(PresentationTypography.BaseSize(CompactFontToken.Caption), Is.EqualTo(13));
            Assert.That(PresentationTypography.BaseSize(CompactFontToken.Micro), Is.EqualTo(11));
        }

        [Test]
        public void PresentationTextMeasure_ReturnsNonZeroHeightForWrappedBody()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = PresentationTypography.BaseSize(CompactFontToken.Body),
                wordWrap = true
            };
            var height = PresentationTextMeasure.MeasureHeight(
                style,
                "Frostbound expedition leader with rune axe and raven mantle.",
                420f);

            Assert.That(height, Is.GreaterThan(18f));
        }

        [Test]
        public void UiArtCatalog_SanitizesStableIdsForResourcePaths()
        {
            Assert.That(UiArtCatalog.SanitizeId("weapon.frost_axe"), Is.EqualTo("weapon_frost_axe"));
            Assert.That(UiArtCatalog.SanitizeId("relic.jotunn_echo_warden"), Is.EqualTo("relic_jotunn_echo_warden"));
        }

        [Test]
        public void UiArtCatalog_FallsBackWithoutThrowingForMissingAssets()
        {
            Assert.That(UiArtCatalog.TryGetItemIcon("weapon.frost_axe", out var itemIcon), Is.False);
            Assert.That(itemIcon, Is.Null);
            Assert.That(UiArtCatalog.TryGetTitleArt(out var titleArt), Is.True);
            Assert.That(titleArt, Is.Not.Null);
        }

        [Test]
        public void MapSelectLayoutMetrics_ReserveHeaderFooterSpace()
        {
            var content = MapSelectLayoutMetrics.ContentRect(new Rect(0f, 0f, 1920f, 1080f));
            Assert.That(content.y, Is.EqualTo(MapSelectLayoutMetrics.HeaderHeight));
            Assert.That(content.height, Is.LessThan(1080f - MapSelectLayoutMetrics.HeaderHeight));
        }

        [Test]
        public void Glyphs_ExposeTouchPromptFamily()
        {
            Assert.That(InputGlyphs.Prompt(BindingAction.Submit, InputPromptDevice.Touch), Is.EqualTo("[TAP]"));
            Assert.That(InputGlyphs.Prompt(BindingAction.MoveUp, InputPromptDevice.Touch), Is.EqualTo("[DRAG]"));
        }

        [Test]
        public void MusicRouting_IncludesTitleScreenInMenuMusic()
        {
            Assert.That(PresentationDirector.MusicFor(RunState.TitleScreen, false), Is.EqualTo(PresentationMusicState.Menu));
        }

        [Test]
        public void SurvivorsHudStyles_CreateAllTypographySlots()
        {
            var styles = SurvivorsHudStyles.Create();
            Assert.That(styles.Display, Is.Not.Null);
            Assert.That(styles.Title, Is.Not.Null);
            Assert.That(styles.Hint, Is.Not.Null);
        }

        [Test]
        public void ItemCatalog_AllEntriesResolveThroughItemPresentationFallback()
        {
            for (var i = 0; i < ItemCatalog.All.Length; i++)
            {
                var item = ItemCatalog.All[i];
                Assert.That(item, Is.Not.Null);
                Assert.That(string.IsNullOrWhiteSpace(item.Id), Is.False);
            }
        }
    }
}
