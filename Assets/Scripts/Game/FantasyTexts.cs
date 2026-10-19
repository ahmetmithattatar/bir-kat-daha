using System.Collections.Generic;
using System.Text;
using RiskLoop.Core;

namespace RiskLoop.Game
{
    /// <summary>
    /// Fantastik içerik metinleri: canavar, boss, silah, malzeme, yuva ve stat adları.
    /// Core sadece id bilir ("katana", "chain_gloves", "goblin_chief"); ekranda ne yazacağı burada.
    /// Yeni içerik eklerken DefaultContent'teki id ile buraya bir satır ekle.
    /// </summary>
    public static class FantasyTexts
    {
        private static readonly Dictionary<string, string> MonsterNames = new Dictionary<string, string>
        {
            // Toprak
            { "cave_rat", "Mağara Faresi" }, { "slime", "Balçık" }, { "bat", "Mağara Yarasası" }, { "goblin", "Goblin Hırsız" },
            // Taş
            { "skeleton", "İskelet Savaşçı" }, { "cave_spider", "Mağara Örümceği" }, { "stone_goblin", "Taş Goblini" }, { "blind_worm", "Kör Solucan" },
            // Kristal
            { "crystal_slime", "Kristal Balçık" }, { "crystal_spider", "Kristal Örümcek" }, { "wraith", "Mağara Hayaleti" }, { "myconid", "Mantar Adam" },
            // Göl
            { "naga", "Naga Savaşçısı" }, { "drowned", "Boğulmuş Ölü" }, { "serpent", "Göl Yılanı" }, { "water_elemental", "Su Elementali" },
            // Demirhane
            { "iron_golem", "Demir Golem" }, { "fire_imp", "Ateş İmpi" }, { "dwarf_ghost", "Cüce Hayaleti" }, { "coal_worm", "Kömür Kurdu" },
            // Fosil
            { "bone_whelp", "Kemik Ejder Yavrusu" }, { "fossil_spider", "Fosil Örümceği" }, { "mummy", "Kadim Mumya" }, { "sand_worm", "Kum Solucanı" },
            // Ejder
            { "dragon_whelp", "Ejder Yavrusu" }, { "kobold", "Kobold Mızrakçı" }, { "flame_bat", "Alev Yarasası" }, { "dragon_cultist", "Ejder Tarikatçısı" },
            // Lav
            { "lava_elemental", "Lav Elementali" }, { "magma_slime", "Magma Balçığı" }, { "demon", "Alev İblisi" }, { "hellhound", "Cehennem Tazısı" },
            // Bosslar
            { "goblin_chief", "Goblin Şefi Grubnak" }, { "bone_lord", "Kemik Lordu Varkoth" }, { "crystal_golem", "Kristal Golem Azurion" },
            { "naga_queen", "Naga Kraliçesi Sssira" }, { "forge_king", "Demirci Kral Thorgrim" }, { "fossil_wyrm", "Fosil Ejder Ossarax" },
            { "elder_dragon", "Kadim Ejder Vyrmathor" }, { "lava_titan", "Lav Titanı Ignar" }, { "core_devourer", "Çekirdek Yiyici Nihil" },
        };

        private static readonly Dictionary<string, string> WeaponNames = new Dictionary<string, string>
        {
            { "dagger", "Paslı Hançer" }, { "shortsword", "Kısa Kılıç" }, { "spiked_club", "Dikenli Sopa" },
            { "hand_axe", "El Baltası" }, { "war_mace", "Savaş Gürzü" }, { "spear", "Mızrak" },
            { "longbow", "Uzun Yay" }, { "mage_staff", "Büyücü Asası" }, { "katana", "Katana" },
            { "great_axe", "Çift Ağızlı Balta" }, { "war_hammer", "Savaş Çekici" }, { "soul_scythe", "Ruh Tırpanı" },
            { "rune_sword", "Runik Kılıç" }, { "crystal_staff", "Kristal Asa" }, { "dragon_spear", "Ejder Mızrağı" },
            { "shadow_blades", "Gölge Hançerleri" }, { "volcanic_greatsword", "Volkanik Büyük Kılıç" }, { "void_scythe", "Hiçlik Tırpanı" },
        };

        private static readonly Dictionary<string, string> MaterialNames = new Dictionary<string, string>
        {
            { "cloth", "Bez" }, { "leather", "Deri" }, { "chain", "Zincir" }, { "plate", "Plaka" },
            { "mithril", "Mithril" }, { "dragonscale", "Ejder Pulu" }, { "void", "Hiçlik" },
            { "copper", "Bakır" }, { "silver", "Gümüş" }, { "gold", "Altın" },
            { "moonstone", "Ay Taşı" }, { "dragoneye", "Ejder Gözü" }, { "star", "Yıldız" },
        };

        // ---------------- Canavarlar ----------------

        public static string MonsterName(string id) => MonsterNames.TryGetValue(id, out string n) ? n : id;

        public static string MonsterKindLabel(MonsterKind kind)
        {
            switch (kind)
            {
                case MonsterKind.MiniBoss: return "MİNİ BOSS";
                case MonsterKind.Boss: return "BOSS";
                default: return "";
            }
        }

        // ---------------- Eşyalar ----------------

        public static string SlotName(GearSlot slot)
        {
            switch (slot)
            {
                case GearSlot.Weapon: return "Silah";
                case GearSlot.Helmet: return "Kask";
                case GearSlot.Armor: return "Zırh";
                case GearSlot.Pants: return "Pantolon";
                case GearSlot.Gloves: return "Eldiven";
                case GearSlot.Boots: return "Çizme";
                case GearSlot.Necklace: return "Kolye";
                case GearSlot.Ring: return "Yüzük";
                default: return slot.ToString();
            }
        }

        /// <summary>Malzeme id'si: "chain_gloves" -> "chain". Silahlarda null.</summary>
        public static string MaterialId(GearItem item)
        {
            if (item.slot == GearSlot.Weapon) return null;
            int cut = item.baseId.LastIndexOf('_');
            return cut > 0 ? item.baseId.Substring(0, cut) : item.baseId;
        }

        /// <summary>Taban adı: "Katana", "Zincir Eldiven".</summary>
        public static string BaseName(GearItem item)
        {
            if (item.slot == GearSlot.Weapon) return WeaponNames.TryGetValue(item.baseId, out string w) ? w : item.baseId;
            string material = MaterialId(item);
            string m = MaterialNames.TryGetValue(material, out string mn) ? mn : material;
            return $"{m} {SlotName(item.slot)}";
        }

        /// <summary>Tam ad, nadirlik renginde: "Efsanevi Katana".</summary>
        public static string GearName(GearItem item) =>
            RarityStyle.Colored($"{RarityStyle.Name(item.tier)} {BaseName(item)}", item.tier);

        // ---------------- Statlar ----------------

        public static string StatName(StatType stat)
        {
            switch (stat)
            {
                case StatType.Attack: return "Saldırı";
                case StatType.Health: return "Can";
                case StatType.Defense: return "Savunma";
                case StatType.CritChance: return "Kritik şansı";
                case StatType.CritDamage: return "Kritik hasarı";
                case StatType.CaveResist: return "Göçük direnci";
                case StatType.FindChance: return "Bulgu şansı";
                case StatType.Luck: return "Şans";
                case StatType.GoldBonus: return "Altın bonusu";
                default: return stat.ToString();
            }
        }

        /// <summary>Stat değeri: düz statlar "+12", yüzdeler "+%4.5".</summary>
        public static string StatValueText(StatType stat, double value)
        {
            string sign = value >= 0 ? "+" : "-";
            double abs = System.Math.Abs(value);
            return CharacterStats.IsPercent(stat) ? $"{sign}%{UIStrings.Pct(abs)}" : $"{sign}{UIStrings.Num(abs)}";
        }

        /// <summary>Eşyanın statları, satır satır (aynı stat birleşik).</summary>
        public static string GearStatLines(GearItem item, GearItem compareTo = null)
        {
            var sb = new StringBuilder();
            foreach (StatType stat in (StatType[])System.Enum.GetValues(typeof(StatType)))
            {
                double v = item.Get(stat);
                double other = compareTo != null ? compareTo.Get(stat) : 0;
                if (v == 0 && (compareTo == null || other == 0)) continue;

                sb.Append($"{StatName(stat)}  {StatValueText(stat, v)}");
                if (compareTo != null)
                {
                    double diff = v - other;
                    if (System.Math.Abs(diff) > 1e-9)
                    {
                        string color = diff > 0 ? "#3CCB5A" : "#FF5A4A";
                        sb.Append($"   <color={color}>({StatValueText(stat, diff)})</color>");
                    }
                }
                sb.Append('\n');
            }
            return sb.ToString().TrimEnd('\n');
        }

        /// <summary>Eşyanın en önemli iki statı, tek satır (bulgu açıklaması için).</summary>
        public static string GearStatShort(GearItem item)
        {
            var parts = new List<string>();
            foreach (StatValue s in item.stats)
            {
                if (parts.Count >= 3) break;
                parts.Add($"{StatName(s.stat)} {StatValueText(s.stat, s.value)}");
            }
            return string.Join(", ", parts);
        }

        /// <summary>Karakter statları özeti (ekipman ekranı ve tur ekranı için).</summary>
        public static string CharacterSummary(CharacterStats s) =>
            $"Saldırı <b>{UIStrings.Num(s.Attack)}</b>   Can <b>{UIStrings.Num(s.Health)}</b>   Savunma <b>{UIStrings.Num(s.Defense)}</b>   " +
            $"Kritik <b>%{UIStrings.Pct(s.CritChance)}</b> x{s.CritDamage:0.00}\n" +
            $"Göçük direnci %{UIStrings.Pct(s.CaveResist)}   Bulgu +%{UIStrings.Pct(s.FindChance)}   " +
            $"Şans +%{UIStrings.Pct(s.Luck)}   Altın +%{UIStrings.Pct(s.GoldBonus)}";
    }
}
