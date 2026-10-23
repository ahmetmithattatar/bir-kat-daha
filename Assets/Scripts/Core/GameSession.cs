using System;
using System.Collections.Generic;

namespace RiskLoop.Core
{
    /// <summary>
    /// Oyunun tüm kuralları burada. Unity'yi bilmez: butonları, metinleri, kaydı tanımaz.
    /// UI sadece bu sınıfın metotlarını çağırır ve özelliklerini okur.
    ///
    /// Sınıf birkaç dosyaya bölünmüş ("partial class", aynı sınıfın parçaları):
    ///   GameSession.cs              tur, kazı, bulgular, yükseltmeler
    ///   GameSession.Combat.cs       canavarlar, vuruşlar, zafer ve ganimet
    ///   GameSession.Gear.cs         eşya üretimi, envanter, kuşanma, satış, güçlendirme
    ///   GameSession.Progression.cs  XP, seviye, açılan özellikler, yetenek ağacı
    ///   GameSession.Prestige.cs     Yeni Çağ
    ///   GameSession.Spells.cs       sınıf, mana, büyü kitabı ve büyüler
    ///   GameSession.Insight.cs      güç/risk göstergeleri (hiçbir şeyi değiştirmez)
    ///
    /// Akış:  StartRun() -> Advance() ... (canavar çıkarsa Attack()/MonsterAttack() ... zafer) -> CashOut()
    ///        Göçük ya da ölümde tur biter. Tur aktif değilken yükseltme alınır, eşya kuşanılır, yetenek seçilir.
    ///
    /// Zarların atılma SIRASI (Python simülasyonunda aynı sırayı kullan):
    ///   Advance:       1) göçük (boss katında yok)  2) boss katı değilse: canavar çıkar mı  3) çıktıysa: hangi canavar
    ///                  canavar yoksa: 4) bulgu (boss katında / harita varken atılmaz) 5) nadirlik [+parlayış] 6) tür [+ikiz]
    ///                  [eşyaysa: RollGearItem]
    ///   Attack:        1) kritik (Kartal Gözü yükü varsa zar yine atılır, sonuç kritik)  2) hasar oynaması
    ///   MonsterAttack: 1) hasar oynaması
    ///   Zafer:         katın bulgusu (4-6), sonra ganimet: [normal canavarsa düşme zarı] her eşya için nadirlik [+parlayış] + RollGearItem
    ///   RollGearItem:  yuva, taban, her ek stat için stat seçimi
    ///   Göçük/ölüm:    enkazdan kurtarma (turda eşya varsa)
    ///   Cast:          Kehanet: sonraki katın göçük zarı ŞİMDİ atılır, Advance yeni zar atmaz. Vuruşlu büyüler Attack gibi (her vuruş 2 zar)
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

        /// <summary>Karakterin kuşanılan eşyalar, yetenekler ve Kadim Güç (prestij) ile toplam statları.</summary>
        public CharacterStats Stats => StatsWith(Progress.equipped);

        /// <summary>Verilen eşya listesiyle statlar (eşya karşılaştırması için "bunu kuşansam" hesabı).</summary>
        public CharacterStats StatsWith(IEnumerable<GearItem> equipped)
        {
            CharacterStats s = CharacterStats.Compute(Balance.combat, equipped);
            TalentBonuses t = Talents;
            double might = Math.Pow(1.0 + Balance.prestige.ancientMight.effectPerLevel, Progress.GetPrestigeLevel(PrestigeType.AncientMight));
            s.Attack *= might * (1.0 + t.Attack);
            s.Health *= might * (1.0 + t.Health);
            s.Defense *= 1.0 + t.Defense;
            s.DigPower *= 1.0 + t.DigPower;
            s.CritChance += t.CritChance;
            s.FindChance += t.FindChance;
            ApplyClass(ref s);   // Sınıf farkı (Savaşçı can, Büyücü saldırı/şans, Okçu kritik/bulgu)
            s.ApplyCaps(Balance.combat);
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

        /// <summary>Şu anki bölge (1..). Tur yoksa ilk bölge.</summary>
        public int CurrentZone => RiskMath.ZoneOf(Balance, CurrentStep + 1);

        /// <summary>Mevcut derinlikteki toplam çarpan (ekipman, yetenek ve Kadim Altın dahil).</summary>
        public double CurrentMultiplier =>
            RiskMath.TotalMultiplier(Balance, CurrentStep, Level(UpgradeType.Earnings), RunProfitBonus)
            * (1.0 + Stats.GoldBonus) * (1.0 + Talents.RewardBonus) * AncientGoldMult;

        /// <summary>
        /// Bir sonraki inişte göçük olasılığı (0..1). Ekipmandaki göçük direnci ve yetenekler riski azaltır,
        /// ama katın taban riskinin altına indiremez. Boss katında göçük yok.
        /// </summary>
        public double NextBustChance => BustChanceAt(CurrentStep + 1);

        private double BustChanceAt(int step)
        {
            TalentBonuses t = Talents;
            double factor = RunSafetyFactor * (1.0 - Stats.CaveResist) * (1.0 - Math.Min(t.RiskReduction, 0.6));
            // Dağın Kalbi: bölgenin ilk yarısında risk ayrıca azalır
            if (t.ZoneStartSafety > 0 && RiskMath.FloorInZone(Balance, step) <= Balance.zoneSize / 2) factor *= 1.0 - t.ZoneStartSafety;
            // Büyü: korumalı katlar (Kaya Omuz, Zamanı Bük, Yol Bulucu)
            if (CurrentRun != null && CurrentRun.SafeFloors > 0 && step > CurrentStep && step <= CurrentStep + CurrentRun.SafeFloors)
                factor *= 1.0 - CurrentRun.SafeFloorsFactor;
            return RiskMath.BustChance(Balance, step, Level(UpgradeType.Safety), factor);
        }

        /// <summary>Bir sonraki inişte hayatta kalınırsa eklenecek kazanç.</summary>
        public double NextStepReward => RewardFor(CurrentStep + 1);

        /// <summary>Bir sonraki inişte bulgu çıkma olasılığı. Boss katı ya da harita varsa 1 (garantili).</summary>
        public double NextFindChance =>
            RiskMath.MilestoneAt(Balance, CurrentStep + 1) != null || (CurrentRun != null && CurrentRun.GuaranteedFinds > 0)
                ? 1.0
                : FindChanceAt(CurrentStep + 1);

        /// <summary>Bir sonraki katta canavar çıkma olasılığı (boss katında boss garantili = 1).</summary>
        public double NextMonsterChance
        {
            get
            {
                MilestoneDefinition m = RiskMath.MilestoneAt(Balance, CurrentStep + 1);
                if (m != null && m.HasBoss) return 1.0;
                return RiskMath.MonsterChance(Balance.combat, CurrentStep + 1);
            }
        }

        /// <summary>Göçükte/yenilgide kurtarılacak oran (0..0.9). Sigorta yükseltmesi + yetenek.</summary>
        public double InsuranceFraction =>
            IsUnlocked(Feature.Insurance) || Talents.Insurance > 0
                ? Math.Min(0.9, RiskMath.InsuranceFraction(Balance, Level(UpgradeType.Insurance)) + Talents.Insurance)
                : 0;

        /// <summary>Mevcut adımdan sonraki ilk boss katı.</summary>
        public MilestoneDefinition NextMilestone => RiskMath.NextMilestone(Balance, CurrentStep);

        private double RewardFor(int step) =>
            RiskMath.StepReward(Balance, step, Level(UpgradeType.Earnings), RunProfitBonus)
            * (1.0 + Stats.GoldBonus) * (1.0 + Talents.RewardBonus) * AncientGoldMult * SpellRewardMult(step);

        /// <summary>Büyü: ödüllü katlar (Yağma Hırsı).</summary>
        private double SpellRewardMult(int step) =>
            CurrentRun != null && CurrentRun.RewardFloors > 0 && step > CurrentStep && step <= CurrentStep + CurrentRun.RewardFloors
                ? CurrentRun.RewardFloorsMult : 1.0;

        private double FindChanceAt(int step) =>
            Math.Min(RiskMath.FindChance(Balance, step, Level(UpgradeType.FindChance)) + Stats.FindChance, Balance.maxFindChance);

        private double LuckAt(int step) =>
            RiskMath.Luck(Balance, step, Level(UpgradeType.Luck), RunLuck) + Stats.Luck + Talents.Luck
            + Balance.prestige.ancientLuck.effectPerLevel * Progress.GetPrestigeLevel(PrestigeType.AncientLuck)
            + PityLuck;

        /// <summary>Talihsizlik telafisi: art arda göçük/yenilgilerden gelen ek şans (yetenekle güçlenir).</summary>
        public double PityLuck =>
            (Balance.pityLuckPerStack + Talents.PityBoost) * Math.Min(Progress.pityStacks, Balance.maxPityStacks);

        /// <summary>Toplam şans (derinlik hariç kaynaklar). Gösterge için.</summary>
        public double LuckBonus => LuckAt(1) - Balance.luckPerStep;

        // ---------------- Tur ----------------

        /// <summary>Tura başlanabilecek katlar: 0 (yüzey) + bossu yenilmiş bölge sonları (geçitler, özellik açıksa).</summary>
        public List<int> AvailableStartSteps
        {
            get
            {
                var list = new List<int> { 0 };
                if (!IsUnlocked(Feature.Checkpoints)) return list;
                Progress.conqueredMilestones.Sort();
                foreach (int step in Progress.conqueredMilestones) list.Add(step);
                return list;
            }
        }

        /// <summary>
        /// Yeni tur. startStep > 0 ise o geçitten (bossu yenilmiş bölge sonu) başlanır:
        /// atlanan katların ödülü/bulgusu yoktur, bir sonraki bölgeden kazmaya başlarsın.
        /// </summary>
        public void StartRun(int startStep = 0)
        {
            if (IsRunActive) throw new InvalidOperationException("Zaten aktif bir tur var.");
            if (!AvailableStartSteps.Contains(startStep)) startStep = 0;

            double maxHealth = Stats.Health;
            CurrentRun = new RunState
            {
                Step = startStep,
                StartStep = startStep,
                PlayerMaxHealth = maxHealth,
                PlayerHealth = maxHealth,
            };
            CurrentRun.MaxMana = MaxMana;
            CurrentRun.Mana = MaxMana * CB.startManaFraction;
            Progress.totalRuns++;
        }

        /// <summary>
        /// Bir adım daha derine in. Zar atılır, göçük olursa tur biter.
        /// Hayatta kalınırsa canavar çıkabilir: o zaman katın bulgusu canavar ölünce verilir.
        /// </summary>
        public AdvanceResult Advance()
        {
            if (!IsRunActive) throw new InvalidOperationException("Aktif tur yok.");
            if (InCombat) throw new InvalidOperationException("Savaş sürerken kazılamaz.");

            int nextStep = CurrentRun.Step + 1;
            double chance = NextBustChance;
            // ZAR 1: göçük. Kehanet bu zarı önceden attıysa o kullanılır (yeni zar atılmaz).
            double roll = CurrentRun.HasForesight ? CurrentRun.ForesightRoll : _random.NextDouble();
            CurrentRun.ForesightRoll = -1;

            var result = new AdvanceResult { Step = nextStep, BustChance = chance, Roll = roll };

            if (roll < chance)
            {
                result.SalvagedItem = BustOutcome(out result.LostEarnings, out result.InsurancePayout, chance);
                result.Busted = true;
                result.Unlucky = chance < Balance.unluckyBustChance;
                return result;
            }

            // Hayatta kaldı: derinlik artar, kazanç ve deneyim eklenir.
            double reward = NextStepReward;
            CurrentRun.Step = nextStep;
            CurrentRun.Earnings += reward;
            result.RewardGained = reward;
            result.XpGained = Balance.progression.xpPerFloor * RiskMath.ZoneOf(Balance, nextStep);
            result.LevelsGained = GainXp(result.XpGained);
            GainMana(CB.manaPerFloor);
            CurrentRun.SurvivalOdds *= 1.0 - chance;   // "Bu tur %X'lik yolu geçtin" (oyuncu ne kadar şanslı olduğunu görsün)
            // Kat sayılı büyüler bir kat azalır (ödül yukarıda zaten hesaplandı)
            if (CurrentRun.SafeFloors > 0 && --CurrentRun.SafeFloors == 0) CurrentRun.SafeFloorsFactor = 0;
            if (CurrentRun.RewardFloors > 0 && --CurrentRun.RewardFloors == 0) CurrentRun.RewardFloorsMult = 1.0;

            // Rekor ve boss katı (rekor güncellenmeden ÖNCE "ilk kez mi" bakılır)
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

            // Canavar: boss katında boss garantili, diğer katlarda zar
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
        /// Turda düşen eşyalar kaybolur; ama en nadir olanı kurtarma şansıyla enkazdan çıkabilir (döndürülür).
        /// Talihsizlik telafisi bir artar (sonraki turlar daha şanslı).
        /// </summary>
        private GearItem BustOutcome(out double lost, out double saved, double bustChance = 1.0)
        {
            saved = CurrentRun.Earnings * InsuranceFraction;
            lost = CurrentRun.Earnings - saved;

            Progress.money += saved;
            Progress.totalBusts++;
            Progress.pityStacks = Math.Min(Progress.pityStacks + 1, Balance.maxPityStacks);
            // Düşük riskte gelen göçük "şanssızlık": telafi fazladan birikir
            if (bustChance < Balance.unluckyBustChance)
                Progress.pityStacks = Math.Min(Progress.pityStacks + Balance.unluckyExtraPityStacks, Balance.maxPityStacks);
            CurrentRun.Payout = saved;
            CurrentRun.Monster = null;
            CurrentRun.Status = RunStatus.Busted;
            CheckBestRun();

            // Enkazdan kurtarma: en nadir eşya, yazı-tura (Enkaz Avcısı yeteneği şansı artırır)
            GearItem best = null;
            foreach (GearItem item in CurrentRun.PendingLoot)
            {
                if (best == null || item.tier > best.tier) best = item;
            }
            double chance = Math.Min(0.95, Balance.bustSalvageChance + Talents.Salvage);
            if (best != null && Progress.inventory.Count < InventoryCap && _random.NextDouble() < chance)   // ZAR: kurtarma
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
        /// Katın bulgusu: boss katında garantili; haritadan garantili adım varsa zarsız; değilse zar.
        /// Savaşsız katta Advance, canavarlı katta zafer çağırır.
        /// </summary>
        private bool TryRollFloorFind(int step, double reward, MilestoneDefinition milestone, out FindResult find)
        {
            find = default;
            if (milestone != null)
            {
                find = RollFind(step, reward, milestone.minTier, fromMilestone: true);
            }
            else if (CurrentRun.FindBoostTiers > 0)
            {
                // Arkan Göz: garantili bulgu, nadirlik birkaç basamak yukarı
                find = RollFind(step, reward, minTier: 0, fromMilestone: false);
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

            ApplyFind(ref find);
            return true;
        }

        private FindResult RollFind(int step, double stepReward, int minTier, bool fromMilestone)
        {
            int tier = RollTier(step, minTier);                                                     // ZAR: nadirlik (+parlayış)
            if (CurrentRun.FindBoostTiers > 0)
            {
                // Arkan Göz: nadirlik birkaç basamak yukarı (bir kez)
                tier = Math.Min(tier + CurrentRun.FindBoostTiers, Balance.rarityTiers.Length - 1);
                CurrentRun.FindBoostTiers = 0;
            }

            var types = (FindType[])Enum.GetValues(typeof(FindType));
            double[] weights = RiskMath.FindTypeWeights(Balance, tier);
            // Henüz açılmamış bulgu türleri çıkmaz (oyuncu seviyesiyle gelir)
            for (int i = 0; i < types.Length; i++)
            {
                if (!IsFindTypeUnlocked(types[i])) weights[i] = 0;
            }
            FindType type = types[RiskMath.WeightedPick(weights, _random.NextDouble())];             // ZAR: tür

            double valueBonusMult = 1.0 + Talents.FindValue;
            var find = new FindResult
            {
                Type = type,
                Tier = tier,
                Value = RiskMath.FindValue(Balance, type, tier, stepReward, Level(UpgradeType.FindValue)),
                FromMilestone = fromMilestone
            };
            if (type == FindType.Treasure || type == FindType.SafeCache || type == FindType.CursedTreasure) find.Value *= valueBonusMult;

            if (type == FindType.Gear)
            {
                find.Item = RollGearItem(step, tier);
                find.Value = SellValue(find.Item);
            }
            else if (Talents.TwinFind > 0 && _random.NextDouble() < Talents.TwinFind)              // ZAR: ikiz (Ejderin Gözü)
            {
                find.Value *= 2;
                find.Twin = true;
            }
            return find;
        }

        /// <summary>Bu bulgu türü oyuncunun seviyesinde açık mı?</summary>
        private bool IsFindTypeUnlocked(FindType type)
        {
            switch (type)
            {
                case FindType.Gear: return IsUnlocked(Feature.Equipment);
                case FindType.LuckCharm:
                case FindType.FindStreak:
                case FindType.Multiplier:
                case FindType.CursedTreasure: return IsUnlocked(Feature.SpecialFinds);
                default: return true;
            }
        }

        /// <summary>Nadirlik zarı: derinlik + yükseltme + tılsım + ekipman + yetenek şansı. Parlayış: %x ihtimalle +1.</summary>
        private int RollTier(int step, int minTier)
        {
            int tier = RiskMath.WeightedPick(RiskMath.TierWeights(Balance, LuckAt(step), minTier), _random.NextDouble());
            int maxTier = Balance.rarityTiers.Length - 1;
            if (Talents.TierUpgrade > 0 && tier < maxTier && _random.NextDouble() < Talents.TierUpgrade) tier++;  // ZAR: parlayış
            return tier;
        }

        /// <summary>Bulguyu tura uygular. find.FirstOfTier burada işaretlenir (ilk kez görülen nadirlik).</summary>
        private void ApplyFind(ref FindResult find)
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
                    CurrentRun.SafetyFactor = Math.Max(CurrentRun.SafetyFactor * (1.0 - Math.Min(find.Value, 0.9)), Balance.minRunSafetyFactor);
                    break;
                case FindType.LuckCharm:
                    CurrentRun.RunLuck = Math.Min(CurrentRun.RunLuck + find.Value, Balance.maxRunLuck);
                    break;
                case FindType.FindStreak:
                    CurrentRun.GuaranteedFinds += (int)find.Value;
                    break;
                case FindType.Multiplier:
                    // Risk altındaki kazancı çarpar: büyük kazanç, ama göçükte hepsi gider
                    double gained = CurrentRun.Earnings * Math.Min(find.Value, Balance.maxMultiplierFindBonus);
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

            // İlk kez görülen nadirlik: sunum bunu gerilimli açılışla kutlar
            if (!Progress.seenTiers.Contains(find.Tier))
            {
                Progress.seenTiers.Add(find.Tier);
                find.FirstOfTier = true;
            }

            CurrentRun.Finds.Add(find);
            Progress.totalFinds++;
            if (find.Tier > Progress.bestTier) Progress.bestTier = find.Tier;
        }

        // ---------------- Yükseltmeler ----------------

        public int GetUpgradeLevel(UpgradeType type) => Progress.GetLevel(type);

        /// <summary>Yükseltme oyuncunun seviyesinde açık mı? (Kazanç ve Güvenlik baştan açık.)</summary>
        public bool IsUpgradeUnlocked(UpgradeType type) => IsUnlocked(UpgradeFeature(type));

        /// <summary>Yükseltmenin bağlı olduğu özellik (Kazanç/Güvenlik için Talents = seviye 1'den küçük, hep açık sayılır).</summary>
        public static Feature? UpgradeFeatureOf(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.Insurance: return Feature.Insurance;
                case UpgradeType.FindChance: return Feature.FindChance;
                case UpgradeType.Luck: return Feature.Luck;
                case UpgradeType.FindValue: return Feature.FindValue;
                default: return null;
            }
        }

        private Feature? UpgradeFeature(UpgradeType type) => UpgradeFeatureOf(type);

        public bool IsUpgradeMaxed(UpgradeType type)
        {
            return GetUpgradeLevel(type) >= Balance.GetUpgrade(type).maxLevel;
        }

        public double GetUpgradeCost(UpgradeType type)
        {
            return RiskMath.UpgradeCost(Balance.GetUpgrade(type), GetUpgradeLevel(type));
        }

        /// <summary>Yükseltmeler sadece turlar arasında ve açıldıktan sonra alınabilir.</summary>
        public bool CanBuyUpgrade(UpgradeType type)
        {
            return !IsRunActive
                && IsUpgradeUnlocked(type)
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
