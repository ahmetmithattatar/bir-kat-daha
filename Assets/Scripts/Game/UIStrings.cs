using System;
using System.Collections.Generic;
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

        /// <summary>Bu turun bulgu etkileri: kazanç bonusu, risk çarpanı.</summary>
        public static string RunBuffs(RunState run)
        {
            if (run == null) return "";

            string text = "";
            if (run.ProfitBonus > 0) text += $"   Kazanç +%{Pct(run.ProfitBonus)}";
            if (run.SafetyFactor < 1) text += $"   Risk x{run.SafetyFactor:0.00}";
            if (run.SafetyFactor > 1) text += $"   <color={AtRiskColor}>Lanet: risk x{run.SafetyFactor:0.00}</color>";
            if (run.RunLuck > 0) text += $"   Şans +%{Pct(run.RunLuck)}";
            if (run.GuaranteedFinds > 0) text += $"   <color={Gold}>Harita: {run.GuaranteedFinds} kat</color>";
            return text;
        }

        public static string CollectionTitle(int count) => count > 0 ? $"Bu turun bulguları ({count})" : "Bu turda henüz bulgu yok";

        // ---------------- Bölge bossları (keşif katları) ----------------

        // Bölge bosslarının katları (her 10 katta bir). 90'dan sonra adlar döner, "Derin" ön ekiyle.
        private static readonly string[] ZoneBossNames =
        {
            "Goblin Tünelleri", "Kayıp Cüce Karakolu", "Kristal Salonlar", "Gözcünün Gölü", "Cüce Kralın Demirhanesi",
            "Kadim Höyükler", "Ejderin Altın Yatağı", "Gölge ve Alev Köprüsü", "Dünyanın Kökleri"
        };

        /// <summary>Her boss katının (10, 20, 30...) kendine özgü adı.</summary>
        public static string MilestoneName(int step)
        {
            if (step <= 0 || step % 10 != 0) return $"{step}. Kat";
            int i = step / 10 - 1;
            string name = ZoneBossNames[i % ZoneBossNames.Length];
            int cycle = i / ZoneBossNames.Length;
            return cycle == 0 ? name : $"Derin {name} {cycle + 1}";
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
            if (r.IsMilestone) text += $"\n<color={Gold}>{MilestoneName(r.Step)}!</color>";
            else if (r.NewRecord) text += $"\n<color={Gold}>Yeni derinlik rekoru!</color>";
            return text;
        }

        // ---------------- Göçük ----------------

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

        // ---------------- Telafi, kurtarma, öfke ----------------

        /// <summary>Göçük/yenilgi sonrası teselli satırları: kurtarılan eşya + talihsizlik telafisi.</summary>
        public static string AfterBust(GearItem salvaged, int pityStacks, double pityLuck)
        {
            string text = "";
            if (salvaged != null) text += $"\n<color={Gold}>ENKAZDAN KURTARDIN:</color> {FantasyTexts.GearName(salvaged)}";
            if (pityStacks > 0) text += $"\n<color=#8FD3FF>Talihsizlik telafisi: sonraki turlarda nadirlik +%{Pct(pityLuck)} ({pityStacks}/5)</color>";
            return text;
        }

        public static string Enrage(int stacks) => $"ÖFKE {stacks}! Saldırı +%{stacks * 20}";

        // ---------------- Güç ve risk göstergesi ----------------

        public static string ThreatName(ThreatLevel t)
        {
            switch (t)
            {
                case ThreatLevel.Easy: return "<color=#3CCB5A>Kolay</color>";
                case ThreatLevel.Medium: return "<color=#FFE03A>Orta</color>";
                case ThreatLevel.Hard: return "<color=#FF9A2E>Zor</color>";
                default: return "<color=#FF3B3B>ÖLÜMCÜL</color>";
            }
        }

        /// <summary>"Gücün: 14. kat seviyesi · Bu kat: Kolay"</summary>
        public static string PowerLine(int powerLevel, ThreatLevel threat, bool inCombat) =>
            $"Savaş gücün: <b>{powerLevel}. kat</b> seviyesi   {(inCombat ? "Bu savaş" : "Sonraki kat")}: {ThreatName(threat)}";

        /// <summary>Göçük açısından ortalama kaç kat daha.</summary>
        public static string FloorsLeft(double floors) =>
            floors >= 40 ? "Göçük riski: çok uzun süre dayanırsın" : $"Göçük hesabıyla ortalama <b>{floors:0.0}</b> kat daha dayanırsın";

        /// <summary>Kumar satırı: kazanırsan / kaybedersen / olasılık + yorum.</summary>
        public static string Gamble(double win, double lose, double chance, double ev)
        {
            string loseText = lose <= 0 ? $"<color={Gold}>0</color>" : $"<color={AtRiskColor}>{Num(lose)}</color>";
            string verdict;
            if (lose <= 0) verdict = $"<color=#3CCB5A>Güvenli adım</color>";
            else if (ev >= 0) verdict = "<color=#3CCB5A>Değer</color>";
            else if (ev > -win) verdict = $"<color={Gold}>Kumar</color>";
            else verdict = "<color=#FF3B3B>Çılgınlık</color>";
            return $"Kazanırsan <color=#3CCB5A>+{Num(win)}</color> · Kaybedersen {loseText} (%{Pct(chance)})  {verdict}";
        }

        public static string SwingsLine(int swings) => $"Duvar: {swings} vuruş";

        // ---------------- Seviye, açılan özellikler ----------------

        public const string LevelUpHeader = "SEVİYE ATLADIN!";
        public static string LevelTitle(int level) => $"Seviye {level}";
        public static string LevelShort(int level) => $"Sv {level}";
        public static string XpGain(double xp) => $"+{Num(xp)} XP";
        public static string LevelUpPoints(int available) =>
            available > 0 ? $"<color={Gold}>+1 yetenek puanı</color> (harcanacak: {available})" : "Daha güçlüsün!";
        public static string XpLine(double xp, double needed) => $"XP {Num(xp)} / {Num(needed)}";

        public static string FeatureName(Feature f)
        {
            switch (f)
            {
                case Feature.Talents: return "Yetenek Ağacı";
                case Feature.Equipment: return "Ekipman ve ganimet";
                case Feature.Insurance: return "Sigorta";
                case Feature.FindChance: return "Maden dedektörü";
                case Feature.Checkpoints: return "Geçitler";
                case Feature.Enhance: return "Güçlendirme";
                case Feature.Luck: return "Uğurlu fener";
                case Feature.FindValue: return "Kuyumcu terazisi";
                case Feature.SpecialFinds: return "Özel bulgular";
                case Feature.Prestige: return "Yeni Çağ";
                default: return f.ToString();
            }
        }

        public static string FeatureHint(Feature f)
        {
            switch (f)
            {
                case Feature.Talents: return "Her seviye 1 puan. Yüzeyde Yetenekler sekmesi.";
                case Feature.Equipment: return "Canavarlar ve kayalar artık eşya düşürür.";
                case Feature.Insurance: return "Göçükte kazancın bir kısmını kurtar.";
                case Feature.FindChance: return "Daha sık bulgu.";
                case Feature.Checkpoints: return "Yendiğin bossun katından başla.";
                case Feature.Enhance: return "Eşyalarını +10'a kadar güçlendir (riskli!).";
                case Feature.Luck: return "Daha nadir bulgular.";
                case Feature.FindValue: return "Hazine ve kese daha değerli.";
                case Feature.SpecialFinds: return "Eski harita, parlak kristal, lanetli put kayalardan çıkar.";
                case Feature.Prestige: return "30. kattan sonra her şeyi sıfırla, Yadigar kazan.";
                default: return "";
            }
        }

        /// <summary>Seviye penceresindeki "YENİ: ..." satırları. Hiç yoksa null.</summary>
        public static string FeaturesUnlocked(List<Feature> features)
        {
            if (features == null || features.Count == 0) return null;
            string text = $"<color={Gold}>YENİ ÖZELLİK AÇILDI!</color>";
            foreach (Feature f in features) text += $"\n<b>{FeatureName(f)}</b>: {FeatureHint(f)}";
            return text;
        }

        public static string LockedShort(int level) => $"Sv {level}";
        public static string LockedTip(Feature f, int level) => $"<b>{FeatureName(f)}</b>\nSeviye {level}'te açılır. {FeatureHint(f)}";
        public static string LockedButton(int level) => $"Seviye {level}'te açılır";
        public static string NextUnlockLine(Feature? f, int level) =>
            f.HasValue ? $"Sıradaki özellik: <b>{FeatureName(f.Value)}</b> (Sv {level})" : "Tüm özellikler açık";

        public static string FirstOfTierLine(int tier) =>
            $"<color={Gold}>İLK {RarityStyle.Name(tier).ToUpper()}!</color> Bu nadirliği ilk kez buldun.";
        public const string TwinFindLine = "<color=#FFE03A>EJDER GÖZÜ: ikiz bulgu!</color>";
        public const string BossAhead = "SONRAKİ KAT: BÖLGE BOSSU!";

        // ---------------- Yetenek ağacı ----------------

        public const string TabTalents = "Yetenekler";
        public static string TalentPoints(int available, int spent) =>
            available > 0 ? $"Yetenek puanı: <color={Gold}><b>{available}</b></color>   (harcanan {spent})" : $"Yetenek puanı: 0   (harcanan {spent})";
        public static string RespecButton(double cost) => $"Sıfırla: {Num(cost)}";
        public static string RespecDone(double cost) => $"Yetenekler sıfırlandı (-{Num(cost)}). Puanların geri geldi.";
        public static string TalentsLocked(int level) => $"Yetenek ağacı seviye {level}'de açılır. Kaz, canavar kes, XP topla!";

        public static string BranchName(TalentBranch b)
        {
            switch (b)
            {
                case TalentBranch.Dwarf: return "CÜCE MADENCİ";
                case TalentBranch.Sword: return "KILIÇ USTASI";
                case TalentBranch.Dragon: return "EJDER AVCISI";
                default: return b.ToString();
            }
        }

        public static string BranchHint(TalentBranch b)
        {
            switch (b)
            {
                case TalentBranch.Dwarf: return "Kazı, risk ve kazanç";
                case TalentBranch.Sword: return "Savaş ve boss";
                case TalentBranch.Dragon: return "Şans, ganimet ve servet";
                default: return "";
            }
        }

        public static string TalentName(string id)
        {
            switch (id)
            {
                case "sturdy_pick": return "Sağlam Sap";
                case "stone_sense": return "Taş Sezgisi";
                case "gold_vein": return "Altın Damar";
                case "deep_instinct": return "Derin İçgüdü";
                case "brace_master": return "Payanda Ustası";
                case "rubble_hunter": return "Enkaz Avcısı";
                case "mountain_heart": return "Dağın Kalbi";
                case "keen_edge": return "Keskin Ağız";
                case "thick_hide": return "Kalın Deri";
                case "deadly_eye": return "Ölümcül Göz";
                case "armor_master": return "Zırh Ustası";
                case "blood_drinker": return "Kan İçen";
                case "cold_blood": return "Soğukkanlı";
                case "kingslayer": return "Kral Katili";
                case "lucky_birth": return "Şanslı Doğum";
                case "jeweler_eye": return "Kuyumcu Gözü";
                case "plunderer": return "Yağmacı";
                case "haggler": return "Pazarlıkçı";
                case "glimmer": return "Pırıltı";
                case "fortune_wheel": return "Talih Çarkı";
                case "dragon_eye": return "Ejder Gözü";
                default: return id;
            }
        }

        /// <summary>Etkinin metni. value = rank * perRank (toplam).</summary>
        public static string TalentEffectText(TalentEffect e, double value)
        {
            switch (e)
            {
                case TalentEffect.DigPower: return $"Kazı gücü +%{Pct(value)}";
                case TalentEffect.RiskReduction: return $"Göçük riski -%{Pct(value)}";
                case TalentEffect.RewardBonus: return $"Kat ödülü +%{Pct(value)}";
                case TalentEffect.FindChance: return $"Bulgu şansı +%{Pct(value)}";
                case TalentEffect.Insurance: return $"Sigorta +%{Pct(value)}";
                case TalentEffect.Salvage: return $"Enkazdan kurtarma +%{Pct(value)}";
                case TalentEffect.ZoneStartSafety: return $"Bölgenin ilk 5 katında göçük riski -%{Pct(value)}";
                case TalentEffect.Attack: return $"Saldırı +%{Pct(value)}";
                case TalentEffect.Health: return $"Can +%{Pct(value)}";
                case TalentEffect.CritChance: return $"Kritik şansı +%{Pct(value)}";
                case TalentEffect.Defense: return $"Savunma +%{Pct(value)}";
                case TalentEffect.Lifesteal: return $"Vurduğun hasarın %{Pct(value)} kadarı can olur";
                case TalentEffect.EnrageSlow: return $"Boss öfkesi %{Pct(value)} daha geç gelir";
                case TalentEffect.BossDamage: return $"Bosslara +%{Pct(value)} hasar";
                case TalentEffect.Luck: return $"Nadirlik +%{Pct(value)}";
                case TalentEffect.FindValue: return $"Hazine/kese değeri +%{Pct(value)}";
                case TalentEffect.GearDrop: return $"Ganimet düşme şansı +%{Pct(value)}";
                case TalentEffect.SellValue: return $"Satış değeri +%{Pct(value)}";
                case TalentEffect.TierUpgrade: return $"Bulgu %{Pct(value)} ihtimalle bir nadirlik üstte";
                case TalentEffect.PityBoost: return $"Talihsizlik telafisi kademe başı +%{Pct(value)}";
                case TalentEffect.TwinFind: return $"Bulgu %{Pct(value)} ihtimalle ikiz çıkar";
                default: return e.ToString();
            }
        }

        public static string TalentTooltip(TalentDefinition t, int rank, bool rowOpen, int pointsPerRow)
        {
            string now = rank > 0 ? TalentEffectText(t.effect, rank * t.perRank) : "Henüz öğrenilmedi";
            string next = rank < t.maxRank ? $"\nSonraki: {TalentEffectText(t.effect, (rank + 1) * t.perRank)}" : "\n<color=#3CCB5A>MAKSİMUM</color>";
            string lockLine = rowOpen ? "" : $"\n<color=#FF9A2E>Bu dala {t.row * pointsPerRow} puan harcayınca açılır</color>";
            return $"<b>{TalentName(t.id)}</b>  ({rank}/{t.maxRank})\n{now}{next}\n<color=#9AA0B4>Tıkla: 1 puan harca</color>{lockLine}";
        }

        // ---------------- Geçitler ----------------

        public static string StartFrom(int step) =>
            step <= 0 ? "Başlangıç: Yüzey" : $"Başlangıç: {step}. kat ({MilestoneName(step)})";
        public static string CheckpointUnlocked(int step) => $"<color={Gold}>GEÇİT AÇILDI: artık {step}. kattan başlayabilirsin!</color>";

        // ---------------- Güçlendirme ----------------

        public static string EnhanceButton(int nextLevel, double chance, double cost) =>
            $"Güçlendir +{nextLevel}\n%{Pct(chance)} · {Num(cost)}";
        public const string EnhanceMaxed = "MAKS +10";
        public static string EnhanceSuccess(int level) => $"<color=#3CCB5A>BAŞARILI! Eşya artık +{level}</color>";
        public static string EnhanceDowngrade(int level) => $"<color=#FF9A2E>Başarısız... seviye +{level}'e düştü</color>";
        public const string EnhanceBroken = "<color=#FF3B3B>EŞYA KIRILDI! Kayboldu...</color>";
        public const string EnhanceFailSafe = "<color=#FFE03A>Başarısız, ama eşya sağlam.</color>";
        public static string EnhanceRiskNote(int current, int downgradeFrom, int breakFrom) =>
            current >= breakFrom ? $"<color=#FF3B3B>Dikkat: başarısızlıkta eşya KIRILIR</color>"
            : current >= downgradeFrom ? $"<color=#FF9A2E>Başarısızlıkta seviye düşer</color>"
            : "Başarısızlıkta kayıp yok";

        public static string PowerCompare(int now, int with) =>
            with == now ? $"Savaş gücü: {now}. kat (değişmez)"
            : with > now ? $"Savaş gücü: {now} → <color=#3CCB5A>{with}. kat</color>"
            : $"Savaş gücü: {now} → <color=#FF5A4A>{with}. kat</color>";

        public static string SwingsCompare(int now, int with) =>
            with == now ? "" : with < now ? $"   Duvar: {now} → <color=#3CCB5A>{with} vuruş</color>" : $"   Duvar: {now} → <color=#FF5A4A>{with} vuruş</color>";

        // ---------------- Yeni Çağ ----------------

        public const string TabPrestige = "Yeni Çağ";
        public static string PrestigeHeader(int age, double relics) => $"<b>{age}. Çağ</b>    Yadigar: <color={Gold}><b>{Num(relics)}</b></color>";
        public static string PrestigeInfo(int minStep, int bestStep, double gain) =>
            bestStep < minStep
                ? $"Yeni Çağ {minStep}. kata ulaşınca açılır (rekor: {bestStep}).\nHer şey sıfırlanır, Yadigar kazanırsın. Kadim yükseltmeler sonsuza kadar kalır."
                : $"Şimdi başlatırsan <color={Gold}>+{Num(gain)} Yadigar</color> kazanırsın.\nPara, yükseltmeler, eşyalar, rekor ve geçitler sıfırlanır. Kadim yükseltmeler kalır. Daha derin = çok daha fazla Yadigar.";
        public static string PrestigeButton(double gain) => $"YENİ ÇAĞ BAŞLAT (+{Num(gain)} Yadigar)";
        public const string PrestigeConfirm = "EMİN MİSİN? Her şey sıfırlanacak!\nTekrar bas";
        public static string PrestigeDone(int age, double gained) => $"<color={Gold}>{age}. ÇAĞ BAŞLADI! +{Num(gained)} Yadigar</color>";
        public static string RelicCostButton(double cost) => $"Al: {Num(cost)} Yadigar";

        // ---------------- Ekipman ekranı ----------------
        public const string TabUpgrades = "Yükseltmeler";
        public const string TabEquipment = "Ekipman";
        public const string EquipButton = "Kuşan";
        public const string UnequipButton = "Çıkar";
        public const string AutoEquipButton = "En iyileri kuşan";
        public const string SellAllButton = "Çantayı sat";
        public const string SellAllConfirm = "Emin misin? Tekrar bas";
        public static string SoldAll(int count, double value) =>
            count > 0 ? $"{count} eşya satıldı: +{Num(value)}" : "Çanta zaten boş.";
        public const string NoItemSelected = "Bir eşyaya tıkla: statlarını gör, kuşan ya da sat.";
        public static string SellButton(double value) => $"Sat: +{Num(value)}";
        public static string InventoryTitle(int count, int cap) => $"Envanter ({count}/{cap})";
        public static string ItemLevelLine(GearItem item, bool equipped) =>
            $"{FantasyTexts.BaseName(item)}  ·  Seviye {item.itemLevel}{(equipped ? "  ·  <color=#3CCB5A>KUŞANILDI</color>" : "")}";
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
