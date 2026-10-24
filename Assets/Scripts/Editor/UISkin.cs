using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.EditorTools
{
    /// <summary>
    /// Arayüz kaplaması (UI skin): paneller, butonlar, yuvalar, çubuklar. Oyunun pixel art diliyle uyumlu,
    /// metal çerçeveli, kabartmalı görünüm. Görseller kodla çizilir ve GERÇEK PNG dosyası olarak kaydedilir
    /// (Assets/Art/UI). Böylece sahnede kalıcıdır ve ileride bir sanatçı aynı adla kendi çizimini koyabilir.
    ///
    /// 9-dilim (sliced): her görselin kenarları sabit kalır, ortası esner; tek küçük PNG her boyda panel olur.
    /// Pikseller 3 kat büyük görünür (spritePixelsPerUnit = 100/3): retro, keskin kenarlar.
    ///
    /// Gri tonlu stiller (buton, çerçeve, yuva, çubuk) Image rengiyle boyanır: aynı buton turuncu da olur yeşil de,
    /// eşya çerçevesi nadirlik renginde. Renkli stiller (panel, üst bar, arka plan) beyazla kullanılır.
    ///
    /// Menü: RiskLoop > UI Kaplamasını Yeniden Üret (PNG'leri baştan çizer; sahne kurucu sadece eksikleri üretir).
    /// </summary>
    public static class UISkin
    {
        public enum Style
        {
            Button, ButtonHover, ButtonPressed, ButtonDisabled,
            Panel, PanelHud, PanelOrnate, Slot, Frame, BarBack, BarFill, TopBar, Background
        }

        private const string Folder = "Assets/Art/UI";
        private const string ShadowMaterialPath = Folder + "/TextShadow.mat";
        private const float PixelScale = 3f;   // Bir doku pikseli ekranda kaç birim

        /// <summary>Gri tonlu (Image rengiyle boyanan) stiller. Gövdeleri ~0.8 parlaklıkta: renk biraz yükseltilerek verilir.</summary>
        public static bool IsTinted(Style s) =>
            s == Style.Button || s == Style.ButtonHover || s == Style.ButtonPressed || s == Style.ButtonDisabled ||
            s == Style.Slot || s == Style.Frame || s == Style.BarBack || s == Style.BarFill;

        [MenuItem("RiskLoop/UI Kaplamasını Yeniden Üret")]
        public static void RegenerateMenu()
        {
            Generate(force: true);
            EditorUtility.DisplayDialog("UI kaplaması", "Görseller yeniden üretildi (Assets/Art/UI).\nSahneyi yeniden kurmana gerek yok.", "Tamam");
        }

        /// <summary>Eksik görselleri üretir (force: hepsini baştan çizer).</summary>
        public static void Generate(bool force)
        {
            Directory.CreateDirectory(Folder);
            foreach (Style s in System.Enum.GetValues(typeof(Style)))
            {
                string path = PathOf(s);
                if (!force && File.Exists(path)) continue;
                Draw(s, out Color[] pixels, out int size, out int border, out float scale);
                WritePng(path, pixels, size, border, scale, s == Style.Background);
            }
            if (force || !File.Exists(ShadowMaterialPath)) CreateShadowMaterial();
            AssetDatabase.Refresh();
        }

        public static Sprite Get(Style s) => AssetDatabase.LoadAssetAtPath<Sprite>(PathOf(s));

        /// <summary>Image'a stili uygular. Gri tonlu stillerde renk, gövde parlaklığını telafi etmek için biraz yükseltilir.</summary>
        public static void Apply(Image image, Style s, Color color)
        {
            image.sprite = Get(s);
            image.type = s == Style.Background ? Image.Type.Tiled : Image.Type.Sliced;
            image.fillCenter = true;
            image.color = IsTinted(s) ? Brighten(color) : color;
        }

        /// <summary>Butona kabartmalı görünüm + basılı/üstünde/pasif görselleri (sprite değişimi).</summary>
        public static void ApplyButton(Button button, Image image, Color color)
        {
            Apply(image, Style.Button, color);
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = Get(Style.ButtonHover),
                selectedSprite = Get(Style.Button),
                pressedSprite = Get(Style.ButtonPressed),
                disabledSprite = Get(Style.ButtonDisabled),
            };
        }

        /// <summary>Yazıya koyu gölge (oyun sahnesinin ve flaşların üstünde de okunsun).</summary>
        public static Material ShadowMaterial => AssetDatabase.LoadAssetAtPath<Material>(ShadowMaterialPath);

        /// <summary>Gölgeli malzemeyi uygular (malzeme yoksa yazı olduğu gibi kalır).</summary>
        public static void ApplyTextShadow(TMP_Text text)
        {
            Material mat = ShadowMaterial;
            if (mat != null) text.fontSharedMaterial = mat;
        }

        private static Color Brighten(Color c) =>
            new Color(Mathf.Min(1f, c.r * 1.22f), Mathf.Min(1f, c.g * 1.22f), Mathf.Min(1f, c.b * 1.22f), c.a);

        private static string PathOf(Style s) => $"{Folder}/{s}.png";

        // ---------------- Çizim ----------------

        private static void Draw(Style s, out Color[] px, out int size, out int border, out float scale)
        {
            scale = PixelScale;
            switch (s)
            {
                case Style.Button: size = 24; border = 8; px = Button(size, 0f, false, false); break;
                case Style.ButtonHover: size = 24; border = 8; px = Button(size, 0.07f, false, false); break;
                case Style.ButtonPressed: size = 24; border = 8; px = Button(size, 0f, true, false); break;
                case Style.ButtonDisabled: size = 24; border = 8; px = Button(size, 0f, false, true); break;
                case Style.Panel: size = 48; border = 16; px = Panel(size, 0.97f, false); break;
                case Style.PanelHud: size = 48; border = 16; px = Panel(size, 0.72f, false); break;
                case Style.PanelOrnate: size = 64; border = 22; px = Panel(size, 0.97f, true); break;
                case Style.Slot: size = 24; border = 8; px = Slot(size); break;
                case Style.Frame: size = 24; border = 8; px = Frame(size); break;
                case Style.BarBack: size = 12; border = 4; scale = 2f; px = BarBack(size); break;
                case Style.BarFill: size = 12; border = 4; scale = 2f; px = BarFill(size); break;
                case Style.TopBar: size = 32; border = 12; px = TopBar(size); break;
                default: size = 64; border = 0; px = Background(size); break;
            }
        }

        private static readonly System.Random Rng = new System.Random(1234);
        private static float Grain(float amount) => ((float)Rng.NextDouble() - 0.5f) * amount;
        private static Color Gray(float v, float a = 1f) => new Color(v, v, v, a);

        /// <summary>Köşesi kesik mi (yuvarlatılmış piksel köşe). cut: kesilen piksel sayısı.</summary>
        private static bool CornerCut(int x, int y, int size, int cut)
        {
            int dx = Mathf.Min(x, size - 1 - x), dy = Mathf.Min(y, size - 1 - y);
            return dx + dy < cut;
        }

        /// <summary>Kabartmalı buton: koyu kontur, üstte ışık, gövdede gradyan, altta kalın gölge (yükseltilmiş taş tuş).</summary>
        private static Color[] Button(int n, float bright, bool pressed, bool disabled)
        {
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    Color c;
                    if (CornerCut(x, y, n, 2)) c = Color.clear;
                    else if (x == 0 || y == 0 || x == n - 1 || y == n - 1 || CornerCut(x, y, n, 3)) c = Gray(0.1f);   // Kontur
                    else if (disabled)
                    {
                        c = Gray(y <= 2 ? 0.32f : 0.5f);
                    }
                    else if (pressed)
                    {
                        // İçe basılmış: üstte iç gölge, alt dudak yok, gövde koyu
                        if (y >= n - 3) c = Gray(0.42f);
                        else if (y <= 1) c = Gray(0.55f);
                        else c = Gray(y >= n - 8 ? 0.6f : 0.68f);
                    }
                    else
                    {
                        float body = BodyShade(y, n) + bright;   // Gradyan sadece kenar dilimlerinde: orta esneyince bant oluşmaz
                        if (y <= 3) c = Gray(0.36f + 0.04f * y);              // Alt dudak (kalınlık = kabartma)
                        else if (y == n - 2) c = Gray(1f);                     // Üst parlama
                        else if (y == n - 3) c = Gray(Mathf.Min(1f, body + 0.06f));
                        else if (x == 1) c = Gray(Mathf.Min(1f, body + 0.05f)); // Sol kenar ışık
                        else if (x == n - 2) c = Gray(body - 0.1f);             // Sağ kenar gölge
                        else c = Gray(body);
                        c.r = Mathf.Min(1f, c.r); c.g = Mathf.Min(1f, c.g); c.b = Mathf.Min(1f, c.b);
                    }
                    px[y * n + x] = c;
                }
            }
            return px;
        }

        /// <summary>Buton gövdesinin satır parlaklığı: alt dilimde koyudan, üst dilimde açığa; ortadaki esneyen kısım düz.</summary>
        private static float BodyShade(int y, int n)
        {
            const int slice = 8;   // Buton kenar dilimi (border) ile aynı
            if (y < slice) return Mathf.Lerp(0.66f, 0.76f, (y - 4f) / 3f);
            if (y >= n - slice) return Mathf.Lerp(0.82f, 0.92f, (y - (n - slice)) / (slice - 3f));
            return 0.79f;
        }

        /// <summary>
        /// Panel: dış kontur, 4 piksel dövme metal çerçeve (üst-sol ışık, alt-sağ gölge), ince altın iç çizgi,
        /// köşelerde perçin, koyu iç zemin (fillAlpha: HUD için yarı saydam). ornate: kalın çerçeve, altın köşe süsleri, yakut.
        /// </summary>
        private static Color[] Panel(int n, float fillAlpha, bool ornate)
        {
            var px = new Color[n * n];
            int frame = ornate ? 6 : 4;
            var metalLight = new Color(0.46f, 0.44f, 0.48f);
            var metal = new Color(0.27f, 0.26f, 0.29f);
            var metalDark = new Color(0.14f, 0.13f, 0.15f);
            var gold = new Color(0.82f, 0.62f, 0.26f);
            var goldDark = new Color(0.5f, 0.35f, 0.12f);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(n - 1 - x, n - 1 - y));
                    Color c;
                    if (CornerCut(x, y, n, 2)) c = Color.clear;
                    else if (edge == 0 || CornerCut(x, y, n, 3)) c = new Color(0.02f, 0.02f, 0.03f);
                    else if (edge <= frame)
                    {
                        // Metal: ışık üst-soldan. Pikselin en yakın olduğu kenar üst ya da solsa aydınlık, alt ya da sağsa gölge
                        int top = n - 1 - y, bottom = y, left = x, right = n - 1 - x;
                        bool lit = Mathf.Min(top, left) <= Mathf.Min(bottom, right);
                        bool middle = edge >= 2 && edge <= frame - 1;
                        c = middle ? (lit ? Color.Lerp(metal, metalLight, 0.4f) : metal) : (lit ? metalLight : metalDark);
                        if (edge == 1) c *= lit ? 1.15f : 0.9f;
                        c += Gray(Grain(0.04f), 0f);
                        c.a = 1f;
                    }
                    else if (edge == frame + 1)
                    {
                        bool lower = Mathf.Min(y, n - 1 - x) < Mathf.Min(n - 1 - y, x);   // Alt ya da sağ kenar
                        c = lower ? goldDark : gold;   // Altın iç çizgi (ışık alan kenarlar parlak)
                    }
                    else if (edge == frame + 2)
                    {
                        c = new Color(0.02f, 0.02f, 0.03f, Mathf.Max(fillAlpha, 0.85f));   // İç gölge
                    }
                    else
                    {
                        const float v = 0.08f;   // Düz iç zemin (esneyince leke olmasın)
                        c = new Color(v, v * 0.95f, v * 1.15f, fillAlpha);
                    }
                    px[y * n + x] = c;
                }
            }

            // Perçinler: çerçevenin ortasında, dört köşede
            int r = frame / 2 + 1;
            foreach (var (cx, cy) in new[] { (r, r), (n - 1 - r, r), (r, n - 1 - r), (n - 1 - r, n - 1 - r) })
            {
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        if (dx * dx + dy * dy <= 1) px[(cy + dy) * n + cx + dx] = dx + dy < 0 ? gold * 1.2f : goldDark;
                px[(cy + 1) * n + cx - 1] = new Color(1f, 0.92f, 0.6f);   // Parlama
            }

            if (ornate)
            {
                // Altın köşe süsleri (L biçimli) ve yakut
                int len = 9;
                foreach (var (sx, sy) in new[] { (1, 1), (-1, 1), (1, -1), (-1, -1) })
                {
                    int ox = sx > 0 ? frame + 3 : n - 1 - frame - 3;
                    int oy = sy > 0 ? frame + 3 : n - 1 - frame - 3;
                    for (int i = 0; i < len; i++)
                    {
                        px[oy * n + ox + sx * i] = gold;
                        px[(oy + sy * i) * n + ox] = gold;
                    }
                    px[(oy + sy) * n + ox + sx] = new Color(0.85f, 0.15f, 0.2f);
                    px[(oy + sy * 2) * n + ox + sx * 2] = new Color(0.55f, 0.08f, 0.12f);
                }
            }
            return px;
        }

        /// <summary>Oyuk yuva: üst-sol koyu iç gölge, alt-sağ ince ışık (içe gömülü kutu).</summary>
        private static Color[] Slot(int n)
        {
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    Color c;
                    if (CornerCut(x, y, n, 1)) c = Color.clear;
                    else if (y == 0 || x == n - 1) c = Gray(1f);                 // Alt-sağ ışık kenarı
                    else if (y == n - 1 || x == 0) c = Gray(0.3f);               // Üst-sol koyu kenar
                    else if (y == n - 2 || x == 1) c = Gray(0.55f);              // İç gölge
                    else if (y == n - 3) c = Gray(0.68f);
                    else c = Gray(0.8f);
                    px[y * n + x] = c;
                }
            }
            return px;
        }

        /// <summary>Metal çerçeve halkası (nadirlik rengiyle boyanır): koyu kontur, 3 piksel kabartmalı kenar.</summary>
        private static Color[] Frame(int n)
        {
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(n - 1 - x, n - 1 - y));
                    Color c;
                    if (CornerCut(x, y, n, 2)) c = Color.clear;
                    else if (edge == 0) c = Gray(0.15f);
                    else if (edge <= 3)
                    {
                        bool topLeft = Mathf.Min(n - 1 - y, x) <= Mathf.Min(y, n - 1 - x);   // En yakın kenar üst ya da sol mu
                        c = Gray(edge == 1 ? (topLeft ? 1f : 0.6f) : edge == 2 ? 0.88f : (topLeft ? 0.72f : 0.55f));
                    }
                    else c = Gray(0.5f);
                    px[y * n + x] = c;
                }
            }
            return px;
        }

        private static Color[] BarBack(int n)
        {
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    Color c;
                    if (CornerCut(x, y, n, 1)) c = Color.clear;
                    else if (y == n - 1 || x == 0) c = Gray(0.25f);
                    else if (y == 0 || x == n - 1) c = Gray(0.95f);
                    else if (y == n - 2) c = Gray(0.45f);
                    else c = Gray(0.62f);
                    px[y * n + x] = c;
                }
            return px;
        }

        /// <summary>Çubuk dolgusu: üstte parlak şerit, altta koyu (cam/sıvı gibi parlak).</summary>
        private static Color[] BarFill(int n)
        {
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    // Alt dilim koyu, orta düz, üst dilim parlak şerit
                    float v = y >= n - 2 ? 1f : y >= n - 4 ? 0.93f : y >= 4 ? 0.82f : Mathf.Lerp(0.55f, 0.75f, y / 3f);
                    if (x == 0 || x == n - 1) v *= 0.85f;
                    px[y * n + x] = Gray(v);
                }
            return px;
        }

        /// <summary>Üst bar: koyu yarı saydam şerit, altta altın çizgi ve yumuşak gölge.</summary>
        private static Color[] TopBar(int n)
        {
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    Color c;
                    if (y == 0) c = new Color(0f, 0f, 0f, 0.25f);
                    else if (y == 1) c = new Color(0f, 0f, 0f, 0.5f);
                    else if (y == 2) c = new Color(0.45f, 0.32f, 0.12f);
                    else if (y == 3) c = new Color(0.85f, 0.65f, 0.28f);
                    else if (y == 4) c = new Color(0.02f, 0.02f, 0.03f, 0.95f);
                    else
                    {
                        float v = Mathf.Lerp(0.05f, 0.11f, (y - 5f) / (n - 6f)) + Grain(0.01f);
                        c = new Color(v, v * 0.95f, v * 1.12f, 0.82f);
                    }
                    px[y * n + x] = c;
                }
            return px;
        }

        /// <summary>Döşenebilir koyu taş zemin (yükseltme ekranının arkası).</summary>
        private static Color[] Background(int n)
        {
            var px = new Color[n * n];
            const int block = 16;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int row = y / (block / 2);
                    int bx = (x + (row % 2) * (block / 2)) % n;
                    bool mortar = y % (block / 2) == 0 || bx % block == 0;
                    int id = (bx / block) * 7 + row * 13;
                    float v = mortar ? 0.04f : 0.085f + 0.02f * ((id * 37 % 11) / 11f) + Grain(0.012f);
                    if (!mortar && y % (block / 2) == block / 2 - 1) v += 0.015f;   // Taşın üst kenarı hafif ışık
                    px[y * n + x] = new Color(v, v * 0.96f, v * 1.1f, 1f);
                }
            }
            return px;
        }

        // ---------------- Dosya ----------------

        private static void WritePng(string path, Color[] pixels, int size, int border, float scale, bool tiled)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f / scale;   // Tuval 100 birim/piksel: doku pikseli 'scale' kat büyük
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        /// <summary>Varsayılan TMP yazı tipinin gölgeli kopyası (alt-sağa koyu, hafif yumuşak gölge).</summary>
        private static void CreateShadowMaterial()
        {
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null) return;
            var mat = new Material(font.material) { name = "TextShadow" };
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.85f));
            mat.SetFloat("_UnderlayOffsetX", 0.55f);
            mat.SetFloat("_UnderlayOffsetY", -0.55f);
            mat.SetFloat("_UnderlayDilate", 0.25f);
            mat.SetFloat("_UnderlaySoftness", 0.15f);
            AssetDatabase.DeleteAsset(ShadowMaterialPath);
            AssetDatabase.CreateAsset(mat, ShadowMaterialPath);
        }
    }
}
