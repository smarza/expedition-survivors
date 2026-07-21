using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public static class UiArtCatalog
    {
        private static readonly Dictionary<string, Texture2D> CharacterPortraits = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Texture2D> MapKeyArts = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Texture2D> ItemIcons = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Texture2D> RelicIcons = new Dictionary<string, Texture2D>();
        private static Texture2D _titleArt;

        public static bool TryGetCharacterPortrait(string characterId, out Texture2D texture)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                texture = null;
                return false;
            }

            if (CharacterPortraits.TryGetValue(characterId, out texture) && texture != null)
            {
                return true;
            }

            var path = $"Art/Characters/{SanitizeId(characterId)}_Portrait";
            texture = Resources.Load<Texture2D>(path);

            if (texture == null && characterId == "ravenbound.haldor")
            {
                texture = RuntimeAssets.Portrait;
            }

            CharacterPortraits[characterId] = texture;
            return texture != null;
        }

        public static bool TryGetMapKeyArt(string mapId, out Texture2D texture)
        {
            if (string.IsNullOrWhiteSpace(mapId))
            {
                texture = null;
                return false;
            }

            if (MapKeyArts.TryGetValue(mapId, out texture) && texture != null)
            {
                return true;
            }

            var path = $"Art/Maps/{SanitizeId(mapId)}_KeyArt";
            texture = Resources.Load<Texture2D>(path);
            MapKeyArts[mapId] = texture;
            return texture != null;
        }

        public static bool TryGetItemIcon(string itemId, out Texture2D texture)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                texture = null;
                return false;
            }

            if (ItemIcons.TryGetValue(itemId, out texture) && texture != null)
            {
                return true;
            }

            var path = $"Art/Items/{SanitizeId(itemId)}_Icon";
            texture = Resources.Load<Texture2D>(path);
            ItemIcons[itemId] = texture;
            return texture != null;
        }

        public static bool TryGetRelicIcon(string relicId, out Texture2D texture)
        {
            if (string.IsNullOrWhiteSpace(relicId))
            {
                texture = null;
                return false;
            }

            if (RelicIcons.TryGetValue(relicId, out texture) && texture != null)
            {
                return true;
            }

            var path = $"Art/Relics/{SanitizeId(relicId)}_Icon";
            texture = Resources.Load<Texture2D>(path);
            RelicIcons[relicId] = texture;
            return texture != null;
        }

        public static bool TryGetTitleArt(out Texture2D texture)
        {
            if (_titleArt != null)
            {
                texture = _titleArt;
                return true;
            }

            _titleArt = Resources.Load<Texture2D>("Art/Title/ProjectExpedition_TitleArt");

            if (_titleArt == null)
            {
                _titleArt = Resources.Load<Texture2D>("Art/Haldor_Stormborn_KeyArt");
            }

            texture = _titleArt;
            return texture != null;
        }

        public static string SanitizeId(string id) => id.Replace('.', '_');

        public static void ClearCache()
        {
            CharacterPortraits.Clear();
            MapKeyArts.Clear();
            ItemIcons.Clear();
            RelicIcons.Clear();
            _titleArt = null;
        }
    }
}
