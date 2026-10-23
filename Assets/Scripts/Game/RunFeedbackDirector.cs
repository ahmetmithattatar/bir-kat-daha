using System.Collections;
using RiskLoop.Core;
using RiskLoop.Game.UI;
using UnityEngine;

namespace RiskLoop.Game
{
    /// <summary>
    /// "Hissiyat yönetmeni": oyun mantığının sonucunu alır ve ekranda bir SAHNE olarak oynatır.
    /// Oyun kuralı yok; sadece sıralama, zamanlama ve efekt seçimi.
    ///
    /// Bir kat = birkaç kazma vuruşu. Son vuruşta duvar titrer (riskle uzar), sonra zar sonucu sahnelenir:
    ///   duvar yıkılır -> canavar varsa savaş başlar | yoksa bulgu delikten çıkar -> tünelde ilerlenir
    ///   göçük -> tavan çöker, taşlar yığılır, ekran kararır
    /// Savaş: her vuruş PlayAttack, canavar vurunca PlayMonsterHit, ölünce PlayVictory (ödül + bulgu + ganimet).
    /// </summary>
    public class RunFeedbackDirector : MonoBehaviour
    {
        [SerializeField] private FxLayerView fx;
        [SerializeField] private RevealPopupView popup;
        [SerializeField] private RunPanelView runPanel;
        [SerializeField] private DigView digView;
        [SerializeField] private TopBarView topBar;

        [Header("Kazı")]
        [Tooltip("Son vuruştan sonra duvarın titreme süresi: taban + risk * ek (gerilim).")]
        [SerializeField] private float trembleBase = 0.12f;
        [SerializeField] private float trembleRiskExtra = 0.7f;

        [Header("Bulgular")]
        [Tooltip("Bu nadirlik ve üstü (3 = lacivert) İLK kez görüldüğünde uzun açılışla gösterilir.")]
        [SerializeField] private int popupMinTier = 3;
        [Tooltip("Daha önce görülmüş bu nadirlik ve üstü (5 = sarı) hızlı kartla gösterilir. Altı adıyla uçar.")]
        [SerializeField] private int quickCardMinTier = 5;

        [Header("Risk hissi")]
        [Tooltip("Hayatta kalınan katta zar, gerçek göçük sınırının bu oran kadar üstündeyse (0.35 = sınırın %35 fazlası) \"KIL PAYI!\" gösterilir.")]
        [SerializeField] private float nearMissMargin = 0.35f;

        private static readonly Color Gold = new Color(1f, 0.88f, 0.23f);
        private static readonly Color DangerRed = new Color(0.9f, 0.15f, 0.1f);
        private static readonly Color CashGreen = new Color(0.24f, 0.8f, 0.35f);
        private static readonly Color CritYellow = new Color(1f, 0.85f, 0.2f);
        private static readonly Color XpBlue = new Color(0.55f, 0.85f, 1f);
        private static readonly Color SpellPurple = new Color(0.7f, 0.45f, 1f);
        private static readonly Color CourageOrange = new Color(1f, 0.55f, 0.15f);

        private int _hits;               // Bu duvara kaç vuruş yapıldı
        private int _swingsNeeded = 3;   // Bu duvar kaç vuruşta yıkılır (kazı gücü / duvar sertliği)
        private bool _bestRunAnnounced;  // "En değerli turun" bu turda zaten duyuruldu mu?

        /// <summary>Normal animasyon hızı. Hit-stop sırasında geçici olarak neredeyse durur.</summary>
        public float BaseSpeed { get; set; } = 1f;

        private float _hitStopUntil;   // Bu zamana kadar animasyonlar donuk (vuruş hissi)

        private void Update()
        {
            // Animasyon hızını tek yerden yönet: hit-stop varsa neredeyse dur, yoksa temel hız
            FxTime.Speed = Time.unscaledTime < _hitStopUntil ? 0.03f : BaseSpeed;
        }

        /// <summary>Kısa donma: kritik vuruşta ve öldürmede "tok" bir his verir.</summary>
        private void HitStop(float seconds)
        {
            _hitStopUntil = Mathf.Max(_hitStopUntil, Time.unscaledTime + seconds);
        }

        /// <summary>Son vuruş yapıldı mı? (GameController buna bakıp zarı attırır.)</summary>
        public bool WallBroken => _hits >= _swingsNeeded;

        /// <summary>Yeni tur başlarken çağrılır (geçitten başlandıysa o kattan).</summary>
        public void OnRunStarted(GameSession session)
        {
            _hits = 0;
            _bestRunAnnounced = false;
            digView.ResetForRun(session.CurrentRun.Step, session.NextBustChance);
            digView.SetWeapon(session.GetEquipped(GearSlot.Weapon));
            digView.SetPickaxe(session.GetEquipped(GearSlot.Pickaxe));
            digView.SetPlayerHealth(session.CurrentRun.PlayerHealth, session.CurrentRun.PlayerMaxHealth);
            if (session.CurrentRun.StartStep > 0) fx.Banner(UIStrings.MilestoneName(session.CurrentRun.StartStep), Gold, 0.6f);
        }

        // ---------------- Kazı ----------------

        /// <summary>Bir kazma vuruşu. swingsNeeded: bu duvarın gerektirdiği vuruş. Son vuruşsa duvar titrer.</summary>
        public IEnumerator PlaySwing(double risk, int swingsNeeded)
        {
            if (_hits == 0) _swingsNeeded = Mathf.Max(1, swingsNeeded);
            _hits++;
            yield return digView.Swing(_hits, _swingsNeeded);

            if (WallBroken)
            {
                runPanel.ShowMessage(UIStrings.DigLine(risk));
                yield return digView.Tremble(trembleBase + trembleRiskExtra * (float)risk);
            }
        }

        /// <summary>Hayatta kalındı: baret, duvar yıkımı, ödül, sonra canavar ya da bulgu.</summary>
        public IEnumerator PlayStep(AdvanceResult r, GameSession session, double valueBefore)
        {
            _hits = 0;
            RunState run = session.CurrentRun;

            // Duvar yıkılır
            yield return digView.Shatter();
            StartCoroutine(UIFx.Punch(runPanel.DepthAnchor, 0.15f, 0.25f));
            runPanel.ShowMessage(UIStrings.StepMessage(r));
            fx.FloatText($"+{UIStrings.Num(r.RewardGained)}", RewardColor(r.RewardGained, valueBefore), 46f, runPanel.RunValueAnchor);

            // Kıl payı: zar gerçek göçük sınırına çok yakın geldiyse heyecanı göster
            if (r.TrueBustChance >= 0.05f && r.Roll < r.TrueBustChance * (1f + nearMissMargin))
            {
                fx.Banner(UIStrings.NearMissTitle(), Gold, 0.45f);
                fx.Shake(10f, 0.25f);
                runPanel.ShowMessage(UIStrings.NearMiss());
            }
            fx.CoinsTo(digView.WallAnchor, runPanel.RunValueAnchor, 6, Gold);   // Altınlar sayaca uçar

            // Riskten sağ çıkma kutlaması: HER katta, gösterilen riskle büyür (ayrıntı metodun yorumunda)
            yield return CelebrateSurvival(r);
            if (r.HealthRegen > 0) digView.SetPlayerHealth(run.PlayerHealth, run.PlayerMaxHealth);

            // Canavar: savaş başlar, bulgu zafere kalır
            if (r.Encounter != null)
            {
                if (r.IsMilestone) fx.Banner(UIStrings.MilestoneName(r.Step), Gold, 0.6f);
                yield return PlayXp(r.XpGained, r.LevelsGained, session);
                digView.HideWall();
                runPanel.ShowMessage(UIStrings.MonsterAppears(r.Encounter));
                if (r.Encounter.Kind != MonsterKind.Normal)
                {
                    fx.Banner(FantasyTexts.MonsterKindLabel(r.Encounter.Kind) + "!", DangerRed, 0.7f);
                    fx.Flash(DangerRed, 0.3f, 0.5f);
                }
                yield return digView.ShowMonster(r.Encounter, session.DifficultyGivesXp(r.Encounter.Difficulty));
                runPanel.Refresh(session);
                yield break;
            }

            // Bulgu delikten çıkar
            if (r.HasFind)
            {
                string extra = r.FirstMilestoneReach ? UIStrings.FirstDiscoveryBonus(r.MilestoneBonus) : null;
                yield return RevealFind(r.Find, r.Step, r.IsMilestone, r.FirstMilestoneReach, extra);
            }
            else if (r.NewRecord)
            {
                fx.FloatText("Yeni rekor!", Gold, 34f, runPanel.DepthAnchor);
            }

            runPanel.Refresh(session, r.HasFind ? r.Find : (FindResult?)null);
            yield return PlayXp(r.XpGained, r.LevelsGained, session);
            yield return digView.Emerge(run.Step, session.NextBustChance);
            if (RiskMath.IsBossFloor(session.Balance, run.Step + 1)) fx.Banner(UIStrings.BossAhead, DangerRed, 0.6f);
            AnnounceBestRun(session);
        }

        // ---------------- Savaş ----------------

        /// <summary>Oyuncunun bir vuruşu: silah sallanır, canavar sarsılır, hasar sayısı uçar.</summary>
        public IEnumerator PlayAttack(AttackResult hit, MonsterInstance monster)
        {
            yield return digView.WeaponSwing();
            digView.MonsterHit(monster, hit.Crit);

            string dmg = UIStrings.Num(Mathf.Max(1f, (float)hit.Damage));
            if (hit.Crit)
            {
                fx.FloatText($"KRİTİK! {dmg}", CritYellow, 58f, digView.MonsterAnchor);
                fx.Shake(8f, 0.12f);
                HitStop(0.05f);
            }
            else
            {
                fx.FloatText(dmg, Color.white, 40f, digView.MonsterAnchor);
            }
        }

        /// <summary>
        /// Büyü sahnesi: mor flaş + büyünün adı, sonra etkisine göre: vuruş (silah iner, büyük hasar sayısı),
        /// kehanet (yeşil GÜVENLİ / kırmızı GÖÇECEK), iyileşme, cebe aktarma, ya da kısa açıklama.
        /// </summary>
        public IEnumerator PlaySpell(SpellResult r, MonsterInstance monster, GameSession session)
        {
            SpellDefinition s = r.Spell;
            fx.Flash(SpellPurple, 0.18f, 0.35f);
            fx.Banner(UIStrings.SpellName(s.id).ToUpper() + "!", SpellPurple, 0.45f);
            fx.Burst(monster != null ? digView.MonsterAnchor : digView.WallAnchor, SpellPurple, 26, 650f);
            runPanel.ShowMessage(UIStrings.SpellCast(s));

            if (r.HasAttack && monster != null)
            {
                yield return digView.WeaponSwing();
                digView.MonsterHit(monster, true);
                string dmg = UIStrings.Num(Mathf.Max(1f, (float)r.TotalDamage));
                string text = r.Hits > 1 ? $"{r.Hits}x  {dmg}!" : $"{dmg}!";
                fx.FloatText(text, CritYellow, 66f, digView.MonsterAnchor);
                fx.Shake(14f, 0.2f);
                HitStop(0.08f);
            }

            switch (s.effect)
            {
                case SpellEffect.Foresight:
                    Color c = r.ForesightBust ? DangerRed : CashGreen;
                    fx.Flash(c, 0.3f, 0.6f);
                    fx.Banner(r.ForesightBust ? "GÖÇECEK!" : "GÜVENLİ!", c, 0.8f);
                    runPanel.ShowMessage(UIStrings.ForesightResult(r.ForesightBust));
                    break;
                case SpellEffect.Heal:
                case SpellEffect.CalmBoss:
                    if (r.Healed > 0) fx.FloatText(UIStrings.SpellHealed(r.Healed), CashGreen, 42f, digView.PlayerHpAnchor);
                    digView.SetPlayerHealth(session.CurrentRun.PlayerHealth, session.CurrentRun.PlayerMaxHealth);
                    break;
                case SpellEffect.BankEarnings:
                    fx.FloatText($"+{UIStrings.Num(r.Banked)}", CashGreen, 52f, runPanel.RunValueAnchor);
                    fx.CoinsTo(runPanel.RunValueAnchor, topBar.MoneyAnchor, 10, Gold);
                    runPanel.ShowMessage(UIStrings.SpellBanked(r.Banked));
                    break;
            }

            runPanel.Refresh(session);
            yield return UIFx.Wait(0.25f);
        }

        /// <summary>Canavar vurdu: atılır, ekran kızarır, can barı düşer. Baret kurtardıysa pencere.</summary>
        public IEnumerator PlayMonsterHit(MonsterHitResult hit, MonsterInstance monster, RunState run)
        {
            yield return digView.MonsterLunge(monster);
            fx.Flash(DangerRed, 0.25f, 0.35f);
            fx.FloatText($"-{UIStrings.Num(hit.Damage)}", DangerRed, 44f, digView.PlayerHpAnchor);
            digView.SetPlayerHealth(System.Math.Max(0, run.PlayerHealth), run.PlayerMaxHealth);
        }

        /// <summary>Canavar öldü: ölüm, ödül, katın bulgusu ve ganimet tek tek açılır, sonra tünelde ilerlenir.</summary>
        public IEnumerator PlayVictory(VictoryResult v, GameSession session)
        {
            RunState run = session.CurrentRun;
            MonsterInstance m = v.Monster;
            bool boss = m.Kind != MonsterKind.Normal;

            HitStop(boss ? 0.15f : 0.07f);
            yield return digView.MonsterDeath(m);
            fx.CoinsTo(digView.MonsterAnchor, runPanel.RunValueAnchor, boss ? 20 : 8, Gold);
            runPanel.ShowMessage(UIStrings.Victory(v) + (v.UnlockedCheckpoint ? "\n" + UIStrings.CheckpointUnlocked(m.Step) : ""));
            if (v.UnlockedCheckpoint)
            {
                fx.Banner("GEÇİT AÇILDI!", Gold, 0.8f);
                fx.Burst(digView.MonsterAnchor, Gold, 40, 800f);
            }
            fx.FloatText($"+{UIStrings.Num(v.Bounty)}", Gold, 50f, runPanel.RunValueAnchor);
            if (boss)
            {
                fx.Banner("ZAFER!", Gold, 0.7f);
                fx.Flash(Gold, 0.3f, 0.6f);
            }

            // Katın bulgusu (keşif katındaysa garantili ve nadir)
            if (v.HasFind)
            {
                bool milestone = m.Milestone != null;
                yield return RevealFind(v.Find, m.Step, milestone, false, null);
            }

            // Ganimet: her eşya tek tek
            foreach (FindResult drop in v.Drops)
            {
                yield return RevealFind(drop, m.Step, false, false, null);
            }

            yield return PlayXp(v.XpGained, v.LevelsGained, session);
            yield return digView.Emerge(run.Step, session.NextBustChance);
            AnnounceBestRun(session);
        }

        /// <summary>Yenilgi: sert sarsıntı, kırmızı ekran, büyük yazı, sahne kararır.</summary>
        public IEnumerator PlayDefeat()
        {
            _hits = 0;
            fx.Shake(40f, 0.8f);
            fx.Flash(DangerRed, 0.7f, 1.0f);
            fx.Banner(UIStrings.DefeatTitle(), DangerRed, 0.9f);
            yield return digView.Collapse();
            digView.HideMonster();
            yield return UIFx.Wait(0.6f);
        }

        // ---------------- Tur sonu ----------------

        /// <summary>Göçük: tavan çöker, sert sarsıntı, kırmızı ekran, büyük yazı.</summary>
        public IEnumerator PlayBust()
        {
            _hits = 0;
            fx.Shake(45f, 0.9f);
            fx.Flash(DangerRed, 0.6f, 1.0f);
            fx.Banner(UIStrings.BustTitle(), DangerRed, 0.9f);
            yield return digView.Collapse();
            yield return UIFx.Wait(0.6f);
        }

        /// <summary>Yüzeye dönüş: kazanılan para uçar, yeşil kutlama.</summary>
        public IEnumerator PlayCashOut(double payout)
        {
            _hits = 0;
            digView.HideMonster();
            fx.FloatText($"+{UIStrings.Num(payout)}", CashGreen, 64f, runPanel.RunValueAnchor);
            fx.Burst(runPanel.RunValueAnchor, CashGreen, 26, 600f);
            fx.Flash(CashGreen, 0.15f, 0.4f);
            yield return UIFx.Wait(0.6f);
        }

        /// <summary>Boss öfkelendi: kırmızı flaş + yazı, öfke çubuğu sıfırlanır.</summary>
        public void PlayEnrage(int stacks)
        {
            fx.Banner(UIStrings.Enrage(stacks), DangerRed, 0.5f);
            fx.Flash(DangerRed, 0.2f, 0.4f);
            fx.Shake(10f, 0.25f);
        }

        /// <summary>Öfke çubuğu: bir sonraki öfkeye ne kadar kaldı.</summary>
        public void SetEnrageProgress(float progress, int stacks)
        {
            digView.SetEnrage(progress, stacks);
        }

        // ---------------- Yardımcılar ----------------

        /// <summary>
        /// Bulgu delikten fırlar, sonra koleksiyona uçar. Gösterim:
        ///   Bu nadirlik İLK kez görülüyor (lacivert+) ya da keşif katı -> uzun açılış (renk tırmanışı, kapatana kadar bekler)
        ///   Daha önce görülmüş nadirlik, sarı+ ya da canavar ganimeti (mavi+) -> hızlı kart (kendiliğinden kapanır)
        ///   Diğerleri -> adıyla uçan yazı
        /// Böylece heyecan ilk seferde yaşanır, üst üste gelen aynı nadirlik oyuncuyu bekletmez.
        /// </summary>
        private IEnumerator RevealFind(FindResult find, int step, bool isMilestone, bool firstReach, string extraHype)
        {
            yield return digView.RevealFind(find);

            bool fullReveal = (find.FirstOfTier && find.Tier >= popupMinTier) || isMilestone;
            bool quickCard = !fullReveal && (find.Tier >= quickCardMinTier || (find.FromMonster && find.Tier >= 2));
            string header = UIStrings.FindPopupHeader(find, step, isMilestone, firstReach);

            if (find.FirstOfTier && find.Tier >= popupMinTier)
            {
                string first = UIStrings.FirstOfTierLine(find.Tier);
                extraHype = string.IsNullOrEmpty(extraHype) ? first : extraHype + "\n" + first;
            }
            if (find.Twin)
            {
                string twin = UIStrings.TwinFindLine;
                extraHype = string.IsNullOrEmpty(extraHype) ? twin : extraHype + "\n" + twin;
            }

            if (fullReveal) yield return popup.ShowFind(find, header, extraHype, fx);
            else if (quickCard) yield return popup.ShowFindQuick(find, header, extraHype, fx);
            else fx.FloatText(UIStrings.FindTitle(find), RarityStyle.TierColor(find.Tier), 34f + find.Tier * 4f, digView.ItemAnchor);

            yield return digView.FlyItemTo(runPanel.CollectionAnchor);
        }

        /// <summary>Kazanılan XP'yi uçur; seviye atlandıysa pencere (yeni özellik açıldıysa okuyana kadar bekler).</summary>
        private IEnumerator PlayXp(double xp, int levelsGained, GameSession session)
        {
            if (xp > 0) fx.FloatText(UIStrings.XpGain(xp), XpBlue, 30f, topBar.LevelAnchor);
            topBar.Refresh(session);
            runPanel.Refresh(session);
            if (levelsGained <= 0) yield break;

            int level = session.Progress.level;
            var unlocked = new System.Collections.Generic.List<Feature>();
            var spells = new System.Collections.Generic.List<SpellDefinition>();
            for (int l = level - levelsGained + 1; l <= level; l++)
            {
                unlocked.AddRange(session.FeaturesUnlockedAt(l));
                spells.AddRange(session.SpellsUnlockedAt(l));
            }

            // Yeni özellik ve yeni büyü aynı pencerede (ikisi de varsa alt alta)
            string news = UIStrings.FeaturesUnlocked(unlocked);
            string newSpells = UIStrings.SpellsLearned(spells);
            if (newSpells != null) news = news == null ? newSpells : news + "\n" + newSpells;

            fx.Banner(UIStrings.LevelUpHeader, XpBlue, 0.6f);
            yield return popup.ShowLevelUp(level, news, session.TalentPointsAvailable, fx);
        }

        /// <summary>
        /// Riskten sağ çıkma kutlaması: HER katta (ilk kattan itibaren), gösterilen riskle sürekli büyür.
        ///   her kat : kıvılcım + "+%X RİSK BONUSU" yazısı toplam sayaca uçar (sayaç da "pat" der)
        ///   %3+     : hafif flaş          %8+  : başlık (CESUR ADIM!)        %20+ : sarsıntı + altın yağmuru
        ///   %32+    : turuncu + donma anı  %45+ : efsanevi (çift flaş, dev patlama, uzun an)
        /// Boss katında risk yok: kutlama da yok.
        /// </summary>
        private IEnumerator CelebrateSurvival(AdvanceResult r)
        {
            float risk = (float)r.BustChance;
            if (risk <= 0f) yield break;
            Color color = risk >= 0.32f ? CourageOrange : Gold;

            // Her kat: kıvılcım ve risk bonusu yazısı (büyüklüğü riskle artar), sayaca doğru
            fx.Burst(digView.WallAnchor, Gold, 6 + Mathf.RoundToInt(70f * risk), 380f + 800f * risk);
            if (r.CourageMult > 1.0)
            {
                fx.FloatText($"+{UIStrings.CourageBonus(r.CourageMult).Substring(1)} RİSK BONUSU", color, 30f + 70f * risk, runPanel.RiskBonusAnchor);
                fx.Burst(runPanel.RiskBonusAnchor, color, 4 + Mathf.RoundToInt(40f * risk), 300f + 500f * risk);
            }
            if (risk >= 0.03f) fx.Flash(color, 0.04f + 0.5f * risk, 0.25f + 0.5f * risk);
            runPanel.ShowMessage(UIStrings.CourageMessage(r.BustChance, r.CourageGain));

            string title = UIStrings.CourageTitle(r.BustChance);
            if (title == null)
            {
                yield return UIFx.Wait(0.05f + 0.6f * risk);
                yield break;
            }
            fx.Banner(title, color, 0.3f + risk);

            if (risk >= 0.2f)
            {
                fx.Shake(6f + 45f * risk, 0.2f + 0.5f * risk);
                fx.CoinsTo(digView.WallAnchor, runPanel.RunValueAnchor, 8 + Mathf.RoundToInt(55f * risk), Gold);
                fx.Burst(runPanel.RunValueAnchor, color, 15 + Mathf.RoundToInt(80f * risk), 700f);
            }
            if (risk >= 0.32f) HitStop(0.08f + 0.25f * risk);   // Zaman bir an durur
            if (risk >= 0.45f)
            {
                // Efsanevi: ikinci flaş ve dev patlama
                yield return UIFx.Wait(0.15f);
                fx.Flash(Color.white, 0.55f, 0.6f);
                fx.Burst(digView.WallAnchor, CourageOrange, 90, 1150f);
                fx.Burst(runPanel.RiskBonusAnchor, Gold, 50, 900f);
                fx.Shake(45f, 0.6f);
            }
            yield return UIFx.Wait(0.1f + 0.8f * risk);   // Anın tadına varsın (risk büyükse daha uzun)
        }

        /// <summary>Bu tur, şimdiye kadarki en değerli turu geçtiyse bir kez duyur.</summary>
        private void AnnounceBestRun(GameSession session)
        {
            double best = session.Progress.bestRunValue;
            if (!_bestRunAnnounced && best > 0 && session.CurrentRun.TotalValue > best)
            {
                _bestRunAnnounced = true;
                fx.Banner("EN DEĞERLİ TURUN!", Gold, 0.7f);
                fx.Burst(runPanel.RunValueAnchor, Gold, 30, 650f);
            }
        }

        /// <summary>
        /// "Kazanç nadirliği": kat ödülü turun o ana kadarki değerine göre büyükse renk yükselir.
        /// Erken katlarda beyaz, turu ikiye katlayan bir kat altın rengi.
        /// </summary>
        private static Color RewardColor(double reward, double valueBefore)
        {
            if (valueBefore <= 0) return Color.white;
            double ratio = reward / valueBefore;
            if (ratio >= 0.5) return Gold;
            if (ratio >= 0.25) return RarityStyle.TierColor(1);
            return Color.white;
        }
    }
}
