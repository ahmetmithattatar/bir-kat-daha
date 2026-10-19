using System;
using RiskLoop.Core;
using RiskLoop.Game.Art;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Ekipman ekranındaki tek kutu: karakter yuvası ya da envanter hücresi.
    /// Çerçeve rengi = eşyanın nadirliği, ortada eşyanın pixel art ikonu. Tıklanınca eşyanın uid'sini yayar.
    /// </summary>
    public class GearSlotView : MonoBehaviour
    {
        [SerializeField] private Image frame;
        [SerializeField] private Image inner;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;   // Karakter yuvalarında yuva adı (envanterde boş)
        [SerializeField] private Button button;

        public event Action<long> Clicked;

        private static readonly Color EmptyFrame = new Color(0.28f, 0.28f, 0.33f);
        private static readonly Color InnerNormal = new Color(0.10f, 0.10f, 0.13f);
        private static readonly Color InnerSelected = new Color(0.25f, 0.25f, 0.32f);

        private long _uid = -1;

        private void Awake()
        {
            button.onClick.AddListener(() =>
            {
                if (_uid >= 0) Clicked?.Invoke(_uid);
            });
        }

        /// <summary>Eşya göster. item null ise boş kutu (emptySlot verilirse o yuvanın soluk şablonu).</summary>
        public void Set(GearItem item, GearSlot? emptySlot, bool selected)
        {
            _uid = item != null ? item.uid : -1;
            inner.color = selected ? InnerSelected : InnerNormal;

            if (item != null)
            {
                frame.color = RarityStyle.TierColor(item.tier);
                icon.enabled = true;
                icon.sprite = GearArt.Icon(item);
                icon.color = Color.white;
            }
            else
            {
                frame.color = EmptyFrame;
                icon.enabled = emptySlot.HasValue;
                if (emptySlot.HasValue)
                {
                    icon.sprite = GearArt.EmptySlotSprite(emptySlot.Value);
                    icon.color = new Color(1f, 1f, 1f, 0.6f);
                }
            }

            if (label != null && emptySlot.HasValue) label.text = FantasyTexts.SlotName(emptySlot.Value);
            button.interactable = item != null;
        }
    }
}
