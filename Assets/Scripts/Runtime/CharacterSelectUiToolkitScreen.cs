using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectExpedition
{
    public sealed class CharacterSelectUiToolkitScreen : MonoBehaviour
    {
        public static bool EnabledByDefault { get; private set; }

        private UIDocument _document;
        private VisualElement _root;
        private Label _renownLabel;
        private Label _detailName;
        private Label _detailRole;
        private Label _detailDescription;
        private Label _detailUltimate;
        private Label _detailHint;
        private Button _confirmButton;
        private Button _filterToggle;
        private Label _filterHint;
        private ScrollView _statsScroll;
        private ScrollView _gridScroll;
        private ScrollView _expeditionsScroll;
        private GameDirector _director;
        private bool _unlockedFilter;
        private Action _onBack;
        private Action _onConfirm;
        private Action<bool> _onFilterChanged;

        public bool IsReady => _root != null;

        public void Initialize(
            GameDirector director,
            Action onBack,
            Action onConfirm,
            Action<bool> onFilterChanged)
        {
            _director = director;
            _onBack = onBack;
            _onConfirm = onConfirm;
            _onFilterChanged = onFilterChanged;

            if (_document == null)
            {
                _document = gameObject.AddComponent<UIDocument>();
            }

            var tree = Resources.Load<VisualTreeAsset>("UI/CharacterSelect/CharacterSelectScreen");
            if (tree == null)
            {
                Debug.LogWarning("Character Select UI Toolkit tree was not found in Resources.");
                enabled = false;
                return;
            }

            _document.visualTreeAsset = tree;
            _root = _document.rootVisualElement.Q<VisualElement>("root");
            if (_root == null)
            {
                Debug.LogWarning("Character Select UI Toolkit root element was not found.");
                enabled = false;
                return;
            }

            CacheElements();
            WireEvents();
            enabled = EnabledByDefault;
        }

        public void SetActive(bool active)
        {
            enabled = active;

            if (_root == null)
            {
                return;
            }

            _root.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Sync(
            int selectedIndex,
            bool unlocked,
            bool hasSelected,
            bool isReady,
            bool unlockedFilter,
            string actionHint)
        {
            if (!IsReady || _director == null)
            {
                return;
            }

            _unlockedFilter = unlockedFilter;
            var definition = ContentCatalog.Character(selectedIndex);
            _renownLabel.text = $"RENOWN  {SaveService.AvailableRenown()}";
            _detailName.text = definition.Name.ToUpperInvariant();
            _detailRole.text = $"{definition.Tribe} — {definition.Role}";
            _detailDescription.text = unlocked ? definition.Description : definition.LockedPreviewLine;
            _detailUltimate.text = unlocked
                ? $"ULTIMATE — {definition.UltimateName}: {definition.UltimateDescription}"
                : "ULTIMATE HIDDEN UNTIL UNLOCK";
            _detailHint.text = actionHint;
            _filterToggle.text = unlockedFilter ? "FILTER: ON" : "FILTER: OFF";
            _filterHint.text = unlockedFilter
                ? "Showing unlocked survivors only."
                : "Showing the full roster.";
            _confirmButton.text = !unlocked ? "LOCKED" : hasSelected ? "CONFIRM" : "SELECT";
            _confirmButton.SetEnabled(unlocked && (!hasSelected || !isReady));

            RebuildStats(definition, unlocked);
            RebuildGrid(selectedIndex, unlockedFilter);
            RebuildExpeditions();
        }

        private void CacheElements()
        {
            _renownLabel = _root.Q<Label>("renown-label");
            _detailName = _root.Q<Label>("detail-name");
            _detailRole = _root.Q<Label>("detail-role");
            _detailDescription = _root.Q<Label>("detail-description");
            _detailUltimate = _root.Q<Label>("detail-ultimate");
            _detailHint = _root.Q<Label>("detail-hint");
            _confirmButton = _root.Q<Button>("confirm-button");
            _filterToggle = _root.Q<Button>("filter-toggle");
            _filterHint = _root.Q<Label>("filter-hint");
            _statsScroll = _root.Q<ScrollView>("stats-scroll");
            _gridScroll = _root.Q<ScrollView>("grid-scroll");
            _expeditionsScroll = _root.Q<ScrollView>("expeditions-scroll");
        }

        private void WireEvents()
        {
            _root.Q<Button>("back-button")?.RegisterCallback<ClickEvent>(_ => _onBack?.Invoke());
            _confirmButton?.RegisterCallback<ClickEvent>(_ => _onConfirm?.Invoke());
            _filterToggle?.RegisterCallback<ClickEvent>(_ =>
            {
                _unlockedFilter = !_unlockedFilter;
                _onFilterChanged?.Invoke(_unlockedFilter);
            });
        }

        private void RebuildStats(CharacterDefinition definition, bool unlocked)
        {
            if (_statsScroll == null)
            {
                return;
            }

            _statsScroll.Clear();
            if (!unlocked)
            {
                _statsScroll.Add(new Label("Unlock this survivor in the CODEX to reveal full stats."));
                return;
            }

            AddStatRow("MAX HEALTH", $"{definition.MaxHealth:0}");
            AddStatRow("MOVE SPEED", $"{definition.MoveSpeed:0.0}");
            AddStatRow("ARMOR", $"{definition.Armor:0.0}");
            AddStatRow("ULTIMATE CD", $"{definition.UltimateCooldown:0}s");
            AddStatRow("ULT DMG", $"{definition.UltimateDamage:0}");
            AddStatRow("ULT RADIUS", $"{definition.UltimateRadius:0.0}");
        }

        private void AddStatRow(string label, string value)
        {
            var row = new VisualElement();
            row.AddToClassList("character-select-stat-row");
            var labelElement = new Label(label);
            labelElement.AddToClassList("character-select-stat-label");
            var valueElement = new Label(value);
            valueElement.AddToClassList("character-select-stat-value");
            row.Add(labelElement);
            row.Add(valueElement);
            _statsScroll.Add(row);
        }

        private void RebuildGrid(int selectedIndex, bool unlockedFilter)
        {
            if (_gridScroll == null)
            {
                return;
            }

            _gridScroll.Clear();
            var row = new VisualElement();
            row.AddToClassList("character-select-grid-row");
            _gridScroll.Add(row);

            for (var index = 0; index < ContentCatalog.Characters.Length; index++)
            {
                if (unlockedFilter && !SaveService.IsCharacterUnlocked(index))
                {
                    continue;
                }

                var definition = ContentCatalog.Character(index);
                var tile = new VisualElement();
                tile.AddToClassList("character-select-grid-tile");
                if (index == selectedIndex)
                {
                    tile.AddToClassList("character-select-grid-tile-selected");
                }

                var name = new Label(definition.Name.ToUpperInvariant());
                name.AddToClassList("character-select-grid-tile-name");
                tile.Add(name);
                row.Add(tile);
            }
        }

        private void RebuildExpeditions()
        {
            if (_expeditionsScroll == null)
            {
                return;
            }

            _expeditionsScroll.Clear();
            for (var mapIndex = 0; mapIndex < ContentCatalog.Maps.Length; mapIndex++)
            {
                var map = ContentCatalog.Map(mapIndex);
                var unlocked = SaveService.IsMapUnlocked(mapIndex);
                var marker = unlocked ? "[x]" : "[ ]";
                var row = new Label($"{marker} {map.Name.ToUpperInvariant()}");
                row.AddToClassList("character-select-expedition-row");
                if (!unlocked)
                {
                    row.style.color = new Color(0.42f, 0.48f, 0.52f);
                }

                _expeditionsScroll.Add(row);
            }
        }
    }
}
