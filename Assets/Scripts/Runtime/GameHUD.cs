using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public sealed class GameHUD : MonoBehaviour
    {
        private enum CampView
        {
            Menu,
            Codex,
            Onboarding
        }

        private GameDirector _director;
        private GUIStyle _title;
        private GUIStyle _heading;
        private GUIStyle _body;
        private GUIStyle _small;
        private GUIStyle _button;
        private GUIStyle _center;
        private GUIStyle _cardTitle;
        private GUIStyle _itemTitle;
        private GUIStyle _micro;
        private GUIStyle _microLeft;
        private GUIStyle _badge;
        private GUIStyle _gridBadge;
        private GUIStyle _selectActionButton;
        private GUIStyle _readyActionButton;
        private GUIStyle _readyConfirmedLabel;
        private GUIStyle _mapTitle;
        private GUIStyle _resultTitle;
        private GUIStyle _resultBody;
        private GUIStyle _statSection;
        private GUIStyle _statLabel;
        private GUIStyle _statValue;
        private GUIStyle _rewardEffect;
        private GUIStyle _rewardDescription;
        private GUIStyle _rewardHint;
        private GUIStyle _rewardCategory;
        private GUIStyle _itemProgress;
        private GUIStyle _itemNext;
        private GUIStyle _buildLoadout;
        private GUIStyle _campEyebrow;
        private GUIStyle _campLedger;
        private GUIStyle _campLocked;
        private GUIStyle _campLeaderName;
        private GUIStyle _campLeaderSubtitle;
        private GUIStyle _campUnlockCost;
        private GUIStyle _campUnlockShortfall;
        private GUIStyle _campUnlockStatus;
        private string _announcement;
        private float _announcementTimer;
        private int _mainSelection;
        private readonly int[] _characterSelections = { 0, 1 };
        private readonly bool[] _characterSelected = new bool[2];
        private readonly bool[] _characterReady = new bool[2];
        private int _mapSelection;
        private int _levelSelection;
        private int _pauseSelection;
        private int _resultSelection;
        private int _settingsSelection;
        private int _styleRevision = -1;
        private int _buildDetailsWeaponScroll;
        private int _dismissedFirstRunHints;
        private CampView _campView;
        private int _unlockSelection;
        private int _codexCategorySelection;
        private int _codexEntrySelection;
        private int _onboardingStep;
        private RunState _previousRunState = RunState.MainMenu;
        private string _campPurchaseMessage;
        private float _campPurchaseMessageTimer;
        private const int MainMenuButtonCount = 4;
        private static readonly BindingAction[] RebindableActions =
        {
            BindingAction.MoveUp, BindingAction.MoveDown, BindingAction.MoveLeft,
            BindingAction.MoveRight, BindingAction.Ultimate, BindingAction.Submit,
            BindingAction.Back, BindingAction.Pause, BindingAction.BuildDetails
        };

        public void Initialize(GameDirector director) => _director = director;

        public void SetAnnouncement(string message, float duration)
        {
            _announcement = message;
            _announcementTimer = duration;
        }

        private void Update()
        {
            _announcementTimer = Mathf.Max(0f, _announcementTimer - Time.unscaledDeltaTime);
            _campPurchaseMessageTimer = Mathf.Max(0f, _campPurchaseMessageTimer - Time.unscaledDeltaTime);
            if (_director == null)
            {
                return;
            }

            var currentState = _director.State;
            if (currentState == RunState.MainMenu &&
                (_previousRunState == RunState.Victory || _previousRunState == RunState.GameOver))
            {
                FocusAffordableUnlockIfAny();
            }

            _previousRunState = currentState;

            switch (currentState)
            {
                case RunState.MainMenu: UpdateMainMenu(); break;
                case RunState.CharacterSelect: UpdateCharacterSelect(); break;
                case RunState.MapSelect: UpdateMapSelect(); break;
                case RunState.LevelUp: UpdateLevelUp(); break;
                case RunState.Paused: UpdatePause(); break;
                case RunState.Settings: UpdateSettings(); break;
                case RunState.GameOver:
                case RunState.Victory: UpdateResults(); break;
            }
        }

        private void UpdateMainMenu()
        {
            if (_campView == CampView.Onboarding)
            {
                UpdateCampOnboarding();
                return;
            }

            if (_campView == CampView.Codex)
            {
                UpdateCodexView();
                return;
            }

            var vertical = LocalInputRouter.AnyMenuVerticalPressed();
            if (vertical != 0)
            {
                CycleUnlockSelection(vertical);
            }

            var direction = LocalInputRouter.AnyMenuHorizontalPressed();
            if (direction != 0)
            {
                _unlockSelection = -1;
                _mainSelection = Wrap(_mainSelection + direction, MainMenuButtonCount);
                NavigateCue();
            }

            if (!LocalInputRouter.AnyMenuSubmitPressed())
            {
                return;
            }

            if (_unlockSelection >= 0)
            {
                TryPurchaseUnlockAt(_unlockSelection);
                return;
            }

            ActivateMainSelection();
        }

        private void UpdateCampOnboarding()
        {
            if (LocalInputRouter.AnyMenuSubmitPressed())
            {
                _onboardingStep++;
                if (_onboardingStep >= CampOnboardingMessages.Length)
                {
                    SaveService.CompleteCampOnboarding();
                    _campView = CampView.Menu;
                    _onboardingStep = 0;
                }

                ConfirmCue();
            }
        }

        private void UpdateCodexView()
        {
            if (LocalInputRouter.MenuBackPressed())
            {
                _campView = CampView.Menu;
                return;
            }

            var categories = CodexCategoryCount();
            var vertical = LocalInputRouter.AnyMenuVerticalPressed();
            if (vertical != 0)
            {
                _codexCategorySelection = Wrap(_codexCategorySelection + vertical, categories);
                _codexEntrySelection = 0;
                NavigateCue();
            }

            var entryCount = CountVisibleCodexEntries(_codexCategorySelection);
            var horizontal = LocalInputRouter.AnyMenuHorizontalPressed();
            if (horizontal != 0 && entryCount > 0)
            {
                _codexEntrySelection = Wrap(_codexEntrySelection + horizontal, entryCount);
                NavigateCue();
            }
        }

        private void ActivateMainSelection()
        {
            if (_mainSelection == 0)
            {
                PrepareCharacterSelection(1);
            }
            else if (_mainSelection == 1)
            {
                PrepareCharacterSelection(2);
            }
            else if (_mainSelection == 2)
            {
                _campView = CampView.Codex;
                _codexCategorySelection = 0;
                _codexEntrySelection = 0;
            }
            else
            {
                _director.OpenSettings();
            }
        }

        private void PrepareCharacterSelection(int playerCount)
        {
            ApplySavedCharacterSelections(playerCount);
            _characterSelected[0] = false;
            _characterSelected[1] = false;
            _characterReady[0] = false;
            _characterReady[1] = false;
            _director.BeginRunSetup(playerCount);
        }

        private void ApplySavedCharacterSelections(int playerCount)
        {
            _characterSelections[0] = SaveService.ResolveLastCharacterSelectionIndex(0);

            if (playerCount <= 1)
            {
                return;
            }

            _characterSelections[1] = SaveService.ResolveLastCharacterSelectionIndex(1);
        }

        private bool TryConfirmCharacterSelect(int player, int playerCount)
        {
            var index = _characterSelections[player];
            if (!SaveService.IsCharacterUnlocked(index))
            {
                return false;
            }

            _characterSelected[player] = true;
            ConfirmCue();

            if (playerCount > 1)
            {
                TryAdvanceCharacterSelectionIfComplete(playerCount);
            }

            return true;
        }

        private bool TryConfirmCharacterReady(int player, int playerCount)
        {
            if (!_characterSelected[player])
            {
                return false;
            }

            _characterReady[player] = true;
            ConfirmCue();

            if (playerCount == 1)
            {
                AdvanceToMapSelect();
            }

            return true;
        }

        private void TryAdvanceCharacterSelectionIfComplete(int playerCount)
        {
            for (var player = 0; player < playerCount; player++)
            {
                if (!_characterSelected[player])
                {
                    return;
                }
            }

            AdvanceToMapSelect();
        }

        private void AdvanceToMapSelect()
        {
            _mapSelection = SharedMetaProgressionModel.FirstUnlockedMapIndex(SaveService.Data);
            _director.ConfirmCharacters(_characterSelections[0], _characterSelections[1]);
        }

        private static bool IsLastPlayedCharacter(int playerIndex, int selectedIndex)
        {
            var characterId = ContentCatalog.Character(selectedIndex).Id;
            if (playerIndex == 0)
            {
                return characterId == SaveService.Data.LastCampLeaderId;
            }

            return characterId == SaveService.Data.LastCoopPartnerId;
        }

        private void UpdateCharacterSelect()
        {
            if (LocalInputRouter.MenuBackPressed())
            {
                _director.ReturnToMenu();
                return;
            }

            var playerCount = _director.PendingPlayerCount;
            for (var player = 0; player < playerCount; player++)
            {
                if (playerCount > 1 && _characterSelected[player])
                {
                    continue;
                }

                if (playerCount == 1 && _characterReady[player])
                {
                    continue;
                }

                if (!_characterSelected[player])
                {
                    var horizontal = LocalInputRouter.MenuHorizontalPressed(player, playerCount);
                    var vertical = LocalInputRouter.MenuVerticalPressed(player, playerCount);
                    if (horizontal != 0 || vertical != 0)
                    {
                        _characterSelections[player] = SharedMetaProgressionModel.NextCharacterIndex(
                            _characterSelections[player], horizontal, vertical);
                        NavigateCue();
                    }
                }

                if (LocalInputRouter.MenuSubmitPressed(player, playerCount))
                {
                    if (!_characterSelected[player])
                    {
                        TryConfirmCharacterSelect(player, playerCount);
                    }
                    else if (playerCount == 1)
                    {
                        TryConfirmCharacterReady(player, playerCount);
                    }
                }
            }
        }

        private void UpdateMapSelect()
        {
            if (LocalInputRouter.MenuBackPressed())
            {
                PrepareCharacterSelection(_director.PendingPlayerCount);
                return;
            }

            var playerCount = _director.PendingPlayerCount;
            var horizontal = LocalInputRouter.MenuHorizontalPressed(0, playerCount);
            var vertical = LocalInputRouter.MenuVerticalPressed(0, playerCount);
            if (horizontal != 0 || vertical != 0)
            {
                _mapSelection = SharedMetaProgressionModel.NextMapIndex(_mapSelection, horizontal, vertical);
                NavigateCue();
            }

            if (LocalInputRouter.MenuSubmitPressed(0, playerCount))
            {
                TryBeginSelectedMap();
            }
        }

        private void TryBeginSelectedMap()
        {
            if (!SaveService.IsMapUnlocked(_mapSelection))
            {
                return;
            }

            _director.SelectMapAndStart(_mapSelection);
            ConfirmCue();
        }

        private void UpdateLevelUp()
        {
            var playerCount = Mathf.Max(1, _director.Players.Count);
            var player = Mathf.Clamp(_director.RewardTurnPlayerIndex, 0, playerCount - 1);
            var directChoice = LocalInputRouter.LevelChoicePressed(player, playerCount);
            if (directChoice >= 0 && directChoice < _director.CurrentRewards.Count)
            {
                _director.ChooseReward(directChoice);
                return;
            }
            var direction = LocalInputRouter.MenuHorizontalPressed(player, playerCount);
            if (direction != 0) _levelSelection = Wrap(_levelSelection + direction, _director.CurrentRewards.Count);
            if (LocalInputRouter.MenuSubmitPressed(player, playerCount)) _director.ChooseReward(_levelSelection);
        }

        private void UpdatePause()
        {
            var direction = LocalInputRouter.AnyMenuVerticalPressed();
            if (direction != 0) { _pauseSelection = Wrap(_pauseSelection + direction, 3); NavigateCue(); }
            if (!LocalInputRouter.AnyMenuSubmitPressed()) return;
            if (_pauseSelection == 0) _director.TogglePause();
            else if (_pauseSelection == 1) _director.OpenSettings();
            else _director.ReturnToMenu();
        }

        private void UpdateSettings()
        {
            if (LocalInputRouter.IsRebinding)
            {
                if (LocalInputRouter.PollRebind()) ConfirmCue();
                return;
            }
            if (LocalInputRouter.MenuBackPressed())
            {
                _director.CloseSettings();
                return;
            }
            const int itemCount = 18;
            var vertical = LocalInputRouter.AnyMenuVerticalPressed();
            if (vertical != 0) { _settingsSelection = Wrap(_settingsSelection + vertical, itemCount); NavigateCue(); }
            var horizontal = LocalInputRouter.AnyMenuHorizontalPressed();
            if (horizontal != 0 && _settingsSelection < 7) AdjustSetting(_settingsSelection, horizontal);
            if (!LocalInputRouter.AnyMenuSubmitPressed()) return;
            if (_settingsSelection >= 7 && _settingsSelection < 16)
                LocalInputRouter.BeginRebind(RebindableActions[_settingsSelection - 7]);
            else if (_settingsSelection == 16)
            {
                PresentationPreferences.ResetToDefaults();
                ConfirmCue();
            }
            else if (_settingsSelection == 17) _director.CloseSettings();
        }

        private static void AdjustSetting(int index, int direction)
        {
            var data = PresentationPreferences.Data;
            switch (index)
            {
                case 0: data.UiScale = Mathf.Clamp(data.UiScale + direction * 0.05f, 0.9f, 1.2f); break;
                case 1: data.HighContrast = !data.HighContrast; break;
                case 2: data.ReducedFlashes = !data.ReducedFlashes; break;
                case 3: data.ScreenShake = Mathf.Clamp01(data.ScreenShake + direction * 0.1f); break;
                case 4: data.MasterVolume = Mathf.Clamp01(data.MasterVolume + direction * 0.1f); break;
                case 5: data.MusicVolume = Mathf.Clamp01(data.MusicVolume + direction * 0.1f); break;
                case 6: data.SfxVolume = Mathf.Clamp01(data.SfxVolume + direction * 0.1f); break;
            }
            PresentationPreferences.Save();
        }

        private void UpdateResults()
        {
            var direction = LocalInputRouter.AnyMenuHorizontalPressed();
            if (direction != 0) _resultSelection = Wrap(_resultSelection + direction, 2);
            if (!LocalInputRouter.AnyMenuSubmitPressed()) return;
            if (_resultSelection == 0) _director.ReplayRun();
            else _director.ReturnToMenu();
        }

        private void OnGUI()
        {
            if (_director == null) return;
            EnsureStyles();
            var oldMatrix = GUI.matrix;
            var viewport = PresentationLayout.Calculate(Screen.width, Screen.height, Screen.safeArea);
            DrawLetterbox(viewport.Offset, viewport.Scale);
            GUI.matrix = Matrix4x4.TRS(viewport.Offset, Quaternion.identity,
                new Vector3(viewport.Scale, viewport.Scale, 1f));

            switch (_director.State)
            {
                case RunState.MainMenu: DrawMainMenu(); break;
                case RunState.CharacterSelect: DrawCharacterSelect(); break;
                case RunState.MapSelect: DrawMapSelect(); break;
                case RunState.Playing: DrawRunHud(); break;
                case RunState.LevelUp: DrawLevelUp(); break;
                case RunState.BuildDetails: DrawBuildDetails(); break;
                case RunState.Paused: DrawPause(); break;
                case RunState.Settings: DrawSettings(); break;
                case RunState.GameOver: DrawResults(false); break;
                case RunState.Victory: DrawResults(true); break;
            }

            if (_announcementTimer > 0f && _director.State == RunState.Playing)
            {
                DrawPanel(new Rect(440, 925, 1040, 78), new Color(0.018f, 0.045f, 0.062f, 0.97f));
                DrawBorder(new Rect(440, 925, 1040, 78), new Color(0.32f, 0.68f, 0.78f, 0.9f), 3f);
                GUI.Label(new Rect(470, 939, 980, 50), _announcement, _center);
            }
            GUI.matrix = oldMatrix;
            GUI.color = Color.white;
        }

        private static readonly string[] KnownRelicIds = { "relic.jotunn_echo", "relic.jotunn_echo_warden" };

        private void DrawMainMenu()
        {
            if (!SaveService.Data.CampOnboardingComplete && _campView == CampView.Menu)
            {
                _campView = CampView.Onboarding;
                _onboardingStep = 0;
            }

            if (_campView == CampView.Codex)
            {
                DrawCodexView();
                return;
            }

            var campLeader = SaveService.ResolveCampLeader();

            DrawCampAtmosphere();

            GUI.Label(new Rect(80, 34, 1760, 78), "PROJECT EXPEDITION", _title);
            GUI.Label(new Rect(84, 108, 1760, 38),
                "FROSTBOUND CAMP — REST BY THE CONVERGENCE FIRE, THEN CHOOSE YOUR NEXT ROUTE", _small);

            DrawCampLedger(new Rect(80, 156, 1760, 58));

            var campPanel = new Rect(80, 232, 1760, 598);
            DrawPanel(campPanel, new Color(0.045f, 0.085f, 0.115f, 0.94f));
            DrawBorder(campPanel, new Color(0.22f, 0.48f, 0.58f, 0.55f), 3f);
            DrawPanel(new Rect(campPanel.x, campPanel.y, campPanel.width, 6), new Color(0.92f, 0.58f, 0.18f, 0.85f));

            DrawCampLeaderSpotlight(new Rect(campPanel.x + 30, campPanel.y + 34, 520, 530), campLeader);
            DrawCampBriefing(new Rect(campPanel.x + 580, campPanel.y + 34, 1150, 218), campLeader);
            DrawCampUnlockBoard(new Rect(campPanel.x + 580, campPanel.y + 266, 1150, 278));
            DrawCampRelicVault(new Rect(campPanel.x + 580, campPanel.y + 550, 1150, 48));

            DrawSelectableButton(new Rect(430, 862, 280, 88), "SOLO", 0, ref _mainSelection, PrepareSolo);
            DrawSelectableButton(new Rect(735, 862, 280, 88), "LOCAL CO-OP", 1, ref _mainSelection, PrepareLocal);
            DrawSelectableButton(new Rect(1040, 862, 280, 88), "CODEX", 2, ref _mainSelection, OpenCodexFromButton);
            DrawSelectableButton(new Rect(1345, 862, 280, 88), "SETTINGS", 3, ref _mainSelection, _director.OpenSettings);
            GUI.Label(new Rect(430, 962, 1195, 55),
                $"↑↓ SELECT UNLOCK ON THE BOARD   •   {Prompt(BindingAction.Submit)} PURCHASE   •   ←→ CAMP MENU", _small);

            if (_campView == CampView.Onboarding)
            {
                DrawCampOnboarding();
            }
        }

        private void OpenCodexFromButton()
        {
            _campView = CampView.Codex;
            _codexCategorySelection = 0;
            _codexEntrySelection = 0;
        }

        private static readonly string[] CampOnboardingMessages =
        {
            "YOUR SPENDABLE RENOWN appears in the ledger at the top — this is the currency you use at camp.",
            "OPEN THE UNLOCK BOARD below the briefing. Select a survivor or route with ↑↓, press Confirm, or click UNLOCK.",
            "THE RELIC VAULT stores trophies earned from Scout victories and optional objectives.",
            "THE CODEX records weapons, gear, evolutions and relics discovered during your runs."
        };

        private void DrawCampOnboarding()
        {
            var message = CampOnboardingMessages[Mathf.Clamp(_onboardingStep, 0, CampOnboardingMessages.Length - 1)];
            var panel = new Rect(460, 380, 1000, 220);
            DrawPanel(new Rect(0, 0, 1920, 1080), new Color(0.01f, 0.02f, 0.03f, 0.72f));
            DrawPanel(panel, new Color(0.055f, 0.105f, 0.145f, 1f));
            DrawBorder(panel, PresentationTheme.Accent, 4f);
            GUI.Label(new Rect(panel.x + 36, panel.y + 24, panel.width - 72, 36), "WELCOME TO FROSTBOUND CAMP", _campEyebrow);
            GUI.Label(new Rect(panel.x + 36, panel.y + 68, panel.width - 72, 90), message, _body);
            GUI.Label(new Rect(panel.x + 36, panel.y + 168, panel.width - 72, 34),
                $"{Prompt(BindingAction.Submit)} CONTINUE ({_onboardingStep + 1}/{CampOnboardingMessages.Length})", _small);
        }

        private void DrawCampLeaderSpotlight(Rect spotlight, CharacterDefinition leader)
        {
            const float outerPad = 16f;
            const float headerHeight = 38f;
            const float footerHeight = 108f;

            DrawPanel(spotlight, new Color(0.07f, 0.13f, 0.17f, 1f));
            DrawBorder(spotlight, leader.Color * 0.85f + PresentationTheme.Accent * 0.15f, 4f);

            var inner = new Rect(
                spotlight.x + outerPad,
                spotlight.y + outerPad,
                spotlight.width - outerPad * 2f,
                spotlight.height - outerPad * 2f);

            var headerRect = new Rect(inner.x, inner.y, inner.width, headerHeight);
            DrawPanel(headerRect, new Color(0.025f, 0.055f, 0.075f, 1f));
            DrawPanel(new Rect(headerRect.x, headerRect.yMax - 2f, headerRect.width, 2f), leader.Color * 0.75f);
            GUI.Label(new Rect(headerRect.x + 14, headerRect.y + 6, headerRect.width - 28, 24), "CAMP LEADER", _campEyebrow);

            var footerRect = new Rect(inner.x, inner.yMax - footerHeight, inner.width, footerHeight);
            DrawPanel(footerRect, new Color(0.025f, 0.055f, 0.075f, 0.98f));
            DrawPanel(new Rect(footerRect.x, footerRect.y, footerRect.width, 2f), leader.Color * 0.75f);

            var glowColor = leader.Color * 0.55f + new Color(0.92f, 0.48f, 0.12f, 1f) * 0.45f;
            var portraitRect = new Rect(inner.x, headerRect.yMax + 10f, inner.width, footerRect.y - headerRect.yMax - 20f);
            DrawPanel(new Rect(portraitRect.x, portraitRect.yMax - 96f, portraitRect.width, 96f),
                new Color(glowColor.r, glowColor.g, glowColor.b, 0.14f));
            DrawCampLeaderPortrait(portraitRect, leader);

            GUI.Label(new Rect(footerRect.x + 14, footerRect.y + 12, footerRect.width - 28, 34),
                leader.Name.ToUpperInvariant(), _campLeaderName);
            GUI.Label(new Rect(footerRect.x + 14, footerRect.y + 46, footerRect.width - 28, 52),
                $"{leader.Tribe.ToUpperInvariant()} — {leader.Role.ToUpperInvariant()}", _campLeaderSubtitle);
        }

        private void DrawCampLeaderPortrait(Rect portraitRect, CharacterDefinition leader)
        {
            CharacterSelectPresentation.DrawPortrait(
                portraitRect, leader, true, CharacterPortraitSize.Large);
        }

        private void DrawCampBriefing(Rect briefing, CharacterDefinition leader)
        {
            DrawPanel(briefing, new Color(0.055f, 0.105f, 0.145f, 1f));
            DrawPanel(new Rect(briefing.x, briefing.y, briefing.width, 4), leader.Color);
            GUI.Label(new Rect(briefing.x + 28, briefing.y + 16, briefing.width - 56, 24), "TONIGHT'S BRIEFING", _campEyebrow);
            GUI.Label(new Rect(briefing.x + 28, briefing.y + 44, briefing.width - 56, 52),
                CampBriefHeadline(leader), _heading);
            GUI.Label(new Rect(briefing.x + 28, briefing.y + 102, briefing.width - 56, 92), leader.Description, _body);
            GUI.Label(new Rect(briefing.x + 28, briefing.y + 204, briefing.width - 56, 30), "SIGNATURE KIT", _cardTitle);
            GUI.Label(new Rect(briefing.x + 42, briefing.y + 238, briefing.width - 72, 108),
                CampSignatureKit(leader), _body);
        }

        private static void DrawCampAtmosphere()
        {
            DrawPanel(new Rect(0, 0, 1920, 1080), new Color(0.018f, 0.045f, 0.065f, 1f));
            DrawPanel(new Rect(0, 0, 1920, 240), new Color(0.05f, 0.16f, 0.2f, 0.55f));
            DrawPanel(new Rect(0, 70, 1920, 110), new Color(0.1f, 0.34f, 0.38f, 0.18f));
            DrawPanel(new Rect(0, 780, 1920, 300), new Color(0.012f, 0.028f, 0.04f, 1f));
            DrawPanel(new Rect(120, 710, 500, 220), new Color(0.78f, 0.36f, 0.08f, 0.08f));
            DrawPanel(new Rect(220, 790, 300, 130), new Color(0.96f, 0.58f, 0.14f, 0.12f));
        }

        private void DrawCampLedger(Rect rect)
        {
            DrawPanel(rect, new Color(0.025f, 0.055f, 0.075f, 0.96f));
            DrawPanel(new Rect(rect.x, rect.y, rect.width, 3), new Color(0.32f, 0.68f, 0.78f, 0.9f));
            GUI.Label(new Rect(rect.x + 24, rect.y + 8, 180, 34), "CAMP LEDGER", _campEyebrow);

            var progress = SaveService.Data;
            var available = SaveService.AvailableRenown();

            var balanceRect = new Rect(rect.x + 200, rect.y + 6, 300, 46);
            DrawPanel(balanceRect, new Color(0.07f, 0.12f, 0.1f, 1f));
            DrawBorder(balanceRect, PresentationTheme.Accent, 3f);
            GUI.Label(new Rect(balanceRect.x + 14, balanceRect.y + 4, balanceRect.width - 28, 18),
                "SPEND AT UNLOCK BOARD", _campEyebrow);
            GUI.Label(new Rect(balanceRect.x + 14, balanceRect.y + 20, balanceRect.width - 28, 24),
                $"{available} RENOWN", _cardTitle);

            var ledgerText =
                $"Lifetime earned {progress.TotalRenown}     •     Mastery H{progress.HaldorMastery} S{progress.SylvaMastery} M{progress.MaraMastery} E{progress.EiraMastery}     •     Expeditions {progress.RunsCompleted}     •     Relics {SaveService.RelicCollectionCount()} / {KnownRelicIds.Length}";
            GUI.Label(new Rect(rect.x + 520, rect.y + 12, rect.width - 544, 40), ledgerText, _campLedger);
        }

        private void DrawCampUnlockBoard(Rect rect)
        {
            DrawPanel(rect, new Color(0.018f, 0.048f, 0.068f, 1f));
            DrawPanel(new Rect(rect.x, rect.y, rect.width, 4), new Color(0.32f, 0.68f, 0.78f, 0.85f));
            GUI.Label(new Rect(rect.x + 22, rect.y + 10, rect.width - 44, 24), "UNLOCK BOARD — SPEND RENOWN HERE", _campEyebrow);
            GUI.Label(new Rect(rect.x + 22, rect.y + 34, rect.width - 44, 22),
                "↑↓ SELECT A ROW   •   CONFIRM OR CLICK THE BADGE TO UNLOCK", _microLeft);

            var affordableHighlight = SharedMetaProgressionModel.FindCheapestAffordableUnlockId(SaveService.Data);
            var purchasableIndex = 0;
            const float rowHeight = 42f;
            const float rowGap = 4f;
            const float rowTop = 62f;
            const float actionWidth = 172f;

            for (var i = 0; i < SharedMetaProgressionModel.Unlocks.Count; i++)
            {
                var unlock = SharedMetaProgressionModel.Unlocks[i];
                if (unlock.RenownCost <= 0)
                {
                    continue;
                }

                var rowY = rect.y + rowTop + purchasableIndex * (rowHeight + rowGap);
                var rowRect = new Rect(rect.x + 18, rowY, rect.width - 36, rowHeight);
                var unlocked = SaveService.IsUnlocked(unlock.ContentId);
                var canAfford = SaveService.CanPurchaseUnlock(unlock.ContentId);
                var selected = purchasableIndex == _unlockSelection;
                var highlightGoal = unlock.ContentId == affordableHighlight && canAfford;

                if (selected || highlightGoal)
                {
                    DrawBorder(rowRect, highlightGoal ? PresentationTheme.Accent : new Color(0.93f, 0.7f, 0.24f), 3f);
                }

                DrawPanel(rowRect, unlocked
                    ? new Color(0.06f, 0.1f, 0.08f, 1f)
                    : canAfford
                        ? new Color(0.05f, 0.09f, 0.12f, 1f)
                        : new Color(0.03f, 0.05f, 0.07f, 1f));

                var nameWidth = rowRect.width - actionWidth - 28f;
                GUI.Label(new Rect(rowRect.x + 14, rowRect.y + 8, nameWidth, 28),
                    unlock.DisplayName.ToUpperInvariant(), unlocked ? _itemTitle : _campLocked);

                DrawCampUnlockRowAction(rowRect, actionWidth, unlocked, canAfford, selected, unlock);

                purchasableIndex++;
            }

            var footerY = rect.yMax - 28f;
            if (_campPurchaseMessageTimer > 0f)
            {
                GUI.Label(new Rect(rect.x + 22, footerY, rect.width - 44, 24), _campPurchaseMessage, _microLeft);
            }
            else if (!string.IsNullOrEmpty(affordableHighlight))
            {
                var unlock = SharedMetaProgressionModel.FindUnlock(affordableHighlight);
                var label = unlock.HasValue
                    ? $"READY — {unlock.Value.RenownCost} RENOWN · {CompactUnlockName(unlock.Value.DisplayName)}"
                    : "SELECT A HIGHLIGHTED ROW TO SPEND RENOWN";
                GUI.Label(new Rect(rect.x + 22, footerY, rect.width - 44, 24), label, _microLeft);
            }
            else if (SaveService.AvailableRenown() > 0)
            {
                GUI.Label(new Rect(rect.x + 22, footerY, rect.width - 44, 24),
                    "SAVE RENOWN — THE NEXT UNLOCK COSTS MORE THAN YOUR BALANCE.", _microLeft);
            }
            else
            {
                GUI.Label(new Rect(rect.x + 22, footerY, rect.width - 44, 24),
                    "FINISH EXPEDITIONS TO EARN RENOWN, THEN RETURN HERE TO UNLOCK.", _microLeft);
            }
        }

        private void DrawCampUnlockRowAction(
            Rect rowRect,
            float actionWidth,
            bool unlocked,
            bool canAfford,
            bool selected,
            UnlockDefinition unlock)
        {
            var actionRect = new Rect(rowRect.xMax - actionWidth - 10f, rowRect.y + 4f, actionWidth, rowRect.height - 8f);
            var lineHeight = (actionRect.height - 2f) * 0.5f;

            if (unlocked)
            {
                GUI.Label(actionRect, "UNLOCKED", _campUnlockStatus);
                return;
            }

            if (canAfford)
            {
                DrawPanel(actionRect, new Color(0.92f, 0.58f, 0.14f, selected ? 1f : 0.92f));

                if (selected)
                {
                    DrawBorder(actionRect, PresentationTheme.Accent, 2f);
                }

                GUI.Label(new Rect(actionRect.x, actionRect.y + 1f, actionWidth, lineHeight), "UNLOCK", _gridBadge);
                GUI.Label(new Rect(actionRect.x, actionRect.y + 1f + lineHeight, actionWidth, lineHeight),
                    $"{unlock.RenownCost} RENOWN", _badge);

                if (GUI.Button(actionRect, GUIContent.none, GUIStyle.none))
                {
                    TryPurchaseUnlockByContentId(unlock.ContentId);
                }

                return;
            }

            var shortfall = unlock.RenownCost - SaveService.AvailableRenown();
            DrawPanel(actionRect, new Color(0.035f, 0.055f, 0.07f, 0.95f));
            GUI.Label(new Rect(actionRect.x, actionRect.y + 1f, actionWidth, lineHeight),
                $"{unlock.RenownCost} RENOWN", _campUnlockCost);
            GUI.Label(new Rect(actionRect.x, actionRect.y + 1f + lineHeight, actionWidth, lineHeight),
                $"NEED {shortfall} MORE", _campUnlockShortfall);
        }

        private static string CompactUnlockName(string displayName)
        {
            var upper = displayName.ToUpperInvariant();
            var colonIndex = upper.IndexOf(':');

            if (colonIndex >= 0 && colonIndex < upper.Length - 2)
            {
                return upper.Substring(colonIndex + 1).Trim();
            }

            if (upper.Length > 28)
            {
                return upper.Substring(0, 25) + "...";
            }

            return upper;
        }

        private void DrawCampRelicVault(Rect rect)
        {
            DrawPanel(rect, new Color(0.018f, 0.048f, 0.068f, 1f));
            DrawPanel(new Rect(rect.x, rect.y, rect.width, 4), PresentationTheme.Accent);
            GUI.Label(new Rect(rect.x + 22, rect.y + 10, 140, 24), "RELIC VAULT", _campEyebrow);

            var collectedCount = SaveService.RelicCollectionCount();
            var summary = collectedCount == 0
                ? "No relics yet — clear the Scout expedition and reach extraction."
                : $"{collectedCount} of {KnownRelicIds.Length} relics secured.";
            GUI.Label(new Rect(rect.x + 170, rect.y + 10, rect.width - 192, 28), summary, _microLeft);
        }

        private static string CampBriefHeadline(CharacterDefinition leader)
        {
            switch (leader.Id)
            {
                case "ravenbound.haldor":
                    return "THE RAVEN'S FAVORITE";
                case "ravenbound.eira":
                    return "THE STORM SCOUT";
                case "oathbound.sylva":
                    return "THE CANOPY WARDEN";
                case "ironway.mara":
                    return "THE FIELD CAPTAIN";
                default:
                    return leader.Role.ToUpperInvariant();
            }
        }

        private static string CampSignatureKit(CharacterDefinition leader)
        {
            var kit = string.Empty;
            var weapons = leader.StarterWeaponIds;
            for (var i = 0; i < weapons.Length; i++)
            {
                if (kit.Length > 0)
                {
                    kit += "\n";
                }

                kit += $"• {CampWeaponLine(weapons[i])}";
            }

            if (kit.Length > 0)
            {
                kit += "\n";
            }

            kit += $"• {leader.UltimateName} — {CampUltimateBlurb(leader.UltimateDescription)}";
            kit += $"\n• Field kit — {leader.MaxHealth:0} HP, {leader.MoveSpeed:0.0} speed, {leader.Armor:0.0} armor";
            return kit;
        }

        private static string CampWeaponLine(string weaponId)
        {
            var item = ItemCatalog.Find(weaponId);
            var weaponName = item != null ? item.Name : weaponId;

            switch (weaponId)
            {
                case "weapon.frost_axe":
                    return "Frost Axe — automatic rune-axe throws";
                case "weapon.raven_guard":
                    return "Raven Guard — automatic shield shockwave";
                case "weapon.grove_thorn_lash":
                    return "Grove Thorn Lash — fast thorn pulse around Sylva";
                case "weapon.canopy_vortex":
                    return "Canopy Vortex — radial canopy burst";
                case "weapon.signal_flare":
                    return "Signal Flare — explosive signal projectile";
                case "weapon.supply_pulse":
                    return "Supply Pulse — periodic heal pulse";
                default:
                    return $"{weaponName} — starter automatic weapon";
            }
        }

        private static string CampUltimateBlurb(string ultimateDescription)
        {
            if (string.IsNullOrWhiteSpace(ultimateDescription))
            {
                return "manual high-impact Ultimate";
            }

            var periodIndex = ultimateDescription.IndexOf('.');
            if (periodIndex > 0)
            {
                return ultimateDescription.Substring(0, periodIndex).ToLowerInvariant();
            }

            return ultimateDescription.ToLowerInvariant();
        }

        private static string RelicUnlockHint(string relicId)
        {
            switch (relicId)
            {
                case "relic.jotunn_echo_warden":
                    return "SCOUT + ALL SHARDS";
                case "relic.jotunn_echo":
                    return "SCOUT VICTORY";
                default:
                    return "LOCKED";
            }
        }

        private void PrepareSolo() => PrepareCharacterSelection(1);
        private void PrepareLocal() => PrepareCharacterSelection(2);

        private void DrawCharacterSelect()
        {
            DrawPanel(new Rect(0, 0, 1920, 1080), new Color(0.025f, 0.06f, 0.09f, 1f));
            GUI.Label(new Rect(420, 55, 1080, 80), "CHOOSE YOUR SURVIVOR", _title);
            GUI.Label(new Rect(360, 135, 1200, 45),
                "Select from the roster — locked survivors can be viewed, but only unlocked ones can be selected.",
                _small);

            var playerCount = _director.PendingPlayerCount;
            if (playerCount == 1)
            {
                DrawCharacterSelectPlayerPanel(new Rect(120, 195, 1680, 725), 0, playerCount);
            }
            else
            {
                DrawCharacterSelectPlayerPanel(new Rect(40, 195, 900, 725), 0, playerCount);
                DrawCharacterSelectPlayerPanel(new Rect(980, 195, 900, 725), 1, playerCount);
            }

            GUI.Label(new Rect(610, 955, 700, 40), $"{Prompt(BindingAction.Back)} — BACK", _small);
        }

        private void DrawCharacterSelectPlayerPanel(Rect panel, int player, int playerCount)
        {
            DrawPanel(panel, new Color(0.055f, 0.105f, 0.145f, 1f));

            var isCoop = playerCount > 1;
            if (isCoop)
            {
                GUI.Label(new Rect(panel.x + 24, panel.y + 12, panel.width - 48, 32),
                    $"PLAYER {player + 1}", _campEyebrow);
            }

            var headerOffset = isCoop ? 52f : 24f;
            const float readyHeight = 58f;
            var hintHeight = isCoop ? 48f : 36f;
            const float footerBottomPad = 16f;
            const float hintButtonGap = 10f;
            const float detailFooterGap = 14f;

            var readyRect = new Rect(
                panel.x + panel.width * 0.5f - 160f,
                panel.yMax - footerBottomPad - readyHeight,
                320f,
                readyHeight);
            var hintRect = new Rect(
                panel.x + 24f,
                readyRect.y - hintButtonGap - hintHeight,
                panel.width - 48f,
                hintHeight);
            var footerTop = hintRect.y - detailFooterGap;

            Rect gridRect;
            Rect detailRect;

            if (isCoop)
            {
                var gridHeight = 280f;
                gridRect = new Rect(panel.x + 24f, panel.y + headerOffset, panel.width - 48f, gridHeight);
                detailRect = new Rect(
                    panel.x + 24f,
                    gridRect.yMax + 16f,
                    panel.width - 48f,
                    Mathf.Max(180f, footerTop - (gridRect.yMax + 16f)));

                DrawPanel(new Rect(panel.x + 24f, footerTop - 2f, panel.width - 48f, 2f),
                    new Color(0.22f, 0.48f, 0.58f, 0.45f));
            }
            else
            {
                var gridWidth = 480f;
                var gridHeight = 520f;
                gridRect = new Rect(panel.x + 24f, panel.y + headerOffset, gridWidth, gridHeight);
                detailRect = new Rect(
                    gridRect.xMax + 24f,
                    gridRect.y,
                    panel.xMax - gridRect.xMax - 48f,
                    footerTop - gridRect.y);
            }

            DrawCharacterSelectGrid(gridRect, player, playerCount);
            DrawCharacterSelectDetail(detailRect, player, playerCount);

            var selectedIndex = _characterSelections[player];
            var unlocked = SaveService.IsCharacterUnlocked(selectedIndex);
            var hasSelected = _characterSelected[player];
            var isConfirmed = playerCount > 1 ? hasSelected : _characterReady[player];
            var canSelect = unlocked && !hasSelected;
            var canReady = playerCount == 1 && hasSelected && !_characterReady[player];
            var actionLabel = hasSelected ? "READY" : "SELECT";
            var disabledLabel = unlocked ? actionLabel : "LOCKED";

            if (isConfirmed)
            {
                if (isCoop && !AllPlayersSelected(playerCount))
                {
                    GUI.Label(hintRect, $"P{player + 1} READY  •  WAITING FOR PARTNER", _micro);
                }
                else
                {
                    GUI.Label(hintRect, $"P{player + 1} READY", _micro);
                }
            }
            else if (!unlocked)
            {
                GUI.Label(hintRect, $"P{player + 1}  •  BACK TO CAMP → UNLOCK BOARD", _micro);
            }
            else if (hasSelected)
            {
                GUI.Label(hintRect,
                    $"P{player + 1}  •  {Prompt(BindingAction.Submit)} READY  •  {LocalInputRouter.AssignmentLabel(player, playerCount)}",
                    _micro);
            }
            else
            {
                GUI.Label(hintRect,
                    $"P{player + 1}  •  ARROWS CHOOSE  •  {Prompt(BindingAction.Submit)} SELECT  •  {LocalInputRouter.AssignmentLabel(player, playerCount)}",
                    _micro);
            }

            if (!hasSelected)
            {
                DrawPrimaryActionButton(
                    readyRect,
                    "SELECT",
                    canSelect,
                    false,
                    disabledLabel,
                    PrimaryActionTheme.Select,
                    () => TryConfirmCharacterSelect(player, playerCount));
            }
            else
            {
                DrawPrimaryActionButton(
                    readyRect,
                    "READY",
                    canReady,
                    isConfirmed,
                    disabledLabel,
                    PrimaryActionTheme.Ready,
                    () => TryConfirmCharacterReady(player, playerCount));
            }
        }

        private bool AllPlayersSelected(int playerCount)
        {
            for (var player = 0; player < playerCount; player++)
            {
                if (!_characterSelected[player])
                {
                    return false;
                }
            }

            return true;
        }

        private enum PrimaryActionTheme
        {
            Select,
            Ready
        }

        private void DrawPrimaryActionButton(
            Rect actionRect,
            string label,
            bool canConfirm,
            bool isConfirmed,
            string disabledLabel,
            PrimaryActionTheme theme,
            System.Action onConfirm)
        {
            if (isConfirmed && theme == PrimaryActionTheme.Ready)
            {
                var glowRect = new Rect(
                    actionRect.x - 6f,
                    actionRect.y - 6f,
                    actionRect.width + 12f,
                    actionRect.height + 12f);
                DrawPanel(glowRect, new Color(0.22f, 0.72f, 0.42f, 0.18f));
                DrawPanel(actionRect, new Color(0.08f, 0.34f, 0.2f, 0.98f));
                DrawBorder(actionRect, new Color(0.46f, 0.94f, 0.6f, 1f), 3f);
                GUI.Label(actionRect, label, _readyConfirmedLabel);
                return;
            }

            if (isConfirmed)
            {
                DrawPanel(actionRect, new Color(0.12f, 0.28f, 0.18f, 0.95f));
                DrawBorder(actionRect, new Color(0.38f, 0.82f, 0.52f, 0.9f), 3f);
                GUI.Label(actionRect, label, _readyConfirmedLabel);
                return;
            }

            if (canConfirm)
            {
                var glowRect = new Rect(
                    actionRect.x - 8f,
                    actionRect.y - 8f,
                    actionRect.width + 16f,
                    actionRect.height + 16f);

                if (theme == PrimaryActionTheme.Ready)
                {
                    DrawPanel(glowRect, new Color(0.24f, 0.78f, 0.46f, 0.24f));
                    DrawBorder(glowRect, new Color(0.38f, 0.9f, 0.58f, 0.95f), 4f);

                    if (GUI.Button(actionRect, label, _readyActionButton))
                    {
                        onConfirm();
                    }
                }
                else
                {
                    DrawPanel(glowRect, new Color(0.98f, 0.78f, 0.22f, 0.2f));
                    DrawBorder(glowRect, new Color(0.98f, 0.82f, 0.32f, 0.95f), 4f);

                    if (GUI.Button(actionRect, label, _selectActionButton))
                    {
                        onConfirm();
                    }
                }

                return;
            }

            DrawPanel(actionRect, new Color(0.08f, 0.1f, 0.12f, 0.85f));
            DrawBorder(actionRect, new Color(0.18f, 0.22f, 0.26f, 0.65f), 2f);
            GUI.Label(actionRect, disabledLabel, _campLocked);
        }

        private void DrawCharacterSelectGrid(Rect gridRect, int player, int playerCount)
        {
            var columns = CharacterSelectPresentation.GridColumns;
            var characterCount = ContentCatalog.Characters.Length;
            var rows = Mathf.Max(1, (characterCount + columns - 1) / columns);
            var gap = playerCount == 1 ? 16f : 12f;
            var tileWidth = (gridRect.width - gap * (columns - 1)) / columns;
            var tileHeight = (gridRect.height - gap * (rows - 1)) / rows;
            var selectedIndex = _characterSelections[player];

            for (var index = 0; index < characterCount; index++)
            {
                var column = index % columns;
                var row = index / columns;
                var tileRect = new Rect(
                    gridRect.x + column * (tileWidth + gap),
                    gridRect.y + row * (tileHeight + gap),
                    tileWidth,
                    tileHeight);
                var definition = ContentCatalog.Character(index);
                var unlocked = SaveService.IsCharacterUnlocked(index);
                var isSelected = index == selectedIndex;
                var tileBackground = isSelected
                    ? new Color(0.09f, 0.18f, 0.22f, 1f)
                    : new Color(0.04f, 0.075f, 0.105f, 1f);

                DrawPanel(tileRect, tileBackground);
                if (isSelected)
                {
                    DrawBorder(tileRect, new Color(0.93f, 0.7f, 0.24f), playerCount == 1 ? 5f : 4f);
                }

                DrawPanel(new Rect(tileRect.x, tileRect.y, tileRect.width, 4f),
                    unlocked ? definition.Color : new Color(0.25f, 0.28f, 0.32f));

                var portraitRect = new Rect(
                    tileRect.x + 12f,
                    tileRect.y + 12f,
                    tileRect.width - 24f,
                    tileRect.height - 52f);
                CharacterSelectPresentation.DrawPortrait(
                    portraitRect, definition, unlocked, CharacterPortraitSize.Compact);

                if (!unlocked)
                {
                    CharacterSelectPresentation.DrawLockBadge(tileRect);
                }

                if (IsLastPlayedCharacter(player, index))
                {
                    var badgeRect = new Rect(tileRect.x + 8f, tileRect.y + 8f, tileRect.width - 16f, 28f);
                    DrawPanel(badgeRect, new Color(0.92f, 0.58f, 0.14f, 0.92f));
                    GUI.Label(badgeRect, "LAST EXPEDITION", _gridBadge);
                }

                var otherPlayerBadge = FindOtherPlayerClaim(player, index, playerCount);
                if (!string.IsNullOrEmpty(otherPlayerBadge))
                {
                    var claimRect = new Rect(tileRect.xMax - 56f, tileRect.y + 8f, 48f, 20f);
                    DrawPanel(claimRect, new Color(0.32f, 0.68f, 0.78f, 0.9f));
                    GUI.Label(new Rect(claimRect.x, claimRect.y + 1f, claimRect.width, claimRect.height - 2f),
                        otherPlayerBadge, _gridBadge);
                }

                var nameRect = new Rect(tileRect.x + 8f, tileRect.yMax - 24f, tileRect.width - 16f, 20f);
                GUI.Label(nameRect, ShortCharacterName(definition.Name), unlocked ? _micro : _campLocked);

                if (!_characterSelected[player] && GUI.Button(tileRect, GUIContent.none, GUIStyle.none))
                {
                    _characterSelections[player] = index;
                    ConfirmCue();
                }
            }
        }

        private string FindOtherPlayerClaim(int player, int characterIndex, int playerCount)
        {
            if (playerCount <= 1)
            {
                return null;
            }

            for (var other = 0; other < playerCount; other++)
            {
                if (other == player)
                {
                    continue;
                }

                if (_characterSelections[other] == characterIndex && _characterSelected[other])
                {
                    return $"P{other + 1}";
                }
            }

            return null;
        }

        private static string ShortCharacterName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            var parts = name.Split(' ');
            if (parts.Length <= 1)
            {
                return name.ToUpperInvariant();
            }

            return $"{parts[0].ToUpperInvariant()} {parts[parts.Length - 1].ToUpperInvariant()}";
        }

        private void DrawCharacterSelectDetail(Rect detailRect, int player, int playerCount)
        {
            var index = _characterSelections[player];
            var definition = ContentCatalog.Character(index);
            var unlocked = SaveService.IsCharacterUnlocked(index);

            DrawPanel(detailRect, new Color(0.045f, 0.085f, 0.115f, 1f));
            DrawPanel(new Rect(detailRect.x, detailRect.y, detailRect.width, 5f),
                unlocked ? definition.Color : new Color(0.25f, 0.28f, 0.32f));

            if (playerCount > 1)
            {
                DrawCharacterSelectDetailCoop(detailRect, definition, unlocked);
                return;
            }

            var portraitSize = 240f;
            var portraitRect = new Rect(detailRect.x + 24f, detailRect.y + 24f, portraitSize, portraitSize);
            CharacterSelectPresentation.DrawPortrait(
                portraitRect, definition, unlocked, CharacterPortraitSize.Large);

            var textX = portraitRect.xMax + 24f;
            var textWidth = detailRect.xMax - textX - 24f;
            var textY = detailRect.y + 24f;

            DrawCharacterSelectDetailContent(textX, textY, textWidth, detailRect.yMax, definition, unlocked);
        }

        private void DrawCharacterSelectDetailCoop(Rect detailRect, CharacterDefinition definition, bool unlocked)
        {
            var portraitSize = Mathf.Min(96f, detailRect.height - 32f, detailRect.width * 0.32f);
            var portraitRect = new Rect(
                detailRect.x + 16f,
                detailRect.y + 16f,
                portraitSize,
                portraitSize);
            CharacterSelectPresentation.DrawPortrait(
                portraitRect, definition, unlocked, CharacterPortraitSize.Compact);

            var textX = portraitRect.xMax + 16f;
            var textWidth = detailRect.xMax - textX - 16f;
            var textY = detailRect.y + 16f;

            DrawCharacterSelectDetailContent(textX, textY, textWidth, detailRect.yMax, definition, unlocked, true);
        }

        private void DrawCharacterSelectDetailContent(
            float textX,
            float textY,
            float textWidth,
            float detailBottom,
            CharacterDefinition definition,
            bool unlocked,
            bool compact = false)
        {
            var nameStyle = compact ? _cardTitle : _heading;
            var nameHeight = compact ? 30f : 52f;

            GUI.Label(new Rect(textX, textY, textWidth, nameHeight), definition.Name.ToUpperInvariant(), nameStyle);
            textY += nameHeight + 4f;
            GUI.Label(new Rect(textX, textY, textWidth, compact ? 22f : 30f),
                $"{definition.Tribe} — {definition.Role}", compact ? _microLeft : _small);
            textY += compact ? 24f : 38f;

            if (!unlocked)
            {
                DrawPanel(new Rect(textX, textY, textWidth, compact ? 28f : 34f), new Color(0.38f, 0.16f, 0.1f, 0.92f));
                GUI.Label(new Rect(textX + 12f, textY + 4f, textWidth - 24f, compact ? 20f : 26f), "NOT YET UNLOCKED", _badge);
                textY += compact ? 34f : 42f;

                var teaser = string.IsNullOrWhiteSpace(definition.LockedPreviewLine)
                    ? $"{definition.Tribe} — {definition.Role}"
                    : definition.LockedPreviewLine;
                var teaserHeight = compact
                    ? Mathf.Min(40f, detailBottom - textY - (compact ? 44f : 96f))
                    : 70f;
                GUI.Label(new Rect(textX, textY, textWidth, teaserHeight), teaser, compact ? _microLeft : _body);
                textY += teaserHeight + 8f;

                var cost = ResolveUnlockCost(definition.Id);
                var available = SaveService.AvailableRenown();
                var shortfall = Mathf.Max(0, cost - available);
                var purchaseHint = shortfall > 0
                    ? $"You have {available} renown — earn {shortfall} more, then open the Unlock Board at camp."
                    : $"Return to camp → Unlock Board to unlock for {cost} renown.";
                var hintHeight = Mathf.Max(0f, detailBottom - textY - 8f);
                if (hintHeight > 0f)
                {
                    GUI.Label(new Rect(textX, textY, textWidth, hintHeight), purchaseHint, _campLocked);
                }

                if (!compact)
                {
                    textY += 88f;
                    var hiddenRect = new Rect(textX, textY, textWidth, 110f);
                    DrawPanel(hiddenRect, new Color(0.025f, 0.065f, 0.087f, 1f));
                    DrawPanel(new Rect(hiddenRect.x, hiddenRect.y, 5f, hiddenRect.height), new Color(0.35f, 0.38f, 0.42f));
                    GUI.Label(new Rect(hiddenRect.x + 18f, hiddenRect.y + 12f, hiddenRect.width - 36f, hiddenRect.height - 24f),
                        "STATS  ???   SPEED  ???   ARMOR  ???\n\nULTIMATE  ???", _campLocked);
                }

                return;
            }

            var descriptionHeight = compact
                ? Mathf.Min(44f, detailBottom - textY - 38f)
                : 90f;
            if (descriptionHeight > 0f)
            {
                GUI.Label(new Rect(textX, textY, textWidth, descriptionHeight), definition.Description, compact ? _microLeft : _body);
                textY += descriptionHeight + 8f;
            }

            if (textY + 22f <= detailBottom)
            {
                GUI.Label(new Rect(textX, textY, textWidth, compact ? 22f : 28f),
                    $"HEALTH {definition.MaxHealth:0}   SPEED {definition.MoveSpeed:0.0}   ARMOR {definition.Armor:0.0}",
                    compact ? _microLeft : _small);
                textY += compact ? 26f : 38f;
            }

            if (compact)
            {
                return;
            }

            var ultimateHeight = Mathf.Min(110f, detailBottom - textY - 12f);
            if (ultimateHeight < 32f)
            {
                return;
            }

            var ultimateRect = new Rect(textX, textY, textWidth, ultimateHeight);
            DrawPanel(ultimateRect, new Color(0.025f, 0.065f, 0.087f, 1f));
            DrawPanel(new Rect(ultimateRect.x, ultimateRect.y, 5f, ultimateRect.height), definition.Color);
            GUI.Label(new Rect(ultimateRect.x + 14f, ultimateRect.y + 6f, ultimateRect.width - 28f, ultimateRect.height - 12f),
                $"ULTIMATE — {definition.UltimateName}: {definition.UltimateDescription}",
                compact ? _microLeft : _body);
        }

        private void DrawMapSelect()
        {
            DrawPanel(new Rect(0, 0, 1920, 1080), new Color(0.025f, 0.06f, 0.09f, 1f));
            GUI.Label(new Rect(430, 55, 1060, 80), "CHOOSE THE EXPEDITION", _title);
            GUI.Label(new Rect(360, 135, 1200, 45),
                "Select from the roster — locked expeditions can be viewed, but only unlocked ones can be started.",
                _small);

            DrawMapSelectPanel(new Rect(120, 195, 1680, 725));
            GUI.Label(new Rect(610, 955, 700, 40), $"{Prompt(BindingAction.Back)} — BACK", _small);
        }

        private void DrawMapSelectPanel(Rect panel)
        {
            DrawPanel(panel, new Color(0.055f, 0.105f, 0.145f, 1f));

            var gridRect = new Rect(panel.x + 24f, panel.y + 24f, 480f, 280f);
            var detailRect = new Rect(
                gridRect.xMax + 24f,
                gridRect.y,
                panel.xMax - gridRect.xMax - 48f,
                panel.yMax - gridRect.y - 96f);

            DrawMapSelectGrid(gridRect);
            DrawMapSelectDetail(detailRect);

            var map = ContentCatalog.Map(_mapSelection);
            var unlocked = SaveService.IsMapUnlocked(_mapSelection);
            var canBegin = unlocked;
            var actionY = panel.yMax - 72f;
            var hintRect = new Rect(panel.x + 24f, actionY - 44f, panel.width - 48f, 36f);

            if (!unlocked)
            {
                GUI.Label(hintRect, $"P1  •  LOCKED — SPEND RENOWN AT CAMP", _micro);
            }
            else
            {
                GUI.Label(hintRect,
                    $"P1  •  ARROWS CHOOSE  •  {Prompt(BindingAction.Submit)} CONFIRM  •  {LocalInputRouter.AssignmentLabel(0, 1)}",
                    _micro);
            }

            var beginRect = new Rect(panel.x + panel.width * 0.5f - 220f, actionY, 440f, 58f);
            DrawPrimaryActionButton(
                beginRect,
                "BEGIN EXPEDITION",
                canBegin,
                false,
                "LOCKED",
                PrimaryActionTheme.Select,
                TryBeginSelectedMap);
        }

        private void DrawMapSelectGrid(Rect gridRect)
        {
            var columns = MapSelectPresentation.GridColumns;
            var mapCount = ContentCatalog.Maps.Length;
            var rows = Mathf.Max(1, (mapCount + columns - 1) / columns);
            var gap = 16f;
            var tileWidth = (gridRect.width - gap * (columns - 1)) / columns;
            var tileHeight = (gridRect.height - gap * (rows - 1)) / rows;

            for (var index = 0; index < mapCount; index++)
            {
                var column = index % columns;
                var row = index / columns;
                var tileRect = new Rect(
                    gridRect.x + column * (tileWidth + gap),
                    gridRect.y + row * (tileHeight + gap),
                    tileWidth,
                    tileHeight);
                var map = ContentCatalog.Map(index);
                var unlocked = SaveService.IsMapUnlocked(index);
                var isSelected = index == _mapSelection;
                var tileBackground = isSelected
                    ? new Color(0.09f, 0.18f, 0.22f, 1f)
                    : new Color(0.04f, 0.075f, 0.105f, 1f);

                DrawPanel(tileRect, tileBackground);
                if (isSelected)
                {
                    DrawBorder(tileRect, new Color(0.93f, 0.7f, 0.24f), 5f);
                }

                DrawPanel(new Rect(tileRect.x, tileRect.y, tileRect.width, 4f),
                    unlocked ? map.GroundColor : new Color(0.25f, 0.28f, 0.32f));

                var previewRect = new Rect(
                    tileRect.x + 12f,
                    tileRect.y + 12f,
                    tileRect.width - 24f,
                    tileRect.height - 52f);
                MapSelectPresentation.DrawPreview(previewRect, map, unlocked, MapPreviewSize.Compact);

                if (!unlocked)
                {
                    CharacterSelectPresentation.DrawLockBadge(tileRect);
                }

                var nameRect = new Rect(tileRect.x + 8f, tileRect.yMax - 26f, tileRect.width - 16f, 22f);
                GUI.Label(nameRect, ShortMapName(map), unlocked ? _micro : _campLocked);

                if (GUI.Button(tileRect, GUIContent.none, GUIStyle.none))
                {
                    _mapSelection = index;
                    ConfirmCue();
                }
            }
        }

        private void DrawMapSelectDetail(Rect detailRect)
        {
            var map = ContentCatalog.Map(_mapSelection);
            var unlocked = SaveService.IsMapUnlocked(_mapSelection);
            var previewSize = 240f;

            DrawPanel(detailRect, new Color(0.045f, 0.085f, 0.115f, 1f));
            DrawPanel(new Rect(detailRect.x, detailRect.y, detailRect.width, 5f),
                unlocked ? map.GroundColor : new Color(0.25f, 0.28f, 0.32f));

            var previewRect = new Rect(detailRect.x + 24f, detailRect.y + 24f, previewSize, previewSize);
            MapSelectPresentation.DrawPreview(previewRect, map, unlocked, MapPreviewSize.Large);

            var textX = previewRect.xMax + 24f;
            var textWidth = detailRect.xMax - textX - 24f;
            var textY = detailRect.y + 24f;

            GUI.Label(new Rect(textX, textY, textWidth, 52), map.Name.ToUpperInvariant(), _heading);
            textY += 56f;
            GUI.Label(new Rect(textX, textY, textWidth, 30), $"{map.Region}   •   {map.DurationLabel}", _small);
            textY += 38f;

            if (!unlocked)
            {
                DrawPanel(new Rect(textX, textY, textWidth, 40f), new Color(0.38f, 0.16f, 0.1f, 0.92f));
                GUI.Label(new Rect(textX + 12f, textY + 6f, textWidth - 24f, 28f), "NOT YET UNLOCKED", _badge);
                textY += 52f;

                var teaser = string.IsNullOrWhiteSpace(map.LockedPreviewLine)
                    ? $"{map.Region} — {map.DurationLabel}"
                    : map.LockedPreviewLine;
                GUI.Label(new Rect(textX, textY, textWidth, 70f), teaser, _body);
                textY += 78f;

                var cost = ResolveUnlockCost(map.Id);
                var available = SaveService.AvailableRenown();
                var shortfall = Mathf.Max(0, cost - available);
                var purchaseHint = shortfall > 0
                    ? $"Unlock at the Frostbound Camp Unlock Board for {cost} Renown.\nYou have {available} available — need {shortfall} more."
                    : $"Unlock at the Frostbound Camp Unlock Board for {cost} Renown.\nYou have {available} available — return to camp to unlock.";
                GUI.Label(new Rect(textX, textY, textWidth, 96f), purchaseHint, _campLocked);
                textY += 104f;

                var hiddenRect = new Rect(textX, textY, textWidth, 110f);
                DrawPanel(hiddenRect, new Color(0.025f, 0.065f, 0.087f, 1f));
                DrawPanel(new Rect(hiddenRect.x, hiddenRect.y, 5f, hiddenRect.height), new Color(0.35f, 0.38f, 0.42f));
                GUI.Label(new Rect(hiddenRect.x + 18f, hiddenRect.y + 12f, hiddenRect.width - 36f, hiddenRect.height - 24f),
                    "JOTUNN ARRIVAL  ???\n\nWEAPON SLOTS  ???   GEAR SLOTS  ???\n\nOBJECTIVES  ???", _campLocked);
                return;
            }

            GUI.Label(new Rect(textX, textY, textWidth, 90f), map.Description, _body);
            textY += 98f;
            GUI.Label(new Rect(textX, textY, textWidth, 28f), $"JOTUNN ARRIVAL  {FormatTime(map.BossSpawnTime)}", _small);
            textY += 34f;
            GUI.Label(new Rect(textX, textY, textWidth, 28f),
                $"WEAPON SLOTS {map.WeaponSlots}   GEAR SLOTS {map.GearSlots}", _small);
            textY += 34f;
            GUI.Label(new Rect(textX, textY, textWidth, 28f),
                $"KILLS {map.RequiredKillObjective}   SHARDS {map.OptionalShardObjective}", _small);
        }

        private static string ShortMapName(MapDefinition map)
        {
            if (map == null)
            {
                return string.Empty;
            }

            if (map.Id == "frostbound.saga")
            {
                return "LONG NIGHT";
            }

            if (map.Id == "frostbound.scout")
            {
                return "FROSTBOUND SHORE";
            }

            return map.Name.ToUpperInvariant();
        }

        private void DrawRunHud()
        {
            if (_director.Player == null) return;
            var playerPanelHeight = _director.Players.Count > 1 ? 270f : 175f;
            DrawPanel(new Rect(28, 25, 660, playerPanelHeight), new Color(0.018f, 0.045f, 0.062f, 0.94f));
            DrawPanel(new Rect(28, 25, 660, 5), new Color(0.28f, 0.68f, 0.82f));
            GUI.Label(new Rect(510, 40, 150, 30), $"LEVEL {_director.Level}", _small);
            for (var i = 0; i < _director.Players.Count; i++)
            {
                var player = _director.Players[i];
                if (player == null) continue;
                var y = 42f + i * 96f;
                GUI.Label(new Rect(50, y, 420, 30), $"P{i + 1}  {player.HeroName.ToUpperInvariant()}", _cardTitle);
                var healthLabel = player.IsDowned
                    ? $"DOWN — REVIVE {Mathf.RoundToInt(player.ReviveProgress * 100f)}%"
                    : $"{Mathf.CeilToInt(player.Health)} / {Mathf.CeilToInt(player.MaxHealth)}";
                DrawBar(new Rect(50, y + 35, 610, 22), player.Health / player.MaxHealth,
                    i == 0 ? new Color(0.28f, 0.68f, 0.88f) : new Color(0.9f, 0.48f, 0.2f), healthLabel, _micro);
                var ultimateFill = player.UltimateReady ? 1f : 1f - player.UltimateRemaining / Mathf.Max(1f, player.UltimateCooldown);
                var ultimateLabel = player.UltimateReady ? $"{player.UltimateName.ToUpperInvariant()} — READY"
                    : $"{player.UltimateName.ToUpperInvariant()} — {player.UltimateRemaining:0}s";
                DrawBar(new Rect(50, y + 64, 610, 20), ultimateFill, new Color(0.7f, 0.38f, 0.9f), ultimateLabel, _micro);
            }
            var xpY = _director.Players.Count > 1 ? 258f : 166f;
            DrawBar(new Rect(50, xpY, 610, 17), _director.Experience / (float)_director.ExperienceToNext,
                new Color(0.22f, 0.72f, 0.9f), $"XP {_director.Experience} / {_director.ExperienceToNext}", _micro);

            DrawPanel(new Rect(735, 26, 450, 88), new Color(0.025f, 0.055f, 0.075f, 0.9f));
            var remainingBoss = Mathf.Max(0f, _director.SelectedMap.BossSpawnTime - _director.Elapsed);
            var timerText = _director.BossSpawned ? "DEFEAT THE JOTUNN" : $"JOTUNN IN {FormatTime(remainingBoss)}";
            GUI.Label(new Rect(750, 39, 420, 34), $"{FormatTime(_director.Elapsed)} / {FormatTime(_director.SelectedMap.Duration)}", _center);
            GUI.Label(new Rect(750, 78, 420, 24), timerText, _small);

            DrawPanel(new Rect(1245, 26, 645, 100), new Color(0.025f, 0.055f, 0.075f, 0.9f));
            GUI.Label(new Rect(1270, 42, 590, 32), $"KILLS  {_director.Kills}     RENOWN  {_director.RunRenown}     ENEMIES  {_director.Enemies.Count}", _body);
            GUI.Label(new Rect(1270, 79, 590, 28),
                $"ULTIMATE {Prompt(BindingAction.Ultimate)}   •   PAUSE {Prompt(BindingAction.Pause)}", _micro);
            DrawBuildTray();
            DrawObjectivePanel();
            DrawFirstRunHints();
            if (_director.ShowPerformanceMetrics) DrawPerformancePanel();
        }

        private void DrawObjectivePanel()
        {
            var route = _director.Route;
            if (route == null)
            {
                return;
            }

            const float panelX = 735f;
            const float panelY = 130f;
            const float panelWidth = 450f;
            const float panelHeight = 118f;
            DrawPanel(new Rect(panelX, panelY, panelWidth, panelHeight), new Color(0.025f, 0.055f, 0.075f, 0.92f));
            DrawPanel(new Rect(panelX, panelY, panelWidth, 4), new Color(0.32f, 0.68f, 0.78f, 0.85f));
            GUI.Label(new Rect(panelX + 18, panelY + 10, panelWidth - 36, 26), "EXPEDITION OBJECTIVES", _statSection);

            var killLine = $"DRAUGR  {route.DraugrKills} / {route.RequiredKillObjective}";
            var shardLine = route.OptionalShardObjective > 0
                ? $"SHARDS  {route.RuneShardsCollected} / {route.OptionalShardObjective}"
                : string.Empty;
            var objectiveBody = killLine;
            if (shardLine.Length > 0)
            {
                objectiveBody += $"\n{shardLine}";
            }

            if (route.BossKilled && route.CurrentPhase == ExpeditionPhase.Extraction)
            {
                var extractionRemaining = Mathf.Max(0f, route.ExtractionDuration - route.ExtractionElapsed);
                objectiveBody += $"\nREACH THE BEACON — {FormatTime(extractionRemaining)} REMAINING";
            }
            else if (route.CanSpawnBoss() && !route.BossSpawned)
            {
                objectiveBody += "\nOBJECTIVES MET — JOTUNN ELIGIBLE";
            }

            GUI.Label(new Rect(panelX + 18, panelY + 36, panelWidth - 36, panelHeight - 44), objectiveBody, _microLeft);
        }

        private void DrawFirstRunHints()
        {
            if (PresentationPreferences.Data.FirstRunHintsSeen)
            {
                return;
            }

            const int hintCount = 7;
            var nextHint = -1;
            for (var i = 0; i < hintCount; i++)
            {
                var dismissed = (_dismissedFirstRunHints & (1 << i)) != 0;
                if (!dismissed)
                {
                    nextHint = i;
                    break;
                }
            }

            if (nextHint < 0)
            {
                PresentationPreferences.Data.FirstRunHintsSeen = true;
                PresentationPreferences.Save();
                return;
            }

            var messages = new[]
            {
                $"{Prompt(BindingAction.MoveUp)} {Prompt(BindingAction.MoveDown)} {Prompt(BindingAction.MoveLeft)} {Prompt(BindingAction.MoveRight)} — MOVE TO DODGE AND COLLECT XP",
                "WEAPONS FIRE AUTOMATICALLY — POSITIONING IS YOUR MAIN SKILL",
                $"{Prompt(BindingAction.Ultimate)} — USE YOUR ULTIMATE WHEN THE EXPEDITION TIGHTENS",
                "LEVEL UP OFFERS FOUR REWARD CARDS — BUILD WEAPONS, GEAR AND EVOLUTIONS",
                "TRACK SCOUT OBJECTIVES IN THE HUD — CULL DRAUGR TO UNLOCK THE JOTUNN EARLY",
                "AFTER THE BOSS FALLS, REACH THE NORTH EXTRACTION BEACON TO COMPLETE THE RUN",
                "RENOWN EARNED IN RUNS CAN UNLOCK NEW SURVIVORS AND EXPEDITIONS AT CAMP"
            };
            var rect = new Rect(520f, 880f, 880f, 92f);
            DrawPanel(rect, new Color(0.018f, 0.045f, 0.062f, 0.96f));
            DrawBorder(rect, PresentationTheme.Accent, 3f);
            GUI.Label(new Rect(rect.x + 22, rect.y + 10, rect.width - 190, rect.height - 20), messages[nextHint], _body);
            if (GUI.Button(new Rect(rect.xMax - 150, rect.y + 24, 130, 44), "DISMISS", _button))
            {
                _dismissedFirstRunHints |= 1 << nextHint;
                if ((_dismissedFirstRunHints & ((1 << hintCount) - 1)) == ((1 << hintCount) - 1))
                {
                    PresentationPreferences.Data.FirstRunHintsSeen = true;
                    PresentationPreferences.Save();
                }
            }
        }

        private void DrawLevelUp()
        {
            DrawPanel(new Rect(0, 0, 1920, 1080), new Color(0.008f, 0.022f, 0.032f, 1f));
            GUI.Label(new Rect(500, 85, 920, 75), "CHOOSE THE NEXT VERSE", _title);
            var owner = _director.Players[Mathf.Clamp(_director.RewardTurnPlayerIndex, 0, _director.Players.Count - 1)];
            GUI.Label(new Rect(460, 165, 1000, 44), $"{owner.HeroName.ToUpperInvariant()} CHOOSES — ONLY P{owner.PlayerIndex + 1}'S DEVICE IS ACTIVE", _center);

            const float cardTop = 255f;
            const float cardHeight = 520f;
            const float cardWidth = 400f;
            const float cardGap = 435f;
            const float innerPad = 28f;
            const float buttonHeight = 64f;

            for (var i = 0; i < _director.CurrentRewards.Count; i++)
            {
                var option = _director.CurrentRewards[i];
                var rect = new Rect(90 + i * cardGap, cardTop, cardWidth, cardHeight);
                var hovered = rect.Contains(Event.current.mousePosition);

                if (hovered)
                {
                    _levelSelection = i;
                }

                DrawPanel(rect, hovered ? new Color(0.075f, 0.145f, 0.19f, 1f) : new Color(0.045f, 0.095f, 0.13f, 1f));
                DrawPanel(new Rect(rect.x, rect.y, rect.width, 14), option.Item.Color);

                if (i == _levelSelection)
                {
                    DrawBorder(rect, new Color(0.96f, 0.72f, 0.22f), 7f);
                }

                var targetLabel = RewardTargetLabel(option);
                var targetColor = option.Shared
                    ? new Color(0.72f, 0.48f, 0.92f)
                    : _director.Players[option.TargetPlayerIndex].Definition.Color;
                DrawRewardTargetIcons(option, new Rect(rect.x + 185, rect.y + 26, 44, 44));
                DrawPanel(new Rect(rect.x + 234, rect.y + 28, 141, 40), targetColor);
                GUI.Label(new Rect(rect.x + 234, rect.y + 28, 141, 40), targetLabel, _badge);

                var contentY = rect.y + 82f;
                GUI.Label(new Rect(rect.x + 25, contentY, rect.width - 50, 56), $"{i + 1}. {option.Item.Name}", _heading);
                contentY += 60f;

                var build = _director.Players[option.TargetPlayerIndex].Build;
                var nextLabel = option.Shared ? "TEAM UPGRADE" : build.NextLabel(option.Item);
                GUI.Label(new Rect(rect.x + 30, contentY, rect.width - 60, 32), nextLabel, _cardTitle);
                contentY += 34f;

                GUI.Label(new Rect(rect.x + 30, contentY, rect.width - 60, 24), RewardCategoryLabel(option), _rewardCategory);
                contentY += 28f;

                GUI.Label(new Rect(rect.x + innerPad, contentY, rect.width - innerPad * 2f, 64),
                    option.Item.Description, _rewardDescription);
                contentY += 70f;

                GUI.Label(new Rect(rect.x + innerPad, contentY, rect.width - innerPad * 2f, 56),
                    RewardEffectPreview(option), _rewardEffect);
                contentY += 62f;

                var hint = EvolutionHint(option);
                if (!string.IsNullOrEmpty(hint))
                {
                    GUI.Label(new Rect(rect.x + innerPad, contentY, rect.width - innerPad * 2f, 52), hint, _rewardHint);
                    contentY += 56f;
                }

                var buttonY = rect.yMax - innerPad - buttonHeight;
                if (buttonY < contentY + 10f)
                {
                    buttonY = contentY + 10f;
                }

                if (GUI.Button(new Rect(rect.x + innerPad, buttonY, rect.width - innerPad * 2f, buttonHeight),
                        "CHOOSE REWARD", _button))
                {
                    _director.ChooseReward(i);
                }

                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                {
                    _director.ChooseReward(i);
                }
            }

            GUI.Label(new Rect(420, 810, 1080, 55),
                $"CLICK A CARD   •   {Prompt(BindingAction.MoveLeft)} CHOOSE   •   {Prompt(BindingAction.Submit)} CONFIRM   •   1–4 / FACE BUTTONS", _small);
        }

        private static string RewardCategoryLabel(RewardOption option)
        {
            if (option == null || option.Item == null)
            {
                return string.Empty;
            }

            if (option.Shared)
            {
                return "TEAM UPGRADE";
            }

            if (option.Item.IsEvolution)
            {
                return "EVOLUTION";
            }

            return option.Item.Category.ToString().ToUpperInvariant();
        }

        private void DrawBuildTray()
        {
            var height = _director.Players.Count > 1 ? 235f : 140f;
            const float panelY = 320f;
            DrawPanel(new Rect(28, panelY, 660, height), new Color(0.018f, 0.045f, 0.062f, 0.94f));
            DrawPanel(new Rect(28, panelY, 660, 4), new Color(0.32f, 0.57f, 0.66f));
            for (var playerIndex = 0; playerIndex < _director.Players.Count; playerIndex++)
            {
                var player = _director.Players[playerIndex];
                var y = 338f + playerIndex * 95f;
                GUI.Label(new Rect(42, y, 92, 28), $"P{playerIndex + 1} BUILD", _microLeft);
                var items = player.Build.Items;
                var visibleIndex = 0;
                for (var i = 0; i < items.Count && visibleIndex < 12; i++)
                {
                    var state = items[i];
                    var definition = ItemCatalog.Find(state.ItemId);
                    if (definition == null || definition.Category == ItemCategory.Boon) continue;
                    var rect = new Rect(145 + visibleIndex * 42, y, 39, 50);
                    var accent = state.IsEvolved ? new Color(0.98f, 0.68f, 0.22f) : definition.Color;
                    DrawPanel(rect, new Color(0.025f, 0.065f, 0.085f, 1f));
                    DrawBorder(rect, accent, 3f);
                    GUI.Label(new Rect(rect.x + 2, rect.y + 3, rect.width - 4, 22),
                        definition.ShortName.Substring(0, Mathf.Min(3, definition.ShortName.Length)), _micro);
                    GUI.Label(new Rect(rect.x + 2, rect.y + 25, rect.width - 4, 20), state.IsEvolved ? "E" : $"L{state.Level}", _micro);
                    visibleIndex++;
                }
                GUI.Label(new Rect(145, y + 54, 485, 24),
                    $"WEAPONS {player.Build.CountCategory(ItemCategory.Weapon)}/{player.Build.WeaponSlots}   •   GEAR {player.Build.CountCategory(ItemCategory.Gear)}/{player.Build.GearSlots}", _small);
            }
            GUI.Label(new Rect(45, panelY + height - 29, 595, 24),
                $"{Prompt(BindingAction.BuildDetails)} — BUILD DETAILS", _micro);
        }

        private void DrawBuildDetails()
        {
            DrawPanel(new Rect(0, 0, 1920, 1080), new Color(0.008f, 0.022f, 0.032f, 1f));
            GUI.Label(new Rect(480, 24, 960, 75), "EXPEDITION BUILD", _title);
            GUI.Label(new Rect(460, 95, 1000, 38), "LIVE STATISTICS AND EVERY ITEM CURRENTLY SHAPING THIS RUN", _small);

            for (var playerIndex = 0; playerIndex < _director.Players.Count; playerIndex++)
            {
                var player = _director.Players[playerIndex];
                var width = _director.Players.Count == 1 ? 1040f : 850f;
                var x = _director.Players.Count == 1 ? 440f : 75f + playerIndex * 885f;
                var rect = new Rect(x, 148, width, 860);
                DrawPanel(rect, new Color(0.035f, 0.08f, 0.108f, 1f));
                DrawPanel(new Rect(rect.x, rect.y, rect.width, 14), player.Definition.Color);
                GUI.Label(new Rect(rect.x + 35, rect.y + 28, rect.width - 70, 48),
                    $"P{playerIndex + 1} — {player.HeroName.ToUpperInvariant()}", _heading);

                var equippedWeapons = player.Weapons.EquippedWeapons;
                const int weaponColumnsPerRow = 2;
                var weaponCount = equippedWeapons.Count;
                var maxScroll = Mathf.Max(0, weaponCount - weaponColumnsPerRow);
                _buildDetailsWeaponScroll = Mathf.Clamp(_buildDetailsWeaponScroll, 0, maxScroll);

                const float statTop = 88f;
                const float statGap = 12f;
                const float rowHeight = 29f;
                const float statHeaderHeight = 38f;
                var columnCount = 1 + Mathf.Min(weaponColumnsPerRow, weaponCount);
                var statWidth = (rect.width - 60f - statGap * (columnCount - 1)) / columnCount;
                var survivorRowCount = 10;
                var weaponRowCount = 0;

                for (var weaponColumn = 0; weaponColumn < weaponColumnsPerRow; weaponColumn++)
                {
                    var weaponIndex = _buildDetailsWeaponScroll + weaponColumn;
                    if (weaponIndex >= weaponCount)
                    {
                        continue;
                    }

                    weaponRowCount = Mathf.Max(weaponRowCount, CountWeaponStatRows(equippedWeapons[weaponIndex]));
                }

                var statHeight = statHeaderHeight + rowHeight * Mathf.Max(survivorRowCount, weaponRowCount);
                var survivorStats = new Rect(rect.x + 30, rect.y + statTop, statWidth, statHeight);
                DrawStatColumn(survivorStats, "SURVIVOR",
                    new[]
                    {
                        "HEALTH", "ARMOR", "MOVE SPEED", "XP MAGNET", "STATUS",
                        "ULTIMATE", "ULT DMG", "ULT RAD", "ULT CD", "ULT CHARGE"
                    },
                    new[]
                    {
                        $"{player.Health:0} / {player.MaxHealth:0}",
                        $"{player.Armor:0.0}",
                        $"{player.MoveSpeed:0.00}",
                        $"{player.MagnetRadius:0.00}",
                        player.IsDowned ? "DOWNED" : "ACTIVE",
                        FitStatValue(player.UltimateName, 16),
                        $"{player.UltimateDamage:0.0}",
                        $"{player.UltimateRadius:0.00}",
                        $"{player.UltimateCooldown:0.00}s",
                        player.UltimateReady ? "READY" : $"{player.UltimateRemaining:0.0}s"
                    },
                    player.Definition.Color,
                    rowHeight);

                for (var weaponColumn = 0; weaponColumn < weaponColumnsPerRow; weaponColumn++)
                {
                    var weaponIndex = _buildDetailsWeaponScroll + weaponColumn;
                    if (weaponIndex >= weaponCount)
                    {
                        continue;
                    }

                    var weaponRect = new Rect(
                        survivorStats.xMax + statGap + weaponColumn * (statWidth + statGap),
                        rect.y + statTop,
                        statWidth,
                        statHeight);
                    DrawWeaponStatColumn(weaponRect, equippedWeapons[weaponIndex], rowHeight);
                }

                var itemsSectionTop = rect.y + statTop + statHeight + 14f;
                if (weaponCount > weaponColumnsPerRow)
                {
                    var weaponAreaLeft = survivorStats.xMax + statGap;
                    var weaponAreaWidth = rect.xMax - 30f - weaponAreaLeft;
                    var navRect = new Rect(weaponAreaLeft, itemsSectionTop, weaponAreaWidth, 40f);
                    DrawPanel(navRect, new Color(0.018f, 0.048f, 0.068f, 0.85f));
                    DrawPanel(new Rect(navRect.x, navRect.yMax - 2f, navRect.width, 2f), player.Definition.Color * 0.65f);

                    var firstVisibleWeapon = _buildDetailsWeaponScroll + 1;
                    var lastVisibleWeapon = _buildDetailsWeaponScroll + Mathf.Min(weaponColumnsPerRow, weaponCount - _buildDetailsWeaponScroll);
                    GUI.Label(new Rect(navRect.x + 52, navRect.y + 4, navRect.width - 104, 32),
                        $"ARMS {firstVisibleWeapon}–{lastVisibleWeapon} OF {weaponCount}",
                        _center);

                    if (_buildDetailsWeaponScroll > 0 &&
                        GUI.Button(new Rect(navRect.x + 8, navRect.y + 4, 40, 32), "◀", _button))
                    {
                        _buildDetailsWeaponScroll--;
                    }

                    if (_buildDetailsWeaponScroll < maxScroll &&
                        GUI.Button(new Rect(navRect.xMax - 48, navRect.y + 4, 40, 32), "▶", _button))
                    {
                        _buildDetailsWeaponScroll++;
                    }

                    itemsSectionTop = navRect.yMax + 14f;
                }

                DrawPanel(new Rect(rect.x + 30, itemsSectionTop, rect.width - 60, 2),
                    new Color(0.08f, 0.16f, 0.2f, 0.9f));
                itemsSectionTop += 12f;

                var itemsHeaderHeight = 44f;
                GUI.Label(new Rect(rect.x + 35, itemsSectionTop, rect.width * 0.55f, itemsHeaderHeight),
                    "ITEMS AND LEVEL EFFECTS", _cardTitle);
                GUI.Label(new Rect(rect.x + rect.width * 0.45f, itemsSectionTop + 6, rect.width * 0.5f - 35, 32),
                    $"LOADOUT   WEAPONS {player.Build.CountCategory(ItemCategory.Weapon)}/{player.Build.WeaponSlots}   •   GEAR {player.Build.CountCategory(ItemCategory.Gear)}/{player.Build.GearSlots}",
                    _buildLoadout);

                var gridTop = itemsSectionTop + itemsHeaderHeight + 8f;
                const float cardGap = 10f;
                const float cardHeight = 102f;
                var cardWidth = (rect.width - 84f - cardGap * 2f) / 3f;
                var items = player.Build.Items;
                var visibleIndex = 0;

                for (var i = 0; i < items.Count && visibleIndex < 12; i++)
                {
                    var state = items[i];
                    var item = ItemCatalog.Find(state.ItemId);
                    if (item == null || item.Category == ItemCategory.Boon)
                    {
                        continue;
                    }

                    var column = visibleIndex % 3;
                    var row = visibleIndex / 3;
                    var itemRect = new Rect(
                        rect.x + 30 + column * (cardWidth + cardGap),
                        gridTop + row * (cardHeight + cardGap),
                        cardWidth,
                        cardHeight);
                    var evolved = state.IsEvolved ? ItemCatalog.Find(state.EvolutionId) : null;
                    DrawBuildItemCard(itemRect, state, item, evolved);
                    visibleIndex++;
                }
            }

            GUI.Label(new Rect(560, 1004, 800, 42),
                $"{Prompt(BindingAction.BuildDetails)} — RETURN TO EXPEDITION", _center);
        }

        private void DrawBuildItemCard(Rect itemRect, ItemState state, ItemDefinition item, ItemDefinition evolved)
        {
            DrawPanel(itemRect, new Color(0.018f, 0.05f, 0.07f, 1f));
            DrawBorder(itemRect, state.IsEvolved ? new Color(0.98f, 0.68f, 0.22f) : item.Color, 3f);
            GetItemEffectLines(state, item, evolved, out var header, out var current, out var next);

            GUI.Label(new Rect(itemRect.x + 12, itemRect.y + 8, itemRect.width - 24, 22), header, _itemTitle);
            GUI.Label(new Rect(itemRect.x + 12, itemRect.y + 32, itemRect.width - 24, 34), current, _itemProgress);

            if (!string.IsNullOrEmpty(next))
            {
                GUI.Label(new Rect(itemRect.x + 12, itemRect.y + 68, itemRect.width - 24, 30), next, _itemNext);
            }
        }

        private static void GetItemEffectLines(ItemState state, ItemDefinition item, ItemDefinition evolved,
            out string header, out string current, out string next)
        {
            if (evolved != null)
            {
                header = $"{evolved.Name} — EVOLVED";
                current = evolved.Description;
                next = string.Empty;
                return;
            }

            header = $"{item.Name} — LEVEL {state.Level}/{item.MaxLevel}";
            current = item.EffectDescriptionAtLevel(state.Level);

            if (state.Level >= item.MaxLevel)
            {
                next = "MAX LEVEL REACHED";
                return;
            }

            next = $"NEXT L{state.Level + 1}: {item.EffectDescriptionAtLevel(state.Level + 1)}";
        }

        private static int CountWeaponStatRows(WeaponInstance weapon)
        {
            var rows = 3;

            switch (weapon.Behavior)
            {
                case WeaponBehaviorKind.ProjectileVolley:
                    rows += 7;
                    break;
                case WeaponBehaviorKind.OwnerPulse:
                    rows += weapon.HealAmount > 0f ? 3 : 4;
                    break;
                case WeaponBehaviorKind.RadialBurst:
                    rows += 4;
                    break;
                case WeaponBehaviorKind.OrbitBlade:
                    rows += 4;
                    break;
            }

            rows += 1;
            return rows;
        }

        private string RewardTargetLabel(RewardOption option)
        {
            if (option.Shared) return "P1 + P2";
            return $"P{option.TargetPlayerIndex + 1}  {_director.Players[option.TargetPlayerIndex].HeroName.Split(' ')[0].ToUpperInvariant()}";
        }

        private static string ItemProgressPreview(ItemState state, ItemDefinition item,
            ItemDefinition evolved)
        {
            GetItemEffectLines(state, item, evolved, out _, out var current, out var next);

            if (string.IsNullOrEmpty(next))
            {
                return current;
            }

            if (next == "MAX LEVEL REACHED")
            {
                return $"MAX • {current}";
            }

            return $"CURRENT: {current}\n{next}";
        }

        private string RewardEffectPreview(RewardOption option)
        {
            if (option == null || option.Item == null) return string.Empty;
            if (option.Item.IsEvolution) return $"EFFECT: {option.Item.Description}";
            if (option.Item.Category == ItemCategory.Boon)
                return $"EFFECT: {option.Item.EffectDescriptionAtLevel(1)}";

            if (!option.Shared)
                return PlayerRewardEffectPreview(option.TargetPlayerIndex, option.Item, false);

            var preview = string.Empty;
            for (var playerIndex = 0; playerIndex < _director.Players.Count; playerIndex++)
            {
                if (preview.Length > 0) preview += "\n";
                preview += PlayerRewardEffectPreview(playerIndex, option.Item, true);
            }
            return preview;
        }

        private string PlayerRewardEffectPreview(int playerIndex, ItemDefinition item, bool showPlayer)
        {
            if (playerIndex < 0 || playerIndex >= _director.Players.Count) return string.Empty;
            var state = _director.Players[playerIndex].Build.Find(item.Id);
            var nextLevel = state == null ? 1 : Mathf.Min(item.MaxLevel, state.Level + 1);
            var prefix = showPlayer ? $"P{playerIndex + 1}  " : "EFFECT: ";
            return $"{prefix}L{nextLevel} — {item.EffectDescriptionAtLevel(nextLevel)}";
        }

        private void DrawRewardTargetIcons(RewardOption option, Rect rect)
        {
            if (option.Shared)
            {
                DrawPlayerToken(0, new Rect(rect.x, rect.y + 3, 34, 34));
                DrawPlayerToken(1, new Rect(rect.x + 17, rect.y + 3, 34, 34));
            }
            else DrawPlayerToken(option.TargetPlayerIndex, rect);
        }

        private void DrawPlayerToken(int playerIndex, Rect rect)
        {
            if (playerIndex < 0 || playerIndex >= _director.Players.Count) return;
            var previous = GUI.color;
            GUI.color = _director.Players[playerIndex].Definition.Color;
            GUI.DrawTexture(rect, RuntimeAssets.Circle.texture, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
            GUI.Label(rect, $"P{playerIndex + 1}", _badge);
        }

        private string EvolutionHint(RewardOption option)
        {
            if (option == null || option.Item == null)
            {
                return string.Empty;
            }

            var item = option.Item;
            if (item.IsEvolution)
            {
                var baseItem = ItemCatalog.Find(item.EvolutionOf);
                var catalyst = ItemCatalog.Find(item.CatalystId);
                return $"{baseItem.Name} max + {catalyst.Name}";
            }

            var playerIndex = Mathf.Clamp(option.TargetPlayerIndex, 0, _director.Players.Count - 1);
            var build = _director.Players[playerIndex].Build;
            if (item.Category == ItemCategory.Weapon)
            {
                var evolution = FindEvolutionForWeapon(item.Id);
                if (evolution != null)
                {
                    var state = build.Find(item.Id);
                    var catalyst = ItemCatalog.Find(evolution.CatalystId);
                    if (state != null && state.Level >= item.MaxLevel && !state.IsEvolved)
                    {
                        var hasCatalyst = build.Find(evolution.CatalystId) != null;
                        if (hasCatalyst)
                        {
                            return "EVOLUTION READY ON NEXT EVOLUTION CARD";
                        }

                        return $"NEED {catalyst.Name} TO EVOLVE";
                    }

                    if (state != null && state.Level == item.MaxLevel - 1)
                    {
                        return $"NEXT LEVEL MAX — PAIR WITH {catalyst.Name}";
                    }
                }
            }

            if (item.Category == ItemCategory.Gear)
            {
                var evolution = FindEvolutionWithCatalyst(item.Id);
                if (evolution != null)
                {
                    var baseDefinition = ItemCatalog.Find(evolution.EvolutionOf);
                    var baseState = build.Find(evolution.EvolutionOf);
                    if (baseState != null && baseState.Level >= baseDefinition.MaxLevel && !baseState.IsEvolved)
                    {
                        return $"{baseDefinition.Name} CAN EVOLVE NOW";
                    }

                    if (baseState != null && baseState.Level < baseDefinition.MaxLevel)
                    {
                        return $"RAISE {baseDefinition.Name} TO MAX LEVEL";
                    }
                }
            }

            return string.Empty;
        }

        private static ItemDefinition FindEvolutionForWeapon(string weaponId)
        {
            for (var i = 0; i < ItemCatalog.All.Length; i++)
            {
                var item = ItemCatalog.All[i];
                if (item.IsEvolution && item.EvolutionOf == weaponId)
                {
                    return item;
                }
            }

            return null;
        }

        private static ItemDefinition FindEvolutionWithCatalyst(string catalystId)
        {
            for (var i = 0; i < ItemCatalog.All.Length; i++)
            {
                var item = ItemCatalog.All[i];
                if (item.IsEvolution && item.CatalystId == catalystId)
                {
                    return item;
                }
            }

            return null;
        }

        private void DrawPause()
        {
            DrawPanel(new Rect(0, 0, 1920, 1080), new Color(0.01f, 0.025f, 0.04f, 0.78f));
            DrawPanel(new Rect(630, 265, 660, 560), new Color(0.055f, 0.105f, 0.14f, 1f));
            GUI.Label(new Rect(700, 310, 520, 70), "EXPEDITION PAUSED", _heading);
            DrawSelection(new Rect(710, 420, 500, 95), _pauseSelection == 0);
            if (GUI.Button(new Rect(720, 430, 480, 75), "CONTINUE", _button)) _director.TogglePause();
            DrawSelection(new Rect(710, 525, 500, 95), _pauseSelection == 1);
            if (GUI.Button(new Rect(720, 535, 480, 75), "SETTINGS", _button)) _director.OpenSettings();
            DrawSelection(new Rect(710, 630, 500, 95), _pauseSelection == 2);
            if (GUI.Button(new Rect(720, 640, 480, 75), "RETURN TO CAMP", _button)) _director.ReturnToMenu();
        }

        private void DrawSettings()
        {
            DrawPanel(new Rect(0, 0, 1920, 1080), new Color(0.008f, 0.022f, 0.032f, 1f));
            GUI.Label(new Rect(460, 35, 1000, 75), "PRESENTATION & CONTROLS", _title);
            GUI.Label(new Rect(410, 105, 1100, 40),
                "ACCESSIBILITY, AUDIO AND P1 KEYBOARD BINDINGS — GAMEPAD GLYPHS FOLLOW THE ACTIVE DEVICE", _small);

            var data = PresentationPreferences.Data;
            DrawPanel(new Rect(120, 175, 790, 710), new Color(0.035f, 0.08f, 0.108f, 1f));
            DrawPanel(new Rect(1010, 175, 790, 710), new Color(0.035f, 0.08f, 0.108f, 1f));
            GUI.Label(new Rect(160, 195, 710, 45), "ACCESSIBILITY & AUDIO", _heading);
            GUI.Label(new Rect(1050, 195, 710, 45), "KEYBOARD — PLAYER 1", _heading);

            var labels = new[] { "UI SCALE", "HIGH CONTRAST", "REDUCED FLASHES", "SCREEN SHAKE", "MASTER", "MUSIC", "SFX" };
            var values = new[]
            {
                $"{data.UiScale * 100f:0}%", data.HighContrast ? "ON" : "OFF",
                data.ReducedFlashes ? "ON" : "OFF", $"{data.ScreenShake * 100f:0}%",
                $"{data.MasterVolume * 100f:0}%", $"{data.MusicVolume * 100f:0}%",
                $"{data.SfxVolume * 100f:0}%"
            };
            for (var i = 0; i < labels.Length; i++)
            {
                var rect = new Rect(155, 270 + i * 78, 720, 62);
                DrawSelection(new Rect(rect.x - 5, rect.y - 5, rect.width + 10, rect.height + 10), _settingsSelection == i);
                DrawPanel(rect, new Color(0.018f, 0.05f, 0.07f, 1f));
                GUI.Label(new Rect(rect.x + 18, rect.y, 300, rect.height), labels[i], _cardTitle);
                if (GUI.Button(new Rect(rect.x + 355, rect.y + 8, 54, 46), "−", _button)) { _settingsSelection = i; AdjustSetting(i, -1); }
                GUI.Label(new Rect(rect.x + 420, rect.y, 210, rect.height), values[i], _center);
                if (GUI.Button(new Rect(rect.x + 645, rect.y + 8, 54, 46), "+", _button)) { _settingsSelection = i; AdjustSetting(i, 1); }
            }

            for (var i = 0; i < RebindableActions.Length; i++)
            {
                var selection = i + 7;
                var rect = new Rect(1045, 255 + i * 62, 720, 48);
                DrawSelection(new Rect(rect.x - 5, rect.y - 5, rect.width + 10, rect.height + 10), _settingsSelection == selection);
                DrawPanel(rect, new Color(0.018f, 0.05f, 0.07f, 1f));
                GUI.Label(new Rect(rect.x + 15, rect.y, 315, rect.height), BindingLabel(RebindableActions[i]), _small);
                var capture = LocalInputRouter.IsRebinding && LocalInputRouter.PendingBinding == RebindableActions[i];
                var value = capture ? "PRESS ANY KEY…" : InputBindingProfile.Display(data.Keyboard.Get(RebindableActions[i]));
                if (GUI.Button(new Rect(rect.x + 350, rect.y + 4, 350, 40), value, _button))
                {
                    _settingsSelection = selection;
                    LocalInputRouter.BeginRebind(RebindableActions[i]);
                }
            }

            DrawSelection(new Rect(580, 925, 350, 85), _settingsSelection == 16);
            if (GUI.Button(new Rect(590, 935, 330, 65), "RESTORE DEFAULTS", _button))
            {
                _settingsSelection = 16;
                PresentationPreferences.ResetToDefaults();
            }
            DrawSelection(new Rect(990, 925, 350, 85), _settingsSelection == 17);
            if (GUI.Button(new Rect(1000, 935, 330, 65), "BACK", _button)) _director.CloseSettings();
            GUI.Label(new Rect(520, 1010, 880, 35),
                $"{Prompt(BindingAction.MoveUp)} {Prompt(BindingAction.MoveDown)} NAVIGATE   •   LEFT/RIGHT ADJUST   •   {Prompt(BindingAction.Back)} BACK", _micro);
        }

        private static string BindingLabel(BindingAction action)
        {
            switch (action)
            {
                case BindingAction.MoveUp: return "MOVE UP / MENU UP";
                case BindingAction.MoveDown: return "MOVE DOWN / MENU DOWN";
                case BindingAction.MoveLeft: return "MOVE LEFT / MENU LEFT";
                case BindingAction.MoveRight: return "MOVE RIGHT / MENU RIGHT";
                case BindingAction.BuildDetails: return "EXPEDITION BUILD";
                default: return action.ToString().ToUpperInvariant();
            }
        }

        private void DrawResults(bool victory)
        {
            DrawPanel(new Rect(0, 0, 1920, 1080), new Color(0.01f, 0.025f, 0.04f, 0.82f));
            var panel = new Rect(460, 120, 1000, 820);
            DrawPanel(panel, new Color(0.055f, 0.105f, 0.14f, 1f));
            DrawPanel(new Rect(panel.x, panel.y, panel.width, 7), new Color(0.32f, 0.68f, 0.78f));

            var titleY = panel.y + 32f;
            GUI.Label(new Rect(panel.x + 60, titleY, panel.width - 120, 96),
                victory ? "A SAGA IS BORN" : "THE ICE CLAIMS THE EXPEDITION", _resultTitle);

            var summaryTop = titleY + 108f;
            var summary = new Rect(panel.x + 70, summaryTop, panel.width - 140, 168);
            DrawPanel(summary, new Color(0.018f, 0.048f, 0.068f, 1f));
            GUI.Label(new Rect(summary.x + 25, summary.y + 14, summary.width - 50, 25), "EXPEDITION", _micro);
            GUI.Label(new Rect(summary.x + 30, summary.y + 40, summary.width - 60, 52),
                _director.SelectedMap.Name.ToUpperInvariant(), _center);
            DrawPanel(new Rect(summary.x + 30, summary.y + 98, summary.width - 60, 2), new Color(0.25f, 0.48f, 0.57f));
            GUI.Label(new Rect(summary.x + 30, summary.y + 106, summary.width - 60, 52),
                $"TIME  {FormatTime(_director.Elapsed)}     •     ENEMIES  {_director.Kills}     •     RENOWN  {_director.RunRenown}\nSEED  {_director.RunSeed}",
                _small);

            var sectionTop = summary.yMax + 22f;
            if (victory)
            {
                var relicId = _director.Route.ResolveVictoryRelicId();
                if (!string.IsNullOrEmpty(relicId))
                {
                    var relicRect = new Rect(panel.x + 70, sectionTop, panel.width - 140, 132);
                    DrawPanel(relicRect, new Color(0.018f, 0.048f, 0.068f, 1f));
                    DrawPanel(new Rect(relicRect.x, relicRect.y, relicRect.width, 4), PresentationTheme.Accent);
                    GUI.Label(new Rect(relicRect.x + 22, relicRect.y + 12, relicRect.width - 44, 24), "RELIC EARNED", _micro);
                    GUI.Label(new Rect(relicRect.x + 22, relicRect.y + 34, relicRect.width - 44, 34),
                        RelicDisplayName(relicId).ToUpperInvariant(), _center);
                    GUI.Label(new Rect(relicRect.x + 22, relicRect.y + 68, relicRect.width - 44, 52),
                        RelicExplanation(relicId), _resultBody);
                    sectionTop = relicRect.yMax + 20f;
                }
            }

            var message = victory
                ? "The Jotunn falls and your expedition is complete. Renown and relic progress are saved."
                : "No expedition is wasted. Your renown is saved — return to camp and try again.";
            GUI.Label(new Rect(panel.x + 90, sectionTop, panel.width - 180, 72), message, _resultBody);
            sectionTop += 78f;

            var renownRect = new Rect(panel.x + 70, sectionTop, panel.width - 140, 132);
            DrawPanel(renownRect, new Color(0.018f, 0.048f, 0.068f, 1f));
            DrawBorder(renownRect, PresentationTheme.Accent, 3f);
            GUI.Label(new Rect(renownRect.x + 22, renownRect.y + 12, renownRect.width - 44, 24), "RENOWN FOR CAMP UNLOCKS", _micro);
            GUI.Label(new Rect(renownRect.x + 22, renownRect.y + 36, renownRect.width - 44, 34),
                $"+{_director.LastRunRenownEarned} THIS RUN     •     {SaveService.AvailableRenown()} TO SPEND AT CAMP", _heading);

            var affordableUnlockId = SharedMetaProgressionModel.FindCheapestAffordableUnlockId(SaveService.Data);
            if (affordableUnlockId != null)
            {
                var unlock = SharedMetaProgressionModel.FindUnlock(affordableUnlockId);
                if (unlock.HasValue)
                {
                    GUI.Label(new Rect(renownRect.x + 22, renownRect.y + 76, renownRect.width - 44, 48),
                        $"RETURN TO CAMP → UNLOCK BOARD → unlock {unlock.Value.DisplayName} for {unlock.Value.RenownCost} renown.",
                        _body);
                }
            }
            else
            {
                GUI.Label(new Rect(renownRect.x + 22, renownRect.y + 76, renownRect.width - 44, 48),
                    "Return to camp and open the Unlock Board after you earn enough renown for the next survivor or route.",
                    _body);
            }

            sectionTop = renownRect.yMax + 16f;

            const float buttonTop = 848f;
            const float buttonHeight = 72f;
            DrawSelection(new Rect(535, buttonTop - 10, 410, 92), _resultSelection == 0);
            if (GUI.Button(new Rect(545, buttonTop, 390, buttonHeight), "REPLAY SAME SEED", _button))
            {
                _director.ReplayRun();
            }

            DrawSelection(new Rect(955, buttonTop - 10, 410, 92), _resultSelection == 1);
            if (GUI.Button(new Rect(965, buttonTop, 390, buttonHeight), "RETURN TO CAMP", _button))
            {
                _director.ReturnToMenu();
            }
        }

        private void DrawWeaponStatColumn(Rect rect, WeaponInstance weapon, float rowHeight = 29f)
        {
            var item = ItemCatalog.Find(weapon.WeaponId);
            var title = item != null ? item.ShortName.ToUpperInvariant() : weapon.WeaponId.ToUpperInvariant();
            var accent = item != null ? item.Color : PresentationTheme.Accent;
            var labels = new List<string>();
            var values = new List<string>();
            labels.Add("DAMAGE");
            values.Add($"{weapon.Damage:0.0}");
            labels.Add("INTERVAL");
            values.Add($"{weapon.Cooldown:0.000}s");
            labels.Add("RATE");
            values.Add($"{1f / Mathf.Max(0.01f, weapon.Cooldown):0.00}/s");

            switch (weapon.Behavior)
            {
                case WeaponBehaviorKind.ProjectileVolley:
                    labels.Add("PROJECTILES");
                    values.Add($"{weapon.Count}");
                    labels.Add("PIERCE");
                    values.Add($"{weapon.Pierce}");
                    labels.Add("CRITICAL");
                    values.Add($"{weapon.CriticalChance * 100f:0}%");
                    labels.Add("PROJ SPEED");
                    values.Add($"{weapon.ProjectileSpeed:0.0}");
                    labels.Add("LIFETIME");
                    values.Add($"{weapon.ProjectileDuration:0.0}s");
                    labels.Add("HIT RADIUS");
                    values.Add($"{weapon.HitRadius:0.00}/{weapon.CriticalHitRadius:0.00}");
                    labels.Add("KNOCKBACK");
                    values.Add($"{weapon.Knockback:0.00}/{weapon.CriticalKnockback:0.00}");
                    break;
                case WeaponBehaviorKind.OwnerPulse:
                    labels.Add("RADIUS");
                    values.Add($"{weapon.PulseRadius:0.00}");
                    labels.Add("KNOCKBACK");
                    values.Add($"{weapon.PulseKnockback:0.00}");
                    if (weapon.HealAmount > 0f)
                    {
                        labels.Add("HEAL PULSE");
                        values.Add($"{weapon.HealAmount:0.0}");
                    }
                    else
                    {
                        labels.Add("HEAL / HIT");
                        values.Add($"{weapon.PulseHealPerHit:0.00}");
                        labels.Add("HEAL CAP");
                        values.Add($"{weapon.PulseHealCap:0.0}");
                    }
                    break;
                case WeaponBehaviorKind.RadialBurst:
                    labels.Add("BURST COUNT");
                    values.Add($"{weapon.RadialBurstCount}");
                    labels.Add("RADIUS");
                    values.Add($"{weapon.RadialRadius:0.00}");
                    labels.Add("PROJ SPEED");
                    values.Add($"{weapon.ProjectileSpeed:0.0}");
                    labels.Add("KNOCKBACK");
                    values.Add($"{weapon.Knockback:0.00}");
                    break;
                case WeaponBehaviorKind.OrbitBlade:
                    labels.Add("BLADES");
                    values.Add($"{weapon.Count}");
                    labels.Add("ORBIT RADIUS");
                    values.Add($"{weapon.OrbitRadius:0.00}");
                    labels.Add("ORBIT SPEED");
                    values.Add($"{weapon.OrbitSpeedDegrees:0.0}°/s");
                    labels.Add("HIT RADIUS");
                    values.Add($"{weapon.HitRadius:0.00}");
                    break;
            }

            labels.Add("EVOLUTION");
            if (weapon.IsEvolved && weapon.ExplosionDamageMultiplier > 0f)
            {
                values.Add(FitStatValue($"{weapon.ExplosionDamageMultiplier * 100f:0}% @ {weapon.ExplosionRadius:0.00}", 14));
            }
            else if (weapon.IsEvolved)
            {
                values.Add("ACTIVE");
            }
            else
            {
                values.Add("NONE");
            }

            DrawStatColumn(rect, title, labels.ToArray(), values.ToArray(), accent, rowHeight);
        }

        private static string RelicDisplayName(string relicId)
        {
            switch (relicId)
            {
                case "relic.jotunn_echo_warden": return "Jotunn Echo Warden";
                case "relic.jotunn_echo": return "Jotunn Echo";
                default:
                    if (string.IsNullOrEmpty(relicId))
                    {
                        return string.Empty;
                    }

                    return relicId.Replace("relic.", string.Empty).Replace('_', ' ');
            }
        }

        private static string RelicExplanation(string relicId)
        {
            switch (relicId)
            {
                case "relic.jotunn_echo_warden":
                    return "Rare trophy — Scout victory plus every optional Rune Shard recovered. Saved permanently to your profile.";
                case "relic.jotunn_echo":
                    return "Victory trophy from clearing the Frostbound Shore. Saved permanently to your profile.";
                default:
                    return "Saved permanently to your profile.";
            }
        }

        private static string FitStatValue(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength - 1) + "…";
        }

        private void DrawStatColumn(Rect rect, string title, string[] labels, string[] values, Color accent,
            float rowHeight = 29f)
        {
            DrawPanel(rect, new Color(0.018f, 0.048f, 0.068f, 1f));
            DrawPanel(new Rect(rect.x, rect.y, rect.width, 4), accent);
            GUI.Label(new Rect(rect.x + 14, rect.y + 7, rect.width - 28, 28), title, _statSection);
            var rowY = rect.y + 38f;
            var count = Mathf.Min(labels.Length, values.Length);
            const float labelWidthRatio = 0.5f;

            for (var i = 0; i < count; i++)
            {
                if ((i & 1) == 1)
                {
                    DrawPanel(new Rect(rect.x + 10, rowY, rect.width - 20, rowHeight),
                        new Color(0.04f, 0.08f, 0.1f, 0.75f));
                }

                GUI.Label(new Rect(rect.x + 14, rowY + 3, rect.width * labelWidthRatio - 14, rowHeight - 6), labels[i],
                    _statLabel);
                GUI.Label(new Rect(rect.x + rect.width * labelWidthRatio, rowY + 3, rect.width * (1f - labelWidthRatio) - 14,
                        rowHeight - 6),
                    values[i], _statValue);
                rowY += rowHeight;
            }
        }

        private void DrawPerformancePanel()
        {
            var rect = new Rect(1470, 790, 420, 218);
            DrawPanel(rect, new Color(0.012f, 0.035f, 0.048f, 0.96f));
            DrawBorder(rect, new Color(0.3f, 0.68f, 0.76f, 0.9f), 3f);
            GUI.Label(new Rect(rect.x + 15, rect.y + 10, rect.width - 30, 28), "PRODUCTION METRICS — F3", _statSection);
            GUI.Label(new Rect(rect.x + 18, rect.y + 43, rect.width - 36, 154),
                $"FPS  {_director.Metrics.FramesPerSecond:0}   •   FRAME  {_director.Metrics.FrameMilliseconds:0.0} ms   •   WORST  {_director.Metrics.WorstFrameMilliseconds:0.0} ms\n" +
                $"ACTIVE  ENEMY {_director.Enemies.Count}   AXE {_director.ActiveProjectiles}   XP {_director.ActiveGems}\n" +
                $"POOL  ENEMY {_director.PooledEnemyCount}   AXE {_director.PooledProjectileCount}   XP {_director.PooledGemCount}\n" +
                $"PRESENT  VFX {_director.Presentation.ActiveVfx}   SFX {_director.Presentation.ActiveSfxVoices}   MUSIC {_director.Presentation.MusicState}\n" +
                $"CREATED {_director.CreatedPooledObjects}   •   REUSED {_director.ReusedPooledObjects}\n" +
                $"GRID {_director.SpatialCellCount} CELLS   •   QUERIES {_director.Metrics.SpatialQueries}\n" +
                $"SEED {_director.RunSeed}   •   {ProductionContentRuntime.SourceLabel}", _microLeft);
        }

        private void DrawCodexView()
        {
            DrawPanel(new Rect(0, 0, 1920, 1080), new Color(0.018f, 0.045f, 0.065f, 1f));
            GUI.Label(new Rect(420, 48, 1080, 80), "EXPEDITION CODEX", _title);
            GUI.Label(new Rect(460, 128, 1000, 36), "Discoveries from your runs — evolutions show recipes once hinted.", _small);

            var categories = CodexCategoryLabels();
            var categoryRect = new Rect(80, 190, 320, 760);
            DrawPanel(categoryRect, new Color(0.045f, 0.085f, 0.115f, 0.96f));

            for (var i = 0; i < categories.Length; i++)
            {
                var row = new Rect(categoryRect.x + 16, categoryRect.y + 20 + i * 52, categoryRect.width - 32, 44);
                var selected = i == _codexCategorySelection;
                DrawPanel(row, selected ? new Color(0.09f, 0.18f, 0.22f, 1f) : new Color(0.03f, 0.06f, 0.08f, 1f));
                if (selected)
                {
                    DrawBorder(row, PresentationTheme.Accent, 3f);
                }

                GUI.Label(new Rect(row.x + 12, row.y + 10, row.width - 24, 24), categories[i], _cardTitle);
            }

            var detailRect = new Rect(430, 190, 1410, 760);
            DrawPanel(detailRect, new Color(0.045f, 0.085f, 0.115f, 0.96f));
            GUI.Label(new Rect(detailRect.x + 28, detailRect.y + 20, detailRect.width - 56, 32),
                categories[Mathf.Clamp(_codexCategorySelection, 0, categories.Length - 1)], _heading);

            var entries = CollectVisibleCodexEntries(_codexCategorySelection);
            if (entries.Count == 0)
            {
                GUI.Label(new Rect(detailRect.x + 28, detailRect.y + 72, detailRect.width - 56, 48),
                    "NO ENTRIES YET — PLAY EXPEDITIONS TO DISCOVER GEAR, EVOLUTIONS AND RELICS.", _body);
            }
            else
            {
                var entryIndex = Mathf.Clamp(_codexEntrySelection, 0, entries.Count - 1);
                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    var visibility = SharedMetaProgressionModel.ResolveCodexVisibility(SaveService.Data, entry);
                    var row = new Rect(detailRect.x + 28, detailRect.y + 72 + i * 88, detailRect.width - 56, 78);
                    var selected = i == entryIndex;
                    DrawPanel(row, selected ? new Color(0.07f, 0.13f, 0.17f, 1f) : new Color(0.025f, 0.055f, 0.075f, 1f));
                    if (selected)
                    {
                        DrawBorder(row, new Color(0.93f, 0.7f, 0.24f), 3f);
                    }

                    if (visibility == CodexVisibility.Hint)
                    {
                        GUI.Label(new Rect(row.x + 14, row.y + 10, row.width - 28, 24), "EVOLUTION RECIPE HINT", _campEyebrow);
                        GUI.Label(new Rect(row.x + 14, row.y + 34, row.width - 28, 36), entry.HintCondition, _microLeft);
                    }
                    else
                    {
                        GUI.Label(new Rect(row.x + 14, row.y + 10, row.width - 28, 26), entry.DisplayName.ToUpperInvariant(), _itemTitle);
                        GUI.Label(new Rect(row.x + 14, row.y + 38, row.width - 28, 32), entry.Description, _microLeft);
                    }
                }
            }

            GUI.Label(new Rect(610, 980, 700, 40),
                $"{Prompt(BindingAction.MoveUp)} {Prompt(BindingAction.MoveDown)} CATEGORY   •   {Prompt(BindingAction.Back)} CAMP", _small);
        }

        private static string[] CodexCategoryLabels()
        {
            return new[]
            {
                "HEROES",
                "EXPEDITIONS",
                "WEAPONS",
                "GEAR",
                "EVOLUTIONS",
                "RELICS"
            };
        }

        private static int CodexCategoryCount() => CodexCategoryLabels().Length;

        private static CodexCategory ResolveCodexCategoryIndex(int index)
        {
            switch (index)
            {
                case 0: return CodexCategory.Hero;
                case 1: return CodexCategory.Expedition;
                case 2: return CodexCategory.Weapon;
                case 3: return CodexCategory.Gear;
                case 4: return CodexCategory.Evolution;
                default: return CodexCategory.Relic;
            }
        }

        private static List<CodexDefinition> CollectVisibleCodexEntries(int categoryIndex)
        {
            var category = ResolveCodexCategoryIndex(categoryIndex);
            var entries = new List<CodexDefinition>();
            var catalog = SharedMetaProgressionModel.CodexEntries;

            for (var i = 0; i < catalog.Count; i++)
            {
                var entry = catalog[i];
                if (entry.Category != category)
                {
                    continue;
                }

                var visibility = SharedMetaProgressionModel.ResolveCodexVisibility(SaveService.Data, entry);
                if (visibility == CodexVisibility.Hidden)
                {
                    continue;
                }

                entries.Add(entry);
            }

            return entries;
        }

        private static int CountVisibleCodexEntries(int categoryIndex)
        {
            return CollectVisibleCodexEntries(categoryIndex).Count;
        }

        private static int CountPurchasableUnlockRows()
        {
            var count = 0;

            for (var i = 0; i < SharedMetaProgressionModel.Unlocks.Count; i++)
            {
                if (SharedMetaProgressionModel.Unlocks[i].RenownCost > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private void CycleUnlockSelection(int direction)
        {
            var count = CountPurchasableUnlockRows();
            if (count <= 0)
            {
                _unlockSelection = -1;
                return;
            }

            if (_unlockSelection < 0)
            {
                _unlockSelection = direction > 0 ? 0 : count - 1;
            }
            else
            {
                _unlockSelection = Wrap(_unlockSelection + direction, count);
            }

            NavigateCue();
        }

        private void FocusAffordableUnlockIfAny()
        {
            _unlockSelection = FindAffordableUnlockRowIndex();
        }

        private static int FindAffordableUnlockRowIndex()
        {
            var row = 0;

            for (var i = 0; i < SharedMetaProgressionModel.Unlocks.Count; i++)
            {
                var unlock = SharedMetaProgressionModel.Unlocks[i];
                if (unlock.RenownCost <= 0)
                {
                    continue;
                }

                if (SaveService.CanPurchaseUnlock(unlock.ContentId))
                {
                    return row;
                }

                row++;
            }

            return -1;
        }

        private void ShowCampPurchaseMessage(string message)
        {
            _campPurchaseMessage = message;
            _campPurchaseMessageTimer = 4f;
        }

        private bool TryPurchaseUnlockByContentId(string contentId)
        {
            var result = SaveService.TryPurchaseUnlock(contentId);
            if (result.Success)
            {
                ShowCampPurchaseMessage($"UNLOCKED — {result.Message.ToUpperInvariant()}");
                FocusAffordableUnlockIfAny();
                ConfirmCue();
                return true;
            }

            return false;
        }

        private bool TryPurchaseUnlockAt(int rowIndex)
        {
            var row = 0;

            for (var i = 0; i < SharedMetaProgressionModel.Unlocks.Count; i++)
            {
                var unlock = SharedMetaProgressionModel.Unlocks[i];
                if (unlock.RenownCost <= 0)
                {
                    continue;
                }

                if (row == rowIndex)
                {
                    if (!SaveService.CanPurchaseUnlock(unlock.ContentId))
                    {
                        ShowCampPurchaseMessage("NOT ENOUGH RENOWN — FINISH MORE EXPEDITIONS FIRST");
                        return false;
                    }

                    return TryPurchaseUnlockByContentId(unlock.ContentId);
                }

                row++;
            }

            return false;
        }

        private static int ResolveUnlockCost(string contentId)
        {
            var unlock = SharedMetaProgressionModel.FindUnlock(contentId);
            if (!unlock.HasValue)
            {
                return 0;
            }

            return unlock.Value.RenownCost;
        }

        private void DrawSelectableButton(Rect rect, string label, int index, ref int selected, System.Action action)
        {
            DrawSelection(new Rect(rect.x - 10, rect.y - 10, rect.width + 20, rect.height + 20), selected == index);
            if (GUI.Button(rect, label, _button))
            {
                selected = index;
                ConfirmCue();
                action();
            }
        }

        private static string Prompt(BindingAction action) =>
            InputGlyphs.Prompt(action, LocalInputRouter.CurrentPromptDevice);

        private void NavigateCue() => _director.Present(PresentationCue.Navigate, Vector2.zero, PresentationTheme.Accent);

        private void ConfirmCue() => _director.Present(PresentationCue.Confirm, Vector2.zero, PresentationTheme.Accent);

        private static int Wrap(int value, int count)
        {
            if (count <= 0) return 0;
            if (value < 0) return count - 1;
            if (value >= count) return 0;
            return value;
        }

        private static string FormatTime(float seconds)
        {
            var total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }

        private void DrawBar(Rect rect, float fill, Color color, string label, GUIStyle style = null)
        {
            DrawPanel(rect, new Color(0.02f, 0.035f, 0.045f, 1f));
            DrawPanel(new Rect(rect.x + 2, rect.y + 2, (rect.width - 4) * Mathf.Clamp01(fill), rect.height - 4), color);
            if (!string.IsNullOrEmpty(label)) GUI.Label(rect, label, style ?? _small);
        }

        private static void DrawSelection(Rect rect, bool selected)
        {
            if (selected) DrawBorder(rect, PresentationTheme.Accent, 6f);
        }

        private static void DrawBorder(Rect rect, Color color, float thickness)
        {
            DrawPanel(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawPanel(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawPanel(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawPanel(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static void DrawPanel(Rect rect, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, RuntimeAssets.White);
            GUI.color = old;
        }

        private static void DrawLetterbox(Vector3 offset, float scale)
        {
            var color = GUI.color;
            GUI.color = new Color(0.006f, 0.016f, 0.024f, 1f);
            if (offset.x > 0f)
            {
                GUI.DrawTexture(new Rect(0f, 0f, offset.x, Screen.height), RuntimeAssets.White);
                GUI.DrawTexture(new Rect(offset.x + 1920f * scale, 0f, offset.x, Screen.height), RuntimeAssets.White);
            }
            if (offset.y > 0f)
            {
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, offset.y), RuntimeAssets.White);
                GUI.DrawTexture(new Rect(0f, offset.y + 1080f * scale, Screen.width, offset.y), RuntimeAssets.White);
            }
            GUI.color = color;
        }

        private void EnsureStyles()
        {
            if (_title != null && _styleRevision == PresentationPreferences.Revision) return;
            _styleRevision = PresentationPreferences.Revision;
            _title = MakeStyle(48, FontStyle.Bold, PresentationTheme.TextPrimary, TextAnchor.MiddleCenter);
            _heading = MakeStyle(31, FontStyle.Bold, PresentationTheme.TextPrimary, TextAnchor.MiddleCenter);
            _cardTitle = MakeStyle(24, FontStyle.Bold, PresentationTheme.Accent, TextAnchor.MiddleLeft);
            _itemTitle = MakeStyle(13, FontStyle.Bold, PresentationTheme.Accent, TextAnchor.UpperLeft);
            _itemTitle.wordWrap = true;
            _body = MakeStyle(22, FontStyle.Normal, PresentationTheme.TextPrimary, TextAnchor.UpperLeft);
            _body.wordWrap = true;
            _small = MakeStyle(17, FontStyle.Bold, PresentationTheme.TextSecondary, TextAnchor.MiddleCenter);
            _micro = MakeStyle(13, FontStyle.Bold, PresentationTheme.TextSecondary, TextAnchor.MiddleCenter);
            _micro.wordWrap = true;
            _microLeft = MakeStyle(12, FontStyle.Bold, PresentationTheme.TextSecondary, TextAnchor.MiddleLeft);
            _microLeft.wordWrap = true;
            _badge = MakeStyle(14, FontStyle.Bold, new Color(0.025f, 0.065f, 0.085f), TextAnchor.MiddleCenter);
            _gridBadge = MakeStyle(10, FontStyle.Bold, new Color(0.025f, 0.065f, 0.085f), TextAnchor.MiddleCenter);
            _gridBadge.wordWrap = false;
            _gridBadge.clipping = TextClipping.Overflow;
            _gridBadge.padding = new RectOffset(4, 4, 2, 2);
            _mapTitle = MakeStyle(25, FontStyle.Bold, PresentationTheme.TextPrimary, TextAnchor.MiddleCenter);
            _mapTitle.wordWrap = true;
            _resultTitle = MakeStyle(39, FontStyle.Bold, PresentationTheme.TextPrimary, TextAnchor.MiddleCenter);
            _resultTitle.wordWrap = true;
            _resultBody = MakeStyle(18, FontStyle.Normal, PresentationTheme.TextSecondary, TextAnchor.MiddleCenter);
            _resultBody.wordWrap = true;
            _statSection = MakeStyle(14, FontStyle.Bold, PresentationTheme.Accent, TextAnchor.MiddleLeft);
            _statLabel = MakeStyle(14, FontStyle.Bold, PresentationTheme.TextSecondary, TextAnchor.UpperLeft);
            _statValue = MakeStyle(14, FontStyle.Bold, PresentationTheme.TextPrimary, TextAnchor.UpperRight);
            _statValue.wordWrap = false;
            _statValue.clipping = TextClipping.Clip;
            _rewardEffect = MakeStyle(16, FontStyle.Bold, PresentationTheme.Accent, TextAnchor.UpperLeft);
            _rewardEffect.wordWrap = true;
            _rewardDescription = MakeStyle(18, FontStyle.Normal, PresentationTheme.TextPrimary, TextAnchor.UpperLeft);
            _rewardDescription.wordWrap = true;
            _rewardHint = MakeStyle(14, FontStyle.Bold, PresentationTheme.TextSecondary, TextAnchor.UpperLeft);
            _rewardHint.wordWrap = true;
            _rewardHint.clipping = TextClipping.Overflow;
            _rewardCategory = MakeStyle(14, FontStyle.Bold, PresentationTheme.TextSecondary, TextAnchor.MiddleLeft);
            _itemProgress = MakeStyle(12, FontStyle.Bold, PresentationTheme.TextPrimary, TextAnchor.UpperLeft);
            _itemProgress.wordWrap = true;
            _itemProgress.clipping = TextClipping.Overflow;
            _itemNext = MakeStyle(11, FontStyle.Bold, PresentationTheme.Accent, TextAnchor.UpperLeft);
            _itemNext.wordWrap = true;
            _itemNext.clipping = TextClipping.Overflow;
            _buildLoadout = MakeStyle(13, FontStyle.Bold, PresentationTheme.TextSecondary, TextAnchor.MiddleRight);
            _buildLoadout.wordWrap = true;
            _campEyebrow = MakeStyle(13, FontStyle.Bold, PresentationTheme.Accent, TextAnchor.MiddleLeft);
            _campLedger = MakeStyle(15, FontStyle.Bold, PresentationTheme.TextPrimary, TextAnchor.MiddleLeft);
            _campLedger.wordWrap = true;
            _campLocked = MakeStyle(12, FontStyle.Bold, new Color(0.42f, 0.48f, 0.52f), TextAnchor.UpperLeft);
            _campLocked.wordWrap = true;
            _campLeaderName = MakeStyle(20, FontStyle.Bold, PresentationTheme.TextPrimary, TextAnchor.MiddleCenter);
            _campLeaderName.wordWrap = true;
            _campLeaderSubtitle = MakeStyle(13, FontStyle.Bold, PresentationTheme.TextSecondary, TextAnchor.UpperCenter);
            _campLeaderSubtitle.wordWrap = true;
            _campLeaderSubtitle.clipping = TextClipping.Overflow;
            _campUnlockCost = MakeStyle(12, FontStyle.Bold, PresentationTheme.TextSecondary, TextAnchor.MiddleRight);
            _campUnlockCost.wordWrap = false;
            _campUnlockCost.padding = new RectOffset(4, 8, 0, 0);
            _campUnlockShortfall = MakeStyle(11, FontStyle.Bold, new Color(0.42f, 0.48f, 0.52f), TextAnchor.MiddleRight);
            _campUnlockShortfall.wordWrap = false;
            _campUnlockShortfall.padding = new RectOffset(4, 8, 0, 0);
            _campUnlockStatus = MakeStyle(13, FontStyle.Bold, PresentationTheme.Accent, TextAnchor.MiddleRight);
            _campUnlockStatus.wordWrap = false;
            _campUnlockStatus.padding = new RectOffset(4, 8, 0, 0);
            _center = MakeStyle(22, FontStyle.Bold, PresentationTheme.TextPrimary, TextAnchor.MiddleCenter);
            _center.wordWrap = true;
            var buttonText = new Color(0.025f, 0.065f, 0.085f);
            _button = new GUIStyle(GUI.skin.button)
            {
                fontSize = PresentationTheme.FontSize(23),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 6, 6)
            };
            SetAllTextColors(_button, buttonText);
            _button.normal.background = MakeButtonTexture(PresentationTheme.Accent);
            _button.hover.background = MakeButtonTexture(new Color(1f, 0.86f, 0.34f));
            _button.active.background = MakeButtonTexture(new Color(0.7f, 0.5f, 0.12f));
            _button.focused.background = _button.hover.background;
            _button.onNormal.background = _button.normal.background;
            _button.onHover.background = _button.hover.background;
            _button.onActive.background = _button.active.background;
            _button.onFocused.background = _button.hover.background;
            var selectButtonText = new Color(0.04f, 0.08f, 0.1f);
            _selectActionButton = new GUIStyle(GUI.skin.button)
            {
                fontSize = PresentationTheme.FontSize(24),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 6, 6)
            };
            SetAllTextColors(_selectActionButton, selectButtonText);
            _selectActionButton.normal.background = MakeButtonTexture(new Color(1f, 0.84f, 0.18f));
            _selectActionButton.hover.background = MakeButtonTexture(new Color(1f, 0.92f, 0.38f));
            _selectActionButton.active.background = MakeButtonTexture(new Color(0.88f, 0.62f, 0.08f));
            _selectActionButton.focused.background = _selectActionButton.hover.background;
            _selectActionButton.onNormal.background = _selectActionButton.normal.background;
            _selectActionButton.onHover.background = _selectActionButton.hover.background;
            _selectActionButton.onActive.background = _selectActionButton.active.background;
            _selectActionButton.onFocused.background = _selectActionButton.hover.background;

            var readyButtonText = new Color(0.03f, 0.1f, 0.06f);
            _readyActionButton = new GUIStyle(GUI.skin.button)
            {
                fontSize = PresentationTheme.FontSize(24),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 6, 6)
            };
            SetAllTextColors(_readyActionButton, readyButtonText);
            _readyActionButton.normal.background = MakeButtonTexture(new Color(0.34f, 0.82f, 0.52f));
            _readyActionButton.hover.background = MakeButtonTexture(new Color(0.46f, 0.92f, 0.62f));
            _readyActionButton.active.background = MakeButtonTexture(new Color(0.22f, 0.62f, 0.38f));
            _readyActionButton.focused.background = _readyActionButton.hover.background;
            _readyActionButton.onNormal.background = _readyActionButton.normal.background;
            _readyActionButton.onHover.background = _readyActionButton.hover.background;
            _readyActionButton.onActive.background = _readyActionButton.active.background;
            _readyActionButton.onFocused.background = _readyActionButton.hover.background;

            _readyConfirmedLabel = MakeStyle(24, FontStyle.Bold, new Color(0.78f, 0.98f, 0.84f), TextAnchor.MiddleCenter);
            _readyConfirmedLabel.padding = new RectOffset(10, 10, 6, 6);
        }

        private static GUIStyle MakeStyle(int size, FontStyle fontStyle, Color color, TextAnchor anchor)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = PresentationTheme.FontSize(size),
                fontStyle = fontStyle,
                alignment = anchor,
                padding = new RectOffset(8, 8, 4, 4)
            };
            SetAllTextColors(style, color);
            return style;
        }

        private static void SetAllTextColors(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
        }

        private static Texture2D MakeButtonTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
