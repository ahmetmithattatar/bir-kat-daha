using System;

namespace RiskLoop.Core
{
    /// <summary>
    /// Oyuncu seviyesiyle kademeli açılan özellikler. Oyun her şeyi baştan göstermez:
    /// oyuncu ilerledikçe yeni sistemler "YENİ ÖZELLİK AÇILDI!" ile gelir (öğrenmesi kolay, hep yeni bir hedef).
    /// Kayıtta tutulmaz (seviyeden hesaplanır): sırası değişebilir.
    /// </summary>
    public enum Feature
    {
        Talents,        // Yetenek ağacı
        Equipment,      // Ekipman sekmesi + eşya düşmesi
        Insurance,      // Sigorta yükseltmesi
        FindChance,     // Maden dedektörü yükseltmesi
        Checkpoints,    // Geçitten başlama
        Enhance,        // Güçlendirme
        Luck,           // Uğurlu fener yükseltmesi
        FindValue,      // Kuyumcu terazisi yükseltmesi
        SpecialFinds,   // Tılsım, harita, kristal, put
        Prestige        // Yeni Çağ
    }

    /// <summary>
    /// Deneyim (XP) ve seviye. XP: her inilen kat, her canavar, her boss. Her seviye 1 yetenek puanı.
    /// xpToNext(L) = xpBase * xpGrowth^(L-1). Bölge (z) arttıkça kazanılan XP de artar: derin = hızlı ilerleme.
    /// </summary>
    [Serializable]
    public class ProgressionBalance
    {
        public double xpBase = 60;
        public double xpGrowth = 1.28;
        public double xpPerFloor = 4;       // * bölge
        public double xpPerKill = 12;       // * bölge
        public double xpPerBoss = 150;      // * bölge
        public int talentPointsPerLevel = 1;

        // Açılış seviyeleri (Feature sırasıyla aynı olmak zorunda değil; GetUnlockLevel eşler)
        public int talentsLevel = 2;
        public int equipmentLevel = 3;
        public int insuranceLevel = 4;
        public int findChanceLevel = 4;
        public int checkpointsLevel = 5;
        public int enhanceLevel = 6;
        public int luckLevel = 7;
        public int findValueLevel = 7;
        public int specialFindsLevel = 8;
        public int prestigeLevel = 12;

        public int GetUnlockLevel(Feature f)
        {
            switch (f)
            {
                case Feature.Talents: return talentsLevel;
                case Feature.Equipment: return equipmentLevel;
                case Feature.Insurance: return insuranceLevel;
                case Feature.FindChance: return findChanceLevel;
                case Feature.Checkpoints: return checkpointsLevel;
                case Feature.Enhance: return enhanceLevel;
                case Feature.Luck: return luckLevel;
                case Feature.FindValue: return findValueLevel;
                case Feature.SpecialFinds: return specialFindsLevel;
                case Feature.Prestige: return prestigeLevel;
                default: return 1;
            }
        }
    }

    // ---------------- Yetenek ağacı ----------------

    /// <summary>Üç dal (LOTR havası): kazı, savaş, şans.</summary>
    public enum TalentBranch
    {
        Dwarf,    // Cüce Yolu: kazı, risk, ödül
        Sword,    // Kılıç Yolu: savaş
        Dragon    // Ejder Yolu: şans, ganimet, para
    }

    /// <summary>Bir yeteneğin ne yaptığı (etki türü). Değer = rank * perRank.</summary>
    public enum TalentEffect
    {
        DigPower,         // Kazı gücü +%
        RiskReduction,    // Göçük riski çarpanı *(1 - x)
        RewardBonus,      // Kat ödülü +%
        FindChance,       // Bulgu şansı +
        Insurance,        // Sigorta +
        Salvage,          // Enkazdan kurtarma şansı +
        ZoneStartSafety,  // Bölgenin ilk 5 katında risk *(1 - x)        (kapanış yeteneği)
        Attack,           // Saldırı +%
        Health,           // Can +%
        CritChance,       // Kritik şansı +
        Defense,          // Savunma +%
        Lifesteal,        // Vurduğun hasarın x'i kadar can
        EnrageSlow,       // Boss öfke aralığı +%
        BossDamage,       // Bosslara hasar +%                        (kapanış yeteneği)
        Luck,             // Şans +
        FindValue,        // Hazine/Kese değeri +%
        GearDrop,         // Eşya düşme şansı +
        SellValue,        // Satış değeri +%
        TierUpgrade,      // Bulgu/eşya nadirliğinin +1 olma şansı ("parlayış")
        PityBoost,        // Talihsizlik telafisi kademe başına +
        TwinFind          // Bulgunun iki kez çıkma şansı                  (kapanış yeteneği)
    }

    /// <summary>Bir yetenek düğümü.</summary>
    [Serializable]
    public class TalentDefinition
    {
        public string id;
        public TalentBranch branch;
        public int row;              // 0..3. Sıra açılması için o dala harcanmış puan: row * pointsPerRow
        public int maxRank;
        public TalentEffect effect;
        public double perRank;

        public TalentDefinition() { }
        public TalentDefinition(string id, TalentBranch branch, int row, int maxRank, TalentEffect effect, double perRank)
        {
            this.id = id;
            this.branch = branch;
            this.row = row;
            this.maxRank = maxRank;
            this.effect = effect;
            this.perRank = perRank;
        }
    }

    /// <summary>
    /// Yetenek ağacı: 3 dal x 4 sıra. Her dalda 7 düğüm (son sıra tek ranklı kapanış yeteneği). Toplam 81 puan.
    /// Bir sıra, o dala (row * pointsPerRow) puan harcanınca açılır: odaklanmayı ödüllendirir, ama karışık build de mümkün.
    /// Sıfırlama (respec) para ister: respecCostBase * seviye^2.
    /// </summary>
    [Serializable]
    public class TalentBalance
    {
        public int pointsPerRow = 5;
        public double respecCostBase = 100;

        public TalentDefinition[] nodes =
        {
            // ---- Cüce Yolu (kazı) ----
            new TalentDefinition("sturdy_pick", TalentBranch.Dwarf, 0, 5, TalentEffect.DigPower, 0.10),
            new TalentDefinition("stone_sense", TalentBranch.Dwarf, 0, 5, TalentEffect.RiskReduction, 0.03),
            new TalentDefinition("gold_vein", TalentBranch.Dwarf, 1, 5, TalentEffect.RewardBonus, 0.06),
            new TalentDefinition("deep_instinct", TalentBranch.Dwarf, 1, 5, TalentEffect.FindChance, 0.02),
            new TalentDefinition("brace_master", TalentBranch.Dwarf, 2, 3, TalentEffect.Insurance, 0.05),
            new TalentDefinition("rubble_hunter", TalentBranch.Dwarf, 2, 3, TalentEffect.Salvage, 0.15),
            new TalentDefinition("mountain_heart", TalentBranch.Dwarf, 3, 1, TalentEffect.ZoneStartSafety, 0.5),

            // ---- Kılıç Yolu (savaş) ----
            new TalentDefinition("keen_edge", TalentBranch.Sword, 0, 5, TalentEffect.Attack, 0.06),
            new TalentDefinition("thick_hide", TalentBranch.Sword, 0, 5, TalentEffect.Health, 0.08),
            new TalentDefinition("deadly_eye", TalentBranch.Sword, 1, 5, TalentEffect.CritChance, 0.02),
            new TalentDefinition("armor_master", TalentBranch.Sword, 1, 5, TalentEffect.Defense, 0.08),
            new TalentDefinition("blood_drinker", TalentBranch.Sword, 2, 3, TalentEffect.Lifesteal, 0.01),
            new TalentDefinition("cold_blood", TalentBranch.Sword, 2, 3, TalentEffect.EnrageSlow, 0.15),
            new TalentDefinition("kingslayer", TalentBranch.Sword, 3, 1, TalentEffect.BossDamage, 0.4),

            // ---- Ejder Yolu (şans) ----
            new TalentDefinition("lucky_birth", TalentBranch.Dragon, 0, 5, TalentEffect.Luck, 0.04),
            new TalentDefinition("jeweler_eye", TalentBranch.Dragon, 0, 5, TalentEffect.FindValue, 0.08),
            new TalentDefinition("plunderer", TalentBranch.Dragon, 1, 5, TalentEffect.GearDrop, 0.06),
            new TalentDefinition("haggler", TalentBranch.Dragon, 1, 5, TalentEffect.SellValue, 0.10),
            new TalentDefinition("glimmer", TalentBranch.Dragon, 2, 3, TalentEffect.TierUpgrade, 0.03),
            new TalentDefinition("fortune_wheel", TalentBranch.Dragon, 2, 3, TalentEffect.PityBoost, 0.04),
            new TalentDefinition("dragon_eye", TalentBranch.Dragon, 3, 1, TalentEffect.TwinFind, 0.12),
        };

        public TalentDefinition Find(string id)
        {
            foreach (TalentDefinition t in nodes)
            {
                if (t.id == id) return t;
            }
            return null;
        }
    }

    /// <summary>Harcanmış yeteneklerin toplam etkisi. GameSession kurallara bunu uygular.</summary>
    public struct TalentBonuses
    {
        public double DigPower, RiskReduction, RewardBonus, FindChance, Insurance, Salvage, ZoneStartSafety;
        public double Attack, Health, CritChance, Defense, Lifesteal, EnrageSlow, BossDamage;
        public double Luck, FindValue, GearDrop, SellValue, TierUpgrade, PityBoost, TwinFind;

        public void Add(TalentEffect effect, double value)
        {
            switch (effect)
            {
                case TalentEffect.DigPower: DigPower += value; break;
                case TalentEffect.RiskReduction: RiskReduction += value; break;
                case TalentEffect.RewardBonus: RewardBonus += value; break;
                case TalentEffect.FindChance: FindChance += value; break;
                case TalentEffect.Insurance: Insurance += value; break;
                case TalentEffect.Salvage: Salvage += value; break;
                case TalentEffect.ZoneStartSafety: ZoneStartSafety += value; break;
                case TalentEffect.Attack: Attack += value; break;
                case TalentEffect.Health: Health += value; break;
                case TalentEffect.CritChance: CritChance += value; break;
                case TalentEffect.Defense: Defense += value; break;
                case TalentEffect.Lifesteal: Lifesteal += value; break;
                case TalentEffect.EnrageSlow: EnrageSlow += value; break;
                case TalentEffect.BossDamage: BossDamage += value; break;
                case TalentEffect.Luck: Luck += value; break;
                case TalentEffect.FindValue: FindValue += value; break;
                case TalentEffect.GearDrop: GearDrop += value; break;
                case TalentEffect.SellValue: SellValue += value; break;
                case TalentEffect.TierUpgrade: TierUpgrade += value; break;
                case TalentEffect.PityBoost: PityBoost += value; break;
                case TalentEffect.TwinFind: TwinFind += value; break;
            }
        }
    }
}
