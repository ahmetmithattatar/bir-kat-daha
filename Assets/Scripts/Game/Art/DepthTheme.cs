using UnityEngine;

namespace RiskLoop.Game.Art
{
    /// <summary>
    /// Derinliğe göre kaya katmanının rengi. Her keşif katı yeni bir bölgenin başlangıcıdır,
    /// böylece oyuncu derine indikçe dünyanın değiştiğini GÖRÜR (ilerleme hissi).
    /// </summary>
    public static class DepthTheme
    {
        // Bölge başlangıç katları ve renkleri (aynı sırada)
        private static readonly int[] StartSteps = { 1, 5, 10, 15, 20, 25, 30, 35 };

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
        };

        /// <summary>Bu kat hangi bölgede (0..)?</summary>
        public static int ZoneIndex(int step)
        {
            int zone = 0;
            for (int i = 0; i < StartSteps.Length; i++)
            {
                if (step >= StartSteps[i]) zone = i;
            }
            return zone;
        }

        public static Color WallColor(int step) => WallColors[ZoneIndex(step)];
    }
}
