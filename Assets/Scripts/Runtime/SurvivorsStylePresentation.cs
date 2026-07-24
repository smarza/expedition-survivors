using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public enum SurvivorsButtonKind
    {
        Gold,
        Green,
        Red,
        Blue
    }

    public static class SurvivorsStylePresentation
    {
        private static Font _uiFont;
        private static GUIStyle _sectionTitleStyle;
        private static GUIStyle _tileNameStyle;
        private static GUIStyle _coinStyle;
        private static GUIStyle _lockBadgeStyle;
        private static GUIStyle _lastRunBadgeStyle;
        private static GUIStyle _transparentButtonStyle;
        private static int _styleRevision = -1;
        private static readonly Dictionary<int, Texture2D> SolidTextures = new Dictionary<int, Texture2D>();

        public static readonly Color BackgroundDeep = new Color(0.04f, 0.08f, 0.12f, 1f);
        public static readonly Color BackgroundMid = new Color(0.06f, 0.11f, 0.16f, 1f);
        public static readonly Color PanelNavy = new Color(0.05f, 0.1f, 0.18f, 0.98f);
        public static readonly Color PanelNavyInset = new Color(0.04f, 0.08f, 0.14f, 0.98f);
        public static readonly Color TileBackground = new Color(0.03f, 0.06f, 0.11f, 1f);
        public static readonly Color TileSelected = new Color(0.07f, 0.12f, 0.2f, 1f);
        public static readonly Color BorderGold = new Color(0.86f, 0.66f, 0.18f, 1f);
        public static readonly Color BorderGoldBright = new Color(0.98f, 0.82f, 0.28f, 1f);
        public static readonly Color BorderGoldDim = new Color(0.35f, 0.42f, 0.48f, 0.85f);
        public static readonly Color TextLight = new Color(0.92f, 0.94f, 0.96f, 1f);
        public static readonly Color TextGold = new Color(0.98f, 0.82f, 0.28f, 1f);
        public static readonly Color TextMuted = new Color(0.62f, 0.7f, 0.76f, 1f);
        public static readonly Color StatPositive = new Color(0.42f, 0.92f, 0.48f, 1f);
        public static readonly Color RowStripe = new Color(0.02f, 0.05f, 0.09f, 0.55f);

        public static void EnsureStyles()
        {
            if (_styleRevision == PresentationPreferences.Revision)
            {
                return;
            }

            _styleRevision = PresentationPreferences.Revision;
            var font = ResolveUiFont();

            _sectionTitleStyle = CreateStyle(font, 13, FontStyle.Bold, TextGold, TextAnchor.MiddleCenter);
            _tileNameStyle = CreateStyle(font, 12, FontStyle.Bold, TextLight, TextAnchor.MiddleCenter);
            _tileNameStyle.wordWrap = true;
            _tileNameStyle.clipping = TextClipping.Overflow;
            _coinStyle = CreateStyle(font, 14, FontStyle.Bold, TextGold, TextAnchor.MiddleLeft);
            _lockBadgeStyle = CreateStyle(font, 9, FontStyle.Bold, TextMuted, TextAnchor.MiddleCenter);
            _lockBadgeStyle.wordWrap = false;
            _lastRunBadgeStyle = CreateStyle(font, 9, FontStyle.Bold, TextGold, TextAnchor.MiddleCenter);
            _lastRunBadgeStyle.wordWrap = false;
            _transparentButtonStyle = new GUIStyle(GUI.skin.button)
            {
                font = font,
                fontSize = PresentationTheme.FontSize(15),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 6, 6),
                border = new RectOffset(0, 0, 0, 0)
            };
            SetTextColor(_transparentButtonStyle, new Color(0.04f, 0.08f, 0.1f));
            ApplySolidBackground(_transparentButtonStyle.normal);
            ApplySolidBackground(_transparentButtonStyle.hover);
            ApplySolidBackground(_transparentButtonStyle.active);
            ApplySolidBackground(_transparentButtonStyle.focused);
            ApplySolidBackground(_transparentButtonStyle.onNormal);
            ApplySolidBackground(_transparentButtonStyle.onHover);
            ApplySolidBackground(_transparentButtonStyle.onActive);
            ApplySolidBackground(_transparentButtonStyle.onFocused);
        }

        public static GUIStyle SectionTitleStyle
        {
            get
            {
                EnsureStyles();
                return _sectionTitleStyle;
            }
        }

        public static GUIStyle TileNameStyle
        {
            get
            {
                EnsureStyles();
                return _tileNameStyle;
            }
        }

        public static GUIStyle CoinStyle
        {
            get
            {
                EnsureStyles();
                return _coinStyle;
            }
        }

        public static GUIStyle CreateLabelStyle(int size, FontStyle fontStyle, Color color, TextAnchor anchor)
        {
            EnsureStyles();
            return CreateStyle(ResolveUiFont(), size, fontStyle, color, anchor);
        }

        public static GUIStyle CreateBodyStyle(TextAnchor anchor) =>
            CreateLabelStyle(15, FontStyle.Normal, TextLight, anchor);

        public static void DrawScreenBackground(Rect screen)
        {
            DrawPanel(screen, BackgroundDeep);

            var vignetteSteps = 3;
            for (var i = 0; i < vignetteSteps; i++)
            {
                var alpha = 0.06f + i * 0.04f;
                var inset = i * 36f;
                var vignetteRect = new Rect(
                    screen.x + inset,
                    screen.y + inset,
                    screen.width - inset * 2f,
                    screen.height - inset * 2f);
                DrawBorder(vignetteRect, new Color(0f, 0f, 0f, alpha), 1f);
            }

            DrawPanel(new Rect(screen.x, screen.y, screen.width, screen.height),
                new Color(BackgroundMid.r, BackgroundMid.g, BackgroundMid.b, 0.35f));
        }

        public static void DrawFlatPanel(Rect rect, Color fill, float borderThickness = 1f, Color borderColor = default)
        {
            DrawPanel(rect, fill);

            if (borderThickness <= 0f)
            {
                return;
            }

            var border = borderColor == default ? BorderGoldDim : borderColor;
            DrawBorder(rect, border, borderThickness);
        }

        public static void DrawOrnatePanel(Rect rect, Color fill, bool emphasized = false)
        {
            var thickness = emphasized ? 2f : 1f;
            var border = emphasized ? BorderGold : BorderGoldDim;
            DrawFlatPanel(rect, fill, thickness, border);
        }

        public static void DrawInsetPanel(Rect rect, Color accentColor = default)
        {
            DrawPanel(rect, PanelNavyInset);

            if (accentColor == default)
            {
                return;
            }

            DrawPanel(new Rect(rect.x, rect.y, rect.width, 3f), accentColor);
        }

        public static void DrawSectionHeader(Rect rect, string title)
        {
            EnsureStyles();
            DrawPanel(rect, new Color(0.03f, 0.06f, 0.1f, 0.88f));
            GUI.Label(rect, title.ToUpperInvariant(), _sectionTitleStyle);
        }

        public static void DrawAlternatingRowBackground(Rect rowRect, int rowIndex)
        {
            if (rowIndex % 2 != 0)
            {
                DrawPanel(rowRect, RowStripe);
            }
        }

        public static void DrawCoinBadge(Rect rect, string label)
        {
            EnsureStyles();
            DrawFlatPanel(rect, PanelNavy, 1f);
            DrawCoinIcon(new Rect(rect.x + 10f, rect.y + rect.height * 0.5f - 12f, 24f, 24f));
            GUI.Label(new Rect(rect.x + 40f, rect.y + 8f, rect.width - 48f, rect.height - 16f), label, _coinStyle);
        }

        public static void DrawCoinIcon(Rect rect)
        {
            DrawPanel(rect, new Color(0.52f, 0.38f, 0.08f, 1f));
            DrawBorder(rect, BorderGoldDim, 1f);
            var center = rect.center;
            DrawDiamond(center, Vector2.one * 0.18f, BorderGoldBright);
        }

        public static void DrawLockBadge(Rect tileRect)
        {
            EnsureStyles();
            var badgeRect = new Rect(tileRect.xMax - 58f, tileRect.y + 5f, 52f, 16f);
            DrawPanel(badgeRect, new Color(0.1f, 0.12f, 0.16f, 0.94f));
            DrawBorder(badgeRect, BorderGoldDim, 1f);
            GUI.Label(badgeRect, "LOCKED", _lockBadgeStyle);
        }

        public static void DrawLastRunBadge(Rect badgeRect)
        {
            EnsureStyles();
            DrawPanel(badgeRect, new Color(0.42f, 0.28f, 0.06f, 0.94f));
            DrawBorder(badgeRect, BorderGoldDim, 1f);
            GUI.Label(badgeRect, "LAST RUN", _lastRunBadgeStyle);
        }

        public static void DrawCharacterTileFrame(Rect tileRect, bool isSelected, Color accentColor, bool unlocked)
        {
            var fill = isSelected ? TileSelected : TileBackground;
            var borderColor = isSelected ? BorderGold : BorderGoldDim;
            var borderThickness = isSelected ? 2f : 1f;
            DrawFlatPanel(tileRect, fill, borderThickness, borderColor);
            DrawPanel(new Rect(tileRect.x + 2f, tileRect.y + 2f, tileRect.width - 4f, 3f),
                unlocked ? accentColor : new Color(0.25f, 0.28f, 0.32f));

            if (isSelected)
            {
                DrawSelectionStar(new Rect(tileRect.x + 6f, tileRect.y + 6f, 16f, 16f));
            }
        }

        public static void DrawSelectionStar(Rect rect)
        {
            var center = rect.center;
            var normalizedSize = rect.width / 128f;
            DrawDiamond(center, Vector2.one * normalizedSize, BorderGoldBright, 0f);
            DrawDiamond(center, Vector2.one * (normalizedSize * 0.55f), new Color(0.98f, 0.92f, 0.55f, 1f), 45f);
        }

        public static void DrawWeaponFrame(Rect rect, string weaponId, Color heroTint)
        {
            DrawFlatPanel(rect, new Color(0.02f, 0.05f, 0.09f, 1f), 1f);
            ItemPresentation.DrawItemIcon(
                InsetRect(rect, 4f),
                weaponId,
                ItemIconSize.Medium,
                false,
                heroTint);
        }

        public static bool DrawButton(Rect rect, string label, SurvivorsButtonKind kind)
        {
            EnsureStyles();
            var hovered = rect.Contains(Event.current.mousePosition);
            var fill = ResolveButtonColor(kind, hovered);
            var border = new Color(fill.r * 0.55f, fill.g * 0.55f, fill.b * 0.55f, 1f);
            DrawPanel(rect, fill);
            DrawBorder(rect, border, 2f);
            GUI.Label(rect, label, _transparentButtonStyle);

            return DrawTouchable(rect);
        }

        public static bool DrawTouchable(Rect rect, System.Action onPress = null)
        {
            var pressed = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            var tapped = TouchInputRouter.RectTapped(rect);

            if ((pressed || tapped) && onPress != null)
            {
                onPress();
            }

            return pressed || tapped;
        }

        public static bool DrawVsFooterButton(Rect rect, string label, SurvivorsButtonKind kind, bool selected = false)
        {
            if (selected)
            {
                DrawBorder(new Rect(rect.x - 4f, rect.y - 4f, rect.width + 8f, rect.height + 8f),
                    BorderGoldBright, 2f);
            }

            return DrawButton(rect, label, kind);
        }

        public static SurvivorsHudStyles CreateHudStyles() => SurvivorsHudStyles.Create();

        public static void DrawVerticalGradient(Rect rect, Color top, Color bottom)
        {
            var steps = Mathf.Max(4, Mathf.CeilToInt(rect.height / 8f));
            var stepHeight = rect.height / steps;

            for (var step = 0; step < steps; step++)
            {
                var t = step / (float)Mathf.Max(1, steps - 1);
                var color = Color.Lerp(top, bottom, t);
                DrawPanel(new Rect(rect.x, rect.y + step * stepHeight, rect.width, stepHeight + 1f), color);
            }
        }

        public static void DrawShadowedLabel(Rect rect, string text, GUIStyle style, Color shadowColor, float shadowOffsetPixels = 2f)
        {
            var shadowStyle = new GUIStyle(style);
            SetTextColor(shadowStyle, shadowColor);

            GUI.Label(
                new Rect(rect.x + shadowOffsetPixels, rect.y + shadowOffsetPixels, rect.width, rect.height),
                text,
                shadowStyle);
            GUI.Label(rect, text, style);
        }

        public static void DrawPanel(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, RuntimeAssets.White);
            GUI.color = previousColor;
        }

        public static void DrawBorder(Rect rect, Color color, float thickness)
        {
            DrawPanel(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawPanel(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawPanel(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawPanel(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        public static Rect InsetRect(Rect rect, float padding) =>
            new Rect(rect.x + padding, rect.y + padding, rect.width - padding * 2f, rect.height - padding * 2f);

        public static Font GetUiFont()
        {
            if (_uiFont != null)
            {
                return _uiFont;
            }

            return ResolveUiFont();
        }

        private static Font ResolveUiFont()
        {
            if (_uiFont != null)
            {
                return _uiFont;
            }

            _uiFont = Resources.Load<Font>("Fonts/UiPixel");

            if (_uiFont != null)
            {
                return _uiFont;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (_uiFont != null)
            {
                return _uiFont;
            }

            _uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            if (_uiFont != null)
            {
                return _uiFont;
            }
#endif

            _uiFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Consolas", "Courier New", "Lucida Console", "Monospace" }, 16);

            if (_uiFont != null)
            {
                return _uiFont;
            }

            _uiFont = GUI.skin != null ? GUI.skin.font : null;
            return _uiFont;
        }

        private static GUIStyle CreateStyle(Font font, int size, FontStyle fontStyle, Color color, TextAnchor anchor)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = PresentationTheme.FontSize(size),
                fontStyle = fontStyle,
                alignment = anchor,
                padding = new RectOffset(4, 4, 2, 2)
            };
            SetTextColor(style, color);
            return style;
        }

        private static Color ResolveButtonColor(SurvivorsButtonKind kind, bool hover)
        {
            switch (kind)
            {
                case SurvivorsButtonKind.Green:
                    return hover ? new Color(0.46f, 0.92f, 0.52f) : new Color(0.28f, 0.72f, 0.36f);
                case SurvivorsButtonKind.Red:
                    return hover ? new Color(0.92f, 0.34f, 0.28f) : new Color(0.72f, 0.18f, 0.14f);
                case SurvivorsButtonKind.Blue:
                    return hover ? new Color(0.38f, 0.58f, 0.92f) : new Color(0.18f, 0.34f, 0.72f);
                default:
                    return hover ? new Color(1f, 0.9f, 0.42f) : new Color(0.96f, 0.76f, 0.18f);
            }
        }

        private static void ApplySolidBackground(GUIStyleState state)
        {
            state.background = GetSolidTexture(Color.clear);
        }

        private static Texture2D GetSolidTexture(Color color)
        {
            var key = color.GetHashCode();
            if (SolidTextures.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            SolidTextures[key] = texture;
            return texture;
        }

        private static void DrawDiamond(Vector2 center, Vector2 scale, Color color, float rotationDegrees = 0f)
        {
            var width = scale.x * 128f;
            var height = scale.y * 128f;
            var rect = new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
            var previousMatrix = GUI.matrix;

            try
            {
                GUIUtility.RotateAroundPivot(rotationDegrees, center);
                DrawTintedSprite(rect, RuntimeAssets.Diamond.texture, color);
            }
            finally
            {
                GUI.matrix = previousMatrix;
            }
        }

        private static void DrawTintedSprite(Rect rect, Texture texture, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
            GUI.color = previousColor;
        }

        private static void SetTextColor(GUIStyle style, Color color)
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
    }
}
