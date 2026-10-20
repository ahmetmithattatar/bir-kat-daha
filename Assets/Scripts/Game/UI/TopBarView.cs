using System;
using RiskLoop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Ekranın en üstü: para, derinlik rekoru, seviye + XP çubuğu ve (kazı butonlarından uzakta) kaydı sıfırla butonu.
    /// Sıfırlama iki basış ister: ilk basış "Emin misin?" der, birkaç saniye içinde tekrar basılırsa sıfırlar.
    /// </summary>
    public class TopBarView : MonoBehaviour
    {
        [SerializeField] private TMP_Text moneyText;
        [SerializeField] private TMP_Text recordText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text xpText;
        [SerializeField] private RectTransform xpBarFill;   // Genişliği sonraki seviyeye ilerleme
        [SerializeField] private Button resetButton;
        [SerializeField] private TMP_Text resetButtonText;
        [SerializeField] private float confirmWindowSeconds = 3f;

        public event Action ResetConfirmed;

        /// <summary>Uçan "+XP" yazıları buradan çıkar.</summary>
        public RectTransform LevelAnchor => levelText.rectTransform;
        public RectTransform MoneyAnchor => moneyText.rectTransform;

        private float _confirmUntil = -1f;  // Bu zamana kadar ikinci basış sıfırlar

        private void Awake()
        {
            resetButton.onClick.AddListener(OnResetClicked);
            resetButtonText.text = UIStrings.ResetSaveButton;
        }

        private void Update()
        {
            // Süre dolduysa butonu normale döndür
            if (_confirmUntil > 0 && Time.unscaledTime > _confirmUntil) CancelConfirm();
        }

        public void Refresh(GameSession session)
        {
            PlayerProgress progress = session.Progress;
            moneyText.text = UIStrings.Money(progress.money);
            recordText.text = UIStrings.Record(progress.bestStep);
            levelText.text = UIStrings.LevelTitle(progress.level);
            xpText.text = UIStrings.XpLine(progress.xp, session.XpToNextLevel);
            xpBarFill.anchorMax = new Vector2((float)Math.Min(1.0, progress.xp / session.XpToNextLevel), 1f);
        }

        /// <summary>Tur sırasında sıfırlama butonu gizlenir.</summary>
        public void SetResetVisible(bool visible)
        {
            CancelConfirm();
            resetButton.gameObject.SetActive(visible);
        }

        private void OnResetClicked()
        {
            if (_confirmUntil > 0)
            {
                CancelConfirm();
                ResetConfirmed?.Invoke();
                return;
            }

            _confirmUntil = Time.unscaledTime + confirmWindowSeconds;
            resetButtonText.text = UIStrings.ResetSaveConfirm;
        }

        private void CancelConfirm()
        {
            _confirmUntil = -1f;
            resetButtonText.text = UIStrings.ResetSaveButton;
        }
    }
}
