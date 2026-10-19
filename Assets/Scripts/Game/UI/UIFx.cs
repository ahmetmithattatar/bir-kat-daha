using System.Collections;
using UnityEngine;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Küçük animasyon yardımcıları (coroutine). Hepsi FxTime.Delta kullanır.
    /// Kullanım: StartCoroutine(UIFx.Punch(rectTransform, 0.2f, 0.25f));
    /// Coroutine = Python'daki generator gibi: her "yield return null" bir kare bekler.
    /// </summary>
    public static class UIFx
    {
        /// <summary>Kısa süre büyüyüp eski boyuna döner ("pat" efekti).</summary>
        public static IEnumerator Punch(Transform target, float amount, float duration)
        {
            Vector3 baseScale = Vector3.one;
            float t = 0f;
            while (t < duration)
            {
                if (target == null) yield break;
                t += FxTime.Delta;
                float k = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);  // 0 -> 1 -> 0
                target.localScale = baseScale * (1f + amount * k);
                yield return null;
            }
            if (target != null) target.localScale = baseScale;
        }

        /// <summary>
        /// Hedefi rastgele sarsar, giderek söner, sonra yerine koyar.
        /// origin verilirse oraya döner. Üst üste binen sarsıntılarda origin VER: yoksa ikinci sarsıntı
        /// birincinin kaymış konumunu "yer" sanır ve nesne zamanla kayar.
        /// </summary>
        public static IEnumerator Shake(RectTransform target, float strength, float duration, Vector2? origin = null)
        {
            Vector2 home = origin ?? target.anchoredPosition;
            float t = 0f;
            while (t < duration)
            {
                t += FxTime.Delta;
                float fade = 1f - Mathf.Clamp01(t / duration);
                target.anchoredPosition = home + Random.insideUnitCircle * strength * fade;
                yield return null;
            }
            target.anchoredPosition = home;
        }

        /// <summary>Saniye bekler (zaman ölçeğinden bağımsız).</summary>
        public static IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += FxTime.Delta;
                yield return null;
            }
        }
    }
}
