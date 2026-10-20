using RiskLoop.Core;
using RiskLoop.Game.Art;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Koleksiyondaki küçük kart: aynı tür + aynı nadirlikteki bulguların grubu.
    /// Çerçeve rengi = nadirlik, ortada pixel art ikon, sağ üstte adet (×3), altta tür ve toplam etki.
    /// Fareyle üstüne gelince ne verdiği ayrıntılı yazar (risk altında mı, cepte mi).
    /// </summary>
    public class FindMiniCardView : MonoBehaviour
    {
        [SerializeField] private Image frame;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private TooltipTrigger tooltip;   // Fareyle üstüne gelince grubun toplam etkisi

        /// <summary>sample: gruptaki son bulgu (ikon ve eşya yuvası için).</summary>
        public void Set(FindResult sample, double totalValue, int count)
        {
            Color color = RarityStyle.TierColor(sample.Tier);
            frame.color = color;
            icon.enabled = true;
            icon.sprite = SpriteLibrary.FindIcon(sample);
            icon.color = SpriteLibrary.FindIconTint(sample);
            typeText.text = UIStrings.FindTypeShortName(sample.Type);
            typeText.color = color;
            valueText.text = sample.Type == FindType.Gear && sample.Item != null
                ? FantasyTexts.SlotName(sample.Item.slot)
                : UIStrings.FindShortValue(sample.Type, totalValue);
            countText.text = count > 1 ? $"×{count}" : "";
            tooltip.SetText(UIStrings.FindGroupTooltip(sample, totalValue, count));
        }

        /// <summary>"N tane daha" kartı (yer kalmadığında son kutu).</summary>
        public void SetOverflow(int hiddenCount)
        {
            frame.color = new Color(0.4f, 0.4f, 0.45f);
            icon.enabled = false;
            typeText.text = $"+{hiddenCount}";
            typeText.color = Color.white;
            valueText.text = "daha";
            countText.text = "";
            tooltip.SetText(UIStrings.FindOverflowTooltip(hiddenCount));
        }
    }
}
