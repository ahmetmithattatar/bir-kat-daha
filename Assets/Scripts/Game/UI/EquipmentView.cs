using System;
using RiskLoop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Ekipman sekmesi: üstte karakter statları, ortada 8 karakter yuvası, altta envanter ızgarası.
    /// Bir kutuya tıklamak eşyayı seçer (detay sol sütunda ItemDetailView'da görünür).
    /// </summary>
    public class EquipmentView : MonoBehaviour
    {
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private GearSlotView[] equippedSlots;   // GearSlot enum sırasıyla
        [SerializeField] private TMP_Text inventoryTitle;
        [SerializeField] private GearSlotView[] inventorySlots;
        [SerializeField] private Button autoEquipButton;

        public event Action<long> ItemSelected;
        public event Action AutoEquipClicked;

        private void Awake()
        {
            foreach (GearSlotView slot in equippedSlots) slot.Clicked += uid => ItemSelected?.Invoke(uid);
            foreach (GearSlotView slot in inventorySlots) slot.Clicked += uid => ItemSelected?.Invoke(uid);
            autoEquipButton.onClick.AddListener(() => AutoEquipClicked?.Invoke());
        }

        public void Refresh(GameSession session, long selectedUid)
        {
            statsText.text = FantasyTexts.CharacterSummary(session.Stats);

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
