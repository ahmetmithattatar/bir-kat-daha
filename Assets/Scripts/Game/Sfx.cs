using System.Collections.Generic;
using UnityEngine;

namespace RiskLoop.Game
{
    /// <summary>Oyundaki ses efektleri.</summary>
    public enum SoundId
    {
        Click,        // Buton
        Pick,         // Kazma vuruşu
        Break,        // Duvar yıkıldı
        Bust,         // Göçük
        Coin,         // Altın
        Find,         // Bulgu (nadirlikle perde yükselir)
        Courage,      // Riskten sağ çıkma (kademeyle yükselir)
        LevelUp,      // Seviye atlama
        Hit,          // Oyuncu vuruşu
        Crit,         // Kritik vuruş
        Hurt,         // Canavar vurdu
        MonsterDie,   // Canavar öldü
        Spell,        // Büyü
        EnhanceOk,    // Güçlendirme başarılı
        EnhanceFail,  // Güçlendirme başarısız
        Shatter,      // Eşya kırıldı
        CashOut       // Yüzeye dönüş
    }

    /// <summary>
    /// Ses efektleri: hiç ses dosyası yok, hepsi oyun açılınca KODLA sentezlenir (sinüs, kare dalga, gürültü + zarf).
    /// Görsellerdeki "dosyasız pixel art" yaklaşımının sesi. Gerçek bir ses tasarımcısı gelirse sadece Build* metotları değişir.
    ///
    /// Kullanım: Sfx.Play(SoundId.Coin) ya da Sfx.Play(SoundId.Find, tier). Ses seviyesi GameSettings.Volume.
    /// Sahnede tek bir Sfx bileşeni olmalı (sahne kurucu ekler); yoksa çağrılar sessizce yok sayılır.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        private const int SampleRate = 44100;
        private const int Voices = 10;   // Aynı anda en fazla bu kadar ses

        private static Sfx _instance;

        private readonly Dictionary<SoundId, AudioClip> _clips = new Dictionary<SoundId, AudioClip>();
        private AudioSource[] _sources;
        private int _next;

        // Domain Reload kapalı: önceki Play'den kalan (silinmiş) örneği unut
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => _instance = null;

        private void Awake()
        {
            _instance = this;
            _sources = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                _sources[i] = gameObject.AddComponent<AudioSource>();
                _sources[i].playOnAwake = false;
            }
            BuildAll();
        }

        /// <summary>
        /// Ses çal. level: perdeyi yükselten kademe (bulguda nadirlik 0..8, kutlamada 1..5). volume: 0..1 ek çarpan.
        /// </summary>
        public static void Play(SoundId id, int level = 0, float volume = 1f)
        {
            if (_instance == null) return;
            _instance.PlayInternal(id, level, volume);
        }

        private void PlayInternal(SoundId id, int level, float volume)
        {
            if (!_clips.TryGetValue(id, out AudioClip clip)) return;
            AudioSource src = _sources[_next];
            _next = (_next + 1) % _sources.Length;
            // Kademe perdeyi yükseltir (yarım ton adımları); ufak rastgelelik aynı sesin tekrarını yormasın
            src.pitch = Mathf.Pow(1.0595f, level * 2) * Random.Range(0.96f, 1.04f);
            src.volume = GameSettings.Volume * volume;
            src.clip = clip;
            src.Play();
        }

        // ---------------- Sentez ----------------

        private void BuildAll()
        {
            _clips[SoundId.Click] = Build("click", 0.05f, (t, d) => Square(t, 1800) * Env(t, d, 0.002f) * 0.25f);
            _clips[SoundId.Pick] = Build("pick", 0.16f, (t, d) => (Noise() * 0.6f + Sine(t, 180 - 400 * t)) * Env(t, d, 0.003f) * 0.6f);
            _clips[SoundId.Break] = Build("break", 0.45f, (t, d) => (Noise() * 0.8f + Sine(t, 90) * 0.5f) * Env(t, d, 0.005f) * 0.6f);
            _clips[SoundId.Bust] = Build("bust", 1.4f, (t, d) => (Noise() * 0.7f + Sine(t, 45 + 10 * Mathf.Sin(t * 9)) * 0.8f) * Env(t, d, 0.02f) * 0.8f);
            _clips[SoundId.Coin] = Build("coin", 0.22f, (t, d) => Sine(t, t < 0.07f ? 1320 : 1760) * Env(t, d, 0.002f) * 0.35f);
            _clips[SoundId.Find] = Build("find", 0.5f, (t, d) => Arp(t, 660, new[] { 1f, 1.25f, 1.5f, 2f }, 0.08f) * Env(t, d, 0.005f) * 0.35f);
            _clips[SoundId.Courage] = Build("courage", 0.6f, (t, d) => (Arp(t, 440, new[] { 1f, 1.26f, 1.5f }, 0.07f) + Sine(t, 880) * 0.3f) * Env(t, d, 0.005f) * 0.35f);
            _clips[SoundId.LevelUp] = Build("levelup", 0.9f, (t, d) => (Arp(t, 523, new[] { 1f, 1.26f, 1.5f, 2f, 2.52f }, 0.1f) + Sine(t, 1046) * 0.2f) * Env(t, d, 0.01f) * 0.35f);
            _clips[SoundId.Hit] = Build("hit", 0.14f, (t, d) => (Noise() * 0.5f + Sine(t, 220 - 600 * t)) * Env(t, d, 0.002f) * 0.55f);
            _clips[SoundId.Crit] = Build("crit", 0.25f, (t, d) => (Noise() * 0.4f + Square(t, 330 - 500 * t) * 0.6f + Sine(t, 1500) * 0.2f) * Env(t, d, 0.002f) * 0.55f);
            _clips[SoundId.Hurt] = Build("hurt", 0.3f, (t, d) => (Sine(t, 110 - 120 * t) + Noise() * 0.3f) * Env(t, d, 0.003f) * 0.7f);
            _clips[SoundId.MonsterDie] = Build("monsterdie", 0.7f, (t, d) => (Square(t, 300 - 350 * t) * 0.5f + Noise() * 0.5f) * Env(t, d, 0.005f) * 0.5f);
            _clips[SoundId.Spell] = Build("spell", 0.6f, (t, d) => (Sine(t, 700 + 900 * t) * 0.5f + Sine(t, 1050 + 1350 * t) * 0.3f) * Env(t, d, 0.03f) * 0.35f);
            _clips[SoundId.EnhanceOk] = Build("enhanceok", 0.7f, (t, d) => (Arp(t, 784, new[] { 1f, 1.5f, 2f }, 0.09f) + Sine(t, 2093) * 0.15f) * Env(t, d, 0.005f) * 0.4f);
            _clips[SoundId.EnhanceFail] = Build("enhancefail", 0.4f, (t, d) => Square(t, 200 - 120 * t) * Env(t, d, 0.005f) * 0.3f);
            _clips[SoundId.Shatter] = Build("shatter", 0.8f, (t, d) => (Noise() * 0.8f + Sine(t, 2400 - 2000 * t) * 0.3f) * Env(t, d, 0.002f) * 0.55f);
            _clips[SoundId.CashOut] = Build("cashout", 0.8f, (t, d) => (Arp(t, 523, new[] { 1f, 1.5f, 2f, 1.5f, 2f }, 0.07f) * 0.6f + Sine(t, 1568) * 0.15f) * Env(t, d, 0.005f) * 0.4f);
        }

        /// <summary>Örnekleri bir fonksiyondan üret: f(zaman, süre) -> -1..1.</summary>
        private static AudioClip Build(string name, float duration, System.Func<float, float, float> f)
        {
            int n = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)SampleRate, duration), -1f, 1f);
            AudioClip clip = AudioClip.Create("sfx_" + name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Sine(float t, float freq) => Mathf.Sin(2f * Mathf.PI * freq * t);
        private static float Square(float t, float freq) => Sine(t, freq) >= 0 ? 1f : -1f;
        private static float Noise() => Random.Range(-1f, 1f);

        /// <summary>Notalar sırayla (arpej): her nota "step" saniye.</summary>
        private static float Arp(float t, float baseFreq, float[] ratios, float step)
        {
            int i = Mathf.Min((int)(t / step), ratios.Length - 1);
            return Sine(t, baseFreq * ratios[i]);
        }

        /// <summary>Zarf: hızlı yükselir (attack), sonra üstel söner. Tıkırtı ve patlamaları yumuşatır.</summary>
        private static float Env(float t, float duration, float attack)
        {
            float a = Mathf.Clamp01(t / Mathf.Max(attack, 0.0001f));
            float decay = Mathf.Exp(-4.5f * t / duration);
            return a * decay;
        }
    }
}
