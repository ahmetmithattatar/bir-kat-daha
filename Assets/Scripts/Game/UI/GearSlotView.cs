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
    /// Çerçeve rengi = eşyanın nadirliği, ortada eşyanın pixel art ikonu (nadirlik ve güçlendirmeyle süslü).
    /// Güçlendirilmiş eşyada arkada nabız gibi parıltı ve köşede "+N". Tıklanınca eşyanın uid'sini yayar.
    /// </summary>
    public class GearSlotView : MonoBehaviour
    {
        [SerializeField] private Image frame;
        [SerializeField] private Image inner;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;   // Karakter yuvalarında yuva adı (envanterde boş)
        [SerializeField] private Button button;
        [SerializeField] private Image glow;              // Güçlendirme parıltısı (ikonun arkasında)
        [SerializeField] private TMP_Text enhanceText;    // "+7" rozeti

        public event Action<long> Clicked;

        private static readonly Color EmptyFrame = new Color(0.28f, 0.28f, 0.33f);
        private static readonly Color InnerNormal = new Color(0.10f, 0.10f, 0.13f);
        private static readonly Color InnerSelected = new Color(0.25f, 0.25f, 0.32f);

        private long _uid = -1;
        private float _glowAlpha;
        private Color _glowColor;

        private static readonly Color Gold = new Color(1f, 0.82f, 0.25f);

        private void Awake()
        {
            button.onClick.AddListener(() =>
            {
                if (_uid >= 0) Clicked?.Invoke(_uid);
            });
            glow.sprite = PixelArt.Glow();
        }

        private void Update()
        {
            if (!glow.enabled) return;
            float pulse = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 3f + _uid);
            glow.color = new Color(_glowColor.r, _glowColor.g, _glowColor.b, _glowAlpha * pulse);
        }

        /// <summary>Eşya göster. item null ise boş kutu (emptySlot verilirse o yuvanın soluk şablonu).</summary>
        public void Set(GearItem item, GearSlot? emptySlot, bool selected, bool usable = true)
        {
            _uid = item != null ? item.uid : -1;
            inner.color = selected ? InnerSelected : InnerNormal;

            if (item != null)
            {
                frame.color = RarityStyle.TierColor(item.tier);
                icon.enabled = true;
                icon.sprite = GearArt.Icon(item);
                // Giyilemiyorsa (seviye ya da sınıf) ikon kırmızımsı ve soluk: "bekle, level atla"
                icon.color = usable ? Color.white : new Color(1f, 0.45f, 0.45f, 0.7f);
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

            // Güçlendirme: +4'ten itibaren parıltı, +10 altın
            int band = item != null ? GearArt.EnhanceBand(item.enhance) : 0;
            enhanceText.text = item != null && item.enhance > 0 ? $"+{item.enhance}" : "";
            enhanceText.color = band >= 3 ? Gold : Color.white;
            _glowAlpha = band == 0 ? 0f : 0.15f + 0.15f * band;
            _glowColor = band >= 3 ? Gold : item != null ? RarityStyle.TierColor(item.tier) : Color.white;
            glow.enabled = _glowAlpha > 0f;

            if (label != null && emptySlot.HasValue) label.text = FantasyTexts.SlotName(emptySlot.Value);
            button.interactable = item != null;
        }
    }
}
