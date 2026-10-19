using System;
using RiskLoop.Core;

namespace RiskLoop.Game
{
    /// <summary>
    /// Ekranda görünen TÜM metinler burada. Tema (kazı) sadece bu dosyada (ve RarityStyle'da) yaşıyor:
    /// temayı değiştirmek ya da çeviri eklemek için önce buraya bak.
    /// Aynı olayda hep aynı cümle çıkmasın diye bazı mesajlar havuzdan rastgele seçilir (Pick).
    /// </summary>
    public static class UIStrings
    {
        // ---------------- Buton etiketleri ----------------
        public const string AdvanceButton = "KAZ!";
        public const string AttackButton = "SALDIR!";
        public const string CashOutButton = "Yüzeye dön";
        public const string FleeButton = "Kaç ve yüzeye dön";
        public const string StartRunButton = "Kazıya başla";
        public const string ResetSaveButton = "Kaydı sıfırla";
        public const string ResetSaveConfirm = "Emin misin?\nTekrar bas";
        public const string MaxedButton = "MAKS";
        public const string UpgradesTitle = "Yükseltmeler";
        public const string KeyHintRun = "↓ / S / Boşluk: kaz (basılı tut)   ↑ / W: yüzeye dön   Sahneye tıklamak da kazar";
        public const string KeyHintUpgrade = "Klavye:  ↓ / S / Boşluk  kazıya başla";
        public const string PopupContinue = "Devam etmek için tıkla ya da bir tuşa bas";

        // ---------------- Durum mesajları ----------------
        public const string Welcome = "Derine indikçe kazanç ve nadir bulgular artar, ama göçük riski de artar.\nNe zaman yüzeye döneceğine sen karar ver.";
        public const string RunStarted = "Kazı başladı. İlk kata inmeye hazırsın.";
        public const string SaveReset = "Kayıt silindi. Sıfırdan başlıyorsun.";

        private const string Gold = "#FFE03A";
        private const string AtRiskColor = "#FF9A2E";
        private const string SafeColor = "#3CCB5A";

        // ---------------- Üst bar ----------------
        public static string Money(double money) => $"Para: {Num(money)}";
        public static string Record(int bestStep) => $"Rekor: {bestStep}. kat";

        // ---------------- Tur ekranı ----------------
        public static string Depth(int step) => $"Derinlik: {step}. kat";
        public static string Multiplier(double multiplier) => $"Çarpan: x{multiplier:0.00}";
        public static string NextRisk(double chance) => $"Sonraki inişte göçük: %{Pct(chance)}";

        public static string RunValue(double value) => $"Turun değeri: {Num(value)}";

        /// <summary>Turun değerinin ne kadarı riskte, ne kadarı cepte. Riske devam kararını bu satır verdirir.</summary>
        public static string RunValueBreakdown(RunState run)
        {
            if (run == null) return "";
            return $"Risk altında: <color={AtRiskColor}>{Num(run.Earnings)}</color>     " +
                   $"Cepte: <color={SafeColor}>{Num(run.Banked)}</color>";
        }

        public static string NextReward(double reward, double findChance, double monsterChance)
        {
            string find = findChance >= 1.0 ? $"<color={Gold}>Bulgu GARANTİ</color>" : $"Bulgu şansı: %{Pct(findChance)}";
            string monster = monsterChance >= 1.0 ? $"<color={AtRiskColor}>BOSS bekliyor!</color>" : $"Canavar şansı: %{Pct(monsterChance)}";
            return $"Sonraki kat: +{Num(reward)}    {find}\n{monster}";
        }

        public static string Insurance(double fraction)
        {
            return fraction > 0
                ? $"Sigorta: göçükte risk altındakinin %{Pct(fraction)} kadarı kurtarılır"
                : "Sigorta yok";
        }

        /// <summary>Bu turun bulgu etkileri: baret, kazanç bonusu, risk çarpanı.</summary>
        public static string RunBuffs(RunState run)
        {
            if (run == null) return "";

            string text = run.Shields > 0 ? $"<color={Gold}>Baret: {run.Shields}</color>" : "Baret: 0";
            if (run.ProfitBonus > 0) text += $"   Kazanç +%{Pct(run.ProfitBonus)}";
            if (run.SafetyFactor < 1) text += $"   Risk x{run.SafetyFactor:0.00}";
            if (run.SafetyFactor > 1) text += $"   <color={AtRiskColor}>Lanet: risk x{run.SafetyFactor:0.00}</color>";
            if (run.RunLuck > 0) text += $"   Şans +%{Pct(run.RunLuck)}";
            if (run.GuaranteedFinds > 0) text += $"   <color={Gold}>Harita: {run.GuaranteedFinds} kat</color>";
            return text;
        }

        public static string CollectionTitle(int count) => count > 0 ? $"Bu turun bulguları ({count})" : "Bu turda henüz bulgu yok";

        // ---------------- Keşif katları (kilometre taşları) ----------------

        /// <summary>Her keşif katının kendine özgü adı. Listede olmayan kat için genel bir ad üretir.</summary>
        public static string MilestoneName(int step)
        {
            switch (step)
            {
                case 3: return "Paslı Sandık";
                case 5: return "Kayıp Madenci Kampı";
                case 10: return "Kristal Mağarası";
                case 15: return "Yeraltı Gölü";
                case 20: return "Cüce Demirhanesi";
                case 25: return "Dev Fosil Mezarlığı";
                case 30: return "Ejderin İni";
                case 35: return "Lav Kalbi";
                case 40: return "Dünyanın Çekirdeği";
                default: return $"{step}. Kat Hazinesi";
            }
        }

        public static string NextMilestone(MilestoneDefinition milestone, int currentStep, bool discovered)
        {
            if (milestone == null) return $"<color={Gold}>Bütün keşifleri tamamladın. Efsanesin!</color>";

            int remaining = milestone.step - currentStep;
            string minTier = RarityStyle.Colored(RarityStyle.Name(milestone.minTier), milestone.minTier);
            string when = remaining == 1 ? $"<color={Gold}>SONRAKİ KAT!</color>" : $"{remaining} kat kaldı";
            string name = discovered ? MilestoneName(milestone.step) : "???";  // Keşfedilmemişin adı gizli: merak
            return $"Sonraki keşif: <b>{name}</b> ({milestone.step}. kat, {when})\nGarantili bulgu, en az {minTier}";
        }

        // ---------------- Kazı anı mesajları (havuzdan rastgele) ----------------

        private static readonly string[] DigLines =
        {
            "Kazma taşa vuruyor...", "Toprak gevşiyor...", "Tozlar yükseliyor...",
            "Derinlerden bir uğultu geliyor...", "Fener titriyor...", "Bir sonraki kata iniliyor..."
        };

        private static readonly string[] DangerDigLines =
        {
            "Tavan inliyor...", "Kum yağıyor... dikkat!", "Destekler gıcırdıyor...",
            "Kayalar çatırdıyor...", "Bu ses hiç iyi değil..."
        };

        /// <summary>Kazı sırasında görünen mesaj. Risk yüksekse daha gergin cümleler.</summary>
        public static string DigLine(double risk) => risk >= 0.3 ? Pick(DangerDigLines) : Pick(DigLines);

        /// <summary>Her inişten sonra tur ekranında görünen kısa mesaj.</summary>
        public static string StepMessage(AdvanceResult r)
        {
            string text = $"{r.Step}. kata indin. +{Num(r.RewardGained)}";
            if (r.ShieldUsed) text = $"<color={Gold}>Baret seni kurtardı!</color> " + text;
            if (r.IsMilestone) text += $"\n<color={Gold}>{MilestoneName(r.Step)}!</color>";
            else if (r.NewRecord) text += $"\n<color={Gold}>Yeni derinlik rekoru!</color>";
            return text;
        }

        // ---------------- Baret ve göçük ----------------

        public const string ShieldBreakHeader = "BARET KIRILDI!";

        private static readonly string[] ShieldBreakLines =
        {
            "Kafana koca bir kaya düştü ama baret dayandı!",
            "Tavan çöktü, baret paramparça... ama sen sağlamsın!",
            "Son anda! Baret seni kurtardı.",
            "Kayalar yağdı, baret hepsini karşıladı!"
        };

        public static string ShieldBreakTitle() => Pick(ShieldBreakLines);

        public static string ShieldsLeft(int shields) =>
            shields > 0 ? $"Kalan baret: {shields}" : $"<color={AtRiskColor}>Hiç baretin kalmadı. Bundan sonrası gerçek risk!</color>";

        private static readonly string[] BustTitles = { "GÖÇÜK!", "TAVAN ÇÖKTÜ!", "KAYALAR ALTINDA!", "HER ŞEY KARARDI!" };

        public static string BustTitle() => Pick(BustTitles);

        // ---------------- Bulgular ----------------
        public static string FindTypeName(FindType type)
        {
            switch (type)
            {
                case FindType.Treasure: return "Hazine";
                case FindType.SafeCache: return "Kese";
                case FindType.ProfitBoost: return "Zengin damar";
                case FindType.Safety: return "Sağlam kaya";
                case FindType.Shield: return "Baret";
                case FindType.LuckCharm: return "Uğur tılsımı";
                case FindType.FindStreak: return "Eski harita";
                case FindType.Multiplier: return "Parlak kristal";
                case FindType.CursedTreasure: return "Lanetli put";
                case FindType.Gear: return "Ekipman";
                default: return type.ToString();
            }
        }

        /// <summary>Koleksiyondaki küçük kartlar için kısa ad.</summary>
        public static string FindTypeShortName(FindType type)
        {
            switch (type)
            {
                case FindType.ProfitBoost: return "Damar";
                case FindType.Safety: return "Kaya";
                case FindType.LuckCharm: return "Tılsım";
                case FindType.FindStreak: return "Harita";
                case FindType.Multiplier: return "Kristal";
                case FindType.CursedTreasure: return "Put";
                case FindType.Gear: return "Eşya";
                default: return FindTypeName(type);
            }
        }

        /// <summary>"Efsanevi Hazine" gibi, nadirlik rengine boyanmış başlık. Ekipmansa eşyanın adı.</summary>
        public static string FindTitle(FindResult find)
        {
            if (find.Type == FindType.Gear && find.Item != null) return FantasyTexts.GearName(find.Item);
            return RarityStyle.Colored($"{RarityStyle.Name(find.Tier)} {FindTypeName(find.Type)}", find.Tier);
        }

        public static string FindDescription(FindResult find)
        {
            switch (find.Type)
            {
                case FindType.Gear:
                    return find.Item != null
                        ? $"{FantasyTexts.GearStatShort(find.Item)}\n<color={AtRiskColor}>Yüzeye dönünce envantere girer</color>"
                        : "";
                case FindType.Treasure: return $"+{Num(find.Value)} tur kazancına (göçükte gider)";
                case FindType.SafeCache: return $"+{Num(find.Value)} doğrudan cebe (güvende)";
                case FindType.ProfitBoost: return $"Bu tur kat ödülleri +%{Pct(find.Value)}";
                case FindType.Safety: return $"Bu tur göçük riski -%{Pct(find.Value)}";
                case FindType.Shield: return $"+{(int)find.Value} baret: bir göçüğü durdurur";
                case FindType.LuckCharm: return $"Bu tur nadir bulgu şansı +%{Pct(find.Value)}";
                case FindType.FindStreak: return $"Sonraki {(int)find.Value} katta bulgu GARANTİ";
                case FindType.Multiplier: return $"Risk altındaki kazanç x{1 + find.Value:0.00}!";
                case FindType.CursedTreasure:
                    return $"+{Num(find.Value)} tur kazancına, ama <color={AtRiskColor}>bu tur risk artar</color>";
                default: return "";
            }
        }

        /// <summary>Küçük kartın değer satırı. value = o gruptaki toplam etki.</summary>
        public static string FindShortValue(FindType type, double value)
        {
            switch (type)
            {
                case FindType.Treasure:
                case FindType.SafeCache:
                case FindType.CursedTreasure: return $"+{Num(value)}";
                case FindType.ProfitBoost: return $"+%{Pct(value)}";
                case FindType.Safety: return $"-%{Pct(value)}";
                case FindType.Shield: return $"+{(int)value}";
                case FindType.LuckCharm: return $"+%{Pct(value)}";
                case FindType.FindStreak: return $"{(int)value} kat";
                case FindType.Multiplier: return $"x{1 + value:0.00}";
                case FindType.Gear: return "";  // Kart, eşyanın yuvasını yazar
                default: return "";
            }
        }

        // Nadirliğe göre heyecan cümleleri (açılır pencerede). Her seviyede birkaç seçenek var.
        private static readonly string[][] TierHypeLines =
        {
            new[] { "Bir şey parıldıyor." },
            new[] { "Fena değil!" },
            new[] { "Güzel bir bulgu!" },
            new[] { "Lacivert bir parıltı... değerli bir şey!", "Bu iyi bir işaret!" },
            new[] { "Yeşil ışık! Şansın açılıyor.", "Seçkin bir bulgu, devam mı?" },
            new[] { "DESTANSI! Gözlerin kamaştı.", "Altın sarısı bir ışık!" },
            new[] { "EFSANEVİ! Böylesi nadir görülür.", "Turuncu alevler gibi parlıyor!" },
            new[] { "MİTİK!! Ellerin titriyor...", "Kıpkırmızı bir parıltı... inanılmaz!" },
            new[] { "KADİM!!! Efsaneler gerçekmiş.", "Mor bir ışık mağarayı doldurdu!!!" },
        };

        public static string TierHype(int tier) => Pick(TierHypeLines[Math.Max(0, Math.Min(tier, TierHypeLines.Length - 1))]);

        /// <summary>Açılır pencere başlığı. step: bulgunun katı, milestone: keşif katında mı.</summary>
        public static string FindPopupHeader(FindResult find, int step, bool isMilestone, bool firstReach)
        {
            if (find.FromMonster) return $"GANİMET: {RarityStyle.Name(find.Tier).ToUpper()} {FantasyTexts.SlotName(find.Item.slot).ToUpper()}";
            if (isMilestone && firstReach) return $"YENİ KEŞİF: {MilestoneName(step).ToUpper()}";
            if (isMilestone) return MilestoneName(step).ToUpper();
            return $"{RarityStyle.Name(find.Tier).ToUpper()} BULGU";
        }

        public static string FirstDiscoveryBonus(double bonus) => $"<color={Gold}>İlk keşif ödülü: +{Num(bonus)} cebe!</color>";

        // ---------------- Savaş ----------------

        public const string KeyHintCombat = "↓ / S / Boşluk: SALDIR (basılı tut)   ↑ / W: kaç ve yüzeye dön   Canavara tıklamak da vurur";

        private static readonly string[] MonsterAppearLines =
        {
            "Karanlıktan bir şey çıktı!", "Bir hırıltı... saldırıya hazır ol!", "Yolunu bir canavar kesti!",
            "Molozların arasından gözler parladı!", "Tünel boş değilmiş!"
        };

        public static string MonsterAppears(MonsterInstance m)
        {
            string name = $"<b>{FantasyTexts.MonsterName(m.Definition.id)}</b>";
            return m.Kind == MonsterKind.Normal ? $"{Pick(MonsterAppearLines)} {name}" : $"<color={Gold}>{FantasyTexts.MonsterKindLabel(m.Kind)}:</color> {name}!";
        }

        public static string MonsterNameLine(MonsterInstance m)
        {
            string name = FantasyTexts.MonsterName(m.Definition.id);
            string kind = FantasyTexts.MonsterKindLabel(m.Kind);
            return kind.Length > 0 ? $"<color={Gold}>{kind}</color>  {name}" : name;
        }

        public static string Health(double hp, double max) => $"{Num(Math.Max(0, Math.Ceiling(hp)))} / {Num(max)}";

        public static string PlayerHealthLabel(double hp, double max) => $"Can: {Health(hp, max)}";

        public static string Victory(VictoryResult v) =>
            $"{FantasyTexts.MonsterName(v.Monster.Definition.id)} yenildi! +{Num(v.Bounty)}";

        public static string ShieldRevive => "Baret seni ölümden döndürdü!";

        private static readonly string[] DefeatTitles = { "YENİLDİN!", "YERE SERİLDİN!", "KARANLIK SENİ YUTTU!" };

        public static string DefeatTitle() => Pick(DefeatTitles);

        public static string CombatSummary(CharacterStats s) =>
            $"Saldırı {Num(s.Attack)}   Savunma {Num(s.Defense)}   Kritik %{Pct(s.CritChance)}";

        // ---------------- Tur sonu ----------------
        public static string CashedOut(RunState run, bool fled) =>
            (fled ? "Canavardan kaçıp yüzeye döndün! " : "Yüzeye döndün! ") +
            $"{run.Step}. kattan {Num(run.Payout)} çıkardın.\n{RunSummary(run, lootSaved: true)}";

        public static string Busted(AdvanceResult r, RunState run)
        {
            string text = $"{BustTitle()} {r.Step}. katta göçük oldu (risk %{Pct(r.BustChance)}).\nKaybedilen: {Num(r.LostEarnings)}";
            if (r.InsurancePayout > 0) text += $"   Sigorta kurtardı: {Num(r.InsurancePayout)}";
            return text + "\n" + RunSummary(run, lootSaved: false);
        }

        public static string Defeated(MonsterHitResult hit, RunState run, string monsterName)
        {
            string text = $"{DefeatTitle()} {monsterName} seni {run.Step}. katta yendi.\nKaybedilen: {Num(hit.LostEarnings)}";
            if (hit.InsurancePayout > 0) text += $"   Sigorta kurtardı: {Num(hit.InsurancePayout)}";
            return text + "\n" + RunSummary(run, lootSaved: false);
        }

        private static string RunSummary(RunState run, bool lootSaved)
        {
            string text = $"Turun toplam değeri: <b>{Num(run.TotalValue)}</b>";
            if (run.IsNewBestRun) text += $"  <color={Gold}>EN DEĞERLİ TUR REKORU!</color>";
            if (run.Kills > 0) text += $"   Yenilen canavar: {run.Kills}";

            if (run.PendingLoot.Count > 0)
            {
                text += lootSaved
                    ? $"\nEnvantere giren eşya: {run.PendingLoot.Count - run.LootAutoSold}"
                    : $"\n<color={AtRiskColor}>Kaybedilen eşya: {run.PendingLoot.Count}</color>";
                if (lootSaved && run.LootAutoSold > 0) text += $"  (envanter dolu, {run.LootAutoSold} eşya satıldı: +{Num(run.LootAutoSoldValue)})";
            }

            if (run.Finds.Count == 0) return text + "\nBu turda bulgu yok.";

            FindResult best = run.Finds[0];
            foreach (FindResult f in run.Finds)
            {
                if (f.Tier > best.Tier) best = f;
            }
            return text + $"\nBulgular: {run.Finds.Count}   En iyisi: {FindTitle(best)}";
        }

        // ---------------- Ekipman ekranı ----------------
        public const string TabUpgrades = "Yükseltmeler";
        public const string TabEquipment = "Ekipman";
        public const string EquipButton = "Kuşan";
        public const string UnequipButton = "Çıkar";
        public const string AutoEquipButton = "En iyileri kuşan";
        public const string NoItemSelected = "Bir eşyaya tıkla: statlarını gör, kuşan ya da sat.";
        public static string SellButton(double value) => $"Sat: +{Num(value)}";
        public static string InventoryTitle(int count, int cap) => $"Envanter ({count}/{cap})";
        public static string ItemLevelLine(GearItem item, bool equipped) =>
            $"{FantasyTexts.SlotName(item.slot)}  ·  Seviye {item.itemLevel}{(equipped ? "  ·  <color=#3CCB5A>KUŞANILDI</color>" : "")}";
        public static string Sold(double value) => $"Satıldı: +{Num(value)}";
        public static string AutoEquipped(int changed) => changed > 0 ? $"{changed} eşya değiştirildi." : "Zaten en iyileri kuşanmışsın.";

        // ---------------- Yükseltmeler ----------------
        public static string UpgradeName(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.Earnings: return "Daha iyi kazma";
                case UpgradeType.Safety: return "Destek kirişleri";
                case UpgradeType.Insurance: return "Sigorta";
                case UpgradeType.FindChance: return "Maden dedektörü";
                case UpgradeType.Luck: return "Uğurlu fener";
                case UpgradeType.FindValue: return "Kuyumcu terazisi";
                case UpgradeType.StartShield: return "Yedek baret";
                default: return type.ToString();
            }
        }

        /// <summary>"mevcut → sonraki" etki metni. Seviye doluysa sadece mevcut.</summary>
        public static string UpgradeEffect(BalanceData b, UpgradeType type, int level, bool maxed)
        {
            string current = UpgradeEffectValue(b, type, level);
            return maxed ? current : $"{current} → {UpgradeEffectValue(b, type, level + 1)}";
        }

        private static string UpgradeEffectValue(BalanceData b, UpgradeType type, int level)
        {
            switch (type)
            {
                case UpgradeType.Earnings: return $"Kazanç x{RiskMath.EarningsBonus(b, level):0.00}";
                case UpgradeType.Safety: return $"Göçük riski x{RiskMath.SafetyFactor(b, level):0.00}";
                case UpgradeType.Insurance: return $"Kurtarılan %{Pct(RiskMath.InsuranceFraction(b, level))}";
                case UpgradeType.FindChance: return $"Bulgu şansı +%{Pct(b.findChanceUpgrade.effectPerLevel * level)}";
                case UpgradeType.Luck: return $"Nadirlik +%{Pct(b.luckUpgrade.effectPerLevel * level)}";
                case UpgradeType.FindValue: return $"Bulgu değeri x{RiskMath.FindValueBonus(b, level):0.00}";
                case UpgradeType.StartShield: return $"Tura {RiskMath.StartShields(b, level)} baretle başla";
                default: return "";
            }
        }

        public static string UpgradeInfo(UpgradeType type, int level, int maxLevel, string effect) =>
            $"<b>{UpgradeName(type)}</b>  (Sv. {level}/{maxLevel})\n{effect}";

        public static string BuyButton(double cost) => $"Al: {Num(cost)}";

        // ---------------- Yardımcılar ----------------

        // Büyük sayı kısaltmaları: 12.3K, 4.56M, 7.8B, 1.2T, sonra Qa, Qi... (idle oyun standardı)
        private static readonly string[] Suffixes = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };

        /// <summary>
        /// Para gösterimi: 10.000'in altında tam sayı (binlik ayraçlı), üstünde kısaltma (12.3K, 4.56M).
        /// Kısaltma listesi biterse bilimsel gösterim (1.23e45). Sayı ne kadar büyük olursa olsun metin kısa kalır.
        /// </summary>
        public static string Num(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "∞";

            value = Math.Floor(value);
            double abs = Math.Abs(value);
            if (abs < 10000) return value.ToString("N0");

            int group = (int)Math.Floor(Math.Log10(abs) / 3);  // 3 hane = 1 grup (bin, milyon, ...)
            if (group >= Suffixes.Length) return value.ToString("0.00e0");

            double scaled = value / Math.Pow(1000, group);
            string format = scaled < 10 ? "0.00" : scaled < 100 ? "0.0" : "0";
            return scaled.ToString(format) + Suffixes[group];
        }

        /// <summary>0.253 -> "25.3"</summary>
        public static string Pct(double fraction) => (fraction * 100.0).ToString("0.0");

        /// <summary>Havuzdan rastgele bir cümle. (Sunum rastgeleliği; oyun kurallarını etkilemez.)</summary>
        private static string Pick(string[] pool) => pool[UnityEngine.Random.Range(0, pool.Length)];
    }
}
