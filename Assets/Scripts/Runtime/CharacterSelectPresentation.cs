using UnityEngine;

namespace ProjectExpedition
{
    public enum CharacterPortraitSize
    {
        Compact,
        Large
    }

    public static class CharacterSelectPresentation
    {
        public const int GridColumns = 2;

        public static void DrawPortrait(Rect rect, CharacterDefinition definition, bool unlocked,
            CharacterPortraitSize size)
        {
            DrawPanel(rect, new Color(0.02f, 0.045f, 0.065f, 1f));

            if (definition == null)
            {
                return;
            }

            var useKeyArt = unlocked && size == CharacterPortraitSize.Large && definition.Id == "ravenbound.haldor";

            if (useKeyArt)
            {
                GUI.DrawTexture(rect, RuntimeAssets.Portrait, ScaleMode.ScaleAndCrop);
                DrawPanel(new Rect(rect.x, rect.y, rect.width, rect.height),
                    new Color(0.02f, 0.05f, 0.08f, 0.12f));
                return;
            }

            var tint = unlocked ? definition.Color : Desaturate(definition.Color, 0.35f);
            DrawSilhouette(rect, definition.Id, tint, size);

            if (!unlocked)
            {
                DrawPanel(new Rect(rect.x, rect.y, rect.width, rect.height),
                    new Color(0.02f, 0.03f, 0.04f, 0.55f));
            }
        }

        public static void DrawLockBadge(Rect rect)
        {
            var badgeRect = new Rect(rect.xMax - 34f, rect.y + 8f, 26f, 26f);
            DrawPanel(badgeRect, new Color(0.08f, 0.1f, 0.12f, 0.92f));
            DrawBorder(badgeRect, new Color(0.55f, 0.58f, 0.62f, 0.95f), 2f);
            GUI.Label(badgeRect, "🔒", CreateLockStyle());
        }

        private static void DrawSilhouette(Rect rect, string characterId, Color tint, CharacterPortraitSize size)
        {
            var scale = size == CharacterPortraitSize.Large ? 1f : 0.72f;
            var center = rect.center;

            switch (characterId)
            {
                case "ravenbound.haldor":
                    DrawHaldorSilhouette(center, scale, tint);
                    break;
                case "ravenbound.eira":
                    DrawEiraSilhouette(center, scale, tint);
                    break;
                case "oathbound.sylva":
                    DrawSylvaSilhouette(center, scale, tint);
                    break;
                case "ironway.mara":
                    DrawMaraSilhouette(center, scale, tint);
                    break;
                default:
                    DrawEmblem(center, scale * 0.42f, tint);
                    break;
            }
        }

        private static void DrawHaldorSilhouette(Vector2 center, float scale, Color tint)
        {
            var mantle = new Color(0.16f, 0.2f, 0.24f) * tint;
            var beard = new Color(0.7f, 0.34f, 0.12f) * tint;
            var brooch = new Color(0.94f, 0.67f, 0.2f) * tint;
            var axe = new Color(0.42f, 0.91f, 1f) * tint;

            DrawEmblem(center + new Vector2(0f, 18f * scale), 0.58f * scale, mantle);
            DrawDiamond(center + new Vector2(10f * scale, -18f * scale), new Vector2(0.22f, 0.32f) * scale, beard);
            DrawDiamond(center + new Vector2(-6f * scale, 4f * scale), Vector2.one * 0.08f * scale, brooch);
            DrawDiamond(center + new Vector2(46f * scale, 8f * scale), new Vector2(0.1f, 0.24f) * scale, axe, -24f);
        }

        private static void DrawEiraSilhouette(Vector2 center, float scale, Color tint)
        {
            var cloak = new Color(0.12f, 0.16f, 0.24f) * tint;
            var feather = new Color(0.58f, 0.72f, 0.92f) * tint;
            var raven = new Color(0.08f, 0.1f, 0.14f) * tint;

            DrawDiamond(center + new Vector2(0f, 6f * scale), new Vector2(0.44f, 0.54f) * scale, cloak);
            DrawDiamond(center + new Vector2(34f * scale, 28f * scale), new Vector2(0.08f, 0.28f) * scale, feather, 18f);
            DrawEmblem(center + new Vector2(-28f * scale, -22f * scale), 0.12f * scale, raven);
        }

        private static void DrawSylvaSilhouette(Vector2 center, float scale, Color tint)
        {
            var cloak = new Color(0.14f, 0.34f, 0.22f) * tint;
            var canopy = new Color(0.22f, 0.48f, 0.28f) * tint;
            var brooch = new Color(0.78f, 0.62f, 0.36f) * tint;
            var staff = new Color(0.36f, 0.58f, 0.32f) * tint;

            DrawDiamond(center + new Vector2(0f, 6f * scale), new Vector2(0.46f, 0.56f) * scale, cloak);
            DrawEmblem(center + new Vector2(-8f * scale, 22f * scale), 0.38f * scale, canopy);
            DrawDiamond(center + new Vector2(2f * scale, 14f * scale), Vector2.one * 0.1f * scale, brooch);
            DrawDiamond(center + new Vector2(42f * scale, -4f * scale), new Vector2(0.08f, 0.36f) * scale, staff, 14f);
        }

        private static void DrawMaraSilhouette(Vector2 center, float scale, Color tint)
        {
            var pack = new Color(0.24f, 0.28f, 0.26f) * tint;
            var plate = new Color(0.34f, 0.38f, 0.4f) * tint;
            var visor = new Color(0.42f, 0.82f, 0.92f) * tint;
            var launcher = new Color(0.92f, 0.48f, 0.18f) * tint;

            DrawEmblem(center + new Vector2(-14f * scale, 12f * scale), 0.3f * scale, pack);
            DrawDiamond(center + new Vector2(4f * scale, 4f * scale), new Vector2(0.36f, 0.48f) * scale, plate);
            DrawDiamond(center + new Vector2(10f * scale, 34f * scale), new Vector2(0.22f, 0.06f) * scale, visor);
            DrawDiamond(center + new Vector2(46f * scale, 6f * scale), new Vector2(0.1f, 0.18f) * scale, launcher, -18f);
        }

        private static void DrawEmblem(Vector2 center, float radius, Color color)
        {
            var size = radius * 2f * 128f;
            var rect = new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
            DrawTintedSprite(rect, RuntimeAssets.Circle.texture, color);
        }

        private static void DrawDiamond(Vector2 center, Vector2 scale, Color color, float rotationDegrees = 0f)
        {
            var width = scale.x * 128f;
            var height = scale.y * 128f;
            var rect = new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);

            var previousMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(rotationDegrees, center);
            DrawTintedSprite(rect, RuntimeAssets.Diamond.texture, color);
            GUI.matrix = previousMatrix;
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

        private static void DrawBorder(Rect rect, Color color, float thickness)
        {
            DrawPanel(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawPanel(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawPanel(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawPanel(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static GUIStyle _lockStyle;

        private static GUIStyle CreateLockStyle()
        {
            if (_lockStyle != null)
            {
                return _lockStyle;
            }

            _lockStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14
            };

            return _lockStyle;
        }
    }
}
