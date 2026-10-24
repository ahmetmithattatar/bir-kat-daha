using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Duraklatma menüsü (Esc ya da üst bardaki MENÜ): Devam, Nasıl Oynanır, Ayarlar, Ana Menü, Oyundan Çık.
    /// Açıkken oyun durur (canavar vurmaz, animasyonlar donar). Tur sürerken Ana Menü kilitli: önce yüzeye dönmeli
    /// (tur ortası kaydedilmez, yoksa risk altındaki kazanç kaybolurdu).
    /// </summary>
    public class PauseMenuView : MenuPanel
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button howToPlayButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private TMP_Text mainMenuLabel;
        [SerializeField] private Button quitButton;

        public event Action ResumeClicked;
        public event Action HowToPlayClicked;
        public event Action SettingsClicked;
        public event Action MainMenuClicked;
        public event Action QuitClicked;

        protected override void Awake()
        {
            base.Awake();
            resumeButton.onClick.AddListener(() => ResumeClicked?.Invoke());
            howToPlayButton.onClick.AddListener(() => HowToPlayClicked?.Invoke());
            settingsButton.onClick.AddListener(() => SettingsClicked?.Invoke());
            mainMenuButton.onClick.AddListener(() => MainMenuClicked?.Invoke());
            quitButton.onClick.AddListener(() => QuitClicked?.Invoke());
        }

        /// <summary>Aç. runActive: tur sürüyorsa Ana Menü kilitli (önce yüzeye dön).</summary>
        public void Open(bool runActive)
        {
            mainMenuButton.interactable = !runActive;
            mainMenuLabel.text = runActive ? UIStrings.MenuMainMenuLocked : UIStrings.MenuMainMenu;
            Open();
        }
    }
}
