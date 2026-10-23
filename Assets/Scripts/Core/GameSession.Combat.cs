using System;
using System.Collections.Generic;

namespace RiskLoop.Core
{
    /// <summary>
    /// GameSession'ın savaş kısmı. Zaman bilmez: "canavar ne zaman vurur" sorusunu Game katmanı sayar
    /// (MonsterAttackInterval) ve zamanı gelince MonsterAttack() çağırır. Böylece kurallar test edilebilir kalır.
    /// </summary>
    public partial class GameSession
    {
        /// <summary>Şu an bir canavarla savaşılıyor mu?</summary>
        public bool InCombat => IsRunActive && CurrentRun.Monster != null;

        /// <summary>Mevcut canavarın kaç saniyede bir vurduğu.</summary>
        public double MonsterAttackInterval =>
            InCombat ? RiskMath.MonsterAttackInterval(Balance.combat, CurrentRun.Monster.Definition) : 0;

        /// <summary>Katta canavar çıkar mı? Keşif katında boss garantili (zar yok).</summary>
        private MonsterInstance RollEncounter(int step, MilestoneDefinition milestone)
        {
            CombatBalance c = Balance.combat;

            if (milestone != null && milestone.HasBoss)
            {
                // Havuzdan rastgele bir boss: her keşif farklı bir karşılaşma olabilir
                int index = Math.Min((int)(_random.NextDouble() * milestone.bossPool.Length), milestone.bossPool.Length - 1);   // ZAR: hangi boss
                MonsterDefinition boss = c.FindMonster(milestone.bossPool[index]);
                return boss != null ? CreateMonster(boss, step, milestone) : null;
            }

            if (_random.NextDouble() >= RiskMath.MonsterChance(c, step)) return null;   // ZAR: canavar çıkar mı

            // Bu derinlikte çıkabilen normal canavarlardan ağırlıklı seçim
            var candidates = new List<MonsterDefinition>();
            foreach (MonsterDefinition m in c.monsters)
            {
                if (m.kind == MonsterKind.Normal && m.weight > 0 && step >= m.minStep && step <= m.maxStep) candidates.Add(m);
            }
            if (candidates.Count == 0) return null;

            var weights = new double[candidates.Count];
            for (int i = 0; i < weights.Length; i++) weights[i] = candidates[i].weight;
            MonsterDefinition picked = candidates[RiskMath.WeightedPick(weights, _random.NextDouble())];   // ZAR: hangi canavar
            return CreateMonster(picked, step, null);
        }

        private MonsterInstance CreateMonster(MonsterDefinition def, int step, MilestoneDefinition milestone)
        {
            double hp = RiskMath.MonsterHealth(Balance.combat, def, step);
            var monster = new MonsterInstance
            {
                Definition = def,
                Step = step,
                MaxHealth = hp,
                Health = hp,
                Attack = RiskMath.MonsterAttack(Balance.combat, def, step),
                Milestone = milestone
            };
            monster.Level = RiskMath.MonsterLevel(Balance.combat, step);
            monster.Difficulty = DifficultyBand(MonsterThreatRatio(monster, Stats));   // Ad rengi ve XP kuralı
            return monster;
        }

        /// <summary>Oyuncu canavara bir kez vurur. Canavar ölürse zafer (ödül + bulgu + ganimet).</summary>
        public AttackResult Attack()
        {
            if (!InCombat) throw new InvalidOperationException("Savaş yok.");
            return DoAttack(1.0, forceCrit: false);
        }

        /// <summary>Bir vuruş. damageMult: büyü çarpanı (Yarma Darbesi x3 gibi). Kartal Gözü yükü varsa kesin kritik.</summary>
        private AttackResult DoAttack(double damageMult, bool forceCrit)
        {

            CharacterStats stats = Stats;
            CombatBalance c = Balance.combat;
            MonsterInstance monster = CurrentRun.Monster;

            bool crit = _random.NextDouble() < stats.CritChance;                                    // ZAR: kritik
            if (CurrentRun.CritCharges > 0)
            {
                crit = true;   // Kartal Gözü (zar yine atılır: zar sırası değişmesin)
                CurrentRun.CritCharges--;
            }
            if (forceCrit) crit = true;
            double variance = 1.0 + (_random.NextDouble() * 2.0 - 1.0) * c.damageVariance;         // ZAR: hasar oynaması
            double damage = stats.Attack * variance * (crit ? stats.CritDamage : 1.0) * damageMult;
            if (monster.Kind != MonsterKind.Normal) damage *= 1.0 + Talents.BossDamage;   // Kral Katili

            monster.Health -= damage;

            // Kan Emici: vurulan hasarın bir kısmı can olarak döner
            double lifesteal = Talents.Lifesteal;
            if (lifesteal > 0)
                CurrentRun.PlayerHealth = Math.Min(CurrentRun.PlayerMaxHealth, CurrentRun.PlayerHealth + damage * lifesteal);
            var result = new AttackResult { Damage = damage, Crit = crit };

            if (monster.IsDead)
            {
                result.Killed = true;
                result.Victory = Victory(monster);
            }
            return result;
        }

        /// <summary>Canavar oyuncuya bir kez vurur. Can biterse tur biter (göçük gibi).</summary>
        public MonsterHitResult MonsterAttack()
        {
            if (!InCombat) throw new InvalidOperationException("Savaş yok.");

            CombatBalance c = Balance.combat;
            MonsterInstance monster = CurrentRun.Monster;

            double variance = 1.0 + (_random.NextDouble() * 2.0 - 1.0) * c.damageVariance;         // ZAR: hasar oynaması
            double enrage = 1.0 + c.enrageAttackPerStack * monster.EnrageStacks;
            double damage = RiskMath.DamageAfterDefense(c, monster.Attack * enrage * variance, Stats.Defense);
            bool warded = false;
            if (CurrentRun.WardHits > 0)
            {
                damage *= 1.0 - CurrentRun.WardFactor;   // Titan Kalkanı / Buz Kalkanı
                if (--CurrentRun.WardHits == 0) CurrentRun.WardFactor = 0;
                warded = true;
            }

            CurrentRun.PlayerHealth -= damage;
            var result = new MonsterHitResult { Damage = damage, Warded = warded };

            if (CurrentRun.PlayerHealth <= 0)
            {
                CurrentRun.PlayerHealth = 0;
                CurrentRun.DiedToMonster = true;
                result.SalvagedItem = BustOutcome(out result.LostEarnings);
                result.PlayerDied = true;
            }
            return result;
        }

        /// <summary>Bossun öfke aralığı (sn), Soğukkanlılık yeteneğiyle uzar. Normal canavarlar öfkelenmez: 0.</summary>
        public double EnrageInterval
        {
            get
            {
                if (!InCombat) return 0;
                double slow = 1.0 + Talents.EnrageSlow;
                switch (CurrentRun.Monster.Kind)
                {
                    case MonsterKind.Boss: return Balance.combat.bossEnrageInterval * slow;
                    case MonsterKind.MiniBoss: return Balance.combat.miniBossEnrageInterval * slow;
                    default: return 0;
                }
            }
        }

        /// <summary>Boss bir kademe öfkelenir (Game katmanı zamanla çağırır). Yeni öfke seviyesini döndürür.</summary>
        public int EnrageMonster()
        {
            if (!InCombat || CurrentRun.Monster.Kind == MonsterKind.Normal) return 0;
            return ++CurrentRun.Monster.EnrageStacks;
        }

        /// <summary>Canavar öldü: öldürme ödülü, katın ertelenmiş bulgusu, ganimet.</summary>
        private VictoryResult Victory(MonsterInstance monster)
        {
            CombatBalance c = Balance.combat;
            var victory = new VictoryResult { Monster = monster };

            CurrentRun.Monster = null;
            CurrentRun.Kills++;
            GainMana(CB.manaPerKill);
            Progress.monstersKilled++;
            if (monster.Kind != MonsterKind.Normal) Progress.bossesKilled++;

            // Deneyim: boss çok, normal canavar orta (bölgeyle artar)
            int zone = RiskMath.ZoneOf(Balance, monster.Step);
            victory.XpGained = (monster.Kind == MonsterKind.Normal ? Balance.progression.xpPerKill : Balance.progression.xpPerBoss) * zone;
            if (!DifficultyGivesXp(monster.Difficulty)) victory.XpGained = 0;   // Çok kolay ya da çok zor: XP yok
            victory.LevelsGained = GainXp(victory.XpGained);

            // Keşif katının bossu yenildi: geçit açılır (sonraki turlara buradan başlanabilir)
            if (monster.Milestone != null && !Progress.conqueredMilestones.Contains(monster.Milestone.step))
            {
                Progress.conqueredMilestones.Add(monster.Milestone.step);
                victory.UnlockedCheckpoint = true;
            }

            // Öldürme ödülü: risk altında (yüzeye dönmeden cebe girmez)
            double reward = CurrentRun.PendingStepReward;
            victory.Bounty = reward * c.bountySteps * monster.Definition.hpMult;
            CurrentRun.Earnings += victory.Bounty;

            // Katın bulgusu (canavar yüzünden ertelenmişti)
            if (TryRollFloorFind(monster.Step, reward, CurrentRun.PendingMilestone, out FindResult find))
            {
                victory.HasFind = true;
                victory.Find = find;
            }

            // Ganimet
            int drops = 0;
            int minTier = 0;
            switch (monster.Kind)
            {
                case MonsterKind.Normal:
                    if (_random.NextDouble() < c.normalDropChance + Talents.GearDrop) drops = 1;   // ZAR: düşme (Ganimetçi artırır)
                    break;
                case MonsterKind.MiniBoss:
                    drops = c.miniBossDrops;
                    minTier = Math.Min(monster.Milestone != null ? Math.Max(0, monster.Milestone.minTier - 1) : 1, c.maxBossDropMinTier - 1);
                    break;
                case MonsterKind.Boss:
                    drops = c.bossDrops;
                    minTier = Math.Min(monster.Milestone != null ? monster.Milestone.minTier : 3, c.maxBossDropMinTier);
                    break;
            }

            // Ganimet Avı büyüsü: en az 1 eşya, nadirlik yukarı
            if (CurrentRun.LootHuntTiers >= 0)
            {
                drops = Math.Max(drops, 1);
                minTier = Math.Min(Math.Max(minTier, 1) + CurrentRun.LootHuntTiers, Balance.rarityTiers.Length - 1);
                CurrentRun.LootHuntTiers = -1;
            }

            if (!IsUnlocked(Feature.Equipment)) drops = 0;   // Ekipman henüz açılmadıysa eşya düşmez

            for (int i = 0; i < drops; i++)
            {
                int tier = RollGearTier(monster.Step, minTier);   // ZAR: eşya nadirliği (sınırlı şans)
                GearItem item = RollGearItem(monster.Step, tier);
                var drop = new FindResult
                {
                    Type = FindType.Gear,
                    Tier = tier,
                    Item = item,
                    Value = SellValue(item),
                    FromMonster = true
                };
                ApplyFind(ref drop);
                victory.Drops.Add(drop);
            }

            CurrentRun.PendingMilestone = null;
            return victory;
        }
    }
}
