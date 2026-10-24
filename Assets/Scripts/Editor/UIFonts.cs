using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace RiskLoop.EditorTools
{
    /// <summary>
    /// Oyunun yazı tipleri (ikisi de Google Fonts, SIL Open Font License: ticari oyunda serbest, lisans metni yanında):
    ///   Başlık: Cinzel  (oyma taş harfli, fantastik; başlıklar, kutlama yazıları, duvara kazınmış kat adı)
    ///   Metin : Rubik   (yuvarlak, kalın gövdeli, küçük boyda da çok okunaklı; tüm diğer yazılar)
    /// TTF dosyalarından TextMeshPro yazı tipi dosyaları (SDF) üretilir: Assets/Fonts/*.asset.
    /// Rubik, TMP'nin varsayılan yazı tipi yapılır: sahnede kodla eklenen her yazı onu kullanır.
    /// Her yazı tipinin bir de gölgeli malzemesi var (koyu alt-sağ gölge: oyun sahnesinin ve flaşların üstünde okunur).
    /// </summary>
    public static class UIFonts
    {
        private const string Folder = "Assets/Fonts";
        private const string BodyTtf = Folder + "/Rubik.ttf";
        private const string TitleTtf = Folder + "/Cinzel.ttf";

        // Atlasa baştan konan karakterler (Türkçe dahil). Diğerleri gerektikçe eklenir (dinamik atlas).
        private const string Characters =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "ÇĞİÖŞÜçğıöşü×→←↑↓▲▼•…–—’“”%₺";

        public static TMP_FontAsset Body => Load(BodyTtf, "Rubik SDF");
        public static TMP_FontAsset Title => Load(TitleTtf, "Cinzel SDF");
        public static Material BodyShadow => AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/Rubik SDF Shadow.mat");
        public static Material TitleShadow => AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/Cinzel SDF Shadow.mat");

        /// <summary>Yazı tipi dosyaları yoksa üretir ve Rubik'i TMP varsayılanı yapar. Sahne kurulmadan önce çağrılır.</summary>
        public static void Ensure()
        {
            TMP_FontAsset body = Load(BodyTtf, "Rubik SDF") ?? Create(BodyTtf, "Rubik SDF");
            TMP_FontAsset title = Load(TitleTtf, "Cinzel SDF") ?? Create(TitleTtf, "Cinzel SDF");
            if (body == null || title == null)
            {
                Debug.LogWarning("Yazı tipi dosyaları (Assets/Fonts/Rubik.ttf, Cinzel.ttf) bulunamadı: varsayılan yazı tipi kullanılacak.");
                return;
            }
            EnsureShadow(body);
            EnsureShadow(title);

            // Varsayılan yazı tipi: kodla eklenen her TextMeshPro bunu alır
            var settings = new SerializedObject(TMP_Settings.instance);
            settings.FindProperty("m_defaultFontAsset").objectReferenceValue = body;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Metin yazısı: Rubik + gölge.</summary>
        public static void ApplyBody(TMP_Text text)
        {
            if (Body == null) return;
            text.font = Body;
            if (BodyShadow != null) text.fontSharedMaterial = BodyShadow;
        }

        /// <summary>Başlık yazısı: Cinzel + gölge, harfler arası biraz açık.</summary>
        public static void ApplyTitle(TMP_Text text)
        {
            if (Title == null) return;
            text.font = Title;
            if (TitleShadow != null) text.fontSharedMaterial = TitleShadow;
            text.characterSpacing = Mathf.Max(text.characterSpacing, 2f);
        }

        private static TMP_FontAsset Load(string ttfPath, string name) =>
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{Folder}/{name}.asset");

        private static TMP_FontAsset Create(string ttfPath, string name)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (font == null) return null;

            // 90 punto örnekleme + 9 piksel dolgu: büyük başlıklarda da keskin kenar ve gölge için yer
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                                                                AtlasPopulationMode.Dynamic, true);
            asset.name = name;
            asset.TryAddCharacters(Characters, out _);

            string path = $"{Folder}/{name}.asset";
            AssetDatabase.CreateAsset(asset, path);
            // Atlas dokuları ve malzeme yazı tipi dosyasının içinde saklanır (alt nesne)
            foreach (Texture2D atlas in asset.atlasTextures)
            {
                atlas.name = $"{name} Atlas";
                AssetDatabase.AddObjectToAsset(atlas, asset);
            }
            asset.material.name = $"{name} Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        /// <summary>Yazı tipinin malzemesinden gölgeli kopya (TMP "underlay").</summary>
        private static void EnsureShadow(TMP_FontAsset font)
        {
            string path = $"{Folder}/{font.name} Shadow.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            var mat = new Material(font.material) { name = $"{font.name} Shadow" };
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.85f));
            mat.SetFloat("_UnderlayOffsetX", 0.6f);
            mat.SetFloat("_UnderlayOffsetY", -0.6f);
            mat.SetFloat("_UnderlayDilate", 0.3f);
            mat.SetFloat("_UnderlaySoftness", 0.2f);
            AssetDatabase.CreateAsset(mat, path);
        }
    }
}
