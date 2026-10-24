using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Butona dokunma hissi: fare üstüne gelince hafifçe büyür, basınca küçülür, bırakınca yerine yaylanır.
    /// Görsel değişim (parlama, içe göçme) kaplamanın sprite değişiminden gelir; bu sadece boyutla oynar.
    /// Pasif butonda tepki vermez. Zaman ölçeğinden bağımsız (menü açıkken de çalışır).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const float HoverScale = 1.03f;
        private const float PressScale = 0.95f;
        private const float Speed = 18f;   // Hedef boyuta yaklaşma hızı

        private Button _button;
        private bool _hover;
        private bool _pressed;

        private void Awake() => _button = GetComponent<Button>();

        private void OnDisable()
        {
            _hover = _pressed = false;
            transform.localScale = Vector3.one;
        }

        public void OnPointerEnter(PointerEventData e) => _hover = true;
        public void OnPointerExit(PointerEventData e) => _hover = _pressed = false;
        public void OnPointerDown(PointerEventData e) => _pressed = true;
        public void OnPointerUp(PointerEventData e) => _pressed = false;

        private void Update()
        {
            float target = 1f;
            if (_button.interactable) target = _pressed ? PressScale : _hover ? HoverScale : 1f;
            float current = transform.localScale.x;
            if (Mathf.Abs(current - target) < 0.0005f) return;   // Yerinde: başka efektlerin boyutuna karışma
            float s = Mathf.Lerp(current, target, 1f - Mathf.Exp(-Speed * Time.unscaledDeltaTime));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
