using System;

namespace RiskLoop.Core
{
    /// <summary>
    /// Oyunun tüm kuralları burada. Unity'yi bilmez: butonları, metinleri, kaydı tanımaz.
    /// UI sadece bu sınıfın metotlarını çağırır ve özelliklerini okur.
    ///
    /// Sınıf üç dosyaya bölünmüş ("partial class", aynı sınıfın parçaları):
    ///   GameSession.cs         tur, kazı, bulgular, yükseltmeler
    ///   GameSession.Combat.cs  canavarlar, vuruşlar, zafer ve ganimet
    ///   GameSession.Gear.cs    eşya üretimi, envanter, kuşanma, satış
    ///
    /// Akış:  StartRun() -> Advance() ... (canavar çıkarsa Attack()/MonsterAttack() ... zafer) -> CashOut()
    ///        Göçük ya da ölümde tur biter. Tur aktif değilken yükseltme alınır, eşya kuşanılır/satılır.
    ///
    /// Zarların atılma SIRASI (Python simülasyonunda aynı sırayı kullan):
    ///   Advance:       1) göçük  2) keşif katı değilse: canavar çıkar mı  3) çıktıysa: hangi canavar
    ///                  canavar yoksa: 4) bulgu (keşif katında / harita varken atılmaz) 5) nadirlik 6) tür [7+ eşyaysa: RollGearItem]
    ///   Attack:        1) kritik  2) hasar oynaması
    ///   MonsterAttack: 1) hasar oynaması
    ///   Zafer:         katın bulgusu (yukarıdaki 4-6), sonra ganimet: [normal canavarsa düşme zarı] her eşya için nadirlik + RollGearItem
    ///   RollGearItem:  yuva, taban, her ek stat için stat seçimi
    /// </summary>
    public partial class GameSession
    {
        public BalanceData Balance { get; }
        public PlayerProgress Progress { get; }
        public RunState CurrentRun { get; private set; }

        private readonly IRandomSource _random;

        public GameSession(BalanceData balance, PlayerProgress progress, IRandomSource random)
        {
            Balance = balance ?? throw new ArgumentNullException(nameof(balance));
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public bool IsRunActive => CurrentRun != null && CurrentRun.Status == RunStatus.Active;

        /// <summary>Karakterin kuşanılan eşyalarla ve Kadim Güç (prestij) ile toplam statları.</summary>
        public CharacterStats Stats => StatsWith(Progress.equipped);

        /// <summary>Verilen eşya listesiyle statlar (eşya karşılaştırması için "bunu kuşansam" hesabı).</summary>
        public CharacterStats StatsWith(System.Collections.Generic.IEnumerable<GearItem> equipped)
        {
            CharacterStats s = CharacterStats.Compute(Balance.combat, equipped);
            double might = Math.Pow(1.0 + Balance.prestige.ancientMight.effectPerLevel, Progress.GetPrestigeLevel(PrestigeType.AncientMight));
            s.Attack *= might;
            s.Health *= might;
            return s;
        }

        /// <summary>Kadim Altın (prestij) çarpanı.</summary>
        private double AncientGoldMult =>
            Math.Pow(1.0 + Balance.prestige.ancientGold.effectPerLevel, Progress.GetPrestigeLevel(PrestigeType.AncientGold));

        /// <summary>Bir sonraki katın duvarı için gereken kazma vuruşu (kazı gücüne göre).</summary>
        public int NextSwingsNeeded => RiskMath.SwingsNeeded(Balance.combat, CurrentStep + 1, Stats.DigPower);

        // ---------------- Ekranda gösterilecek değerler ----------------

        private int Level(UpgradeType type) => Progress.GetLevel(type);

        private int CurrentStep => CurrentRun != null ? CurrentRun.Step : 0;
        private double RunProfitBonus => CurrentRun != null ? CurrentRun.ProfitBonus : 0;
        private double RunSafetyFactor => CurrentRun != null ? CurrentRun.SafetyFactor : 1;
        private double RunLuck => CurrentRun != null ? CurrentRun.RunLuck : 0;

        /// <summary>Mevcut derinlikteki toplam çarpan (ekipmandaki altın bonusu ve Kadim Altın dahil).</summary>
        public double CurrentMultiplier =>
            RiskMath.TotalMultiplier(Balance, CurrentStep, Level(UpgradeType.Earnings), RunProfitBonus) * (1.0 + Stats.GoldBonus) * AncientGoldMult;

        /// <summary>
        /// Bir sonraki inişte patlama olasılığı (0..1). Ekipmandaki göçük direnci riski azaltır,
        /// ama derinliğin taban riskinin altına indiremez. Baret bunu değiştirmez, patlamayı yutar.
        /// </summary>
        public double NextBustChance =>
            RiskMath.BustChance(Balance, CurrentStep + 1, Level(UpgradeType.Safety), RunSafetyFactor * (1.0 - Stats.CaveResist));

        /// <summary>Bir sonraki inişte hayatta kalınırsa eklenecek kazanç.</summary>
        public double NextStepReward => RewardFor(CurrentStep + 1);

        /// <summary>Bir sonraki inişte bulgu çıkma olasılığı. Kilometre taşı ya da harita varsa 1 (garantili).</summary>
        public double NextFindChance =>
            RiskMath.MilestoneAt(Balance, CurrentStep + 1) != null || (CurrentRun != null && CurrentRun.GuaranteedFinds > 0)
                ? 1.0
                : FindChanceAt(CurrentStep + 1);

        /// <summary>Bir sonraki katta canavar çıkma olasılığı (keşif katında boss garantili = 1).</summary>
        public double NextMonsterChance
        {
            get
            {
                MilestoneDefinition m = RiskMath.MilestoneAt(Balance, CurrentStep + 1);
                if (m != null && m.HasBoss) return 1.0;
                return RiskMath.MonsterChance(Balance.combat, CurrentStep + 1);
            }
        }

        /// <summary>Patlamada kurtarılacak oran (0..1).</summary>
        public double InsuranceFraction => RiskMath.InsuranceFraction(Balance, Level(UpgradeType.Insurance));

        /// <summary>Mevcut adımdan sonraki ilk kilometre taşı (yoksa null).</summary>
        public MilestoneDefinition NextMilestone => RiskMath.NextMilestone(Balance, CurrentStep);

        private double RewardFor(int step) =>
            RiskMath.StepReward(Balance, step, Level(UpgradeType.Earnings), RunProfitBonus) * (1.0 + Stats.GoldBonus) * AncientGoldMult;

        private double FindChanceAt(int step) =>
            Math.Min(RiskMath.FindChance(Balance, step, Level(UpgradeType.FindChance)) + Stats.FindChance, Balance.maxFindChance);

        private double LuckAt(int step) =>
            RiskMath.Luck(Balance, step, Level(UpgradeType.Luck), RunLuck) + Stats.Luck
            + Balance.prestige.ancientLuck.effectPerLevel * Progress.GetPrestigeLevel(PrestigeType.AncientLuck)
            + PityLuck;

        /// <summary>Talihsizlik telafisi: art arda göçük/yenilgilerden gelen ek şans.</summary>
        public double PityLuck => Balance.pityLuckPerStack * Math.Min(Progress.pityStacks, Balance.maxPityStacks);

        /// <summary>Toplam şans (derinlik hariç kaynaklar): yükseltme + ekipman + kadim + tılsım + telafi. Gösterge için.</summary>
        public double LuckBonus => LuckAt(1) - Balance.luckPerStep;

        // ---------------- Tur ----------------

        /// <summary>Tura başlanabilecek katlar: 0 (yüzey) + bossu yenilmiş keşif katları (geçitler).</summary>
        public System.Collections.Generic.List<int> AvailableStartSteps
        {
            get
            {
                var list = new System.Collections.Generic.List<int> { 0 };
                foreach (MilestoneDefinition m in Balance.milestones)
                {
                    if (Progress.conqueredMilestones.Contains(m.step)) list.Add(m.step);
                }
                return list;
            }
        }

        /// <summary>
        /// Yeni tur. startStep > 0 ise o geçitten (bossu yenilmiş keşif katı) başlanır:
        /// atlanan katların ödülü/bulgusu yoktur, risk ve canavarlar o derinlikten başlar.
        /// </summary>
        public void StartRun(int startStep = 0)
        {
            if (IsRunActive) throw new InvalidOperationException("Zaten aktif bir tur var.");
            if (startStep < 0 || (startStep > 0 && !Progress.conqueredMilestones.Contains(startStep))) startStep = 0;

            double maxHealth = Stats.Health;
            CurrentRun = new RunState
            {
                Step = startStep,
                StartStep = startStep,
                Shields = Math.Min(Balance.maxShieldsPerRun,
                          RiskMath.StartShields(Balance, Level(UpgradeType.StartShield))
                          + (int)Math.Round(Balance.prestige.ancientShield.effectPerLevel * Progress.GetPrestigeLevel(PrestigeType.AncientShield))),
                PlayerMaxHealth = maxHealth,
                PlayerHealth = maxHealth,
            };
            Progress.totalRuns++;
        }

        /// <summary>
        /// Bir adım daha derine in. Zar atılır, patlarsa (ve baret yoksa) tur biter.
        /// Hayatta kalınırsa canavar çıkabilir: o zaman katın bulgusu canavar ölünce verilir.
        /// </summary>
        public AdvanceResult Advance()
        {
            if (!IsRunActive) throw new InvalidOperationException("Aktif tur yok.");
            if (InCombat) throw new InvalidOperationException("Savaş sürerken kazılamaz.");

            int nextStep = CurrentRun.Step + 1;
            double chance = NextBustChance;
            double roll = _random.NextDouble();               // ZAR 1: göçük

            var result = new AdvanceResult { Step = nextStep, BustChance = chance, Roll = roll };

            if (roll < chance)
            {
                if (CurrentRun.Shields > 0)
                {
                    // Baret patlamayı yuttu: tur devam eder, adım normal sayılır.
                    // Ama enkaz risk altındaki kazancın bir kısmını gömer (baret bedelsiz değil).
                    CurrentRun.Shields--;
                    CurrentRun.ShieldsUsed++;
                    Progress.shieldsUsed++;
                    result.ShieldUsed = true;
                    result.ShieldLoss = CurrentRun.Earnings * Balance.shieldEarningsLoss;
                    CurrentRun.Earnings -= result.ShieldLoss;
                }
                else
                {
                    result.SalvagedItem = BustOutcome(out result.LostEarnings, out result.InsurancePayout);
                    result.Busted = true;
                    return result;
                }
            }

            // Hayatta kaldı: derinlik artar, kazanç eklenir.
            double reward = NextStepReward;
            CurrentRun.Step = nextStep;
            CurrentRun.Earnings += reward;
            result.RewardGained = reward;

            // Rekor ve kilometre taşı (rekor güncellenmeden ÖNCE "ilk kez mi" bakılır)
            bool isNewRecord = nextStep > Progress.bestStep;
            MilestoneDefinition milestone = RiskMath.MilestoneAt(Balance, nextStep);
            if (milestone != null)
            {
                result.IsMilestone = true;
                if (isNewRecord)
                {
                    result.FirstMilestoneReach = true;
                    result.MilestoneBonus = reward * milestone.firstReachRewardSteps;
                    Progress.money += result.MilestoneBonus;
                    CurrentRun.Banked += result.MilestoneBonus;
                    CurrentRun.MilestoneBonusTotal += result.MilestoneBonus;
                }
            }
            if (isNewRecord)
            {
                Progress.bestStep = nextStep;
                result.NewRecord = true;
            }
            if (nextStep > Progress.allTimeBestStep) Progress.allTimeBestStep = nextStep;

            // Canavar: keşif katında boss garantili, diğer katlarda zar
            MonsterInstance monster = RollEncounter(nextStep, milestone);   // ZAR 2-3
            if (monster != null)
            {
                CurrentRun.Monster = monster;
                CurrentRun.PendingMilestone = milestone;
                CurrentRun.PendingStepReward = reward;
                result.Encounter = monster;
                return result;  // Bulgu zafere ertelendi
            }

            // Savaşsız kat: biraz can dolar, bulgu zarı
            result.HealthRegen = RegenerateHealth();
            if (TryRollFloorFind(nextStep, reward, milestone, out FindResult find))
            {
                result.HasFind = true;
                result.Find = find;
            }
            return result;
        }

        /// <summary>
        /// Yüzeye dön: risk altındaki kazancı ve turda düşen eşyaları cebe at.
        /// Savaş sürerken çağrılırsa kaçış sayılır (canavarın ödülü gider). Cebe giren parayı döndürür.
        /// </summary>
        public double CashOut()
        {
            if (!IsRunActive) throw new InvalidOperationException("Aktif tur yok.");

            CurrentRun.Monster = null;  // Kaçış
            double payout = CurrentRun.Earnings;
            Progress.money += payout;
            CurrentRun.Payout = payout;
            CurrentRun.Status = RunStatus.CashedOut;
            MoveLootToInventory();
            CheckBestRun();
            Progress.pityStacks = 0;  // Başarılı dönüş: talihsizlik telafisi sıfırlanır
            return payout;
        }

        /// <summary>Tur bitince: bu tur en değerli tur rekorunu kırdı mı?</summary>
        private void CheckBestRun()
        {
            double value = CurrentRun.TotalValue;
            if (value > Progress.bestRunValue)
            {
                Progress.bestRunValue = value;
                CurrentRun.IsNewBestRun = true;
            }
        }

        /// <summary>
        /// Göçük ya da ölüm: tur kazancı gider, sigorta bir kısmını kurtarır. Cebe girmiş (Banked) para güvende.
        /// Turda düşen eşyalar kaybolur; ama en nadir olanı bustSalvageChance ile enkazdan kurtulabilir (döndürülür).
        /// Talihsizlik telafisi bir artar (sonraki turlar daha şanslı).
        /// </summary>
        private GearItem BustOutcome(out double lost, out double saved)
        {
            saved = CurrentRun.Earnings * InsuranceFraction;
            lost = CurrentRun.Earnings - saved;

            Progress.money += saved;
            Progress.totalBusts++;
            Progress.pityStacks = Math.Min(Progress.pityStacks + 1, Balance.maxPityStacks);
            CurrentRun.Payout = saved;
            CurrentRun.Monster = null;
            CurrentRun.Status = RunStatus.Busted;
            CheckBestRun();

            // Enkazdan kurtarma: en nadir eşya, yazı-tura
            GearItem best = null;
            foreach (GearItem item in CurrentRun.PendingLoot)
            {
                if (best == null || item.tier > best.tier) best = item;
            }
            if (best != null && Progress.inventory.Count < InventoryCap && _random.NextDouble() < Balance.bustSalvageChance)   // ZAR: kurtarma
            {
                Progress.inventory.Add(best);
                return best;
            }
            return null;
        }

        private double RegenerateHealth()
        {
            double before = CurrentRun.PlayerHealth;
            CurrentRun.PlayerHealth = Math.Min(CurrentRun.PlayerMaxHealth,
                CurrentRun.PlayerHealth + CurrentRun.PlayerMaxHealth * Balance.combat.hpRegenPerFloor);
            return CurrentRun.PlayerHealth - before;
        }

        // ---------------- Bulgu yardımcıları ----------------

        /// <summary>
        /// Katın bulgusu: kilometre taşında garantili; haritadan garantili adım varsa zarsız; değilse zar.
        /// Savaşsız katta Advance, canavarlı katta zafer çağırır.
        /// </summary>
        private bool TryRollFloorFind(int step, double reward, MilestoneDefinition milestone, out FindResult find)
        {
            find = default;
            if (milestone != null)
            {
                find = RollFind(step, reward, milestone.minTier, fromMilestone: true);
            }
            else if (CurrentRun.GuaranteedFinds > 0)
            {
                CurrentRun.GuaranteedFinds--;
                find = RollFind(step, reward, minTier: 0, fromMilestone: false);
            }
            else if (_random.NextDouble() < FindChanceAt(step))   // ZAR: bulgu
            {
                find = RollFind(step, reward, minTier: 0, fromMilestone: false);
            }
            else
            {
                return false;
            }

            ApplyFind(find);
            return true;
        }

        private FindResult RollFind(int step, double stepReward, int minTier, bool fromMilestone)
        {
            int tier = RollTier(step, minTier);                                                                     // ZAR: nadirlik

            var types = (FindType[])Enum.GetValues(typeof(FindType));
            int typeIndex = RiskMath.WeightedPick(RiskMath.FindTypeWeights(Balance, tier), _random.NextDouble());   // ZAR: tür
            FindType type = types[typeIndex];

            var find = new FindResult
            {
                Type = type,
                Tier = tier,
                Value = RiskMath.FindValue(Balance, type, tier, stepReward, Level(UpgradeType.FindValue)),
                FromMilestone = fromMilestone
            };

            if (type == FindType.Gear)
            {
                find.Item = RollGearItem(step, tier);
                find.Value = RiskMath.GearSellValue(Balance.combat, find.Item);
            }
            return find;
        }

        /// <summary>Nadirlik zarı: derinlik + yükseltme + tılsım + ekipman şansı.</summary>
        private int RollTier(int step, int minTier)
        {
            return RiskMath.WeightedPick(RiskMath.TierWeights(Balance, LuckAt(step), minTier), _random.NextDouble());
        }

        private void ApplyFind(FindResult find)
        {
            switch (find.Type)
            {
                case FindType.Treasure:
                    CurrentRun.Earnings += find.Value;
                    break;
                case FindType.SafeCache:
                    Progress.money += find.Value;
                    CurrentRun.Banked += find.Value;
                    break;
                case FindType.ProfitBoost:
                    CurrentRun.ProfitBonus = Math.Min(CurrentRun.ProfitBonus + find.Value, Balance.maxRunProfitBonus);
                    break;
                case FindType.Safety:
                    CurrentRun.SafetyFactor = Math.Max(CurrentRun.SafetyFactor * (1.0 - find.Value), Balance.minRunSafetyFactor);
                    break;
                case FindType.Shield:
                    CurrentRun.Shields = Math.Min(CurrentRun.Shields + (int)find.Value, Balance.maxShieldsPerRun);
                    CurrentRun.ShieldsFound += (int)find.Value;
                    break;
                case FindType.LuckCharm:
                    CurrentRun.RunLuck = Math.Min(CurrentRun.RunLuck + find.Value, Balance.maxRunLuck);
                    break;
                case FindType.FindStreak:
                    CurrentRun.GuaranteedFinds += (int)find.Value;
                    break;
                case FindType.Multiplier:
                    // Risk altındaki kazancı çarpar: büyük kazanç, ama göçükte hepsi gider
                    double gained = CurrentRun.Earnings * find.Value;
                    CurrentRun.Earnings += gained;
                    CurrentRun.MultiplierGained += gained;
                    break;
                case FindType.CursedTreasure:
                    CurrentRun.Earnings += find.Value;
                    CurrentRun.SafetyFactor = Math.Min(CurrentRun.SafetyFactor * (1.0 + Balance.cursedRiskIncrease), Balance.maxRunSafetyFactor);
                    break;
                case FindType.Gear:
                    CurrentRun.PendingLoot.Add(find.Item);  // Risk altında: yüzeye dönünce envantere
                    break;
            }

            // Tur özeti için türe göre toplamlar
            if (find.Type == FindType.Treasure || find.Type == FindType.CursedTreasure) CurrentRun.TreasureTotal += find.Value;
            if (find.Type == FindType.SafeCache) CurrentRun.CacheTotal += find.Value;

            CurrentRun.Finds.Add(find);
            Progress.totalFinds++;
            if (find.Tier > Progress.bestTier) Progress.bestTier = find.Tier;
        }

        // ---------------- Yükseltmeler ----------------

        public int GetUpgradeLevel(UpgradeType type) => Progress.GetLevel(type);

        public bool IsUpgradeMaxed(UpgradeType type)
        {
            return GetUpgradeLevel(type) >= Balance.GetUpgrade(type).maxLevel;
        }

        public double GetUpgradeCost(UpgradeType type)
        {
            return RiskMath.UpgradeCost(Balance.GetUpgrade(type), GetUpgradeLevel(type));
        }

        /// <summary>Yükseltmeler sadece turlar arasında alınabilir.</summary>
        public bool CanBuyUpgrade(UpgradeType type)
        {
            return !IsRunActive
                && !IsUpgradeMaxed(type)
                && Progress.money >= GetUpgradeCost(type);
        }

        public bool TryBuyUpgrade(UpgradeType type)
        {
            if (!CanBuyUpgrade(type)) return false;

            Progress.money -= GetUpgradeCost(type);
            Progress.SetLevel(type, GetUpgradeLevel(type) + 1);
            return true;
        }
    }
}
