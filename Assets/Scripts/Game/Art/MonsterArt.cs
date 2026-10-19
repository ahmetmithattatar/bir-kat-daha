using System.Collections.Generic;
using UnityEngine;

namespace RiskLoop.Game.Art
{
    /// <summary>
    /// Canavar görselleri: 14 temel çizim (şablon) x canavar başına renk paleti = 41 farklı canavar.
    /// Şablon harfleri: K kontur, A ana renk, a gölge, C vurgu (giysi/taş/alev), W beyaz (diş/göz parlaması), R kırmızı göz.
    /// Yeni canavar: DefaultContent'e id, FantasyTexts'e isim, buraya (şablon, 3 renk) satırı.
    /// </summary>
    public static class MonsterArt
    {
        private struct Look
        {
            public string Template;
            public Color32 Main, Shade, Accent;
            public Look(string template, string main, string shade, string accent)
            {
                Template = template;
                Main = Hex(main);
                Shade = Hex(shade);
                Accent = Hex(accent);
            }
        }

        private static readonly Dictionary<string, Look> Looks = new Dictionary<string, Look>
        {
            // Toprak
            { "cave_rat", new Look("rat", "8B7B6B", "5E5248", "E8A0A0") },
            { "slime", new Look("slime", "5BC85B", "2E8A3A", "A8F0A8") },
            { "bat", new Look("bat", "6A5A7A", "3E3248", "C04040") },
            { "goblin", new Look("goblin", "7BB050", "4A7A30", "8B5A2B") },
            // Taş
            { "skeleton", new Look("skeleton", "E8E0C8", "A89F88", "6A6A6A") },
            { "cave_spider", new Look("spider", "5A4A3A", "33281F", "8A6A4A") },
            { "stone_goblin", new Look("goblin", "9A9A9A", "666666", "5A4A3A") },
            { "blind_worm", new Look("worm", "C89A8A", "8A6A5A", "E06060") },
            // Kristal
            { "crystal_slime", new Look("slime", "B08AF0", "7050C0", "E0D0FF") },
            { "crystal_spider", new Look("spider", "8AA8F0", "4A60B0", "D0E0FF") },
            { "wraith", new Look("wraith", "A0C0D8", "607890", "FFFFFF") },
            { "myconid", new Look("mushroom", "C04848", "802828", "E8DCC0") },
            // Göl
            { "naga", new Look("serpent", "3AA090", "1F6858", "E8C060") },
            { "drowned", new Look("skeleton", "8AB0A0", "5A7A70", "3A5A50") },
            { "serpent", new Look("serpent", "4A8AC8", "2A5A90", "E8E080") },
            { "water_elemental", new Look("elemental", "5AB0F0", "2A70B0", "FFFFFF") },
            // Demirhane
            { "iron_golem", new Look("golem", "8A8F98", "4A4E55", "FF8A30") },
            { "fire_imp", new Look("imp", "E0602A", "A03A18", "FFC040") },
            { "dwarf_ghost", new Look("wraith", "E8C890", "A88A58", "FFF0C0") },
            { "coal_worm", new Look("worm", "3A3A3A", "1E1E1E", "FF6A20") },
            // Fosil
            { "bone_whelp", new Look("dragon", "E8E0C8", "A89F88", "C8B890") },
            { "fossil_spider", new Look("spider", "C8B890", "8A7A58", "E8D8B0") },
            { "mummy", new Look("skeleton", "D8C8A0", "9A8A68", "6A5A40") },
            { "sand_worm", new Look("worm", "D8B878", "9A7A48", "C04030") },
            // Ejder
            { "dragon_whelp", new Look("dragon", "3AA050", "1E6A30", "E8C040") },
            { "kobold", new Look("goblin", "C86A3A", "8A4520", "6A6A6A") },
            { "flame_bat", new Look("bat", "E05A2A", "902A10", "FFD040") },
            { "dragon_cultist", new Look("wraith", "7A2A4A", "4A1230", "E8C040") },
            // Lav
            { "lava_elemental", new Look("elemental", "FF6A1A", "B03A0A", "FFE060") },
            { "magma_slime", new Look("slime", "E04A1A", "901A0A", "FFB040") },
            { "demon", new Look("imp", "A01A2A", "600A18", "FF8A30") },
            { "hellhound", new Look("rat", "5A1A1A", "300A0A", "FF5A20") },
            // Bosslar
            { "goblin_chief", new Look("goblin", "5A9A3A", "36602A", "D8B040") },
            { "bone_lord", new Look("skeleton", "F0F0F0", "B0B0C0", "8040C0") },
            { "crystal_golem", new Look("golem", "9A80E8", "5A40B0", "E0F0FF") },
            { "naga_queen", new Look("serpent", "2AB0A0", "16706A", "FFD040") },
            { "forge_king", new Look("golem", "B0603A", "70381E", "FFC040") },
            { "fossil_wyrm", new Look("dragon", "D8CCA8", "9A8E6A", "A8946A") },
            { "elder_dragon", new Look("dragon", "C02A2A", "701616", "FFD040") },
            { "lava_titan", new Look("elemental", "FF7A2A", "A03A0A", "FFFFA0") },
            { "core_devourer", new Look("imp", "4A2A7A", "28164A", "C080FF") },
        };

        private static readonly Dictionary<string, string[]> Templates = new Dictionary<string, string[]>
        {
            { "slime", new[] {
                "................", "................", "................", "................",
                "......KKKK......", "....KKAAAAKK....", "...KAAWAAAAAK...", "..KAAWAAAAAAAK..",
                "..KAAAAAAAAAAK..", ".KAAAKKAAKKAAAK.", ".KAAAKWAAKWAAAK.", ".KAAAAAAAAAAAAK.",
                ".KaAAAACCAAAAaK.", ".KaaAAAAAAAAaaK.", "..KKaaaaaaaaKK..", "....KKKKKKKK...." } },
            { "rat", new[] {
                "................", "................", "................", "................",
                "................", "..KK............", ".KCAK...KKKKK...", ".KAAAKKKAAAAAK..",
                "KRKAAAAAAAAAAAK.", "KAAAAAAAAAAaAAK.", ".KWAAAAAAAAaaAK.", "..KKAAAAAAAaaK.K",
                "....KaKKKaKKK.KC", "....KK..KK...KC.", ".............K..", "................" } },
            { "bat", new[] {
                "................", "................", "................", "K..............K",
                "KK....K..K....KK", "KAK...KKKK...KAK", "KAAK.KAAAAK.KAAK", "KAaAKARAARAKAaAK",
                "KAaaAAAAAAAAaaAK", ".KAaaAAWWAAaaAK.", "..KAaKAAAAKaAK..", "...KK.KAAK.KK...",
                ".......KK.......", "................", "................", "................" } },
            { "goblin", new[] {
                "................", "................", ".....KKKKKK.....", "K...KAAAAAAK...K",
                "KAKKAARAARAAKKAK", ".KAAAAAAAAAAAAK.", "..KKAAKWWKAAKK..", "....KAAAAAAK....",
                "...KCCCCCCCCK...", "..KACCCCCCCCAK..", "..KAKCCCCCCKAK..", "...KKCCKKCCKK...",
                ".....KAK.KAK....", ".....KaK.KaK....", "....KKKK.KKKK...", "................" } },
            { "skeleton", new[] {
                "................", ".....KKKKKK.....", "....KAAAAAAK....", "....KAKKAKKK....",
                "....KAKRAKRK....", "....KAAAAAAK....", ".....KAWAWK.....", "......KKKK......",
                "...KKKAAAAKKK...", "..KAKKAKKAKKAK..", "..KAK.KAAK.KAK..", "......KCCK......",
                ".....KAKKAK.....", ".....KAK.KAK....", "....KKK...KKK...", "................" } },
            { "spider", new[] {
                "................", "................", "................", "..K..........K..",
                "...K..KKKK..K...", "K...KKAAAAKK...K", ".K.KAACAACAAK.K.", "..KAAaAAAAaAAK..",
                "KKKAAAAAAAAAAKKK", "...KARARRARAK...", "..K.KAAAAAAK.K..", ".K..KKAWWAKK..K.",
                "K..K..KKKK..K..K", "...K........K...", "................", "................" } },
            { "wraith", new[] {
                "................", "......KKKK......", "....KKAAAAKK....", "...KAAAAAAAAK...",
                "..KAAKKAAKKAAK..", "..KAAKRAAKRAAK..", "..KAAAAAAAAAAK..", "..KAAAACCAAAAK..",
                "..KAaAAAAAAAaK..", ".KAAaAAAAAAaAAK.", ".KAaaAAAAAAaaAK.", "KAAaaaAAAAaaaAAK",
                "KAKaaKaaaaKaaKAK", "KK.KK.KKKK.KK.KK", "................", "................" } },
            { "golem", new[] {
                "................", "....KKKKKKKK....", "...KAAAAAAAAK...", "...KAKCKKCKAK...",
                "...KAAAAAAAAK...", ".KKKKaAAAAaKKKK.", "KAAAKAAAAAAKAAAK", "KAaAKAACCAAKAaAK",
                "KAaAKAACCAAKAaAK", "KAAAKAAAAAAKAAAK", ".KKKKaaaaaaKKKK.", "....KAAKKAAK....",
                "...KAAAK.KAAAK..", "...KAAAK.KAAAK..", "...KKKKK.KKKKK..", "................" } },
            { "worm", new[] {
                "................", "................", "................", "..........KKKK..",
                ".........KAAAAK.", "........KAWKAAAK", "........KAAAAAAK", "...KKKK.KACCCAK.",
                "..KAAAAKKAAAAK..", ".KAaKAAAAAaAK...", ".KAAKAaKAAAK....", "KAaAAKAAKKK.....",
                "KAAaAKK.........", ".KKKK...........", "................", "................" } },
            { "mushroom", new[] {
                "................", "....KKKKKKKK....", "..KKAAWAAAAAKK..", ".KAAAAAAAWAAAAK.",
                "KAAWAAAAAAAAWAAK", "KAAAAAWAAAAAAAAK", ".KKKKKKKKKKKKKK.", "....KCCCCCCK....",
                "....KCRCCRCK....", "....KCCCCCCK....", "...KCCCKKCCCK...", "..KCKCCCCCCKCK..",
                "....KCCCCCCK....", "....KCCKKCCK....", "....KKK..KKK....", "................" } },
            { "serpent", new[] {
                "................", ".....KKKK.......", "....KAAAAK......", "...KARAARAK.....",
                "...KAAAAAAK.....", "....KAWWAK......", ".....KAAK.......", "....KCAAAK......",
                "...KCAAAAK......", "...KAAAAK.......", "....KAAAAKK.....", ".....KKAAAAK....",
                "..KK...KAAAAK...", ".KAAKKKAAAaK....", "..KaaAAAaaK.....", "...KKKKKKK......" } },
            { "elemental", new[] {
                "................", "......K..K......", ".....KAKKAK.....", "....KAAAAAAK....",
                "...KAAWAAWAAK...", "...KAAAAAAAAK...", "..KAaAACCAAaAK..", ".KAAaACCCCAaAAK.",
                ".KAaAAACCAAAaAK.", "..KAAaAAAAaAAK..", "...KAAaAAaAAK...", "..KAKAAAAAAKAK..",
                ".KA..KAAAAK..AK.", "......KAAK......", ".......KK.......", "................" } },
            { "imp", new[] {
                "................", "..K..........K..", "..KK..KKKK..KK..", "...KKKAAAAKKK...",
                "....KAAAAAAK....", "....KARAARAK....", "....KAAAAAAK....", ".....KAWWAK.....",
                "..KKKKAAAAKKKK..", ".KCCKAAAAAAKCCK.", "KCCCKAAAAAAKCCCK", ".KKK.KAAAAK.KKK.",
                ".....KAKKAK.....", ".....KAK.KAK....", "....KKK...KKK...", "................" } },
            { "dragon", new[] {
                "................", "..KK........KK..", ".KCCK......KCCK.", ".KCCCK.KK.KCCCK.",
                "..KCCKKAAKKCCK..", "...KKAAAAAAKK...", "....KARAARAAK...", "....KAAAAAAAAK..",
                "....KAWAWAAAAK..", ".....KKKKAAAK...", "......KAAAAAK...", ".....KAAaaAAAK..",
                "....KAAaaaaAAK.K", "....KAAAAAAAKKAK", ".....KKKKKKKKAK.", "..............K." } },
        };

        public static Sprite SpriteFor(string monsterId)
        {
            Look look = Looks.TryGetValue(monsterId, out Look l) ? l : new Look("slime", "888888", "555555", "AAAAAA");
            string[] rows = Templates.TryGetValue(look.Template, out string[] t) ? t : Templates["slime"];

            var palette = new Dictionary<char, Color32>
            {
                { 'K', new Color32(0x14, 0x10, 0x10, 0xFF) },
                { 'W', new Color32(0xFF, 0xFF, 0xF0, 0xFF) },
                { 'R', new Color32(0xFF, 0x30, 0x20, 0xFF) },
                { 'A', look.Main },
                { 'a', look.Shade },
                { 'C', look.Accent },
            };
            return PixelArt.FromRows($"monster_{monsterId}", rows, palette);
        }

        /// <summary>Canavarın ana rengi (parçacıklar ve aura için).</summary>
        public static Color MainColor(string monsterId) =>
            Looks.TryGetValue(monsterId, out Look l) ? (Color)l.Main : Color.gray;

        private static Color32 Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }
    }
}
