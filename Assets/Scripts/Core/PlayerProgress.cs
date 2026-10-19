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
    /// Oto-kazı durma kuralları (oyuncunun tercihi, kayda girer).
    /// Oto-kazı bu koşullardan biri olunca kendiliğinden durur ve kontrol oyuncuya geçer.
    /// </summary>
    [Serializable]
    public class AutoDigSettings
    {
        public bool stopWhenShieldsGone = true;  // Baret bitince dur (oto başlarken baret varsa)
        public int stopRiskPercent = 30;         // Göçük riski bunu geçince dur
        public int stopTier = 4;                 // Bu nadirlik ve üstü bulguda dur (4 = yeşil)
        public bool autoFightNormal = true;      // Normal canavarlarla kendisi savaşsın (boss gelince her zaman durur)
    }

    /// <summary>
    /// Turlar arasında kalıcı olan her şey: para, yükseltmeler, ekipman, istatistikler, Yeni Çağ verileri.
    /// JSON'a kaydedilir. Alan eklerken saveVersion'ı artır.
    /// Yeni Çağ'da sıfırlananlar: para, yükseltmeler, ekipman, rekorlar, geçitler. KALANLAR: çağ, yadigar, kadim yükseltmeler, ayarlar.
    /// </summary>
    [Serializable]
    public class PlayerProgress
    {
        public int saveVersion = 5;
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
        public int shieldsUsed;
        public double bestRunValue;  // En değerli turun toplamı ("rekor tur" tatmini için)
        public int pityStacks;       // Talihsizlik telafisi: art arda göçük/yenilgi sayısı (sınırlı)

        // Yeni Çağ (prestij): sıfırlanmaz
        public int age = 1;
        public double relics;
        public int allTimeBestStep;
        public List<UpgradeLevelEntry> prestigeLevels = new List<UpgradeLevelEntry>();

        // Oto-kazı tercihleri: sıfırlanmaz
        public AutoDigSettings autoDig = new AutoDigSettings();

        public int GetLevel(UpgradeType type) => GetLevel(upgradeLevels, type.ToString());
        public void SetLevel(UpgradeType type, int level) => SetLevel(upgradeLevels, type.ToString(), level);

        public int GetPrestigeLevel(PrestigeType type) => GetLevel(prestigeLevels, type.ToString());
        public void SetPrestigeLevel(PrestigeType type, int level) => SetLevel(prestigeLevels, type.ToString(), level);

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
