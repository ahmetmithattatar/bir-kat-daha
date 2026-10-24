using UnityEngine;

namespace RiskLoop.Game.Art
{
    /// <summary>
    /// Kazılan tünel ve önündeki kaya yüzü (kodla, dosyasız). Konsept: oyuncu kendi açtığı tünelde ilerler,
    /// duvarlar KAZDIĞI MALZEMEDİR: önce toprak, sonra taş, sonra kristal, yeraltı gölü taşı, demir, fosil, ejder pulu,
    /// bazalt-lav, hiçlik. Bölgenin son katlarında bir sonraki malzeme yamalar halinde belirir (toprağın içinden taş çıkar).
    ///
    /// 3B hissi:
    ///   - Perspektif: ekranın her pikseli için bakış çizgisinin yuvarlak (elips) tünelin duvarına çarptığı derinlik (z)
    ///     hesaplanır. Ekranda merkezden uzaklık r ise z = R / r. Tünelin sonu (EndZ) kaya yüzüyle aynı boyda (640x600).
    ///   - Kabartma ışığı: her malzemenin bir yükseklik haritası var (çakıl, taş blok, kristal...). Kameraya bakan
    ///     yamaçlar aydınlık, çukurlar gölgede. Elimizdeki fenerin ışığı derinlikle kararır.
    ///   - Tünel duvarı hafif düzensiz (elle kazılmış gibi).
    /// İlerleme animasyonu (DigView.Emerge) bu görüntüyü büyüterek kameranın öne gittiği hissini verir.
    /// </summary>
    public static class MineArt
    {
        private const float EndZ = 3f;
        private const float Aspect = 16f / 9f;
        // Tünel kesiti: elips. Ekran-yüksekliği biriminde (1 = ekranın yarı yüksekliği). EndZ'de 640x600 duvar.
        private const float RadiusX = 0.593f * EndZ;
        private const float RadiusY = 0.556f * EndZ;
        private const int AroundWrap = 24;   // Tünelin çevresi kaç desen birimi (dikişsiz sarmak için tam sayı)
        private const float DepthScale = 2.2f; // Derinlik yönünde desen sıklığı: çevreyle aynı ölçek (desenler basık görünmesin)

        private static readonly Color LampLight = new Color(1f, 0.92f, 0.8f);   // Elimizdeki fenerin sıcak ışığı

        /// <summary>Kazılan malzeme türü. Sıra DepthTheme bölge sırasıyla aynı.</summary>
        public enum Material { Soil, Stone, Crystal, WetStone, Iron, Fossil, Scales, Basalt, Void }

        /// <summary>Bir noktadaki malzeme: renk, yükseklik (kabartma), kendi ışığı (parlayan cevher), parlaklık (ıslak).</summary>
        private struct Sample
        {
            public Color albedo;
            public float height;   // 0..~1.5
            public float glow;     // 0..1, karanlıkta da görünür
            public float gloss;    // 0..1, ıslak/metalik parlama
        }

        /// <summary>Malzeme ayarları: ana renk, ikinci renk (cevher/kristal/lav), tür.</summary>
        public struct Palette
        {
            public Material material;
            public Color baseColor;
            public Color accent;
        }

        // ---------------- Dışarıya açık kısım: kat için tünel ve kaya yüzü ----------------

        private const int TunnelWidth = 384, TunnelHeight = 216;   // Ekranda 5 kat büyür
        private const int FaceWidth = 128, FaceHeight = 120;       // Duvar 640x600

        // Bir görselin üretimi ~0.3 sn sürebilir. Sonraki katınki arka planda (ayrı iş parçacığında) önceden hesaplanır,
        // böylece ilerleme animasyonu takılmaz. Pikseller saf matematik (Unity nesnesi yok), bu yüzden iş parçacığında güvenli;
        // Texture/Sprite ise ana iş parçacığında oluşturulur.
        private static readonly System.Collections.Generic.Dictionary<string, System.Threading.Tasks.Task<Color32[]>> Pending =
            new System.Collections.Generic.Dictionary<string, System.Threading.Tasks.Task<Color32[]>>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Pending.Clear();   // Domain Reload kapalı: her Play'de temiz başla

        /// <summary>Bu katın tüneli (malzeme, geçiş yamaları dahil).</summary>
        public static Sprite TunnelFor(int step)
        {
            Describe(step, out string tunnelKey, out _, out int tunnelSeed, out _, out Palette main, out Palette next, out float mix);
            return Get(tunnelKey, TunnelWidth, TunnelHeight, () => TunnelPixels(tunnelSeed, main, next, mix));
        }

        /// <summary>Bu katta önümüzdeki kazılacak kaya yüzü.</summary>
        public static Sprite FaceFor(int step)
        {
            Describe(step, out _, out string faceKey, out _, out int faceSeed, out Palette main, out Palette next, out float mix);
            return Get(faceKey, FaceWidth, FaceHeight, () => FacePixels(faceSeed, main, next, mix));
        }

        /// <summary>Bu katın görsellerini arka planda hazırlamaya başla (henüz yoksa).</summary>
        public static void Prewarm(int step)
        {
            Describe(step, out string tunnelKey, out string faceKey, out int tunnelSeed, out int faceSeed,
                     out Palette main, out Palette next, out float mix);
            StartPending(tunnelKey, () => TunnelPixels(tunnelSeed, main, next, mix));
            StartPending(faceKey, () => FacePixels(faceSeed, main, next, mix));
        }

        /// <summary>Kat için önbellek anahtarları, seed'ler ve malzemeler. Aynı bölge + geçiş oranı = aynı tünel.</summary>
        private static void Describe(int step, out string tunnelKey, out string faceKey, out int tunnelSeed, out int faceSeed,
                                     out Palette main, out Palette next, out float mix)
        {
            int zone = DepthTheme.ZoneIndex(step);
            main = DepthTheme.Palette(step);
            next = DepthTheme.NextPalette(step);
            mix = DepthTheme.TransitionMix(step);
            int mixKey = Mathf.RoundToInt(mix * 4f);
            int variant = step % 4;   // Her bölgede 4 farklı kaya yüzü dönüşümlü
            tunnelKey = $"tunnel_{zone}_{mixKey}";
            faceKey = $"face_{zone}_{mixKey}_{variant}";
            tunnelSeed = zone * 31 + 5;
            faceSeed = zone * 100 + variant;
        }

        private static void StartPending(string key, System.Func<Color32[]> compute)
        {
            if (Pending.ContainsKey(key) || PixelArt.TryGetCached(key, out _)) return;
            Pending[key] = System.Threading.Tasks.Task.Run(compute);
        }

        /// <summary>Önbellekte varsa onu, arka planda hazırlanıyorsa onu (bitmesini bekler), yoksa hemen üretir.</summary>
        private static Sprite Get(string key, int width, int height, System.Func<Color32[]> compute)
        {
            if (PixelArt.TryGetCached(key, out Sprite cached)) return cached;
            Color32[] pixels;
            if (Pending.TryGetValue(key, out var task))
            {
                Pending.Remove(key);
                pixels = task.Result;   // Genelde çoktan bitmiştir
            }
            else
            {
                pixels = compute();
            }
            return PixelArt.Finish(key, PixelArt.NewTexture(width, height), pixels);
        }

        // ---------------- Tünel ----------------

        /// <summary>
        /// Tam ekran tünel (384x216, ekranda 5 kat büyür). next + mix: bölge geçişinde sonraki malzeme
        /// yamalarının oranı (0 = hiç, 0.75 = çoğu).
        /// </summary>
        private static Color32[] TunnelPixels(int seed, Palette main, Palette next, float mix)
        {
            const int width = TunnelWidth, height = TunnelHeight;
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width * 2f - 1f;   // -1..1
                    float v = (y + 0.5f) / height * 2f - 1f;  // -1..1, yukarı pozitif
                    Color c = TunnelPixel(u * Aspect, v, x, y, seed, main, next, mix);
                    c.a = 1f;
                    pixels[y * width + x] = c;
                }
            }
            return pixels;
        }

        private static Color TunnelPixel(float sx, float sy, int px, int py, int seed, Palette main, Palette next, float mix)
        {
            // Elips kesitte normalize konum ve açı. Açı dikişi tavanın tam ortasında (θ = ±π), desenler orada sarar.
            float ex = sx / RadiusX, ey = sy / RadiusY;
            float r = Mathf.Sqrt(ex * ex + ey * ey) + 1e-5f;
            float theta = Mathf.Atan2(ex, -ey);
            float around = theta / (2f * Mathf.PI) + 0.5f;   // 0..1
            float rim = RimRadius(around);
            float z = rim / r;
            float grit = (Hash(px, py, seed + 99) - 0.5f) * 0.05f;

            // Tünelin sonu: kaya yüzünün arkası (kenarından görünen oyuk), zifiri karanlık
            if (z >= EndZ) return main.baseColor * (0.07f + grit);

            float a = around * AroundWrap;
            float b = z * DepthScale;
            float worldY = -Mathf.Cos(theta) * rim;   // Duvardaki gerçek yükseklik (katmanlar yatay dursun diye)

            Sample s = Blend(a, b, worldY, AroundWrap, seed, main, next, mix);
            // Kabartma: kameraya bakan yamaç (derinlikle yüksekliği azalan) aydınlık
            const float e = 0.06f;
            float hNear = Blend(a, b - e, worldY, AroundWrap, seed, main, next, mix).height;
            float hFar = Blend(a, b + e, worldY, AroundWrap, seed, main, next, mix).height;
            float facing = Mathf.Clamp((hFar - hNear) / (2f * e) * -0.18f, -0.55f, 0.7f);

            float falloff = 1.4f / (z * z * 0.6f + 0.8f);                // Fener ışığı derinlikle kararır
            float shade = falloff * (0.72f + 0.28f * Mathf.Clamp01(s.height)) * (1f + facing);
            // Uzakta ince desen titreşmesin: ayrıntı ortalama renge karışır
            s.albedo = Color.Lerp(s.albedo, main.baseColor * 0.85f, 0.5f * Smooth(2.1f, EndZ, z));
            Color c = s.albedo * shade;
            c.r *= LampLight.r; c.g *= LampLight.g; c.b *= LampLight.b;
            if (s.gloss > 0f && facing > 0f) c += Color.white * (s.gloss * facing * facing * 0.35f * falloff);   // Islak parıltı
            if (s.glow > 0f) c = Color.Lerp(c, s.albedo * 1.25f, s.glow * Mathf.Lerp(1f, 0.6f, z / EndZ));
            return c + new Color(grit, grit, grit);
        }

        /// <summary>Tünel ağzının düzensizliği (elle kazılmış gibi). around 0..1, dikişsiz.</summary>
        private static float RimRadius(float around)
        {
            return 1f + 0.10f * (Noise(around * 7f, 0.5f, 901, 7) - 0.5f) + 0.03f * (Noise(around * 23f, 2.5f, 902, 23) - 0.5f);
        }

        // ---------------- Kaya yüzü (kazılan duvar) ----------------

        /// <summary>
        /// Önümüzdeki kazılan yüz (128x120, duvar 640x600). Tünelle aynı malzeme, yandan gelen ışıkla kabartmalı.
        /// Kenarı tünelin sonuyla birebir aynı düzensiz elips: dışı saydam.
        /// </summary>
        private static Color32[] FacePixels(int seed, Palette main, Palette next, float mix)
        {
            const int width = FaceWidth, height = FaceHeight;
            var pixels = new Color32[width * height];
            const float e = 0.05f;
            const float scale = 3.2f;   // Yüzde desen büyüklüğü (tünelin uzak ucuyla uyumlu)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = (x + 0.5f) / width * 2f - 1f;
                    float ny = (y + 0.5f) / height * 2f - 1f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float around = Mathf.Atan2(nx, -ny) / (2f * Mathf.PI) + 0.5f;
                    float rim = RimRadius(around) * 0.985f;
                    if (r > rim)
                    {
                        pixels[y * width + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float a = nx * scale + 40f, b = ny * scale + 40f;
                    Sample s = Blend(a, b, ny, 0, seed, main, next, mix);
                    float hx1 = Blend(a - e, b, ny, 0, seed, main, next, mix).height;
                    float hx2 = Blend(a + e, b, ny, 0, seed, main, next, mix).height;
                    float hy1 = Blend(a, b - e, ny - e / scale, 0, seed, main, next, mix).height;
                    float hy2 = Blend(a, b + e, ny + e / scale, 0, seed, main, next, mix).height;
                    float dhdx = (hx2 - hx1) / (2f * e);
                    float dhdy = (hy2 - hy1) / (2f * e);

                    // Yükseklik haritasının yüzey normali ile üst-sol-önden gelen ışık
                    const float bump = 0.55f;
                    var normal = new Vector3(-dhdx * bump, -dhdy * bump, 1f).normalized;
                    float diffuse = Mathf.Max(0f, Vector3.Dot(normal, new Vector3(-0.42f, 0.5f, 0.76f)));

                    float center = 1.08f - 0.5f * r * r;                       // Fenerin ortası parlak
                    float rimShadow = 1f - 0.55f * Smooth(rim * 0.78f, rim, r); // Kenara doğru oyuğa giren gölge
                    float shade = (0.25f + 0.85f * diffuse) * center * rimShadow * (0.75f + 0.25f * Mathf.Clamp01(s.height));
                    Color c = s.albedo * shade;
                    c.r *= LampLight.r; c.g *= LampLight.g; c.b *= LampLight.b;
                    if (s.gloss > 0f) c += Color.white * (s.gloss * Mathf.Pow(diffuse, 8f) * 0.5f);
                    if (s.glow > 0f) c = Color.Lerp(c, s.albedo * 1.25f, s.glow);   // Parlayan malzeme kendi ışığıyla görünür
                    float grit = (Hash(x, y, seed + 31) - 0.5f) * 0.05f;
                    c += new Color(grit, grit, grit);
                    c.a = 1f;
                    pixels[y * width + x] = c;
                }
            }
            return pixels;
        }

        // ---------------- Malzemeler ----------------

        /// <summary>Ana malzeme + (geçişte) sonraki malzemenin büyüyen yamaları.</summary>
        private static Sample Blend(float a, float b, float worldY, int wrap, int seed, Palette main, Palette next, float mix)
        {
            Sample s = Mat(main, a, b, worldY, wrap, seed);
            if (mix <= 0f) return s;
            float patch = FbmF(a, b, 0.5f, wrap, seed + 77, 2);
            float k = Smooth(mix + 0.04f, mix - 0.04f, patch);   // patch < mix olan yerler sonraki malzeme
            if (k <= 0f) return s;
            Sample n = Mat(next, a, b, worldY, wrap, seed + 1000);
            n.height += 0.25f;   // Yeni malzeme yamaları biraz kabarık (toprağın içinden çıkan taş)
            return new Sample
            {
                albedo = Color.Lerp(s.albedo, n.albedo, k),
                height = Mathf.Lerp(s.height, n.height, k),
                glow = Mathf.Lerp(s.glow, n.glow, k),
                gloss = Mathf.Lerp(s.gloss, n.gloss, k),
            };
        }

        private static Sample Mat(Palette p, float a, float b, float worldY, int wrap, int seed)
        {
            switch (p.material)
            {
                case Material.Soil: return Soil(p, a, b, wrap, seed);
                case Material.Stone: return Stone(p, a, b, wrap, seed, 0.55f);
                case Material.Crystal: return Crystal(p, a, b, wrap, seed);
                case Material.WetStone: return WetStone(p, a, b, worldY, wrap, seed);
                case Material.Iron: return Iron(p, a, b, wrap, seed);
                case Material.Fossil: return Fossil(p, a, b, worldY, wrap, seed);
                case Material.Scales: return Scales(p, a, b, wrap, seed);
                case Material.Basalt: return Basalt(p, a, b, wrap, seed);
                default: return Void(p, a, b, wrap, seed);
            }
        }

        /// <summary>Toprak: ince taneli, topaklı; aralarda çakıl taşları ve kökler.</summary>
        private static Sample Soil(Palette p, float a, float b, int wrap, int seed)
        {
            // Topaklar: kaba kabartma + koyu-açık lekeler (nemli ve kuru toprak)
            float n = FbmF(a, b, 1.5f, wrap, seed, 4);
            float clod = FbmF(a, b, 4f, wrap, seed + 3, 2);
            float damp = FbmF(a, b, 0.5f, wrap, seed + 4, 2);
            var s = new Sample
            {
                albedo = p.baseColor * (0.62f + 0.5f * n + 0.15f * (clod - 0.5f)) * Mathf.Lerp(0.8f, 1.1f, damp),
                height = n * 0.7f + clod * 0.35f,
            };

            // Çakıl: seyrek, farklı boylarda yuvarlak taşlar
            float d = VoronoiF(a, b, 2f, wrap, seed + 5, out _, out float id, out _);
            float size = 0.12f + 0.22f * Mathf.Repeat(id * 7.3f, 1f);
            if (id > 0.78f && d < size)
            {
                float t = 1f - d / size;
                s.height += 0.45f * t * t * (3f - 2f * t);
                s.albedo = Color.Lerp(p.baseColor, p.accent, 0.7f) * (0.75f + 0.4f * Mathf.Repeat(id * 3.1f, 1f));
            }
            // Kökler: ince, kıvrımlı koyu çizgiler
            if (Mathf.Abs(Noise(S(a, wrap, 1.5f), b * 0.75f, seed + 50, R(wrap, 1.5f)) - 0.5f) < 0.022f &&
                NoiseF(a, b, 0.5f, wrap, seed + 51) > 0.55f)
            {
                s.albedo = new Color(0.22f, 0.15f, 0.1f);
                s.height += 0.3f;
            }
            return s;
        }

        /// <summary>Taş: kırık bloklar, aralarında koyu çatlaklar. bulge: blokların kabarıklığı.</summary>
        private static Sample Stone(Palette p, float a, float b, int wrap, int seed, float bulge)
        {
            VoronoiF(a, b, 1.5f, wrap, seed + 3, out float edge, out float id, out _);
            float n = FbmF(a, b, 4f, wrap, seed + 4, 2);
            var s = new Sample
            {
                albedo = p.baseColor * (0.78f + 0.3f * (id - 0.5f) + 0.25f * (n - 0.5f) + 0.12f),
                height = Smooth(0f, 0.35f, edge) * bulge + n * 0.35f,
            };
            if (edge < 0.05f) s.albedo *= 0.35f;   // Çatlak
            return s;
        }

        /// <summary>Kristal: koyu taş, içinden fışkıran parlak kristal kümeleri (iki tonlu yüzeyler).</summary>
        private static Sample Crystal(Palette p, float a, float b, int wrap, int seed)
        {
            Palette dark = p;
            dark.baseColor = p.baseColor * 0.6f;
            Sample s = Stone(dark, a, b, wrap, seed, 0.4f);
            float d = VoronoiF(a, b, 2.5f, wrap, seed + 9, out _, out float id, out Vector2 rel);
            float diamond = Mathf.Abs(rel.x) * 1.6f + Mathf.Abs(rel.y);   // Baklava (kristal kesiti)
            float crystalSize = 0.2f + 0.3f * Mathf.Repeat(id * 9.7f, 1f);   // Farklı boylarda kristaller
            if (id > 0.8f && diamond < crystalSize)
            {
                float facet = rel.x > 0f ? 1.15f : 0.7f;              // Sağ ve sol yüz farklı aydınlık
                if (rel.y > 0.12f) facet += 0.25f;                     // Uç kısım parlak
                s.albedo = p.accent * facet;
                s.height = 0.8f + crystalSize - diamond;
                s.glow = 0.28f;
                s.gloss = 0.6f;
            }
            return s;
        }

        /// <summary>Islak göl taşı: yatay pürüzsüz katmanlar, ıslak parıltı.</summary>
        private static Sample WetStone(Palette p, float a, float b, float worldY, int wrap, int seed)
        {
            float n = FbmF(a, b, 1.5f, wrap, seed + 12, 3);
            float band = Mathf.Sin(worldY * 9f + n * 4f);
            var s = new Sample
            {
                albedo = p.baseColor * (0.8f + 0.18f * band + 0.2f * (n - 0.5f)),
                height = 0.4f + 0.25f * band + n * 0.3f + FbmF(a, b, 3f, wrap, seed + 13, 2) * 0.5f,
                gloss = 0.9f,
            };
            if (Mathf.Abs(band) < 0.06f) s.albedo = Color.Lerp(s.albedo, p.accent, 0.45f);   // İnce mineral çizgi
            return s;
        }

        /// <summary>Demir: taş bloklar, pas lekeleri, metal pırıltıları.</summary>
        private static Sample Iron(Palette p, float a, float b, int wrap, int seed)
        {
            Sample s = Stone(p, a, b, wrap, seed, 0.5f);
            float rust = FbmF(a, b, 1.5f, wrap, seed + 60, 3);
            if (rust > 0.56f) s.albedo = Color.Lerp(s.albedo, p.accent * 0.7f, 0.55f * Smooth(0.56f, 0.7f, rust));
            if (Hash(Wrap(Mathf.FloorToInt(a * 10f), wrap * 10), Mathf.FloorToInt(b * 10f), seed + 61) > 0.985f)
            {
                s.albedo = new Color(0.85f, 0.85f, 0.9f);
                s.gloss = 1f;
                s.height += 0.3f;
            }
            return s;
        }

        /// <summary>Fosil: açık renkli katmanlı kireçtaşı, içinde kemik parçaları ve deniz kabukları.</summary>
        private static Sample Fossil(Palette p, float a, float b, float worldY, int wrap, int seed)
        {
            float n = FbmF(a, b, 2f, wrap, seed + 70, 3);
            float band = Mathf.Sin(worldY * 12f + n * 3f);
            var s = new Sample { albedo = p.baseColor * (0.85f + 0.1f * band + 0.2f * (n - 0.5f)), height = n * 0.5f + 0.1f * band };

            float d = VoronoiF(a, b, 2f, wrap, seed + 71, out _, out float id, out Vector2 rel);
            if (id > 0.8f)
            {
                // Kemik: hücre kimliğine göre döndürülmüş ince uzun elips
                float ang = id * 31f;
                float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
                float lx = rel.x * ca - rel.y * sa, ly = rel.x * sa + rel.y * ca;
                float q = lx * lx / 0.11f + ly * ly / 0.006f;
                if (q < 1f) { s.albedo = p.accent; s.height += 0.5f * (1f - q); }
            }
            else if (id > 0.68f && Mathf.Abs(d - 0.16f) < 0.035f)
            {
                s.albedo = Color.Lerp(s.albedo, p.accent, 0.7f);   // Kabuk halkası
                s.height += 0.3f;
            }
            return s;
        }

        /// <summary>Ejder pulu: üst üste binen kabarık pullar, aralarda zümrüt pırıltısı.</summary>
        private static Sample Scales(Palette p, float a, float b, int wrap, int seed)
        {
            float x = a * 3f, y = b * 3f;
            int col = Mathf.FloorToInt(x);
            if ((col & 1) == 1) y += 0.5f;   // Pulları yarım kaydır (kiremit gibi)
            float fx = x - col - 0.5f, fy = y - Mathf.Floor(y) - 0.5f;
            float d2 = fx * fx + fy * fy * 1.3f;
            float scale = Mathf.Clamp01(1f - d2 * 2.6f);
            float n = NoiseF(a, b, 6f, wrap, seed + 80);
            var s = new Sample
            {
                albedo = p.baseColor * (0.5f + 0.65f * scale + 0.15f * (n - 0.5f)),
                height = scale,
                gloss = 0.5f,
            };
            if (scale < 0.08f && Hash(Wrap(col, wrap * 3), Mathf.FloorToInt(y), seed + 81) > 0.7f)
            {
                s.albedo = p.accent;
                s.glow = 0.3f;
            }
            return s;
        }

        /// <summary>Bazalt: kara bloklar, aralarındaki çatlaklardan lav parlar.</summary>
        private static Sample Basalt(Palette p, float a, float b, int wrap, int seed)
        {
            VoronoiF(a, b, 1.3f, wrap, seed + 90, out float edge, out float id, out _);
            float n = FbmF(a, b, 4f, wrap, seed + 91, 2);
            var s = new Sample
            {
                albedo = p.baseColor * (0.8f + 0.4f * id + 0.3f * (n - 0.5f)),
                height = Smooth(0f, 0.3f, edge) * 0.7f + n * 0.3f,
            };
            if (edge < 0.08f)
            {
                float hot = 1f - edge / 0.08f;
                s.albedo = Color.Lerp(new Color(0.55f, 0.1f, 0.03f), p.accent, hot);
                s.glow = 0.4f + 0.6f * hot;
                s.height = 0f;
            }
            return s;
        }

        /// <summary>Hiçlik: neredeyse siyah mor kaya, içinde yıldız gibi parlayan zerreler.</summary>
        private static Sample Void(Palette p, float a, float b, int wrap, int seed)
        {
            float n = FbmF(a, b, 1.5f, wrap, seed + 100, 3);
            var s = new Sample { albedo = p.baseColor * (0.45f + 0.6f * n), height = n * 0.8f };
            float d = VoronoiF(a, b, 5f, wrap, seed + 101, out _, out float id, out _);
            if (id > 0.82f && d < 0.12f)
            {
                s.albedo = Color.Lerp(p.accent, Color.white, 0.5f);
                s.glow = 1f - d / 0.12f;
            }
            return s;
        }

        // ---------------- Gürültü yardımcıları ----------------
        // wrap > 0: x ekseni o kadar birimde bir tekrar eder (tünelin çevresi dikişsiz sarar). 0 = tekrar yok.

        /// <summary>Sıklık f'de sarma birimi (çevre AroundWrap * f birim; tam sayıya yuvarlanır).</summary>
        private static int R(int wrap, float f) => wrap > 0 ? Mathf.Max(1, Mathf.RoundToInt(wrap * f)) : 0;

        private static int Wrap(int i, int wrap) => wrap > 0 ? ((i % wrap) + wrap) % wrap : i;

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

        /// <summary>Yumuşak değer gürültüsü (x ekseni wrap'te sarar).</summary>
        private static float Noise(float x, float y, int seed, int wrap)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float tx = x - x0, ty = y - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            int xa = Wrap(x0, wrap), xb = Wrap(x0 + 1, wrap);
            float top = Mathf.Lerp(Hash(xa, y0, seed), Hash(xb, y0, seed), tx);
            float bottom = Mathf.Lerp(Hash(xa, y0 + 1, seed), Hash(xb, y0 + 1, seed), tx);
            return Mathf.Lerp(top, bottom, ty);
        }

        /// <summary>a eksenini f sıklığına ölçekler; sarmalı yüzeyde periyot tam sayı olsun diye f hafifçe yuvarlanır.</summary>
        private static float S(float a, int wrap, float f) => wrap > 0 ? a * R(wrap, f) / wrap : a * f;

        // Sıklık f'de (a, b) yüzey koordinatıyla çağrılan kısayollar: sarma hesabını tek yerde yaparlar
        private static float NoiseF(float a, float b, float f, int wrap, int seed) => Noise(S(a, wrap, f), b * f, seed, R(wrap, f));
        private static float FbmF(float a, float b, float f, int wrap, int seed, int octaves) => Fbm(S(a, wrap, f), b * f, R(wrap, f), seed, octaves);
        private static float VoronoiF(float a, float b, float f, int wrap, int seed, out float edge, out float id, out Vector2 rel) =>
            Voronoi(S(a, wrap, f), b * f, R(wrap, f), seed, out edge, out id, out rel);

        /// <summary>Katmanlı gürültü (fbm): kaba şekil + yarı boyda + çeyrek boyda ayrıntı.</summary>
        private static float Fbm(float x, float y, int wrap, int seed, int octaves)
        {
            float sum = 0f, amp = 0.5f, total = 0f;
            int freq = 1;
            for (int o = 0; o < octaves; o++)
            {
                sum += Noise(x * freq, y * freq, seed + o * 13, wrap * freq) * amp;
                total += amp;
                amp *= 0.5f;
                freq *= 2;
            }
            return sum / total;
        }

        /// <summary>
        /// Hücre gürültüsü (Voronoi): her hücrede rastgele bir nokta. Dönüş: en yakın noktaya uzaklık.
        /// edge: iki hücrenin sınırına yakınlık (küçük = sınırda), id: hücreye özgü 0..1, rel: noktaya göre konum.
        /// </summary>
        private static float Voronoi(float x, float y, int wrap, int seed, out float edge, out float id, out Vector2 rel)
        {
            int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
            float d1 = float.MaxValue, d2 = float.MaxValue;
            id = 0f;
            rel = Vector2.zero;
            for (int j = -1; j <= 1; j++)
            {
                for (int i = -1; i <= 1; i++)
                {
                    int gx = cx + i, gy = cy + j;
                    int hx = Wrap(gx, wrap);   // Nokta konumu sarmasız, rastgelelik sarmalı: dikişsiz
                    float ppx = gx + Hash(hx, gy, seed);
                    float ppy = gy + Hash(hx, gy, seed + 1);
                    float dx = x - ppx, dy = y - ppy;
                    float d = dx * dx + dy * dy;
                    if (d < d1)
                    {
                        d2 = d1;
                        d1 = d;
                        id = Hash(hx, gy, seed + 2);
                        rel = new Vector2(dx, dy);
                    }
                    else if (d < d2) d2 = d;
                }
            }
            d1 = Mathf.Sqrt(d1);
            edge = Mathf.Sqrt(d2) - d1;
            return d1;
        }

        private static float Smooth(float from, float to, float t) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, t));
    }
}
