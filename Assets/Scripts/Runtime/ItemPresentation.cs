using UnityEngine;

namespace ProjectExpedition
{
    public enum ItemIconSize
    {
        Small,
        Medium,
        Large
    }

    public static class ItemPresentation
    {
        public static void DrawItemIcon(Rect rect, string itemId, ItemIconSize size, bool isEvolved = false, Color accent = default)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            var item = ItemCatalog.Find(itemId);
            var color = accent == default && item != null ? item.Color : accent;
            var frameColor = isEvolved ? SurvivorsStylePresentation.BorderGoldBright : SurvivorsStylePresentation.BorderGoldDim;

            SurvivorsStylePresentation.DrawFlatPanel(rect, SurvivorsStylePresentation.PanelNavyInset, 1f, frameColor);

            if (UiArtCatalog.TryGetItemIcon(itemId, out var texture))
            {
                var inset = SurvivorsStylePresentation.InsetRect(rect, 3f);
                GUI.DrawTexture(inset, texture, ScaleMode.ScaleToFit, true);
            }
            else if (item != null)
            {
                DrawFallbackIcon(rect, item, size);
            }
            else
            {
                DrawUnknownIcon(rect, color);
            }

            if (isEvolved)
            {
                var badgeRect = new Rect(rect.xMax - 18f, rect.y + 2f, 16f, 12f);
                SurvivorsStylePresentation.DrawPanel(badgeRect, SurvivorsStylePresentation.BorderGoldBright);
            }
        }

        public static void DrawRelicIcon(Rect rect, string relicId, bool isWarden = false)
        {
            SurvivorsStylePresentation.DrawFlatPanel(rect, SurvivorsStylePresentation.PanelNavyInset, 1f);

            if (UiArtCatalog.TryGetRelicIcon(relicId, out var texture))
            {
                var inset = SurvivorsStylePresentation.InsetRect(rect, 3f);
                GUI.DrawTexture(inset, texture, ScaleMode.ScaleToFit, true);
            }
            else
            {
                DrawRelicFallback(rect, relicId);
            }

            if (isWarden || relicId != null && relicId.EndsWith("_warden"))
            {
                var overlay = new Rect(rect.x, rect.y, rect.width, rect.height);
                SurvivorsStylePresentation.DrawBorder(overlay, SurvivorsStylePresentation.BorderGoldBright, 2f);
            }
        }

        private static void DrawFallbackIcon(Rect rect, ItemDefinition item, ItemIconSize size)
        {
            var inset = SurvivorsStylePresentation.InsetRect(rect, 6f);
            var center = inset.center;
            var scale = ResolveIconScale(inset, size);

            switch (item.Category)
            {
                case ItemCategory.Weapon:
                    CharacterSelectPresentation.DrawStarterWeaponIcon(inset, item.Id, item.Color);
                    break;
                case ItemCategory.Evolution:
                    DrawDiamond(center, Vector2.one * scale * 1.1f, item.Color, 0f);
                    DrawDiamond(center, Vector2.one * scale * 0.55f, SurvivorsStylePresentation.BorderGoldBright, 45f);
                    break;
                case ItemCategory.Boon:
                    DrawEmblem(center, scale * 0.9f, item.Color);
                    break;
                case ItemCategory.Gear:
                    DrawGearIcon(inset, item);
                    break;
                default:
                    DrawPanel(inset, new Color(item.Color.r, item.Color.g, item.Color.b, 0.35f));
                    GUI.Label(inset, item.ShortName, SurvivorsStylePresentation.TileNameStyle);
                    break;
            }
        }

        internal static bool TryResolveGearStatIconKind(UpgradeId effect, out CharacterStatIconKind kind)
        {
            switch (effect)
            {
                case UpgradeId.MoveSpeed:
                    kind = CharacterStatIconKind.Speed;
                    return true;
                case UpgradeId.MaxHealth:
                case UpgradeId.SapRegen:
                case UpgradeId.Heal:
                case UpgradeId.PersistentHealAura:
                    kind = CharacterStatIconKind.Health;
                    return true;
                case UpgradeId.Armor:
                case UpgradeId.SiegeKnockback:
                    kind = CharacterStatIconKind.Armor;
                    return true;
                case UpgradeId.Magnet:
                    kind = CharacterStatIconKind.Magnet;
                    return true;
                case UpgradeId.CriticalRunes:
                case UpgradeId.UltimateDamage:
                    kind = CharacterStatIconKind.Damage;
                    return true;
                case UpgradeId.UltimateCooldown:
                    kind = CharacterStatIconKind.Cooldown;
                    return true;
                case UpgradeId.AxePierce:
                case UpgradeId.ExtraAxe:
                case UpgradeId.ExtraOrbit:
                case UpgradeId.ExtraRadial:
                    kind = CharacterStatIconKind.Radius;
                    return true;
                default:
                    kind = default;
                    return false;
            }
        }

        private static void DrawGearIcon(Rect rect, ItemDefinition item)
        {
            if (TryResolveGearStatIconKind(item.EffectAtLevel(1), out var kind))
            {
                CharacterSelectPresentation.DrawStatIcon(rect, kind, item.Color);
                return;
            }

            DrawGearAbbreviation(rect, item);
        }

        private static void DrawGearAbbreviation(Rect rect, ItemDefinition item)
        {
            DrawPanel(rect, new Color(item.Color.r, item.Color.g, item.Color.b, 0.35f));

            var shortName = item.ShortName ?? string.Empty;
            var label = shortName.Length <= 3
                ? shortName
                : shortName.Substring(0, 3);

            GUI.Label(rect, label, SurvivorsStylePresentation.TileNameStyle);
        }

        private static void DrawRelicFallback(Rect rect, string relicId)
        {
            var tint = ResolveRelicTint(relicId);
            var inset = SurvivorsStylePresentation.InsetRect(rect, 8f);
            DrawPanel(inset, new Color(tint.r, tint.g, tint.b, 0.28f));
            DrawEmblem(inset.center, Mathf.Min(inset.width, inset.height) / 128f * 0.5f, tint);
        }

        private static void DrawUnknownIcon(Rect rect, Color color)
        {
            var inset = SurvivorsStylePresentation.InsetRect(rect, 8f);
            DrawPanel(inset, new Color(color.r, color.g, color.b, 0.22f));
            DrawDiamond(inset.center, Vector2.one * (inset.width / 160f), color, 0f);
        }

        private static Color ResolveRelicTint(string relicId)
        {
            if (string.IsNullOrEmpty(relicId))
            {
                return SurvivorsStylePresentation.TextMuted;
            }

            if (relicId.Contains("heartwood"))
            {
                return new Color(0.42f, 0.82f, 0.48f);
            }

            if (relicId.Contains("siege"))
            {
                return new Color(0.72f, 0.52f, 0.38f);
            }

            return new Color(0.38f, 0.72f, 0.92f);
        }

        private static float ResolveIconScale(Rect inset, ItemIconSize size)
        {
            var baseScale = Mathf.Min(inset.width, inset.height) / 96f;

            switch (size)
            {
                case ItemIconSize.Small:
                    return baseScale * 0.85f;
                case ItemIconSize.Large:
                    return baseScale * 1.15f;
                default:
                    return baseScale;
            }
        }

        private static void DrawPanel(Rect rect, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, RuntimeAssets.White);
            GUI.color = old;
        }

        private static void DrawDiamond(Vector2 center, Vector2 scale, Color color, float rotationDegrees)
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

        private static void DrawEmblem(Vector2 center, float scale, Color color)
        {
            var radius = scale * 128f * 0.5f;
            var rect = new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f);
            DrawTintedSprite(rect, RuntimeAssets.Circle.texture, color);
        }

        private static void DrawTintedSprite(Rect rect, Texture texture, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = old;
        }
    }
}
