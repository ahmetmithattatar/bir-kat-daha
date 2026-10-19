using System.Collections;
using RiskLoop.Core;
using RiskLoop.Game.Art;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Ekranın ortasında açılan büyük kart: nadir bulgu, keşif katı, baret kırılması.
    /// Oyuncu tıklayana ya da tuşa basana kadar açık kalır. Bu yüzden önemli anlar tuşa seri basarak kaçırılamaz.
    ///
    /// Bulgu açılışı: çerçeve beyazdan başlayıp gerçek nadirliğe kadar renk renk tırmanır (gerilim),
    /// sonunda isim açılır, parçacıklar saçılır. Nadirlik ne kadar yüksekse tırmanış o kadar uzun.
    /// </summary>
    public class RevealPopupView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;  // Görünürlük ve tıklama engelleme
        [SerializeField] private RectTransform card;
        [SerializeField] private Image frame;
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text hypeText;
        [SerializeField] private TMP_Text continueText;

        [Header("İkon")]
        [SerializeField] private Image iconImage;   // Bulgunun pixel art görseli
        [SerializeField] private Image iconRays;    // Arkasında dönen ışık hüzmeleri

        [Header("Zamanlama (saniye)")]
        [SerializeField] private float tierStepDuration = 0.11f;   // Renk tırmanışında her basamak
        [SerializeField] private float baseDismissDelay = 0.35f;   // Kapatılabilmesi için en az bekleme
        [SerializeField] private float dismissDelayPerTier = 0.08f;

        private static readonly Color ShieldColor = new Color(1f, 0.6f, 0.15f);

        private void Awake()
        {
            iconRays.sprite = PixelArt.Rays();
            Hide();
        }

        private void Update()
        {
            if (canvasGroup.alpha > 0f) iconRays.rectTransform.Rotate(0f, 0f, -35f * Time.unscaledDeltaTime);
        }

        /// <summary>
        /// Bulgu açılışı. Kapatılana kadar bekler (coroutine).
        /// header: üst başlık, extraHype: heyecan cümlesinin önüne eklenecek satır (ör. ilk keşif ödülü), yoksa null.
        /// </summary>
        public IEnumerator ShowFind(FindResult find, string header, string extraHype, FxLayerView fx)
        {
            Open();
            headerText.text = header;
            descriptionText.text = "";
            hypeText.text = "";
            continueText.text = "";

            // İkon önce siyah siluet: ne olduğu belli, ne kadar iyi olduğu belli değil
            iconImage.enabled = true;
            iconImage.sprite = SpriteLibrary.FindIcon(find);
            iconImage.color = Color.black;
            iconRays.enabled = false;

            // 1) Gerilim: çerçeve nadirlik basamaklarını tek tek tırmanır, isim gizli
            titleText.text = "?";
            titleText.color = Color.white;
            for (int tier = 0; tier <= find.Tier; tier++)
            {
                frame.color = RarityStyle.TierColor(tier);
                StartCoroutine(UIFx.Punch(iconImage.rectTransform, 0.1f, tierStepDuration));
                StartCoroutine(UIFx.Punch(card, 0.04f + 0.01f * tier, tierStepDuration));
                yield return UIFx.Wait(tierStepDuration * (1f + tier * 0.15f));  // Yukarı çıktıkça yavaşlar
            }

            // 2) Açılış: isim, açıklama, efektler. Nadirlik arttıkça efekt büyür.
            Color tierColor = RarityStyle.TierColor(find.Tier);
            iconImage.color = SpriteLibrary.FindIconTint(find);
            iconRays.enabled = true;
            iconRays.color = new Color(tierColor.r, tierColor.g, tierColor.b, 0.85f);
            iconRays.rectTransform.localScale = Vector3.one * (0.7f + 0.08f * find.Tier);
            StartCoroutine(UIFx.Punch(iconImage.rectTransform, 0.35f, 0.3f));
            titleText.text = UIStrings.FindTitle(find);
            descriptionText.text = UIStrings.FindDescription(find);
            string hype = UIStrings.TierHype(find.Tier);
            if (!string.IsNullOrEmpty(extraHype)) hype = extraHype + "\n" + hype;
            hypeText.text = hype;

            StartCoroutine(UIFx.Punch(card, 0.12f + 0.03f * find.Tier, 0.3f));
            fx.Burst(card, tierColor, 12 + find.Tier * 8, 500f + find.Tier * 120f);
            fx.Flash(tierColor, 0.08f + 0.04f * find.Tier, 0.5f);
            if (find.Tier >= 6) fx.Shake(6f + 3f * find.Tier, 0.4f);

            // 3) Oyuncu bakana kadar bekle
            yield return WaitForDismiss(baseDismissDelay + dismissDelayPerTier * find.Tier);
            Hide();
        }

        /// <summary>Baret kırıldı. Oyuncunun baretin gittiğini KESİN fark etmesi için durdurur.</summary>
        public IEnumerator ShowShieldBreak(int shieldsLeft, FxLayerView fx)
        {
            Open();
            frame.color = ShieldColor;
            headerText.text = UIStrings.ShieldBreakHeader;
            titleText.text = UIStrings.ShieldBreakTitle();
            titleText.color = Color.white;
            descriptionText.text = UIStrings.ShieldsLeft(shieldsLeft);
            hypeText.text = "";
            continueText.text = "";

            // İkon: kırık baret (yan yatmış, sönük)
            iconImage.enabled = true;
            iconImage.sprite = SpriteLibrary.HelmetSprite;
            iconImage.color = new Color(1f, 0.75f, 0.6f, 0.85f);
            iconImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            iconRays.enabled = false;

            StartCoroutine(UIFx.Punch(card, 0.2f, 0.35f));
            fx.Burst(card, new Color(0.55f, 0.5f, 0.45f), 30, 750f);  // Taş parçaları
            fx.Burst(card, ShieldColor, 14, 500f);                    // Baret parçaları

            yield return WaitForDismiss(0.6f);
            Hide();
        }

        private IEnumerator WaitForDismiss(float minDelay)
        {
            yield return UIFx.Wait(minDelay);
            continueText.text = UIStrings.PopupContinue;

            while (!DismissPressed())
            {
                // "Devam" yazısı nefes alır gibi yanıp söner
                continueText.alpha = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f);
                yield return null;
            }
        }

        /// <summary>Bu karede tıklama, dokunma ya da herhangi bir tuş var mı?</summary>
        private static bool DismissPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            return false;
#else
            return Input.anyKeyDown;
#endif
        }

        private void Open()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;  // Arkadaki butonlara tıklanmasın
        }

        private void Hide()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            card.localScale = Vector3.one;
            iconImage.rectTransform.localRotation = Quaternion.identity;
            iconImage.rectTransform.localScale = Vector3.one;
        }
    }
}
