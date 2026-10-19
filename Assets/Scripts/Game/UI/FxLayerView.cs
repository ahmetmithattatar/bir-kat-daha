using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Ekran efektleri: sarsıntı, renkli flaş, uçan yazı, parçacık patlaması, ortada büyük yazı (banner).
    /// Hepsi basit UI şekilleri ve metinlerle yapılır (sprite/asset gerekmez).
    /// Tüm Canvas'ın en üstünde, tıklamaları engellemeyen bir katmandır.
    ///
    /// Uçan yazı ve parçacıklar şablondan kopyalanır (Instantiate) ve iş bitince silinir.
    /// Aynı anda en fazla birkaç düzine nesne olur, performans sorunu yaratmaz.
    /// </summary>
    public class FxLayerView : MonoBehaviour
    {
        [SerializeField] private RectTransform shakeTarget;        // Sarsılacak kök (tüm arayüz)
        [SerializeField] private Image flashImage;                 // Tam ekran renkli perde
        [SerializeField] private TMP_Text floatingTextTemplate;    // Kapalı şablon
        [SerializeField] private Image particleTemplate;           // Kapalı şablon (küçük kare)
        [SerializeField] private TMP_Text bannerText;              // Ortadaki büyük yazı

        private Coroutine _shakeRoutine;
        private Coroutine _flashRoutine;
        private Coroutine _bannerRoutine;

        private void Awake()
        {
            SetAlpha(flashImage, 0f);
            bannerText.alpha = 0f;
        }

        // ---------------- Sarsıntı ----------------

        public void Shake(float strength, float duration)
        {
            if (_shakeRoutine != null)
            {
                StopCoroutine(_shakeRoutine);
                shakeTarget.anchoredPosition = Vector2.zero;
            }
            _shakeRoutine = StartCoroutine(UIFx.Shake(shakeTarget, strength, duration));
        }

        // ---------------- Flaş ----------------

        public void Flash(Color color, float peakAlpha, float duration)
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine(color, peakAlpha, duration));
        }

        private IEnumerator FlashRoutine(Color color, float peakAlpha, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += FxTime.Delta;
                color.a = peakAlpha * (1f - Mathf.Clamp01(t / duration));
                flashImage.color = color;
                yield return null;
            }
            SetAlpha(flashImage, 0f);
        }

        // ---------------- Uçan yazı ----------------

        /// <summary>from'un üstünden yükselip kaybolan yazı ("+1.2K" gibi).</summary>
        public void FloatText(string text, Color color, float fontSize, RectTransform from)
        {
            TMP_Text label = Instantiate(floatingTextTemplate, transform);
            label.gameObject.SetActive(true);
            label.text = text;
            label.color = color;
            label.fontSize = fontSize;
            label.transform.position = from.position + new Vector3(Random.Range(-60f, 60f), 0f, 0f);
            StartCoroutine(FloatRoutine(label));
        }

        private IEnumerator FloatRoutine(TMP_Text label)
        {
            const float duration = 1.3f;
            const float rise = 160f;
            RectTransform rt = label.rectTransform;
            Vector2 start = rt.anchoredPosition;
            float t = 0f;
            while (t < duration)
            {
                t += FxTime.Delta;
                float k = Mathf.Clamp01(t / duration);
                rt.anchoredPosition = start + new Vector2(0f, rise * (1f - (1f - k) * (1f - k)));  // Yavaşlayarak yüksel
                rt.localScale = Vector3.one * (k < 0.15f ? Mathf.Lerp(0.6f, 1.15f, k / 0.15f) : Mathf.Lerp(1.15f, 1f, (k - 0.15f) * 4f));
                label.alpha = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                yield return null;
            }
            Destroy(label.gameObject);
        }

        // ---------------- Parçacık patlaması ----------------

        /// <summary>from'un ortasından dışarı saçılan küçük kareler. Nadir bulgularda daha çok ve daha hızlı.</summary>
        public void Burst(RectTransform from, Color color, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                Image particle = Instantiate(particleTemplate, transform);
                particle.gameObject.SetActive(true);
                particle.color = color;
                float size = Random.Range(10f, 24f);
                particle.rectTransform.sizeDelta = new Vector2(size, size);
                particle.transform.position = from.position;
                particle.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));
                Vector2 velocity = Random.insideUnitCircle.normalized * speed * Random.Range(0.4f, 1f);
                StartCoroutine(ParticleRoutine(particle, velocity));
            }
        }

        private IEnumerator ParticleRoutine(Image particle, Vector2 velocity)
        {
            float duration = Random.Range(0.6f, 1.0f);
            const float gravity = -900f;
            RectTransform rt = particle.rectTransform;
            Color color = particle.color;
            float t = 0f;
            while (t < duration)
            {
                float dt = FxTime.Delta;
                t += dt;
                velocity.y += gravity * dt;
                rt.anchoredPosition += velocity * dt;
                rt.Rotate(0f, 0f, 360f * dt);
                color.a = 1f - Mathf.Clamp01(t / duration);
                particle.color = color;
                yield return null;
            }
            Destroy(particle.gameObject);
        }

        // ---------------- Banner ----------------

        /// <summary>Ekranın ortasında büyük yazı: büyüyerek gelir, bekler, solar.</summary>
        public void Banner(string text, Color color, float holdSeconds)
        {
            if (_bannerRoutine != null) StopCoroutine(_bannerRoutine);
            _bannerRoutine = StartCoroutine(BannerRoutine(text, color, holdSeconds));
        }

        private IEnumerator BannerRoutine(string text, Color color, float holdSeconds)
        {
            bannerText.text = text;
            bannerText.color = color;
            RectTransform rt = bannerText.rectTransform;

            const float inTime = 0.18f;
            const float outTime = 0.35f;
            float t = 0f;
            while (t < inTime)
            {
                t += FxTime.Delta;
                float k = Mathf.Clamp01(t / inTime);
                rt.localScale = Vector3.one * Mathf.Lerp(1.8f, 1f, k);
                bannerText.alpha = k;
                yield return null;
            }
            yield return UIFx.Wait(holdSeconds);
            t = 0f;
            while (t < outTime)
            {
                t += FxTime.Delta;
                bannerText.alpha = 1f - Mathf.Clamp01(t / outTime);
                yield return null;
            }
            bannerText.alpha = 0f;
        }

        private static void SetAlpha(Image image, float alpha)
        {
            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }
    }
}
