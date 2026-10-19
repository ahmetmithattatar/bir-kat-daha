using System.Collections.Generic;
using RiskLoop.Core;
using UnityEngine;

namespace RiskLoop.Game.Art
{
    /// <summary>
    /// Ekipman görselleri. Silahlar: 10 şablon (kılıç, hançer, balta, gürz, mızrak, yay, asa, çekiç, tırpan, sopa),
    /// her silahın kendi metal/vurgu rengi. Zırh ve takılar: yuva başına bir şablon, malzemeye göre boyanır.
    /// Harfler: K kontur, A ana (metal/malzeme), a gölge, H sap, h koyu sap, C vurgu (taş/süs), W parıltı.
    /// </summary>
    public static class GearArt
    {
        // ---------------- Şablonlar ----------------

        private static readonly Dictionary<string, string[]> Templates = new Dictionary<string, string[]>
        {
            { "sword", new[] {
                "..............KK", ".............KWK", "............KWAK", "...........KWAK.",
                "..........KWAK..", ".........KWAK...", "........KWAK....", ".......KWAK.....",
                "..KK..KWAK......", "..KCKKWAK.......", "...KCKAK........", "....KHCK........",
                "...KHK.KK.......", "..KHK...........", ".KCK............", "..K............." } },
            { "dagger", new[] {
                "................", "................", "................", "................",
                "................", "..........KK....", ".........KWK....", "........KWAK....",
                ".......KWAK.....", "..KK..KWAK......", "..KCKKWAK.......", "...KCKAK........",
                "....KHCK........", "...KHK.KK.......", "..KCK...........", "................" } },
            { "axe", new[] {
                "................", "........KKK.....", ".......KAAAK....", "......KAWAAK....",
                ".....KAWAAAK....", "....KKAAAAK.....", "....KHKAAK......", "...KHK.KK.......",
                "..KHK...........", "..KHK...........", ".KHK............", ".KHK............",
                "KHK.............", "KhK.............", "KK..............", "................" } },
            { "mace", new[] {
                "...........KKK..", "..........KAAAK.", ".........KACAWAK", ".........KAAAAAK",
                "..........KAAAK.", ".........KHKKK..", "........KHK.....", ".......KHK......",
                "......KHK.......", ".....KHK........", "....KHK.........", "...KHK..........",
                "..KhK...........", ".KhK............", ".KK.............", "................" } },
            { "spear", new[] {
                "..............KK", ".............KWK", "............KAAK", "...........KAAK.",
                "..........KCKK..", ".........KHK....", "........KHK.....", ".......KHK......",
                "......KHK.......", ".....KHK........", "....KHK.........", "...KHK..........",
                "..KHK...........", ".KhK............", ".KK.............", "................" } },
            { "bow", new[] {
                "....KKKK........", "...KHHHHK.......", "..KHK..KKK......", "..KK.....KK.....",
                ".KHK......KK....", ".KHK.......KK...", ".KHK........KK..", ".KCK.........WK.",
                ".KHK........KK..", ".KHK.......KK...", ".KHK......KK....", "..KK.....KK.....",
                "..KHK..KKK......", "...KHHHHK.......", "....KKKK........", "................" } },
            { "staff", new[] {
                "...........KKK..", "..........KCWCK.", "..........KCCCK.", "...........KKK..",
                "..........KHK...", ".........KHK....", "........KHK.....", ".......KHK......",
                "......KHK.......", ".....KHK........", "....KHK.........", "...KHK..........",
                "..KHK...........", ".KhK............", ".KK.............", "................" } },
            { "hammer", new[] {
                "........KKKKK...", ".......KAAAAAK..", "......KAWAAAAAK.", "......KAAAAAAaK.",
                ".......KAAAAaK..", "......KHKKKKK...", ".....KHK........", "....KHK.........",
                "...KHK..........", "...KHK..........", "..KHK...........", "..KHK...........",
                ".KhK............", ".KhK............", ".KK.............", "................" } },
            { "scythe", new[] {
                "....KKKKKKK.....", "...KWAAAAAAKK...", "..KWAKKKKKAAAK..", "..KAK.....KKAAK.",
                "...K........KAK.", "............KHK.", "...........KHK..", "..........KHK...",
                ".........KHK....", "........KHK.....", ".......KHK......", "......KHK.......",
                ".....KHK........", "....KhK.........", "....KK..........", "................" } },
            { "club", new[] {
                "...........KK...", "..........KHHK..", ".........KHCHHK.", ".........KHHHCK.",
                "........KHCHHK..", ".......KHHHK....", "......KHHK......", ".....KHK........",
                "....KHK.........", "...KHK..........", "..KhK...........", ".KhK............",
                ".KK.............", "................", "................", "................" } },

            { "pickaxe", new[] {
                "......aaaa......", "....aaAAAAaa....", "...aAAaaaaAAa...", "..aAa..hh..aAa..",
                "..aa...HH...aa..", ".aa....HH....aa.", ".a.....HH.....a.", ".......HH.......",
                ".......HH.......", ".......HH.......", ".......HH.......", ".......HH.......",
                ".......HH.......", ".......hh.......", ".......hh.......", "................" } },

            // Zırh ve takılar
            { "helmet", new[] {
                "................", "................", ".....KKKKKK.....", "....KAAWAAAK....",
                "...KAAWAAAAAK...", "..KAAAAAAAAAAK..", "..KAAAAAAAAAAK..", "..KAKKKKKKKKAK..",
                "..KAKCCCCCCKAK..", "..KAK......KAK..", "..KaK......KaK..", "..KKK......KKK..",
                "................", "................", "................", "................" } },
            { "armor", new[] {
                "................", "...KKK....KKK...", "..KAAAKKKKAAAK..", ".KAAaAAAAAAaAAK.",
                ".KAAaAAWAAAaAAK.", ".KKAAAAAAAAAAKK.", "..KAAAAAAAAAAK..", "..KAAAACCAAAAK..",
                "..KAaAAAAAAaAK..", "..KAaAAAAAAaAK..", "..KAAAAAAAAAAK..", "..KaaaaaaaaaaK..",
                "...KKKKKKKKKK...", "................", "................", "................" } },
            { "pants", new[] {
                "................", "...KKKKKKKKKK...", "...KCCCCCCCCK...", "...KAAAAAAAAK...",
                "...KAAAWAAAAK...", "...KAAAAAAAAK...", "...KAAAKKAAAK...", "...KAAAKKAAAK...",
                "...KAAK..KAAK...", "...KAAK..KAAK...", "...KAaK..KaAK...", "...KAaK..KaAK...",
                "...KKKK..KKKK...", "................", "................", "................" } },
            { "gloves", new[] {
                "................", "................", "......K.K.K.....", ".....KAKAKAK....",
                ".....KAKAKAK....", "....KKAAAAAAK...", "...KAKAAAAAAK...", "...KAAAAWAAAK...",
                "....KAAAAAAAK...", ".....KAAAAAK....", ".....KCCCCCK....", ".....KCCCCCK....",
                ".....KKKKKKK....", "................", "................", "................" } },
            { "boots", new[] {
                "................", "................", "....KKKKK.......", "....KCCCK.......",
                "....KAAAK.......", "....KAAAK.......", "....KAWAK.......", "....KAAAK.......",
                "....KAAAKKKKK...", "....KAAAAAAAAK..", "....KAAAAAAAAAK.", "....KaaaaaaaaaK.",
                "....KKKKKKKKKKK.", "................", "................", "................" } },
            { "necklace", new[] {
                "................", "...KK......KK...", "..KAK......KAK..", "..KAK......KAK..",
                "...KAK....KAK...", "...KAK....KAK...", "....KAK..KAK....", ".....KAKKAK.....",
                "......KCCK......", ".....KCWCCK.....", ".....KCCCCK.....", "......KCCK......",
                ".......KK.......", "................", "................", "................" } },
            { "ring", new[] {
                "................", "................", "......KKKK......", ".....KCWCCK.....",
                ".....KCCCCK.....", "......KKKK......", ".....KAAAAK.....", "....KAK..KAK....",
                "...KAK....KAK...", "...KAK....KAK...", "...KAK....KAK...", "....KAK..KAK....",
                ".....KAAAAK.....", "......KKKK......", "................", "................" } },
        };

        // ---------------- Silah görünümleri: (şablon, metal, metal gölge, vurgu) ----------------

        private static readonly Dictionary<string, (string template, string metal, string shade, string accent)> Weapons =
            new Dictionary<string, (string, string, string, string)>
        {
            { "dagger", ("dagger", "B8B0A8", "7A7068", "8B5A2B") },
            { "shortsword", ("sword", "C8CDD4", "8A8F98", "C04040") },
            { "spiked_club", ("club", "C8CDD4", "8A8F98", "A0A0A0") },
            { "hand_axe", ("axe", "C8CDD4", "8A8F98", "C04040") },
            { "war_mace", ("mace", "C8CDD4", "8A8F98", "E0C040") },
            { "spear", ("spear", "C8CDD4", "8A8F98", "C04040") },
            { "longbow", ("bow", "C8CDD4", "8A8F98", "40C060") },
            { "mage_staff", ("staff", "C8CDD4", "8A8F98", "8060FF") },
            { "katana", ("sword", "E0E4EC", "9AA0AA", "C03030") },
            { "great_axe", ("axe", "D0D5DC", "7A808A", "E0C040") },
            { "war_hammer", ("hammer", "B0B8C4", "6A707A", "E0C040") },
            { "soul_scythe", ("scythe", "A0C0D0", "607080", "60E0FF") },
            { "rune_sword", ("sword", "90C8FF", "4A80C0", "40E0FF") },
            { "crystal_staff", ("staff", "C8CDD4", "8A8F98", "80E0FF") },
            { "dragon_spear", ("spear", "3AA050", "1E6A30", "E8C040") },
            { "shadow_blades", ("dagger", "5A5A70", "30303A", "A060FF") },
            { "volcanic_greatsword", ("sword", "E06A2A", "902A10", "FFD040") },
            { "void_scythe", ("scythe", "8A5AC8", "4A2A80", "C080FF") },
        };

        // ---------------- Kazma başı renkleri: (metal, metal gölge) ----------------

        private static readonly Dictionary<string, (string metal, string shade)> Pickaxes =
            new Dictionary<string, (string, string)>
        {
            { "rusty_pick", ("A87A5A", "6A4A34") },
            { "iron_pick", ("B0B4BC", "6A6E76") },
            { "steel_pick", ("D0D5DC", "8A8F98") },
            { "dwarf_pick", ("E0B040", "9A7020") },
            { "mithril_pick", ("C8F0FF", "70B0D0") },
            { "runic_pick", ("80C8FF", "3A70B0") },
            { "dragonbone_pick", ("F0E8D0", "B0A080") },
            { "star_pick", ("FFFFFF", "C0B0FF") },
        };

        // ---------------- Malzeme renkleri: (ana, gölge, vurgu) ----------------

        private static readonly Dictionary<string, (string main, string shade, string accent)> Materials =
            new Dictionary<string, (string, string, string)>
        {
            { "cloth", ("C8B89A", "8A7A60", "6A5A48") },
            { "leather", ("8A5A30", "5C3A1A", "C89A50") },
            { "chain", ("A8AEB8", "6A707A", "5A4A3A") },
            { "plate", ("D0D5DC", "8A8F98", "C8A040") },
            { "mithril", ("B8E0F0", "6AA0C0", "FFFFFF") },
            { "dragonscale", ("3AA050", "1E6A30", "E8C040") },
            { "void", ("6A3AA0", "3A1A60", "C080FF") },
            { "copper", ("C87A3A", "8A4A1A", "40C0A0") },
            { "silver", ("D8DCE4", "9AA0AA", "4080E0") },
            { "gold", ("FFD23A", "C88A1A", "E03030") },
            { "moonstone", ("E0E8FF", "A0A8D0", "B0D0FF") },
            { "dragoneye", ("E8C040", "A07A1A", "30C040") },
            { "star", ("FFFFFF", "C0C0E0", "FFE060") },
        };

        /// <summary>Malzemenin renkleri (hex, # olmadan). Karakter katmanları da bunu kullanır.</summary>
        public static bool TryGetMaterial(string materialId, out string main, out string shade, out string accent)
        {
            if (materialId != null && Materials.TryGetValue(materialId, out var c))
            {
                main = c.Item1;
                shade = c.Item2;
                accent = c.Item3;
                return true;
            }
            main = "A0A0A0";
            shade = "606060";
            accent = "C0C0C0";
            return false;
        }

        /// <summary>Eşyanın ikonu.</summary>
        public static Sprite Icon(GearItem item)
        {
            if (item.slot == GearSlot.Weapon) return WeaponSprite(item.baseId);
            if (item.slot == GearSlot.Pickaxe) return PickaxeSprite(item.baseId);

            string material = FantasyTexts.MaterialId(item);
            var colors = Materials.TryGetValue(material, out var c) ? c : ("A0A0A0", "606060", "C0C0C0");
            string template = SlotTemplate(item.slot);
            return Build($"gear_{template}_{material}", Templates[template], colors.Item1, colors.Item2, colors.Item3);
        }

        /// <summary>Silah görseli (elde tutulan silah için de kullanılır).</summary>
        public static Sprite WeaponSprite(string weaponId)
        {
            var w = Weapons.TryGetValue(weaponId, out var found) ? found : ("sword", "C8CDD4", "8A8F98", "C04040");
            return Build($"weapon_{weaponId}", Templates[w.Item1], w.Item2, w.Item3, w.Item4);
        }

        /// <summary>Kazma görseli (elde tutulan kazma için de kullanılır). null = başlangıç (paslı) kazma.</summary>
        public static Sprite PickaxeSprite(string pickaxeId)
        {
            string id = pickaxeId ?? "rusty_pick";
            var c = Pickaxes.TryGetValue(id, out var found) ? found : ("B0B4BC", "6A6E76");
            return Build($"pickaxe_{id}", Templates["pickaxe"], c.Item1, c.Item2, c.Item1);
        }

        /// <summary>Boş yuva için soluk şablon (ekipman ekranında).</summary>
        public static Sprite EmptySlotSprite(GearSlot slot)
        {
            string template = slot == GearSlot.Weapon ? "sword" : slot == GearSlot.Pickaxe ? "pickaxe" : SlotTemplate(slot);
            return Build($"empty_{template}", Templates[template], "3A3A44", "2A2A32", "3A3A44");
        }

        private static string SlotTemplate(GearSlot slot)
        {
            switch (slot)
            {
                case GearSlot.Helmet: return "helmet";
                case GearSlot.Armor: return "armor";
                case GearSlot.Pants: return "pants";
                case GearSlot.Gloves: return "gloves";
                case GearSlot.Boots: return "boots";
                case GearSlot.Necklace: return "necklace";
                case GearSlot.Ring: return "ring";
                default: return "sword";
            }
        }

        private static Sprite Build(string key, string[] rows, string main, string shade, string accent)
        {
            var palette = new Dictionary<char, Color32>
            {
                { 'K', new Color32(0x14, 0x10, 0x10, 0xFF) },
                { 'W', new Color32(0xFF, 0xFF, 0xF8, 0xFF) },
                { 'A', Hex(main) },
                { 'a', Hex(shade) },
                { 'C', Hex(accent) },
                { 'H', new Color32(0x8A, 0x5A, 0x30, 0xFF) },
                { 'h', new Color32(0x5C, 0x3A, 0x1A, 0xFF) },
            };
            return PixelArt.FromRows(key, rows, palette);
        }

        private static Color32 Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }
    }
}
