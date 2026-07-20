using UnityEngine;

namespace ProjectExpedition
{
    public enum MapPreviewSize
    {
        Compact,
        Large
    }

    public static class MapSelectPresentation
    {
        public const int GridColumns = 2;

        public static void DrawPreview(Rect rect, MapDefinition definition, bool unlocked, MapPreviewSize size)
        {
            DrawPanel(rect, new Color(0.02f, 0.045f, 0.065f, 1f));

            if (definition == null)
            {
                return;
            }

            var tint = unlocked ? definition.GroundColor : Desaturate(definition.GroundColor, 0.35f);
            DrawShoreScene(rect, definition.Id, tint, size);

            if (!unlocked)
            {
                DrawPanel(new Rect(rect.x, rect.y, rect.width, rect.height),
                    new Color(0.02f, 0.03f, 0.04f, 0.55f));
            }
        }

        private static void DrawShoreScene(Rect rect, string mapId, Color groundTint, MapPreviewSize size)
        {
            var skyHeight = rect.height * (size == MapPreviewSize.Large ? 0.42f : 0.38f);
            var skyRect = new Rect(rect.x, rect.y, rect.width, skyHeight);
            var groundRect = new Rect(rect.x, rect.y + skyHeight, rect.width, rect.height - skyHeight);

            var isSaga = mapId == "frostbound.saga";
            var skyTop = isSaga ? new Color(0.03f, 0.06f, 0.1f) : new Color(0.05f, 0.12f, 0.16f);
            var skyBottom = isSaga ? new Color(0.08f, 0.14f, 0.2f) : new Color(0.1f, 0.22f, 0.28f);
            DrawVerticalGradient(skyRect, skyTop, skyBottom);

            var auroraStrength = isSaga ? 0.55f : 0.32f;
            var auroraColor = new Color(0.18f, 0.72f, 0.62f, auroraStrength);
            var auroraRect = new Rect(
                rect.x + rect.width * 0.12f,
                rect.y + rect.height * 0.06f,
                rect.width * 0.76f,
                skyHeight * 0.42f);
            DrawPanel(auroraRect, auroraColor);

            DrawPanel(groundRect, groundTint * 1.15f);
            DrawHorizonLine(new Rect(rect.x, rect.y + skyHeight - 2f, rect.width, 4f), new Color(0.22f, 0.42f, 0.52f, 0.85f));

            var peakScale = size == MapPreviewSize.Large ? 1f : 0.72f;
            DrawPeak(
                new Vector2(rect.x + rect.width * 0.18f, rect.y + skyHeight),
                new Vector2(rect.width * 0.22f * peakScale, skyHeight * 0.72f * peakScale),
                new Color(0.12f, 0.2f, 0.26f) * groundTint);
            DrawPeak(
                new Vector2(rect.x + rect.width * 0.52f, rect.y + skyHeight),
                new Vector2(rect.width * (isSaga ? 0.28f : 0.24f) * peakScale, skyHeight * (isSaga ? 0.88f : 0.78f) * peakScale),
                new Color(0.1f, 0.18f, 0.24f) * groundTint);
            DrawPeak(
                new Vector2(rect.x + rect.width * 0.82f, rect.y + skyHeight),
                new Vector2(rect.width * 0.2f * peakScale, skyHeight * 0.66f * peakScale),
                new Color(0.14f, 0.22f, 0.28f) * groundTint);

            if (isSaga)
            {
                DrawPanel(new Rect(rect.x + rect.width * 0.68f, rect.y + rect.height * 0.08f, rect.width * 0.12f, rect.width * 0.12f),
                    new Color(0.72f, 0.78f, 0.86f, 0.35f));
            }

            var beaconX = rect.x + rect.width * 0.5f;
            var beaconY = groundRect.y + groundRect.height * 0.55f;
            DrawBeacon(new Rect(beaconX - 6f, beaconY - 6f, 12f, 12f), new Color(0.42f, 0.91f, 1f, 1f));
            DrawPanel(new Rect(beaconX - 18f, beaconY + 8f, 36f, 4f), new Color(0.28f, 0.68f, 0.82f, 0.65f));
        }

        private static void DrawPeak(Vector2 baseCenter, Vector2 size, Color color)
        {
            var halfWidth = size.x * 0.5f;
            var peakRect = new Rect(baseCenter.x - halfWidth, baseCenter.y - size.y, size.x, size.y);
            DrawTintedSprite(peakRect, RuntimeAssets.Diamond.texture, color);
        }

        private static void DrawBeacon(Rect rect, Color color)
        {
            DrawTintedSprite(rect, RuntimeAssets.Circle.texture, color);
        }

        private static void DrawHorizonLine(Rect rect, Color color)
        {
            DrawPanel(rect, color);
        }

        private static void DrawVerticalGradient(Rect rect, Color top, Color bottom)
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

        private static void DrawTintedSprite(Rect rect, Texture texture, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
            GUI.color = previousColor;
        }

        private static Color Desaturate(Color color, float saturationScale)
        {
            var gray = color.grayscale;
            return Color.Lerp(new Color(gray, gray, gray, color.a), color, saturationScale);
        }

        private static void DrawPanel(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, RuntimeAssets.White, ScaleMode.StretchToFill);
            GUI.color = previousColor;
        }
    }
}
