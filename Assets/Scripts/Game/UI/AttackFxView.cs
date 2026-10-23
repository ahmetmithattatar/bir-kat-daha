using System.Collections;
using RiskLoop.Core;
using RiskLoop.Game.Art;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Silah saldırısı efektleri. Eşya ne kadar iyiyse saldırı o kadar gösterişli:
    ///   kalite = nadirlik kademesi (0..2) + güçlendirme kademesi (0..3)  -> 0..5  (GearArt.AttackQuality)
    ///
    ///   Yakın dövüş (kılıç, balta...): silah savrulur
    ///     1+ arkasında hayalet izler   2+ canavarın üstünde parlak kesik   3+ vuruşta ışık halkası
    ///     4+ silahtan enerji hilali fırlar   5 altın çift kesik (X) + şok dalgası
    ///   Uzaktan (yay, arbalet): ok uçar; kaliteyle ok büyür, iz bırakır, parlar, 4+ üç ok birden
    ///   Büyü (değnek, asa, küre, kitap): büyü küresi uçar; kaliteyle büyür, kıvılcım saçar, patlaması büyür
    /// Efekt görselleri çalışırken kodla oluşturulur (sahne kurucuya ek iş yok). DigView kurar ve çağırır.
    /// </summary>
    public class AttackFxView : MonoBehaviour
    {
        private RectTransform _layer;          // Efektlerin çizildiği katman (sahnenin içinde)
        private RectTransform _weaponRoot;
        private Image _weaponImage;
        private RectTransform _target;         // Canavar gövdesi
        private UIParticleSpawner _particles;

        private Image[] _ghosts;               // Savuruş izleri
        private Image _slash, _slash2, _ring, _projectile;

        private GearItem _weapon;
        private GearArt.WeaponStyle _style;
        private int _quality;
        private Color _color = Color.white;

        private static readonly Color Gold = new Color(1f, 0.82f, 0.25f);

        public void Init(RectTransform layer, RectTransform weaponRoot, Image weaponImage, RectTransform target, UIParticleSpawner particles)
        {
            _layer = layer;
            _weaponRoot = weaponRoot;
            _weaponImage = weaponImage;
            _target = target;
            _particles = particles;

            // Hayalet izler silahın kardeşi olarak (aynı dönme noktasında)
            _ghosts = new Image[3];
            for (int i = 0; i < _ghosts.Length; i++)
            {
                _ghosts[i] = NewImage($"WeaponGhost_{i}", weaponRoot.parent, weaponRoot.sizeDelta);
                var rt = _ghosts[i].rectTransform;
                rt.anchorMin = weaponRoot.anchorMin;
                rt.anchorMax = weaponRoot.anchorMax;
                rt.pivot = weaponRoot.pivot;
                rt.anchoredPosition = weaponRoot.anchoredPosition;
                rt.SetSiblingIndex(weaponRoot.GetSiblingIndex());   // Silahın arkasında
                _ghosts[i].preserveAspect = true;
            }

            _slash = NewImage("Slash", layer, new Vector2(520, 70));
            _slash2 = NewImage("Slash2", layer, new Vector2(520, 70));
            _ring = NewImage("ImpactRing", layer, new Vector2(300, 300));
            _projectile = NewImage("Projectile", layer, new Vector2(90, 90));
            _slash.sprite = _slash2.sprite = _ring.sprite = PixelArt.Glow();
        }

        private static Image NewImage(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.rectTransform.sizeDelta = size;
            img.raycastTarget = false;
            img.enabled = false;
            return img;
        }

        /// <summary>Kuşanılan silah değişince (tur başında) çağrılır.</summary>
        public void SetWeapon(GearItem weapon)
        {
            _weapon = weapon;
            _style = weapon != null ? GearArt.StyleOf(weapon.baseId) : GearArt.WeaponStyle.Melee;
            _quality = GearArt.AttackQuality(weapon);
            _color = weapon == null ? Color.white
                   : GearArt.EnhanceBand(weapon.enhance) >= 3 ? Gold
                   : RarityStyle.TierColor(weapon.tier);
            foreach (Image g in _ghosts) g.sprite = _weaponImage.sprite;
        }

        /// <summary>Tam saldırı: silah hareketi + kaliteye göre efektler. Canavar vurulma anında biter.</summary>
        public IEnumerator Play(float rest, float windup, float strike)
        {
            switch (_style)
            {
                case GearArt.WeaponStyle.Ranged: yield return PlayRanged(rest, windup); break;
                case GearArt.WeaponStyle.Magic: yield return PlayMagic(rest, windup); break;
                default: yield return PlayMelee(rest, windup, strike); break;
            }
            if (_quality >= 3) StartCoroutine(Ring(_quality >= 5 ? 1.6f : 1f));
            if (_quality >= 5) _particles.Spawn(_target.position, Gold, 16, 700f, gravity: -600f, minSize: 5f, maxSize: 11f, lifetime: 0.5f);
            else if (_quality >= 2) _particles.Spawn(_target.position, _color, 6 + _quality * 3, 450f, gravity: -900f, minSize: 4f, maxSize: 9f, lifetime: 0.45f);
        }

        // ---------------- Yakın dövüş ----------------

        private IEnumerator PlayMelee(float rest, float windup, float strike)
        {
            float windupTime = 0.05f + 0.01f * _quality;   // İyi silah daha "ağır" ve tok kalkar
            yield return Rotate(_weaponRoot, rest, windup + 5f * _quality, windupTime, easeIn: false);

            // Enerji hilali (4+): savuruşla aynı anda silahtan fırlar
            if (_quality >= 4) { _projectile.sprite = PixelArt.Glow(); StartCoroutine(Fly(_projectile, TipPosition(), _target.position, 0.12f, 1.1f, spin: 900f)); }

            // Savuruş + hayalet izler (1+)
            int ghosts = Mathf.Min(_quality, _ghosts.Length);
            for (int i = 0; i < ghosts; i++) StartCoroutine(Ghost(_ghosts[i], windup, strike, 0.02f * (i + 1), 0.45f - 0.1f * i));
            yield return Rotate(_weaponRoot, windup + 5f * _quality, strike - 5f * Mathf.Min(_quality, 3), 0.04f, easeIn: true);

            // Kesik (2+), altın X (5)
            if (_quality >= 2) StartCoroutine(Slash(_slash, -28f, 0.18f));
            if (_quality >= 5) StartCoroutine(Slash(_slash2, 28f, 0.18f));

            StartCoroutine(Rotate(_weaponRoot, strike, rest, 0.07f + 0.01f * _quality, easeIn: false));
        }

        private IEnumerator Ghost(Image ghost, float from, float to, float delay, float alpha)
        {
            yield return UIFx.Wait(delay);
            ghost.enabled = true;
            ghost.sprite = _weaponImage.sprite;
            float t = 0f;
            const float duration = 0.12f;
            while (t < duration)
            {
                t += FxTime.Delta;
                float k = Mathf.Clamp01(t / duration);
                ghost.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(from, to, k * k));
                ghost.color = new Color(_color.r, _color.g, _color.b, alpha * (1f - k));
                yield return null;
            }
            ghost.enabled = false;
        }

        private IEnumerator Slash(Image slash, float angle, float duration)
        {
            slash.enabled = true;
            slash.rectTransform.position = _target.position;
            slash.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            float size = 1f + 0.12f * _quality;
            float t = 0f;
            while (t < duration)
            {
                t += FxTime.Delta;
                float k = Mathf.Clamp01(t / duration);
                slash.rectTransform.localScale = new Vector3(Mathf.Lerp(0.2f, size, 1f - (1f - k) * (1f - k)), 1f - 0.6f * k, 1f);
                slash.color = new Color(Mathf.Lerp(1f, _color.r, k), Mathf.Lerp(1f, _color.g, k), Mathf.Lerp(1f, _color.b, k), 1f - k);
                yield return null;
            }
            slash.enabled = false;
        }

        // ---------------- Uzaktan (ok) ----------------

        private IEnumerator PlayRanged(float rest, float windup)
        {
            // Yay gerilir (geri çekil), ok bırakılır
            yield return Rotate(_weaponRoot, rest, rest + 12f, 0.06f + 0.01f * _quality, easeIn: false);
            _projectile.sprite = IconArt.Get("a_volley");   // Ok resmi
            float speed = 0.16f - 0.015f * _quality;          // İyi yay daha hızlı atar
            if (_quality >= 4)
            {
                // Üç ok birden: ikisi hafif yanlara
                StartCoroutine(Fly(_ghostArrow(0), TipPosition(), _target.position + new Vector3(-40f, 30f, 0f), speed, 0.25f, spin: 0f));
                StartCoroutine(Fly(_ghostArrow(1), TipPosition(), _target.position + new Vector3(40f, -30f, 0f), speed, 0.25f, spin: 0f));
            }
            StartCoroutine(Rotate(_weaponRoot, rest + 12f, rest, 0.1f, easeIn: false));
            yield return Fly(_projectile, TipPosition(), _target.position, speed, 0.9f + 0.1f * _quality, spin: 0f);
        }

        // Ek oklar için hayalet görüntüleri yeniden kullan (savuruş izi zaten yok)
        private Image _ghostArrow(int i)
        {
            Image img = _ghosts[i];
            img.sprite = IconArt.Get("a_volley");
            return img;
        }

        // ---------------- Büyü (küre) ----------------

        private IEnumerator PlayMagic(float rest, float windup)
        {
            // Asa kalkar, uçta küre toplanır, fırlar
            yield return Rotate(_weaponRoot, rest, rest + 20f, 0.07f + 0.01f * _quality, easeIn: false);
            _projectile.sprite = PixelArt.Glow();
            _particles.Spawn(TipPosition(), _color, 4 + _quality * 2, 180f, gravity: 0f, minSize: 3f, maxSize: 7f, lifetime: 0.3f);
            StartCoroutine(Rotate(_weaponRoot, rest + 20f, rest, 0.12f, easeIn: false));
            yield return Fly(_projectile, TipPosition(), _target.position, 0.17f - 0.012f * _quality, 0.8f + 0.25f * _quality, spin: 0f);
            _particles.Spawn(_target.position, _color, 10 + _quality * 4, 380f + _quality * 60f, gravity: -300f, minSize: 4f, maxSize: 10f, lifetime: 0.45f);
        }

        // ---------------- Ortak ----------------

        /// <summary>Mermi (ok, küre, enerji hilali) A'dan B'ye uçar. Kaliteyle iz bırakır.</summary>
        private IEnumerator Fly(Image img, Vector3 from, Vector3 to, float duration, float scale, float spin)
        {
            img.enabled = true;
            img.color = _style == GearArt.WeaponStyle.Ranged ? Color.white : new Color(_color.r, _color.g, _color.b, 0.95f);
            img.rectTransform.localScale = Vector3.one * scale;
            Vector3 dir = to - from;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float t = 0f;
            while (t < duration)
            {
                t += FxTime.Delta;
                float k = Mathf.Clamp01(t / duration);
                img.rectTransform.position = Vector3.Lerp(from, to, k);
                // Ok resmi sağ üste bakıyor (45 derece): uçuş yönüne çevir
                img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, spin != 0f ? -spin * t : angle - 45f);
                if (_quality >= 2 && _particles != null && Random.value < 0.5f)
                    _particles.Spawn(img.rectTransform.position, _color, 1, 40f, gravity: 0f, minSize: 3f, maxSize: 6f, lifetime: 0.25f);
                yield return null;
            }
            img.enabled = false;
        }

        private IEnumerator Ring(float size)
        {
            _ring.enabled = true;
            _ring.rectTransform.position = _target.position;
            float t = 0f;
            const float duration = 0.25f;
            while (t < duration)
            {
                t += FxTime.Delta;
                float k = Mathf.Clamp01(t / duration);
                _ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.3f, size, k);
                _ring.color = new Color(_color.r, _color.g, _color.b, 0.7f * (1f - k));
                yield return null;
            }
            _ring.enabled = false;
        }

        /// <summary>Silahın ucu (mermiler buradan çıkar).</summary>
        private Vector3 TipPosition()
        {
            Rect r = _weaponImage.rectTransform.rect;
            return _weaponImage.rectTransform.TransformPoint(new Vector3(r.xMax * 0.6f, r.yMax * 0.6f, 0f));
        }

        private static IEnumerator Rotate(RectTransform target, float from, float to, float duration, bool easeIn)
        {
            float t = 0f;
            while (t < duration)
            {
                t += FxTime.Delta;
                float k = Mathf.Clamp01(t / duration);
                float e = easeIn ? k * k : 1f - (1f - k) * (1f - k);
                target.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(from, to, e));
                yield return null;
            }
            target.localRotation = Quaternion.Euler(0f, 0f, to);
        }
    }
}
