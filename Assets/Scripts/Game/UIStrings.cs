using System;
using System.Collections.Generic;
using RiskLoop.Core;
using UnityEngine;

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

        /// <summary>
        /// Koleksiyondaki kartın ipucu (fareyle üstüne gelince): grubun toplam etkisi ve risk altında mı, cepte mi.
        /// Oyuncu turun neye değdiğini görsün: "kristal geldi, kazancım x1.5, şimdi dönmek mi daha derin mi?"
        /// value: grubun toplam etkisi (Sağlam kaya ve kristalde birleşik çarpan - 1).
        /// </summary>
        public static string FindGroupTooltip(FindResult sample, double value, int count)
        {
            string title = FindTitle(sample) + (count > 1 ? $"  ×{count}" : "");
            string body;
            switch (sample.Type)
            {
                case FindType.Treasure:
                    body = $"Tur kazancına <b>+{Num(value)}</b>\n<color={AtRiskColor}>Risk altında: göçükte gider, yüzeye dönünce cebe girer</color>"; break;
                case FindType.SafeCache:
                    body = $"<b>+{Num(value)}</b> doğrudan cebe girdi\n<color=#3CCB5A>Güvende: göçükte bile kaybolmaz</color>"; break;
                case FindType.ProfitBoost:
                    body = $"Bu tur bütün kat ödülleri <b>+%{Pct(value)}</b>\nNe kadar derine inersen o kadar çok kazandırır"; break;
                case FindType.Safety:
                    body = $"Bu tur göçük riski <b>-%{Pct(value)}</b> (taban riskin altına inmez)"; break;
                case FindType.LuckCharm:
                    body = $"Bu tur nadir bulgu şansı <b>+%{Pct(value)}</b>"; break;
                case FindType.FindStreak:
                    body = $"Toplam <b>{(int)value}</b> katta bulgu garantisi verdi"; break;
                case FindType.Multiplier:
                    body = $"Risk altındaki kazanç <b>x{1 + value:0.00}</b> oldu (bulunduğu anda)\n<color={AtRiskColor}>Çarpılan kazanç da risk altında</color>"; break;
                case FindType.CursedTreasure:
                    body = $"Tur kazancına <b>+{Num(value)}</b>, ama <color={AtRiskColor}>bu tur göçük riski arttı</color>"; break;
                case FindType.Gear:
                    body = sample.Item != null
                        ? $"{FantasyTexts.GearStatShort(sample.Item)}\n<color={AtRiskColor}>Yüzeye dönünce envantere girer, göçükte kaybolur</color>"
                        : "";
                    break;
                default: body = ""; break;
            }
            return $"<b>{title}</b>\n{body}";
        }

        public static string FindOverflowTooltip(int hiddenGroups) =>
            $"Yer kalmadı: {hiddenGroups} grup daha var.\nEn nadir bulgular solda gösterilir.";

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
            string name = $"<b><color={DifficultyColor(m.Difficulty)}>{FantasyTexts.MonsterName(m.Definition.id)}</color></b>";
            return m.Kind == MonsterKind.Normal ? $"{Pick(MonsterAppearLines)} {name}" : $"<color={Gold}>{FantasyTexts.MonsterKindLabel(m.Kind)}:</color> {name}!";
        }

        // Canavar zorluk renkleri (0 çok kolay .. 8 çok zor), WoW'daki "con" renkleri gibi
        private static readonly string[] DifficultyColors =
        {
            "#E8E8E0", // açık beyaz: çok kolay (XP yok)
            "#9FD8FF", // açık mavi
            "#3C8CFF", // mavi
            "#3CCB5A", // yeşil
            "#FFE03A", // sarı
            "#FF9A2E", // turuncu
            "#FF3B3B", // kırmızı
            "#FF6EC7", // pembe
            "#8A8A94", // gri: çok zor (XP yok)
        };

        public static string DifficultyColor(int band) => DifficultyColors[Math.Max(0, Math.Min(band, DifficultyColors.Length - 1))];

        /// <summary>Canavarın adı zorluk renginde. XP vermeyen kademelerde "(XP yok)".</summary>
        public static string MonsterNameLine(MonsterInstance m, bool givesXp)
        {
            string name = $"<color={DifficultyColor(m.Difficulty)}>{FantasyTexts.MonsterName(m.Definition.id)}  <size=70%>Sv {m.Level}</size></color>";
            if (!givesXp) name += " <size=60%><color=#9AA0B4>(XP yok)</color></size>";
            string kind = FantasyTexts.MonsterKindLabel(m.Kind);
            return kind.Length > 0 ? $"<color={Gold}>{kind}</color>  {name}" : name;
        }

        public static string Health(double hp, double max) => $"{Num(Math.Max(0, Math.Ceiling(hp)))} / {Num(max)}";

        public static string PlayerHealthLabel(double hp, double max) => $"Can: {Health(hp, max)}";

        public static string Victory(VictoryResult v) =>
            $"{FantasyTexts.MonsterName(v.Monster.Definition.id)} yenildi! +{Num(v.Bounty)}";

        private static readonly string[] DefeatTitles = { "YENİLDİN!", "YERE SERİLDİN!", "KARANLIK SENİ YUTTU!" };

        public static string DefeatTitle() => Pick(DefeatTitles);

        // ---------------- Tur sonu ----------------
        public static string CashedOut(RunState run, bool fled) =>
            (fled ? "Canavardan kaçıp yüzeye döndün! " : "Yüzeye döndün! ") +
            $"{run.Step}. kattan {Num(run.Payout)} çıkardın.\n{RunSummary(run, lootSaved: true)}";

        public static string Busted(AdvanceResult r, RunState run)
        {
            string text = $"{BustTitle()} {r.Step}. katta göçük oldu (risk %{Pct(r.BustChance)}).\nKaybedilen: {Num(r.LostEarnings)}";
            return text + "\n" + RunSummary(run, lootSaved: false);
        }

        public static string Defeated(MonsterHitResult hit, RunState run, string monsterName)
        {
            string text = $"{DefeatTitle()} {monsterName} seni {run.Step}. katta yendi.\nKaybedilen: {Num(hit.LostEarnings)}";
            return text + "\n" + RunSummary(run, lootSaved: false);
        }

        private static string RunSummary(RunState run, bool lootSaved)
        {
            string text = $"Turun toplam değeri: <b>{Num(run.TotalValue)}</b>";
            if (run.IsNewBestRun) text += $"  <color={Gold}>EN DEĞERLİ TUR REKORU!</color>";
            if (run.Kills > 0) text += $"   Yenilen canavar: {run.Kills}";
            if (run.CourageTotalMult > 1.01) text += $"   <color=#FFC83A>Cesaret ödülü toplam {CourageBonus(run.CourageTotalMult)}</color>";

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

        // ---------------- Kurtarma, öfke ----------------

        /// <summary>Göçük/yenilgi sonrası teselli satırı: enkazdan kurtarılan eşya.</summary>
        public static string AfterBust(GearItem salvaged)
        {
            string text = "";
            if (salvaged != null) text += $"\n<color={Gold}>ENKAZDAN KURTARDIN:</color> {FantasyTexts.GearName(salvaged)}";
            return text;
        }

        public static string Enrage(int stacks) => $"ÖFKE {stacks}! Saldırı +%{stacks * 20}";

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

        /// <summary>Ne açılacağını SÖYLEMEZ, sadece ne zaman: merak + hedef. 0 = hepsi açık.</summary>
        public static string NextUnlockTeaser(int level) => level > 0 ? $"Seviye {level}'de yeni bir şey açılacak..." : "";

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
                case "brace_master": return "Derin Soluk";
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
                case TalentEffect.CourageBonus: return $"Cesaret ödülü +%{Pct(value)}";
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
                case TalentEffect.RiskLuck: return $"Riskli katta bulgu nadirliği +%{Pct(value)}";
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
        public const string EnhanceBrokenTitle = "EŞYA PARAMPARÇA!";

        /// <summary>Görünüşün değiştiği eşik seviyeler: ekranda başlık (null = eşik değil).</summary>
        public static string EnhanceMilestone(int level)
        {
            switch (level)
            {
                case 4: return "+4: ALTIN PARILTI!";
                case 7: return "+7: CİLALI ÇELİK!";
                case 10: return "+10: ALTIN KAPLAMA!!!";
                default: return null;
            }
        }
        public const string EnhanceFailSafe = "<color=#FFE03A>Başarısız, ama eşya sağlam.</color>";
        public static string EnhanceRiskNote(int current, int downgradeFrom, int breakFrom) =>
            current >= breakFrom ? $"<color=#FF3B3B>Dikkat: başarısızlıkta eşya KIRILIR</color>"
            : current >= downgradeFrom ? $"<color=#FF9A2E>Başarısızlıkta seviye düşer</color>"
            : "Başarısızlıkta kayıp yok";

        // ---------------- Kudret (eşya puanı) ----------------

        public const string KudretLabel = "KUDRET";

        // Kudret rütbeleri (sadece görünüş: karakterin ayak altı ışığı ve kıvılcımları bununla büyür)
        private static readonly double[] KudretRankThresholds = { 0, 150, 500, 1200, 2500, 5000, 9000 };
        private static readonly string[] KudretRankNames = { "Çaylak", "Madenci", "Yiğit", "Kahraman", "Efsane", "Yarı Tanrı", "Ölümsüz" };
        private static readonly Color[] KudretRankColors =
        {
            new Color(0.7f, 0.7f, 0.75f), new Color(0.75f, 0.9f, 1f), new Color(0.3f, 0.75f, 1f), new Color(0.3f, 0.9f, 0.4f),
            new Color(1f, 0.85f, 0.25f), new Color(1f, 0.55f, 0.15f), new Color(0.75f, 0.4f, 1f)
        };

        public static int KudretRank(double kudret)
        {
            int rank = 0;
            for (int i = 0; i < KudretRankThresholds.Length; i++) if (kudret >= KudretRankThresholds[i]) rank = i;
            return rank;
        }

        public static string KudretRankName(int rank) => KudretRankNames[Math.Max(0, Math.Min(rank, KudretRankNames.Length - 1))];
        public static Color KudretRankColor(int rank) => KudretRankColors[Math.Max(0, Math.Min(rank, KudretRankColors.Length - 1))];

        /// <summary>Sonraki rütbe için gereken Kudret (son rütbedeyse 0).</summary>
        public static double NextKudretRankAt(double kudret)
        {
            int rank = KudretRank(kudret);
            return rank + 1 < KudretRankThresholds.Length ? KudretRankThresholds[rank + 1] : 0;
        }

        // ---------------- Sınıflar ----------------

        public const string ClassSelectTitle = "SINIFINI SEÇ";
        public const string ClassSelectHint = "Sınıf; canını, gücünü, silahlarını ve büyü kitabını belirler. Yeni Çağda yeniden seçebilirsin.";
        public const string ChooseButton = "SEÇ";
        public static string ClassChosen(HeroClass c) =>
            $"<color=#FFE03A>{ClassName(c).ToUpper()} OLDUN!</color> Büyü kitabın açıldı. Kazıda büyü kısayolları: 1-6.";
        public static string WrongClassWeapon(HeroClass c) => $"<color=#FF5A4A>Bu silah {ClassName(c)} içindir</color>";

        public static string ClassName(HeroClass c)
        {
            switch (c)
            {
                case HeroClass.Warrior: return "Savaşçı";
                case HeroClass.Mage: return "Büyücü";
                case HeroClass.Archer: return "Okçu";
                default: return "Sınıfsız";
            }
        }

        public static string ClassDescription(HeroClass c)
        {
            switch (c)
            {
                case HeroClass.Warrior:
                    return "Kalın zırh, sağlam yürek.\n<color=#3CCB5A>Can +%30, Savunma +%35</color>\nKılıç, balta, gürz, çekiç\n" +
                           "Büyüleri: güçlü darbe, iyileşme, tavanı omuzlama, ganimet hırsı";
                case HeroClass.Mage:
                    return "Kadim bilginin ve kaderin efendisi.\n<color=#3CCB5A>Saldırı +%20, Şans +%6, Mana +50</color> <color=#FF9A2E>Can -%10</color>\n" +
                           "Değnek, asa, küre, kitap\nBüyüleri: ateş topu, KEHANET (göçüğü önceden gör), altın simyası";
                case HeroClass.Archer:
                    return "Keskin göz, hızlı el.\n<color=#3CCB5A>Kritik +%8, Bulgu +%3, Kazı gücü +%15</color>\nYay, arbalet, hançer, mızrak\n" +
                           "Büyüleri: ok yağmuru, izci gözü, kesin kritik, ganimet avı";
                default: return "";
            }
        }

        // ---------------- Büyüler ----------------

        public const string TabSpells = "Büyü Kitabı";
        public const string ManaLabel = "MANA";
        public static string ManaText(double mana, double max) => $"{(int)mana} / {(int)max}";

        public static string SpellName(string id)
        {
            switch (id)
            {
                case "w_cleave": return "Yarma Darbesi";
                case "w_iron_will": return "Demir İrade";
                case "w_shoulder": return "Kaya Omuz";
                case "w_warcry": return "Savaş Narası";
                case "w_greed": return "Yağma Hırsı";
                case "w_titan": return "Titan Kalkanı";
                case "m_fireball": return "Ateş Topu";
                case "m_foresight": return "Kehanet";
                case "m_alchemy": return "Altın Simyası";
                case "m_frost": return "Buz Kalkanı";
                case "m_arcane_eye": return "Arkan Göz";
                case "m_timebend": return "Zamanı Bük";
                case "a_volley": return "Ok Yağmuru";
                case "a_scout": return "İzci Gözü";
                case "a_eagle": return "Kartal Gözü";
                case "a_pathfinder": return "Yol Bulucu";
                case "a_hunt": return "Ganimet Avı";
                case "a_deadeye": return "Ölüm Atışı";
                default: return id;
            }
        }

        /// <summary>Büyünün ne yaptığı (sayılar tanımdan).</summary>
        public static string SpellDescription(SpellDefinition s)
        {
            switch (s.effect)
            {
                case SpellEffect.StrikeMult: return $"Savaş: saldırı gücünün <b>{s.value * 100:0}%</b>'i kadar tek vuruş";
                case SpellEffect.MultiStrike: return $"Savaş: hemen <b>{(int)s.value}</b> ok birden";
                case SpellEffect.Heal: return $"Canının <b>%{Pct(s.value)}</b>'ini yenile";
                case SpellEffect.SafeFloors: return $"Sonraki <b>{s.duration}</b> katta göçük riski <b>-%{Pct(s.value)}</b>";
                case SpellEffect.RewardFloors: return $"Sonraki <b>{s.duration}</b> katın ödülü <b>x{s.value:0.#}</b>";
                case SpellEffect.BankEarnings: return $"Risk altındaki kazancın <b>%{Pct(s.value)}</b>'i hemen cebe (göçükte bile kaybolmaz)";
                case SpellEffect.Foresight: return "Sonraki katın kaderini gör: <b>göçecek mi, güvenli mi?</b>";
                case SpellEffect.FindBoost: return $"Sonraki katta bulgu garantili, nadirliği <b>+{(int)s.value}</b> basamak";
                case SpellEffect.GuaranteedFinds: return $"Sonraki <b>{s.duration}</b> katta bulgu garantili";
                case SpellEffect.Ward: return $"Canavarın sonraki <b>{s.duration}</b> vuruşu <b>-%{Pct(s.value)}</b> hasar";
                case SpellEffect.CritCharges: return $"Sonraki <b>{s.duration}</b> vuruş kesin kritik";
                case SpellEffect.CalmBoss: return $"Savaş: bossun öfkesi sıfırlanır, canının <b>%{Pct(s.value)}</b>'i yenilenir";
                case SpellEffect.LootHunt: return $"Sonraki zaferde en az 1 eşya, nadirliği <b>+{(int)s.value}</b>";
                default: return "";
            }
        }

        public static string SpellWhen(SpellDefinition s) =>
            s.CombatOnly ? "<color=#FF9A2E>savaşta</color>" : s.DigOnly ? "<color=#8FD3FF>kazarken</color>" : "<color=#9AA0B4>her an</color>";

        public static string SpellTooltip(SpellDefinition s, bool known, int hotkey)
        {
            if (!known) return $"<b>???</b>\nSeviye {s.unlockLevel}'de öğrenilir.";
            string key = hotkey > 0 ? $"   <color=#9AA0B4>[{hotkey}]</color>" : "";
            return $"<b>{SpellName(s.id)}</b>{key}\n{SpellDescription(s)}\n<color=#6FA8FF>{(int)s.manaCost} mana</color> · {SpellWhen(s)}";
        }

        public static string SpellBookHeader(HeroClass c, double maxMana, double perFloor, double perKill) =>
            $"<b>{ClassName(c).ToUpper()} BÜYÜ KİTABI</b>\n<size=75%><color=#9AA0B4>Mana {(int)maxMana}. Tura yarı dolu başlarsın; " +
            $"her katta +{(int)perFloor}, her öldürmede +{(int)perKill}. Derine inmek = daha çok büyü.</color></size>";

        public static string SpellBookRow(SpellDefinition s, bool known) =>
            known
                ? $"<b>{SpellName(s.id)}</b>  <color=#6FA8FF>{(int)s.manaCost} mana</color>  {SpellWhen(s)}\n<size=85%>{SpellDescription(s)}</size>"
                : $"<color=#666677><b>???</b>\n<size=85%>Seviye {s.unlockLevel}'de öğrenilir</size></color>";

        public static string SpellsLearned(List<SpellDefinition> spells)
        {
            if (spells == null || spells.Count == 0) return null;
            string text = $"<color=#B080FF>YENİ BÜYÜ!</color>";
            foreach (SpellDefinition s in spells) text += $"\n<b>{SpellName(s.id)}</b>: {SpellDescription(s)}";
            return text;
        }

        public static string ForesightResult(bool bust) =>
            bust ? "<color=#FF3B3B>KEHANET: Sonraki kat GÖÇECEK!</color> Yüzeye dön ya da kaderine meydan oku."
                 : "<color=#3CCB5A>KEHANET: Sonraki kat GÜVENLİ.</color> Kaz!";
        public static string SpellBanked(double value) => $"<color=#3CCB5A>+{Num(value)} cebe aktarıldı</color> (artık güvende)";
        public static string SpellHealed(double value) => $"<color=#3CCB5A>+{Num(value)} can</color>";
        public static string SpellCast(SpellDefinition s) => $"<color=#B080FF>{SpellName(s.id)}!</color> {SpellDescription(s)}";

        /// <summary>Eşya detayında: "Kudret 132" ve kuşanırsan toplamın nasıl değişeceği.</summary>
        public static string KudretCompare(double itemKudret, double totalNow, double totalWith, bool equipped)
        {
            string head = $"Kudret <b>{Num(itemKudret)}</b>";
            if (equipped) return head + $"   (toplam {Num(totalNow)})";
            double diff = totalWith - totalNow;
            if (Math.Abs(diff) < 0.5) return head + $"   toplam {Num(totalNow)} (değişmez)";
            string color = diff > 0 ? "#3CCB5A" : "#FF5A4A";
            string arrow = diff > 0 ? "▲" : "▼";
            return head + $"   toplam {Num(totalNow)} → <color={color}>{Num(totalWith)} {arrow}{(diff > 0 ? "+" : "")}{Num(diff)}</color>";
        }

        // ---------------- Risk göstergesi (Cesaret terazisi) ----------------
        // Risk tek bir büyük sayıyla anlatılır: sonraki kata GEÇME şansı. Yanında yorum yok.

        private static readonly string[] NearMissTitles = { "KIL PAYI!", "AZ KALSIN!", "TAVAN İNLEDİ!", "ŞANSLI GÜN!" };
        public static string NearMissTitle() => Pick(NearMissTitles);
        public static string NearMiss() => "<color=#FFE03A>Kıl payı!</color> Tavan son anda dayandı.";
        public const string GaugeCombatTitle = "BU SAVAŞ";
        public static string GaugeWin(double win) => $"<size=70%>KAZANIRSAN</size>\n<color=#3CCB5A><b>+{Num(win)}</b></color>";

        /// <summary>Cesaret ödülü yüzde olarak: çarpan 1.69 -> "+%69" (1,xx yerine tatmin edici büyük sayı).</summary>
        public static string CourageBonus(double mult) => $"+%{(mult - 1.0) * 100.0:0}";

        /// <summary>
        /// Turun TOPLAM risk bonusu (terazinin solunda, büyük altın): "RİSK BONUSU +%57". Her geçilen katta artar.
        /// </summary>
        public static string RiskBonusTotal(double totalMult) =>
            totalMult > 1.0005
                ? $"<size=55%><color=#C8B070>RİSK BONUSU</color></size>\n<color=#FFC83A><b>{CourageBonus(totalMult)}</b></color>"
                : "<size=55%><color=#8A8A94>RİSK BONUSU</color></size>\n<color=#8A8A94><b>+%0</b></color>";

        /// <summary>Kazanırsan: bu katın risk bonusu yanında altın "+%12" (tüm kazancın büyüyeceğini göstersin).</summary>
        public static string GaugeWin(double win, double courage) =>
            courage > 1.0
                ? $"<size=70%>KAZANIRSAN</size>\n<color=#3CCB5A><b>+{Num(win)}</b></color>  <color=#FFC83A><b>{CourageBonus(courage)} bonus</b></color>"
                : GaugeWin(win);

        // Kutlama başlıkları: 7 kademe (renkler beyaz → mavi → yeşil → sarı → turuncu → kırmızı → pembe),
        // gösterilen risk büyüdükçe daha görkemli. En güvenli katta bile kısa bir başlık var.
        private static readonly string[][] CourageTitles =
        {
            new[] { "SAĞLAM ADIM!", "İYİ GİDİYOR!", "DEVAM!" },
            new[] { "CESUR ADIM!", "YİĞİTÇE!", "SAĞLAM YÜREK!" },
            new[] { "YÜREKLİ!", "KORKUSUZ!", "DAĞ GİBİ!" },
            new[] { "GÖZÜ PEK!", "ATEŞ GİBİ!", "YILMAZ!" },
            new[] { "GÖZÜ KARA!", "KADER SENDEN YANA!", "ÖLÜMLE DANS!" },
            new[] { "YENİLMEZ!", "KADERE MEYDAN OKUDUN!", "DEMİR YÜREK!" },
            new[] { "EFSANEVİ CESARET!", "İMKANSIZI BAŞARDIN!", "DESTANLIK!" },
        };

        public const int CourageTierCount = 7;

        /// <summary>
        /// Kutlama kademesi 1..7 (gösterilen göçük riskine göre). Bossa giden 9 katta (geçme %88 -> %32)
        /// oyuncu her katta bir üst renge çıkar: 1. kat beyaz, 2 mavi, 3 yeşil, 4 sarı, 5-6 turuncu, 7 kırmızı, 8-9 pembe.
        /// </summary>
        public static int CourageTier(double bustChance)
        {
            if (bustChance >= 0.60) return 7;
            if (bustChance >= 0.50) return 6;
            if (bustChance >= 0.38) return 5;
            if (bustChance >= 0.30) return 4;
            if (bustChance >= 0.22) return 3;
            if (bustChance >= 0.15) return 2;
            return 1;
        }

        public static string CourageTitle(int tier) => Pick(CourageTitles[System.Math.Clamp(tier, 1, CourageTierCount) - 1]);

        public static string CourageMessage(double chance, double gain) =>
            gain > 0
                ? $"<color=#FFC83A>%{Pct(1 - chance)} şansla geçtin!</color> Risk bonusu: <b>+{Num(gain)}</b>"
                : $"<color=#FFE03A>%{Pct(1 - chance)} şansla geçtin!</color>";
        public static string GaugeLose(double lose) => $"<size=70%>KAYBEDERSEN</size>\n<color=#FF4A3A><b>-{Num(lose)}</b></color>";
        public const string GaugeNothingToLose = "<size=70%>KAYBEDERSEN</size>\n<color=#9AA0B4><b>henüz bir şey yok</b></color>";
        public static string GaugeFlee(double value) => $"<size=70%>KAÇARSAN</size>\n<color=#3CCB5A><b>{Num(value)} cebe</b></color>";
        public static string GaugeDie(double lose) => $"<size=70%>ÖLÜRSEN</size>\n<color=#FF4A3A><b>-{Num(lose)}</b></color>";

        /// <summary>KAZ butonu: altında küçük geçme şansı ve cesaret ödülü (basmadan önce bir daha görsün).</summary>
        public static string AdvanceButtonWithRisk(double risk, double courage = 1.0) =>
            courage > 1.0
                ? $"{AdvanceButton}\n<size=40%>geçme %{Pct(1 - risk)}  <color=#FFC83A>{CourageBonus(courage)}</color></size>"
                : $"{AdvanceButton}\n<size=40%>geçme %{Pct(1 - risk)}</size>";

        public static string SwingsCompare(int now, int with) =>
            with == now ? "" : with < now ? $"   Duvar: {now} → <color=#3CCB5A>{with} vuruş</color>" : $"   Duvar: {now} → <color=#FF5A4A>{with} vuruş</color>";

        // ---------------- Yeni Çağ ----------------

        public const string TabPrestige = "Yeni Çağ";
        public static string PrestigeHeader(int age, double relics) => $"<b>{age}. Çağ</b>    Yadigar: <color={Gold}><b>{Num(relics)}</b></color>";
        public static string PrestigeInfo(int minStep, int bestStep, double gain) =>
            bestStep < minStep
                ? $"Yeni Çağ {minStep}. kata ulaşınca açılır (rekor: {bestStep}).\nHer şey sıfırlanır, Yadigar kazanırsın. Kadim yükseltmeler sonsuza kadar kalır."
                : $"Şimdi başlatırsan <color={Gold}>+{Num(gain)} Yadigar</color> kazanırsın.\nPara, yükseltmeler, eşyalar, rekor, geçitler, seviye, yetenekler ve sınıf sıfırlanır (sınıfını yeniden seçersin). Kadim yükseltmeler kalır. Daha derin = çok daha fazla Yadigar.";
        public static string PrestigeButton(double gain) => $"YENİ ÇAĞ BAŞLAT (+{Num(gain)} Yadigar)";
        public const string PrestigeConfirm = "EMİN MİSİN? Her şey sıfırlanacak!\nTekrar bas";
        public static string PrestigeDone(int age, double gained) => $"<color={Gold}>{age}. ÇAĞ BAŞLADI! +{Num(gained)} Yadigar</color>";
        public static string RelicCostButton(double cost) => $"Al: {Num(cost)} Yadigar";

        // ---------------- Ekipman ekranı ----------------
        public const string TabUpgrades = "Yükseltmeler";
        public const string TabEquipment = "Ekipman";
        public const string EquipButton = "Kuşan";
        public const string UnequipButton = "Çıkar";
        public const string SellAllButton = "Çantayı sat";
        public const string SellAllConfirm = "Emin misin? Tekrar bas";
        public static string SoldAll(int count, double value) =>
            count > 0 ? $"{count} eşya satıldı: +{Num(value)}" : "Çanta zaten boş.";
        public const string NoItemSelected = "Bir eşyaya tıkla: statlarını gör, kuşan ya da sat.";
        public static string SellButton(double value) => $"Sat\n<size=75%>+{Num(value)}</size>";
        public const string SellConfirm = "<color=#FF5A4A>Emin misin?</color>\n<size=75%>tekrar bas</size>";
        public static string InventoryTitle(int count, int cap) => $"Envanter ({count}/{cap})";
        /// <summary>"Kısa Kılıç · Kat 24 · Giyme Sv 12" (seviye yetmiyorsa kırmızı).</summary>
        public static string ItemLevelLine(GearItem item, bool equipped, bool meetsLevel)
        {
            string req = item.requiredLevel > 0
                ? (meetsLevel ? $"  ·  Giyme Sv {item.requiredLevel}" : $"  ·  <color=#FF5A4A>Giyme Sv {item.requiredLevel}</color>")
                : "";
            return $"{FantasyTexts.BaseName(item)}  ·  Kat {item.itemLevel}{req}{(equipped ? "  ·  <color=#3CCB5A>KUŞANILDI</color>" : "")}";
        }

        public static string LevelTooLow(int required, int level) =>
            $"<color=#FF5A4A>Seviye {required} gerekir</color> (sen {level}). Çantanda tut, seviye atlayınca giy!";
        public static string Sold(double value) => $"Satıldı: +{Num(value)}";

        // ---------------- Yükseltmeler ----------------
        public static string UpgradeName(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.Earnings: return "Daha iyi kazma";
                case UpgradeType.Safety: return "Destek kirişleri";
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
                case UpgradeType.FindChance: return $"Bulgu şansı +%{Pct(b.findChanceUpgrade.effectPerLevel * level)}";
                case UpgradeType.Luck: return $"Nadirlik +%{Pct(b.luckUpgrade.effectPerLevel * level)}";
                case UpgradeType.FindValue: return $"Bulgu değeri x{RiskMath.FindValueBonus(b, level):0.00}";
                default: return "";
            }
        }

        public static string UpgradeInfo(UpgradeType type, int level, int maxLevel, string effect) =>
            $"<b>{UpgradeName(type)}</b>  (Sv. {level}/{maxLevel})\n{effect}";

        public static string BuyButton(double cost) => $"Al: {Num(cost)}";

        // ---------------- Menüler ----------------

        public const string GameTitle = "BİR KAT <color=#FFB347>DAHA!</color>";   // Oyunun adı: "bir tur daha" döngüsünün ta kendisi
        public const string GameSubtitle = "Ne kadar derine inmeye yüreğin yeter?";
        public static string Version(string v) => $"sürüm {v}";

        public const string MenuContinue = "DEVAM ET";
        public const string MenuNewGame = "YENİ OYUN";
        public const string MenuNewGameConfirm = "EMİN MİSİN? Kayıt silinir\n<size=60%>tekrar bas</size>";
        public const string MenuHowToPlay = "NASIL OYNANIR";
        public const string MenuSettings = "AYARLAR";
        public const string MenuQuit = "ÇIKIŞ";
        public const string MenuResume = "DEVAM";
        public const string MenuMainMenu = "ANA MENÜ";
        public const string MenuMainMenuLocked = "ANA MENÜ\n<size=55%>önce yüzeye dön</size>";
        public const string MenuQuitGame = "OYUNDAN ÇIK";
        public const string PauseTitle = "DURAKLATILDI";
        public const string TopBarMenu = "MENÜ (Esc)";

        public const string SettingsTitle = "AYARLAR";
        public const string SettingsVolume = "Ses";
        public const string SettingsFullscreen = "Tam ekran";
        public const string SettingsShake = "Ekran sarsıntısı";
        public const string SettingsFlash = "Parlak flaşlar";
        public const string SettingsReset = "Kaydı sıfırla";
        public const string SettingsResetConfirm = "<color=#FF5A4A>Emin misin? Her şey silinir</color>\n<size=60%>tekrar bas</size>";
        public const string SettingsResetLocked = "Kaydı sıfırla\n<size=60%>tur sürerken olmaz</size>";
        public const string SettingsBack = "GERİ";
        public static string SettingsToggle(string name, bool on) =>
            $"{name}: {(on ? "<color=#3CCB5A>AÇIK</color>" : "<color=#FF5A4A>KAPALI</color>")}";

        public const string HowToPlayNext = "SONRAKİ";
        public const string HowToPlayPrev = "ÖNCEKİ";
        public const string HowToPlayDone = "ANLADIM!";
        public const string HowToPlayClose = "KAPAT";

        /// <summary>Nasıl oynanır sayfaları (başlık, metin). Kısa tutuldu: oyuncu okusun, sonra oynayarak öğrensin.</summary>
        public static readonly (string title, string body)[] HowToPlayPages =
        {
            ("KAZ, AMA NE ZAMAN DURACAĞINI BİL",
             "Her kat altın verir ama bir sonraki katta tavan çökebilir.\n\n" +
             "<b>↓ / S / Boşluk</b> ya da duvara tıkla: kaz (basılı tutunca art arda vurur)\n" +
             "<b>↑ / W</b> ya da <b>Yüzeye dön</b>: turun kazancını cebine at\n\n" +
             "Göçük olursa o turda biriken her şey gider. Kararı sen verirsin."),
            ("GEÇME ŞANSI",
             "Ekranın altındaki büyük sayı, bir sonraki kata <b>geçme şansın</b>.\n\n" +
             "Derine indikçe bu sayı düşer. %90'da rahat kazarsın, %40'ta yüreğin ağzına gelir.\n\n" +
             "Ekran kenarı kırmızı atmaya başladıysa tehlike büyüyor demektir."),
            ("RİSK BONUSU",
             "Her geçtiğin kat, o turda biriken TÜM kazancını büyütür.\n" +
             "Geçme şansı ne kadar düşükse bonus o kadar büyük.\n\n" +
             "Soldaki <color=#FFC83A><b>RİSK BONUSU</b></color> sayacı turun toplamını gösterir. Cesaretin ödüllendirilir!"),
            ("CANAVARLAR VE BOSSLAR",
             "Duvarın ardından canavar çıkabilir: kaz tuşu artık <b>SALDIR</b> olur.\n" +
             "Her 10. katta bir bölge bossu bekler. Yenersen nadir ganimet ve o kattan başlama hakkı (geçit) kazanırsın.\n\n" +
             "Canavarın adının rengi zorluğunu gösterir: maviden kırmızıya zorlaşır."),
            ("GÜÇLEN",
             "Yüzeyde kazandığın altınla yükseltme al, düşen eşyaları kuşan.\n" +
             "Seviye atladıkça yeni özellikler, yetenekler ve büyüler açılır.\n\n" +
             "Büyüler <b>1-6</b> tuşlarıyla. Bir eşyanın üstüne gelince ne kadar güçlü olduğunu görürsün (Kudret)."),
            ("HAZIRSIN",
             "Bossu geçemiyorsan bölgeyi tekrar kaz: eşya topla, güçlen, tekrar dene.\n\n" +
             "<b>Esc</b> oyunu duraklatır. Ayarlardan sesi, tam ekranı ve sarsıntıyı değiştirebilirsin.\n\n" +
             "İyi kazılar!"),
        };

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
