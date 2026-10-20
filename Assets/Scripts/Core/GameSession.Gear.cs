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

            // 3) Taban statlar (düz statlar seviyeyle üstel, yüzde statlar yavaş büyür)
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
                    double value = SellValue(item);
                    Progress.money += value;
                    CurrentRun.LootAutoSold++;
                    CurrentRun.LootAutoSoldValue += value;
                }
            }
        }

        // ---------------- Kudret (eşya puanı) ----------------

        /// <summary>Tek eşyanın Kudret puanı (malzeme kalitesi taban tanımından okunur).</summary>
        public double KudretOf(GearItem item)
        {
            if (item == null) return 0;
            GearBaseDefinition def = Balance.combat.FindGearBase(item.baseId);
            return RiskMath.GearKudret(Balance.combat, item, def != null ? def.statMult : 1.0);
        }

        /// <summary>Kuşanılan bütün eşyaların toplam Kudret'i.</summary>
        public double Kudret
        {
            get
            {
                double total = 0;
                foreach (GearItem item in Progress.equipped) total += KudretOf(item);
                return total;
            }
        }

        /// <summary>Bu eşyayı kuşansam toplam Kudret ne olur? (Aynı yuvadaki eşyanın yerine geçer.)</summary>
        public double KudretWith(GearItem item)
        {
            if (item == null) return Kudret;
            GearItem current = GetEquipped(item.slot);
            if (current != null && current.uid == item.uid) return Kudret;
            return Kudret - KudretOf(current) + KudretOf(item);
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

        /// <summary>Satış değeri (Pazarlıkçı yeteneğiyle artar).</summary>
        public double SellValue(GearItem item) => RiskMath.GearSellValue(Balance.combat, item) * (1.0 + Talents.SellValue);

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

        /// <summary>Envanterdeki (kuşanılmamış) tüm eşyaları sat. Toplam satış fiyatını döndürür.</summary>
        public double SellAllInventory(out int count)
        {
            count = 0;
            if (!CanManageGear) return 0;
            double total = 0;
            foreach (GearItem item in Progress.inventory) total += SellValue(item);
            count = Progress.inventory.Count;
            Progress.inventory.Clear();
            Progress.money += total;
            return total;
        }

        // ---------------- Güçlendirme ----------------

        public double EnhanceCost(GearItem item) => RiskMath.EnhanceCost(Balance.combat, item);
        public double EnhanceChance(GearItem item) => RiskMath.EnhanceChance(Balance.combat, item);
        public bool CanEnhance(GearItem item) =>
            CanManageGear && IsUnlocked(Feature.Enhance) && item != null && item.enhance < Balance.combat.enhanceMax && Progress.money >= EnhanceCost(item);

        /// <summary>
        /// Eşyayı güçlendirmeyi dene (+1). Ücret her durumda ödenir.
        /// Başarısızlıkta: yüksek seviyede 1 düşer, çok yüksek seviyede eşya KIRILIR (kuşanılmışsa da).
        /// </summary>
        public EnhanceResult Enhance(long uid)
        {
            GearItem item = FindItem(uid);
            var result = new EnhanceResult();
            if (!CanEnhance(item)) return result;

            CombatBalance c = Balance.combat;
            result.Attempted = true;
            result.Cost = EnhanceCost(item);
            result.Chance = EnhanceChance(item);
            Progress.money -= result.Cost;

            if (_random.NextDouble() < result.Chance)   // ZAR: güçlendirme
            {
                item.enhance++;
                result.Success = true;
            }
            else if (item.enhance >= c.enhanceBreakFrom)
            {
                Progress.inventory.Remove(item);
                Progress.equipped.Remove(item);
                result.Destroyed = true;
            }
            else if (item.enhance >= c.enhanceDowngradeFrom)
            {
                item.enhance--;
                result.Downgraded = true;
            }
            result.NewLevel = item.enhance;
            return result;
        }
    }
}
