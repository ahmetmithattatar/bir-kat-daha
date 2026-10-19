using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Fare imlecini takip eden açıklama kutusu (tek bir tane, Canvas'ın en üstünde).
    /// TooltipTrigger olan bir öğenin üstüne gelince açılır. Ekran kenarına yaklaşınca diğer tarafa geçer.
    /// </summary>
    public class TooltipView : MonoBehaviour
    {
        public static TooltipView Instance { get; private set; }

        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text text;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Vector2 offset = new Vector2(24f, -24f);

        private bool _visible;

        private void Awake()
        {
            Instance = this;
            Hide();
        }

        public void Show(string content)
        {
            if (string.IsNullOrEmpty(content)) return;
            text.text = content;
            group.alpha = 1f;
            _visible = true;
            Position();
        }

        public void Hide()
        {
            group.alpha = 0f;
            _visible = false;
        }

        private void Update()
        {
            if (_visible) Position();
        }

        private void Position()
        {
            Vector2 mouse = MousePosition();
            // Ekranın sağ yarısındaysa kutu imlecin soluna, alt kısmındaysa üstüne açılır
            bool right = mouse.x > Screen.width * 0.6f;
            bool bottom = mouse.y < Screen.height * 0.35f;
            panel.pivot = new Vector2(right ? 1f : 0f, bottom ? 0f : 1f);
            Vector2 o = new Vector2(right ? -Mathf.Abs(offset.x) : Mathf.Abs(offset.x), bottom ? Mathf.Abs(offset.y) : -Mathf.Abs(offset.y));
            panel.position = mouse + o;
        }

        private static Vector2 MousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }
    }
}
