using System;
using System.Collections.Generic;

namespace RiskLoop.Core
{
    /// <summary>Ekipman yuvaları. Kayıtta sayı olarak tutulur: SADECE SONA ekle, sırayı değiştirme.</summary>
    public enum GearSlot
    {
        Weapon, Helmet, Armor, Pants, Gloves, Boots, Necklace, Ring,
        Pickaxe   // Kazı gücü: duvarı daha az vuruşta yıkar
    }

    /// <summary>
    /// Statlar. Savaş statları, kazı statları ve kazı gücü (build hem savaşı hem kazıyı etkiler).
    /// Kayıtta sayı olarak tutulur: SADECE SONA ekle.
    /// </summary>
    public enum StatType
    {
        Attack,      // Vuruş hasarı (düz sayı)
        Health,      // Can (düz sayı)
        Defense,     // Savunma (düz sayı, canavar hasarını oransal azaltır)
        CritChance,  // Kritik şansı (0.05 = %5)
        CritDamage,  // Kritik hasar çarpanına eklenir (0.10 = +%10)
        CaveResist,  // Göçük riskini azaltır (0.05 = riski %5 azalt). Taban riskin altına inemez
        FindChance,  // Bulgu şansına eklenir
        Luck,        // Nadirlik şansına eklenir
        GoldBonus,   // Kat ödüllerini artırır (0.10 = +%10)
        DigPower     // Kazı gücü (düz sayı): duvar sertliği / kazı gücü = gereken vuruş
    }

    /// <summary>Bir stat ve değeri.</summary>
    [Serializable]
    public class StatValue
    {
        public StatType stat;
        public double value;

        public StatValue() { }
        public StatValue(StatType stat, double value) { this.stat = stat; this.value = value; }
    }

    /// <summary>
    /// Bir eşya. Kayda girer (JsonUtility). baseId -> GearBaseDefinition (stat profili) ve Game katmanında isim/görsel.
    /// Statları düşerken bir kez hesaplanır. Güçlendirme (enhance) her seviyede tüm statları %10 artırır.
    /// </summary>
    [Serializable]
    public class GearItem
    {
        public long uid;           // Benzersiz numara (kuşanma/satma için)
        public string baseId;      // "katana", "chain_gloves" ...
        public GearSlot slot;
        public int tier;           // Nadirlik 0..8
        public int itemLevel;      // Düştüğü kat (statlar bununla büyür)
        public int enhance;        // Güçlendirme seviyesi (+0 .. +10)
        public List<StatValue> stats = new List<StatValue>();

        /// <summary>Güçlendirme çarpanı (CombatBalance.enhanceStatBonus ile). Varsayılan %10/seviye.</summary>
        public double EnhanceMult(double bonusPerLevel) => 1.0 + bonusPerLevel * enhance;

        /// <summary>Bu eşyadaki bir statın HAM toplamı (güçlendirmesiz).</summary>
        public double Get(StatType type)
        {
            double total = 0;
            foreach (StatValue s in stats)
            {
                if (s.stat == type) total += s.value;
            }
            return total;
        }

        /// <summary>Güçlendirme dahil statın değeri (ekranda ve hesaplarda bu kullanılır).</summary>
        public double GetEffective(StatType type, double bonusPerLevel) => Get(type) * EnhanceMult(bonusPerLevel);
    }
}
