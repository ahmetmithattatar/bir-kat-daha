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
            return new MonsterInstance
            {
                Definition = def,
                Step = step,
                MaxHealth = hp,
                Health = hp,
                Attack = RiskMath.MonsterAttack(Balance.combat, def, step),
                Milestone = milestone
            };
        }

        /// <summary>Oyuncu canavara bir kez vurur. Canavar ölürse zafer (ödül + bulgu + ganimet).</summary>
        public AttackResult Attack()
        {
            if (!InCombat) throw new InvalidOperationException("Savaş yok.");

            CharacterStats stats = Stats;
            CombatBalance c = Balance.combat;
            MonsterInstance monster = CurrentRun.Monster;

            bool crit = _random.NextDouble() < stats.CritChance;                                    // ZAR: kritik
            double variance = 1.0 + (_random.NextDouble() * 2.0 - 1.0) * c.damageVariance;         // ZAR: hasar oynaması
            double damage = stats.Attack * variance * (crit ? stats.CritDamage : 1.0);

            monster.Health -= damage;
            var result = new AttackResult { Damage = damage, Crit = crit };

            if (monster.IsDead)
            {
                result.Killed = true;
                result.Victory = Victory(monster);
            }
            return result;
        }

        /// <summary>Canavar oyuncuya bir kez vurur. Can biterse baret kurtarır, yoksa tur biter (göçük gibi).</summary>
        public MonsterHitResult MonsterAttack()
        {
            if (!InCombat) throw new InvalidOperationException("Savaş yok.");

            CombatBalance c = Balance.combat;
            MonsterInstance monster = CurrentRun.Monster;

            double variance = 1.0 + (_random.NextDouble() * 2.0 - 1.0) * c.damageVariance;         // ZAR: hasar oynaması
            double damage = RiskMath.DamageAfterDefense(c, monster.Attack * variance, Stats.Defense);

            CurrentRun.PlayerHealth -= damage;
            var result = new MonsterHitResult { Damage = damage };

            if (CurrentRun.PlayerHealth <= 0)
            {
                if (CurrentRun.Shields > 0)
                {
                    // Baret ölümü engeller: az canla ayağa kalkarsın
                    CurrentRun.Shields--;
                    CurrentRun.ShieldsUsed++;
                    Progress.shieldsUsed++;
                    CurrentRun.PlayerHealth = CurrentRun.PlayerMaxHealth * c.shieldReviveHealth;
                    result.ShieldUsed = true;
                }
                else
                {
                    CurrentRun.PlayerHealth = 0;
                    CurrentRun.DiedToMonster = true;
                    BustOutcome(out result.LostEarnings, out result.InsurancePayout);
                    result.PlayerDied = true;
                }
            }
            return result;
        }

        /// <summary>Canavar öldü: öldürme ödülü, katın ertelenmiş bulgusu, ganimet.</summary>
        private VictoryResult Victory(MonsterInstance monster)
        {
            CombatBalance c = Balance.combat;
            var victory = new VictoryResult { Monster = monster };

            CurrentRun.Monster = null;
            CurrentRun.Kills++;
            Progress.monstersKilled++;
            if (monster.Kind != MonsterKind.Normal) Progress.bossesKilled++;

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
                    if (_random.NextDouble() < c.normalDropChance) drops = 1;   // ZAR: düşme
                    break;
                case MonsterKind.MiniBoss:
                    drops = c.miniBossDrops;
                    minTier = monster.Milestone != null ? Math.Max(0, monster.Milestone.minTier - 1) : 1;
                    break;
                case MonsterKind.Boss:
                    drops = c.bossDrops;
                    minTier = monster.Milestone != null ? monster.Milestone.minTier : 3;
                    break;
            }

            for (int i = 0; i < drops; i++)
            {
                int tier = RollTier(monster.Step, minTier);   // ZAR: nadirlik
                GearItem item = RollGearItem(monster.Step, tier);
                var drop = new FindResult
                {
                    Type = FindType.Gear,
                    Tier = tier,
                    Item = item,
                    Value = RiskMath.GearSellValue(c, item),
                    FromMonster = true
                };
                ApplyFind(drop);
                victory.Drops.Add(drop);
            }

            CurrentRun.PendingMilestone = null;
            return victory;
        }
    }
}
