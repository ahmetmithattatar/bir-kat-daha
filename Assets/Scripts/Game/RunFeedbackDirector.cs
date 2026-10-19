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
        [Tooltip("Bir katı kazmak için kaç vuruş gerekir.")]
        [SerializeField] private int hitsPerFloor = 3;
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
        private bool _bestRunAnnounced;  // "En değerli turun" bu turda zaten duyuruldu mu?

        /// <summary>Son vuruş yapıldı mı? (GameController buna bakıp zarı attırır.)</summary>
        public bool WallBroken => _hits >= hitsPerFloor;

        /// <summary>Yeni tur başlarken çağrılır.</summary>
        public void OnRunStarted(GameSession session)
        {
            _hits = 0;
            _bestRunAnnounced = false;
            digView.ResetForRun(0, session.NextBustChance);
            digView.SetWeapon(session.GetEquipped(GearSlot.Weapon));
            digView.SetPlayerHealth(session.CurrentRun.PlayerHealth, session.CurrentRun.PlayerMaxHealth);
        }

        // ---------------- Kazı ----------------

        /// <summary>Bir kazma vuruşu. Son vuruşsa duvar titrer (sonuç bekleniyor).</summary>
        public IEnumerator PlaySwing(double risk)
        {
            _hits++;
            yield return digView.Swing(_hits, hitsPerFloor);

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
                yield return digView.ShieldSave();
                yield return popup.ShowShieldBreak(run.Shields, fx);
                StartCoroutine(UIFx.Punch(runPanel.BuffsAnchor, 0.3f, 0.4f));
            }

            // Duvar yıkılır
            yield return digView.Shatter();
            StartCoroutine(UIFx.Punch(runPanel.DepthAnchor, 0.15f, 0.25f));
            runPanel.ShowMessage(UIStrings.StepMessage(r));
            fx.FloatText($"+{UIStrings.Num(r.RewardGained)}", RewardColor(r.RewardGained, valueBefore), 46f, runPanel.RunValueAnchor);
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

            yield return digView.MonsterDeath(m);
            runPanel.ShowMessage(UIStrings.Victory(v));
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

        // ---------------- Yardımcılar ----------------

        /// <summary>Bulgu delikten fırlar; nadirse/keşif katıysa/ganimetse pencere, değilse uçan yazı; sonra koleksiyona uçar.</summary>
        private IEnumerator RevealFind(FindResult find, int step, bool isMilestone, bool firstReach, string extraHype)
        {
            yield return digView.RevealFind(find);

            bool bigMoment = find.Tier >= popupMinTier || isMilestone || (find.FromMonster && find.Tier >= 2);
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
