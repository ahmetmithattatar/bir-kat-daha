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
        // LOTR havası, özgün isimler (Steam'de satılacak oyunda telif/marka riski olmasın diye birebir isim yok).
        private static readonly Dictionary<string, string> MonsterNames = new Dictionary<string, string>
        {
            // Goblin Tünelleri (1-4)
            { "cave_rat", "Mağara Faresi" }, { "slime", "Balçık" }, { "bat", "Kara Yarasa" },
            { "goblin", "Goblin Yağmacı" }, { "orc_scout", "Ork İzci" }, { "warg_pup", "Warg Yavrusu" },
            // Kayıp Cüce Karakolu (5-9)
            { "skeleton", "İskelet Cüce Muhafız" }, { "cave_spider", "Mağara Örümceği" }, { "stone_goblin", "Taş Goblini" },
            { "blind_worm", "Kör Solucan" }, { "orc_warrior", "Ork Savaşçısı" }, { "barrow_wight", "Höyük Hortlağı" },
            // Kristal Salonlar (10-14)
            { "crystal_slime", "Kristal Balçık" }, { "crystal_spider", "Kristal Örümcek" }, { "wraith", "Solgun Hayalet" },
            { "myconid", "Mantar Adam" }, { "cave_troll", "Mağara Trolü" }, { "dark_warg", "Kara Warg" },
            // Gözcünün Gölü (15-19)
            { "naga", "Naga Savaşçısı" }, { "drowned", "Boğulmuş Ölü" }, { "serpent", "Göl Yılanı" },
            { "water_elemental", "Su Elementali" }, { "lake_tentacle", "Gözcünün Kolu" }, { "marsh_ghoul", "Bataklık Ölüsü" },
            // Cüce Kralın Demirhanesi (20-24)
            { "iron_golem", "Demir Golem" }, { "fire_imp", "Ateş İmpi" }, { "dwarf_ghost", "Cüce Hayaleti" },
            { "coal_worm", "Kömür Kurdu" }, { "black_orc", "Kara Ork Muhafızı" }, { "forge_troll", "Demirhane Trolü" },
            // Kadim Höyükler (25-29)
            { "bone_whelp", "Kemik Ejder Yavrusu" }, { "fossil_spider", "Fosil Örümceği" }, { "mummy", "Kadim Mumya" },
            { "sand_worm", "Kum Solucanı" }, { "giant_spider", "Dev Örümcek" }, { "cursed_dead", "Lanetli Ölü" },
            // Ejderin Altın Yatağı (30-34)
            { "dragon_whelp", "Ejder Yavrusu" }, { "kobold", "Ejder Uşağı Kobold" }, { "flame_bat", "Alev Yarasası" },
            { "dragon_cultist", "Ejder Tarikatçısı" }, { "fire_drake", "Ateş Drakı" }, { "war_troll", "Savaş Trolü" },
            // Gölge ve Alev (35+)
            { "lava_elemental", "Lav Elementali" }, { "magma_slime", "Magma Balçığı" }, { "demon", "Alev İblisi" },
            { "hellhound", "Cehennem Tazısı" }, { "shadow_rider", "Gölge Süvarisi" }, { "flame_wraith", "Alev Hayaleti" },

            // ---- Boss havuzları ----
            // 3. kat
            { "goblin_chief", "Goblin Şefi Grubnak" }, { "orc_captain", "Ork Yüzbaşısı Ugrak" }, { "rat_king", "Fare Kralı Kıstırık" },
            { "slime_mother", "Balçık Ana Glorp" }, { "warg_alpha", "Sürü Lideri Kurtkan" },
            // 5. kat
            { "bone_lord", "Kemik Lordu Varkoth" }, { "spider_matron", "Örümcek Ana Vessith" }, { "wight_thane", "Höyük Beyi Drauvar" },
            { "orc_warlord", "Ork Savaş Beyi Mazruk" }, { "stone_troll", "Taş Trol Gruk" },
            // 10. kat
            { "crystal_golem", "Kristal Golem Azurion" }, { "troll_king", "Mağara Trolü Kralı Ghaz" }, { "crystal_wyrm", "Kristal Ejder Lysar" },
            { "pale_queen", "Solgun Kraliçe Ethryl" }, { "myconid_elder", "Kadim Mantar Moldor" },
            // 15. kat
            { "naga_queen", "Naga Kraliçesi Sssira" }, { "lake_watcher", "Göldeki Gözcü" }, { "drowned_king", "Boğulmuş Kral Velmar" },
            { "storm_spirit", "Fırtına Ruhu Zarath" }, { "marsh_hag", "Bataklık Cadısı Grisel" },
            // 20. kat
            { "forge_king", "Demirci Kral Thorgrim" }, { "black_orc_chief", "Kara Ork Şefi Bolgrath" }, { "iron_troll_lord", "Demir Trol Lordu Krag" },
            { "imp_lord", "İmp Lordu Pyrax" }, { "ghost_king", "Hayalet Cüce Kral Durnhelm" },
            // 25. kat
            { "fossil_wyrm", "Fosil Ejder Ossarax" }, { "barrow_king", "Höyük Kralı Morvath" }, { "great_spider", "Dev Örümcek Ana Ungrath" },
            { "sand_wurm", "Kum Kurdu Ssarak" }, { "mummy_lord", "Mumya Lordu Ankhet" },
            // 30. kat
            { "elder_dragon", "Kadim Ejder Vyrmathor" }, { "gold_sleeper", "Altın Uykucu Smaragor" }, { "dragon_priest", "Ejder Rahibi Xalvor" },
            { "war_troll_king", "Savaş Trolü Kralı Gromthar" }, { "drake_mother", "Drak Ana Kyrra" },
            // 35. kat
            { "lava_titan", "Lav Titanı Ignar" }, { "black_rider", "Kara Süvari Vorgath" }, { "hell_warg", "Cehennem Warg'ı Fenrak" },
            { "magma_mother", "Magma Ana Ignis" }, { "demon_lord", "İblis Lordu Azgar" },
            // 40. kat
            { "core_devourer", "Çekirdek Yiyici Nihil" }, { "shadow_flame", "Gölge ve Alev Durgoth" }, { "world_serpent", "Dünya Yılanı Jörmund" },
            { "root_watcher", "Köklerin Gözcüsü" }, { "ancient_lich", "Kadim Ölü Kral Mordrek" },
        };

        private static readonly Dictionary<string, string> PickaxeNames = new Dictionary<string, string>
        {
            { "rusty_pick", "Paslı Kazma" }, { "iron_pick", "Demir Kazma" }, { "steel_pick", "Çelik Kazma" },
            { "dwarf_pick", "Cüce Kazması" }, { "mithril_pick", "Mithril Kazma" }, { "runic_pick", "Runik Kazma" },
            { "dragonbone_pick", "Ejder Kemiği Kazma" }, { "star_pick", "Yıldız Demiri Kazma" },
        };

        // Kırmızı ve mor eşyalar efsanevi bir isim alır (eşyanın uid'sine göre sabit): "Mitik Ork Yaran"
        private static readonly string[] LegendWeapons =
        {
            "Ork Yaran", "Batı'nın Işığı", "Gölge Biçen", "Kral Kılıcı", "Yıldız Kırığı", "Ejder Dişi",
            "Fırtına Çağıran", "Ebedi Alev", "Kara Ay", "Son Umut", "Taht Bekçisi", "Dağların Öfkesi"
        };
        private static readonly string[] LegendArmor =
        {
            "Ay Işığı Örgüsü", "Ejderyürek", "Gölge Pelerini", "Dağ Kalbi", "Kralların Duruşu", "Yıldız Pulu",
            "Kadim Muhafız", "Batı Kalesi"
        };
        private static readonly string[] LegendJewelry =
        {
            "Akşam Yıldızı", "Kadim Mühür", "Ateş Halkası", "Denizin Gözyaşı", "Ay Kolyesi", "Kralın Nişanı"
        };
        private static readonly string[] LegendPickaxes =
        {
            "Dağ Yaran", "Cüce Kralın Kazması", "Taş Fısıltısı", "Kök Delen"
        };

        public const int LegendaryTier = 7;

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
                case GearSlot.Pickaxe: return "Kazma";
                default: return slot.ToString();
            }
        }

        /// <summary>Malzeme id'si: "chain_gloves" -> "chain". Silah ve kazmalarda null.</summary>
        public static string MaterialId(GearItem item)
        {
            if (item.slot == GearSlot.Weapon || item.slot == GearSlot.Pickaxe) return null;
            int cut = item.baseId.LastIndexOf('_');
            return cut > 0 ? item.baseId.Substring(0, cut) : item.baseId;
        }

        /// <summary>Taban adı: "Katana", "Zincir Eldiven", "Cüce Kazması".</summary>
        public static string BaseName(GearItem item)
        {
            if (item.slot == GearSlot.Weapon) return WeaponNames.TryGetValue(item.baseId, out string w) ? w : item.baseId;
            if (item.slot == GearSlot.Pickaxe) return PickaxeNames.TryGetValue(item.baseId, out string p) ? p : item.baseId;
            string material = MaterialId(item);
            string m = MaterialNames.TryGetValue(material, out string mn) ? mn : material;
            return $"{m} {SlotName(item.slot)}";
        }

        /// <summary>Kırmızı ve mor eşyanın efsanevi adı (yoksa null).</summary>
        public static string LegendName(GearItem item)
        {
            if (item.tier < LegendaryTier) return null;
            string[] pool;
            switch (item.slot)
            {
                case GearSlot.Weapon: pool = LegendWeapons; break;
                case GearSlot.Pickaxe: pool = LegendPickaxes; break;
                case GearSlot.Necklace:
                case GearSlot.Ring: pool = LegendJewelry; break;
                default: pool = LegendArmor; break;
            }
            return pool[(int)(item.uid % pool.Length)];
        }

        /// <summary>Tam ad, nadirlik renginde: "+3 Efsanevi Katana", efsanevilerde "Mitik Ork Yaran".</summary>
        public static string GearName(GearItem item)
        {
            string legend = LegendName(item);
            string name = legend != null ? $"{RarityStyle.Name(item.tier)} {legend}" : $"{RarityStyle.Name(item.tier)} {BaseName(item)}";
            if (item.enhance > 0) name = $"+{item.enhance} {name}";
            return RarityStyle.Colored(name, item.tier);
        }

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
                case StatType.DigPower: return "Kazı gücü";
                default: return stat.ToString();
            }
        }

        /// <summary>Stat değeri: düz statlar "+12", yüzdeler "+%4.5", kazı gücü "+1.3".</summary>
        public static string StatValueText(StatType stat, double value)
        {
            string sign = value >= 0 ? "+" : "-";
            double abs = System.Math.Abs(value);
            if (stat == StatType.DigPower && abs < 100) return $"{sign}{abs:0.0}";
            return CharacterStats.IsPercent(stat) ? $"{sign}%{UIStrings.Pct(abs)}" : $"{sign}{UIStrings.Num(abs)}";
        }

        /// <summary>
        /// Eşyanın statları (güçlendirme dahil), satır satır. compareTo verilirse kuşanılı eşyaya göre fark (+yeşil/-kırmızı).
        /// </summary>
        public static string GearStatLines(GearItem item, GearItem compareTo = null, double enhanceBonus = 0.10)
        {
            var sb = new StringBuilder();
            foreach (StatType stat in (StatType[])System.Enum.GetValues(typeof(StatType)))
            {
                double v = item.GetEffective(stat, enhanceBonus);
                double other = compareTo != null ? compareTo.GetEffective(stat, enhanceBonus) : 0;
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
            $"Kazı gücü <b>{s.DigPower:0.0}</b>   Göçük direnci %{UIStrings.Pct(s.CaveResist)}   Bulgu +%{UIStrings.Pct(s.FindChance)}   " +
            $"Şans +%{UIStrings.Pct(s.Luck)}   Altın +%{UIStrings.Pct(s.GoldBonus)}";

        // ---------------- Kadim (Yeni Çağ) yükseltmeleri ----------------

        public static string PrestigeName(PrestigeType type)
        {
            switch (type)
            {
                case PrestigeType.AncientGold: return "Kadim Altın";
                case PrestigeType.AncientLuck: return "Yıldızların Lütfu";
                case PrestigeType.AncientInsight: return "Kadim Bilgelik";
                case PrestigeType.AncientMight: return "Kralların Kanı";
                case PrestigeType.AncientWealth: return "Ejder Hazinesi";
                default: return type.ToString();
            }
        }

        public static string PrestigeEffect(PrestigeBalance p, PrestigeType type, int level)
        {
            UpgradeDefinition d = p.Get(type);
            switch (type)
            {
                case PrestigeType.AncientGold: return $"Kat ödülleri x{System.Math.Pow(1 + d.effectPerLevel, level):0.00}";
                case PrestigeType.AncientLuck: return $"Nadirlik +%{UIStrings.Pct(d.effectPerLevel * level)}";
                case PrestigeType.AncientInsight: return $"Kazanılan XP x{System.Math.Pow(1 + d.effectPerLevel, level):0.00}";
                case PrestigeType.AncientMight: return $"Saldırı ve Can x{System.Math.Pow(1 + d.effectPerLevel, level):0.00}";
                case PrestigeType.AncientWealth:
                    return level > 0 ? $"Yeni çağa {UIStrings.Num(d.effectPerLevel * System.Math.Pow(4, level - 1))} parayla başla" : "Yeni çağa parasız başla";
                default: return "";
            }
        }
    }
}
