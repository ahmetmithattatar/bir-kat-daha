using System.Collections.Generic;
using RiskLoop.Core;
using UnityEngine;

namespace RiskLoop.Game.Art
{
    /// <summary>
    /// Oyunun tüm pixel art ikonları, 16x16 metin ızgarası olarak. Tema burada yaşar (kazı).
    ///
    /// Bir ikonu değiştirmek için ızgaradaki harfleri değiştirmen yeterli. Harf -> renk tablosu: Palette.
    ///   . saydam  K kontur(siyah)  W beyaz parıltı
    ///   Y altın   O koyu altın     B kahve      b koyu kahve   T kâğıt   t koyu kâğıt
    ///   G gri     g koyu gri       L açık gri   R kırmızı      E yeşil   e koyu yeşil
    ///   U mavi    u koyu mavi
    ///
    /// Çeşitlilik: Hazine ve Kese'nin nadirliğe göre farklı çizimleri var (para -> külçe -> sandık -> taç).
    /// Kristalin rengi nadirlik rengidir (gri tonlarda çizilip Image.color ile boyanır).
    /// </summary>
    public static class SpriteLibrary
    {
        private static readonly Dictionary<char, Color32> Palette = new Dictionary<char, Color32>
        {
            { 'K', new Color32(0x1A, 0x14, 0x10, 0xFF) },
            { 'W', new Color32(0xFF, 0xFF, 0xF0, 0xFF) },
            { 'Y', new Color32(0xFF, 0xD2, 0x3A, 0xFF) },
            { 'O', new Color32(0xC8, 0x8A, 0x1A, 0xFF) },
            { 'B', new Color32(0x9A, 0x62, 0x2E, 0xFF) },
            { 'b', new Color32(0x5C, 0x3A, 0x1A, 0xFF) },
            { 'T', new Color32(0xEC, 0xD9, 0xA0, 0xFF) },
            { 't', new Color32(0xB8, 0x9E, 0x62, 0xFF) },
            { 'G', new Color32(0x8A, 0x8F, 0x98, 0xFF) },
            { 'g', new Color32(0x4A, 0x4E, 0x55, 0xFF) },
            { 'L', new Color32(0xD0, 0xD5, 0xDC, 0xFF) },
            { 'R', new Color32(0xE0, 0x30, 0x28, 0xFF) },
            { 'E', new Color32(0x3C, 0xB4, 0x4A, 0xFF) },
            { 'e', new Color32(0x1E, 0x6E, 0x2A, 0xFF) },
            { 'U', new Color32(0x4A, 0x8B, 0xE5, 0xFF) },
            { 'u', new Color32(0x22, 0x4A, 0x90, 0xFF) },
        };

        // Kristal: gri tonlarda (sonra nadirlik rengiyle boyanır)
        private static readonly Dictionary<char, Color32> GemPalette = new Dictionary<char, Color32>
        {
            { 'K', new Color32(0x20, 0x20, 0x28, 0xFF) },
            { 'W', new Color32(0xFF, 0xFF, 0xFF, 0xFF) },
            { 'U', new Color32(0xD8, 0xD8, 0xD8, 0xFF) },
            { 'u', new Color32(0x90, 0x90, 0x90, 0xFF) },
        };

        // ---------------- Çizimler ----------------

        private static readonly string[] Pickaxe =
        {
            "......gggg......",
            "....ggLLLLgg....",
            "...gLLggggLLg...",
            "..gLg..bb..gLg..",
            "..gg...BB...gg..",
            ".gg....BB....gg.",
            ".g.....BB.....g.",
            ".......BB.......",
            ".......BB.......",
            ".......BB.......",
            ".......BB.......",
            ".......BB.......",
            ".......BB.......",
            ".......bb.......",
            ".......bb.......",
            "................",
        };

        private static readonly string[] Coins =
        {
            "................",
            "................",
            "................",
            "................",
            "................",
            "......KKKK......",
            ".....KYWYYK.....",
            ".....KOOOOK.....",
            "..KKKKYYYYKKKK..",
            ".KYWYKOOOOKYWYK.",
            ".KOOOKYYYYKOOOK.",
            ".KYYYKOOOOKYYYK.",
            ".KOOOKYYYYKOOOK.",
            ".KYYYKOOOOKYYYK.",
            ".KOOOKKKKKKOOOK.",
            "..KKK......KKK..",
        };

        private static readonly string[] GoldBar =
        {
            "................",
            "................",
            "................",
            "................",
            "................",
            "....KKKKKKKKK...",
            "...KYWWYYYYYOK..",
            "..KYWYYYYYYYOOK.",
            ".KYYYYYYYYYYOOOK",
            ".KOOOOOOOOOOKOOK",
            ".KOOOOOOOOOOKOK.",
            ".KOOOOOOOOOOKK..",
            "..KKKKKKKKKKK...",
            "................",
            "................",
            "................",
        };

        private static readonly string[] Chest =
        {
            "................",
            "................",
            "...KKKKKKKKKK...",
            "..KBBBBBBBBBBK..",
            ".KBbBBBBBBBBbBK.",
            ".KBbBBBBBBBBbBK.",
            ".KKKKKKYYKKKKKK.",
            ".KYYYYKYWKYYYYK.",
            ".KBbBBKOOKBBbBK.",
            ".KBbBBBKKBBBbBK.",
            ".KBbBBBBBBBBbBK.",
            ".KBbBBBBBBBBbBK.",
            ".KYYYYYYYYYYYYK.",
            ".KKKKKKKKKKKKKK.",
            "................",
            "................",
        };

        private static readonly string[] Crown =
        {
            "................",
            "................",
            "................",
            "..K....KK....K..",
            ".KYK..KYYK..KYK.",
            ".KYYK.KYYK.KYYK.",
            ".KYYYKYYYYKYYYK.",
            ".KYYYYYRRYYYYYK.",
            ".KYWYYRRRRYYYYK.",
            ".KYYYYYRRYYYYYK.",
            ".KOOOOOOOOOOOOK.",
            ".KYUYYYRYYYUYYK.",
            ".KOOOOOOOOOOOOK.",
            "..KKKKKKKKKKKK..",
            "................",
            "................",
        };

        private static readonly string[] Pouch =
        {
            "................",
            "................",
            "......KKKK......",
            ".....KTTTTK.....",
            "......KRRK......",
            ".....KBBBBK.....",
            "....KBBBBBBK....",
            "...KBBBYYBBBK...",
            "..KBBBYYYYBBBK..",
            "..KBBBBYYBBBBK..",
            "..KBBBBBBBBBBK..",
            "..KbBBBBBBBBbK..",
            "...KbbBBBBbbK...",
            "....KKKKKKKK....",
            "................",
            "................",
        };

        private static readonly string[] Sack =
        {
            "................",
            ".....KKKKKK.....",
            "....KTTTTTTK....",
            ".....KRRRRK.....",
            "....KBBBBBBK....",
            "...KBBBBBBBBK...",
            "..KBBBBYYBBBBK..",
            ".KBBBBYYYYBBBBK.",
            ".KBBBBBYYBBBBBK.",
            ".KBBBBBBBBBBBBK.",
            ".KbBBBBBBBBBBbK.",
            "..KbbBBBBBBbbK..",
            "...KKKKKKKKKK...",
            "..KK....KKKK....",
            ".KYWK..KYYWYK...",
            "..KK....KKKK....",
        };

        private static readonly string[] OreVein =
        {
            "................",
            "................",
            "....KKKKKKK.....",
            "...KgGGGGGgK....",
            "..KGGYGGGGGGK...",
            ".KGGGYYGGLGGGK..",
            ".KGGGGYYGGGGGK..",
            ".KGLGGGYGGGYGGK.",
            ".KGGGGGGYGGYYGK.",
            ".KGGYYGGYYYGGGK.",
            ".KgGGYWGGGGGGgK.",
            "..KgGGGGGLGGgK..",
            "...KggggggggK...",
            "....KKKKKKKK....",
            "................",
            "................",
        };

        private static readonly string[] SolidRock =
        {
            "................",
            "................",
            ".....KKKKKK.....",
            "...KKLLGGGGKK...",
            "..KLLGGGGGGGgK..",
            ".KLGGGGUUGGGGgK.",
            ".KGGGGUUUUGGGgK.",
            ".KGGGGUWUUGGGgK.",
            ".KGGGGUUUUGGGgK.",
            ".KGGGGGUUGGGggK.",
            ".KgGGGGGGGGGggK.",
            "..KggGGGGGgggK..",
            "...KKgggggggK...",
            ".....KKKKKKK....",
            "................",
            "................",
        };

        private static readonly string[] Helmet =
        {
            "................",
            "................",
            "................",
            "......KKKK......",
            "....KKYYYYKK....",
            "...KYYYWWYYYK...",
            "..KYYYKLLKYYYK..",
            "..KYYKLWWLKYYK..",
            "..KYYKLWWLKYYK..",
            "..KYYYKLLKYYYK..",
            ".KKYYYYYYYYYYKK.",
            "KOOOOOOOOOOOOOOK",
            ".KKKKKKKKKKKKKK.",
            "................",
            "................",
            "................",
        };

        private static readonly string[] Clover =
        {
            "................",
            "................",
            "....KKK..KKK....",
            "...KEEEKKEEEK...",
            "...KEeEEEEeEK...",
            "...KEEEWEEEEK...",
            "....KKEEEEKK....",
            "...KEEEEEEEEK...",
            "...KEeEEEEeEK...",
            "...KEEEKKEEEK...",
            "....KKKKeKKK....",
            ".........eK.....",
            "..........eK....",
            "...........K....",
            "................",
            "................",
        };

        private static readonly string[] Map =
        {
            "................",
            "................",
            ".KKKKKKKKKKKKKK.",
            "KtTTTTTTTTTTTTtK",
            "KtTTTRTTTTTTTTtK",
            "KtTTTTRTTRRTTTtK",
            "KtTTTTTRRTTRTTtK",
            "KtTTEETTTTTTRTtK",
            "KtTEEEETTTTRTRtK",
            "KtTTEETTTTTTRTtK",
            "KtTTTTTTTTTRTRtK",
            "KtTUUTTTTTTTTTtK",
            "KtUUUUTTTTTTTTtK",
            "KtTTTTTTTTTTTTtK",
            ".KKKKKKKKKKKKKK.",
            "................",
        };

        private static readonly string[] Gem =
        {
            "................",
            "................",
            "................",
            "....KKKKKKKK....",
            "...KWUUWUUUUK...",
            "..KWUUWUUUUUuK..",
            ".KKKKKKKKKKKKKK.",
            ".KWUUUUUUUUUUuK.",
            "..KUUUUUUUUUuK..",
            "...KUUUUUUUuK...",
            "....KUUUUUuK....",
            ".....KUUUuK.....",
            "......KUuK......",
            ".......KK.......",
            "................",
            "................",
        };

        private static readonly string[] Idol =
        {
            "................",
            "......KKKK......",
            ".....KEEEEK.....",
            "....KEeEEeEK....",
            "....KERReREK....",
            "....KEEEEEEK....",
            "....KEeKKeEK....",
            ".....KEEEEK.....",
            "....KKEEEEKK....",
            "...KEEKEEKEEK...",
            "...KEeKEEKeEK...",
            "....KKEEEEKK....",
            ".....KEeeEK.....",
            "....KKEEEEKK....",
            "...KOOOOOOOOK...",
            "...KKKKKKKKKK...",
        };

        // ---------------- Erişim ----------------

        public static Sprite PickaxeSprite => PixelArt.FromRows("pickaxe", Pickaxe, Palette);
        public static Sprite HelmetSprite => PixelArt.FromRows("helmet", Helmet, Palette);

        /// <summary>Bulgunun ikonu. Hazine ve Kese nadirlikle görünüş değiştirir.</summary>
        public static Sprite FindIcon(FindType type, int tier)
        {
            switch (type)
            {
                case FindType.Treasure:
                    if (tier >= 7) return PixelArt.FromRows("crown", Crown, Palette);
                    if (tier >= 5) return PixelArt.FromRows("chest", Chest, Palette);
                    if (tier >= 3) return PixelArt.FromRows("goldbar", GoldBar, Palette);
                    return PixelArt.FromRows("coins", Coins, Palette);
                case FindType.SafeCache:
                    return tier >= 4 ? PixelArt.FromRows("sack", Sack, Palette) : PixelArt.FromRows("pouch", Pouch, Palette);
                case FindType.ProfitBoost: return PixelArt.FromRows("ore", OreVein, Palette);
                case FindType.Safety: return PixelArt.FromRows("solidrock", SolidRock, Palette);
                case FindType.Shield: return HelmetSprite;
                case FindType.LuckCharm: return PixelArt.FromRows("clover", Clover, Palette);
                case FindType.FindStreak: return PixelArt.FromRows("map", Map, Palette);
                case FindType.Multiplier: return PixelArt.FromRows("gem", Gem, GemPalette);
                case FindType.CursedTreasure: return PixelArt.FromRows("idol", Idol, Palette);
                case FindType.Gear: return GearArt.WeaponSprite("shortsword");
                default: return PixelArt.FromRows("coins", Coins, Palette);
            }
        }

        /// <summary>İkonun Image.color'ı. Kristal nadirlik rengini alır, diğerleri kendi renginde (beyaz = boyama yok).</summary>
        public static Color FindIconTint(FindType type, int tier)
        {
            return type == FindType.Multiplier ? RarityStyle.TierColor(tier) : Color.white;
        }

        /// <summary>Bulgunun ikonu; ekipmansa eşyanın kendi görseli.</summary>
        public static Sprite FindIcon(FindResult find)
        {
            if (find.Type == FindType.Gear && find.Item != null) return GearArt.Icon(find.Item);
            return FindIcon(find.Type, find.Tier);
        }

        public static Color FindIconTint(FindResult find) => FindIconTint(find.Type, find.Tier);
    }
}
