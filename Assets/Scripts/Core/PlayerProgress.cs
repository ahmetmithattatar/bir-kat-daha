using System;
using System.Collections.Generic;

namespace RiskLoop.Core
{
    /// <summary>Kayıt dosyasındaki tek bir yükseltme seviyesi.</summary>
    [Serializable]
    public class UpgradeLevelEntry
    {
        // Enum'un sayısı yerine adını saklıyoruz. Böylece enum'a yeni değer eklense de eski kayıtlar bozulmaz.
        public string id;
        public int level;
    }

    /// <summary>
    /// Turlar arasında kalıcı olan her şey: para, yükseltmeler, istatistikler.
    /// JSON'a kaydedilir. Alan eklerken saveVersion'ı artır.
    /// </summary>
    [Serializable]
    public class PlayerProgress
    {
        public int saveVersion = 4;
        public double money;
        public List<UpgradeLevelEntry> upgradeLevels = new List<UpgradeLevelEntry>();

        // Ekipman
        public List<GearItem> equipped = new List<GearItem>();   // Yuva başına en fazla bir eşya
        public List<GearItem> inventory = new List<GearItem>();
        public long nextItemUid = 1;
        public int monstersKilled;
        public int bossesKilled;

        // İstatistikler (ileride denge ve başarımlar için faydalı)
        public int totalRuns;
        public int totalBusts;
        public int bestStep;       // Derinlik rekoru. Kilometre taşı "ilk kez" ödülü buna bakar
        public int bestTier;       // Bulunan en yüksek nadirlik
        public int totalFinds;
        public int shieldsUsed;
        public double bestRunValue;  // En değerli turun toplamı ("rekor tur" tatmini için)

        public int GetLevel(UpgradeType type)
        {
            UpgradeLevelEntry entry = FindEntry(type);
            return entry != null ? entry.level : 0;
        }

        public void SetLevel(UpgradeType type, int level)
        {
            UpgradeLevelEntry entry = FindEntry(type);
            if (entry == null)
            {
                entry = new UpgradeLevelEntry { id = type.ToString() };
                upgradeLevels.Add(entry);
            }
            entry.level = level;
        }

        private UpgradeLevelEntry FindEntry(UpgradeType type)
        {
            string id = type.ToString();
            foreach (UpgradeLevelEntry entry in upgradeLevels)
            {
                if (entry.id == id) return entry;
            }
            return null;
        }
    }
}
