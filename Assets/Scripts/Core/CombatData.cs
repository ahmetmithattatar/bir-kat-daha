using System;
using System.Collections.Generic;

namespace RiskLoop.Core
{
    public enum MonsterKind
    {
        Normal,
        MiniBoss,
        Boss
    }

    /// <summary>
    /// Bir canavar türünün denge sayıları. İsim ve görsel Game katmanında (id ile eşlenir).
    /// Canlar ve saldırılar derinlikle üstel büyür, hpMult/atkMult türün güçlü/zayıf yanını verir.
    /// </summary>
    [Serializable]
    public class MonsterDefinition
    {
        public string id;
        public MonsterKind kind;
        public int minStep;      // Bu kattan itibaren çıkabilir
        public int maxStep;      // Bu kata kadar çıkabilir (bosslar için kullanılmaz)
        public double weight;    // Aynı aralıktaki canavarlar arasında seçilme ağırlığı
        public double hpMult;
        public double atkMult;

        public MonsterDefinition() { }
        public MonsterDefinition(string id, MonsterKind kind, int minStep, int maxStep, double weight, double hpMult, double atkMult)
        {
            this.id = id;
            this.kind = kind;
            this.minStep = minStep;
            this.maxStep = maxStep;
            this.weight = weight;
            this.hpMult = hpMult;
            this.atkMult = atkMult;
        }
    }

    /// <summary>
    /// Bir eşya tabanı (ör. "katana", "chain_gloves"). Taban statlar burada; derinlik ve nadirlikle büyütülür.
    /// </summary>
    [Serializable]
    public class GearBaseDefinition
    {
        public string id;
        public GearSlot slot;
        public int minStep;       // Bu kattan itibaren düşebilir (daha iyi malzemeler derinde)
        public double weight;
        public double statMult;   // Malzeme kalitesi (bez 0.8 ... hiçlik 2.0)
        public StatValue[] stats;

        public GearBaseDefinition() { }
        public GearBaseDefinition(string id, GearSlot slot, int minStep, double weight, double statMult, params StatValue[] stats)
        {
            this.id = id;
            this.slot = slot;
            this.minStep = minStep;
            this.weight = weight;
            this.statMult = statMult;
            this.stats = stats;
        }
    }

    /// <summary>
    /// Savaş, canavar ve ekipman denge sayıları. BalanceData.combat içinde durur, JSON'a onunla birlikte çıkar.
    /// </summary>
    [Serializable]
    public class CombatBalance
    {
        // --- Oyuncu (ekipmansız) ---
        public double playerBaseAttack = 10;
        public double playerBaseHealth = 100;
        public double playerBaseDefense = 0;
        public double playerBaseCritChance = 0.05;
        public double playerBaseCritDamage = 1.5;      // Kritik vuruş = saldırı x 1.5
        public double playerAttackCooldown = 0.16;     // İki vuruş arası en az süre (sn). Tık spamını sınırlar
        public double damageVariance = 0.10;           // Hasar +-%10 oynar
        public double playerBaseDigPower = 1.0;        // Kazmasız kazı gücü

        // --- Duvar sertliği: gereken vuruş = ceil(sertlik / kazı gücü), 1..maxSwingsPerFloor ---
        // sertlik(step) = wallBaseHardness * wallHardnessGrowth^(step-1). Kazmalar derinlikle daha hızlı büyür (1.14):
        // yeni kazma bulmak kazıyı gözle görülür hızlandırır.
        public double wallBaseHardness = 3.0;
        public double wallHardnessGrowth = 1.07;
        public int maxSwingsPerFloor = 8;

        // Savunma: alınan hasar = saldırı * (1 - def / (def + defenseFactor * saldırı))
        // Ölçekten bağımsız: savunma, canavar saldırısının 5 katıysa hasar yarıya iner.
        public double defenseFactor = 5;

        public double hpRegenPerFloor = 0.08;          // Savaşsız her katta canın %8'i dolar
        public double shieldReviveHealth = 0.30;       // Baret ölümü engellerse %30 canla kalkarsın

        // --- Stat sınırları ---
        public double maxCritChance = 0.60;
        public double maxCaveResist = 0.40;
        public double maxGoldBonus = 3.0;

        // --- Canavarlar ---
        // hp(step)  = monsterBaseHealth * monsterHealthGrowth^(step-1) * hpMult
        // atk(step) = monsterBaseAttack * monsterAttackGrowth^(step-1) * atkMult
        public double monsterBaseHealth = 40;
        public double monsterHealthGrowth = 1.18;
        public double monsterBaseAttack = 6;
        public double monsterAttackGrowth = 1.16;
        public double monsterAttackInterval = 1.6;     // Canavar kaç saniyede bir vurur (Game katmanı sayar)
        public double bossAttackIntervalMult = 0.85;   // Bosslar biraz daha sık vurur

        // Karşılaşma: chance(step) = min(monsterChanceBase + monsterChancePerStep*(step-1), maxMonsterChance)
        public double monsterChanceBase = 0.15;
        public double monsterChancePerStep = 0.012;
        public double maxMonsterChance = 0.5;

        public double bountySteps = 1.5;               // Öldürme ödülü = kat ödülü * bountySteps * hpMult (risk altında)

        // --- Ganimet ---
        public double normalDropChance = 0.40;
        public int miniBossDrops = 1;
        public int bossDrops = 2;

        // --- Ekipman ---
        // Düz statlar (Saldırı, Can, Savunma, Kazı gücü): taban * malzeme * nadirlik * gearStatGrowth^(itemLevel-1)
        // Yüzde statlar: taban * malzeme * nadirlik * (1 + pctStatPerLevel*(itemLevel-1))
        // nadirlik = gearRarityGrowth ^ tier   (1.22: mavi x1.5, yeşil x2.2, kırmızı x4, mor x4.9). Seviyeler arası fark hissedilir.
        // Ek rastgele stat sayısı = tier / 2  (mor = 4)
        public double gearStatGrowth = 1.14;
        public double gearRarityGrowth = 1.22;
        public double pctStatPerLevel = 0.02;
        public double sellBase = 5;                    // Satış = sellBase * gearStatGrowth^(lvl-1) * (1+tier)^1.5 * güçlendirme
        public int inventoryCap = 32;

        // --- Güçlendirme (+1 .. +10). Shaiya tarzı: şans düşer, yüksek seviyede başarısızlık acıtır ---
        // Her seviye tüm statları +%10. Ücret = satış değeri * enhanceCostMult * enhanceCostGrowth^seviye
        // Başarısızlık: seviye >= enhanceDowngradeFrom ise 1 düşer; seviye >= enhanceBreakFrom ise eşya KIRILIR (yok olur).
        public int enhanceMax = 10;
        public double enhanceStatBonus = 0.10;
        public double enhanceCostMult = 2.0;
        public double enhanceCostGrowth = 1.6;
        public int enhanceDowngradeFrom = 4;
        public int enhanceBreakFrom = 7;
        public double[] enhanceSuccess = { 0.95, 0.90, 0.85, 0.75, 0.65, 0.55, 0.45, 0.35, 0.25, 0.15 };

        // --- Güç göstergesi (InsightMath) ---
        // Oyuncu saniyede kaç vuruş yapar (tahmini; animasyon + bekleme). Tehdit = canavarı öldürene kadar alınan hasar / can.
        public double estimatedSecondsPerAttack = 0.3;
        public double threatEasy = 0.2;      // < %20 can kaybı: Kolay
        public double threatMedium = 0.5;    // < %50: Orta
        public double threatHard = 1.0;      // < %100: Zor, üstü Ölümcül
        // Ortalama boss çarpanları (havuzların ortalaması), güç seviyesi hesabı için
        public double avgMiniBossHpMult = 3.6;
        public double avgMiniBossAtkMult = 1.3;
        public double avgBossHpMult = 6.8;
        public double avgBossAtkMult = 1.55;

        public StatValue[] affixPool = DefaultContent.AffixPool();
        public GearBaseDefinition[] gearBases = DefaultContent.GearBases();
        public MonsterDefinition[] monsters = DefaultContent.Monsters();

        public MonsterDefinition FindMonster(string id)
        {
            foreach (MonsterDefinition m in monsters)
            {
                if (m.id == id) return m;
            }
            return null;
        }

        public GearBaseDefinition FindGearBase(string id)
        {
            foreach (GearBaseDefinition g in gearBases)
            {
                if (g.id == id) return g;
            }
            return null;
        }
    }

    // ---------------- Çalışma anı (tur içi) durumlar ----------------

    /// <summary>Şu an savaşılan canavar.</summary>
    public class MonsterInstance
    {
        public MonsterDefinition Definition;
        public int Step;
        public double MaxHealth;
        public double Health;
        public double Attack;
        public MilestoneDefinition Milestone;  // Boss ise hangi keşif katının bossu

        public MonsterKind Kind => Definition.kind;
        public bool IsDead => Health <= 0;
    }

    /// <summary>Canavar öldüğünde kazanılanlar.</summary>
    public class VictoryResult
    {
        public MonsterInstance Monster;
        public double Bounty;                                 // Tur kazancına (risk altında)
        public bool HasFind;                                  // Katın (ertelenmiş) bulgusu
        public FindResult Find;
        public List<FindResult> Drops = new List<FindResult>();  // Düşen eşyalar (Type = Gear)
        public bool UnlockedCheckpoint;                       // Bu zaferle yeni bir geçit açıldı mı?
    }

    /// <summary>Oyuncunun bir vuruşu.</summary>
    public struct AttackResult
    {
        public double Damage;
        public bool Crit;
        public bool Killed;
        public VictoryResult Victory;  // Killed ise dolu
    }

    /// <summary>Güçlendirme denemesinin sonucu.</summary>
    public struct EnhanceResult
    {
        public bool Attempted;   // Para yetmediyse ya da maks seviyedeyse false
        public bool Success;
        public bool Downgraded;
        public bool Destroyed;
        public int NewLevel;
        public double Cost;
        public double Chance;
    }

    /// <summary>Tehdit seviyesi (güç göstergesi).</summary>
    public enum ThreatLevel
    {
        Easy, Medium, Hard, Deadly
    }

    /// <summary>Canavarın bir vuruşu.</summary>
    public struct MonsterHitResult
    {
        public double Damage;
        public bool ShieldUsed;        // Can bitti ama baret kurtardı
        public bool PlayerDied;        // Tur bitti (göçük gibi)
        public double LostEarnings;
        public double InsurancePayout;
    }
}
