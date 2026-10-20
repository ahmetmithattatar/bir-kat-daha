using System;

namespace RiskLoop.Core
{
    /// <summary>
    /// Bir yükseltmenin denge sayıları.
    /// Fiyat formülü:  cost(level) = ceil(baseCost * costGrowth ^ level)
    /// Etkinin nasıl uygulandığı RiskMath içinde, her türün kendi fonksiyonunda.
    /// </summary>
    [Serializable]
    public class UpgradeDefinition
    {
        public double baseCost;        // İlk seviyenin fiyatı
        public double costGrowth;      // Her seviyede fiyat bu sayıyla çarpılır
        public double effectPerLevel;  // Seviye başına etki (ör. 0.15 = %15)
        public int maxLevel;           // Bu seviyeden sonra satın alınamaz

        // Unity'nin serileştiricisi ve JsonUtility için boş kurucu
        public UpgradeDefinition() { }

        public UpgradeDefinition(double baseCost, double costGrowth, double effectPerLevel, int maxLevel)
        {
            this.baseCost = baseCost;
            this.costGrowth = costGrowth;
            this.effectPerLevel = effectPerLevel;
            this.maxLevel = maxLevel;
        }
    }

    /// <summary>
    /// Bir nadirlik seviyesi. weight = ne kadar sık çıktığı, power = ne kadar güçlü olduğu.
    /// Nadir seviyeler: düşük weight, yüksek power.
    /// </summary>
    [Serializable]
    public class RarityTierDefinition
    {
        public double weight;
        public double power;

        public RarityTierDefinition() { }
        public RarityTierDefinition(double weight, double power) { this.weight = weight; this.power = power; }
    }

    /// <summary>Bir bulgu türünün denge sayıları.</summary>
    [Serializable]
    public class FindTypeDefinition
    {
        public double weight;     // Bulgu çıktığında bu türün seçilme ağırlığı
        public double baseValue;  // Etkinin temel büyüklüğü. Nadirliğin power değeriyle çarpılır
        public int minTier;       // Bu türün çıkabileceği en düşük nadirlik

        public FindTypeDefinition() { }
        public FindTypeDefinition(double weight, double baseValue, int minTier)
        {
            this.weight = weight;
            this.baseValue = baseValue;
            this.minTier = minTier;
        }
    }

    /// <summary>
    /// Kilometre taşı (hazine odası): bu adıma ulaşınca garantili bulgu çıkar.
    /// İlk kez ulaşılınca ayrıca güvenli rekor ödülü verilir.
    /// </summary>
    [Serializable]
    public class MilestoneDefinition
    {
        public int step;                    // Hangi adımda
        public int minTier;                 // Garantili bulgunun en düşük nadirliği
        public double firstReachRewardSteps;// İlk ulaşma ödülü = o adımın ödülü * bu sayı (doğrudan cebe)
        public string[] bossPool;           // Bu katı koruyabilecek bosslar; her seferinde biri rastgele gelir. Boşsa boss yok

        public MilestoneDefinition() { }
        public MilestoneDefinition(int step, int minTier, double firstReachRewardSteps, params string[] bossPool)
        {
            this.step = step;
            this.minTier = minTier;
            this.firstReachRewardSteps = firstReachRewardSteps;
            this.bossPool = bossPool;
        }

        public bool HasBoss => bossPool != null && bossPool.Length > 0;
    }

    /// <summary>
    /// TÜM denge sayıları burada. Unity'den bağımsız düz bir C# sınıfı.
    /// Unity tarafında BalanceConfig (ScriptableObject) bunu Inspector'da düzenlenebilir yapar
    /// ve JSON'a aktarabilir. Python simülasyonu da aynı JSON'u okuyabilir.
    ///
    /// Alan adlarını değiştirirsen JSON anahtarları da değişir: Python tarafını güncellemeyi unutma.
    /// </summary>
    [Serializable]
    public class BalanceData
    {
        // --- Ekonomi ---
        public double startingMoney = 0;

        // --- Kazanç eğrisi ---
        // reward(step) = baseStepReward * stepMultiplierGrowth ^ (step - 1) * earningsBonus * (1 + runProfitBonus)
        // 1.12: derine inmek belirgin kazandırır ama ekonomi (ve yükseltmeler) bir çağda patlamaz.
        public double baseStepReward = 10;
        public double stepMultiplierGrowth = 1.12;

        // --- Bölgeler ---
        // Oyun 10 katlık bölgelerden oluşur. Bölgenin son katında (10, 20, 30 ...) boss bekler.
        // Göçük riski BÖLGE İÇİNDE artar (bölgenin 1. katı düşük, 9. katı yüksek); derin bölgeler riskle değil
        // canavar gücüyle zorlaşır. Böylece takılan oyuncu o bölgeyi farmlayıp (eşya + para) bossu geçecek güce ulaşır.
        public int zoneSize = 10;

        // --- Risk eğrisi (bölge içinde) ---
        // f = bölge içindeki kat (1..9), z = bölge (1..)
        // raw(step)   = baseBustChance + bustChancePerZoneFloor * (f - 1) + bustChancePerZone * (z - 1)
        // floor(step) = minBustChance + bustFloorPerZoneFloor * (f - 1) + bustFloorPerZone * (z - 1)
        // chance      = clamp(raw * safetyFactor * runSafetyFactor * yetenek, floor, max).  Boss katında (f = 10) göçük yok.
        public double baseBustChance = 0.04;
        public double bustChancePerZoneFloor = 0.025;
        public double bustChancePerZone = 0.01;
        public double minBustChance = 0.01;
        public double bustFloorPerZoneFloor = 0.012;
        public double bustFloorPerZone = 0.005;
        public double maxBustChance = 0.95;

        // --- Tur içi bulgu bonuslarının üst sınırları (ekonomi patlamasın diye) ---
        public double maxRunProfitBonus = 2.0;   // Zengin damarlar toplamda en fazla +%200
        public double minRunSafetyFactor = 0.5;  // Sağlam kayalar riski en fazla yarıya indirir

        // --- Bulgular ---
        // findChance(step) = clamp(findChanceBase + findChancePerStep * (step - 1) + findChanceUpgrade, 0, maxFindChance)
        public double findChanceBase = 0.30;
        public double findChancePerStep = 0.005;
        public double maxFindChance = 0.90;

        // Şans: nadirlik ağırlıkları  weight(t) = tier.weight * (1 + luck) ^ t  ile kayar.
        // luck = luckPerStep * step + yükseltme + ekipman + tılsım + yetenek + telafi. Derine inmek nadir bulgu şansını artırır.
        public double luckPerStep = 0.012;
        public double maxRunLuck = 1.5;          // Tılsımlardan gelen tur şansı en fazla bu kadar

        // 9 nadirlik: beyaz, açık mavi, mavi, lacivert, yeşil, sarı, turuncu, kırmızı, mor
        // Her seviye bir öncekinden ~3.3 kat NADİR ve belirgin güçlü: mor bir "jackpot" olmalı.
        public RarityTierDefinition[] rarityTiers =
        {
            new RarityTierDefinition(100, 1.0),
            new RarityTierDefinition(30, 1.6),
            new RarityTierDefinition(9, 2.5),
            new RarityTierDefinition(2.7, 4),
            new RarityTierDefinition(0.8, 6.5),
            new RarityTierDefinition(0.24, 10),
            new RarityTierDefinition(0.07, 16),
            new RarityTierDefinition(0.02, 26),
            new RarityTierDefinition(0.006, 45),
        };

        // Bulgu türleri. Değer formülleri:
        //   Treasure / SafeCache: stepReward * baseValue * power * findValueBonus   (para)
        //   ProfitBoost:          baseValue * power  -> runProfitBonus'a eklenir    (ör. 0.04 * 3.2 = +%12.8)
        //   Safety:               min(baseValue * power, maxSafetyFindReduction) -> runSafetyFactor *= (1 - değer)
        public FindTypeDefinition treasureFind = new FindTypeDefinition(40, 1.5, 0);
        public FindTypeDefinition safeCacheFind = new FindTypeDefinition(20, 0.8, 0);
        public FindTypeDefinition profitBoostFind = new FindTypeDefinition(18, 0.04, 0);
        public FindTypeDefinition safetyFind = new FindTypeDefinition(15, 0.03, 0);
        public double maxSafetyFindReduction = 0.5;

        // --- Talihsizlik telafisi (göçük moral bozmasın) ---
        // Her göçük/yenilgi sonraki turlara +nadirlik (birikir, sınırlı). Başarılı bir yüzeye dönüş sıfırlar.
        public double pityLuckPerStack = 0.12;
        public int maxPityStacks = 5;

        // --- Enkazdan kurtarma: göçükte turun en nadir eşyası bu olasılıkla kurtulur (envanterde yer varsa) ---
        public double bustSalvageChance = 0.5;

        // Özel bulgular (oyuncu seviyesiyle açılır, bkz. progression):
        //   LuckCharm:      baseValue * power -> runLuck'a eklenir (bu turun kalanında daha nadir bulgular)
        //   FindStreak:     floor(baseValue + streakStepsPerTier * tier) adım boyunca bulgu garantili
        //   Multiplier:     min(baseValue * power, maxMultiplierFindBonus) -> risk altındaki kazanç *= (1 + değer)
        //   CursedTreasure: Treasure gibi para (daha büyük baseValue) + runSafetyFactor *= (1 + cursedRiskIncrease)
        public FindTypeDefinition luckCharmFind = new FindTypeDefinition(10, 0.06, 0);
        public FindTypeDefinition findStreakFind = new FindTypeDefinition(6, 1, 1);
        public FindTypeDefinition multiplierFind = new FindTypeDefinition(5, 0.10, 2);
        public FindTypeDefinition cursedTreasureFind = new FindTypeDefinition(5, 4.0, 2);
        // Ekipman: kayadan çıkan eşya (Value = satış değeri, eşya FindResult.Item'da). Turda risk altında
        public FindTypeDefinition gearFind = new FindTypeDefinition(12, 0, 0);
        public double streakStepsPerTier = 0.5;
        public double maxMultiplierFindBonus = 2.0;   // Tek kristal en fazla x3
        public double cursedRiskIncrease = 0.15;      // Her lanetli hazine bu tur riski %15 artırır
        public double maxRunSafetyFactor = 1.6;       // Lanetler toplamda riski en fazla %60 artırır

        // Boss katları: her bölgenin son katı. Her birinde 5 bossluk havuz (her inişte biri rastgele gelir).
        // Bölge isimleri UIStrings.ZoneName'de. Bossu yenmek: garantili bulgu (en az minTier), ganimet ve o bölgeden başlama geçidi.
        public MilestoneDefinition[] milestones =
        {
            new MilestoneDefinition(10, 2, 3, "goblin_chief", "orc_captain", "rat_king", "slime_mother", "warg_alpha"),
            new MilestoneDefinition(20, 3, 4, "bone_lord", "spider_matron", "wight_thane", "orc_warlord", "stone_troll"),
            new MilestoneDefinition(30, 3, 5, "crystal_golem", "troll_king", "crystal_wyrm", "pale_queen", "myconid_elder"),
            new MilestoneDefinition(40, 4, 6, "naga_queen", "lake_watcher", "drowned_king", "storm_spirit", "marsh_hag"),
            new MilestoneDefinition(50, 5, 8, "forge_king", "black_orc_chief", "iron_troll_lord", "imp_lord", "ghost_king"),
            new MilestoneDefinition(60, 6, 10, "fossil_wyrm", "barrow_king", "great_spider", "sand_wurm", "mummy_lord"),
            new MilestoneDefinition(70, 7, 12, "elder_dragon", "gold_sleeper", "dragon_priest", "war_troll_king", "drake_mother"),
            new MilestoneDefinition(80, 8, 15, "lava_titan", "black_rider", "hell_warg", "magma_mother", "demon_lord"),
            new MilestoneDefinition(90, 8, 20, "core_devourer", "shadow_flame", "world_serpent", "root_watcher", "ancient_lich"),
        };

        // --- Savaş, canavarlar, ekipman (ayrı sınıfta, çok sayı var) ---
        public CombatBalance combat = new CombatBalance();

        // --- Oyuncu seviyesi ve kademeli açılan özellikler ---
        public ProgressionBalance progression = new ProgressionBalance();

        // --- Yetenek ağacı ---
        public TalentBalance talents = new TalentBalance();

        // --- Yeni Çağ (prestij) ---
        public PrestigeBalance prestige = new PrestigeBalance();

        // --- Yükseltmeler (maks seviyeler yüksek: tek çağda hepsi dolmasın) ---
        // Kazanç:   earningsBonus = (1 + effect) ^ level           -> 0.10 ile 40. seviye = x45
        public UpgradeDefinition earningsUpgrade = new UpgradeDefinition(50, 1.6, 0.10, 40);
        // Güvenlik: safetyFactor = (1 - effect) ^ level            -> 0.05 ile 25. seviye = x0.28
        public UpgradeDefinition safetyUpgrade = new UpgradeDefinition(75, 1.65, 0.05, 25);
        // Sigorta:  savedFraction = 1 - (1 - effect) ^ level       -> 0.07 ile 15. seviye = %66
        public UpgradeDefinition insuranceUpgrade = new UpgradeDefinition(100, 1.75, 0.07, 15);
        // Dedektör: findChance += effect * level                   -> 0.02 ile 20. seviye = +%40
        public UpgradeDefinition findChanceUpgrade = new UpgradeDefinition(60, 1.55, 0.02, 20);
        // Fener:    luck += effect * level                         -> 0.03 ile 25. seviye = +0.75 şans
        public UpgradeDefinition luckUpgrade = new UpgradeDefinition(120, 1.62, 0.03, 25);
        // Terazi:   findValueBonus = (1 + effect) ^ level          -> 0.08 ile 30. seviye = x10
        public UpgradeDefinition findValueUpgrade = new UpgradeDefinition(90, 1.58, 0.08, 30);

        /// <summary>Yükseltme türüne göre tanımı döndürür.</summary>
        public UpgradeDefinition GetUpgrade(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.Earnings: return earningsUpgrade;
                case UpgradeType.Safety: return safetyUpgrade;
                case UpgradeType.Insurance: return insuranceUpgrade;
                case UpgradeType.FindChance: return findChanceUpgrade;
                case UpgradeType.Luck: return luckUpgrade;
                case UpgradeType.FindValue: return findValueUpgrade;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        /// <summary>Bulgu türüne göre tanımı döndürür.</summary>
        public FindTypeDefinition GetFindType(FindType type)
        {
            switch (type)
            {
                case FindType.Treasure: return treasureFind;
                case FindType.SafeCache: return safeCacheFind;
                case FindType.ProfitBoost: return profitBoostFind;
                case FindType.Safety: return safetyFind;
                case FindType.LuckCharm: return luckCharmFind;
                case FindType.FindStreak: return findStreakFind;
                case FindType.Multiplier: return multiplierFind;
                case FindType.CursedTreasure: return cursedTreasureFind;
                case FindType.Gear: return gearFind;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
