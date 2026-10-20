using RiskLoop.Core;
using UnityEngine;

namespace RiskLoop.Game
{
    /// <summary>
    /// Tur ekranındaki stat kartlarının kısa değerleri ve fareyle üstüne gelince çıkan açıklamaları.
    /// Ekranda sadece sayı, ayrıntı ipucunda: oyuncu bir bakışta okur, merak edince öğrenir.
    /// </summary>
    public static class RunTooltips
    {
        public static readonly Color Neutral = new Color(0.92f, 0.92f, 0.95f);
        public static readonly Color Good = new Color(0.25f, 0.8f, 0.35f);
        public static readonly Color Warn = new Color(1f, 0.88f, 0.23f);
        public static readonly Color Bad = new Color(1f, 0.55f, 0.18f);
        public static readonly Color Danger = new Color(1f, 0.25f, 0.2f);
        public static readonly Color Info = new Color(0.56f, 0.83f, 1f);

        public static Color RiskColor(double risk) => risk < 0.12 ? Good : risk < 0.25 ? Warn : risk < 0.4 ? Bad : Danger;

        public static Color ThreatColor(ThreatLevel t)
        {
            switch (t)
            {
                case ThreatLevel.Easy: return Good;
                case ThreatLevel.Medium: return Warn;
                case ThreatLevel.Hard: return Bad;
                default: return Danger;
            }
        }

        public static string ThreatShort(ThreatLevel t)
        {
            switch (t)
            {
                case ThreatLevel.Easy: return "Kolay";
                case ThreatLevel.Medium: return "Orta";
                case ThreatLevel.Hard: return "Zor";
                default: return "ÖLÜMCÜL";
            }
        }

        // ---------------- Açıklamalar ----------------

        public static string Risk(GameSession s)
        {
            int next = (s.CurrentRun != null ? s.CurrentRun.Step : 0) + 1;
            double floor = RiskMath.BustFloor(s.Balance, next);
            return $"<b>Göçük riski</b>\nSonraki inişte göçük olma ihtimali: <b>%{UIStrings.Pct(s.NextBustChance)}</b>.\n" +
                   $"Derine indikçe artar. Destek kirişleri, Sağlam kaya ve ekipmandaki göçük direnci azaltır, " +
                   $"ama bu derinliğin taban riski <b>%{UIStrings.Pct(floor)}</b>'in altına inemez.";
        }

        public static string BossReach(GameSession s)
        {
            MilestoneDefinition m = s.NextMilestone;
            int zone = RiskMath.ZoneOf(s.Balance, m != null ? m.step : 1);
            return $"<b>Bölge bossuna ulaşma: %{UIStrings.Pct(s.ReachNextBossChance())}</b>\n" +
                   $"Göçüğe yakalanmadan {(m != null ? m.step : 0)}. kattaki bossa varma ihtimalin. Her bölge 10 kat, sonunda bir boss.\n" +
                   $"Bölge içinde risk kat kat artar, yeni bölgeye geçince (bossu yenince) {zone + 1}. bölgenin başında biraz düşer. Boss katında göçük olmaz.\n" +
                   "Bossu yenemiyorsan: bu bölgeyi farmla (eşya + para), güçlen, tekrar dene.";
        }

        public static string Power(GameSession s) =>
            $"<b>Savaş gücü: {s.CurrentPowerLevel}. kat seviyesi</b>\nNormal canavarları rahat, bölge bossunu sağ çıkarak yenebileceğin en derin kat.\n" +
            "Daha iyi eşya, güçlendirme ve Kralların Kanı bunu yükseltir. Bosslar uzayan savaşta öfkelenir: hasarın yetmezse takılırsın.";

        public static string Threat(GameSession s) =>
            $"<b>Tehdit: {ThreatShort(s.CurrentThreat)}</b>\n{(s.InCombat ? "Bu savaşı" : "Sonraki kattaki canavarı")} öldürene kadar canının ne kadarını kaybedeceğinin tahmini.\n" +
            "Kolay < %20, Orta < %50, Zor < %100, Ölümcül: büyük ihtimalle ölürsün.";

        public static string FloorsLeft(GameSession s) =>
            $"<b>Dayanma</b>\nGöçük riskiyle bir sonraki boss katına kadar ortalama <b>{s.ExpectedFloorsLeft():0.0}</b> kat daha inebilirsin (canavarlar hariç).\n" +
            "Bu sayı küçüldükçe her iniş daha büyük kumar.";

        public static string NextReward(GameSession s) =>
            $"<b>Sonraki kat ödülü: +{UIStrings.Num(s.NextStepReward)}</b>\nToplam çarpan x{s.CurrentMultiplier:0.00}: " +
            "derinlik, Daha iyi kazma, altın bonusu, Zengin damar ve Kadim Altın.\nÖdül risk altındadır: göçükte gider, yüzeye dönünce cebe girer.";

        public static string FindChance(GameSession s) =>
            $"<b>Bulgu şansı: %{UIStrings.Pct(System.Math.Min(1.0, s.NextFindChance))}</b>\nKatta bir şey bulma ihtimali. Keşif katında ve haritayla garanti.\n" +
            "Maden dedektörü ve ekipmandaki bulgu şansı artırır. Canavarlı katta bulgu zafere kalır.";

        public static string MonsterChance(GameSession s) =>
            $"<b>Canavar şansı: %{UIStrings.Pct(System.Math.Min(1.0, s.NextMonsterChance))}</b>\nDuvarın ardından canavar çıkma ihtimali. Derinlikle artar.\n" +
            "Her 10. katta bölge bossu bekler. Canavar katın hazinesini korur: yenersen bulgu ve ganimet.";

        public static string Insurance(GameSession s) =>
            $"<b>Sigorta: %{UIStrings.Pct(s.InsuranceFraction)}</b>\nGöçükte ya da yenilgide risk altındaki kazancın bu kadarı kurtarılır.";

        public static string Dig(GameSession s) =>
            $"<b>Duvar: {s.NextSwingsNeeded} vuruş</b>\nKazı gücün {s.Stats.DigPower:0.0}. Duvarlar derinlikle sertleşir.\n" +
            "Daha iyi bir kazma bulmak kazıyı gözle görülür hızlandırır.";

        public static string Luck(GameSession s)
        {
            string pity = s.PityLuck > 0 ? $"\nTalihsizlik telafisi: <b>+%{UIStrings.Pct(s.PityLuck)}</b> (göçük/yenilgilerden, başarılı dönüşte sıfırlanır)" : "";
            return $"<b>Nadirlik şansı: +%{UIStrings.Pct(s.LuckBonus)}</b>\nNadir bulgu ve eşya ihtimalini artırır. Derinlik de ayrıca ekler.\n" +
                   "Kaynaklar: Uğurlu fener, ekipman, Uğur tılsımı, Yıldızların Lütfu." + pity;
        }

        public static string Combat(GameSession s) => "<b>Savaş statları</b>\n" + FantasyTexts.CharacterSummary(s.Stats);

        public static string Gamble(GameSession s)
        {
            s.NextGamble(out double win, out double lose, out double chance);
            return $"<b>Sonraki iniş kumarı</b>\nKazanırsan <b>+{UIStrings.Num(win)}</b> (%{UIStrings.Pct(1 - chance)})\n" +
                   $"Kaybedersen <b>{UIStrings.Num(lose)}</b> (%{UIStrings.Pct(chance)}) ve tur biter (sigorta bir kısmını kurtarır)\n" +
                   $"Beklenen değer: {UIStrings.Num(s.NextGambleExpectedValue())}";
        }

        public static string Buffs(GameSession s) =>
            "<b>Bu turun etkileri</b>\nBulgulardan gelen geçici güçler: Zengin damar (kat ödülü +%), Sağlam kaya (risk azalır), " +
            "Lanetli put (risk artar), Uğur tılsımı (nadirlik), Eski harita (garantili bulgu katları). Tur bitince sıfırlanır.";

        public static string Milestone(GameSession s)
        {
            MilestoneDefinition m = s.NextMilestone;
            if (m == null) return "";
            string tier = RarityStyle.Colored(RarityStyle.Name(m.minTier), m.minTier);
            return $"<b>{UIStrings.MilestoneName(m.step)}</b> ({m.step}. kat)\nBölge bossu bekliyor. Yenersen: en az {tier} garantili bulgu, ganimet " +
                   "ve bu kattan başlayabileceğin bir geçit. İlk kez ulaşınca ayrıca keşif ödülü.";
        }

        // ---------------- Kısa satırlar ----------------

        public static string GambleShort(GameSession s)
        {
            s.NextGamble(out double win, out double lose, out double chance);
            string verdict;
            double ev = s.NextGambleExpectedValue();
            if (lose <= 0) verdict = "<color=#3CCB5A>Güvenli</color>";
            else if (ev >= 0) verdict = "<color=#3CCB5A>Değer</color>";
            else if (ev > -win) verdict = "<color=#FFE03A>Kumar</color>";
            else verdict = "<color=#FF3B3B>Çılgınlık</color>";
            return $"<color=#3CCB5A>+{UIStrings.Num(win)}</color>  /  <color=#FF9A2E>-{UIStrings.Num(lose)}</color>  (%{UIStrings.Pct(chance)})   {verdict}";
        }

        public static string BuffsShort(RunState run)
        {
            if (run == null) return "";
            string t = "";
            if (run.ProfitBonus > 0) t += $"Damar +%{UIStrings.Pct(run.ProfitBonus)}  ";
            if (run.SafetyFactor < 1) t += $"Kaya x{run.SafetyFactor:0.00}  ";
            if (run.SafetyFactor > 1) t += $"<color=#FF9A2E>Lanet x{run.SafetyFactor:0.00}</color>  ";
            if (run.RunLuck > 0) t += $"Tılsım +%{UIStrings.Pct(run.RunLuck)}  ";
            if (run.GuaranteedFinds > 0) t += $"<color=#FFE03A>Harita {run.GuaranteedFinds}</color>";
            return t.Length > 0 ? t : "<color=#888899>Etki yok</color>";
        }

        public static string MilestoneShort(GameSession s, int step)
        {
            MilestoneDefinition m = s.NextMilestone;
            if (m == null) return "Tüm keşifler tamam";
            bool discovered = m.step <= s.Progress.bestStep;
            string name = discovered ? UIStrings.MilestoneName(m.step) : "???";
            int left = m.step - step;
            return left == 1 ? $"<color=#FFE03A>SONRAKİ KAT: {name}</color>" : $"Sonraki: <b>{name}</b> ({left} kat)";
        }
    }
}
