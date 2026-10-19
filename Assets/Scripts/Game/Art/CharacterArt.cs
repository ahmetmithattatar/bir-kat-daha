using System.Collections.Generic;
using UnityEngine;

namespace RiskLoop.Game.Art
{
    /// <summary>
    /// Karakter görselinin katmanları (24x32 piksel). Hepsi aynı tuvalde, üst üste konur:
    /// pelerin (arkada) → gövde → pantolon → çizme → zırh → omuzluk → eldiven → kolye → boynuz → kask.
    /// Her ekipman katmanı malzeme renkleriyle boyanır (A ana, a gölge, C vurgu). Nadirlik yükseldikçe
    /// görünüm değişir: yeşil ve üstü kaskta boynuz, zırhta omuzluk; kırmızı ve üstü pelerin.
    /// Silah ve kazma ayrı Image'larda (GearArt'tan).
    /// </summary>
    public static class CharacterArt
    {
        public const int Width = 24;
        public const int Height = 32;

        private static readonly Dictionary<string, string[]> Layers = new Dictionary<string, string[]>
        {
            // Gövde: K kontur, B ten, b ten gölgesi, E göz, U iç giysi
            { "body", new[] {
                "........................", "........................", ".........KKKKKK.........", "........KBBBBBBK........",
                "........KBBBBBBK........", "........KBEBBEBK........", "........KBBBBBBK........", "........KBBbbBBK........",
                ".........KBBBBK.........", "..........KBBK..........", "......KKKKUUUUKKKK......", ".....KBUUUUUUUUUUBK.....",
                "....KBBKUUUUUUUUKBBK....", "....KBBKUUUUUUUUKBBK....", "....KBBKUUUUUUUUKBBK....", "....KBBKUUUUUUUUKBBK....",
                "....KBBKUUUUUUUUKBBK....", "....KbbKUUUUUUUUKbbK....", "....KBBKKUUUUUUKKBBK....", ".......KUUUUUUUUK.......",
                ".......KUUUKKUUUK.......", ".......KUUUKKUUUK.......", ".......KUUUKKUUUK.......", ".......KUUUKKUUUK.......",
                ".......KUUUKKUUUK.......", ".......KUUUKKUUUK.......", ".......KBBBKKBBBK.......", ".......KBBBKKBBBK.......",
                "......KBBBBKKBBBBK......", "......KKKKKKKKKKKK......", "........................", "........................" } },

            { "helmet", new[] {
                "........................", ".........KKKKKK.........", "........KAAAAAAK........", ".......KAAWAAAAAK.......",
                ".......KAaaaaaaAK.......", ".......KAK....KAK.......", ".......KAK....KAK.......", ".......KaK....KaK......." } },

            { "horns", new[] {
                "....KK............KK....", "....KCK..........KCK....", ".....KCK........KCK.....", "......KCK......KCK......" } },

            { "armor", new[] {
                "........................", "........................", "........................", "........................",
                "........................", "........................", "........................", "........................",
                "........................", "........................", "......KKKKAAAAKKKK......", ".....KAAAAAAAAAAAAK.....",
                "....KAAKAAAAAAAAKAAK....", "....KAAKAAACCAAAKAAK....", "........AAaCCaAA........", "........AAAAAAAA........",
                "........AaAAAAaA........", "........AAAAAAAA........", "........KaaaaaaK........" } },

            { "pauldrons", new[] {
                "........................", "........................", "........................", "........................",
                "........................", "........................", "........................", "........................",
                "........................", "...KKKK..........KKKK...", "..KCCCCK........KCCCCK..", "..KCWCCCK......KCCCWCK..",
                "...KKKKK........KKKKK..." } },

            { "cape", new[] {
                "........................", "........................", "........................", "........................",
                "........................", "........................", "........................", "........................",
                "........................", "........................", "......KAAAAAAAAAAK......", ".....KAAAAAAAAAAAAK.....",
                ".....KAAAAAAAAAAAAK.....", ".....KAAAAAAAAAAAAK.....", ".....KAAAAAAAAAAAAK.....", ".....KAAAAAAAAAAAAK.....",
                ".....KAAAAAAAAAAAAK.....", ".....KAAAAAAAAAAAAK.....", ".....KAAAAAAAAAAAAK.....", ".....KAAAAAAAAAAAAK.....",
                ".....KAaAAAAAAAAaAK.....", ".....KAaAAAAAAAAaAK.....", ".....KAaAAAAAAAAaAK.....", ".....KAAAAAAAAAAAAK.....",
                ".....KAAAAAAAAAAAAK.....", ".....KAAAAAAAAAAAAK.....", ".....KAAAAAAAAAAAAK.....", "....KAAAAAAAAAAAAAAK....",
                "....KaaaaaaaaaaaaaaK....", "....KKKKKKKKKKKKKKKK...." } },

            { "pants", new[] {
                "........................", "........................", "........................", "........................",
                "........................", "........................", "........................", "........................",
                "........................", "........................", "........................", "........................",
                "........................", "........................", "........................", "........................",
                "........................", "........................", "........................", ".......KAAAAAAAAK.......",
                ".......KAAAKKAAAK.......", ".......KAaAKKAaAK.......", ".......KAaAKKAaAK.......", ".......KAAAKKAAAK.......",
                ".......KAaAKKAaAK.......", ".......KAAAKKAAAK......." } },

            { "boots", new[] {
                "........................", "........................", "........................", "........................",
                "........................", "........................", "........................", "........................",
                "........................", "........................", "........................", "........................",
                "........................", "........................", "........................", "........................",
                "........................", "........................", "........................", "........................",
                "........................", "........................", "........................", "........................",
                "........................", ".......KCCCKKCCCK.......", ".......KAAAKKAAAK.......", ".......KAAAKKAAAK.......",
                "......KAAAAKKAAAAK......", "......KKKKKKKKKKKK......" } },

            { "gloves", new[] {
                "........................", "........................", "........................", "........................",
                "........................", "........................", "........................", "........................",
                "........................", "........................", "........................", "........................",
                "........................", "........................", "........................", "........................",
                "....KAAK........KAAK....", "....KAAK........KAAK....", "....KCCK........KCCK...." } },

            { "necklace", new[] {
                "........................", "........................", "........................", "........................",
                "........................", "........................", "........................", "........................",
                "........................", "........................", "..........AAAA..........", "..........KCCK.........." } },
        };

        private static readonly Color32 Outline = new Color32(0x14, 0x10, 0x10, 0xFF);

        /// <summary>Çıplak gövde (ten ve iç giysi).</summary>
        public static Sprite Body()
        {
            var palette = new Dictionary<char, Color32>
            {
                { 'K', Outline },
                { 'B', new Color32(0xE8, 0xB8, 0x90, 0xFF) },
                { 'b', new Color32(0xC0, 0x88, 0x68, 0xFF) },
                { 'E', new Color32(0x2A, 0x1E, 0x1A, 0xFF) },
                { 'U', new Color32(0x3A, 0x3A, 0x44, 0xFF) },
            };
            return PixelArt.FromRows("char_body", Pad(Layers["body"]), palette);
        }

        /// <summary>Bir ekipman katmanı, verilen renklerle (hex, # olmadan).</summary>
        public static Sprite Layer(string layer, string main, string shade, string accent)
        {
            var palette = new Dictionary<char, Color32>
            {
                { 'K', Outline },
                { 'W', new Color32(0xFF, 0xFF, 0xF8, 0xFF) },
                { 'A', Hex(main) },
                { 'a', Hex(shade) },
                { 'C', Hex(accent) },
            };
            return PixelArt.FromRows($"char_{layer}_{main}_{shade}_{accent}", Pad(Layers[layer]), palette);
        }

        /// <summary>Tüm katmanlar 24x32 olsun diye eksik satırları boş satırla tamamla.</summary>
        private static string[] Pad(string[] rows)
        {
            if (rows.Length >= Height) return rows;
            var padded = new string[Height];
            for (int i = 0; i < Height; i++) padded[i] = i < rows.Length ? rows[i] : "";
            return padded;
        }

        public static string ToHex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        private static Color32 Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }
    }
}
