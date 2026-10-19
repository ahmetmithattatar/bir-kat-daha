using System;
using System.Collections.Generic;

namespace RiskLoop.Core
{
    /// <summary>
    /// GameSession'ın "durumu anlama" kısmı: oyuncunun ne kadar güçlü olduğunu, bulunduğu katın ne kadar tehlikeli
    /// olduğunu ve bir sonraki inişin kumar oranını hesaplar. Hiçbir şeyi DEĞİŞTİRMEZ, zar atmaz (beklenen değerler).
    ///
    ///   Savaş gücü (PowerLevel): normal canavarları rahat, mini bossları sağ çıkarak yenebileceğin en derin kat.
    ///     WoW'daki eşya seviyesi gibi: "Gücün 14. kat seviyesinde". Eşya ve yükseltmelerin ilerlemesini tek sayıda gösterir.
    ///   Tehdit (ThreatAt): canavarı öldürene kadar canının yüzde kaçını kaybedeceğin -> Kolay / Orta / Zor / Ölümcül.
    ///   Kalan kat (ExpectedFloorsLeft): göçük riski ve baretlerle ortalama kaç kat daha dayanırsın.
    ///   Kumar (NextGamble): sonraki inişte kazanacağın, kaybedebileceğin ve olasılığı.
    /// </summary>
    public partial class GameSession
    {
        private const int InsightMaxStep = 80;

        // ---------------- Savaş tehdidi ----------------

        /// <summary>Bu kattaki (ortalama) bir canavarı öldürene kadar kaybedilen canın oranı. 1 = ölürsün.</summary>
        public double ThreatRatio(int step, MonsterKind kind, CharacterStats s)
        {
            CombatBalance c = Balance.combat;
            double hpMult = kind == MonsterKind.Boss ? c.avgBossHpMult : kind == MonsterKind.MiniBoss ? c.avgMiniBossHpMult : 1.0;
            double atkMult = kind == MonsterKind.Boss ? c.avgBossAtkMult : kind == MonsterKind.MiniBoss ? c.avgMiniBossAtkMult : 1.0;
            var avg = new MonsterDefinition("avg", kind, step, step, 1, hpMult, atkMult);

            double dps = s.Attack * (1.0 + s.CritChance * (s.CritDamage - 1.0)) / c.estimatedSecondsPerAttack;
            double secondsToKill = RiskMath.MonsterHealth(c, avg, step) / Math.Max(dps, 0.001);
            double hitsTaken = secondsToKill / RiskMath.MonsterAttackInterval(c, avg);
            double damage = hitsTaken * RiskMath.DamageAfterDefense(c, RiskMath.MonsterAttack(c, avg, step), s.Defense);
            return damage / Math.Max(s.Health, 1.0);
        }

        public ThreatLevel ToThreatLevel(double ratio)
        {
            CombatBalance c = Balance.combat;
            if (ratio < c.threatEasy) return ThreatLevel.Easy;
            if (ratio < c.threatMedium) return ThreatLevel.Medium;
            if (ratio < c.threatHard) return ThreatLevel.Hard;
            return ThreatLevel.Deadly;
        }

        /// <summary>Bir katın tehdidi (keşif katıysa bossa göre).</summary>
        public ThreatLevel ThreatAt(int step)
        {
            MilestoneDefinition m = RiskMath.MilestoneAt(Balance, step);
            MonsterKind kind = MonsterKind.Normal;
            if (m != null && m.HasBoss)
            {
                MonsterDefinition first = Balance.combat.FindMonster(m.bossPool[0]);
                kind = first != null ? first.kind : MonsterKind.MiniBoss;
            }
            return ToThreatLevel(ThreatRatio(step, kind, Stats));
        }

        /// <summary>Şu an savaşılan canavarın tehdidi (yoksa sonraki katınki).</summary>
        public ThreatLevel CurrentThreat
        {
            get
            {
                if (InCombat)
                {
                    MonsterInstance m = CurrentRun.Monster;
                    double ratio = ThreatRatio(m.Step, m.Kind, Stats) * (m.Health / m.MaxHealth);
                    // Kalan cana göre: canın azsa aynı canavar daha tehlikeli
                    double healthLeft = CurrentRun.PlayerHealth / Math.Max(CurrentRun.PlayerMaxHealth, 1.0);
                    return ToThreatLevel(ratio / Math.Max(healthLeft, 0.05));
                }
                return ThreatAt(CurrentStep + 1);
            }
        }

        // ---------------- Savaş gücü ----------------

        /// <summary>
        /// Savaş gücü: normal canavarlar "Orta"dan kolay VE mini bosslar ölümcül değil olan en derin kat.
        /// </summary>
        public int PowerLevel(CharacterStats s)
        {
            CombatBalance c = Balance.combat;
            int level = 0;
            for (int step = 1; step <= InsightMaxStep; step++)
            {
                bool normalOk = ThreatRatio(step, MonsterKind.Normal, s) < c.threatMedium;
                bool miniOk = ThreatRatio(step, MonsterKind.MiniBoss, s) < c.threatHard;
                if (!normalOk || !miniOk) break;
                level = step;
            }
            return Math.Max(level, 1);
        }

        public int CurrentPowerLevel => PowerLevel(Stats);

        /// <summary>Bu eşyayı kuşansam savaş gücüm kaç olur? (Karşılaştırma için.)</summary>
        public int PowerLevelWith(GearItem item)
        {
            var gear = new List<GearItem>();
            foreach (GearItem e in Progress.equipped)
            {
                if (item == null || e.slot != item.slot) gear.Add(e);
            }
            if (item != null) gear.Add(item);
            return PowerLevel(StatsWith(gear));
        }

        /// <summary>Bu eşyayı kuşansam bir katın duvarı kaç vuruşta yıkılır? (Kazma karşılaştırması.)</summary>
        public int SwingsWith(GearItem item, int step)
        {
            var gear = new List<GearItem>();
            foreach (GearItem e in Progress.equipped)
            {
                if (item == null || e.slot != item.slot) gear.Add(e);
            }
            if (item != null) gear.Add(item);
            return RiskMath.SwingsNeeded(Balance.combat, step, StatsWith(gear).DigPower);
        }

        // ---------------- Göçük: kaç kat daha? ----------------

        /// <summary>
        /// Göçük riski ve baretlerle ortalama kaç kat daha dayanırsın (savaş hariç, beklenen değer).
        /// Baret sayısına göre olasılık dağılımı adım adım ilerletilir (dinamik programlama).
        /// </summary>
        public double ExpectedFloorsLeft()
        {
            int startStep = CurrentStep;
            int shields = CurrentRun != null ? CurrentRun.Shields : RiskMath.StartShields(Balance, Level(UpgradeType.StartShield));
            double safety = RunSafetyFactor * (1.0 - Stats.CaveResist);

            // alive[j] = j baret kalmışken hâlâ hayatta olma olasılığı
            var alive = new double[shields + 1];
            alive[shields] = 1.0;
            double expected = 0;

            for (int n = 1; n <= InsightMaxStep; n++)
            {
                double p = RiskMath.BustChance(Balance, startStep + n, Level(UpgradeType.Safety), safety);
                var next = new double[shields + 1];
                for (int j = 0; j <= shields; j++)
                {
                    if (alive[j] <= 0) continue;
                    next[j] += alive[j] * (1.0 - p);        // Göçük yok
                    if (j > 0) next[j - 1] += alive[j] * p;  // Göçük, baret yuttu
                }
                alive = next;

                double total = 0;
                foreach (double a in alive) total += a;
                expected += total;
                if (total < 1e-4) break;
            }
            return expected;
        }

        // ---------------- Kumar oranı ----------------

        /// <summary>Sonraki iniş: kazanırsan (kat ödülü), kaybedersen (risk altındaki - sigorta; baret varsa 0), olasılık.</summary>
        public void NextGamble(out double win, out double lose, out double chance)
        {
            win = NextStepReward;
            chance = NextBustChance;
            bool shielded = CurrentRun != null && CurrentRun.Shields > 0;
            double atRisk = CurrentRun != null ? CurrentRun.Earnings : 0;
            lose = shielded ? 0 : atRisk * (1.0 - InsuranceFraction);
        }

        /// <summary>Beklenen değer: kazanırsan * (1-olasılık) - kaybedersen * olasılık.</summary>
        public double NextGambleExpectedValue()
        {
            NextGamble(out double win, out double lose, out double chance);
            return win * (1.0 - chance) - lose * chance;
        }
    }
}
