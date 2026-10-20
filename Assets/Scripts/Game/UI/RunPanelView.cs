using System;
using RiskLoop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Tur ekranı (tam ekran kazı sahnesinin ÜSTÜNDE yarı saydam paneller, sahnenin ortası açık kalır):
    ///   sol üst : karakter + derinlik + turun değeri, en fazla 4 sade kart (Kudret, boss / bu savaş, sigorta, telafi), tur etkileri
    ///   alt orta: Kumar terazisi (RiskGaugeView): bir sonraki hamlenin riski ve ödülü büyük
    ///   sağ     : bulgu koleksiyonu, butonlar
    /// Yeni oyuncuyu sayı yağmuruna boğmamak için kartlar ilgili özellik açılınca belirir. Ayrıntılar fareyle (ipucu).
    /// Sadece GÖSTERİR ve tıklamaları haber verir. Karar vermez, oyun kuralı bilmez.
    /// </summary>
    public class RunPanelView : MonoBehaviour
    {
        // Kartların sırası (sahne kurucu da bu sırayla kart üretir)
        private const int ChipKudret = 0, ChipBoss = 1, ChipInsurance = 2, ChipPity = 3;
        public const int ChipCount = 4;

        [Header("Üst")]
        [SerializeField] private AvatarView avatar;
        [SerializeField] private TMP_Text depthText;
        [SerializeField] private TMP_Text runValueText;       // Sayarak artar (count-up)
        [SerializeField] private TMP_Text runValueBreakdownText;

        [Header("Stat kartları")]
        [SerializeField] private StatChipView[] chips;

        [Header("Tur etkileri (açıklaması fareyle)")]
        [SerializeField] private TMP_Text buffsText;
        [SerializeField] private TooltipTrigger buffsTip;

        [Header("Kumar terazisi")]
        [SerializeField] private RiskGaugeView riskGauge;

        [Header("Diğer metinler")]
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text keyHintText;       // Kazıda ve savaşta farklı
        [SerializeField] private TMP_Text advanceLabel;      // "KAZ!" ya da "SALDIR!"
        [SerializeField] private TMP_Text cashOutLabel;      // "Yüzeye dön" ya da "Kaç ve yüzeye dön"

        [Header("Bulgular")]
        [SerializeField] private FindCollectionView collection;

        [Header("Kazı sahnesi")]
        [SerializeField] private DigView digView;   // Sahneye tıklamak da kazmayı vurur

        [Header("Butonlar")]
        [SerializeField] private Button advanceButton;
        [SerializeField] private Button cashOutButton;

        // GameController bu olaylara abone olur. (Python'daki callback listesi gibi düşün.)
        public event Action AdvanceClicked;
        public event Action CashOutClicked;

        // Efektlerin nereden çıkacağını bilmesi için (uçan yazılar vs.)
        public RectTransform RunValueAnchor => runValueText.rectTransform;
        public RectTransform DepthAnchor => depthText.rectTransform;
        public RectTransform CollectionAnchor => (RectTransform)collection.transform;

        // Sayaç animasyonu: ekrandaki değer hedefe doğru akar
        private double _shownValue;
        private double _targetValue;
        private bool _hasCashOutStep;

        private void Awake()
        {
            advanceButton.onClick.AddListener(() => AdvanceClicked?.Invoke());
            cashOutButton.onClick.AddListener(() => CashOutClicked?.Invoke());
            digView.SwingClicked += () => AdvanceClicked?.Invoke();
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
            bool combat = session.InCombat;

            avatar.Refresh(session);
            depthText.text = $"{step}. KAT";
            runValueBreakdownText.text = UIStrings.RunValueBreakdown(run);

            // ---- Sade kartlar: sadece açık olan özellikler, en fazla 4 ----
            bool gear = session.IsUnlocked(Feature.Equipment);
            chips[ChipKudret].gameObject.SetActive(gear);
            if (gear) chips[ChipKudret].Set(UIStrings.KudretLabel, UIStrings.Num(session.Kudret), RunTooltips.Warn, RunTooltips.Kudret(session));

            if (combat)
            {
                ThreatLevel threat = session.CurrentThreat;
                chips[ChipBoss].Set("BU SAVAŞ", RunTooltips.ThreatShort(threat), RunTooltips.ThreatColor(threat), RunTooltips.Threat(session));
            }
            else
            {
                double bossChance = session.ReachNextBossChance();
                chips[ChipBoss].Set($"{RiskMath.NextMilestone(session.Balance, step).step}. KAT BOSS", $"%{UIStrings.Pct(bossChance)}",
                                    bossChance >= 0.6 ? RunTooltips.Good : bossChance >= 0.3 ? RunTooltips.Warn : RunTooltips.Danger, RunTooltips.BossReach(session));
            }

            chips[ChipInsurance].gameObject.SetActive(session.IsUnlocked(Feature.Insurance));
            chips[ChipInsurance].Set("SİGORTA", $"%{UIStrings.Pct(session.InsuranceFraction)}", RunTooltips.Info, RunTooltips.Insurance(session));

            // Talihsizlik telafisi sadece birikmişse görünür (göçükten sonra teselli)
            chips[ChipPity].gameObject.SetActive(session.PityLuck > 0);
            chips[ChipPity].Set("TELAFİ", $"+%{UIStrings.Pct(session.PityLuck)}", RunTooltips.Info, RunTooltips.Luck(session));

            // ---- Tur etkileri: sadece varsa ----
            string buffs = RunTooltips.BuffsShort(run);
            buffsText.gameObject.SetActive(!string.IsNullOrEmpty(buffs));
            buffsText.text = buffs;
            buffsTip.SetText(RunTooltips.Buffs(session));

            riskGauge.Refresh(session);

            // Savaş durumuna göre etiketler. KAZ butonu riski de söyler: basmadan önce bir daha görsün.
            keyHintText.text = combat ? UIStrings.KeyHintCombat : UIStrings.KeyHintRun;
            advanceLabel.text = combat ? UIStrings.AttackButton : UIStrings.AdvanceButtonWithRisk(session.NextBustChance);
            cashOutLabel.text = combat ? UIStrings.FleeButton : UIStrings.CashOutButton;

            // Turun değeri: yeni turda sıfırdan başla, sonra sayarak art
            double value = run != null ? run.TotalValue : 0;
            if (run == null || step == run.StartStep) _shownValue = value;
            _targetValue = value;
            runValueText.text = UIStrings.Num(_shownValue);

            collection.Refresh(run, highlight);

            // İlk inişten önce bozdurmanın anlamı yok (kazanç 0)
            _hasCashOutStep = run != null && step > run.StartStep;
            cashOutButton.interactable = _hasCashOutStep && advanceButton.interactable;
        }

        public void ShowMessage(string message)
        {
            messageText.text = message;
        }

        /// <summary>Animasyon oynarken butonlar kilitlenir (spam önleme).</summary>
        public void SetButtonsInteractable(bool interactable)
        {
            advanceButton.interactable = interactable;
            cashOutButton.interactable = interactable && _hasCashOutStep;
        }
    }
}
