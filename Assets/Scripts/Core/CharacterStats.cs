using System;
using System.Collections.Generic;

namespace RiskLoop.Core
{
    /// <summary>
    /// Karakterin toplam statları = ekipmansız taban + kuşanılan eşyaların toplamı (güçlendirme dahil, sınırlarla).
    /// Savaş statları savaşta, kazı statları (CaveResist, FindChance, Luck, GoldBonus, DigPower) GameSession'da kazıya uygulanır.
    /// Prestij (Yeni Çağ) çarpanları GameSession.Stats içinde eklenir.
    /// </summary>
    public struct CharacterStats
    {
        public double Attack;
        public double Health;
        public double Defense;
        public double CritChance;
        public double CritDamage;   // Toplam çarpan (1.5 = kritik %150 hasar)
        public double CaveResist;
        public double FindChance;
        public double Luck;
        public double GoldBonus;
        public double DigPower;

        public static CharacterStats Compute(CombatBalance c, IEnumerable<GearItem> equipped)
        {
            var s = new CharacterStats
            {
                Attack = c.playerBaseAttack,
                Health = c.playerBaseHealth,
                Defense = c.playerBaseDefense,
                CritChance = c.playerBaseCritChance,
                CritDamage = c.playerBaseCritDamage,
                DigPower = c.playerBaseDigPower,
            };

            foreach (GearItem item in equipped)
            {
                if (item == null) continue;
                double mult = item.EnhanceMult(c.enhanceStatBonus);
                foreach (StatValue v in item.stats) s.Add(v.stat, v.value * mult);
            }

            s.ApplyCaps(c);
            return s;
        }

        /// <summary>Sınırlar: kritik, göçük direnci ve altın sonsuza gitmesin.</summary>
        public void ApplyCaps(CombatBalance c)
        {
            CritChance = Math.Min(CritChance, c.maxCritChance);
            CaveResist = Math.Min(CaveResist, c.maxCaveResist);
            GoldBonus = Math.Min(GoldBonus, c.maxGoldBonus);
        }

        public double Get(StatType type)
        {
            switch (type)
            {
                case StatType.Attack: return Attack;
                case StatType.Health: return Health;
                case StatType.Defense: return Defense;
                case StatType.CritChance: return CritChance;
                case StatType.CritDamage: return CritDamage;
                case StatType.CaveResist: return CaveResist;
                case StatType.FindChance: return FindChance;
                case StatType.Luck: return Luck;
                case StatType.GoldBonus: return GoldBonus;
                case StatType.DigPower: return DigPower;
                default: return 0;
            }
        }

        public void Add(StatType type, double value)
        {
            switch (type)
            {
                case StatType.Attack: Attack += value; break;
                case StatType.Health: Health += value; break;
                case StatType.Defense: Defense += value; break;
                case StatType.CritChance: CritChance += value; break;
                case StatType.CritDamage: CritDamage += value; break;
                case StatType.CaveResist: CaveResist += value; break;
                case StatType.FindChance: FindChance += value; break;
                case StatType.Luck: Luck += value; break;
                case StatType.GoldBonus: GoldBonus += value; break;
                case StatType.DigPower: DigPower += value; break;
            }
        }

        /// <summary>Yüzde olarak gösterilen statlar (Saldırı, Can, Savunma, Kazı gücü düz sayıdır).</summary>
        public static bool IsPercent(StatType type) =>
            type != StatType.Attack && type != StatType.Health && type != StatType.Defense && type != StatType.DigPower;
    }
}
