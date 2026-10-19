using System;
using System.Collections.Generic;

namespace RiskLoop.Core
{
    /// <summary>Ekipman yuvaları. Kayıtta sayı olarak tutulur: SADECE SONA ekle, sırayı değiştirme.</summary>
    public enum GearSlot
    {
        Weapon, Helmet, Armor, Pants, Gloves, Boots, Necklace, Ring
    }

    /// <summary>
    /// Statlar. İlk beşi savaş, son dördü kazı statı (build hem savaşı hem kazıyı etkiler).
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
        GoldBonus    // Kat ödüllerini artırır (0.10 = +%10)
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
    /// Statları düşerken bir kez hesaplanır ve sabit kalır.
    /// </summary>
    [Serializable]
    public class GearItem
    {
        public long uid;           // Benzersiz numara (kuşanma/satma için)
        public string baseId;      // "katana", "chain_gloves" ...
        public GearSlot slot;
        public int tier;           // Nadirlik 0..8
        public int itemLevel;      // Düştüğü kat (statlar bununla büyür)
        public List<StatValue> stats = new List<StatValue>();

        /// <summary>Bu eşyadaki bir statın toplamı (aynı stat birden fazla satırda olabilir).</summary>
        public double Get(StatType type)
        {
            double total = 0;
            foreach (StatValue s in stats)
            {
                if (s.stat == type) total += s.value;
            }
            return total;
        }
    }
}
