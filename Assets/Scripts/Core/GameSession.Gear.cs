using System;
using System.Collections.Generic;

namespace RiskLoop.Core
{
    /// <summary>
    /// GameSession'ın ekipman kısmı: eşya üretimi, envanter, kuşanma, satış.
    /// Envanter işlemleri sadece turlar arasında yapılır (tur sırasında build değişmez).
    /// </summary>
    public partial class GameSession
    {
        /// <summary>Eşyalar şu an yönetilebilir mi? (Tur yokken.)</summary>
        public bool CanManageGear => !IsRunActive;

        public int InventoryCap => Balance.combat.inventoryCap;

        // ---------------- Üretim ----------------

        /// <summary>
        /// Yeni eşya: yuva (eşit şans), o yuvada bu derinlikte düşebilen taban (ağırlıklı), taban statlar,
        /// sonra nadirliğe göre ek rastgele statlar. Statlar bir kez hesaplanır ve sabit kalır.
        /// </summary>
        private GearItem RollGearItem(int step, int tier)
        {
            CombatBalance c = Balance.combat;

            // 1) Yuva
            var slots = (GearSlot[])Enum.GetValues(typeof(GearSlot));
            GearSlot slot = slots[Math.Min((int)(_random.NextDouble() * slots.Length), slots.Length - 1)];   // ZAR: yuva

            // 2) Taban: bu yuvada, bu derinlikte düşebilenler
            var candidates = new List<GearBaseDefinition>();
            foreach (GearBaseDefinition g in c.gearBases)
            {
                if (g.slot == slot && step >= g.minStep) candidates.Add(g);
            }
            if (candidates.Count == 0)
            {
                // Bu yuvada uygun taban yoksa (veri hatası) ilk uygun tabana düş
                foreach (GearBaseDefinition g in c.gearBases) if (step >= g.minStep) { candidates.Add(g); break; }
            }
            var weights = new double[candidates.Count];
            for (int i = 0; i < weights.Length; i++) weights[i] = candidates[i].weight;
            GearBaseDefinition baseDef = candidates[RiskMath.WeightedPick(weights, _random.NextDouble())];   // ZAR: taban

            var item = new GearItem
            {
                uid = Progress.nextItemUid++,
                baseId = baseDef.id,
                slot = baseDef.slot,
                tier = tier,
                itemLevel = step
            };

            // 3) Taban statlar
            foreach (StatValue s in baseDef.stats)
            {
                item.stats.Add(new StatValue(s.stat, RiskMath.GearStatValue(c, s, baseDef.statMult, tier, step)));
            }

            // 4) Ek statlar: havuzdan tekrarsız seçim
            var pool = new List<StatValue>(c.affixPool);
            int count = Math.Min(RiskMath.GearAffixCount(tier), pool.Count);
            for (int i = 0; i < count; i++)
            {
                int index = Math.Min((int)(_random.NextDouble() * pool.Count), pool.Count - 1);   // ZAR: ek stat
                StatValue affix = pool[index];
                pool.RemoveAt(index);
                item.stats.Add(new StatValue(affix.stat, RiskMath.GearStatValue(c, affix, 1.0, tier, step)));
            }
            return item;
        }

        /// <summary>Yüzeye dönüşte turun ganimeti envantere girer. Yer yoksa otomatik satılır.</summary>
        private void MoveLootToInventory()
        {
            foreach (GearItem item in CurrentRun.PendingLoot)
            {
                if (Progress.inventory.Count < InventoryCap)
                {
                    Progress.inventory.Add(item);
                }
                else
                {
                    double value = RiskMath.GearSellValue(Balance.combat, item);
                    Progress.money += value;
                    CurrentRun.LootAutoSold++;
                    CurrentRun.LootAutoSoldValue += value;
                }
            }
        }

        // ---------------- Envanter ----------------

        public GearItem GetEquipped(GearSlot slot)
        {
            foreach (GearItem item in Progress.equipped)
            {
                if (item.slot == slot) return item;
            }
            return null;
        }

        /// <summary>uid'si verilen eşyayı bul (envanter ya da kuşanılmış).</summary>
        public GearItem FindItem(long uid)
        {
            foreach (GearItem item in Progress.inventory) if (item.uid == uid) return item;
            foreach (GearItem item in Progress.equipped) if (item.uid == uid) return item;
            return null;
        }

        public bool IsEquipped(GearItem item) => item != null && Progress.equipped.Contains(item);

        /// <summary>Envanterdeki eşyayı kuşan. Aynı yuvadaki eski eşya envantere döner.</summary>
        public bool Equip(long uid)
        {
            if (!CanManageGear) return false;
            GearItem item = Progress.inventory.Find(i => i.uid == uid);
            if (item == null) return false;

            GearItem old = GetEquipped(item.slot);
            Progress.inventory.Remove(item);
            if (old != null)
            {
                Progress.equipped.Remove(old);
                Progress.inventory.Add(old);
            }
            Progress.equipped.Add(item);
            return true;
        }

        /// <summary>Kuşanılmış eşyayı çıkar (envanterde yer varsa).</summary>
        public bool Unequip(long uid)
        {
            if (!CanManageGear || Progress.inventory.Count >= InventoryCap) return false;
            GearItem item = Progress.equipped.Find(i => i.uid == uid);
            if (item == null) return false;

            Progress.equipped.Remove(item);
            Progress.inventory.Add(item);
            return true;
        }

        public double SellValue(GearItem item) => RiskMath.GearSellValue(Balance.combat, item);

        /// <summary>Envanterdeki eşyayı sat. Satış fiyatını döndürür (satılamadıysa 0).</summary>
        public double Sell(long uid)
        {
            if (!CanManageGear) return 0;
            GearItem item = Progress.inventory.Find(i => i.uid == uid);
            if (item == null) return 0;

            double value = SellValue(item);
            Progress.inventory.Remove(item);
            Progress.money += value;
            return value;
        }

        /// <summary>Her yuva için en yüksek güç puanlı eşyayı kuşanır. Kaç eşya değiştiğini döndürür.</summary>
        public int AutoEquip()
        {
            if (!CanManageGear) return 0;
            int changed = 0;
            foreach (GearSlot slot in (GearSlot[])Enum.GetValues(typeof(GearSlot)))
            {
                GearItem current = GetEquipped(slot);
                GearItem best = current;
                double bestScore = current != null ? RiskMath.GearPowerScore(Balance.combat, current) : double.MinValue;
                foreach (GearItem item in Progress.inventory)
                {
                    if (item.slot != slot) continue;
                    double score = RiskMath.GearPowerScore(Balance.combat, item);
                    if (score > bestScore)
                    {
                        best = item;
                        bestScore = score;
                    }
                }
                if (best != null && best != current && Equip(best.uid)) changed++;
            }
            return changed;
        }
    }
}
