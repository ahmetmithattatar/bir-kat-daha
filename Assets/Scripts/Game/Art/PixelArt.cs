using System.Collections.Generic;
using UnityEngine;

namespace RiskLoop.Game.Art
{
    /// <summary>
    /// Kodla görsel üretimi. Hiç resim dosyası yok:
    ///   - FromRows: metin ızgarasından pixel art sprite (her harf bir renk, '.' saydam)
    ///   - Rock / Cracks / Rays / Glow / Vignette: rastgele (ama seed'li, yani her seferinde aynı) üretilen dokular
    /// Hepsi "Point" filtreli: büyütülünce bulanıklaşmaz, retro piksel görünümü korunur.
    /// Üretilen her şey önbelleğe alınır (aynı sprite iki kez üretilmez, bellek sızmaz).
    /// </summary>
    public static class PixelArt
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Her Play başında önbelleği boşalt. Projede "Enter Play Mode Options" ile Domain Reload kapalı: statik alanlar
        /// iki Play arasında hafızada kalır, ama Unity Play bitince kodla üretilen sprite/texture'ları siler.
        /// Temizlenmezse ikinci Play'de önbellek SİLİNMİŞ sprite döndürür ve görseller beyaz kutu olur.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Cache.Clear();

        /// <summary>Önbellekte geçerli (silinmemiş) sprite varsa döndürür. Silinmişse kaydı atar, yeniden üretilsin.</summary>
        internal static bool TryGetCached(string key, out Sprite sprite)
        {
            if (Cache.TryGetValue(key, out sprite))
            {
                if (sprite != null) return true;   // Unity'de silinmiş nesne "== null" olur
                Cache.Remove(key);
            }
            sprite = null;
            return false;
        }

        /// <summary>
        /// Metin ızgarasından sprite. rows[0] en ÜST satırdır. Eksik/fazla karakter hata vermez ('.' sayılır).
        /// </summary>
        public static Sprite FromRows(string key, string[] rows, Dictionary<char, Color32> palette)
        {
            if (TryGetCached(key, out Sprite cached)) return cached;

            int height = rows.Length;
            int width = 0;
            foreach (string row in rows) width = Mathf.Max(width, row.Length);

            Texture2D tex = NewTexture(width, height);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                string row = rows[y];
                for (int x = 0; x < width; x++)
                {
                    char c = x < row.Length ? row[x] : '.';
                    Color32 color = palette.TryGetValue(c, out Color32 p) ? p : new Color32(0, 0, 0, 0);
                    // Texture'da y=0 en alttır, metinde en üst: ters çeviriyoruz
                    pixels[(height - 1 - y) * width + x] = color;
                }
            }
            return Finish(key, tex, pixels);
        }

        /// <summary>Kaya dokusu: taban renk üzerinde gürültü, koyu lekeler ve parlak benekler.</summary>
        public static Sprite Rock(string key, int seed, Color baseColor, int size = 48)
        {
            if (TryGetCached(key, out Sprite cached)) return cached;

            var rng = new System.Random(seed);
            Texture2D tex = NewTexture(size, size);
            var pixels = new Color32[size * size];

            // Kaba lekeler için düşük çözünürlüklü gürültü (4x4 bloklar)
            int blocks = size / 4 + 1;
            var blockNoise = new float[blocks, blocks];
            for (int bx = 0; bx < blocks; bx++)
                for (int by = 0; by < blocks; by++)
                    blockNoise[bx, by] = (float)rng.NextDouble();

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float coarse = blockNoise[x / 4, y / 4];
                    float fine = (float)rng.NextDouble();
                    float shade = 0.72f + coarse * 0.28f + (fine - 0.5f) * 0.12f;
                    if (fine > 0.985f) shade += 0.35f;                 // parlak benek (mineral)
                    if (coarse < 0.12f && fine < 0.6f) shade -= 0.25f; // koyu çatlak/leke
                    Color c = baseColor * shade;
                    c.a = 1f;
                    pixels[y * size + x] = c;
                }
            }
            return Finish(key, tex, pixels);
        }

        /// <summary>
        /// Çatlak katmanı: merkezden dışa rastgele yürüyen koyu çizgiler. stage arttıkça daha çok ve uzun çatlak.
        /// Aynı seed + farklı stage = aynı çatlakların büyümüş hali (birikimli görünür).
        /// </summary>
        public static Sprite Cracks(string key, int seed, int stage, int size = 48)
        {
            if (TryGetCached(key, out Sprite cached)) return cached;

            Texture2D tex = NewTexture(size, size);
            var pixels = new Color32[size * size];
            var dark = new Color32(15, 10, 8, 230);
            var light = new Color32(255, 230, 180, 90);  // çatlak kenarında hafif parlama

            int branches = 3 + stage * 2;
            for (int b = 0; b < branches; b++)
            {
                // Her dalın kendi rastgele kaynağı var: stage artınca eski dallar AYNI yoldan uzar, yenileri eklenir
                var rng = new System.Random(seed * 31 + b);
                float angle = (float)(rng.NextDouble() * Mathf.PI * 2);
                int length = (int)(size * (0.15f + 0.12f * stage) * (0.6 + rng.NextDouble() * 0.6));
                float x = size / 2f, y = size / 2f + size * 0.05f;
                for (int i = 0; i < length; i++)
                {
                    angle += (float)(rng.NextDouble() - 0.5) * 0.9f;
                    x += Mathf.Cos(angle);
                    y += Mathf.Sin(angle);
                    int px = (int)x, py = (int)y;
                    if (px < 1 || py < 1 || px >= size - 1 || py >= size - 1) break;
                    pixels[py * size + px] = dark;
                    if (pixels[(py + 1) * size + px].a == 0) pixels[(py + 1) * size + px] = light;
                }
            }
            return Finish(key, tex, pixels);
        }

        /// <summary>Dönen ışık hüzmeleri (nadir bulgunun arkasında). Beyaz çizilir, Image.color ile boyanır.</summary>
        public static Sprite Rays(int rayCount = 12, int size = 64)
        {
            string key = $"rays_{rayCount}_{size}";
            if (TryGetCached(key, out Sprite cached)) return cached;

            Texture2D tex = NewTexture(size, size);
            var pixels = new Color32[size * size];
            float c = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - c, dy = y - c;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / c;
                    if (dist > 1f) continue;
                    float angle = Mathf.Atan2(dy, dx);
                    float ray = Mathf.Cos(angle * rayCount) > 0.55f ? 1f : 0f;
                    float a = ray * (1f - dist) * 0.9f + (1f - dist) * 0.25f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            return Finish(key, tex, pixels);
        }

        /// <summary>Yumuşak yuvarlak parıltı (merkez dolu, kenar saydam).</summary>
        public static Sprite Glow(int size = 32)
        {
            string key = $"glow_{size}";
            if (TryGetCached(key, out Sprite cached)) return cached;

            Texture2D tex = NewTexture(size, size);
            var pixels = new Color32[size * size];
            float c = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01(1f - d);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
                }
            return Finish(key, tex, pixels);
        }

        /// <summary>Kenarları kararan çerçeve (fener ışığı hissi). Ortası saydam.</summary>
        public static Sprite Vignette(int size = 64)
        {
            string key = $"vignette_{size}";
            if (TryGetCached(key, out Sprite cached)) return cached;

            Texture2D tex = NewTexture(size, size);
            tex.filterMode = FilterMode.Bilinear;  // Bu yumuşak olsun
            var pixels = new Color32[size * size];
            float c = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01((d - 0.45f) / 0.6f);
                    pixels[y * size + x] = new Color(0f, 0f, 0f, a);
                }
            return Finish(key, tex, pixels);
        }

        /// <summary>Şok dalgası halkası: içi boş, kenarı yumuşak beyaz çember (Image rengiyle boyanır).</summary>
        public static Sprite Ring(int size = 96)
        {
            string key = $"ring_{size}";
            if (TryGetCached(key, out Sprite cached)) return cached;

            Texture2D tex = NewTexture(size, size);
            var pixels = new Color32[size * size];
            float c = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;   // 0 merkez, 1 kenar
                    float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.88f) / 0.1f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
                }
            }
            return Finish(key, tex, pixels);
        }

        // ---------------- Yardımcılar ----------------

        internal static Texture2D NewTexture(int width, int height)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        internal static Sprite Finish(string key, Texture2D tex, Color32[] pixels)
        {
            tex.SetPixels32(pixels);
            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16f);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }
    }
}
