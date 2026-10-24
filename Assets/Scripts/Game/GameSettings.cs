using System;
using UnityEngine;

namespace RiskLoop.Game
{
    /// <summary>
    /// Oyuncu ayarları (ses, tam ekran, sarsıntı, parlama, rehber görüldü mü). PlayerPrefs'te saklanır:
    /// kayıt dosyasından (save.json) bağımsızdır, "Yeni Oyun" ayarları silmez.
    /// Değişince Changed olayı yayılır (ses ve ekran modu anında uygulanır).
    /// </summary>
    public static class GameSettings
    {
        private const string VolumeKey = "settings.volume";
        private const string FullscreenKey = "settings.fullscreen";
        private const string ShakeKey = "settings.shake";
        private const string FlashKey = "settings.flash";
        private const string TutorialKey = "settings.tutorialSeen";

        public static event Action Changed;

        // Domain Reload kapalı: önceki Play'in (silinmiş) aboneleri kalmasın
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Changed = null;

        /// <summary>Ana ses seviyesi 0..1 (varsayılan 0.7).</summary>
        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 0.7f);
            set { PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value)); Save(); }
        }

        /// <summary>Tam ekran mı? (Varsayılan: pencere.)</summary>
        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(FullscreenKey, 0) == 1;
            set { PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0); ApplyScreen(); Save(); }
        }

        /// <summary>Ekran sarsıntısı açık mı? (Bazı oyuncular rahatsız olur, kapatabilsin.)</summary>
        public static bool ScreenShake
        {
            get => PlayerPrefs.GetInt(ShakeKey, 1) == 1;
            set { PlayerPrefs.SetInt(ShakeKey, value ? 1 : 0); Save(); }
        }

        /// <summary>Parlak flaşlar açık mı? Kapalıyken flaşlar çok hafif olur (göz yorgunluğu, ışığa hassasiyet).</summary>
        public static bool Flashes
        {
            get => PlayerPrefs.GetInt(FlashKey, 1) == 1;
            set { PlayerPrefs.SetInt(FlashKey, value ? 1 : 0); Save(); }
        }

        /// <summary>"Nasıl oynanır" rehberi bir kez gösterildi mi? (İlk oyunda otomatik açılır.)</summary>
        public static bool TutorialSeen
        {
            get => PlayerPrefs.GetInt(TutorialKey, 0) == 1;
            set { PlayerPrefs.SetInt(TutorialKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Kayıtlı ekran modunu uygula (oyun açılışında ve ayar değişince).</summary>
        public static void ApplyScreen()
        {
            if (Fullscreen)
            {
                Resolution r = Screen.currentResolution;
                Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
            }
            else
            {
                Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
            }
        }

        private static void Save()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
