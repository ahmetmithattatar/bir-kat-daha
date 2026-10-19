using UnityEngine;
using UnityEngine.EventSystems;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Üstüne fare gelince TooltipView'da açıklama gösterir. Text her yenilemede güncellenebilir.
    /// Çalışması için aynı nesnede raycastTarget'i açık bir Image (ya da başka Graphic) olmalı.
    /// </summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [TextArea] public string Text;

        private bool _hovering;

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovering = true;
            if (TooltipView.Instance != null) TooltipView.Instance.Show(Text);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovering = false;
            if (TooltipView.Instance != null) TooltipView.Instance.Hide();
        }

        /// <summary>Metni değiştir; fare zaten üstündeyse kutuyu da güncelle.</summary>
        public void SetText(string text)
        {
            Text = text;
            if (_hovering && TooltipView.Instance != null) TooltipView.Instance.Show(Text);
        }

        private void OnDisable()
        {
            if (_hovering && TooltipView.Instance != null) TooltipView.Instance.Hide();
            _hovering = false;
        }
    }
}
