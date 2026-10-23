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

        /// <summary>Belirli bir canavar örneğinin (kendi can ve saldırısıyla) tehdit oranı: öldürene kadar kaybedilecek can oranı.</summary>
        public double MonsterThreatRatio(MonsterInstance m, CharacterStats s)
        {
            CombatBalance c = Balance.combat;
            double dps = s.Attack * (1.0 + s.CritChance * (s.CritDamage - 1.0)) / c.estimatedSecondsPerAttack;
            double secondsToKill = m.MaxHealth / Math.Max(dps, 0.001);
            double hitsTaken = secondsToKill / RiskMath.MonsterAttackInterval(c, m.Definition);
            double damage = hitsTaken * RiskMath.DamageAfterDefense(c, m.Attack, s.Defense);
            return damage / Math.Max(s.Health, 1.0);
        }

        /// <summary>Tehdit oranından zorluk kademesi 0 (açık beyaz, çok kolay) .. 8 (gri, çok zor).</summary>
        public int DifficultyBand(double ratio)
        {
            double[] t = Balance.combat.difficultyThresholds;
            int band = 0;
            while (band < t.Length && ratio >= t[band]) band++;
            return band;
        }

        /// <summary>Bu kademe XP verir mi? En kolay ve en zor kademe vermez.</summary>
        public bool DifficultyGivesXp(int band) => band > 0 && band < Balance.combat.difficultyThresholds.Length;

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
        /// Savaş gücü: normal canavarlar "Orta"dan kolay VE bölge bossu ölümcül değil olan en derin kat.
        /// </summary>
        public int PowerLevel(CharacterStats s)
        {
            CombatBalance c = Balance.combat;
            int level = 0;
            for (int step = 1; step <= InsightMaxStep; step++)
            {
                bool normalOk = ThreatRatio(step, MonsterKind.Normal, s) < c.threatMedium;
                bool miniOk = ThreatRatio(step, MonsterKind.Boss, s) < c.threatHard;   // Bölge bossunu sağ çıkarak yenebilir mi
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
        /// Göçük riskiyle, bir sonraki boss katına kadar ortalama kaç kat daha inebilirsin (savaş hariç, beklenen değer).
        /// Boss katında durur: oradan sonrası bossu yenmene bağlı.
        /// </summary>
        public double ExpectedFloorsLeft()
        {
            int startStep = CurrentStep;
            double alive = 1.0;
            double expected = 0;
            for (int n = 1; n <= InsightMaxStep; n++)
            {
                int step = startStep + n;
                alive *= 1.0 - BustChanceAt(step);
                expected += alive;
                if (RiskMath.IsBossFloor(Balance, step) || alive < 1e-4) break;
            }
            return expected;
        }

        /// <summary>Göçük olmadan bir sonraki boss katına ulaşma olasılığı (0..1).</summary>
        public double ReachNextBossChance()
        {
            int startStep = CurrentStep;
            double alive = 1.0;
            for (int step = startStep + 1; step <= startStep + InsightMaxStep; step++)
            {
                if (RiskMath.IsBossFloor(Balance, step)) break;
                alive *= 1.0 - BustChanceAt(step);
            }
            return alive;
        }

        // ---------------- Kumar oranı ----------------

        /// <summary>
        /// Sonraki iniş: kazanırsan (kat ödülü + Cesaret çarpanının kazandırdığı), kaybedersen (risk altındaki;
        /// sigorta sadece normal kısmı kurtarır, kumar kazancı tamamen gider), olasılık (göçük).
        /// </summary>
        public void NextGamble(out double win, out double lose, out double chance)
        {
            chance = NextBustChance;
            double atRisk = CurrentRun != null ? CurrentRun.Earnings : 0;
            double courage = CurrentRun != null ? CurrentRun.CourageEarnings : 0;
            double reward = NextStepReward;
            win = (atRisk + reward) * NextCourageMult - atRisk;
            lose = Math.Max(0, atRisk - courage) * (1.0 - InsuranceFraction) + courage;
        }

        /// <summary>Beklenen değer: kazanırsan * (1-olasılık) - kaybedersen * olasılık.</summary>
        public double NextGambleExpectedValue()
        {
            NextGamble(out double win, out double lose, out double chance);
            return win * (1.0 - chance) - lose * chance;
        }
    }
}
