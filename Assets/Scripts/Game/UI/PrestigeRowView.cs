using System;
using RiskLoop.Core;
using RiskLoop.Game.Art;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>Kadim yükseltme satırı: ad, seviye, etki ve "Al: N Yadigar" butonu.</summary>
    public class PrestigeRowView : MonoBehaviour
    {
        [SerializeField] private PrestigeType type;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text buyText;
        [SerializeField] private Image icon;

        public event Action<PrestigeType> BuyClicked;

        private void Awake()
        {
            buyButton.onClick.AddListener(() => BuyClicked?.Invoke(type));
            icon.sprite = IconArt.Prestige(type);
        }

        public void Refresh(GameSession session)
        {
            int level = session.GetPrestigeLevel(type);
            UpgradeDefinition def = session.Balance.prestige.Get(type);
            bool maxed = session.IsPrestigeMaxed(type);

            string now = FantasyTexts.PrestigeEffect(session.Balance.prestige, type, level);
            string effect = maxed ? now : $"{now} → {FantasyTexts.PrestigeEffect(session.Balance.prestige, type, level + 1)}";
            infoText.text = $"<b>{FantasyTexts.PrestigeName(type)}</b>  (Sv. {level}/{def.maxLevel})\n{effect}";

            buyText.text = maxed ? UIStrings.MaxedButton : UIStrings.RelicCostButton(session.GetPrestigeCost(type));
            buyButton.interactable = session.CanBuyPrestige(type);
        }
    }
}
