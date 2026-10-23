using System;
using System.Collections.Generic;

namespace RiskLoop.Core
{
    /// <summary>
    /// GameSession'ın sınıf ve büyü kısmı.
    ///   Sınıf: ilk girişte seçilir (Savaşçı / Büyücü / Okçu). Taban statları ve silah türlerini belirler.
    ///   Büyü kitabı: her sınıfın 6 büyüsü, oyuncu seviyesiyle açılır. Büyüler mana harcar.
    ///   Mana: tura yarı dolu başlar, her katta ve her öldürmede dolar. Derine inmek = daha çok büyü.
    /// Büyüler sadece savaşa değil kazıya (risk, kat ödülü), paraya (cebe aktarma) ve bulgulara etki eder.
    /// </summary>
    public partial class GameSession
    {
        private ClassBalance CB => Balance.classes;

        // ---------------- Sınıf ----------------

        public HeroClass CurrentClass => (HeroClass)Progress.heroClass;

        public bool HasClass => CurrentClass != HeroClass.None;

        public ClassDefinition ClassDef => CB.Get(CurrentClass);

        /// <summary>Sınıf seç (sadece seçilmemişken, tur dışında).</summary>
        public bool ChooseClass(HeroClass heroClass)
        {
            if (HasClass || IsRunActive || heroClass == HeroClass.None || CB.Get(heroClass) == null) return false;
            Progress.heroClass = (int)heroClass;
            return true;
        }

        /// <summary>Bu silahı bu sınıf kullanabilir mi? (Silah dışı eşyalar herkese uygun.)</summary>
        public bool CanUseItem(GearItem item)
        {
            if (item == null || item.slot != GearSlot.Weapon) return true;
            GearBaseDefinition def = Balance.combat.FindGearBase(item.baseId);
            return def == null || def.heroClass == HeroClass.None || !HasClass || def.heroClass == CurrentClass;
        }

        /// <summary>Bu taban bu sınıfa düşebilir mi? (Silahlar sadece kendi sınıfına; sınıf seçilmemişse hepsi.)</summary>
        private bool IsDroppableForClass(GearBaseDefinition g) =>
            g.slot != GearSlot.Weapon || g.heroClass == HeroClass.None || !HasClass || g.heroClass == CurrentClass;

        /// <summary>Sınıf farkını statlara uygular (StatsWith içinden).</summary>
        private void ApplyClass(ref CharacterStats s)
        {
            ClassDefinition d = ClassDef;
            if (d == null) return;
            s.Health *= d.healthMult;
            s.Attack *= d.attackMult;
            s.Defense *= d.defenseMult;
            s.DigPower *= d.digPowerMult;
            s.CritChance += d.critChance;
            s.FindChance += d.findChance;
            s.Luck += d.luck;
        }

        // ---------------- Büyü kitabı ----------------

        public double MaxMana => CB.baseMaxMana + (ClassDef != null ? ClassDef.maxManaBonus : 0);

        /// <summary>Sınıfın bütün büyüleri (açılış seviyesine göre sıralı).</summary>
        public List<SpellDefinition> ClassSpells
        {
            get
            {
                var list = new List<SpellDefinition>();
                foreach (SpellDefinition s in CB.spells) if (s.heroClass == CurrentClass) list.Add(s);
                list.Sort((a, b) => a.unlockLevel.CompareTo(b.unlockLevel));
                return list;
            }
        }

        public bool IsSpellKnown(SpellDefinition s) => s != null && s.heroClass == CurrentClass && Progress.level >= s.unlockLevel;

        /// <summary>Bilinen büyüler (kısayol çubuğu için).</summary>
        public List<SpellDefinition> KnownSpells
        {
            get
            {
                var list = new List<SpellDefinition>();
                foreach (SpellDefinition s in ClassSpells) if (IsSpellKnown(s)) list.Add(s);
                return list;
            }
        }

        /// <summary>Tam olarak bu seviyede açılan büyüler (seviye penceresi için).</summary>
        public List<SpellDefinition> SpellsUnlockedAt(int level)
        {
            var list = new List<SpellDefinition>();
            foreach (SpellDefinition s in ClassSpells) if (s.unlockLevel == level) list.Add(s);
            return list;
        }

        /// <summary>Büyü şu an kullanılabilir mi? (Bilinen, mana yetiyor, doğru an, etkisi boşa gitmeyecek.)</summary>
        public bool CanCast(SpellDefinition s)
        {
            if (!IsRunActive || !IsSpellKnown(s) || CurrentRun.Mana < s.manaCost) return false;
            if (s.CombatOnly && !InCombat) return false;
            if (s.DigOnly && InCombat) return false;

            switch (s.effect)
            {
                case SpellEffect.Heal: return CurrentRun.PlayerHealth < CurrentRun.PlayerMaxHealth;
                case SpellEffect.BankEarnings: return CurrentRun.Earnings > 0;
                case SpellEffect.Foresight: return !CurrentRun.HasForesight;
                case SpellEffect.CalmBoss: return CurrentRun.Monster.Kind != MonsterKind.Normal;
                default: return true;
            }
        }

        public bool CanCast(string id) => CanCast(CB.FindSpell(id));

        /// <summary>Büyüyü kullan. Mana harcanır, etki uygulanır.</summary>
        public SpellResult Cast(string id)
        {
            SpellDefinition s = CB.FindSpell(id);
            var result = new SpellResult { Spell = s };
            if (!CanCast(s)) return result;

            RunState run = CurrentRun;
            run.Mana -= s.manaCost;
            result.Success = true;

            switch (s.effect)
            {
                case SpellEffect.StrikeMult:
                    result.HasAttack = true;
                    result.Attack = DoAttack(s.value, forceCrit: false);
                    result.TotalDamage = result.Attack.Damage;
                    result.Hits = 1;
                    break;

                case SpellEffect.MultiStrike:
                    result.HasAttack = true;
                    for (int i = 0; i < (int)s.value && InCombat; i++)
                    {
                        result.Attack = DoAttack(1.0, forceCrit: false);
                        result.TotalDamage += result.Attack.Damage;
                        result.Hits++;
                    }
                    break;

                case SpellEffect.Heal:
                    double before = run.PlayerHealth;
                    run.PlayerHealth = Math.Min(run.PlayerMaxHealth, run.PlayerHealth + run.PlayerMaxHealth * s.value);
                    result.Healed = run.PlayerHealth - before;
                    break;

                case SpellEffect.SafeFloors:
                    run.SafeFloors = Math.Max(run.SafeFloors, s.duration);
                    run.SafeFloorsFactor = Math.Max(run.SafeFloorsFactor, s.value);
                    break;

                case SpellEffect.RewardFloors:
                    run.RewardFloors = Math.Max(run.RewardFloors, s.duration);
                    run.RewardFloorsMult = Math.Max(run.RewardFloorsMult, s.value);
                    break;

                case SpellEffect.BankEarnings:
                    double moved = run.Earnings * s.value;
                    run.CourageEarnings -= run.CourageEarnings * s.value;   // Cebe giden kısım orantılı
                    run.Earnings -= moved;
                    run.Banked += moved;
                    Progress.money += moved;
                    result.Banked = moved;
                    break;

                case SpellEffect.Foresight:
                    run.ForesightRoll = _random.NextDouble();                    // ZAR: kehanet (sonraki katın göçük zarı ŞİMDİ atılır)
                    result.ForesightBust = run.ForesightRoll < NextBustChance;
                    break;

                case SpellEffect.FindBoost:
                    run.FindBoostTiers = Math.Max(run.FindBoostTiers, (int)s.value);
                    break;

                case SpellEffect.GuaranteedFinds:
                    run.GuaranteedFinds += s.duration;
                    break;

                case SpellEffect.Ward:
                    run.WardHits = Math.Max(run.WardHits, s.duration);
                    run.WardFactor = Math.Max(run.WardFactor, s.value);
                    break;

                case SpellEffect.CritCharges:
                    run.CritCharges += s.duration;
                    break;

                case SpellEffect.CalmBoss:
                    run.Monster.EnrageStacks = 0;
                    double healBefore = run.PlayerHealth;
                    run.PlayerHealth = Math.Min(run.PlayerMaxHealth, run.PlayerHealth + run.PlayerMaxHealth * s.value);
                    result.Healed = run.PlayerHealth - healBefore;
                    break;

                case SpellEffect.LootHunt:
                    run.LootHuntTiers = Math.Max(run.LootHuntTiers, (int)s.value);
                    break;
            }
            return result;
        }

        /// <summary>Kehanet varsa: sonraki kat göçecek mi? (Yoksa null.) Risk o arada değişse bile güncel riske göre.</summary>
        public bool? ForesightSaysBust =>
            CurrentRun != null && CurrentRun.HasForesight ? CurrentRun.ForesightRoll < NextBustChance : (bool?)null;

        /// <summary>Mana kazan (kat ve öldürme). Üst sınır MaxMana.</summary>
        private void GainMana(double amount)
        {
            if (CurrentRun == null) return;
            CurrentRun.Mana = Math.Min(CurrentRun.MaxMana, CurrentRun.Mana + amount);
        }
    }
}
