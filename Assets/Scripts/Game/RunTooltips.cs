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

        // Kilitli özellikler ipuçlarında anılmaz: açıldığında oyuncu "yeni bir şey geldi" hissini yaşasın.
        private static string If(GameSession s, Feature f, string text) => s.IsUnlocked(f) ? text : null;

        /// <summary>"a, b ve c" (boş olanları atlar).</summary>
        private static string List(params string[] parts)
        {
            var items = new System.Collections.Generic.List<string>();
            foreach (string p in parts) if (!string.IsNullOrEmpty(p)) items.Add(p);
            if (items.Count <= 1) return items.Count == 1 ? items[0] : "";
            return string.Join(", ", items.GetRange(0, items.Count - 1)) + " ve " + items[items.Count - 1];
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

        public static string Kudret(GameSession s) =>
            $"<b>Kudret: {UIStrings.Num(s.Kudret)}</b>\nKuşandığın eşyaların toplam gücü. Derinden düşen, nadir, iyi malzemeli ve " +
            $"güçlendirilmiş eşya daha çok Kudret verir.\nSavaş gücün: {s.CurrentPowerLevel}. kat seviyesi\n" + FantasyTexts.CharacterSummary(s.Stats);

        public static string Threat(GameSession s) =>
            $"<b>Tehdit: {ThreatShort(s.CurrentThreat)}</b>\n{(s.InCombat ? "Bu savaşı" : "Sonraki kattaki canavarı")} öldürene kadar canının ne kadarını kaybedeceğinin tahmini.\n" +
            "Kolay < %20, Orta < %50, Zor < %100, Ölümcül: büyük ihtimalle ölürsün.";

        public static string Insurance(GameSession s) =>
            $"<b>Sigorta: %{UIStrings.Pct(s.InsuranceFraction)}</b>\nGöçükte ya da yenilgide risk altındaki kazancın bu kadarı kurtarılır.";

        public static string Luck(GameSession s)
        {
            string pity = s.PityLuck > 0 ? $"\nTalihsizlik telafisi: <b>+%{UIStrings.Pct(s.PityLuck)}</b> (göçük/yenilgilerden, başarılı dönüşte sıfırlanır)" : "";
            return $"<b>Nadirlik şansı: +%{UIStrings.Pct(s.LuckBonus)}</b>\nNadir bulgu ve eşya ihtimalini artırır. Derinlik de ayrıca ekler.\n" +
                   $"Kaynaklar: {List(If(s, Feature.Luck, "Uğurlu fener"), If(s, Feature.Equipment, "ekipman"), "Uğur tılsımı", If(s, Feature.Talents, "yetenekler"), If(s, Feature.Prestige, "Yıldızların Lütfu"))}." + pity;
        }

        public static string Gamble(GameSession s)
        {
            s.NextGamble(out double win, out double lose, out double chance);
            return $"<b>Sonraki iniş kumarı</b>\nKazanırsan <b>+{UIStrings.Num(win)}</b> (%{UIStrings.Pct(1 - chance)})\n" +
                   $"Kaybedersen <b>{UIStrings.Num(lose)}</b> (%{UIStrings.Pct(chance)}) ve tur biter{(s.IsUnlocked(Feature.Insurance) ? " (sigorta bir kısmını kurtarır)" : "")}\n" +
                   $"Beklenen değer: {UIStrings.Num(s.NextGambleExpectedValue())}";
        }

        public static string Buffs(GameSession s) =>
            "<b>Bu turun etkileri</b>\nBulgulardan gelen geçici güçler: " +
            List("Zengin damar (kat ödülü +%)", "Sağlam kaya (risk azalır)", "Uğur tılsımı (nadirlik)",
                 If(s, Feature.SpecialFinds, "Lanetli put (risk artar)"), If(s, Feature.SpecialFinds, "Eski harita (garantili bulgu katları)")) +
            ". Tur bitince sıfırlanır.";

        // ---------------- Kısa satırlar ----------------

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

    }
}
