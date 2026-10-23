using System;
using System.Collections.Generic;

namespace RiskLoop.Core
{
    /// <summary>
    /// GameSession'ın ilerleme kısmı: deneyim (XP), oyuncu seviyesi, seviyeyle açılan özellikler ve yetenek ağacı.
    /// Seviye ve yetenekler Yeni Çağda sıfırlanır (Kadim Bilgelik yeniden yükselmeyi hızlandırır).
    /// </summary>
    public partial class GameSession
    {
        private ProgressionBalance Prog => Balance.progression;

        // ---------------- Deneyim ve seviye ----------------

        /// <summary>Bu seviyeden bir sonrakine gereken XP.</summary>
        public double XpToNextLevel => XpRequired(Progress.level);

        public double XpRequired(int level) => Math.Ceiling(Prog.xpBase * Math.Pow(Prog.xpGrowth, Math.Max(level, 1) - 1));

        /// <summary>XP ekle (Kadim Bilgelik çarpanıyla). Atlanan seviye sayısını döndürür.</summary>
        private int GainXp(double amount)
        {
            double insight = Math.Pow(1.0 + Balance.prestige.ancientInsight.effectPerLevel, Progress.GetPrestigeLevel(PrestigeType.AncientInsight));
            Progress.xp += amount * insight;

            int levels = 0;
            while (Progress.xp >= XpToNextLevel)
            {
                Progress.xp -= XpToNextLevel;
                Progress.level++;
                levels++;
            }
            return levels;
        }

        // ---------------- Açılan özellikler ----------------

        public bool IsUnlocked(Feature feature) => Progress.level >= Prog.GetUnlockLevel(feature);

        private bool IsUnlocked(Feature? feature) => !feature.HasValue || IsUnlocked(feature.Value);

        public int UnlockLevel(Feature feature) => Prog.GetUnlockLevel(feature);

        /// <summary>Tam olarak bu seviyede açılan özellikler (seviye atlama penceresi için).</summary>
        public List<Feature> FeaturesUnlockedAt(int level)
        {
            var list = new List<Feature>();
            foreach (Feature f in (Feature[])Enum.GetValues(typeof(Feature)))
            {
                if (Prog.GetUnlockLevel(f) == level) list.Add(f);
            }
            return list;
        }

        /// <summary>Henüz açılmamış en yakın özellik (hedef göstermek için). Hepsi açıksa null.</summary>
        public Feature? NextLockedFeature
        {
            get
            {
                Feature? best = null;
                foreach (Feature f in (Feature[])Enum.GetValues(typeof(Feature)))
                {
                    int lvl = Prog.GetUnlockLevel(f);
                    if (lvl > Progress.level && (best == null || lvl < Prog.GetUnlockLevel(best.Value))) best = f;
                }
                return best;
            }
        }

        // ---------------- Yetenek ağacı ----------------

        private TalentBalance TB => Balance.talents;

        /// <summary>Kazanılan toplam yetenek puanı (her seviye 1, seviye 1'de 0).</summary>
        public int TalentPointsEarned => (Progress.level - 1) * Prog.talentPointsPerLevel;

        public int TalentPointsSpent
        {
            get
            {
                int spent = 0;
                foreach (TalentDefinition t in TB.nodes) spent += Progress.GetTalentRank(t.id);
                return spent;
            }
        }

        public int TalentPointsAvailable => IsUnlocked(Feature.Talents) ? TalentPointsEarned - TalentPointsSpent : 0;

        public int GetTalentRank(TalentDefinition t) => Progress.GetTalentRank(t.id);

        /// <summary>Bir dala harcanmış puan (sıraların açılması buna bağlı).</summary>
        public int PointsInBranch(TalentBranch branch)
        {
            int spent = 0;
            foreach (TalentDefinition t in TB.nodes)
            {
                if (t.branch == branch) spent += Progress.GetTalentRank(t.id);
            }
            return spent;
        }

        /// <summary>Düğümün sırası açık mı? (O dala row * pointsPerRow puan harcanmış olmalı.)</summary>
        public bool IsTalentRowOpen(TalentDefinition t) => PointsInBranch(t.branch) >= t.row * TB.pointsPerRow;

        public bool CanLearnTalent(TalentDefinition t) =>
            t != null && !IsRunActive && TalentPointsAvailable > 0 && IsTalentRowOpen(t) && GetTalentRank(t) < t.maxRank;

        public bool TryLearnTalent(string id)
        {
            TalentDefinition t = TB.Find(id);
            if (!CanLearnTalent(t)) return false;
            Progress.SetTalentRank(t.id, GetTalentRank(t) + 1);
            return true;
        }

        /// <summary>Yetenekleri sıfırlama ücreti: respecCostBase * seviye^2.</summary>
        public double RespecCost => Math.Ceiling(TB.respecCostBase * Progress.level * Progress.level);

        public bool CanRespec => !IsRunActive && TalentPointsSpent > 0 && Progress.money >= RespecCost;

        /// <summary>Tüm yetenek puanlarını geri al (ücretli). Başka bir build denemek için.</summary>
        public bool Respec()
        {
            if (!CanRespec) return false;
            Progress.money -= RespecCost;
            Progress.talentRanks.Clear();
            return true;
        }

        /// <summary>Harcanmış tüm yeteneklerin toplam etkisi.</summary>
        public TalentBonuses Talents
        {
            get
            {
                var bonus = new TalentBonuses();
                foreach (TalentDefinition t in TB.nodes)
                {
                    int rank = Progress.GetTalentRank(t.id);
                    if (rank > 0) bonus.Add(t.effect, rank * t.perRank);
                }
                return bonus;
            }
        }
    }
}
