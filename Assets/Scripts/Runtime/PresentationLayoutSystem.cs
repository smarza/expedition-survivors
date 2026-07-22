using UnityEngine;

namespace ProjectExpedition
{
    public static class PresentationSpacing
    {
        public const float Space4 = 4f;
        public const float Space8 = 8f;
        public const float Space12 = 12f;
        public const float Space16 = 16f;
        public const float Space24 = 24f;
    }

    public enum CompactFontToken
    {
        Display,
        Heading,
        Body,
        Caption,
        Micro
    }

    public static class PresentationTypography
    {
        public static int BaseSize(CompactFontToken token)
        {
            switch (token)
            {
                case CompactFontToken.Display: return 36;
                case CompactFontToken.Heading: return 22;
                case CompactFontToken.Body: return 16;
                case CompactFontToken.Caption: return 13;
                case CompactFontToken.Micro: return 11;
                default: return 16;
            }
        }

        public static int ScaledSize(CompactFontToken token) =>
            PresentationTheme.FontSize(BaseSize(token));
    }

    public readonly struct LayoutZone
    {
        public readonly Rect Rect;

        public LayoutZone(Rect rect)
        {
            Rect = rect;
        }

        public LayoutZone(float x, float y, float width, float height)
        {
            Rect = new Rect(x, y, width, height);
        }

        public LayoutZone Inset(float padding)
        {
            return new LayoutZone(
                Rect.x + padding,
                Rect.y + padding,
                Mathf.Max(0f, Rect.width - padding * 2f),
                Mathf.Max(0f, Rect.height - padding * 2f));
        }

        public LayoutZone Inset(float horizontal, float vertical)
        {
            return new LayoutZone(
                Rect.x + horizontal,
                Rect.y + vertical,
                Mathf.Max(0f, Rect.width - horizontal * 2f),
                Mathf.Max(0f, Rect.height - vertical * 2f));
        }

        public LayoutZone[] DivideHorizontal(float gap, params float[] widths)
        {
            if (widths == null || widths.Length == 0)
            {
                return new[] { this };
            }

            var totalWidth = 0f;
            for (var i = 0; i < widths.Length; i++)
            {
                totalWidth += widths[i];
            }

            totalWidth += gap * (widths.Length - 1);
            var zones = new LayoutZone[widths.Length];
            var cursorX = Rect.x;

            for (var i = 0; i < widths.Length; i++)
            {
                zones[i] = new LayoutZone(cursorX, Rect.y, widths[i], Rect.height);
                cursorX += widths[i] + gap;
            }

            return zones;
        }

        public LayoutZone[] DivideVertical(float gap, params float[] heights)
        {
            if (heights == null || heights.Length == 0)
            {
                return new[] { this };
            }

            var totalHeight = 0f;
            for (var i = 0; i < heights.Length; i++)
            {
                totalHeight += heights[i];
            }

            totalHeight += gap * (heights.Length - 1);
            var zones = new LayoutZone[heights.Length];
            var cursorY = Rect.y;

            for (var i = 0; i < heights.Length; i++)
            {
                zones[i] = new LayoutZone(Rect.x, cursorY, Rect.width, heights[i]);
                cursorY += heights[i] + gap;
            }

            return zones;
        }

        public static float ComputeGridTileSize(float areaWidth, float areaHeight, int columns, int rows, float gap)
        {
            if (columns <= 0 || rows <= 0)
            {
                return 0f;
            }

            var tileWidth = (areaWidth - gap * (columns - 1)) / columns;
            var tileHeight = (areaHeight - gap * (rows - 1)) / rows;
            return Mathf.Min(tileWidth, tileHeight);
        }
    }

    public static class PresentationTextMeasure
    {
        public static float MeasureHeight(GUIStyle style, string text, float width)
        {
            if (style == null || string.IsNullOrEmpty(text))
            {
                return 0f;
            }

            return style.CalcHeight(new GUIContent(text), width);
        }

        public static float MeasureWidth(GUIStyle style, string text)
        {
            if (style == null || string.IsNullOrEmpty(text))
            {
                return 0f;
            }

            return style.CalcSize(new GUIContent(text)).x;
        }

        public static float ClampHeight(float measuredHeight, float minimumHeight, float maximumHeight)
        {
            return Mathf.Clamp(measuredHeight, minimumHeight, maximumHeight);
        }
    }

    public static class CharacterSelectLayoutMetrics
    {
        public const float SideColumnWidth = 280f;
        public const float StatsColumnWidth = SideColumnWidth;
        public const float FilterColumnWidth = SideColumnWidth;
        public const float SoloFooterHeight = 200f;
        public const float SoloHeaderHeight = 56f;
        public const float ScreenPadding = 24f;
        public const float ColumnGap = 12f;
        public const float StatRowHeight = 34f;
        public const float GridNameBandHeight = 32f;
        public const float MinimumGridTileSize = 148f;
        public const int SoloGridColumns = 4;
        public const int CoopGridColumns = 3;

        public static float SoloGridWidth(float innerBodyWidth) =>
            Mathf.Max(720f, innerBodyWidth - StatsColumnWidth - FilterColumnWidth - ColumnGap * 2f);

        public static int GridColumnsFor(int playerCount) =>
            playerCount > 1 ? CoopGridColumns : SoloGridColumns;

        public static bool MeetsMinimumTileSize(float gridWidth, float gridHeight, int characterCount, int columns, float gap)
        {
            var rows = Mathf.Max(1, (characterCount + columns - 1) / columns);
            var tileWidth = (gridWidth - gap * (columns - 1)) / columns;
            var tileHeight = (gridHeight - gap * (rows - 1)) / rows;
            return tileWidth >= MinimumGridTileSize && tileHeight >= MinimumGridTileSize;
        }
    }

    public static class MapSelectLayoutMetrics
    {
        public const float StatsColumnWidth = CharacterSelectLayoutMetrics.StatsColumnWidth;
        public const float ChallengeColumnWidth = CharacterSelectLayoutMetrics.FilterColumnWidth;
        public const float FooterHeight = CharacterSelectLayoutMetrics.SoloFooterHeight;
        public const float HeaderHeight = CharacterSelectLayoutMetrics.SoloHeaderHeight;
        public const float ScreenPadding = CharacterSelectLayoutMetrics.ScreenPadding;
        public const float ColumnGap = CharacterSelectLayoutMetrics.ColumnGap;
        public const float StatRowHeight = CharacterSelectLayoutMetrics.StatRowHeight;
        public const float GridNameBandHeight = CharacterSelectLayoutMetrics.GridNameBandHeight;
        public const float MinimumGridTileSize = CharacterSelectLayoutMetrics.MinimumGridTileSize;
        public const int GridColumns = CharacterSelectLayoutMetrics.SoloGridColumns;

        public static float GridWidth(float innerBodyWidth) =>
            CharacterSelectLayoutMetrics.SoloGridWidth(innerBodyWidth);

        public static bool MeetsMinimumTileSize(float gridWidth, float gridHeight, int mapCount, float gap)
        {
            var rows = Mathf.Max(1, (mapCount + GridColumns - 1) / GridColumns);
            var tileWidth = (gridWidth - gap * (GridColumns - 1)) / GridColumns;
            var tileHeight = (gridHeight - gap * (rows - 1)) / rows;
            return tileWidth >= MinimumGridTileSize && tileHeight >= MinimumGridTileSize;
        }
    }
}
