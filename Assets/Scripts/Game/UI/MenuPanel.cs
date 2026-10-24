using UnityEngine;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Tüm tam ekran menülerin (ana menü, duraklatma, ayarlar, nasıl oynanır) ortak tabanı:
    /// CanvasGroup ile aç/kapat. Açıkken arkadaki her şeye tıklama engellenir.
    /// </summary>
    public abstract class MenuPanel : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup canvasGroup;

        public bool IsOpen => canvasGroup != null && canvasGroup.alpha > 0f;

        protected virtual void Awake()
        {
            SetVisible(false);
        }

        public virtual void Open()
        {
            SetVisible(true);
        }

        public virtual void Close()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
        }
    }
}
