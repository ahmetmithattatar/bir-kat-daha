using System;

namespace RiskLoop.Core
{
    /// <summary>
    /// Yeni Çağ (prestij) yükseltmeleri. Yadigar ile alınır, çağlar boyunca KALICIDIR.
    /// Kayıtta isimle tutulur: sona ekleme serbest, yeniden adlandırma eski kayıtları bozar.
    /// </summary>
    public enum PrestigeType
    {
        AncientGold,    // Kat ödülleri x(1+e)^lvl
        AncientLuck,    // Şans + e*lvl
        AncientShield,  // Her tura +1 baret / seviye
        AncientMight,   // Saldırı ve Can x(1+e)^lvl
        AncientWealth   // Yeni çağa başlangıç parası: e * 4^(lvl-1)
    }

    /// <summary>
    /// Yeni Çağ: her şeyi (para, yükseltmeler, eşyalar, rekorlar, geçitler) sıfırla, Yadigar kazan.
    /// Yadigar = floor(bestStep^relicExponent / relicDivisor). Sadece bestStep >= minStep iken açılır.
    /// Örnek (1.6, 10): 15. kat -> 7, 20 -> 12, 30 -> 23, 40 -> 36.
    /// Amaç: oyun "bitince" yeniden, daha güçlü ve daha hızlı başlamak (uzun vadeli hedef).
    /// </summary>
    [Serializable]
    public class PrestigeBalance
    {
        public int minStep = 15;
        public double relicExponent = 1.6;
        public double relicDivisor = 10;

        public UpgradeDefinition ancientGold = new UpgradeDefinition(3, 1.5, 0.25, 30);
        public UpgradeDefinition ancientLuck = new UpgradeDefinition(4, 1.6, 0.05, 20);
        public UpgradeDefinition ancientShield = new UpgradeDefinition(25, 3.0, 1, 1);
        public UpgradeDefinition ancientMight = new UpgradeDefinition(3, 1.5, 0.15, 30);
        public UpgradeDefinition ancientWealth = new UpgradeDefinition(5, 2.0, 500, 10);

        public UpgradeDefinition Get(PrestigeType type)
        {
            switch (type)
            {
                case PrestigeType.AncientGold: return ancientGold;
                case PrestigeType.AncientLuck: return ancientLuck;
                case PrestigeType.AncientShield: return ancientShield;
                case PrestigeType.AncientMight: return ancientMight;
                case PrestigeType.AncientWealth: return ancientWealth;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
