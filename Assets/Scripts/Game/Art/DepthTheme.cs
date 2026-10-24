using UnityEngine;

namespace RiskLoop.Game.Art
{
    /// <summary>
    /// Derinliğe göre kaya katmanının rengi. Her 10 katlık bölge (sonunda boss) kendi rengindedir; 90. kattan sonra renkler döner,
    /// böylece oyuncu derine indikçe dünyanın değiştiğini GÖRÜR (ilerleme hissi).
    /// </summary>
    public static class DepthTheme
    {
        private const int ZoneSize = 10;   // BalanceData.zoneSize ile aynı (sadece renk için)

        private static readonly Color[] WallColors =
        {
            new Color(0.50f, 0.41f, 0.33f), // toprak ve kumtaşı (çamur gibi durmasın diye taşsı)
            new Color(0.50f, 0.50f, 0.53f), // taş
            new Color(0.45f, 0.38f, 0.68f), // kristal mağarası
            new Color(0.26f, 0.40f, 0.55f), // yeraltı gölü
            new Color(0.55f, 0.28f, 0.20f), // cüce demirhanesi
            new Color(0.66f, 0.62f, 0.48f), // fosil mezarlığı
            new Color(0.24f, 0.38f, 0.28f), // ejderin ini
            new Color(0.70f, 0.24f, 0.10f), // lav kalbi
            new Color(0.20f, 0.17f, 0.26f), // dünyanın kökleri
        };

        /// <summary>Bu kat hangi bölgede (0..)?</summary>
        public static int ZoneIndex(int step) => ((System.Math.Max(step, 1) - 1) / ZoneSize) % WallColors.Length;

        public static Color WallColor(int step) => WallColors[ZoneIndex(step)];

        // Her bölgenin cevheri (kayadaki damar ve benekler):
        // altın, gümüş, ametist, safir, bakır, kemik, zümrüt, magma, hiçlik
        private static readonly Color[] OreColors =
        {
            new Color(1.00f, 0.80f, 0.25f),
            new Color(0.85f, 0.90f, 0.98f),
            new Color(0.80f, 0.50f, 1.00f),
            new Color(0.35f, 0.70f, 1.00f),
            new Color(1.00f, 0.55f, 0.25f),
            new Color(0.95f, 0.92f, 0.80f),
            new Color(0.35f, 1.00f, 0.55f),
            new Color(1.00f, 0.45f, 0.10f),
            new Color(0.70f, 0.40f, 1.00f),
        };

        public static Color OreColor(int step) => OreColors[ZoneIndex(step)];

        /// <summary>
        /// Maden görselinin zenginliği: 1-10. kat 0 (sade), her 10 katta +1, en fazla MineArt.MaxLevel.
        /// Renkler 9 bölgede bir döner ama zenginlik dönmez: derindeki maden hep daha gösterişli.
        /// </summary>
        public static int DetailLevel(int step) => System.Math.Min((System.Math.Max(step, 1) - 1) / ZoneSize, MineArt.MaxLevel);
    }
}
