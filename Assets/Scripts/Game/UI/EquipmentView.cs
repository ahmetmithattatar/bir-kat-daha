using System;
using RiskLoop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Ekipman sekmesi: üstte savaş gücü + karakter statları, ortada karakter yuvaları, altta envanter ızgarası.
    /// Bir kutuya tıklamak eşyayı seçer (detay sol sütunda ItemDetailView'da görünür).
    /// "Çantayı sat" iki basış ister (yanlışlıkla her şeyi satmamak için).
    /// </summary>
    public class EquipmentView : MonoBehaviour
    {
        [SerializeField] private AvatarView avatar;   // Giyilen eşyalar karakterin üstünde
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private GearSlotView[] equippedSlots;   // GearSlot enum sırasıyla
        [SerializeField] private TMP_Text inventoryTitle;
        [SerializeField] private GearSlotView[] inventorySlots;
        [SerializeField] private Button sellAllButton;
        [SerializeField] private TMP_Text sellAllLabel;
        [SerializeField] private float confirmSeconds = 3f;

        public event Action<long> ItemSelected;
        public event Action SellAllClicked;

        private float _confirmUntil = -1f;

        private void Awake()
        {
            foreach (GearSlotView slot in equippedSlots) slot.Clicked += uid => ItemSelected?.Invoke(uid);
            foreach (GearSlotView slot in inventorySlots) slot.Clicked += uid => ItemSelected?.Invoke(uid);
            sellAllButton.onClick.AddListener(OnSellAll);
            sellAllLabel.text = UIStrings.SellAllButton;
        }

        private void Update()
        {
            if (_confirmUntil > 0 && Time.unscaledTime > _confirmUntil)
            {
                _confirmUntil = -1f;
                sellAllLabel.text = UIStrings.SellAllButton;
            }
        }

        private void OnSellAll()
        {
            if (_confirmUntil > 0)
            {
                _confirmUntil = -1f;
                sellAllLabel.text = UIStrings.SellAllButton;
                SellAllClicked?.Invoke();
                return;
            }
            _confirmUntil = Time.unscaledTime + confirmSeconds;
            sellAllLabel.text = UIStrings.SellAllConfirm;
        }

        public void Refresh(GameSession session, long selectedUid)
        {
            avatar.Refresh(session);
            statsText.text = $"Savaş gücün: <b>{session.CurrentPowerLevel}. kat</b> seviyesi\n" + FantasyTexts.CharacterSummary(session.Stats);

            var slots = (GearSlot[])Enum.GetValues(typeof(GearSlot));
            for (int i = 0; i < equippedSlots.Length && i < slots.Length; i++)
            {
                GearItem item = session.GetEquipped(slots[i]);
                equippedSlots[i].Set(item, slots[i], item != null && item.uid == selectedUid);
            }

            var inventory = session.Progress.inventory;
            inventoryTitle.text = UIStrings.InventoryTitle(inventory.Count, session.InventoryCap);
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                GearItem item = i < inventory.Count ? inventory[i] : null;
                inventorySlots[i].Set(item, null, item != null && item.uid == selectedUid);
            }
        }
    }
}
