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
        public string bossId;               // Bu katı koruyan boss (CombatBalance.monsters). Boşsa boss yok

        public MilestoneDefinition() { }
        public MilestoneDefinition(int step, int minTier, double firstReachRewardSteps, string bossId = null)
        {
            this.step = step;
            this.minTier = minTier;
            this.firstReachRewardSteps = firstReachRewardSteps;
            this.bossId = bossId;
        }
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
        public double baseStepReward = 10;
        public double stepMultiplierGrowth = 1.25;

        // --- Risk eğrisi ---
        // chance(step) = clamp((baseBustChance + bustChancePerStep * (step - 1)) * safetyFactor * runSafetyFactor, floor(step), max)
        // floor(step)  = minBustChance + bustFloorPerStep * (step - 1)
        // Taban risk derinlikle artar: yükseltmeler sığ katları güvenli yapar ama derin katlar HEP tehlikeli kalır.
        // Bu olmadan risk %1'e iner, derinlik sınırsızlaşır ve üstel ödül ekonomiyi patlatır.
        public double baseBustChance = 0.05;
        public double bustChancePerStep = 0.04;
        public double minBustChance = 0.01;
        public double bustFloorPerStep = 0.01;
        public double maxBustChance = 0.95;

        // --- Tur içi bulgu bonuslarının üst sınırları (ekonomi patlamasın diye) ---
        public double maxRunProfitBonus = 2.0;   // Zengin damarlar toplamda en fazla +%200
        public double minRunSafetyFactor = 0.5;  // Sağlam kayalar riski en fazla yarıya indirir

        // --- Bulgular ---
        // findChance(step) = clamp(findChanceBase + findChancePerStep * (step - 1) + findChanceUpgrade, 0, maxFindChance)
        public double findChanceBase = 0.30;
        public double findChancePerStep = 0.015;
        public double maxFindChance = 0.90;

        // Şans: nadirlik ağırlıkları  weight(t) = tier.weight * (1 + luck) ^ t  ile kayar.
        // luck = luckPerStep * step + luckUpgrade. Derine inmek nadir bulgu şansını artırır.
        public double luckPerStep = 0.04;

        // 9 nadirlik: beyaz, açık mavi, mavi, lacivert, yeşil, sarı, turuncu, kırmızı, mor
        public RarityTierDefinition[] rarityTiers =
        {
            new RarityTierDefinition(100, 1.0),
            new RarityTierDefinition(45, 1.5),
            new RarityTierDefinition(20, 2.2),
            new RarityTierDefinition(9, 3.2),
            new RarityTierDefinition(4, 4.7),
            new RarityTierDefinition(1.8, 7),
            new RarityTierDefinition(0.8, 10),
            new RarityTierDefinition(0.35, 15),
            new RarityTierDefinition(0.15, 25),
        };

        // Bulgu türleri. Değer formülleri:
        //   Treasure / SafeCache: stepReward * baseValue * power * findValueBonus   (para)
        //   ProfitBoost:          baseValue * power  -> runProfitBonus'a eklenir    (ör. 0.04 * 3.2 = +%12.8)
        //   Safety:               min(baseValue * power, maxSafetyFindReduction) -> runSafetyFactor *= (1 - değer)
        //   Shield:               tier >= shieldDoubleTier ise 2, değilse 1 baret
        public FindTypeDefinition treasureFind = new FindTypeDefinition(40, 1.5, 0);
        public FindTypeDefinition safeCacheFind = new FindTypeDefinition(20, 0.8, 0);
        public FindTypeDefinition profitBoostFind = new FindTypeDefinition(18, 0.04, 0);
        public FindTypeDefinition safetyFind = new FindTypeDefinition(15, 0.03, 0);
        public FindTypeDefinition shieldFind = new FindTypeDefinition(7, 1, 3);
        public double maxSafetyFindReduction = 0.5;
        public int shieldDoubleTier = 7;

        // Yeni türler:
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

        // Keşif katları (kilometre taşları). Erken tatmin (3, 5), sonra "bir kat daha" gerilimi (10, 15, ...).
        // Taban risk yüzünden 30+ katlar efsanevi hedeflerdir. İsimleri UIStrings.MilestoneName'de.
        public MilestoneDefinition[] milestones =
        {
            new MilestoneDefinition(3, 1, 2, "goblin_chief"),
            new MilestoneDefinition(5, 2, 3, "bone_lord"),
            new MilestoneDefinition(10, 3, 5, "crystal_golem"),
            new MilestoneDefinition(15, 4, 6, "naga_queen"),
            new MilestoneDefinition(20, 5, 8, "forge_king"),
            new MilestoneDefinition(25, 6, 10, "fossil_wyrm"),
            new MilestoneDefinition(30, 7, 12, "elder_dragon"),
            new MilestoneDefinition(35, 8, 15, "lava_titan"),
            new MilestoneDefinition(40, 8, 20, "core_devourer"),
        };

        // --- Savaş, canavarlar, ekipman (ayrı sınıfta, çok sayı var) ---
        public CombatBalance combat = new CombatBalance();

        // --- Yükseltmeler ---
        // Kazanç:   earningsBonus = (1 + effect) ^ level           -> 0.15 ile 3. seviye = x1.52
        public UpgradeDefinition earningsUpgrade = new UpgradeDefinition(50, 1.6, 0.15, 20);
        // Güvenlik: safetyFactor = (1 - effect) ^ level            -> 0.08 ile 3. seviye = x0.78
        public UpgradeDefinition safetyUpgrade = new UpgradeDefinition(75, 1.7, 0.08, 15);
        // Sigorta:  savedFraction = 1 - (1 - effect) ^ level       -> 0.10 ile 3. seviye = %27.1
        public UpgradeDefinition insuranceUpgrade = new UpgradeDefinition(100, 1.8, 0.10, 10);
        // Dedektör: findChance += effect * level                   -> 0.03 ile 5. seviye = +%15
        public UpgradeDefinition findChanceUpgrade = new UpgradeDefinition(60, 1.55, 0.03, 15);
        // Fener:    luck += effect * level                         -> 0.05 ile 10. seviye = +0.5 şans
        public UpgradeDefinition luckUpgrade = new UpgradeDefinition(120, 1.65, 0.05, 20);
        // Terazi:   findValueBonus = (1 + effect) ^ level          -> 0.12 ile 20. seviye = x9.6 (0.20 ile x38'di, fazlaydı)
        public UpgradeDefinition findValueUpgrade = new UpgradeDefinition(90, 1.6, 0.12, 20);
        // Baret:    startShields = round(effect * level)           -> her seviye +1 baret
        public UpgradeDefinition startShieldUpgrade = new UpgradeDefinition(2000, 6, 1, 3);

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
                case UpgradeType.StartShield: return startShieldUpgrade;
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
                case FindType.Shield: return shieldFind;
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
