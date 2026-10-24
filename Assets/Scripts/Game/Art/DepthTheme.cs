using UnityEngine;

namespace RiskLoop.Game.Art
{
    /// <summary>
    /// Derinliğe göre kazılan malzeme. Her 10 katlık bölge (sonunda boss) kendi malzemesindedir:
    /// toprak → taş → kristal → göl taşı → demir → fosil → ejder pulu → bazalt/lav → hiçlik; 90. kattan sonra döner.
    /// Bölgenin son 3 katında bir sonraki malzeme yamalar halinde belirir (toprağın içinden taş çıkar):
    /// oyuncu derine indikçe dünyanın değiştiğini GÖRÜR (ilerleme hissi).
    /// </summary>
    public static class DepthTheme
    {
        private const int ZoneSize = 10;   // BalanceData.zoneSize ile aynı (sadece görsel için)

        // Bölge sırası = MineArt.Material sırası. baseColor: ana renk, accent: çakıl/kristal/cevher/lav rengi.
        private static readonly MineArt.Palette[] Palettes =
        {
            P(MineArt.Material.Soil,     new Color(0.46f, 0.32f, 0.21f), new Color(0.56f, 0.52f, 0.47f)), // toprak
            P(MineArt.Material.Stone,    new Color(0.50f, 0.50f, 0.52f), new Color(0.85f, 0.90f, 0.98f)), // taş
            P(MineArt.Material.Crystal,  new Color(0.36f, 0.32f, 0.44f), new Color(0.80f, 0.50f, 1.00f)), // kristal mağarası
            P(MineArt.Material.WetStone, new Color(0.28f, 0.38f, 0.48f), new Color(0.45f, 0.80f, 1.00f)), // yeraltı gölü
            P(MineArt.Material.Iron,     new Color(0.42f, 0.32f, 0.28f), new Color(0.85f, 0.42f, 0.16f)), // cüce demirhanesi
            P(MineArt.Material.Fossil,   new Color(0.70f, 0.65f, 0.52f), new Color(0.96f, 0.93f, 0.82f)), // fosil mezarlığı
            P(MineArt.Material.Scales,   new Color(0.22f, 0.36f, 0.27f), new Color(0.35f, 1.00f, 0.55f)), // ejderin ini
            P(MineArt.Material.Basalt,   new Color(0.17f, 0.14f, 0.13f), new Color(1.00f, 0.60f, 0.15f)), // lav kalbi
            P(MineArt.Material.Void,     new Color(0.27f, 0.21f, 0.36f), new Color(0.75f, 0.50f, 1.00f)), // dünyanın kökleri
        };

        private static MineArt.Palette P(MineArt.Material m, Color baseColor, Color accent) =>
            new MineArt.Palette { material = m, baseColor = baseColor, accent = accent };

        /// <summary>Bu kat hangi bölgede (0..8, döner)?</summary>
        public static int ZoneIndex(int step) => ((System.Math.Max(step, 1) - 1) / ZoneSize) % Palettes.Length;

        public static MineArt.Palette Palette(int step) => Palettes[ZoneIndex(step)];

        /// <summary>Bir sonraki bölgenin malzemesi (geçiş yamaları için).</summary>
        public static MineArt.Palette NextPalette(int step) => Palettes[(ZoneIndex(step) + 1) % Palettes.Length];

        /// <summary>
        /// Geçiş oranı: bölgenin 8. katında %25, 9. katında %50, boss katında %75 sonraki malzeme. Diğer katlarda 0.
        /// Dörtte birlik adımlar: aynı görsel tekrar üretilmez (önbellek).
        /// </summary>
        public static float TransitionMix(int step)
        {
            int floorInZone = (System.Math.Max(step, 1) - 1) % ZoneSize + 1;
            return floorInZone >= 8 ? (floorInZone - 7) * 0.25f : 0f;
        }

        /// <summary>Moloz ve parçacıklar için malzemenin ana rengi.</summary>
        public static Color WallColor(int step) => Palette(step).baseColor;

        public static Color OreColor(int step) => Palette(step).accent;
    }
}
