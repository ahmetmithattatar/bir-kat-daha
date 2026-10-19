using System;
using RiskLoop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Oto-kazı paneli: büyük aç/kapat butonu + 4 durma kuralı. Her kural butonuna tıklamak değerini değiştirir.
    /// Ayarlar PlayerProgress.autoDig'de durur (kayda girer), değişince Changed olayı yayılır.
    /// </summary>
    public class AutoDigView : MonoBehaviour
    {
        [SerializeField] private Button toggleButton;
        [SerializeField] private Image toggleImage;
        [SerializeField] private TMP_Text toggleLabel;
        [SerializeField] private Button shieldsButton;
        [SerializeField] private TMP_Text shieldsLabel;
        [SerializeField] private Button riskButton;
        [SerializeField] private TMP_Text riskLabel;
        [SerializeField] private Button tierButton;
        [SerializeField] private TMP_Text tierLabel;
        [SerializeField] private Button fightButton;
        [SerializeField] private TMP_Text fightLabel;

        public event Action ToggleClicked;
        public event Action Changed;

        // Risk eşiği tıklandıkça bu değerler arasında döner
        private static readonly int[] RiskSteps = { 10, 15, 20, 25, 30, 40, 50, 60, 75 };

        private static readonly Color OnColor = new Color(0.95f, 0.65f, 0.15f);
        private static readonly Color OffColor = new Color(0.35f, 0.35f, 0.40f);

        private AutoDigSettings _settings;

        private void Awake()
        {
            toggleButton.onClick.AddListener(() => ToggleClicked?.Invoke());
            shieldsButton.onClick.AddListener(() => Modify(s => s.stopWhenShieldsGone = !s.stopWhenShieldsGone));
            fightButton.onClick.AddListener(() => Modify(s => s.autoFightNormal = !s.autoFightNormal));
            riskButton.onClick.AddListener(() => Modify(s => s.stopRiskPercent = NextRisk(s.stopRiskPercent)));
            tierButton.onClick.AddListener(() => Modify(s => s.stopTier = s.stopTier >= 8 ? 2 : s.stopTier + 1));
        }

        public void Show(bool autoOn, AutoDigSettings settings)
        {
            _settings = settings;
            toggleLabel.text = autoOn ? UIStrings.AutoOn : UIStrings.AutoOff;
            toggleImage.color = autoOn ? OnColor : OffColor;
            shieldsLabel.text = UIStrings.AutoShields(settings.stopWhenShieldsGone);
            riskLabel.text = UIStrings.AutoRisk(settings.stopRiskPercent);
            tierLabel.text = UIStrings.AutoTier(settings.stopTier);
            fightLabel.text = UIStrings.AutoFight(settings.autoFightNormal);
        }

        private void Modify(Action<AutoDigSettings> change)
        {
            if (_settings == null) return;
            change(_settings);
            Changed?.Invoke();
        }

        private static int NextRisk(int current)
        {
            foreach (int r in RiskSteps)
            {
                if (r > current) return r;
            }
            return RiskSteps[0];
        }
    }
}
