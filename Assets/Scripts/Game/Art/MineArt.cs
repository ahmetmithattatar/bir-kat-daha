using UnityEngine;

namespace RiskLoop.Game.Art
{
    /// <summary>
    /// Maden tüneli ve kazılan kaya yüzü (kodla, dosyasız).
    ///
    /// Tünel: ekranın her pikseli için "bu bakış çizgisi tünelin hangi yüzeyine çarpıyor?" hesaplanır (basit perspektif).
    /// Tünel bir kutu: yarı genişlik HalfW, yarı yükseklik HalfH, kamera tam ortada. Ekranda (u, v) noktası
    /// z derinliğinde dünyadaki (u*z, v*z) noktasıdır. Yan duvara z = HalfW/|u|'da, tavan/tabana z = HalfH/|v|'de çarpar.
    /// Hangisi yakınsa o görünür. Tünelin sonu (EndZ) kazılan kaya duvarıyla aynı boyda: 1920x1080 ekranda 640x600.
    ///
    /// Kaya yüzü: taş blokları (Voronoi hücreleri), aralarında çatlaklar, kabartma, cevher damarı, düzensiz (saydam) kenar.
    ///
    /// GÖRSEL ZENGİNLİK DERİNLİKLE ARTAR (level 0..5, her 10 katta bir, DepthTheme.DetailLevel):
    /// ilk katlar sade (yumuşak kaya, tek destek çerçevesi, birkaç büyük taş), oyuncu indikçe katmanlar, raylar,
    /// fenerler, cevher damarları ve en derinde parlayan cevher eklenir. Derine inmek görsel bir ödül de olur.
    /// </summary>
    public static class MineArt
    {
        // Tünel geometrisi (dünya birimi). EndZ'de genişlik 1/3, yükseklik 0.556 ekran = 640x600 duvar.
        private const float HalfW = 1f;
        private const float HalfH = 1.667f;
        private const float EndZ = 3f;
        private const float Aspect = 16f / 9f;
        private const float FrameThickness = 0.09f;

        // Raylar ve traversler (tabanda, dünya birimi)
        private const float RailX = 0.36f;
        private const float RailWidth = 0.035f;
        private const float SleeperPeriod = 0.2f;

        private static readonly Color WoodColor = new Color(0.45f, 0.29f, 0.15f);
        private static readonly Color SteelColor = new Color(0.62f, 0.64f, 0.68f);
        private static readonly Color LanternLight = new Color(1f, 0.78f, 0.45f);
        private static readonly Color AmbientLight = new Color(1f, 0.92f, 0.8f);   // Elimizdeki fenerin sıcak ışığı

        public const int MaxLevel = 5;

        /// <summary>Bir görsel seviyede neler çizilir. Tek yerde toplandı: seviyeleri ayarlamak için sadece Levels'a bak.</summary>
        private struct Detail
        {
            public float[] frames;     // Ahşap destek çerçevelerinin derinlikleri
            public float[] lanterns;   // Asılı fenerlerin derinlikleri
            public float contrast;     // Kaya gürültüsünün sertliği (0.5 = yumuşak, 1 = tam)
            public float strata;       // Tortu katmanlarının belirginliği (0 = yok)
            public bool cracks;        // Duvarlarda ince çatlaklar
            public float oreSpecks;    // Duvarlarda cevher beneği eşiği (1 = hiç yok, 0.95 = sık)
            public bool rails;         // Tabanda ray + travers
            public float oreGlow;      // Cevherin karanlıkta parlaması (0 = yok)
            public int stones;         // Kaya yüzündeki taş sayısı (az = büyük, sade bloklar)
            public float bevel;        // Taş kenarı kabartması
            public int veins;          // Kaya yüzündeki cevher damarı sayısı
            public bool sparkle;       // Damarda parıltı pikselleri
            public float grains;       // Kaya yüzünde tek cevher tanesi eşiği (1 = yok)
            public float grit;         // İnce kumlanma
        }

        private static readonly Detail[] Levels =
        {
            // 0: 1-10. kat. Sade: yumuşak kaya, tek çerçeve, 6 büyük taş, damar yok
            new Detail { frames = new[] { 1.55f }, lanterns = new float[0], contrast = 0.5f, strata = 0f, cracks = false,
                         oreSpecks = 1f, rails = false, oreGlow = 0f, stones = 6, bevel = 0.12f, veins = 0, sparkle = false,
                         grains = 1f, grit = 0.03f },
            // 1: 11-20. kat. Hafif katmanlar, iki çerçeve, ilk cevher taneleri
            new Detail { frames = new[] { 1.55f, 2.6f }, lanterns = new float[0], contrast = 0.65f, strata = 0.5f, cracks = false,
                         oreSpecks = 0.985f, rails = false, oreGlow = 0f, stones = 9, bevel = 0.18f, veins = 0, sparkle = false,
                         grains = 0.997f, grit = 0.04f },
            // 2: 21-30. kat. Raylar, çatlaklar, ince bir cevher damarı
            new Detail { frames = new[] { 1.12f, 1.55f, 2.6f }, lanterns = new float[0], contrast = 0.8f, strata = 0.75f, cracks = true,
                         oreSpecks = 0.97f, rails = true, oreGlow = 0f, stones = 12, bevel = 0.22f, veins = 1, sparkle = false,
                         grains = 0.997f, grit = 0.05f },
            // 3: 31-40. kat. Asılı fener, dört çerçeve, parıltılı damar
            new Detail { frames = new[] { 1.12f, 1.55f, 2.05f, 2.6f }, lanterns = new[] { 1.55f }, contrast = 0.9f, strata = 1f, cracks = true,
                         oreSpecks = 0.96f, rails = true, oreGlow = 0f, stones = 15, bevel = 0.25f, veins = 1, sparkle = true,
                         grains = 0.996f, grit = 0.07f },
            // 4: 41-50. kat. İki fener, yoğun cevher
            new Detail { frames = new[] { 1.12f, 1.55f, 2.05f, 2.6f }, lanterns = new[] { 1.12f, 2.05f }, contrast = 1f, strata = 1f, cracks = true,
                         oreSpecks = 0.95f, rails = true, oreGlow = 0.15f, stones = 18, bevel = 0.25f, veins = 2, sparkle = true,
                         grains = 0.994f, grit = 0.08f },
            // 5: 51+. kat. Cevher karanlıkta parlar, iki parlak damar
            new Detail { frames = new[] { 1.12f, 1.55f, 2.05f, 2.6f }, lanterns = new[] { 1.12f, 2.05f }, contrast = 1f, strata = 1f, cracks = true,
                         oreSpecks = 0.94f, rails = true, oreGlow = 0.6f, stones = 20, bevel = 0.28f, veins = 2, sparkle = true,
                         grains = 0.99f, grit = 0.08f },
        };

        private static Detail GetDetail(int level) => Levels[Mathf.Clamp(level, 0, Levels.Length - 1)];

        // ---------------- Tünel ----------------

        /// <summary>Tam ekran tünel görseli (384x216, ekranda 5 kat büyür). level: görsel zenginlik 0..5.</summary>
        public static Sprite Tunnel(string key, int seed, Color rock, Color ore, int level)
        {
            if (PixelArt.TryGetCached(key, out Sprite cached)) return cached;

            Detail d = GetDetail(level);
            const int width = 384, height = 216;
            Texture2D tex = PixelArt.NewTexture(width, height);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Ekran koordinatı -1..1 (v > 0 yukarı, texture'da y=0 en alt)
                    float u = (x + 0.5f) / width * 2f - 1f;
                    float v = (y + 0.5f) / height * 2f - 1f;
                    Color c = TunnelPixel(u, v, x, y, seed, rock, ore, d);
                    c.a = 1f;
                    pixels[y * width + x] = c;
                }
            }
            return PixelArt.Finish(key, tex, pixels);
        }

        private static Color TunnelPixel(float u, float v, int px, int py, int seed, Color rock, Color ore, Detail d)
        {
            float au = Mathf.Abs(u) + 1e-4f;
            float av = Mathf.Abs(v) + 1e-4f;
            float zSide = HalfW / au;
            float zRoof = HalfH / av;
            float z = Mathf.Min(zSide, zRoof);
            float grit = (Hash(px, py, seed + 99) - 0.5f) * d.grit;   // Ekran ölçeğinde ince kumlanma

            // Tünelin sonu: duvarın arkası, zifiri karanlık kaya (duvarın düzensiz kenarından görünür)
            if (z >= EndZ) return rock * (0.1f + grit * 0.5f);

            Color surface;
            bool isOre = false;
            if (zSide <= zRoof)
            {
                // Yan duvar: derinlik z, yükseklik Y
                float Y = v * z;
                float side = u > 0 ? 50f : 0f;   // Sol ve sağ duvar farklı desen
                if (InFrame(z, d.frames, out float local))
                    surface = Wood(local, Y * 3f, seed);   // Dikme (direk)
                else
                    surface = RockSurface(z * 5f + side, Y * 5f, Y, seed, rock, ore, d, out isOre);
                // Köşelere yakın (tavan/taban birleşimi) gölge
                surface *= 1f - 0.4f * Smooth(0.75f, 1f, Mathf.Abs(Y) / HalfH);
            }
            else if (v > 0f)
            {
                // Tavan: derinlik z, yatay X
                float X = u * z;
                if (InFrame(z, d.frames, out float local))
                    surface = Wood(local, X * 3f + 7f, seed);   // Başlık kirişi
                else
                    surface = RockSurface(z * 5f + 20f, X * 5f, X * 0.3f, seed, rock, ore, d, out isOre) * 0.85f;
                surface *= 1f - 0.35f * Smooth(0.7f, 1f, Mathf.Abs(X) / HalfW);
            }
            else
            {
                surface = Floor(u * z, z, seed, rock, d);
            }

            // Işık: elimizdeki fener yakını aydınlatır, derinlikle kararır. Asılı fenerler sıcak ışık halesi verir.
            float light = Mathf.Clamp01(1.3f / (z * z * 0.45f + 0.85f));
            Color lit = surface * light;
            lit.r *= AmbientLight.r; lit.g *= AmbientLight.g; lit.b *= AmbientLight.b;
            lit += LanternGlow(u, v, d.lanterns, out bool lanternBody) * surface.grayscale * 1.6f;
            if (lanternBody) return new Color(1f, 0.9f, 0.55f);
            if (isOre) lit = Color.Lerp(lit, ore, d.oreGlow);   // Derinde cevher karanlıkta da parlar
            return lit + new Color(grit, grit, grit);
        }

        /// <summary>Bu derinlik bir ahşap çerçevenin içinde mi? local: çerçeve içindeki konum 0..1.</summary>
        private static bool InFrame(float z, float[] frames, out float local)
        {
            foreach (float fz in frames)
            {
                if (z >= fz && z < fz + FrameThickness)
                {
                    local = (z - fz) / FrameThickness;
                    return true;
                }
            }
            local = 0f;
            return false;
        }

        /// <summary>Ahşap: lif çizgileri + kenarlarda koyu çizgi (kalas gibi görünsün).</summary>
        private static Color Wood(float local, float along, int seed)
        {
            float grain = Noise(local * 6f, along * 4f, seed + 31);
            float shade = 0.75f + grain * 0.35f;
            if (local < 0.12f || local > 0.88f) shade *= 0.6f;   // Kalasın kenarı
            if (local < 0.3f) shade *= 1.15f;                    // Kameraya bakan yüz biraz aydınlık
            return WoodColor * shade;
        }

        /// <summary>
        /// Kaya yüzeyi: kaba gürültü + (seviyeye göre) tortu katmanları, ince çatlaklar, cevher benekleri.
        /// (a, b) yüzey koordinatı, strataY katman yönü. isOre: bu piksel cevher mi (parlama için).
        /// </summary>
        private static Color RockSurface(float a, float b, float strataY, int seed, Color rock, Color ore, Detail d, out bool isOre)
        {
            isOre = false;
            float coarse = Noise(a, b, seed);
            float detail = Noise(a * 3f, b * 3f, seed + 7);
            float raw = 0.65f + coarse * 0.35f + (detail - 0.5f) * 0.2f;
            float shade = 0.82f + (raw - 0.82f) * d.contrast;   // Düşük kontrast = yumuşak, sade kaya

            // Katmanlar: dalgalı yatay bantlar, açık-koyu sırayla
            if (d.strata > 0f)
            {
                float band = strataY * 3.2f + Noise(a * 0.4f, b * 0.2f, seed + 3) * 1.6f;
                float bandShade = 0.26f * Mathf.Sin(band * 3.1f) + 0.1f * Mathf.Sin(band * 9.7f);
                shade *= 1f + (bandShade - 0.2f * d.strata) * d.strata;
            }

            // İnce çatlak: gürültünün tam ortadan geçtiği yerler koyu çizgi olur
            if (d.cracks && Mathf.Abs(Noise(a * 1.3f, b * 1.3f, seed + 11) - 0.5f) < 0.012f) shade *= 0.55f;

            // Cevher benekleri: seyrek hücrelerde parlak taş
            int cx = Mathf.FloorToInt(a * 2.6f), cy = Mathf.FloorToInt(b * 2.6f);
            if (Hash(cx, cy, seed + 17) > d.oreSpecks)
            {
                float fx = a * 2.6f - cx - 0.5f, fy = b * 2.6f - cy - 0.5f;
                if (fx * fx + fy * fy < 0.05f)
                {
                    isOre = true;
                    return ore * (1.1f + detail * 0.4f);
                }
            }
            return rock * shade;
        }

        /// <summary>Taban: çakıl, seviyeye göre traversler + iki ray.</summary>
        private static Color Floor(float X, float z, int seed, Color rock, Detail d)
        {
            float gravel = Noise(X * 14f, z * 14f, seed + 41);
            Color c = rock * (0.66f + (gravel - 0.5f) * 0.3f * d.contrast);
            float ax = Mathf.Abs(X);

            if (d.rails)
            {
                float sleeper = Mathf.Repeat(z, SleeperPeriod) / SleeperPeriod;
                if (ax < 0.55f && sleeper < 0.32f)
                    c = WoodColor * (0.7f + Noise(X * 8f, z * 3f, seed + 43) * 0.3f);   // Travers

                float railDist = Mathf.Abs(ax - RailX);
                if (railDist < RailWidth)
                {
                    // Rayın üstü parlak, kenarı koyu
                    float top = 1f - railDist / RailWidth;
                    c = SteelColor * (0.55f + 0.6f * top);
                }
            }
            // Kenarlar duvara doğru gölgede
            c *= 1f - 0.35f * Smooth(0.65f, 1f, ax / HalfW);
            return c;
        }

        /// <summary>Tavandaki asılı fenerlerin ışığı (ekran uzayında). body: fenerin kendisi mi.</summary>
        private static Color LanternGlow(float u, float v, float[] lanterns, out bool body)
        {
            body = false;
            float glow = 0f;
            foreach (float lz in lanterns)
            {
                float fz = lz + FrameThickness * 0.5f;
                float sy = (HalfH - 0.55f) / fz;   // Fener tavandan biraz aşağıda asılı
                float dx = u * Aspect;
                float dy = v - sy;
                float d2 = dx * dx + dy * dy;
                float bodyR = 0.07f / fz;
                if (d2 < bodyR * bodyR) body = true;
                // İp: fenerden tavana ince çizgi
                if (Mathf.Abs(dx) < 0.012f && dy > 0f && dy < 0.55f / fz) body = true;
                float r = 0.6f / fz;
                glow += Mathf.Exp(-d2 / (r * r)) * (0.9f / fz);
            }
            return LanternLight * glow;
        }

        // ---------------- Kaya yüzü (kazılan duvar) ----------------

        /// <summary>Kazılan kaya yüzü: taş blokları, kabartma, (seviyeye göre) katmanlar, cevher damarları, düzensiz kenar.</summary>
        public static Sprite RockFace(string key, int seed, Color rock, Color ore, int level, int width = 128, int height = 120)
        {
            if (PixelArt.TryGetCached(key, out Sprite cached)) return cached;

            Detail dt = GetDetail(level);
            var rng = new System.Random(seed);
            // Taş blokların merkezleri (Voronoi). Az taş = büyük, sade bloklar
            int stoneCount = dt.stones;
            var stones = new Vector2[stoneCount];
            var stoneShade = new float[stoneCount];
            for (int i = 0; i < stoneCount; i++)
            {
                stones[i] = new Vector2((float)rng.NextDouble() * 2.4f - 1.2f, (float)rng.NextDouble() * 2.4f - 1.2f);
                stoneShade[i] = 1f + ((float)rng.NextDouble() - 0.5f) * 0.45f * dt.contrast;
            }
            // Cevher damarları: yüzü çapraz geçen dalgalı çizgiler
            var veins = new float[dt.veins, 5];
            for (int v = 0; v < dt.veins; v++)
            {
                veins[v, 0] = 0.15f + (float)rng.NextDouble() * 0.3f;     // dalga yüksekliği
                veins[v, 1] = 2f + (float)rng.NextDouble() * 2f;          // dalga sıklığı
                veins[v, 2] = (float)rng.NextDouble() * 6.28f;            // dalga kayması
                veins[v, 3] = (float)rng.NextDouble() * 0.8f - 0.4f;      // yükseklik
                veins[v, 4] = (float)rng.NextDouble() * 0.8f - 0.4f;      // eğim
            }

            Texture2D tex = PixelArt.NewTexture(width, height);
            var pixels = new Color32[width * height];
            var lightDir = new Vector2(-0.55f, 0.83f);   // Işık üst-soldan

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = (x + 0.5f) / width * 2f - 1f;
                    float ny = (y + 0.5f) / height * 2f - 1f;

                    // Düzensiz kenar: yuvarlatılmış dikdörtgen + gürültü. Dışı saydam (tünelin karanlık sonu görünür)
                    float dist = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));
                    dist = Mathf.Lerp(dist, Mathf.Sqrt(nx * nx + ny * ny) / 1.3f, 0.35f);
                    float limit = 0.9f + 0.1f * Noise(x * 0.12f, y * 0.12f, seed + 5);
                    if (dist > limit)
                    {
                        pixels[y * width + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    // En yakın iki taş: aradaki fark küçükse iki taşın sınırındayız (çatlak)
                    var p = new Vector2(nx, ny);
                    int nearest = 0;
                    float d1 = float.MaxValue, d2 = float.MaxValue;
                    for (int i = 0; i < stoneCount; i++)
                    {
                        float d = (p - stones[i]).sqrMagnitude;
                        if (d < d1) { d2 = d1; d1 = d; nearest = i; }
                        else if (d < d2) d2 = d;
                    }
                    float edgeDist = Mathf.Sqrt(d2) - Mathf.Sqrt(d1);

                    float detail = Noise(x * 0.25f, y * 0.25f, seed + 9);
                    float fine = Noise(x * 0.7f, y * 0.7f, seed + 10);
                    float texture = (detail - 0.5f) * 0.3f + (fine - 0.5f) * 0.18f;
                    float shade = stoneShade[nearest] * (0.87f + texture * dt.contrast) * 0.88f;

                    // Katmanlar (hafif eğik bantlar)
                    if (dt.strata > 0f)
                    {
                        float band = (ny + nx * 0.15f) * 7f + Noise(x * 0.05f, y * 0.05f, seed + 13) * 2f;
                        shade *= 1f + 0.12f * dt.strata * Mathf.Sin(band * 2.2f);
                    }

                    // Kabartma: taşın ışığa bakan kenarı parlak, öbür kenarı koyu
                    float facing = Vector2.Dot((p - stones[nearest]).normalized, lightDir);
                    if (edgeDist < 0.12f) shade *= 1f + facing * dt.bevel * (1f - edgeDist / 0.12f);
                    if (edgeDist < 0.025f) shade *= 0.4f;                     // Taşlar arası çatlak

                    // Ortası fenerle aydınlık, kenarlar gölgede
                    shade *= 1.05f - 0.6f * Smooth(0.4f, limit, dist);

                    Color c = rock * shade;

                    // Cevher damarları
                    bool inVein = false;
                    float nearestVein = float.MaxValue;
                    for (int v = 0; v < dt.veins; v++)
                    {
                        float curve = veins[v, 3] + veins[v, 4] * nx + veins[v, 0] * Mathf.Sin(nx * veins[v, 1] + veins[v, 2]);
                        float thickness = 0.012f + 0.03f * Noise(x * 0.18f, 3f + v * 10f, seed + 21);
                        float off = Mathf.Abs(ny - curve) - thickness;
                        nearestVein = Mathf.Min(nearestVein, off);
                        if (off < 0f && edgeDist > 0.02f) inVein = true;
                    }
                    if (inVein)
                    {
                        c = ore * (0.95f + detail * 0.35f);
                        if (dt.sparkle && Hash(x, y, seed + 23) > 0.86f) c = Color.Lerp(ore, Color.white, 0.7f);   // Parıltı
                    }
                    else
                    {
                        // En derinde damarın etrafı cevher ışığıyla boyanır
                        if (dt.oreGlow > 0f && nearestVein < 0.12f)
                            c = Color.Lerp(c, ore, dt.oreGlow * 0.45f * (1f - nearestVein / 0.12f));
                        // Seyrek tek cevher taneleri
                        if (Hash(x / 2, y / 2, seed + 29) > dt.grains) c = ore * 1.15f;
                    }

                    // İnce kumlanma
                    float grit = (Hash(x, y, seed + 31) - 0.5f) * dt.grit;
                    c += new Color(grit, grit, grit);
                    c.a = 1f;
                    pixels[y * width + x] = c;
                }
            }
            return PixelArt.Finish(key, tex, pixels);
        }

        // ---------------- Gürültü yardımcıları ----------------

        /// <summary>Tam sayı koordinatından 0..1 sözde rastgele sayı (aynı girdi = aynı sonuç).</summary>
        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        /// <summary>Yumuşak değer gürültüsü: ızgara köşelerindeki Hash değerleri arasında pürüzsüz geçiş.</summary>
        private static float Noise(float x, float y, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float tx = x - x0, ty = y - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            float a = Mathf.Lerp(Hash(x0, y0, seed), Hash(x0 + 1, y0, seed), tx);
            float b = Mathf.Lerp(Hash(x0, y0 + 1, seed), Hash(x0 + 1, y0 + 1, seed), tx);
            return Mathf.Lerp(a, b, ty);
        }

        private static float Smooth(float from, float to, float t) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, t));
    }
}
