using UnityEngine;

namespace ProjectExpedition
{
    public enum CharacterPortraitSize
    {
        Compact,
        Large
    }

    public enum CharacterStatIconKind
    {
        Health,
        Speed,
        Armor,
        Magnet,
        Cooldown,
        Damage,
        Radius
    }

    public static class CharacterSelectPresentation
    {
        public const int GridColumns = CharacterSelectLayoutMetrics.SoloGridColumns;

        public const string HealthLabel = "HEALTH";
        public const string SpeedLabel = "SPEED";
        public const string ArmorLabel = "ARMOR";
        public const string UltimateCooldownLabel = "ULT COOLDOWN";
        public const string UltimateDamageLabel = "ULT DAMAGE";
        public const string UltimateRangeLabel = "ULT RANGE";
        public const string MagnetLabel = "MAGNET";

        public static int GridColumnsFor(int playerCount) =>
            CharacterSelectLayoutMetrics.GridColumnsFor(playerCount);

        public static void DrawPortrait(Rect rect, CharacterDefinition definition, bool unlocked,
            CharacterPortraitSize size)
        {
            DrawPanel(rect, SurvivorsStylePresentation.PanelNavyInset);

            if (definition == null)
            {
                return;
            }

            Texture2D portrait = null;
            var useKeyArt = unlocked && UiArtCatalog.TryGetCharacterPortrait(definition.Id, out portrait);

            if (useKeyArt && portrait != null)
            {
                GUI.DrawTexture(rect, portrait, ScaleMode.ScaleAndCrop);
                DrawPanel(new Rect(rect.x, rect.y, rect.width, rect.height),
                    new Color(0.02f, 0.05f, 0.08f, unlocked ? 0.12f : 0.55f));
                return;
            }

            var tint = unlocked ? definition.Color : Desaturate(definition.Color, 0.35f);

            GUI.BeginGroup(rect);
            var localRect = new Rect(0f, 0f, rect.width, rect.height);

            if (size == CharacterPortraitSize.Large)
            {
                var glowRect = new Rect(localRect.width * 0.18f, localRect.height * 0.12f,
                    localRect.width * 0.64f, localRect.height * 0.72f);
                DrawPanel(glowRect, new Color(tint.r, tint.g, tint.b, 0.12f));
            }

            DrawSilhouette(localRect, definition.Id, tint, size);

            if (!unlocked)
            {
                DrawPanel(localRect, new Color(0.02f, 0.03f, 0.04f, 0.55f));
            }

            GUI.EndGroup();
        }

        public static void DrawLockBadge(Rect rect)
        {
            SurvivorsStylePresentation.DrawLockBadge(rect);
        }

        public static void DrawStatIcon(Rect rect, CharacterStatIconKind kind, Color tint)
        {
            var iconRect = new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f);
            var color = ApplyHeroTint(ResolveStatBaseColor(kind), tint);
            var iconScale = Mathf.Min(iconRect.width, iconRect.height) / 128f;

            switch (kind)
            {
                case CharacterStatIconKind.Health:
                    DrawEmblem(iconRect.center, 0.42f * iconScale, color);
                    break;
                case CharacterStatIconKind.Speed:
                    DrawDiamond(iconRect.center, new Vector2(0.34f, 0.48f) * iconScale, color, 18f);
                    break;
                case CharacterStatIconKind.Armor:
                    DrawPanel(iconRect, color);
                    DrawBorder(iconRect, Color.Lerp(color, Color.white, 0.35f), 1f);
                    break;
                case CharacterStatIconKind.Magnet:
                    DrawDiamond(iconRect.center, Vector2.one * (iconRect.width / 160f), color);
                    break;
                case CharacterStatIconKind.Cooldown:
                    DrawEmblem(iconRect.center, 0.34f * iconScale,
                        new Color(color.r, color.g, color.b, 0.72f));
                    DrawDiamond(iconRect.center, Vector2.one * (iconRect.width / 220f), color, 45f);
                    break;
                case CharacterStatIconKind.Damage:
                    DrawDiamond(iconRect.center, new Vector2(0.18f, 0.42f) * iconScale, color, -24f);
                    break;
                case CharacterStatIconKind.Radius:
                    DrawEmblem(iconRect.center, 0.46f * iconScale,
                        new Color(color.r, color.g, color.b, 0.28f));
                    DrawEmblem(iconRect.center, 0.24f * iconScale, color);
                    break;
            }
        }

        public static void DrawStarterWeaponIcon(Rect rect, string weaponId, Color heroTint)
        {
            DrawPanel(rect, SurvivorsStylePresentation.PanelNavyInset);

            var weaponColor = ResolveWeaponColor(weaponId, heroTint);
            var center = rect.center;
            var scale = ResolveWeaponIconScale(rect);

            switch (weaponId)
            {
                case "weapon.frost_axe":
                    DrawDiamond(center, new Vector2(0.12f, 0.28f) * scale, weaponColor, -24f);
                    break;
                case "weapon.raven_guard":
                    DrawEmblem(center, 0.34f * scale, weaponColor);
                    break;
                case "weapon.grove_thorn_lash":
                    DrawDiamond(center, new Vector2(0.1f, 0.34f) * scale, weaponColor, 12f);
                    break;
                case "weapon.canopy_vortex":
                    DrawEmblem(center, 0.28f * scale, weaponColor);
                    DrawDiamond(center, Vector2.one * 0.12f * scale, weaponColor, 45f);
                    break;
                case "weapon.signal_flare":
                    DrawDiamond(center, new Vector2(0.14f, 0.22f) * scale, weaponColor, -18f);
                    break;
                case "weapon.supply_pulse":
                    DrawEmblem(center, 0.22f * scale, weaponColor);
                    DrawPanel(new Rect(rect.x + 4f, rect.y + rect.height * 0.5f - 2f, rect.width - 8f, 4f), weaponColor);
                    break;
                default:
                    DrawDiamond(center, Vector2.one * 0.18f * scale, weaponColor);
                    break;
            }
        }

        public static string ResolvePrimaryStarterWeaponId(CharacterDefinition definition)
        {
            if (definition?.StarterWeaponIds == null || definition.StarterWeaponIds.Length == 0)
            {
                return "weapon.frost_axe";
            }

            return definition.StarterWeaponIds[0];
        }

        private static Color ResolveStatBaseColor(CharacterStatIconKind kind)
        {
            switch (kind)
            {
                case CharacterStatIconKind.Health: return new Color(0.82f, 0.24f, 0.28f);
                case CharacterStatIconKind.Speed: return new Color(0.42f, 0.88f, 0.52f);
                case CharacterStatIconKind.Armor: return new Color(0.58f, 0.66f, 0.74f);
                case CharacterStatIconKind.Magnet: return new Color(0.35f, 0.82f, 0.96f);
                case CharacterStatIconKind.Cooldown: return new Color(0.72f, 0.58f, 0.92f);
                case CharacterStatIconKind.Damage: return new Color(0.96f, 0.52f, 0.22f);
                case CharacterStatIconKind.Radius: return new Color(0.92f, 0.78f, 0.28f);
                default: return new Color(0.62f, 0.72f, 0.76f);
            }
        }

        private static Color ResolveWeaponColor(string weaponId, Color heroTint)
        {
            switch (weaponId)
            {
                case "weapon.frost_axe": return ApplyHeroTint(new Color(0.42f, 0.91f, 1f), heroTint);
                case "weapon.raven_guard": return ApplyHeroTint(new Color(0.5f, 0.78f, 0.92f), heroTint);
                case "weapon.grove_thorn_lash": return ApplyHeroTint(new Color(0.42f, 0.78f, 0.32f), heroTint);
                case "weapon.canopy_vortex": return ApplyHeroTint(new Color(0.32f, 0.72f, 0.42f), heroTint);
                case "weapon.signal_flare": return ApplyHeroTint(new Color(0.96f, 0.58f, 0.18f), heroTint);
                case "weapon.supply_pulse": return ApplyHeroTint(new Color(0.82f, 0.62f, 0.28f), heroTint);
                default: return ApplyHeroTint(new Color(0.72f, 0.76f, 0.82f), heroTint);
            }
        }

        private static void DrawSilhouette(Rect rect, string characterId, Color tint, CharacterPortraitSize size)
        {
            var scale = ResolveSilhouetteScale(rect, size);
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
                case "oathbound.bren":
                    DrawBrenSilhouette(center, scale, tint);
                    break;
                case "ironway.mara":
                    DrawMaraSilhouette(center, scale, tint);
                    break;
                case "ironway.rex":
                    DrawRexSilhouette(center, scale, tint);
                    break;
                default:
                    DrawEmblem(center, scale * 0.42f, ApplyHeroTint(new Color(0.35f, 0.38f, 0.42f), tint));
                    break;
            }
        }

        private static float ResolveSilhouetteScale(Rect rect, CharacterPortraitSize size)
        {
            var minDimension = Mathf.Min(rect.width, rect.height);
            var normalized = minDimension / 128f;

            if (size == CharacterPortraitSize.Large)
            {
                return normalized * 0.9f;
            }

            return normalized * 0.62f;
        }

        private static float ResolveWeaponIconScale(Rect rect)
        {
            var minDimension = Mathf.Min(rect.width, rect.height);
            return minDimension / 96f;
        }

        private static Color ApplyHeroTint(Color baseColor, Color heroTint)
        {
            var blended = Color.Lerp(baseColor, heroTint, 0.32f);
            return new Color(
                Mathf.Clamp01(blended.r * 1.12f),
                Mathf.Clamp01(blended.g * 1.12f),
                Mathf.Clamp01(blended.b * 1.12f),
                1f);
        }

        private static void DrawHaldorSilhouette(Vector2 center, float scale, Color tint)
        {
            var mantle = ApplyHeroTint(new Color(0.16f, 0.2f, 0.24f), tint);
            var beard = ApplyHeroTint(new Color(0.7f, 0.34f, 0.12f), tint);
            var brooch = ApplyHeroTint(new Color(0.94f, 0.67f, 0.2f), tint);
            var axe = ApplyHeroTint(new Color(0.42f, 0.91f, 1f), tint);

            DrawEmblem(center + new Vector2(0f, 18f * scale), 0.58f * scale, mantle);
            DrawDiamond(center + new Vector2(10f * scale, -18f * scale), new Vector2(0.22f, 0.32f) * scale, beard);
            DrawDiamond(center + new Vector2(-6f * scale, 4f * scale), Vector2.one * 0.08f * scale, brooch);
            DrawDiamond(center + new Vector2(46f * scale, 8f * scale), new Vector2(0.1f, 0.24f) * scale, axe, -24f);
        }

        private static void DrawEiraSilhouette(Vector2 center, float scale, Color tint)
        {
            var cloak = ApplyHeroTint(new Color(0.12f, 0.16f, 0.24f), tint);
            var feather = ApplyHeroTint(new Color(0.58f, 0.72f, 0.92f), tint);
            var raven = ApplyHeroTint(new Color(0.08f, 0.1f, 0.14f), tint);

            DrawDiamond(center + new Vector2(0f, 6f * scale), new Vector2(0.44f, 0.54f) * scale, cloak);
            DrawDiamond(center + new Vector2(34f * scale, 28f * scale), new Vector2(0.08f, 0.28f) * scale, feather, 18f);
            DrawEmblem(center + new Vector2(-28f * scale, -22f * scale), 0.12f * scale, raven);
        }

        private static void DrawSylvaSilhouette(Vector2 center, float scale, Color tint)
        {
            var cloak = ApplyHeroTint(new Color(0.14f, 0.34f, 0.22f), tint);
            var canopy = ApplyHeroTint(new Color(0.22f, 0.48f, 0.28f), tint);
            var brooch = ApplyHeroTint(new Color(0.78f, 0.62f, 0.36f), tint);
            var staff = ApplyHeroTint(new Color(0.36f, 0.58f, 0.32f), tint);

            DrawDiamond(center + new Vector2(0f, 6f * scale), new Vector2(0.46f, 0.56f) * scale, cloak);
            DrawEmblem(center + new Vector2(-8f * scale, 22f * scale), 0.38f * scale, canopy);
            DrawDiamond(center + new Vector2(2f * scale, 14f * scale), Vector2.one * 0.1f * scale, brooch);
            DrawDiamond(center + new Vector2(42f * scale, -4f * scale), new Vector2(0.08f, 0.36f) * scale, staff, 14f);
        }

        private static void DrawBrenSilhouette(Vector2 center, float scale, Color tint)
        {
            var mantle = ApplyHeroTint(new Color(0.22f, 0.34f, 0.24f), tint);
            var wrap = ApplyHeroTint(new Color(0.34f, 0.24f, 0.16f), tint);
            var brooch = ApplyHeroTint(new Color(0.72f, 0.52f, 0.24f), tint);
            var staff = ApplyHeroTint(new Color(0.48f, 0.38f, 0.28f), tint);

            DrawEmblem(center + new Vector2(0f, 16f * scale), 0.54f * scale, mantle);
            DrawDiamond(center + new Vector2(-6f * scale, -12f * scale), new Vector2(0.36f, 0.22f) * scale, wrap);
            DrawDiamond(center + new Vector2(4f * scale, 12f * scale), Vector2.one * 0.1f * scale, brooch);
            DrawDiamond(center + new Vector2(42f * scale, -2f * scale), new Vector2(0.08f, 0.38f) * scale, staff, 10f);
        }

        private static void DrawMaraSilhouette(Vector2 center, float scale, Color tint)
        {
            var pack = ApplyHeroTint(new Color(0.24f, 0.28f, 0.26f), tint);
            var plate = ApplyHeroTint(new Color(0.52f, 0.56f, 0.58f), tint);
            var visor = ApplyHeroTint(new Color(0.42f, 0.82f, 0.92f), tint);
            var launcher = ApplyHeroTint(new Color(0.92f, 0.48f, 0.18f), tint);

            DrawEmblem(center + new Vector2(-18f * scale, 14f * scale), 0.34f * scale, pack);
            DrawDiamond(center + new Vector2(4f * scale, 2f * scale), new Vector2(0.42f, 0.56f) * scale, plate);
            DrawDiamond(center + new Vector2(8f * scale, 38f * scale), new Vector2(0.28f, 0.08f) * scale, visor);
            DrawDiamond(center + new Vector2(52f * scale, 4f * scale), new Vector2(0.12f, 0.22f) * scale, launcher, -18f);
        }

        private static void DrawRexSilhouette(Vector2 center, float scale, Color tint)
        {
            var shield = ApplyHeroTint(new Color(0.34f, 0.36f, 0.4f), tint);
            var plate = ApplyHeroTint(new Color(0.58f, 0.5f, 0.38f), tint);
            var band = ApplyHeroTint(new Color(0.92f, 0.58f, 0.18f), tint);
            var launcher = ApplyHeroTint(new Color(0.78f, 0.42f, 0.18f), tint);

            DrawEmblem(center + new Vector2(-16f * scale, 8f * scale), 0.3f * scale, shield);
            DrawDiamond(center + new Vector2(6f * scale, 0f), new Vector2(0.4f, 0.54f) * scale, plate);
            DrawDiamond(center + new Vector2(8f * scale, 34f * scale), new Vector2(0.24f, 0.07f) * scale, band);
            DrawDiamond(center + new Vector2(48f * scale, 8f * scale), new Vector2(0.14f, 0.28f) * scale, launcher, -22f);
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
    }
}
