using System;
using RiskLoop.Core;
using RiskLoop.Game.Art;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Seçili eşyanın detayı: büyük ikon, ad (nadirlik renginde), yuva/seviye, statlar.
    /// Envanterdeki eşyada statların yanında kuşanılı eşyaya göre fark (+yeşil / -kırmızı) görünür.
    /// Butonlar: Kuşan / Çıkar ve Sat.
    /// </summary>
    public class ItemDetailView : MonoBehaviour
    {
        [SerializeField] private Image frame;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button equipButton;
        [SerializeField] private TMP_Text equipLabel;
        [SerializeField] private Button sellButton;
        [SerializeField] private TMP_Text sellLabel;

        public event Action<long> EquipClicked;
        public event Action<long> UnequipClicked;
        public event Action<long> SellClicked;

        private GearItem _item;
        private bool _equipped;

        private void Awake()
        {
            equipButton.onClick.AddListener(() =>
            {
                if (_item == null) return;
                if (_equipped) UnequipClicked?.Invoke(_item.uid);
                else EquipClicked?.Invoke(_item.uid);
            });
            sellButton.onClick.AddListener(() =>
            {
                if (_item != null && !_equipped) SellClicked?.Invoke(_item.uid);
            });
        }

        public void Show(GameSession session, GearItem item)
        {
            _item = item;
            if (item == null)
            {
                frame.color = new Color(0.28f, 0.28f, 0.33f);
                icon.enabled = false;
                nameText.text = "";
                levelText.text = "";
                statsText.text = UIStrings.NoItemSelected;
                equipButton.gameObject.SetActive(false);
                sellButton.gameObject.SetActive(false);
                return;
            }

            _equipped = session.IsEquipped(item);
            GearItem current = _equipped ? null : session.GetEquipped(item.slot);

            frame.color = RarityStyle.TierColor(item.tier);
            icon.enabled = true;
            icon.sprite = GearArt.Icon(item);
            nameText.text = FantasyTexts.GearName(item);
            levelText.text = UIStrings.ItemLevelLine(item, _equipped);
            statsText.text = FantasyTexts.GearStatLines(item, current);

            equipButton.gameObject.SetActive(true);
            equipLabel.text = _equipped ? UIStrings.UnequipButton : UIStrings.EquipButton;
            sellButton.gameObject.SetActive(!_equipped);
            sellLabel.text = UIStrings.SellButton(session.SellValue(item));
        }

        public void ShowMessage(string message)
        {
            messageText.text = message;
        }
    }
}
