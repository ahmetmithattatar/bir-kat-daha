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
            new Color(0.55f, 0.38f, 0.24f), // toprak
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
    }
}
