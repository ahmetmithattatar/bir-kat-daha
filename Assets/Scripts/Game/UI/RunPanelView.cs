using System;
using RiskLoop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Tur ekranı: derinlik, turun değeri, güç ve risk göstergeleri, bulgu koleksiyonu, oto-kazı ve butonlar.
    /// Sadece GÖSTERİR ve tıklamaları haber verir. Karar vermez, oyun kuralı bilmez.
    ///
    /// Oyuncunun "devam mı, dön mü?" kararı için üç satır:
    ///   Güç satırı   : savaş gücün kaçıncı kat seviyesinde, sonraki katın tehdidi, duvar kaç vuruş
    ///   Kumar satırı : kazanırsan / kaybedersen / olasılık + yorum (Değer, Kumar, Çılgınlık)
    ///   Dayanma      : göçük hesabıyla ortalama kaç kat daha
    /// </summary>
    public class RunPanelView : MonoBehaviour
    {
        [Header("Metinler")]
        [SerializeField] private TMP_Text depthText;
        [SerializeField] private TMP_Text runValueText;       // Sayarak artar (count-up)
        [SerializeField] private TMP_Text runValueBreakdownText;
        [SerializeField] private TMP_Text buffsText;
        [SerializeField] private TMP_Text riskText;
        [SerializeField] private TMP_Text nextRewardText;
        [SerializeField] private TMP_Text insuranceText;
        [SerializeField] private TMP_Text milestoneText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text combatStatsText;   // Saldırı / Savunma / Kritik
        [SerializeField] private TMP_Text keyHintText;       // Kazıda ve savaşta farklı
        [SerializeField] private TMP_Text advanceLabel;      // "KAZ!" ya da "SALDIR!"
        [SerializeField] private TMP_Text cashOutLabel;      // "Yüzeye dön" ya da "Kaç ve yüzeye dön"

        [Header("Güç ve risk göstergesi")]
        [SerializeField] private TMP_Text powerText;
        [SerializeField] private TMP_Text gambleText;
        [SerializeField] private TMP_Text floorsLeftText;

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
        public RectTransform BuffsAnchor => buffsText.rectTransform;
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
            runValueText.text = UIStrings.RunValue(_shownValue);
        }

        /// <summary>
        /// Tüm metinleri oturumdaki güncel değerlerle yeniler.
        /// highlight: koleksiyonda "pat" diye vurgulanacak yeni bulgu (yoksa null).
        /// </summary>
        public void Refresh(GameSession session, FindResult? highlight = null)
        {
            RunState run = session.CurrentRun;
            int step = run != null ? run.Step : 0;
            double risk = session.NextBustChance;
            bool combat = session.InCombat;

            depthText.text = $"{UIStrings.Depth(step)}   <size=60%>{UIStrings.Multiplier(session.CurrentMultiplier)}</size>";
            runValueBreakdownText.text = UIStrings.RunValueBreakdown(run);
            buffsText.text = UIStrings.RunBuffs(run);
            riskText.text = UIStrings.NextRisk(risk);
            nextRewardText.text = UIStrings.NextReward(session.NextStepReward, session.NextFindChance, session.NextMonsterChance);
            insuranceText.text = UIStrings.Insurance(session.InsuranceFraction);

            // Güç ve risk göstergesi
            powerText.text = $"{UIStrings.PowerLine(session.CurrentPowerLevel, session.CurrentThreat, combat)}   {UIStrings.SwingsLine(session.NextSwingsNeeded)}";
            session.NextGamble(out double win, out double lose, out double chance);
            gambleText.text = UIStrings.Gamble(win, lose, chance, session.NextGambleExpectedValue());
            floorsLeftText.text = UIStrings.FloorsLeft(session.ExpectedFloorsLeft());

            // Savaş durumuna göre etiketler
            combatStatsText.text = UIStrings.CombatSummary(session.Stats);
            keyHintText.text = combat ? UIStrings.KeyHintCombat : UIStrings.KeyHintRun;
            advanceLabel.text = combat ? UIStrings.AttackButton : UIStrings.AdvanceButton;
            cashOutLabel.text = combat ? UIStrings.FleeButton : UIStrings.CashOutButton;

            MilestoneDefinition next = session.NextMilestone;
            bool discovered = next != null && next.step <= session.Progress.bestStep;
            milestoneText.text = UIStrings.NextMilestone(next, step, discovered);

            // Turun değeri: yeni turda sıfırdan başla, sonra sayarak art
            double value = run != null ? run.TotalValue : 0;
            if (run == null || step == run.StartStep) _shownValue = value;
            _targetValue = value;
            runValueText.text = UIStrings.RunValue(_shownValue);

            // Risk çubuğu: dolu kısım = risk yüzdesi
            riskBarFill.anchorMax = new Vector2((float)risk, 1f);
            Color riskColor = Color.Lerp(SafeColor, DangerColor, (float)risk / riskForFullRed);
            riskBarFillImage.color = riskColor;
            riskText.color = riskColor;

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
