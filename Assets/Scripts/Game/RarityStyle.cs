using UnityEngine;

namespace RiskLoop.Game
{
    /// <summary>
    /// Nadirlik seviyelerinin görünüşü: isim ve çerçeve rengi.
    /// Sıra BalanceData.rarityTiers ile aynı: 0 = en sıradan, 8 = en iyi.
    /// Seviye sayısını değiştirirsen bu iki diziyi de güncelle.
    /// </summary>
    public static class RarityStyle
    {
        private static readonly string[] Names =
        {
            "Sıradan", "Kaliteli", "Nadir", "Değerli", "Seçkin", "Destansı", "Efsanevi", "Mitik", "Kadim"
        };

        private static readonly Color32[] Colors =
        {
            new Color32(0xFF, 0xFF, 0xFF, 0xFF), // beyaz
            new Color32(0x8F, 0xD3, 0xFF, 0xFF), // açık mavi
            new Color32(0x2F, 0x8C, 0xFF, 0xFF), // mavi
            new Color32(0x4A, 0x5C, 0xE0, 0xFF), // lacivert (koyu zeminde okunsun diye biraz açık)
            new Color32(0x3C, 0xCB, 0x5A, 0xFF), // yeşil
            new Color32(0xFF, 0xE0, 0x3A, 0xFF), // sarı
            new Color32(0xFF, 0x9A, 0x2E, 0xFF), // turuncu
            new Color32(0xFF, 0x3B, 0x3B, 0xFF), // kırmızı
            new Color32(0xB0, 0x4D, 0xFF, 0xFF), // mor
        };

        public static string Name(int tier) => Names[ClampTier(tier)];

        public static Color TierColor(int tier) => Colors[ClampTier(tier)];

        /// <summary>TextMeshPro zengin metni ile metni o seviyenin rengine boyar.</summary>
        public static string Colored(string text, int tier) =>
            $"<color=#{ColorUtility.ToHtmlStringRGB(TierColor(tier))}>{text}</color>";

        private static int ClampTier(int tier) => Mathf.Clamp(tier, 0, Names.Length - 1);
    }
}
