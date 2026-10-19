using System;
using RiskLoop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Tur ekranı: karakter + derinlik + turun değeri, 12 stat kartı (açıklamaları fareyle), risk çubuğu,
    /// kısa kumar / etki / keşif satırları, bulgu koleksiyonu, oto-kazı ve butonlar.
    /// Sadece GÖSTERİR ve tıklamaları haber verir. Karar vermez, oyun kuralı bilmez.
    /// </summary>
    public class RunPanelView : MonoBehaviour
    {
        // Kartların sırası (sahne kurucu da bu sırayla 12 kart üretir)
        private const int ChipRisk = 0, ChipShields = 1, ChipPower = 2, ChipThreat = 3, ChipFloors = 4, ChipReward = 5,
                          ChipFind = 6, ChipMonster = 7, ChipInsurance = 8, ChipDig = 9, ChipLuck = 10, ChipAttack = 11;
        public const int ChipCount = 12;

        [Header("Üst")]
        [SerializeField] private AvatarView avatar;
        [SerializeField] private TMP_Text depthText;
        [SerializeField] private TMP_Text runValueText;       // Sayarak artar (count-up)
        [SerializeField] private TMP_Text runValueBreakdownText;

        [Header("Stat kartları")]
        [SerializeField] private StatChipView[] chips;

        [Header("Kısa satırlar (açıklamaları fareyle)")]
        [SerializeField] private TMP_Text gambleText;
        [SerializeField] private TooltipTrigger gambleTip;
        [SerializeField] private TMP_Text buffsText;
        [SerializeField] private TooltipTrigger buffsTip;
        [SerializeField] private TMP_Text milestoneText;
        [SerializeField] private TooltipTrigger milestoneTip;

        [Header("Diğer metinler")]
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text keyHintText;       // Kazıda ve savaşta farklı
        [SerializeField] private TMP_Text advanceLabel;      // "KAZ!" ya da "SALDIR!"
        [SerializeField] private TMP_Text cashOutLabel;      // "Yüzeye dön" ya da "Kaç ve yüzeye dön"

        [Header("Risk çubuğu")]
        [SerializeField] private RectTransform riskBarFill;  // Genişliği risk yüzdesine göre ayarlanır
        [SerializeField] private Image riskBarFillImage;     // Rengi riskle yeşilden kırmızıya döner
        [SerializeField] private float riskForFullRed = 0.5f; // Bu riskte çubuk tamamen kırmızı

        [Header("Bulgular")]
        [SerializeField] private FindCollectionView collection;

        [Header("Kazı sahnesi")]
        [SerializeField] private DigView digView;   // Sahneye tıklamak da kazmayı vurur

        [Header("Oto-kazı")]
        [SerializeField] private AutoDigView autoDig;

        [Header("Butonlar")]
        [SerializeField] private Button advanceButton;
        [SerializeField] private Button cashOutButton;

        // GameController bu olaylara abone olur. (Python'daki callback listesi gibi düşün.)
        public event Action AdvanceClicked;
        public event Action CashOutClicked;
        public event Action AutoToggleClicked;
        public event Action AutoSettingsChanged;

        // Efektlerin nereden çıkacağını bilmesi için (uçan yazılar vs.)
        public RectTransform RunValueAnchor => runValueText.rectTransform;
        public RectTransform DepthAnchor => depthText.rectTransform;
        public RectTransform BuffsAnchor => chips[ChipShields].Rect;
        public RectTransform CollectionAnchor => (RectTransform)collection.transform;

        private static readonly Color SafeColor = new Color(0.25f, 0.8f, 0.35f);
        private static readonly Color DangerColor = new Color(0.9f, 0.2f, 0.2f);

        // Sayaç animasyonu: ekrandaki değer hedefe doğru akar
        private double _shownValue;
        private double _targetValue;
        private bool _hasCashOutStep;

        private void Awake()
        {
            advanceButton.onClick.AddListener(() => AdvanceClicked?.Invoke());
            cashOutButton.onClick.AddListener(() => CashOutClicked?.Invoke());
            digView.SwingClicked += () => AdvanceClicked?.Invoke();
            autoDig.ToggleClicked += () => AutoToggleClicked?.Invoke();
            autoDig.Changed += () => AutoSettingsChanged?.Invoke();
        }

        private void Update()
        {
            if (_shownValue == _targetValue) return;

            // Her karede farkın bir kısmını kapat (yumuşak yaklaşma). Yeterince yakınsa eşitle.
            double k = 1.0 - Math.Exp(-10.0 * FxTime.Delta);
            _shownValue += (_targetValue - _shownValue) * k;
            if (Math.Abs(_targetValue - _shownValue) < 1.0) _shownValue = _targetValue;
            runValueText.text = UIStrings.Num(_shownValue);
        }

        /// <summary>
        /// Tüm değerleri oturumdaki güncel değerlerle yeniler.
        /// highlight: koleksiyonda "pat" diye vurgulanacak yeni bulgu (yoksa null).
        /// </summary>
        public void Refresh(GameSession session, FindResult? highlight = null)
        {
            RunState run = session.CurrentRun;
            int step = run != null ? run.Step : 0;
            double risk = session.NextBustChance;
            bool combat = session.InCombat;

            avatar.Refresh(session);
            depthText.text = $"{step}. KAT";
            runValueBreakdownText.text = UIStrings.RunValueBreakdown(run);

            // ---- Stat kartları: kısa değer + renk + açıklama ----
            int shields = run != null ? run.Shields : 0;
            ThreatLevel threat = session.CurrentThreat;
            double find = Math.Min(1.0, session.NextFindChance);
            double monster = Math.Min(1.0, session.NextMonsterChance);

            chips[ChipRisk].Set("RİSK", $"%{UIStrings.Pct(risk)}", RunTooltips.RiskColor(risk), RunTooltips.Risk(session));
            chips[ChipShields].Set("BARET", $"{shields}/{session.Balance.maxShieldsPerRun}",
                                   shields > 0 ? RunTooltips.Warn : RunTooltips.Danger, RunTooltips.Shields(session));
            chips[ChipPower].Set("GÜÇ", $"{session.CurrentPowerLevel}. kat",
                                 session.CurrentPowerLevel >= step + 1 ? RunTooltips.Good : RunTooltips.Bad, RunTooltips.Power(session));
            chips[ChipThreat].Set(combat ? "BU SAVAŞ" : "TEHDİT", RunTooltips.ThreatShort(threat), RunTooltips.ThreatColor(threat), RunTooltips.Threat(session));
            double floors = session.ExpectedFloorsLeft();
            chips[ChipFloors].Set("DAYANMA", floors >= 40 ? "40+ kat" : $"~{floors:0.0} kat",
                                  floors >= 6 ? RunTooltips.Good : floors >= 2.5 ? RunTooltips.Warn : RunTooltips.Danger, RunTooltips.FloorsLeft(session));
            chips[ChipReward].Set("SONRAKİ", $"+{UIStrings.Num(session.NextStepReward)}", RunTooltips.Good, RunTooltips.NextReward(session));
            chips[ChipFind].Set("BULGU", find >= 1 ? "GARANTİ" : $"%{UIStrings.Pct(find)}", find >= 1 ? RunTooltips.Warn : RunTooltips.Info, RunTooltips.FindChance(session));
            chips[ChipMonster].Set("CANAVAR", monster >= 1 ? "BOSS!" : $"%{UIStrings.Pct(monster)}", monster >= 1 ? RunTooltips.Danger : RunTooltips.Neutral, RunTooltips.MonsterChance(session));
            chips[ChipInsurance].Set("SİGORTA", $"%{UIStrings.Pct(session.InsuranceFraction)}", RunTooltips.Info, RunTooltips.Insurance(session));
            chips[ChipDig].Set("DUVAR", $"{session.NextSwingsNeeded} vuruş", RunTooltips.Neutral, RunTooltips.Dig(session));
            chips[ChipLuck].Set("ŞANS", $"+%{UIStrings.Pct(session.LuckBonus)}", session.PityLuck > 0 ? RunTooltips.Info : RunTooltips.Neutral, RunTooltips.Luck(session));
            chips[ChipAttack].Set("SALDIRI", UIStrings.Num(session.Stats.Attack), RunTooltips.Neutral, RunTooltips.Combat(session));

            // ---- Kısa satırlar ----
            gambleText.text = RunTooltips.GambleShort(session);
            gambleTip.SetText(RunTooltips.Gamble(session));
            buffsText.text = RunTooltips.BuffsShort(run);
            buffsTip.SetText(RunTooltips.Buffs(session));
            milestoneText.text = RunTooltips.MilestoneShort(session, step);
            milestoneTip.SetText(RunTooltips.Milestone(session));

            // Savaş durumuna göre etiketler
            keyHintText.text = combat ? UIStrings.KeyHintCombat : UIStrings.KeyHintRun;
            advanceLabel.text = combat ? UIStrings.AttackButton : UIStrings.AdvanceButton;
            cashOutLabel.text = combat ? UIStrings.FleeButton : UIStrings.CashOutButton;

            // Turun değeri: yeni turda sıfırdan başla, sonra sayarak art
            double value = run != null ? run.TotalValue : 0;
            if (run == null || step == run.StartStep) _shownValue = value;
            _targetValue = value;
            runValueText.text = UIStrings.Num(_shownValue);

            // Risk çubuğu: dolu kısım = risk yüzdesi
            riskBarFill.anchorMax = new Vector2((float)risk, 1f);
            riskBarFillImage.color = Color.Lerp(SafeColor, DangerColor, (float)risk / riskForFullRed);

            collection.Refresh(run, highlight);

            // İlk inişten önce bozdurmanın anlamı yok (kazanç 0)
            _hasCashOutStep = run != null && step > run.StartStep;
            cashOutButton.interactable = _hasCashOutStep && advanceButton.interactable;
        }

        public void ShowMessage(string message)
        {
            messageText.text = message;
        }

        public void SetAuto(bool on, AutoDigSettings settings)
        {
            autoDig.Show(on, settings);
        }

        /// <summary>Animasyon oynarken butonlar kilitlenir (spam önleme).</summary>
        public void SetButtonsInteractable(bool interactable)
        {
            advanceButton.interactable = interactable;
            cashOutButton.interactable = interactable && _hasCashOutStep;
        }
    }
}
