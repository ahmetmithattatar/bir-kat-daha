using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Küçük stat kartı: üstte soluk etiket ("RİSK"), altta büyük değer ("%24"). Açıklaması fareyle üstüne gelince çıkar.
    /// Ekranı yazı yığını yerine bir bakışta okunan değerlerle doldurur.
    /// </summary>
    public class StatChipView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text value;
        [SerializeField] private Image accent;          // Sol kenarda renkli şerit (değerin rengi)
        [SerializeField] private TooltipTrigger tooltip;

        public RectTransform Rect => (RectTransform)transform;

        public void Set(string labelText, string valueText, Color color, string tooltipText)
        {
            label.text = labelText;
            value.text = valueText;
            value.color = color;
            accent.color = color;
            tooltip.SetText(tooltipText);
        }
    }
}
