using System;

namespace RiskLoop.Core
{
    /// <summary>
    /// GameSession'ın Yeni Çağ (prestij) kısmı.
    /// Yeni Çağ: para, yükseltmeler, ekipman, rekorlar, geçitler, seviye, yetenekler ve sınıf sıfırlanır; Yadigar kazanılır.
    /// Yadigar ile alınan Kadim yükseltmeler çağlar boyunca kalır, her çağ öncekinden hızlı ve derine gider.
    /// </summary>
    public partial class GameSession
    {
        private PrestigeBalance P => Balance.prestige;

        /// <summary>Şimdi Yeni Çağ başlatılırsa kazanılacak Yadigar.</summary>
        public double RelicsForPrestige => Math.Floor(Math.Pow(Progress.bestStep, P.relicExponent) / P.relicDivisor);

        public bool CanPrestige => !IsRunActive && IsUnlocked(Feature.Prestige) && Progress.bestStep >= P.minStep && RelicsForPrestige > 0;

        /// <summary>Yeni Çağ başlangıç parası (Kadim Servet).</summary>
        public double PrestigeStartingMoney
        {
            get
            {
                int lvl = Progress.GetPrestigeLevel(PrestigeType.AncientWealth);
                return Balance.startingMoney + (lvl > 0 ? P.ancientWealth.effectPerLevel * Math.Pow(4, lvl - 1) : 0);
            }
        }

        /// <summary>Yeni Çağ'ı başlat. Kazanılan Yadigar'ı döndürür (başlatılamadıysa 0).</summary>
        public double Prestige()
        {
            if (!CanPrestige) return 0;

            double gained = RelicsForPrestige;
            Progress.relics += gained;
            Progress.age++;

            // Sıfırlananlar
            Progress.upgradeLevels.Clear();
            Progress.equipped.Clear();
            Progress.inventory.Clear();
            Progress.conqueredMilestones.Clear();
            Progress.bestStep = 0;
            Progress.bestRunValue = 0;
            Progress.money = PrestigeStartingMoney;
            Progress.heroClass = (int)HeroClass.None;   // Yeni çağ, yeni sınıf seçimi
            // 11. tur: seviye, deneyim ve yetenekler de sıfırlanır (her çağ gerçekten yeni bir başlangıç).
            // Özellikler yeniden seviyeyle açılır; Kadim Bilgelik (XP çarpanı) bu yolculuğu hızlandırır.
            Progress.level = 1;
            Progress.xp = 0;
            Progress.talentRanks.Clear();
            Progress.seenTiers.Clear();   // İlk-kez açılış heyecanı yeni çağda tekrar yaşanır
            CurrentRun = null;
            return gained;
        }

        // ---------------- Kadim yükseltmeler ----------------

        public int GetPrestigeLevel(PrestigeType type) => Progress.GetPrestigeLevel(type);

        public bool IsPrestigeMaxed(PrestigeType type) => GetPrestigeLevel(type) >= P.Get(type).maxLevel;

        public double GetPrestigeCost(PrestigeType type) => RiskMath.UpgradeCost(P.Get(type), GetPrestigeLevel(type));

        public bool CanBuyPrestige(PrestigeType type) =>
            !IsRunActive && !IsPrestigeMaxed(type) && Progress.relics >= GetPrestigeCost(type);

        public bool TryBuyPrestige(PrestigeType type)
        {
            if (!CanBuyPrestige(type)) return false;
            Progress.relics -= GetPrestigeCost(type);
            Progress.SetPrestigeLevel(type, GetPrestigeLevel(type) + 1);
            return true;
        }
    }
}
