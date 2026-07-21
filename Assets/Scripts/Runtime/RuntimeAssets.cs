using UnityEngine;

namespace ProjectExpedition
{
    public static class RuntimeAssets
    {
        private static Sprite _circle;
        private static Sprite _diamond;
        private static Sprite _square;
        private static Texture2D _portrait;
        private static Texture2D _white;

        public static Sprite Circle => _circle != null ? _circle : (_circle = MakeCircleSprite(64));
        public static Sprite Diamond => _diamond != null ? _diamond : (_diamond = MakeDiamondSprite(48));
        public static Sprite Square => _square != null ? _square : (_square = MakeSquareSprite(32));
        public static Texture2D Portrait
        {
            get
            {
                if (_portrait != null) return _portrait;
                _portrait = Resources.Load<Texture2D>("Art/Haldor_Stormborn_KeyArt");
                return _portrait != null ? _portrait : (_portrait = MakeHaldorPortrait());
            }
        }
        public static Texture2D White => _white != null ? _white : (_white = MakeSolid(Color.white));

        private static Sprite MakeCircleSprite(int size)
        {
            var texture = NewTexture(size, size);
            var pixels = new Color32[size * size];
            var radius = size * 0.47f;
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                pixels[y * size + x] = distance <= radius ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        private static Sprite MakeDiamondSprite(int size)
        {
            var texture = NewTexture(size, size);
            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var inside = Mathf.Abs(x - center) + Mathf.Abs(y - center) <= center * 0.92f;
                pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        private static Sprite MakeSquareSprite(int size)
        {
            var texture = NewTexture(size, size);
            var pixels = new Color32[size * size];
            var fill = new Color32(255, 255, 255, 255);

            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = fill;
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        private static Texture2D MakeHaldorPortrait()
        {
            const int size = 256;
            var texture = NewTexture(size, size);
            var pixels = new Color32[size * size];
            Fill(pixels, new Color32(13, 29, 48, 255));

            // Aurora and fjord backdrop.
            PaintCircle(pixels, size, 190, 55, 125, new Color32(25, 96, 115, 255));
            PaintRect(pixels, size, 0, 183, 256, 73, new Color32(18, 42, 63, 255));
            PaintTriangle(pixels, size, new Vector2Int(0, 204), new Vector2Int(83, 126), new Vector2Int(142, 204), new Color32(36, 58, 72, 255));
            PaintTriangle(pixels, size, new Vector2Int(112, 204), new Vector2Int(207, 115), new Vector2Int(256, 204), new Color32(30, 52, 68, 255));

            // Fur mantle and broad body.
            PaintCircle(pixels, size, 128, 218, 89, new Color32(47, 43, 40, 255));
            PaintCircle(pixels, size, 67, 197, 41, new Color32(113, 105, 91, 255));
            PaintCircle(pixels, size, 192, 197, 41, new Color32(113, 105, 91, 255));
            PaintRect(pixels, size, 82, 153, 92, 103, new Color32(36, 55, 70, 255));

            // Head, undercut and face.
            PaintCircle(pixels, size, 128, 103, 54, new Color32(219, 159, 111, 255));
            PaintRect(pixels, size, 77, 62, 102, 30, new Color32(144, 86, 44, 255));
            PaintTriangle(pixels, size, new Vector2Int(81, 72), new Vector2Int(132, 33), new Vector2Int(177, 74), new Color32(177, 111, 54, 255));

            // Beard, braids, eyes and scar.
            PaintTriangle(pixels, size, new Vector2Int(83, 117), new Vector2Int(173, 117), new Vector2Int(128, 190), new Color32(159, 89, 39, 255));
            PaintCircle(pixels, size, 107, 104, 5, new Color32(29, 45, 51, 255));
            PaintCircle(pixels, size, 149, 104, 5, new Color32(29, 45, 51, 255));
            PaintRect(pixels, size, 99, 129, 58, 6, new Color32(92, 48, 31, 255));
            PaintLine(pixels, size, 159, 87, 151, 108, 3, new Color32(122, 63, 48, 255));
            PaintLine(pixels, size, 104, 142, 96, 189, 7, new Color32(183, 104, 47, 255));
            PaintLine(pixels, size, 151, 142, 161, 189, 7, new Color32(183, 104, 47, 255));

            // Raven brooch, shield rim and frost-rune axe.
            PaintCircle(pixels, size, 128, 199, 14, new Color32(194, 145, 44, 255));
            PaintTriangle(pixels, size, new Vector2Int(116, 197), new Vector2Int(139, 187), new Vector2Int(135, 207), new Color32(21, 31, 39, 255));
            PaintCircleOutline(pixels, size, 224, 200, 53, 9, new Color32(128, 139, 143, 255));
            PaintLine(pixels, size, 39, 222, 68, 89, 10, new Color32(100, 71, 45, 255));
            PaintTriangle(pixels, size, new Vector2Int(50, 91), new Vector2Int(82, 67), new Vector2Int(77, 112), new Color32(128, 152, 159, 255));
            PaintLine(pixels, size, 57, 89, 76, 85, 3, new Color32(83, 223, 239, 255));

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D MakeSolid(Color color)
        {
            var texture = NewTexture(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static Texture2D NewTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Project Expedition Runtime Art"
            };
            return texture;
        }

        private static void Fill(Color32[] pixels, Color32 color)
        {
            for (var i = 0; i < pixels.Length; i++) pixels[i] = color;
        }

        private static void PaintRect(Color32[] p, int w, int x, int y, int width, int height, Color32 c)
        {
            for (var yy = Mathf.Max(0, y); yy < Mathf.Min(w, y + height); yy++)
            for (var xx = Mathf.Max(0, x); xx < Mathf.Min(w, x + width); xx++) p[yy * w + xx] = c;
        }

        private static void PaintCircle(Color32[] p, int w, int cx, int cy, int r, Color32 c)
        {
            var rr = r * r;
            for (var y = Mathf.Max(0, cy - r); y < Mathf.Min(w, cy + r); y++)
            for (var x = Mathf.Max(0, cx - r); x < Mathf.Min(w, cx + r); x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= rr) p[y * w + x] = c;
        }

        private static void PaintCircleOutline(Color32[] p, int w, int cx, int cy, int r, int thickness, Color32 c)
        {
            var outer = r * r;
            var inner = (r - thickness) * (r - thickness);
            for (var y = Mathf.Max(0, cy - r); y < Mathf.Min(w, cy + r); y++)
            for (var x = Mathf.Max(0, cx - r); x < Mathf.Min(w, cx + r); x++)
            {
                var d = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                if (d <= outer && d >= inner) p[y * w + x] = c;
            }
        }

        private static void PaintTriangle(Color32[] p, int w, Vector2Int a, Vector2Int b, Vector2Int c, Color32 color)
        {
            var minX = Mathf.Max(0, Mathf.Min(a.x, Mathf.Min(b.x, c.x)));
            var maxX = Mathf.Min(w - 1, Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
            var minY = Mathf.Max(0, Mathf.Min(a.y, Mathf.Min(b.y, c.y)));
            var maxY = Mathf.Min(w - 1, Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
            for (var y = minY; y <= maxY; y++)
            for (var x = minX; x <= maxX; x++)
            {
                var pnt = new Vector2(x, y);
                var d1 = Sign(pnt, a, b); var d2 = Sign(pnt, b, c); var d3 = Sign(pnt, c, a);
                var neg = d1 < 0 || d2 < 0 || d3 < 0;
                var pos = d1 > 0 || d2 > 0 || d3 > 0;
                if (!(neg && pos)) p[y * w + x] = color;
            }
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3) =>
            (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);

        private static void PaintLine(Color32[] p, int w, int x0, int y0, int x1, int y1, int thickness, Color32 color)
        {
            var steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            for (var i = 0; i <= steps; i++)
            {
                var t = steps == 0 ? 0f : i / (float)steps;
                PaintCircle(p, w, Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), thickness, color);
            }
        }
    }
}
