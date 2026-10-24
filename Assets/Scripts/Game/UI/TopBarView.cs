using System;
using RiskLoop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Ekranın en üstü: para, seviye + XP çubuğu, derinlik rekoru ve sağda MENÜ butonu (duraklatma menüsünü açar, Esc ile aynı).
    /// Kaydı sıfırlama Ayarlar menüsüne taşındı (kazı butonlarından ve yanlış tıklamadan uzak).
    /// </summary>
    public class TopBarView : MonoBehaviour
    {
        [SerializeField] private TMP_Text moneyText;
        [SerializeField] private TMP_Text recordText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text xpText;
        [SerializeField] private RectTransform xpBarFill;   // Genişliği sonraki seviyeye ilerleme
        [SerializeField] private Button menuButton;
        [SerializeField] private TMP_Text menuButtonText;

        public event Action MenuClicked;

        /// <summary>Uçan "+XP" yazıları buradan çıkar.</summary>
        public RectTransform LevelAnchor => levelText.rectTransform;
        public RectTransform MoneyAnchor => moneyText.rectTransform;

        private void Awake()
        {
            menuButton.onClick.AddListener(() => MenuClicked?.Invoke());
            menuButtonText.text = UIStrings.TopBarMenu;
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
    }
}
