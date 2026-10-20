using System;
using RiskLoop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>Yükseltme ekranındaki tek bir satır: ad, seviye, etki ve "Al" butonu. Kilitliyse "Seviye X'te açılır".</summary>
    public class UpgradeRowView : MonoBehaviour
    {
        [SerializeField] private UpgradeType upgradeType;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text buyButtonText;

        public event Action<UpgradeType> BuyClicked;

        private void Awake()
        {
            buyButton.onClick.AddListener(() => BuyClicked?.Invoke(upgradeType));
        }

        public void Refresh(GameSession session)
        {
            // Henüz açılmamış yükseltme: adı görünür (merak), etkisi ve butonu kilitli
            Feature? feature = GameSession.UpgradeFeatureOf(upgradeType);
            if (feature.HasValue && !session.IsUnlocked(feature.Value))
            {
                int unlock = session.UnlockLevel(feature.Value);
                infoText.text = $"<color=#777788><b>{UIStrings.UpgradeName(upgradeType)}</b>\n{UIStrings.FeatureHint(feature.Value)}</color>";
                buyButtonText.text = UIStrings.LockedButton(unlock);
                buyButton.interactable = false;
                return;
            }

            int level = session.GetUpgradeLevel(upgradeType);
            int maxLevel = session.Balance.GetUpgrade(upgradeType).maxLevel;
            bool maxed = session.IsUpgradeMaxed(upgradeType);

            string effect = UIStrings.UpgradeEffect(session.Balance, upgradeType, level, maxed);
            infoText.text = UIStrings.UpgradeInfo(upgradeType, level, maxLevel, effect);

            buyButtonText.text = maxed ? UIStrings.MaxedButton : UIStrings.BuyButton(session.GetUpgradeCost(upgradeType));
            buyButton.interactable = session.CanBuyUpgrade(upgradeType);
        }
    }
}
