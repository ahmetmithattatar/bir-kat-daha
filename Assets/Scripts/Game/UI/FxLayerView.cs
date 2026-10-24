using System.Collections;
using RiskLoop.Game.Art;
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

        private Image _raysImage;        // Banner'ın arkasında dönen ışık hüzmeleri (kutlama kademesi yükselince)
        private Coroutine _raysRoutine;

        private void Awake()
        {
            SetAlpha(flashImage, 0f);
            bannerText.alpha = 0f;
            // Koyu kontur: beyaz/sarı yazı parlak flaşın üstünde de okunsun
            bannerText.outlineWidth = 0.18f;
            bannerText.outlineColor = new Color32(20, 12, 8, 255);

            // Hüzmeler banner'ın hemen arkasında (kodla eklenir, sahne kurucuya gerek yok)
            _raysImage = CreateLayerImage("CelebrationRays", PixelArt.Rays(16, 96), new Vector2(1100, 1100));
            _raysImage.transform.SetSiblingIndex(bannerText.transform.GetSiblingIndex());
            _raysImage.gameObject.SetActive(false);
        }

        /// <summary>Efekt katmanında tıklama almayan, ortalanmış bir görsel oluşturur.</summary>
        private Image CreateLayerImage(string name, Sprite sprite, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = size;
            return image;
        }

        // ---------------- Sarsıntı ----------------

        public void Shake(float strength, float duration)
        {
            if (!GameSettings.ScreenShake) return;   // Ayarlardan kapatılabilir
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
            if (!GameSettings.Flashes) peakAlpha *= 0.25f;   // Parlak flaşlar kapalıysa çok hafif
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

        // ---------------- Altın akışı ----------------

        /// <summary>
        /// from'dan to'ya kavisle uçan altın parçacıkları; vardıklarında hedef "pat" diye büyür.
        /// Kazancın nereye gittiğini GÖRÜNÜR kılar (sayaca akan para hissi).
        /// </summary>
        public void CoinsTo(RectTransform from, RectTransform to, int count, Color color)
        {
            for (int i = 0; i < count; i++) StartCoroutine(CoinRoutine(from.position, to, color, i * 0.035f));
        }

        private IEnumerator CoinRoutine(Vector3 start, RectTransform to, Color color, float delay)
        {
            yield return UIFx.Wait(delay);
            Image coin = Instantiate(particleTemplate, transform);
            coin.gameObject.SetActive(true);
            coin.color = color;
            coin.rectTransform.sizeDelta = new Vector2(14f, 14f);
            Vector3 jitter = new Vector3(Random.Range(-60f, 60f), Random.Range(-30f, 50f), 0f);
            Vector3 control = (start + to.position) * 0.5f + new Vector3(0f, 180f, 0f) + jitter;

            const float duration = 0.55f;
            float t = 0f;
            while (t < duration)
            {
                t += FxTime.Delta;
                float k = Mathf.Clamp01(t / duration);
                float e = k * k;  // Hızlanarak hedefe
                // İkinci derece Bezier: başlangıç -> kontrol -> hedef (kavis)
                Vector3 a = Vector3.Lerp(start + jitter * 0.3f, control, e);
                Vector3 b = Vector3.Lerp(control, to.position, e);
                coin.transform.position = Vector3.Lerp(a, b, e);
                coin.transform.Rotate(0f, 0f, 720f * FxTime.Delta);
                yield return null;
            }
            Destroy(coin.gameObject);
            if (to != null) StartCoroutine(UIFx.Punch(to, 0.08f, 0.12f));
        }

        // ---------------- Banner ----------------

        /// <summary>
        /// Ekranın ortasında büyük yazı: büyüyerek gelir, bekler, solar.
        /// punch > 1: daha büyükten çarparak gelir, hedefi aşıp geri oturur ve beklerken titrer (büyük kutlamalar).
        /// </summary>
        public void Banner(string text, Color color, float holdSeconds, float punch = 1f)
        {
            if (_bannerRoutine != null) StopCoroutine(_bannerRoutine);
            _bannerRoutine = StartCoroutine(BannerRoutine(text, color, holdSeconds, punch));
        }

        private IEnumerator BannerRoutine(string text, Color color, float holdSeconds, float punch)
        {
            bannerText.text = text;
            bannerText.color = color;
            RectTransform rt = bannerText.rectTransform;
            float size = 1f + 0.12f * (punch - 1f);   // Büyük kutlamada yazı da biraz daha büyük kalır

            const float inTime = 0.18f;
            const float outTime = 0.35f;
            float t = 0f;
            while (t < inTime)
            {
                t += FxTime.Delta;
                float k = Mathf.Clamp01(t / inTime);
                // Büyükten küçülerek çarpar; punch'ta hedefin biraz altına iner (sonra geri sıçrar)
                float overshoot = punch > 1f ? Mathf.Sin(k * Mathf.PI) * -0.08f * (punch - 1f) : 0f;
                rt.localScale = Vector3.one * (Mathf.Lerp(1.8f * punch, size, k * k) + overshoot);
                bannerText.alpha = k;
                yield return null;
            }
            // Bekleme: büyük kutlamada yazı hafifçe titrer ve nabız gibi atar
            t = 0f;
            while (t < holdSeconds)
            {
                t += FxTime.Delta;
                float jitter = punch > 1.3f ? (punch - 1.3f) * 3f : 0f;
                rt.localScale = Vector3.one * (size + 0.03f * (punch - 1f) * Mathf.Sin(t * 30f));
                rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-jitter, jitter));
                yield return null;
            }
            rt.localRotation = Quaternion.identity;
            t = 0f;
            while (t < outTime)
            {
                t += FxTime.Delta;
                bannerText.alpha = 1f - Mathf.Clamp01(t / outTime);
                yield return null;
            }
            bannerText.alpha = 0f;
            rt.localScale = Vector3.one;
        }

        // ---------------- Kutlama: şok dalgası, ekran çatlağı, hüzmeler ----------------

        /// <summary>from'dan yayılan renkli halka (patlamanın dalgası). Büyüdükçe incelir ve solar.</summary>
        public void Shockwave(RectTransform from, Color color, float maxSize, float duration, float delay = 0f)
        {
            StartCoroutine(ShockwaveRoutine(from.position, color, maxSize, duration, delay));
        }

        private IEnumerator ShockwaveRoutine(Vector3 position, Color color, float maxSize, float duration, float delay)
        {
            if (delay > 0f) yield return UIFx.Wait(delay);
            Image ring = CreateLayerImage("Shockwave", PixelArt.Ring(), Vector2.one * 40f);
            ring.transform.position = position;
            float t = 0f;
            while (t < duration)
            {
                t += FxTime.Delta;
                float k = Mathf.Clamp01(t / duration);
                float e = 1f - (1f - k) * (1f - k) * (1f - k);   // Hızlı başlar, yavaşlar
                ring.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(40f, maxSize, e);
                color.a = 1f - k;
                ring.color = color;
                yield return null;
            }
            Destroy(ring.gameObject);
        }

        /// <summary>
        /// Ekran çatlar: merkezden dallanan parlak çizgiler bir anda belirir, hafifçe büyüyüp solar.
        /// branches arttıkça çatlak çoğalır (kademe yükseldikçe daha çok "kırılır").
        /// </summary>
        public void ScreenCracks(Color color, int branches, float duration, int seed)
        {
            if (!GameSettings.Flashes) color.a *= 0.4f;
            StartCoroutine(CracksRoutine(color, branches, duration, seed));
        }

        private IEnumerator CracksRoutine(Color color, int branches, float duration, int seed)
        {
            Image cracks = CreateLayerImage("ScreenCracks", PixelArt.GlassCracks(seed, branches), new Vector2(1400, 1400));
            cracks.transform.SetSiblingIndex(bannerText.transform.GetSiblingIndex());   // Yazının arkasında
            cracks.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            float peak = color.a;
            float t = 0f;
            while (t < duration)
            {
                t += FxTime.Delta;
                float k = Mathf.Clamp01(t / duration);
                cracks.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.08f, k);
                color.a = peak * (k < 0.5f ? 1f : 1f - (k - 0.5f) / 0.5f);
                cracks.color = color;
                yield return null;
            }
            Destroy(cracks.gameObject);
        }

        /// <summary>Banner'ın arkasında dönen, büyüyen ışık hüzmeleri.</summary>
        public void Rays(Color color, float duration)
        {
            if (_raysRoutine != null) StopCoroutine(_raysRoutine);
            _raysRoutine = StartCoroutine(RaysRoutine(color, duration));
        }

        private IEnumerator RaysRoutine(Color color, float duration)
        {
            _raysImage.gameObject.SetActive(true);
            _raysImage.transform.position = bannerText.transform.position;   // Yazının tam arkasında
            float t = 0f;
            while (t < duration)
            {
                t += FxTime.Delta;
                float k = Mathf.Clamp01(t / duration);
                _raysImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.2f, 1f - (1f - k) * (1f - k));
                _raysImage.rectTransform.Rotate(0f, 0f, 70f * FxTime.Delta);
                color.a = 0.55f * (k < 0.2f ? k / 0.2f : 1f - (k - 0.2f) / 0.8f);
                _raysImage.color = color;
                yield return null;
            }
            _raysImage.gameObject.SetActive(false);
        }

        private static void SetAlpha(Image image, float alpha)
        {
            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }
    }
}
