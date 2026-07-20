using UnityEngine;

namespace ProjectExpedition
{
    public sealed class GameHUD : MonoBehaviour
    {
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
        private GUIStyle _mapTitle;
        private GUIStyle _resultTitle;
        private GUIStyle _statSection;
        private GUIStyle _statLabel;
        private GUIStyle _statValue;
        private GUIStyle _rewardEffect;
        private GUIStyle _rewardDescription;
        private GUIStyle _itemProgress;
        private string _announcement;
        private float _announcementTimer;
        private int _mainSelection;
        private readonly int[] _characterSelections = { 0, 1 };
        private readonly bool[] _characterReady = new bool[2];
        private int _mapSelection;
        private int _levelSelection;
        private int _pauseSelection;
        private int _resultSelection;
        private int _settingsSelection;
        private int _styleRevision = -1;
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
            if (_director == null) return;
            switch (_director.State)
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
            var direction = LocalInputRouter.AnyMenuHorizontalPressed();
            if (direction != 0) { _mainSelection = Wrap(_mainSelection + direction, 3); NavigateCue(); }
            if (LocalInputRouter.AnyMenuSubmitPressed()) ActivateMainSelection();
        }

        private void ActivateMainSelection()
        {
            if (_mainSelection == 0) PrepareCharacterSelection(1);
            else if (_mainSelection == 1) PrepareCharacterSelection(2);
            else _director.OpenSettings();
        }

        private void PrepareCharacterSelection(int playerCount)
        {
            _characterSelections[0] = 0;
            _characterSelections[1] = 1;
            _characterReady[0] = false;
            _characterReady[1] = false;
            _director.BeginRunSetup(playerCount);
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
                if (_characterReady[player]) continue;
                var direction = LocalInputRouter.MenuHorizontalPressed(player, playerCount);
                if (direction != 0)
                    _characterSelections[player] = Wrap(_characterSelections[player] + direction, ContentCatalog.Characters.Length);
                if (LocalInputRouter.MenuSubmitPressed(player, playerCount)) _characterReady[player] = true;
            }
            var allReady = true;
            for (var i = 0; i < playerCount; i++) allReady &= _characterReady[i];
            if (allReady)
            {
                _mapSelection = 0;
                _director.ConfirmCharacters(_characterSelections[0], _characterSelections[1]);
            }
        }

        private void UpdateMapSelect()
        {
            if (LocalInputRouter.MenuBackPressed())
            {
                PrepareCharacterSelection(_director.PendingPlayerCount);
                return;
            }
            var direction = LocalInputRouter.MenuHorizontalPressed(0, _director.PendingPlayerCount);
            if (direction != 0) _mapSelection = Wrap(_mapSelection + direction, ContentCatalog.Maps.Length);
            if (LocalInputRouter.MenuSubmitPressed(0, _director.PendingPlayerCount))
                _director.SelectMapAndStart(_mapSelection);
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

        private void DrawMainMenu()
        {
            DrawPanel(new Rect(0, 0, 1920, 1080), new Color(0.025f, 0.06f, 0.09f, 1f));
            GUI.Label(new Rect(110, 70, 1220, 90), "PROJECT EXPEDITION", _title);
            GUI.Label(new Rect(115, 156, 1000, 45), "THE FROSTBOUND SHORE — PRESENTATION FOUNDATION", _small);

            DrawPanel(new Rect(110, 235, 520, 600), new Color(0.07f, 0.13f, 0.17f, 1f));
            GUI.DrawTexture(new Rect(140, 265, 460, 460), RuntimeAssets.Portrait, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(150, 742, 440, 44), "HALDOR STORMBORN", _center);
            GUI.Label(new Rect(150, 788, 440, 30), "RAVENBOUND VIKING", _small);

            DrawPanel(new Rect(680, 235, 1120, 600), new Color(0.055f, 0.105f, 0.145f, 1f));
            GUI.Label(new Rect(735, 280, 980, 60), "THE RAVEN'S FAVORITE", _heading);
            GUI.Label(new Rect(735, 350, 960, 125),
                "Haldor leads the first tribe into the Convergence. Automatic weapons define the expedition; one strategic Ultimate gives each survivor a decisive moment without turning the game into a shooter.", _body);
            GUI.Label(new Rect(735, 500, 900, 42), "SIGNATURE KIT", _cardTitle);
            GUI.Label(new Rect(755, 555, 900, 150),
                "• Frost Axe — automatic rune-axe throws\n• Raven Guard — automatic shield shockwave\n• Ravenstorm — manual high-impact Ultimate\n• Oath-Bound — extra health and armor", _body);
            GUI.Label(new Rect(735, 712, 950, 35),
                $"Mastery {SaveService.Data.HaldorMastery}   •   Renown {SaveService.Data.TotalRenown}   •   Best {SaveService.Data.BestKills} kills", _small);

            DrawSelectableButton(new Rect(680, 865, 330, 90), "SOLO", 0, ref _mainSelection, PrepareSolo);
            DrawSelectableButton(new Rect(1045, 865, 330, 90), "LOCAL CO-OP", 1, ref _mainSelection, PrepareLocal);
            DrawSelectableButton(new Rect(1410, 865, 330, 90), "SETTINGS", 2, ref _mainSelection, _director.OpenSettings);
            GUI.Label(new Rect(680, 965, 1120, 55),
                $"{Prompt(BindingAction.MoveLeft)} {Prompt(BindingAction.MoveRight)} NAVIGATE   •   {Prompt(BindingAction.Submit)} CONFIRM", _small);
        }

        private void PrepareSolo() => PrepareCharacterSelection(1);
        private void PrepareLocal() => PrepareCharacterSelection(2);

        private void DrawCharacterSelect()
        {
            DrawPanel(new Rect(0, 0, 1920, 1080), new Color(0.025f, 0.06f, 0.09f, 1f));
            GUI.Label(new Rect(420, 55, 1080, 80), "CHOOSE YOUR SURVIVOR", _title);
            GUI.Label(new Rect(460, 135, 1000, 45), "Each local player controls their own selection with their assigned device.", _small);
            var count = _director.PendingPlayerCount;
            for (var player = 0; player < count; player++)
            {
                var width = count == 1 ? 760f : 760f;
                var x = count == 1 ? 580f : 150f + player * 860f;
                var rect = new Rect(x, 195, width, 725);
                var definition = ContentCatalog.Character(_characterSelections[player]);
                DrawPanel(rect, new Color(0.055f, 0.105f, 0.145f, 1f));
                DrawPanel(new Rect(rect.x, rect.y, rect.width, 14), definition.Color);
                if (!_characterReady[player] && GUI.Button(new Rect(rect.x + 35, rect.y + 105, 80, 70), "◀", _button))
                    _characterSelections[player] = Wrap(_characterSelections[player] - 1, ContentCatalog.Characters.Length);
                if (!_characterReady[player] && GUI.Button(new Rect(rect.xMax - 115, rect.y + 105, 80, 70), "▶", _button))
                    _characterSelections[player] = Wrap(_characterSelections[player] + 1, ContentCatalog.Characters.Length);
                var previousColor = GUI.color;
                GUI.color = definition.Color;
                GUI.DrawTexture(new Rect(rect.x + 278, rect.y + 42, 204, 204), RuntimeAssets.Circle.texture, ScaleMode.ScaleToFit, true);
                GUI.color = previousColor;
                GUI.Label(new Rect(rect.x + 40, rect.y + 255, rect.width - 80, 50), definition.Name.ToUpperInvariant(), _heading);
                GUI.Label(new Rect(rect.x + 40, rect.y + 305, rect.width - 80, 34), $"{definition.Tribe} — {definition.Role}", _small);
                GUI.Label(new Rect(rect.x + 55, rect.y + 350, rect.width - 110, 75), definition.Description, _body);
                GUI.Label(new Rect(rect.x + 55, rect.y + 430, rect.width - 110, 36),
                    $"HEALTH {definition.MaxHealth:0}   SPEED {definition.MoveSpeed:0.0}   ARMOR {definition.Armor:0.0}", _small);
                var ultimateRect = new Rect(rect.x + 40, rect.y + 475, rect.width - 80, 110);
                DrawPanel(ultimateRect, new Color(0.025f, 0.065f, 0.087f, 1f));
                DrawPanel(new Rect(ultimateRect.x, ultimateRect.y, 5, ultimateRect.height), definition.Color);
                GUI.Label(new Rect(ultimateRect.x + 18, ultimateRect.y + 8, ultimateRect.width - 36, ultimateRect.height - 16),
                    $"ULTIMATE — {definition.UltimateName}: {definition.UltimateDescription}", _body);
                GUI.Label(new Rect(rect.x + 35, rect.y + 592, rect.width - 70, 42), _characterReady[player]
                    ? $"P{player + 1} READY"
                    : $"P{player + 1}  •  ◀ ▶ CHOOSE  •  {Prompt(BindingAction.Submit)} CONFIRM  •  {LocalInputRouter.AssignmentLabel(player, count)}", _micro);
                if (!_characterReady[player] && GUI.Button(new Rect(rect.x + 220, rect.y + 646, 320, 58), "READY", _button))
                    _characterReady[player] = true;
            }
            GUI.Label(new Rect(610, 955, 700, 40), $"{Prompt(BindingAction.Back)} — BACK", _small);
        }

        private void DrawMapSelect()
        {
            DrawPanel(new Rect(0, 0, 1920, 1080), new Color(0.025f, 0.06f, 0.09f, 1f));
            GUI.Label(new Rect(430, 60, 1060, 80), "CHOOSE THE EXPEDITION", _title);
            GUI.Label(new Rect(480, 140, 960, 40), "Map time controls phases, pressure, events and the final boss.", _small);
            for (var i = 0; i < ContentCatalog.Maps.Length; i++)
            {
                var map = ContentCatalog.Maps[i];
                var rect = new Rect(210 + i * 770, 260, 700, 520);
                DrawPanel(rect, i == _mapSelection ? new Color(0.09f, 0.18f, 0.22f, 1f) : new Color(0.045f, 0.085f, 0.115f, 1f));
                if (i == _mapSelection) DrawBorder(rect, new Color(0.93f, 0.7f, 0.24f), 7f);
                DrawPanel(new Rect(rect.x + 35, rect.y + 35, rect.width - 70, 145), map.GroundColor);
                GUI.Label(new Rect(rect.x + 40, rect.y + 190, rect.width - 80, 78), map.Name.ToUpperInvariant(), _mapTitle);
                GUI.Label(new Rect(rect.x + 50, rect.y + 268, rect.width - 100, 34), $"{map.Region}   •   {map.DurationLabel}", _small);
                GUI.Label(new Rect(rect.x + 55, rect.y + 315, rect.width - 110, 105), map.Description, _body);
                GUI.Label(new Rect(rect.x + 55, rect.y + 425, rect.width - 110, 35), $"JOTUNN ARRIVAL  {FormatTime(map.BossSpawnTime)}", _small);
                if (GUI.Button(new Rect(rect.x + 190, rect.y + 465, 320, 48), i == _mapSelection ? "BEGIN EXPEDITION" : "SELECT", _button))
                {
                    if (_mapSelection == i) _director.SelectMapAndStart(i);
                    else _mapSelection = i;
                }
            }
            GUI.Label(new Rect(590, 860, 740, 55), $"P1: ◀ ▶ SELECT   •   {Prompt(BindingAction.Submit)} CONFIRM", _center);
            GUI.Label(new Rect(610, 930, 700, 40), $"{Prompt(BindingAction.Back)} — BACK", _small);
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
            if (_director.ShowPerformanceMetrics) DrawPerformancePanel();
        }

        private void DrawLevelUp()
        {
            DrawPanel(new Rect(0, 0, 1920, 1080), new Color(0.008f, 0.022f, 0.032f, 1f));
            GUI.Label(new Rect(500, 85, 920, 75), "CHOOSE THE NEXT VERSE", _title);
            var owner = _director.Players[Mathf.Clamp(_director.RewardTurnPlayerIndex, 0, _director.Players.Count - 1)];
            GUI.Label(new Rect(460, 165, 1000, 44), $"{owner.HeroName.ToUpperInvariant()} CHOOSES — ONLY P{owner.PlayerIndex + 1}'S DEVICE IS ACTIVE", _center);
            for (var i = 0; i < _director.CurrentRewards.Count; i++)
            {
                var option = _director.CurrentRewards[i];
                var rect = new Rect(90 + i * 435, 255, 400, 485);
                var hovered = rect.Contains(Event.current.mousePosition);
                if (hovered) _levelSelection = i;
                DrawPanel(rect, hovered ? new Color(0.075f, 0.145f, 0.19f, 1f) : new Color(0.045f, 0.095f, 0.13f, 1f));
                DrawPanel(new Rect(rect.x, rect.y, rect.width, 14), option.Item.Color);
                if (i == _levelSelection) DrawBorder(rect, new Color(0.96f, 0.72f, 0.22f), 7f);
                var targetLabel = RewardTargetLabel(option);
                var targetColor = option.Shared ? new Color(0.72f, 0.48f, 0.92f) : _director.Players[option.TargetPlayerIndex].Definition.Color;
                DrawRewardTargetIcons(option, new Rect(rect.x + 185, rect.y + 26, 44, 44));
                DrawPanel(new Rect(rect.x + 234, rect.y + 28, 141, 40), targetColor);
                GUI.Label(new Rect(rect.x + 234, rect.y + 28, 141, 40), targetLabel, _badge);
                GUI.Label(new Rect(rect.x + 25, rect.y + 82, rect.width - 50, 65), $"{i + 1}. {option.Item.Name}", _heading);
                var build = _director.Players[option.TargetPlayerIndex].Build;
                var nextLabel = option.Shared ? "TEAM UPGRADE" : build.NextLabel(option.Item);
                GUI.Label(new Rect(rect.x + 30, rect.y + 150, rect.width - 60, 38), nextLabel, _cardTitle);
                GUI.Label(new Rect(rect.x + 28, rect.y + 198, rect.width - 56, 78),
                    option.Item.Description, _rewardDescription);
                GUI.Label(new Rect(rect.x + 28, rect.y + 278, rect.width - 56, 72),
                    RewardEffectPreview(option), _rewardEffect);
                GUI.Label(new Rect(rect.x + 28, rect.y + 350, rect.width - 56, 28), EvolutionHint(option.Item), _small);
                if (GUI.Button(new Rect(rect.x + 28, rect.y + 385, rect.width - 56, 64), "CHOOSE REWARD", _button))
                    _director.ChooseReward(i);
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) _director.ChooseReward(i);
            }
            GUI.Label(new Rect(420, 790, 1080, 55),
                $"CLICK A CARD   •   {Prompt(BindingAction.MoveLeft)} CHOOSE   •   {Prompt(BindingAction.Submit)} CONFIRM   •   1–4 / FACE BUTTONS", _small);
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
            GUI.Label(new Rect(480, 30, 960, 75), "EXPEDITION BUILD", _title);
            GUI.Label(new Rect(460, 101, 1000, 38), "LIVE STATISTICS AND EVERY ITEM CURRENTLY SHAPING THIS RUN", _small);
            for (var playerIndex = 0; playerIndex < _director.Players.Count; playerIndex++)
            {
                var player = _director.Players[playerIndex];
                var width = _director.Players.Count == 1 ? 1040f : 850f;
                var x = _director.Players.Count == 1 ? 440f : 75f + playerIndex * 885f;
                var rect = new Rect(x, 155, width, 810);
                DrawPanel(rect, new Color(0.035f, 0.08f, 0.108f, 1f));
                DrawPanel(new Rect(rect.x, rect.y, rect.width, 14), player.Definition.Color);
                GUI.Label(new Rect(rect.x + 35, rect.y + 31, rect.width - 70, 55), $"P{playerIndex + 1} — {player.HeroName.ToUpperInvariant()}", _heading);
                var statGap = 10f;
                var statWidth = (rect.width - 60f - statGap * 2f) / 3f;
                var survivorStats = new Rect(rect.x + 30, rect.y + 95, statWidth, 310);
                var axeStats = new Rect(survivorStats.xMax + statGap, rect.y + 95, statWidth, 310);
                var guardStats = new Rect(axeStats.xMax + statGap, rect.y + 95, statWidth, 310);
                DrawStatColumn(survivorStats, "SURVIVOR",
                    new[] { "HEALTH", "ARMOR", "MOVE SPEED", "XP MAGNET", "STATUS", "ULTIMATE", "ULT DAMAGE", "ULT RADIUS", "ULT INTERVAL", "ULT CHARGE" },
                    new[] { $"{player.Health:0} / {player.MaxHealth:0}", $"{player.Armor:0.0}", $"{player.MoveSpeed:0.00}", $"{player.MagnetRadius:0.00}", player.IsDowned ? "DOWNED" : "ACTIVE", player.UltimateName, $"{player.UltimateDamage:0.0}", $"{player.UltimateRadius:0.00}", $"{player.UltimateCooldown:0.00}s", player.UltimateReady ? "READY" : $"{player.UltimateRemaining:0.0}s" },
                    player.Definition.Color);
                DrawStatColumn(axeStats, "FROST AXE",
                    new[] { "DAMAGE", "INTERVAL", "RATE", "PROJECTILES", "PIERCE", "CRITICAL", "CRIT DAMAGE", "PROJ SPEED", "LIFETIME", "HIT RADIUS", "KNOCKBACK", "EVOLUTION" },
                    new[] { $"{player.Weapons.AxeDamage:0.0}", $"{player.Weapons.AxeCooldown:0.000}s", $"{1f / Mathf.Max(0.01f, player.Weapons.AxeCooldown):0.00}/s", $"{player.Weapons.AxeCount}", $"{player.Weapons.AxePierce}", $"{player.Weapons.CriticalChance * 100f:0}%", $"x{player.Weapons.CriticalDamageMultiplier:0.0}", $"{player.Weapons.AxeProjectileSpeed:0.0}", $"{player.Weapons.AxeProjectileDuration:0.0}s", $"{player.Weapons.AxeHitRadius:0.00}/{player.Weapons.CriticalAxeHitRadius:0.00}", $"{player.Weapons.AxeKnockback:0.00}/{player.Weapons.CriticalAxeKnockback:0.00}", player.Weapons.FrostAxeEvolved ? $"{player.Weapons.CleaverExplosionDamageMultiplier * 100f:0}% @ {player.Weapons.CleaverExplosionRadius:0.00}" : "NONE" },
                    new Color(0.54f, 0.72f, 0.9f));
                DrawStatColumn(guardStats, "RAVEN GUARD",
                    new[] { "DAMAGE", "INTERVAL", "RATE", "RADIUS", "KNOCKBACK", "HEAL / HIT", "HEAL CAP", "EVOLUTION" },
                    new[] { $"{player.Weapons.ShieldDamage:0.0}", $"{player.Weapons.ShieldCooldown:0.000}s", $"{1f / Mathf.Max(0.01f, player.Weapons.ShieldCooldown):0.00}/s", $"{player.Weapons.ShieldRadius:0.00}", $"{player.Weapons.ShieldKnockback:0.00}", $"{player.Weapons.ShieldHealingPerHit:0.00}", $"{player.Weapons.ShieldHealingCap:0.0}", player.Weapons.RavenGuardEvolved ? "STORM AEGIS" : "NONE" },
                    new Color(0.45f, 0.9f, 0.72f));

                GUI.Label(new Rect(rect.x + 35, rect.y + 418, rect.width - 340, 38), "ITEMS AND LEVEL EFFECTS", _cardTitle);
                GUI.Label(new Rect(rect.xMax - 310, rect.y + 422, 270, 30),
                    $"WEAPONS {player.Build.CountCategory(ItemCategory.Weapon)}/{player.Build.WeaponSlots}   •   GEAR {player.Build.CountCategory(ItemCategory.Gear)}/{player.Build.GearSlots}", _micro);
                var items = player.Build.Items;
                var visibleIndex = 0;
                for (var i = 0; i < items.Count && visibleIndex < 12; i++)
                {
                    var state = items[i];
                    var item = ItemCatalog.Find(state.ItemId);
                    if (item == null || item.Category == ItemCategory.Boon) continue;
                    var column = visibleIndex % 3;
                    var row = visibleIndex / 3;
                    var gap = 12f;
                    var cardWidth = (rect.width - 84f - gap * 2f) / 3f;
                    var itemRect = new Rect(rect.x + 30 + column * (cardWidth + gap), rect.y + 462 + row * 78, cardWidth, 74);
                    DrawPanel(itemRect, new Color(0.018f, 0.05f, 0.07f, 1f));
                    DrawBorder(itemRect, state.IsEvolved ? new Color(0.98f, 0.68f, 0.22f) : item.Color, 3f);
                    var evolved = state.IsEvolved ? ItemCatalog.Find(state.EvolutionId) : null;
                    GUI.Label(new Rect(itemRect.x + 10, itemRect.y + 3, itemRect.width - 20, 28),
                        evolved != null ? $"{evolved.Name} — EVOLVED" : $"{item.Name} — LEVEL {state.Level}/{item.MaxLevel}", _itemTitle);
                    GUI.Label(new Rect(itemRect.x + 10, itemRect.y + 31, itemRect.width - 20, 40),
                        ItemProgressPreview(state, item, evolved), _itemProgress);
                    visibleIndex++;
                }
            }
            GUI.Label(new Rect(560, 990, 800, 42),
                $"{Prompt(BindingAction.BuildDetails)} — RETURN TO EXPEDITION", _center);
        }

        private string RewardTargetLabel(RewardOption option)
        {
            if (option.Shared) return "P1 + P2";
            return $"P{option.TargetPlayerIndex + 1}  {_director.Players[option.TargetPlayerIndex].HeroName.Split(' ')[0].ToUpperInvariant()}";
        }

        private static string ItemProgressPreview(ItemState state, ItemDefinition item,
            ItemDefinition evolved)
        {
            if (evolved != null) return evolved.Description;
            var current = item.EffectDescriptionAtLevel(state.Level);
            if (state.Level >= item.MaxLevel) return $"MAX • {current}";
            return $"CURRENT: {current}\nNEXT L{state.Level + 1}: {item.EffectDescriptionAtLevel(state.Level + 1)}";
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

        private static string EvolutionHint(ItemDefinition item)
        {
            if (!item.IsEvolution) return item.Category.ToString().ToUpperInvariant();
            var baseItem = ItemCatalog.Find(item.EvolutionOf);
            var catalyst = ItemCatalog.Find(item.CatalystId);
            return $"{baseItem.Name} MAX + {catalyst.Name}";
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
            var panel = new Rect(485, 170, 950, 720);
            DrawPanel(panel, new Color(0.055f, 0.105f, 0.14f, 1f));
            DrawPanel(new Rect(panel.x, panel.y, panel.width, 7), new Color(0.32f, 0.68f, 0.78f));
            GUI.Label(new Rect(545, 205, 830, 118), victory ? "A SAGA IS BORN" : "THE ICE CLAIMS THE EXPEDITION", _resultTitle);

            var summary = new Rect(555, 335, 810, 205);
            DrawPanel(summary, new Color(0.018f, 0.048f, 0.068f, 1f));
            GUI.Label(new Rect(summary.x + 25, summary.y + 14, summary.width - 50, 25), "EXPEDITION", _micro);
            GUI.Label(new Rect(summary.x + 30, summary.y + 42, summary.width - 60, 58), _director.SelectedMap.Name.ToUpperInvariant(), _center);
            DrawPanel(new Rect(summary.x + 30, summary.y + 105, summary.width - 60, 2), new Color(0.25f, 0.48f, 0.57f));
            GUI.Label(new Rect(summary.x + 30, summary.y + 112, summary.width - 60, 80),
                $"TIME  {FormatTime(_director.Elapsed)}     •     ENEMIES  {_director.Kills}     •     RENOWN  {_director.RunRenown}\nSEED  {_director.RunSeed}", _small);
            GUI.Label(new Rect(600, 565, 720, 80), victory
                ? "The Jotunn falls. The tribe carries a new verse back to camp."
                : "No expedition is wasted. The camp remembers what its survivors learned.", _center);
            DrawSelection(new Rect(545, 700, 410, 100), _resultSelection == 0);
            if (GUI.Button(new Rect(555, 710, 390, 80), "REPLAY SAME SEED", _button)) _director.ReplayRun();
            DrawSelection(new Rect(965, 700, 410, 100), _resultSelection == 1);
            if (GUI.Button(new Rect(975, 710, 390, 80), "RETURN TO CAMP", _button)) _director.ReturnToMenu();
        }

        private void DrawStatColumn(Rect rect, string title, string[] labels, string[] values, Color accent)
        {
            DrawPanel(rect, new Color(0.018f, 0.048f, 0.068f, 1f));
            DrawPanel(new Rect(rect.x, rect.y, rect.width, 4), accent);
            GUI.Label(new Rect(rect.x + 14, rect.y + 7, rect.width - 28, 28), title, _statSection);
            var rowY = rect.y + 38f;
            const float rowHeight = 22f;
            var count = Mathf.Min(labels.Length, values.Length);
            for (var i = 0; i < count; i++)
            {
                if ((i & 1) == 1)
                    DrawPanel(new Rect(rect.x + 10, rowY, rect.width - 20, rowHeight), new Color(0.04f, 0.08f, 0.1f, 0.75f));
                GUI.Label(new Rect(rect.x + 14, rowY, rect.width * 0.54f - 14, rowHeight), labels[i], _statLabel);
                GUI.Label(new Rect(rect.x + rect.width * 0.54f, rowY, rect.width * 0.46f - 14, rowHeight), values[i], _statValue);
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
            _badge = MakeStyle(14, FontStyle.Bold, new Color(0.025f, 0.065f, 0.085f), TextAnchor.MiddleCenter);
            _mapTitle = MakeStyle(25, FontStyle.Bold, PresentationTheme.TextPrimary, TextAnchor.MiddleCenter);
            _mapTitle.wordWrap = true;
            _resultTitle = MakeStyle(39, FontStyle.Bold, PresentationTheme.TextPrimary, TextAnchor.MiddleCenter);
            _resultTitle.wordWrap = true;
            _statSection = MakeStyle(14, FontStyle.Bold, PresentationTheme.Accent, TextAnchor.MiddleLeft);
            _statLabel = MakeStyle(14, FontStyle.Bold, PresentationTheme.TextSecondary, TextAnchor.MiddleLeft);
            _statValue = MakeStyle(16, FontStyle.Bold, PresentationTheme.TextPrimary, TextAnchor.MiddleRight);
            _rewardEffect = MakeStyle(16, FontStyle.Bold, PresentationTheme.Accent, TextAnchor.UpperLeft);
            _rewardEffect.wordWrap = true;
            _rewardDescription = MakeStyle(18, FontStyle.Normal, PresentationTheme.TextPrimary, TextAnchor.UpperLeft);
            _rewardDescription.wordWrap = true;
            _itemProgress = MakeStyle(11, FontStyle.Bold, PresentationTheme.TextSecondary, TextAnchor.UpperLeft);
            _itemProgress.wordWrap = true;
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
