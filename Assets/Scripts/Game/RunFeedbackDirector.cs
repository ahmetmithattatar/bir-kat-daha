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
    ///   baret kırıldıysa -> baret sahnesi + pencere
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

        [Header("Kazı")]
        [Tooltip("Son vuruştan sonra duvarın titreme süresi: taban + risk * ek (gerilim).")]
        [SerializeField] private float trembleBase = 0.12f;
        [SerializeField] private float trembleRiskExtra = 0.7f;

        [Header("Bulgular")]
        [Tooltip("Bu nadirlik ve üstü (3 = lacivert) açılır pencerede gösterilir. Altı koleksiyona uçar.")]
        [SerializeField] private int popupMinTier = 3;

        private static readonly Color Gold = new Color(1f, 0.88f, 0.23f);
        private static readonly Color DangerRed = new Color(0.9f, 0.15f, 0.1f);
        private static readonly Color CashGreen = new Color(0.24f, 0.8f, 0.35f);
        private static readonly Color CritYellow = new Color(1f, 0.85f, 0.2f);

        private int _hits;               // Bu duvara kaç vuruş yapıldı
        private int _swingsNeeded = 3;   // Bu duvar kaç vuruşta yıkılır (kazı gücü / duvar sertliği)
        private bool _bestRunAnnounced;  // "En değerli turun" bu turda zaten duyuruldu mu?

        /// <summary>Oto-kazı açıkken: sıradan anlarda pencere açılmaz, sadece durma koşulundaki anlarda.</summary>
        public bool AutoMode { get; set; }

        /// <summary>Normal animasyon hızı (oto-kazıda 2.5). Hit-stop sırasında geçici olarak neredeyse durur.</summary>
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

        /// <summary>Oto-kazıda bu nadirlik ve üstü pencere açar (ve oto-kazıyı durdurur).</summary>
        public int AutoStopTier { get; set; } = 4;

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

            // Baret göçüğü yuttu: sahnede baret taşları karşılar ve kırılır, sonra durduran pencere
            if (r.ShieldUsed)
            {
                fx.Flash(DangerRed, 0.4f, 0.6f);
                if (r.ShieldLoss > 0) fx.FloatText($"-{UIStrings.Num(r.ShieldLoss)}", DangerRed, 52f, runPanel.RunValueAnchor);
                runPanel.ShowMessage(UIStrings.ShieldLoss(r.ShieldLoss));
                yield return digView.ShieldSave();
                // Oto-kazıda baret kalmışsa durdurmaz (banner yeter); son baret giderse her zaman pencere
                if (!AutoMode || run.Shields == 0) yield return popup.ShowShieldBreak(run.Shields, fx);
                else fx.Banner($"{UIStrings.ShieldBreakHeader} {UIStrings.ShieldsLeft(run.Shields)}", Gold, 0.4f);
                StartCoroutine(UIFx.Punch(runPanel.BuffsAnchor, 0.3f, 0.4f));
            }

            // Duvar yıkılır
            yield return digView.Shatter();
            StartCoroutine(UIFx.Punch(runPanel.DepthAnchor, 0.15f, 0.25f));
            runPanel.ShowMessage(UIStrings.StepMessage(r));
            fx.FloatText($"+{UIStrings.Num(r.RewardGained)}", RewardColor(r.RewardGained, valueBefore), 46f, runPanel.RunValueAnchor);
            fx.CoinsTo(digView.WallAnchor, runPanel.RunValueAnchor, 6, Gold);   // Altınlar sayaca uçar
            if (r.HealthRegen > 0) digView.SetPlayerHealth(run.PlayerHealth, run.PlayerMaxHealth);

            // Canavar: savaş başlar, bulgu zafere kalır
            if (r.Encounter != null)
            {
                if (r.IsMilestone) fx.Banner(UIStrings.MilestoneName(r.Step), Gold, 0.6f);
                digView.HideWall();
                runPanel.ShowMessage(UIStrings.MonsterAppears(r.Encounter));
                if (r.Encounter.Kind != MonsterKind.Normal)
                {
                    fx.Banner(FantasyTexts.MonsterKindLabel(r.Encounter.Kind) + "!", DangerRed, 0.7f);
                    fx.Flash(DangerRed, 0.3f, 0.5f);
                }
                yield return digView.ShowMonster(r.Encounter);
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
            yield return digView.Emerge(run.Step, session.NextBustChance);
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

        /// <summary>Canavar vurdu: atılır, ekran kızarır, can barı düşer. Baret kurtardıysa pencere.</summary>
        public IEnumerator PlayMonsterHit(MonsterHitResult hit, MonsterInstance monster, RunState run)
        {
            yield return digView.MonsterLunge(monster);
            fx.Flash(DangerRed, 0.25f, 0.35f);
            fx.FloatText($"-{UIStrings.Num(hit.Damage)}", DangerRed, 44f, digView.PlayerHpAnchor);
            digView.SetPlayerHealth(System.Math.Max(0, run.PlayerHealth), run.PlayerMaxHealth);

            if (hit.ShieldUsed)
            {
                fx.Banner(UIStrings.ShieldRevive, Gold, 0.6f);
                yield return digView.ShieldSave();
                yield return popup.ShowShieldBreak(run.Shields, fx);
                digView.SetPlayerHealth(run.PlayerHealth, run.PlayerMaxHealth);
            }
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

            runPanel.Refresh(session);
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

        /// <summary>Oto-kazı durdu: oyuncunun dikkatini çek (kontrol sende).</summary>
        public void AnnounceAutoStop(string text)
        {
            fx.Banner(text, Gold, 0.7f);
            fx.Flash(Gold, 0.15f, 0.4f);
        }

        // ---------------- Yardımcılar ----------------

        /// <summary>Bulgu delikten fırlar; nadirse/keşif katıysa/ganimetse pencere, değilse uçan yazı; sonra koleksiyona uçar.</summary>
        private IEnumerator RevealFind(FindResult find, int step, bool isMilestone, bool firstReach, string extraHype)
        {
            yield return digView.RevealFind(find);

            // Oto-kazıda sadece durma eşiğindeki bulgular ve keşif katları pencere açar; sıradan anlar akıp geçer
            bool bigMoment = AutoMode
                ? find.Tier >= AutoStopTier || isMilestone
                : find.Tier >= popupMinTier || isMilestone || (find.FromMonster && find.Tier >= 2);
            if (bigMoment)
            {
                string header = UIStrings.FindPopupHeader(find, step, isMilestone, firstReach);
                yield return popup.ShowFind(find, header, extraHype, fx);
            }
            else
            {
                fx.FloatText(UIStrings.FindTitle(find), RarityStyle.TierColor(find.Tier), 34f + find.Tier * 4f, digView.ItemAnchor);
            }
            yield return digView.FlyItemTo(runPanel.CollectionAnchor);
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
