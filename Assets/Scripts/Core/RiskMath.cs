using System;

namespace RiskLoop.Core
{
    /// <summary>
    /// Saf matematik: durum tutmaz, rastgelelik kullanmaz.
    /// Aynı girdi her zaman aynı çıktıyı verir. Python'a birebir taşınacak kısım burası.
    /// Zar gereken yerlerde zarı DIŞARIDAN alır (roll parametresi), kendisi atmaz.
    ///
    /// "step" = derinlik. 1. adım ilk iniştir.
    /// </summary>
    public static class RiskMath
    {
        // ---------------- Kazanç ----------------

        /// <summary>Sadece derinlikten gelen çarpan. step 1 -> x1.</summary>
        public static double StepMultiplier(BalanceData b, int step)
        {
            int s = Math.Max(step, 1);
            return Math.Pow(b.stepMultiplierGrowth, s - 1);
        }

        /// <summary>Kazanç yükseltmesinin çarpanı. Seviyeler birbirinin üstüne çarpılır.</summary>
        public static double EarningsBonus(BalanceData b, int earningsLevel)
        {
            return Math.Pow(1.0 + b.earningsUpgrade.effectPerLevel, earningsLevel);
        }

        /// <summary>Toplam çarpan = derinlik * yükseltme * (1 + bu turun bulgu bonusu).</summary>
        public static double TotalMultiplier(BalanceData b, int step, int earningsLevel, double runProfitBonus = 0)
        {
            return StepMultiplier(b, step) * EarningsBonus(b, earningsLevel) * (1.0 + runProfitBonus);
        }

        /// <summary>O adıma inince tur kazancına eklenen miktar.</summary>
        public static double StepReward(BalanceData b, int step, int earningsLevel, double runProfitBonus = 0)
        {
            return b.baseStepReward * TotalMultiplier(b, step, earningsLevel, runProfitBonus);
        }

        // ---------------- Risk ----------------

        /// <summary>Güvenlik yükseltmesinin risk çarpanı (1'den küçük = daha güvenli).</summary>
        public static double SafetyFactor(BalanceData b, int safetyLevel)
        {
            return Math.Pow(1.0 - b.safetyUpgrade.effectPerLevel, safetyLevel);
        }

        /// <summary>
        /// Bu adıma inerken patlama (göçük) olasılığı, 0..1 arası.
        /// Her adım bağımsız bir zar atışıdır. runSafetyFactor = bu turda bulunan "Safety" bulgularının çarpanı.
        /// </summary>
        public static double BustChance(BalanceData b, int step, int safetyLevel, double runSafetyFactor = 1)
        {
            int s = Math.Max(step, 1);
            double raw = b.baseBustChance + b.bustChancePerStep * (s - 1);
            double reduced = raw * SafetyFactor(b, safetyLevel) * runSafetyFactor;
            return Clamp(reduced, BustFloor(b, s), b.maxBustChance);
        }

        /// <summary>Bu derinlikte riskin inebileceği en düşük değer. Hiçbir yükseltme bunun altına indiremez.</summary>
        public static double BustFloor(BalanceData b, int step)
        {
            int s = Math.Max(step, 1);
            return Math.Min(b.minBustChance + b.bustFloorPerStep * (s - 1), b.maxBustChance);
        }

        // ---------------- Sigorta ----------------

        /// <summary>Patlamada kurtarılan tur kazancı oranı, 0..1. Hiçbir zaman 1'e ulaşmaz.</summary>
        public static double InsuranceFraction(BalanceData b, int insuranceLevel)
        {
            return 1.0 - Math.Pow(1.0 - b.insuranceUpgrade.effectPerLevel, insuranceLevel);
        }

        // ---------------- Bulgular ----------------

        /// <summary>Bu adıma inince bir bulguya denk gelme olasılığı (kilometre taşı dışında).</summary>
        public static double FindChance(BalanceData b, int step, int findChanceLevel)
        {
            int s = Math.Max(step, 1);
            double chance = b.findChanceBase
                          + b.findChancePerStep * (s - 1)
                          + b.findChanceUpgrade.effectPerLevel * findChanceLevel;
            return Clamp(chance, 0, b.maxFindChance);
        }

        /// <summary>Şans değeri: derinlik + yükseltme + bu turun tılsımları. Nadirlik ağırlıklarını kaydırır.</summary>
        public static double Luck(BalanceData b, int step, int luckLevel, double runLuck = 0)
        {
            return b.luckPerStep * Math.Max(step, 1) + b.luckUpgrade.effectPerLevel * luckLevel + runLuck;
        }

        /// <summary>
        /// Her nadirlik seviyesinin ağırlığı. minTier altındakiler 0 olur.
        /// weight(t) = tier.weight * (1 + luck) ^ t   -> şans arttıkça yüksek seviyeler daha çok çıkar.
        /// </summary>
        public static double[] TierWeights(BalanceData b, double luck, int minTier)
        {
            var weights = new double[b.rarityTiers.Length];
            for (int t = 0; t < weights.Length; t++)
            {
                weights[t] = t < minTier ? 0 : b.rarityTiers[t].weight * Math.Pow(1.0 + luck, t);
            }
            return weights;
        }

        /// <summary>Ağırlıkları olasılığa çevirir (toplamları 1 olur). Ekranda ve simülasyonda kullanışlı.</summary>
        public static double[] Normalize(double[] weights)
        {
            double total = 0;
            foreach (double w in weights) total += w;

            var result = new double[weights.Length];
            for (int i = 0; i < weights.Length; i++) result[i] = total > 0 ? weights[i] / total : 0;
            return result;
        }

        /// <summary>
        /// Ağırlıklı seçim. roll 0..1 arası bir zar. Hangi indeksin seçildiğini döndürür.
        /// Python'daki random.choices(weights=...) ile aynı mantık, ama zar dışarıdan.
        /// </summary>
        public static int WeightedPick(double[] weights, double roll)
        {
            double total = 0;
            foreach (double w in weights) total += w;

            double target = roll * total;
            double cumulative = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (weights[i] > 0 && target < cumulative) return i;
            }

            // Yuvarlama hatasına karşı: ağırlığı olan son indeks
            for (int i = weights.Length - 1; i >= 0; i--)
            {
                if (weights[i] > 0) return i;
            }
            return 0;
        }

        /// <summary>Verilen nadirlikte çıkabilecek bulgu türlerinin ağırlıkları (FindType sırasıyla).</summary>
        public static double[] FindTypeWeights(BalanceData b, int tier)
        {
            var types = (FindType[])Enum.GetValues(typeof(FindType));
            var weights = new double[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                FindTypeDefinition def = b.GetFindType(types[i]);
                weights[i] = tier >= def.minTier ? def.weight : 0;
            }
            return weights;
        }

        /// <summary>Para veren bulguların (Treasure, SafeCache) yükseltme çarpanı.</summary>
        public static double FindValueBonus(BalanceData b, int findValueLevel)
        {
            return Math.Pow(1.0 + b.findValueUpgrade.effectPerLevel, findValueLevel);
        }

        /// <summary>
        /// Bir bulgunun değeri. Türüne göre anlamı:
        /// Treasure/SafeCache = para, ProfitBoost = eklenecek bonus oranı, Safety = risk azaltma oranı, Shield = adet.
        /// </summary>
        public static double FindValue(BalanceData b, FindType type, int tier, double stepReward, int findValueLevel)
        {
            FindTypeDefinition def = b.GetFindType(type);
            double power = b.rarityTiers[tier].power;

            switch (type)
            {
                case FindType.Treasure:
                case FindType.SafeCache:
                case FindType.CursedTreasure:
                    return stepReward * def.baseValue * power * FindValueBonus(b, findValueLevel);
                case FindType.LuckCharm:
                    return def.baseValue * power;
                case FindType.FindStreak:
                    return Math.Floor(def.baseValue + b.streakStepsPerTier * tier);
                case FindType.Multiplier:
                    return Math.Min(def.baseValue * power, b.maxMultiplierFindBonus);
                case FindType.ProfitBoost:
                    return def.baseValue * power;
                case FindType.Safety:
                    return Math.Min(def.baseValue * power, b.maxSafetyFindReduction);
                case FindType.Shield:
                    return tier >= b.shieldDoubleTier ? 2 : 1;
                default:
                    return 0;
            }
        }

        // ---------------- Kilometre taşları ----------------

        /// <summary>Bu adım bir kilometre taşıysa tanımını, değilse null döndürür.</summary>
        public static MilestoneDefinition MilestoneAt(BalanceData b, int step)
        {
            foreach (MilestoneDefinition m in b.milestones)
            {
                if (m.step == step) return m;
            }
            return null;
        }

        /// <summary>Bu adımdan SONRAKİ ilk kilometre taşı. Yoksa null.</summary>
        public static MilestoneDefinition NextMilestone(BalanceData b, int currentStep)
        {
            MilestoneDefinition best = null;
            foreach (MilestoneDefinition m in b.milestones)
            {
                if (m.step > currentStep && (best == null || m.step < best.step)) best = m;
            }
            return best;
        }

        // ---------------- Savaş ----------------

        /// <summary>Bu katta canavar çıkma olasılığı (keşif katı hariç; orada boss garantili).</summary>
        public static double MonsterChance(CombatBalance c, int step)
        {
            return Math.Min(c.monsterChanceBase + c.monsterChancePerStep * (Math.Max(step, 1) - 1), c.maxMonsterChance);
        }

        public static double MonsterHealth(CombatBalance c, MonsterDefinition m, int step)
        {
            return c.monsterBaseHealth * Math.Pow(c.monsterHealthGrowth, Math.Max(step, 1) - 1) * m.hpMult;
        }

        public static double MonsterAttack(CombatBalance c, MonsterDefinition m, int step)
        {
            return c.monsterBaseAttack * Math.Pow(c.monsterAttackGrowth, Math.Max(step, 1) - 1) * m.atkMult;
        }

        /// <summary>
        /// Savunmadan sonra alınan hasar. Ölçekten bağımsız: savunma = 5 x saldırı ise hasar yarıya iner.
        /// </summary>
        public static double DamageAfterDefense(CombatBalance c, double attack, double defense)
        {
            if (attack <= 0) return 0;
            double reduction = defense / (defense + c.defenseFactor * attack);
            return Math.Max(1.0, attack * (1.0 - reduction));
        }

        /// <summary>Canavar kaç saniyede bir vurur (Game katmanı sayar).</summary>
        public static double MonsterAttackInterval(CombatBalance c, MonsterDefinition m)
        {
            return m.kind == MonsterKind.Normal ? c.monsterAttackInterval : c.monsterAttackInterval * c.bossAttackIntervalMult;
        }

        // ---------------- Ekipman ----------------

        /// <summary>Nadirlik çarpanı: gearRarityGrowth^tier (1.22 ile mor x4.9).</summary>
        public static double GearRarityMult(CombatBalance c, int tier) => Math.Pow(c.gearRarityGrowth, tier);

        // ---------------- Kazı gücü ----------------

        /// <summary>Duvar sertliği: derinlikle büyür.</summary>
        public static double WallHardness(CombatBalance c, int step) =>
            c.wallBaseHardness * Math.Pow(c.wallHardnessGrowth, Math.Max(step, 1) - 1);

        /// <summary>Bu katın duvarını yıkmak için gereken kazma vuruşu (1..maxSwingsPerFloor).</summary>
        public static int SwingsNeeded(CombatBalance c, int step, double digPower)
        {
            double swings = Math.Ceiling(WallHardness(c, step) / Math.Max(digPower, 0.1));
            return (int)Math.Max(1, Math.Min(swings, c.maxSwingsPerFloor));
        }

        // ---------------- Güçlendirme ----------------

        /// <summary>Bir sonraki seviyeye güçlendirme ücreti.</summary>
        public static double EnhanceCost(CombatBalance c, GearItem item)
        {
            double baseValue = c.sellBase * Math.Pow(c.gearStatGrowth, Math.Max(item.itemLevel, 1) - 1) * Math.Pow(1 + item.tier, 1.5);
            return Math.Ceiling(baseValue * c.enhanceCostMult * Math.Pow(c.enhanceCostGrowth, item.enhance));
        }

        /// <summary>Bir sonraki seviyeye çıkma şansı (0..1). Maks seviyedeyse 0.</summary>
        public static double EnhanceChance(CombatBalance c, GearItem item)
        {
            if (item.enhance >= c.enhanceMax || item.enhance >= c.enhanceSuccess.Length) return 0;
            return c.enhanceSuccess[item.enhance];
        }

        /// <summary>
        /// Eşyadaki bir statın değeri. Düz statlar derinlikle üstel, yüzde statlar yavaş (doğrusal) büyür.
        /// </summary>
        public static double GearStatValue(CombatBalance c, StatValue baseStat, double materialMult, int tier, int itemLevel)
        {
            int lvl = Math.Max(itemLevel, 1);
            double rarity = GearRarityMult(c, tier);
            if (CharacterStats.IsPercent(baseStat.stat))
            {
                return baseStat.value * materialMult * rarity * (1.0 + c.pctStatPerLevel * (lvl - 1));
            }
            return baseStat.value * materialMult * rarity * Math.Pow(c.gearStatGrowth, lvl - 1);
        }

        /// <summary>Ek rastgele stat sayısı: tier / 2 (mor = 4).</summary>
        public static int GearAffixCount(int tier) => Math.Max(0, tier / 2);

        /// <summary>Satış fiyatı. Derin ve nadir eşya daha değerli.</summary>
        public static double GearSellValue(CombatBalance c, GearItem item)
        {
            return Math.Ceiling(c.sellBase * Math.Pow(c.gearStatGrowth, Math.Max(item.itemLevel, 1) - 1) * Math.Pow(1 + item.tier, 1.5)
                                * (1.0 + 0.5 * item.enhance));
        }

        /// <summary>
        /// Eşyanın kaba "güç puanı": otomatik kuşanma ve karşılaştırma için.
        /// Yüzde statlar, eşyanın düz statları ölçeğinde bir karşılığa çevrilir.
        /// </summary>
        public static double GearPowerScore(CombatBalance c, GearItem item)
        {
            double scale = Math.Pow(c.gearStatGrowth, Math.Max(item.itemLevel, 1) - 1);
            double score = 0;
            foreach (StatValue s in item.stats)
            {
                switch (s.stat)
                {
                    case StatType.Attack: score += s.value * 3.0; break;
                    case StatType.Health: score += s.value * 0.5; break;
                    case StatType.Defense: score += s.value * 1.5; break;
                    case StatType.CritChance: score += s.value * 150 * scale; break;
                    case StatType.CritDamage: score += s.value * 40 * scale; break;
                    case StatType.CaveResist: score += s.value * 300 * scale; break;
                    case StatType.FindChance: score += s.value * 200 * scale; break;
                    case StatType.Luck: score += s.value * 100 * scale; break;
                    case StatType.GoldBonus: score += s.value * 120 * scale; break;
                    case StatType.DigPower: score += s.value * 4.0; break;
                }
            }
            return score * item.EnhanceMult(c.enhanceStatBonus);
        }

        // ---------------- Yükseltmeler ----------------

        /// <summary>Tur başında verilen baret sayısı.</summary>
        public static int StartShields(BalanceData b, int startShieldLevel)
        {
            return (int)Math.Round(b.startShieldUpgrade.effectPerLevel * startShieldLevel);
        }

        /// <summary>Şu anki seviyeden bir sonrakine geçmenin fiyatı. Yukarı yuvarlanır (tam sayı fiyatlar).</summary>
        public static double UpgradeCost(UpgradeDefinition def, int currentLevel)
        {
            return Math.Ceiling(def.baseCost * Math.Pow(def.costGrowth, currentLevel));
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
