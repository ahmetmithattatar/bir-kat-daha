using RiskLoop.Core;
using RiskLoop.Game.Art;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// "Kumar terazisi": ekranın alt ortasında, bir sonraki hamlenin riskini ve ödülünü BÜYÜK gösterir.
    ///   Kazıda : büyük göçük yüzdesi + renkli çubuk + "KAZANIRSAN +X" | "KAYBEDERSEN -Y" + yorum (Güvenli / Değer / Kumar / Çılgınlık)
    ///   Savaşta: tehdit + "KAÇARSAN +X cebe" | "ÖLÜRSEN -Y"
    /// Ayrıca kalp atışı: risk ve kaybedilecek para büyüdükçe ekran kenarları kırmızı nabız gibi atar, nabız hızlanır.
    /// Oyuncu her basışta neyi riske attığını HİSSEDER: risk/ödül döngüsünün kalbi burası.
    /// Sadece gösterir, oyun kuralı bilmez.
    /// </summary>
    public class RiskGaugeView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;        // Nabızda hafifçe büyür
        [SerializeField] private TMP_Text titleText;         // "GÖÇÜK RİSKİ" / "BU SAVAŞ"
        [SerializeField] private TMP_Text riskText;          // "%18" / "Zor"
        [SerializeField] private TMP_Text verdictText;       // "Değer", "Kumar"...
        [SerializeField] private RectTransform barFill;
        [SerializeField] private Image barFillImage;
        [SerializeField] private TMP_Text winText;
        [SerializeField] private TMP_Text loseText;
        [SerializeField] private Image heartbeat;            // Tam ekran kenar kızarması (tıklamayı engellemez)
        [SerializeField] private TooltipTrigger tooltip;

        [Header("Kalp atışı")]
        [Tooltip("Bu riskin altında nabız yok.")]
        [SerializeField] private float pulseStartRisk = 0.08f;
        [Tooltip("Bu riskte nabız en güçlü.")]
        [SerializeField] private float pulseFullRisk = 0.45f;
        [SerializeField] private float maxPulseAlpha = 0.55f;
        [SerializeField] private float minBpm = 60f;
        [SerializeField] private float maxBpm = 150f;

        private static readonly Color Safe = new Color(0.25f, 0.8f, 0.35f);
        private static readonly Color Warn = new Color(1f, 0.85f, 0.2f);
        private static readonly Color Danger = new Color(0.95f, 0.2f, 0.15f);

        private float _intensity;     // 0..1: nabzın gücü
        private float _bpm = 60f;
        private float _phase;         // Kalp atışı döngüsü (0..1)
        private double _lastRisk = -1;

        private void Awake()
        {
            heartbeat.sprite = PixelArt.Vignette();   // Kenarlar dolu, orta saydam: sadece ekran kenarı kızarır
        }

        private void Update()
        {
            // Kalp atışı: "küt-küt" (iki kısa vuruş), sonra sessizlik
            _phase += Time.unscaledDeltaTime * _bpm / 60f;
            if (_phase > 1f) _phase -= 1f;
            float beat = Beat(_phase);

            Color c = heartbeat.color;
            c.a = _intensity * maxPulseAlpha * beat;
            heartbeat.color = c;

            // Panel de nabızla hafifçe büyür (sadece ciddi riskte)
            root.localScale = Vector3.one * (1f + 0.035f * _intensity * beat);
        }

        /// <summary>Çift vuruşlu nabız eğrisi: 0..0.12 birinci vuruş, 0.2..0.32 ikinci (daha zayıf).</summary>
        private static float Beat(float p)
        {
            float first = Mathf.Clamp01(1f - Mathf.Abs(p - 0.06f) / 0.06f);
            float second = 0.6f * Mathf.Clamp01(1f - Mathf.Abs(p - 0.26f) / 0.06f);
            return Mathf.Max(first, second);
        }

        public void Refresh(GameSession session)
        {
            if (session.CurrentRun == null)
            {
                _intensity = 0f;
                return;
            }
            if (session.InCombat) RefreshCombat(session);
            else RefreshDig(session);
        }

        private void RefreshDig(GameSession session)
        {
            session.NextGamble(out double win, out double lose, out double chance);
            Color color = RiskColor(chance);

            // Büyük sayı GEÇME şansı (%95 = hiç düşünmeden kaz, %55 = kumar). Çubuk da geçme şansı kadar dolu.
            double success = 1.0 - chance;
            titleText.text = UIStrings.GaugeRiskTitleWithOdds(session.CurrentRun.SurvivalOdds);
            riskText.text = $"%{UIStrings.Pct(success)}";
            riskText.color = color;
            barFill.anchorMax = new Vector2(Mathf.Clamp01((float)success), 1f);
            barFillImage.color = color;

            // Cesaret: riskli katı geçersen tüm kazanç katlanır. Çarpan varsa "KAZANIRSAN" altında büyük yazar
            double courage = session.NextCourageMult;
            winText.text = UIStrings.GaugeWin(win, courage);
            loseText.text = lose > 0 ? UIStrings.GaugeLose(lose) : UIStrings.GaugeNothingToLose;
            bool? foresight = session.ForesightSaysBust;
            verdictText.text = foresight.HasValue
                ? (foresight.Value ? "<color=#FF3B3B>KEHANET: GÖÇECEK</color>" : "<color=#3CCB5A>KEHANET: GÜVENLİ</color>")
                : courage > 1.0 ? UIStrings.CourageVerdict(courage)
                : UIStrings.GambleVerdict(session.NextGambleExpectedValue(), win, lose);
            tooltip.SetText(RunTooltips.Gamble(session));

            // Nabız: risk büyüdükçe ve kaybedilecek bir şey varken
            float riskPart = Mathf.InverseLerp(pulseStartRisk, pulseFullRisk, (float)chance);
            _intensity = lose > 0 ? riskPart : 0f;
            _bpm = Mathf.Lerp(minBpm, maxBpm, riskPart);

            // Risk bir öncekinden belirgin arttıysa sayı "pat" diye büyür: oyuncu fark etsin
            if (_lastRisk >= 0 && chance > _lastRisk + 0.015) StartCoroutine(UIFx.Punch(riskText.rectTransform, 0.35f, 0.3f));
            if (courage > 1.0) StartCoroutine(UIFx.Punch(winText.rectTransform, 0.15f, 0.3f));   // Cesaret çarpanı dikkat çeksin
            _lastRisk = chance;
        }

        private void RefreshCombat(GameSession session)
        {
            ThreatLevel threat = session.CurrentThreat;
            Color color = RunTooltips.ThreatColor(threat);
            RunState run = session.CurrentRun;

            titleText.text = UIStrings.GaugeCombatTitle;
            riskText.text = RunTooltips.ThreatShort(threat);
            riskText.color = color;
            barFill.anchorMax = new Vector2((float)(1.0 - run.PlayerHealth / System.Math.Max(run.PlayerMaxHealth, 1.0)), 1f);
            barFillImage.color = color;

            winText.text = UIStrings.GaugeFlee(run.TotalValue);
            loseText.text = UIStrings.GaugeDie(run.Earnings * (1.0 - session.InsuranceFraction));
            verdictText.text = "";
            tooltip.SetText(RunTooltips.Threat(session));

            int t = (int)threat;   // Kolay 0 .. Ölümcül 3
            _intensity = t / 3f;
            _bpm = Mathf.Lerp(minBpm, maxBpm, t / 3f);
            _lastRisk = -1;
        }

        public void Clear()
        {
            _intensity = 0f;
            _lastRisk = -1;
        }

        private static Color RiskColor(double risk) =>
            risk < 0.12 ? Color.Lerp(Safe, Warn, (float)(risk / 0.12)) : Color.Lerp(Warn, Danger, Mathf.Clamp01((float)((risk - 0.12) / 0.3)));
    }
}
