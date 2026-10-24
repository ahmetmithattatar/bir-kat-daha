using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Ayarlar: ses seviyesi (kaydırıcı), tam ekran, ekran sarsıntısı, parlak flaşlar, kaydı sıfırla (iki basış), Geri.
    /// Değerler anında uygulanır ve GameSettings ile saklanır.
    /// </summary>
    public class SettingsView : MenuPanel
    {
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Text volumeValue;
        [SerializeField] private Button fullscreenButton;
        [SerializeField] private TMP_Text fullscreenLabel;
        [SerializeField] private Button shakeButton;
        [SerializeField] private TMP_Text shakeLabel;
        [SerializeField] private Button flashButton;
        [SerializeField] private TMP_Text flashLabel;
        [SerializeField] private Button resetButton;
        [SerializeField] private TMP_Text resetLabel;
        [SerializeField] private Button backButton;
        [SerializeField] private float confirmSeconds = 3f;

        public event Action BackClicked;
        public event Action ResetConfirmed;

        private float _confirmUntil = -1f;
        private bool _resetAllowed = true;

        protected override void Awake()
        {
            base.Awake();
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.onValueChanged.AddListener(v =>
            {
                GameSettings.Volume = v;
                volumeValue.text = $"%{Mathf.RoundToInt(v * 100)}";
            });
            fullscreenButton.onClick.AddListener(() => { GameSettings.Fullscreen = !GameSettings.Fullscreen; RefreshLabels(); });
            shakeButton.onClick.AddListener(() => { GameSettings.ScreenShake = !GameSettings.ScreenShake; RefreshLabels(); });
            flashButton.onClick.AddListener(() => { GameSettings.Flashes = !GameSettings.Flashes; RefreshLabels(); });
            resetButton.onClick.AddListener(OnReset);
            backButton.onClick.AddListener(() => BackClicked?.Invoke());
        }

        private void Update()
        {
            if (_confirmUntil > 0f && Time.unscaledTime > _confirmUntil)
            {
                _confirmUntil = -1f;
                resetLabel.text = UIStrings.SettingsReset;
            }
        }

        /// <summary>Aç. resetAllowed: tur sürerken kayıt sıfırlanamaz.</summary>
        public void Open(bool resetAllowed)
        {
            _resetAllowed = resetAllowed;
            volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
            volumeValue.text = $"%{Mathf.RoundToInt(GameSettings.Volume * 100)}";
            RefreshLabels();
            Open();
        }

        private void RefreshLabels()
        {
            fullscreenLabel.text = UIStrings.SettingsToggle(UIStrings.SettingsFullscreen, GameSettings.Fullscreen);
            shakeLabel.text = UIStrings.SettingsToggle(UIStrings.SettingsShake, GameSettings.ScreenShake);
            flashLabel.text = UIStrings.SettingsToggle(UIStrings.SettingsFlash, GameSettings.Flashes);
            resetButton.interactable = _resetAllowed;
            resetLabel.text = _resetAllowed ? UIStrings.SettingsReset : UIStrings.SettingsResetLocked;
        }

        /// <summary>Kaydı sıfırla: iki basış (3 sn içinde).</summary>
        private void OnReset()
        {
            if (_confirmUntil < 0f)
            {
                _confirmUntil = Time.unscaledTime + confirmSeconds;
                resetLabel.text = UIStrings.SettingsResetConfirm;
                return;
            }
            _confirmUntil = -1f;
            resetLabel.text = UIStrings.SettingsReset;
            ResetConfirmed?.Invoke();
        }
    }
}
