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
    /// Turlar arasında kalıcı olan her şey: para, yükseltmeler, ekipman, istatistikler, Yeni Çağ verileri.
    /// JSON'a kaydedilir. Alan eklerken saveVersion'ı artır.
    /// Yeni Çağda sıfırlananlar: para, yükseltmeler, ekipman, rekorlar, geçitler, seviye, XP, yetenekler, sınıf. KALANLAR: çağ, yadigar, kadim yükseltmeler.
    /// </summary>
    [Serializable]
    public class PlayerProgress
    {
        public int saveVersion = 7;
        public double money;
        public List<UpgradeLevelEntry> upgradeLevels = new List<UpgradeLevelEntry>();

        // Ekipman
        public List<GearItem> equipped = new List<GearItem>();   // Yuva başına en fazla bir eşya
        public List<GearItem> inventory = new List<GearItem>();
        public long nextItemUid = 1;
        public int monstersKilled;
        public int bossesKilled;

        // Geçitler: bossu yenilen keşif katları. Tura buradan başlanabilir.
        public List<int> conqueredMilestones = new List<int>();

        // İstatistikler (ileride denge ve başarımlar için faydalı)
        public int totalRuns;
        public int totalBusts;
        public int bestStep;       // Derinlik rekoru (bu çağ). Kilometre taşı "ilk kez" ödülü buna bakar
        public int bestTier;       // Bulunan en yüksek nadirlik
        public int totalFinds;
        public double bestRunValue;  // En değerli turun toplamı ("rekor tur" tatmini için)

        // Yeni Çağ (prestij): sıfırlanmaz
        public int age = 1;
        public double relics;
        public int allTimeBestStep;
        public List<UpgradeLevelEntry> prestigeLevels = new List<UpgradeLevelEntry>();

        // Oyuncu seviyesi ve yetenekler: Yeni Çağda da KALIR (hesap ilerlemesi)
        public int heroClass;   // HeroClass sayı olarak (0 = seçilmedi). Yeni Çağda sıfırlanır, yeniden seçilir
        public int level = 1;
        public double xp;                 // Mevcut seviyedeki XP
        public List<UpgradeLevelEntry> talentRanks = new List<UpgradeLevelEntry>();

        // Görülen nadirlikler: bir nadirlik ilk kez gelince gerilimli açılış, sonra hızlı geçiş. Sıfırlanmaz.
        public List<int> seenTiers = new List<int>();

        public int GetLevel(UpgradeType type) => GetLevel(upgradeLevels, type.ToString());
        public void SetLevel(UpgradeType type, int level) => SetLevel(upgradeLevels, type.ToString(), level);

        public int GetPrestigeLevel(PrestigeType type) => GetLevel(prestigeLevels, type.ToString());
        public void SetPrestigeLevel(PrestigeType type, int level) => SetLevel(prestigeLevels, type.ToString(), level);

        public int GetTalentRank(string id) => GetLevel(talentRanks, id);
        public void SetTalentRank(string id, int rank) => SetLevel(talentRanks, id, rank);

        private static int GetLevel(List<UpgradeLevelEntry> list, string id)
        {
            foreach (UpgradeLevelEntry entry in list)
            {
                if (entry.id == id) return entry.level;
            }
            return 0;
        }

        private static void SetLevel(List<UpgradeLevelEntry> list, string id, int level)
        {
            foreach (UpgradeLevelEntry entry in list)
            {
                if (entry.id == id)
                {
                    entry.level = level;
                    return;
                }
            }
            list.Add(new UpgradeLevelEntry { id = id, level = level });
        }
    }
}
