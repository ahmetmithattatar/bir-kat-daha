using UnityEngine;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Animasyonların ortak saati. Speed = 1 normal; oto-kazıda 2.5 gibi bir değer tüm efektleri hızlandırır.
    /// Oyunun kendisi (Core) zaman bilmez; bu sadece sunum hızıdır.
    /// (Time.timeScale kullanmıyoruz: canavar sayacı gibi gerçek zamanlı şeyler etkilenmesin.)
    /// </summary>
    public static class FxTime
    {
        public static float Speed = 1f;

        /// <summary>Her Play başında hız normale döner (Domain Reload kapalıyken statik alan önceki Play'den kalabilir).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Speed = 1f;

        /// <summary>Bu kareden geçen animasyon süresi (hızlandırılmış).</summary>
        public static float Delta => Time.unscaledDeltaTime * Speed;
    }
}
