using System.Collections.Generic;
using RiskLoop.Core;
using UnityEngine;

namespace RiskLoop.Game.Art
{
    /// <summary>
    /// Yükseltme, yetenek, büyü, sınıf ve Kadim yükseltme ikonları (16x16 pixel art, kodla çizilir).
    /// Az sayıda resim yazısı (piktogram) var; her ikon bir piktogram + kendi rengi:
    /// aynı "göz" piktogramı yeşil boyanınca Derin İçgüdü, mor boyanınca Kehanet olur.
    /// Harfler: K kontur, A ana renk, a ana rengin gölgesi, C vurgu rengi, W parıltı, . boş.
    /// </summary>
    public static class IconArt
    {
        private static readonly Dictionary<string, string[]> Pictos = new Dictionary<string, string[]>
        {
            { "pick", new[] {
                "................", "...KKKKKKKKK....", "..KAAAAAAAAAK...", ".KAWAaaaaaaAAK..", ".KAaK..KK..KaAK.",
                "..KK..KCCK..KK..", "......KCCK......", "......KCCK......", "......KCCK......", "......KCCK......",
                "......KCCK......", "......KCCK......", "......KCCK......", "......KCCK......", ".......KK.......", "................" } },
            { "beam", new[] {
                "................", ".KKKKKKKKKKKKKK.", ".KAAAAAAAAAAAAK.", ".KaaaaaaaaaaaaK.", ".KKKKKKKKKKKKKK.",
                "..KAK......KAK..", "..KAK......KAK..", "..KAaK....KaAK..", "..KAKaK..KaKAK..", "..KAK.KaaK.KAK..",
                "..KAK..KK..KAK..", "..KAK......KAK..", "..KAK......KAK..", ".KCCCK....KCCCK.", ".KKKKK....KKKKK.", "................" } },
            { "shield", new[] {
                "................", "..KKKKKKKKKKKK..", "..KAAAAAAAAAAK..", "..KAWAAAAAAAaK..", "..KAAAACCAAAaK..",
                "..KAAACCCCAAaK..", "..KAAAACCAAAaK..", "..KAAAACCAAAaK..", "...KAAAAAAAaK...", "...KAAAAAAAaK...",
                "....KAAAAAaK....", ".....KAAAaK.....", "......KAaK......", ".......KK.......", "................", "................" } },
            { "coins", new[] {
                "................", ".......KKKKK....", "......KAAAAAK...", "......KAWAAaK...", ".....KKAAAAaK...",
                "....KAAKKKKK....", "....KAWAAAAK....", "...KKAAAAAaK....", "..KAAKKKKKKKK...", "..KAWAAAAAAAK...",
                "..KAAAAAAAAaK...", "..KKKKKKKKKKK...", "..KCCCCCCCCCK...", "..KKKKKKKKKKK...", "................", "................" } },
            { "lens", new[] {
                "................", "....KKKKK.......", "...KCCCCCK......", "..KCWCCCCCK.....", "..KCWCCCCCK.....",
                "..KCCCCCCCK.....", "..KCCCCCCCK.....", "...KCCCCCK......", "....KKKKKAK.....", ".........KAK....",
                "..........KAK...", "...........KAK..", "............KAK.", ".............KK.", "................", "................" } },
            { "lantern", new[] {
                "................", ".......KK.......", "......K..K......", ".....KKKKKK.....", ".....KAAAAK.....",
                "....KKKKKKKK....", "....KACCCCAK....", "....KACWCCAK....", "....KACCCCAK....", "....KACCCCAK....",
                "....KACCCCAK....", "....KKKKKKKK....", ".....KAAAAK.....", ".....KKKKKK.....", "................", "................" } },
            { "scale", new[] {
                "................", ".......KK.......", ".......KK.......", ".KKKKKKKKKKKKKK.", ".KAAAAAKKAAAAAK.",
                "..K...KAAK...K..", ".K.K...KK...K.K.", "K...K..KK..K...K", "KCCCK..KK..KCCCK", ".KKK...KK...KKK.",
                ".......KK.......", ".......KK.......", ".....KKKKKK.....", "....KAAAAAAK....", "....KKKKKKKK....", "................" } },
            { "sword", new[] {
                "..............KK", ".............KWK", "............KWAK", "...........KWAK.", "..........KWAK..",
                ".........KWAK...", "........KWAK....", ".......KWAK.....", "..KK..KWAK......", "..KCKKWAK.......",
                "...KCKAK........", "....KCCK........", "...KCK.KK.......", "..KCK...........", ".KCK............", "..K............." } },
            { "heart", new[] {
                "................", "................", "...KKK...KKK....", "..KAAAK.KAAAK...", ".KAWWAAKAAAAAK..",
                ".KAWAAAAAAAAAK..", ".KAAAAAAAAAAaK..", ".KAAAAAAAAAAaK..", "..KAAAAAAAAaK...", "...KAAAAAAaK....",
                "....KAAAAaK.....", ".....KAAaK......", "......KaK.......", ".......K........", "................", "................" } },
            { "eye", new[] {
                "................", "................", "................", ".....KKKKKK.....", "...KKAAAAAAKK...",
                "..KAAAKKKKAAAK..", ".KAAAKCCCCKAAAK.", "KAAAKCCKKCCKAAAK", "KAAAKCCKKCCKAAAK", ".KAAAKCCCCKAAAK.",
                "..KAAAKKKKAAAK..", "...KKAAAAAAKK...", ".....KKKKKK.....", "................", "................", "................" } },
            { "armor", new[] {
                "................", "...KKK....KKK...", "..KAAAKKKKAAAK..", ".KAAaAAAAAAaAAK.", ".KAAaAAWAAAaAAK.",
                ".KKAAAAAAAAAAKK.", "..KAAAAAAAAAAK..", "..KAAAACCAAAAK..", "..KAaAAAAAAaAK..", "..KAaAAAAAAaAK..",
                "..KAAAAAAAAAAK..", "..KaaaaaaaaaaK..", "...KKKKKKKKKK...", "................", "................", "................" } },
            { "drop", new[] {
                "................", ".......KK.......", ".......KK.......", "......KAAK......", "......KAAK......",
                ".....KAAAAK.....", ".....KAWAAK.....", "....KAWAAAAK....", "....KAAAAAAK....", "...KAAAAAAaaK...",
                "...KAAAAAAaaK...", "...KAAAAAaaaK...", "....KAAaaaaK....", ".....KKKKKK.....", "................", "................" } },
            { "snow", new[] {
                "................", ".......KK.......", "....K..KK..K....", ".....KKAAKK.....", "...K.KAWWAK.K...",
                "....KAAKKAAK....", ".KKKAAK..KAAKKK.", ".KAAWK....KWAAK.", ".KKKAAK..KAAKKK.", "....KAAKKAAK....",
                "...K.KAWWAK.K...", ".....KKAAKK.....", "....K..KK..K....", ".......KK.......", "................", "................" } },
            { "crown", new[] {
                "................", "................", "..K....K....K...", ".KCK..KCK..KCK..", ".KAK..KAK..KAK..",
                ".KAAK.KAK.KAAK..", ".KAAAKKAKKAAAK..", ".KAAAAAAAAAAAK..", ".KAWAAACCAAAAK..", ".KAAAACCCCAAAK..",
                ".KaaaaaaaaaaaK..", ".KKKKKKKKKKKKK..", "................", "................", "................", "................" } },
            { "clover", new[] {
                "................", ".....KKK.KKK....", "....KAAAKAAAK...", "....KAWAKAAaK...", ".KKKKAAAAAAaKKK.",
                "KAAAKKAAAAKKAAAK", "KAWAAAKKKKAAAAaK", "KAAAAAKCCKAAAAaK", ".KKKKKKCCKKKKKK.", "....KAAKCKAAK...",
                "....KAWKCKAaK...", ".....KKKCKKK....", ".......KCK......", "......KCK.......", "......KK........", "................" } },
            { "gem", new[] {
                "................", "................", "....KKKKKKKK....", "...KAWAAAAAaK...", "..KAWAAAAAAAaK..",
                ".KKKKKKKKKKKKKK.", ".KAAWAAAAAAAAaK.", "..KAAAAAAAAAaK..", "...KAAAAAAAaK...", "....KAAAAAaK....",
                ".....KAAAaK.....", "......KAaK......", ".......KK.......", "................", "................", "................" } },
            { "sack", new[] {
                "................", "......KKKK......", ".....KCKKCK.....", "......KCCK......", ".....KAAAAK.....",
                "....KAAAAAAK....", "...KAAWAAAAaK...", "..KAAWAAAAAAaK..", "..KAAAACCAAAaK..", "..KAAACCCCAAaK..",
                "..KAAAACCAAAaK..", "..KAAAAAAAAAaK..", "...KaaaaaaaaK...", "....KKKKKKKK....", "................", "................" } },
            { "star", new[] {
                "................", ".......KK.......", ".......KK.......", "......KAAK......", "......KAAK......",
                ".KKKKKKAWKKKKKK.", ".KAAAAAWWAAAAAK.", "..KAAAAAAAAAAK..", "...KAAAAAAAAK...", "....KAAAAAAK....",
                "...KAAAKKAAAK...", "...KAAK..KAAK...", "..KAAK....KAAK..", "..KKK......KKK..", "................", "................" } },
            { "wheel", new[] {
                "................", ".....KKKKKK.....", "....KAAKKAAK....", "...KAK.KK.KAK...", "..KAK..KK..KAK..",
                ".KAAKK.KK.KKAAK.", ".KAK.KKCCKK.KAK.", ".KKKKKCWCCKKKKK.", ".KKKKKCCCCKKKKK.", ".KAK.KKCCKK.KAK.",
                ".KAAKK.KK.KKAAK.", "..KAK..KK..KAK..", "...KAK.KK.KAK...", "....KAAKKAAK....", ".....KKKKKK.....", "................" } },
            { "mountain", new[] {
                "................", "................", "........KK......", ".......KWWK.....", "......KWWAAK....",
                ".....KAAAAAK....", "....KAAAAaAAK...", "...KAAAAaaAAKK..", "..KAAAAaaAAKAAK.", ".KAAAAaaAAKAAaAK",
                ".KAAAaaAAKAAAaAK", "KAAAaaAAKAAAAaaK", "KCCCCCCCCCCCCCCK", "KKKKKKKKKKKKKKKK", "................", "................" } },
            { "flame", new[] {
                "................", ".......K........", "......KAK.......", "......KAAK......", ".....KAAAK..K...",
                "....KAAAAAKKAK..", "...KAAACAAAAAK..", "...KAACCAAAAAK..", "..KAAACCCAACAAK.", "..KAACCWCCCCAAK.",
                "..KAACCWWCCCAAK.", "..KAAACCCCCAAK..", "...KAAAACCAAK...", "....KKAAAAKK....", "......KKKK......", "................" } },
            { "bolt", new[] {
                "................", ".........KKKK...", "........KAAAK...", ".......KAWAK....", "......KAWAK.....",
                ".....KAAAK......", "....KAAAAKKKK...", "...KKKKAAAAAK...", "......KAAAAK....", ".....KAAAK......",
                "....KAAK........", "...KAAK.........", "...KAK..........", "...KK...........", "................", "................" } },
            { "arrow", new[] {
                "................", "...........KKKK.", "..........KWAAK.", "...........KAAK.", "..........KAKAK.",
                ".........KAK.KK.", "........KHK.....", ".......KHK......", "......KHK.......", ".....KHK........",
                "..KK.KHK........", "..KCKHK.........", "...KCK..........", "..KCKCK.........", "..KK.KK.........", "................" } },
            { "book", new[] {
                "................", "..KKKKKK.KKKKK..", ".KAAAAAAKAAAAAK.", ".KAWWWAAKAWWWAK.", ".KAAAAAAKAAAAAK.",
                ".KAWWWAAKAWWWAK.", ".KAAAAAAKAAAAAK.", ".KAWWWAAKACCCAK.", ".KAAAAAAKACWCAK.", ".KAWWWAAKACCCAK.",
                ".KAAAAAAKAAAAAK.", ".KaaaaaaKaaaaaK.", "..KKKKKKKKKKKK..", "................", "................", "................" } },
            { "hourglass", new[] {
                "................", "...KKKKKKKKKK...", "...KAAAAAAAAK...", "....KCCCCCCK....", "....KWCCCCCK....",
                ".....KCCCCK.....", "......KCCK......", ".......KK.......", "......K..K......", ".....K.CC.K.....",
                "....K.CCCC.K....", "....KCCCCCCK....", "...KAAAAAAAAK...", "...KKKKKKKKKK...", "................", "................" } },
            { "skull", new[] {
                "................", ".....KKKKKK.....", "....KAAAAAAK....", "...KAWAAAAAAK...", "...KAAAAAAAAK...",
                "...KAKKAAKKAK...", "...KAKKAAKKAK...", "...KAAAKKAAAK...", "....KAAAAAAK....", ".....KAKAKAK....",
                ".....KAKAKAK....", "......KKKKK.....", "................", "................", "................", "................" } },
            { "fist", new[] {
                "................", "................", ".....KKKKKKK....", "....KAKAKAKAK...", "....KAKAKAKAK...",
                "...KKAKAKAKAK...", "..KAAKKKKKKKAK..", "..KAWAAAAAAAAK..", "..KAAAAAAAAAAK..", "...KAAAAAAAAK...",
                "....KAAAAAAK....", "....KCCCCCCK....", "....KCCCCCCK....", "....KKKKKKKK....", "................", "................" } },
            { "bow", new[] {
                "....KKKK........", "...KAAAAK.......", "..KAK..KKK......", "..KK.....KK.....", ".KAK......KK....",
                ".KAK.......KK...", ".KAK........KK..", ".KCK.........WK.", ".KAK........KK..", ".KAK.......KK...",
                ".KAK......KK....", "..KK.....KK.....", "..KAK..KKK......", "...KAAAAK.......", "....KKKK........", "................" } },
            { "staff", new[] {
                "...........KKK..", "..........KCWCK.", "..........KCCCK.", "...........KKK..", "..........KAK...",
                ".........KAK....", "........KAK.....", ".......KAK......", "......KAK.......", ".....KAK........",
                "....KAK.........", "...KAK..........", "..KAK...........", ".KaK............", ".KK.............", "................" } },
            { "potion", new[] {
                "................", "......KKKK......", "......KCCK......", ".......KK.......", "......KAAK......",
                ".....KAAAAK.....", "....KAWAAAAK....", "...KAWAAAAAAK...", "...KAAAAAAAAK...", "...KAAAAAAAaK...",
                "...KAAAAAAaaK...", "....KAAAAaaK....", ".....KKKKKK.....", "................", "................", "................" } },
        };

        // id -> (piktogram, ana renk, vurgu rengi) (hex, # olmadan)
        private static readonly Dictionary<string, (string picto, string main, string accent)> Icons =
            new Dictionary<string, (string, string, string)>
        {
            // Yükseltmeler
            { "up_Earnings", ("pick", "E0B040", "8A5A30") },
            { "up_Safety", ("beam", "A0703C", "707070") },
            { "up_Insurance", ("shield", "4A90E0", "FFD23A") },
            { "up_FindChance", ("lens", "B0B4BC", "8FE0FF") },
            { "up_Luck", ("lantern", "C89A50", "FFC040") },
            { "up_FindValue", ("scale", "E0C040", "40C0A0") },

            // Yetenekler: Cüce Madenci
            { "sturdy_pick", ("pick", "B0B4BC", "8A5A30") },
            { "stone_sense", ("mountain", "9A9AA8", "6A5A48") },
            { "gold_vein", ("coins", "FFD23A", "C88A1A") },
            { "deep_instinct", ("eye", "70C070", "205020") },
            { "brace_master", ("beam", "C8A040", "8A8F98") },
            { "rubble_hunter", ("sack", "A08060", "C0C0C0") },
            { "mountain_heart", ("heart", "C87A3A", "FFD23A") },
            // Kılıç Ustası
            { "keen_edge", ("sword", "D0D5DC", "C04040") },
            { "thick_hide", ("armor", "8A5A30", "C89A50") },
            { "deadly_eye", ("eye", "E04040", "300808") },
            { "armor_master", ("shield", "A8AEB8", "C8A040") },
            { "blood_drinker", ("drop", "D02020", "000000") },
            { "cold_blood", ("snow", "90D8FF", "FFFFFF") },
            { "kingslayer", ("crown", "E04040", "FFD23A") },
            // Ejder Avcısı
            { "lucky_birth", ("clover", "40C060", "8A5A30") },
            { "jeweler_eye", ("gem", "40B0FF", "FFFFFF") },
            { "plunderer", ("sack", "C89A50", "FFD23A") },
            { "haggler", ("coins", "D8DCE4", "4080E0") },
            { "glimmer", ("star", "FFE060", "FFFFFF") },
            { "fortune_wheel", ("wheel", "C88A1A", "E03030") },
            { "dragon_eye", ("eye", "E8C040", "1E6A30") },

            // Büyüler: Savaşçı
            { "w_cleave", ("sword", "FF9A2E", "C04040") },
            { "w_iron_will", ("heart", "E04040", "FFD23A") },
            { "w_shoulder", ("mountain", "B0A090", "8A5A30") },
            { "w_warcry", ("fist", "E0C040", "C04040") },
            { "w_greed", ("coins", "FFD23A", "E03030") },
            { "w_titan", ("shield", "FFD23A", "C04040") },
            // Büyücü
            { "m_fireball", ("flame", "FF6A20", "FFE060") },
            { "m_foresight", ("eye", "B060FF", "FFFFFF") },
            { "m_alchemy", ("potion", "FFD23A", "B060FF") },
            { "m_frost", ("snow", "70C8FF", "FFFFFF") },
            { "m_arcane_eye", ("star", "C080FF", "FFFFFF") },
            { "m_timebend", ("hourglass", "A0A8D0", "B060FF") },
            // Okçu
            { "a_volley", ("arrow", "C8CDD4", "40C060") },
            { "a_scout", ("lens", "8A5A30", "90F0A0") },
            { "a_eagle", ("eye", "FFD23A", "8A5A30") },
            { "a_pathfinder", ("wheel", "40A060", "FFD23A") },
            { "a_hunt", ("sack", "40A060", "FFD23A") },
            { "a_deadeye", ("skull", "E8E0D0", "E04040") },

            // Sınıflar
            { "class_Warrior", ("sword", "D0D5DC", "C04040") },
            { "class_Mage", ("staff", "8A5A30", "B060FF") },
            { "class_Archer", ("bow", "8A5A30", "40C060") },

            // Kadim yükseltmeler
            { "pr_AncientGold", ("coins", "FFD23A", "C0C0E0") },
            { "pr_AncientLuck", ("star", "C0C0FF", "FFFFFF") },
            { "pr_AncientInsight", ("book", "B060FF", "FFE060") },
            { "pr_AncientMight", ("fist", "E04040", "FFD23A") },
            { "pr_AncientWealth", ("sack", "E8C040", "30C040") },
        };

        public static Sprite Upgrade(UpgradeType type) => Get("up_" + type);
        public static Sprite Talent(string id) => Get(id);
        public static Sprite Spell(string id) => Get(id);
        public static Sprite Class(HeroClass c) => Get("class_" + c);
        public static Sprite Prestige(PrestigeType type) => Get("pr_" + type);

        /// <summary>İkon. Bilinmeyen id için gri yıldız (veri hatası belli olsun ama oyun çökmesin).</summary>
        public static Sprite Get(string id)
        {
            var icon = Icons.TryGetValue(id, out var found) ? found : ("star", "808080", "A0A0A0");
            Color main = Hex(icon.Item2);
            var palette = new Dictionary<char, Color32>
            {
                { 'K', new Color32(0x14, 0x10, 0x10, 0xFF) },
                { 'W', new Color32(0xFF, 0xFF, 0xF8, 0xFF) },
                { 'A', main },
                { 'a', main * 0.62f },
                { 'C', Hex(icon.Item3) },
                { 'H', new Color32(0x8A, 0x5A, 0x30, 0xFF) },
            };
            return PixelArt.FromRows($"icon_{id}", Pictos[icon.Item1], palette);
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }
    }
}
