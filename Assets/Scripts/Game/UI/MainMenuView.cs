using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Ana menü: oyun adı, Devam Et (kayıt varsa), Yeni Oyun (kayıt varsa iki basış: kayıt silinir), Nasıl Oynanır, Ayarlar, Çıkış.
    /// Arkada kazı sahnesinin karanlık bir görüntüsü kalır (perde yarı saydam).
    /// </summary>
    public class MainMenuView : MenuPanel
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text versionText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private TMP_Text newGameLabel;
        [SerializeField] private Button howToPlayButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private float confirmSeconds = 3f;

        public event Action ContinueClicked;
        public event Action NewGameClicked;
        public event Action HowToPlayClicked;
        public event Action SettingsClicked;
        public event Action QuitClicked;

        private bool _hasSave;
        private float _confirmUntil = -1f;

        protected override void Awake()
        {
            base.Awake();
            continueButton.onClick.AddListener(() => ContinueClicked?.Invoke());
            newGameButton.onClick.AddListener(OnNewGame);
            howToPlayButton.onClick.AddListener(() => HowToPlayClicked?.Invoke());
            settingsButton.onClick.AddListener(() => SettingsClicked?.Invoke());
            quitButton.onClick.AddListener(() => QuitClicked?.Invoke());
        }

        private void Update()
        {
            if (_confirmUntil > 0f && Time.unscaledTime > _confirmUntil)
            {
                _confirmUntil = -1f;
                newGameLabel.text = UIStrings.MenuNewGame;
            }
        }

        /// <summary>Menüyü aç. hasSave: kayıt var mı (Devam Et görünür, Yeni Oyun onay ister).</summary>
        public void Open(bool hasSave)
        {
            _hasSave = hasSave;
            _confirmUntil = -1f;
            titleText.text = UIStrings.GameTitle;
            subtitleText.text = UIStrings.GameSubtitle;
            versionText.text = UIStrings.Version(Application.version);
            continueButton.gameObject.SetActive(hasSave);
            newGameLabel.text = UIStrings.MenuNewGame;
            Open();
        }

        /// <summary>Kayıt varsa ilk basış "Emin misin?" der (kayıt silinecek), ikinci basış yeni oyun başlatır.</summary>
        private void OnNewGame()
        {
            if (_hasSave && _confirmUntil < 0f)
            {
                _confirmUntil = Time.unscaledTime + confirmSeconds;
                newGameLabel.text = UIStrings.MenuNewGameConfirm;
                return;
            }
            _confirmUntil = -1f;
            NewGameClicked?.Invoke();
        }
    }
}
