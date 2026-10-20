using System;

namespace RiskLoop.Core
{
    /// <summary>
    /// Kahraman sınıfı. Kayıtta SAYI olarak durur: SADECE SONA ekle. None = henüz seçilmedi (ilk girişte seçim ekranı).
    /// </summary>
    public enum HeroClass
    {
        None, Warrior, Mage, Archer
    }

    /// <summary>
    /// Büyü etkileri. Büyüler sadece savaşa değil kazıya, riske, paraya ve bulgulara da etki eder.
    /// Süreler SANİYE değil kat ya da vuruş sayısıdır: Core zaman bilmez, kurallar test edilebilir kalır.
    /// </summary>
    public enum SpellEffect
    {
        StrikeMult,      // Savaş: hemen value x saldırı gücünde bir vuruş
        MultiStrike,     // Savaş: hemen (int)value adet normal vuruş
        Heal,            // Canın value oranı kadar iyileş
        SafeFloors,      // duration kat boyunca göçük riski x(1 - value) (taban risk yine geçerli)
        RewardFloors,    // duration kat boyunca kat ödülü x value
        BankEarnings,    // Risk altındaki kazancın value oranı hemen cebe (güvende)
        Foresight,       // Kehanet: sonraki katın zarı şimdi atılır, göçük olup olmayacağı söylenir
        FindBoost,       // Sonraki katta bulgu garantili ve nadirlik +(int)value basamak
        GuaranteedFinds, // duration kat boyunca bulgu garantili (eski harita gibi)
        Ward,            // Sonraki duration canavar vuruşunun hasarı x(1 - value)
        CritCharges,     // Sonraki duration vuruş kesin kritik
        CalmBoss,        // Bossun öfkesi sıfırlanır + value oranında can
        LootHunt         // Sonraki zaferde en az 1 eşya düşer, nadirliği +(int)value
    }

    /// <summary>Bir büyünün tanımı. İsim ve açıklama Game katmanında (UIStrings), id ile eşlenir.</summary>
    [Serializable]
    public class SpellDefinition
    {
        public string id;
        public HeroClass heroClass;
        public int unlockLevel;      // Bu oyuncu seviyesinde açılır (büyü kitabına girer)
        public double manaCost;
        public SpellEffect effect;
        public double value;
        public int duration;         // Kat ya da vuruş sayısı (etkiye göre)

        public SpellDefinition() { }
        public SpellDefinition(string id, HeroClass heroClass, int unlockLevel, double manaCost, SpellEffect effect, double value, int duration = 0)
        {
            this.id = id;
            this.heroClass = heroClass;
            this.unlockLevel = unlockLevel;
            this.manaCost = manaCost;
            this.effect = effect;
            this.value = value;
            this.duration = duration;
        }

        /// <summary>Sadece savaşta kullanılabilir mi?</summary>
        public bool CombatOnly => effect == SpellEffect.StrikeMult || effect == SpellEffect.MultiStrike || effect == SpellEffect.CalmBoss;

        /// <summary>Sadece kazarken (savaş dışında) kullanılabilir mi?</summary>
        public bool DigOnly => effect == SpellEffect.Foresight || effect == SpellEffect.BankEarnings || effect == SpellEffect.FindBoost;
    }

    /// <summary>Sınıfın taban stat farkları. Çarpanlar 1 = değişmez.</summary>
    [Serializable]
    public class ClassDefinition
    {
        public HeroClass heroClass;
        public double healthMult = 1.0;
        public double attackMult = 1.0;
        public double defenseMult = 1.0;
        public double digPowerMult = 1.0;
        public double critChance;     // Eklenen
        public double findChance;     // Eklenen
        public double luck;           // Eklenen
        public double maxManaBonus;   // Eklenen

        public ClassDefinition() { }
    }

    /// <summary>
    /// Sınıflar, mana ve büyüler. BalanceData.classes içinde, JSON'a onunla çıkar.
    ///
    /// Mana: tura startManaFraction dolu başlar, her inilen katta manaPerFloor, her öldürmede manaPerKill dolar.
    /// Yani derine inmek büyü yakıtı demek: risk almak büyüyle ödüllendirilir.
    /// Her sınıfın 6 büyüsü var, seviye 1, 3, 5, 8, 11, 14'te açılır.
    /// </summary>
    [Serializable]
    public class ClassBalance
    {
        public double baseMaxMana = 100;
        public double startManaFraction = 0.5;
        public double manaPerFloor = 12;
        public double manaPerKill = 18;

        public ClassDefinition[] classes =
        {
            // Savaşçı: dayanıklı, yakın dövüş
            new ClassDefinition { heroClass = HeroClass.Warrior, healthMult = 1.3, defenseMult = 1.35, attackMult = 1.05 },
            // Büyücü: güçlü vuruş, şans, bol mana
            new ClassDefinition { heroClass = HeroClass.Mage, attackMult = 1.2, healthMult = 0.9, luck = 0.06, maxManaBonus = 50 },
            // Okçu: kritik, bulgu, hızlı kazı
            new ClassDefinition { heroClass = HeroClass.Archer, critChance = 0.08, findChance = 0.03, digPowerMult = 1.15 },
        };

        public SpellDefinition[] spells =
        {
            // ---- Savaşçı ----
            new SpellDefinition("w_cleave",     HeroClass.Warrior, 1,  30, SpellEffect.StrikeMult, 3.0),
            new SpellDefinition("w_iron_will",  HeroClass.Warrior, 3,  40, SpellEffect.Heal, 0.4),
            new SpellDefinition("w_shoulder",   HeroClass.Warrior, 5,  45, SpellEffect.SafeFloors, 0.4, 3),
            new SpellDefinition("w_warcry",     HeroClass.Warrior, 8,  35, SpellEffect.CalmBoss, 0.15),
            new SpellDefinition("w_greed",      HeroClass.Warrior, 11, 60, SpellEffect.RewardFloors, 2.0, 2),
            new SpellDefinition("w_titan",      HeroClass.Warrior, 14, 50, SpellEffect.Ward, 0.8, 4),

            // ---- Büyücü ----
            new SpellDefinition("m_fireball",   HeroClass.Mage, 1,  25, SpellEffect.StrikeMult, 2.5),
            new SpellDefinition("m_foresight",  HeroClass.Mage, 3,  40, SpellEffect.Foresight, 0),
            new SpellDefinition("m_alchemy",    HeroClass.Mage, 5,  50, SpellEffect.BankEarnings, 0.25),
            new SpellDefinition("m_frost",      HeroClass.Mage, 8,  35, SpellEffect.Ward, 0.7, 3),
            new SpellDefinition("m_arcane_eye", HeroClass.Mage, 11, 60, SpellEffect.FindBoost, 2),
            new SpellDefinition("m_timebend",   HeroClass.Mage, 14, 70, SpellEffect.SafeFloors, 0.6, 2),

            // ---- Okçu ----
            new SpellDefinition("a_volley",     HeroClass.Archer, 1,  30, SpellEffect.MultiStrike, 4),
            new SpellDefinition("a_scout",      HeroClass.Archer, 3,  35, SpellEffect.GuaranteedFinds, 0, 2),
            new SpellDefinition("a_eagle",      HeroClass.Archer, 5,  35, SpellEffect.CritCharges, 0, 5),
            new SpellDefinition("a_pathfinder", HeroClass.Archer, 8,  50, SpellEffect.SafeFloors, 0.3, 4),
            new SpellDefinition("a_hunt",       HeroClass.Archer, 11, 55, SpellEffect.LootHunt, 1),
            new SpellDefinition("a_deadeye",    HeroClass.Archer, 14, 70, SpellEffect.StrikeMult, 5.0),
        };

        public ClassDefinition Get(HeroClass c)
        {
            foreach (ClassDefinition d in classes) if (d.heroClass == c) return d;
            return null;
        }

        public SpellDefinition FindSpell(string id)
        {
            foreach (SpellDefinition s in spells) if (s.id == id) return s;
            return null;
        }
    }

    /// <summary>Bir büyünün sonucu (UI sahnelemek için).</summary>
    public struct SpellResult
    {
        public bool Success;
        public SpellDefinition Spell;
        public bool HasAttack;          // Vuruşlu büyü (StrikeMult / MultiStrike)
        public AttackResult Attack;     // Son vuruşun sonucu (öldürdüyse Victory içinde)
        public double TotalDamage;
        public int Hits;
        public bool ForesightBust;      // Kehanet: sonraki kat göçecek mi?
        public double Healed;
        public double Banked;
    }
}
