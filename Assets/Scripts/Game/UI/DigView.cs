using System;
using System.Collections;
using System.Collections.Generic;
using RiskLoop.Core;
using RiskLoop.Game.Art;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Birinci şahıs kazı sahnesi: önünde kaya duvarı, etrafında tünel, sağ altta kazma.
    ///
    /// Her vuruş: kazma geriye çekilir ve hızla duvara iner, kıvılcım ve moloz saçılır, duvar sarsılır, çatlak büyür.
    /// Son vuruşta duvar parçalanır, açılan delikten (varsa) bulgu parlayarak çıkar, sonra tünelde ilerlenir
    /// ve karanlıktan yeni duvar belirir. Derine indikçe kaya rengi değişir (DepthTheme).
    /// Risk sahnede de görünür: tavandaki çatlak koyulaşır, tavandan daha sık çakıl düşer.
    ///
    /// Oyun kuralı yok; sadece gösterir. Tıklanınca SwingClicked olayını yayar.
    /// </summary>
    public class DigView : MonoBehaviour
    {
        [Header("Tünel")]
        [SerializeField] private RectTransform sceneRoot;     // Tüm sahne; büyük sarsıntılarda bu sarsılır
        [SerializeField] private Image[] tunnelRings;        // Dıştan içe iç içe çerçeveler (derinlik hissi)
        [SerializeField] private RectTransform tunnelRoot;    // İlerleme animasyonunda büyütülür
        [SerializeField] private Image ceilingCrack;          // Riskle koyulaşan tavan çatlağı

        [Header("Duvar")]
        [SerializeField] private RectTransform wallRoot;
        [SerializeField] private Image wallImage;
        [SerializeField] private Image crackImage;
        [SerializeField] private TMP_Text depthLabel;         // Duvara kazınmış "12. KAT"
        [SerializeField] private Image holeImage;             // Duvar yıkılınca açılan karanlık delik

        [Header("Bulgu")]
        [SerializeField] private RectTransform itemRoot;
        [SerializeField] private Image raysImage;
        [SerializeField] private Image glowImage;
        [SerializeField] private Image itemImage;

        [Header("Kazma ve baret")]
        [SerializeField] private RectTransform pickaxeRoot;
        [SerializeField] private Image pickaxeImage;

        [Header("Atmosfer")]
        [SerializeField] private Image vignette;
        [SerializeField] private Image darkOverlay;
        [SerializeField] private RectTransform fallenRocksRoot;  // Göçükte düşen taşlar burada birikir
        [SerializeField] private UIParticleSpawner particles;
        [SerializeField] private Button clickCatcher;            // Sahneye tıklamak = kazma vur

        [Header("Savaş: canavar")]
        [SerializeField] private RectTransform monsterRoot;   // Kap: gövde + aura + isim/can barı (ölçeklenmez)
        [SerializeField] private RectTransform monsterBody;   // Büyüyen/sarsılan gövde
        [SerializeField] private Image monsterImage;
        [SerializeField] private Image monsterAura;           // Bosslarda arkada nabız gibi atan parıltı
        [SerializeField] private TMP_Text monsterNameText;
        [SerializeField] private RectTransform monsterHpFill;
        [SerializeField] private TMP_Text monsterHpText;

        [Header("Savaş: oyuncu")]
        [SerializeField] private RectTransform weaponRoot;    // Sol alt köşe: diğer elde silah
        [SerializeField] private Image weaponImage;
        [SerializeField] private RectTransform playerHpFill;
        [SerializeField] private Image playerHpFillImage;
        [SerializeField] private TMP_Text playerHpText;

        [Header("Atmosfer ve parıltı")]
        [SerializeField] private Image torchGlow;             // Duvarı aydınlatan sıcak, titreyen ışık
        [SerializeField] private Image lootBeam;              // Nadir bulguda yukarı uzanan renkli ışın
        [SerializeField] private RectTransform enrageBar;     // Boss öfke çubuğu (kap)
        [SerializeField] private RectTransform enrageFill;
        [SerializeField] private TMP_Text enrageText;

        [Header("Kazma açıları (derece)")]
        [SerializeField] private float restAngle = 25f;
        [SerializeField] private float windupAngle = -12f;
        [SerializeField] private float strikeAngle = 58f;

        public event Action SwingClicked;

        /// <summary>Bulgunun koleksiyona uçarken çıktığı yer.</summary>
        public RectTransform ItemAnchor => itemRoot;

        /// <summary>Hasar sayılarının çıktığı yer.</summary>
        public RectTransform MonsterAnchor => monsterBody;

        /// <summary>Duvarın ortası (kat ödülünün altınları buradan uçar).</summary>
        public RectTransform WallAnchor => wallRoot;

        /// <summary>Oyuncunun aldığı hasar sayıları can barından çıkar.</summary>
        public RectTransform PlayerHpAnchor => (RectTransform)playerHpFill.parent;

        // Silah açıları (sol elde). Kılıç çizimi zaten 45° çapraz: +15 ile neredeyse dik durur,
        // geri çekilince dikleşir (+50), vuruşta canavara doğru yatar (-35).
        private const float WeaponRest = 15f;
        private const float WeaponWindup = 50f;
        private const float WeaponStrike = -35f;

        private static readonly Color HitTint = new Color(1f, 0.45f, 0.45f);
        private Color _auraColor;


        private double _risk;
        private int _digStep;        // Şu an kazılan kat
        private int _wallSeed;
        private float _nextPebbleTime;
        private Vector2 _pickaxeRestPos;
        private readonly List<GameObject> _fallenRocks = new List<GameObject>();
        private AttackFxView _attackFx;   // Silah kalitesine göre saldırı efektleri

        private void Awake()
        {
            clickCatcher.onClick.AddListener(() => SwingClicked?.Invoke());
            _pickaxeRestPos = pickaxeRoot.anchoredPosition;

            pickaxeImage.sprite = SpriteLibrary.PickaxeSprite;
            raysImage.sprite = PixelArt.Rays();
            glowImage.sprite = PixelArt.Glow();
            holeImage.sprite = PixelArt.Glow();
            vignette.sprite = PixelArt.Vignette();

            itemRoot.gameObject.SetActive(false);
            monsterRoot.gameObject.SetActive(false);
            monsterAura.sprite = PixelArt.Glow();
            torchGlow.sprite = PixelArt.Glow(64);
            lootBeam.sprite = PixelArt.Glow(64);
            lootBeam.enabled = false;
            enrageBar.gameObject.SetActive(false);
            weaponRoot.localRotation = Quaternion.Euler(0, 0, WeaponRest);
            SetAlpha(holeImage, 0f);
            SetAlpha(darkOverlay, 0f);
            pickaxeRoot.localRotation = Quaternion.Euler(0, 0, restAngle);

            _attackFx = sceneRoot.gameObject.AddComponent<AttackFxView>();
            _attackFx.Init(sceneRoot, weaponRoot, weaponImage, monsterBody, particles);
        }

        private void Update()
        {
            float now = Time.unscaledTime;

            // Fener titremesi: kenar karartması hafifçe nefes alır, meşale ışığı titrer
            float flicker = Mathf.PerlinNoise(now * 3f, 0f);
            SetAlpha(vignette, 0.85f + 0.08f * flicker);
            torchGlow.color = new Color(1f, 0.72f, 0.38f, 0.16f + 0.12f * Mathf.PerlinNoise(now * 7f, 3f));
            torchGlow.rectTransform.localScale = Vector3.one * (1f + 0.04f * flicker);

            // Ganimet ışını nabız gibi atar
            if (lootBeam.enabled)
            {
                Color b = lootBeam.color;
                b.a = 0.45f + 0.25f * Mathf.Sin(now * 6f);
                lootBeam.color = b;
            }

            // Havada süzülen toz zerrecikleri (fener ışığında)
            if (UnityEngine.Random.value < 0.06f)
            {
                Vector3 p = sceneRoot.position + new Vector3(UnityEngine.Random.Range(-260f, 260f), UnityEngine.Random.Range(-240f, 120f), 0f);
                particles.Spawn(p, new Color(1f, 0.9f, 0.7f, 0.35f), 1, 12f, gravity: 18f, minSize: 3f, maxSize: 6f,
                                lifetime: 2.2f, baseVelocity: new Vector2(UnityEngine.Random.Range(-10f, 10f), 14f), rotate: false);
            }

            // Nadir bulgunun arkasındaki ışık hüzmeleri döner
            if (itemRoot.gameObject.activeSelf) raysImage.rectTransform.Rotate(0f, 0f, -40f * FxTime.Delta);

            // Boss aurası nabız gibi atar
            if (monsterRoot.gameObject.activeSelf && monsterAura.enabled)
            {
                float pulse = 0.35f + 0.25f * Mathf.Sin(now * 4f);
                monsterAura.color = new Color(_auraColor.r, _auraColor.g, _auraColor.b, pulse);
            }

            // Tavandan çakıl: risk arttıkça daha sık (görünür tehlike)
            if (now >= _nextPebbleTime)
            {
                float r = Mathf.Clamp01((float)_risk * 2f);
                _nextPebbleTime = now + Mathf.Lerp(2.5f, 0.2f, r) * UnityEngine.Random.Range(0.6f, 1.4f);
                Vector3 top = TopEdgePoint(UnityEngine.Random.Range(0.1f, 0.9f));
                particles.Spawn(top, new Color(0.45f, 0.4f, 0.36f), 1 + (int)(r * 3), 30f,
                                gravity: -900f, minSize: 4f, maxSize: 10f, lifetime: 1.4f, baseVelocity: new Vector2(0, -60f));
            }
        }

        // ---------------- Durum ----------------

        /// <summary>Yeni tur: sahneyi temizle, ilk duvarı göster.</summary>
        public void ResetForRun(int currentStep, double nextRisk)
        {
            foreach (GameObject rock in _fallenRocks) Destroy(rock);
            _fallenRocks.Clear();
            SetAlpha(darkOverlay, 0f);
            HideItem();
            HideMonster();
            pickaxeRoot.anchoredPosition = _pickaxeRestPos;
            pickaxeRoot.localRotation = Quaternion.Euler(0, 0, restAngle);
            SetDepth(currentStep, nextRisk);
            wallRoot.gameObject.SetActive(true);
            wallRoot.localScale = Vector3.one;
            wallImage.color = Color.white;
        }

        /// <summary>Kazılacak duvarı ve tüneli derinliğe göre ayarla. currentStep = inilmiş son kat.</summary>
        public void SetDepth(int currentStep, double nextRisk)
        {
            _risk = nextRisk;
            _digStep = currentStep + 1;
            _wallSeed = _digStep * 7919;

            int zone = DepthTheme.ZoneIndex(_digStep);
            Color rockColor = DepthTheme.WallColor(_digStep);
            Color oreColor = DepthTheme.OreColor(_digStep);
            int variant = _digStep % 4;  // Her bölgede 4 farklı kaya yüzü dönüşümlü
            int detail = DepthTheme.DetailLevel(_digStep);   // Görsel zenginlik: ilk katlar sade, her 10 katta artar
            wallImage.sprite = MineArt.RockFace($"face_{zone}_{detail}_{variant}", zone * 100 + variant, rockColor, oreColor, detail);

            // Tünel tek tam ekran perspektif görsel (destek çerçeveleri, raylar, fenerler).
            // Eski iç içe halkalardan sadece ilki (tam ekran olan) kullanılır, diğerleri kapalı.
            for (int i = 0; i < tunnelRings.Length; i++)
            {
                tunnelRings[i].enabled = i == 0;
                if (i > 0) continue;
                tunnelRings[i].sprite = MineArt.Tunnel($"tunnel_{zone}_{detail}", zone * 31 + 5, rockColor, oreColor, detail);
                tunnelRings[i].color = Color.white;
            }

            depthLabel.text = $"{_digStep}. KAT";
            crackImage.enabled = false;
            SetAlpha(ceilingCrack, Mathf.Clamp01((float)nextRisk * 1.8f));
            ceilingCrack.sprite = PixelArt.Cracks("ceiling_crack", 4242, 3);
        }

        // ---------------- Vuruş ----------------

        /// <summary>Bir kazma vuruşu. hitIndex: 1'den başlar. Son vuruşta çatlak en büyük hali olur.</summary>
        public IEnumerator Swing(int hitIndex, int hitsPerFloor)
        {
            // 1) Geriye çekil (hızlı)
            yield return RotatePickaxe(restAngle, windupAngle, 0.07f, easeIn: false);

            // 2) Duvara in (çok hızlı) + hafif öne atılma
            pickaxeRoot.anchoredPosition = _pickaxeRestPos + new Vector2(-30f, 20f);
            yield return RotatePickaxe(windupAngle, strikeAngle, 0.05f, easeIn: true);

            // 3) Çarpma anı: çatlak, kıvılcım, moloz, sarsıntı
            Impact(hitIndex, hitsPerFloor);

            // 4) Dinlenme pozisyonuna dön
            pickaxeRoot.anchoredPosition = _pickaxeRestPos;
            yield return RotatePickaxe(strikeAngle, restAngle, 0.1f, easeIn: false);
        }

        private void Impact(int hitIndex, int hitsPerFloor)
        {
            int stage = Mathf.Clamp(Mathf.CeilToInt(3f * hitIndex / hitsPerFloor), 1, 3);
            crackImage.enabled = true;
            crackImage.sprite = PixelArt.Cracks($"crack_{_wallSeed}_{stage}", _wallSeed, stage);

            Vector3 hit = wallRoot.position;
            Color rockColor = DepthTheme.WallColor(_digStep);

            // Kıvılcım: sarı-beyaz, hızlı, kısa ömürlü
            particles.Spawn(hit, new Color(1f, 0.92f, 0.55f), 6 + hitIndex * 2, 520f,
                            gravity: -1500f, minSize: 4f, maxSize: 9f, lifetime: 0.35f);
            // Moloz: kaya renginde, sağa-yukarı fırlar, düşer
            particles.Spawn(hit, rockColor * 0.9f, 5 + hitIndex * 2, 280f,
                            gravity: -1700f, minSize: 8f, maxSize: 18f, lifetime: 0.8f, baseVelocity: new Vector2(160f, 260f));
            // Toz bulutu
            particles.Spawn(hit, new Color(0.8f, 0.75f, 0.65f, 0.35f), 4, 90f,
                            gravity: 0f, minSize: 24f, maxSize: 44f, lifetime: 0.6f, rotate: false);

            StartCoroutine(UIFx.Shake(wallRoot, 6f + hitIndex * 3f, 0.15f, Vector2.zero));
            StartCoroutine(UIFx.Punch(wallRoot, 0.025f, 0.12f));
        }

        /// <summary>Son vuruştan sonra, sonuç gelmeden: duvar ve tavan titrer. Risk yüksekse daha uzun ve sert (gerilim).</summary>
        public IEnumerator Tremble(float duration)
        {
            float r = (float)_risk;
            StartCoroutine(UIFx.Shake(wallRoot, 3f + r * 14f, duration, Vector2.zero));
            float t = 0f;
            while (t < duration)
            {
                t += FxTime.Delta;
                if (UnityEngine.Random.value < 0.25f + r)
                {
                    particles.Spawn(TopEdgePoint(UnityEngine.Random.value), new Color(0.5f, 0.45f, 0.4f), 1, 40f,
                                    gravity: -1000f, minSize: 4f, maxSize: 9f, lifetime: 1f, baseVelocity: new Vector2(0, -80f));
                }
                yield return null;
            }
        }

        // ---------------- Duvar yıkılır, ilerlenir ----------------

        /// <summary>Duvar parçalanır, arkasındaki karanlık delik görünür.</summary>
        public IEnumerator Shatter()
        {
            Color rockColor = DepthTheme.WallColor(_digStep);
            Vector3 center = wallRoot.position;

            // Büyük parçalar her yöne
            particles.Spawn(center, rockColor, 26, 650f, gravity: -1600f, minSize: 16f, maxSize: 40f, lifetime: 0.9f);
            particles.Spawn(center, rockColor * 0.7f, 14, 450f, gravity: -1600f, minSize: 10f, maxSize: 24f, lifetime: 0.8f);
            particles.Spawn(center, new Color(0.85f, 0.8f, 0.7f, 0.4f), 6, 140f, gravity: 0f, minSize: 40f, maxSize: 80f, lifetime: 0.7f, rotate: false);

            wallRoot.gameObject.SetActive(false);
            holeImage.color = new Color(0f, 0f, 0f, 0.95f);
            StartCoroutine(UIFx.Shake(sceneRoot, 10f, 0.2f, Vector2.zero));
            yield return UIFx.Wait(0.12f);
        }

        /// <summary>Tünelde ilerle: halkalar büyüyüp geçer, karanlıktan yeni duvar belirir.</summary>
        public IEnumerator Emerge(int currentStep, double nextRisk)
        {
            SetDepth(currentStep, nextRisk);

            wallRoot.gameObject.SetActive(true);
            const float duration = 0.3f;
            float t = 0f;
            while (t < duration)
            {
                t += FxTime.Delta;
                float k = Mathf.Clamp01(t / duration);
                float ease = 1f - (1f - k) * (1f - k);
                tunnelRoot.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, ease);   // Öne doğru hareket hissi
                wallRoot.localScale = Vector3.one * Mathf.Lerp(0.35f, 1f, ease);      // Yeni duvar uzaktan yaklaşır
                float b = Mathf.Lerp(0.15f, 1f, ease);
                wallImage.color = new Color(b, b, b, 1f);
                SetAlpha(holeImage, 0.95f * (1f - ease));
                yield return null;
            }
            tunnelRoot.localScale = Vector3.one;
            wallRoot.localScale = Vector3.one;
            wallImage.color = Color.white;
            SetAlpha(holeImage, 0f);
        }

        // ---------------- Bulgu ----------------

        /// <summary>Bulgu delikten parlayarak çıkar. Nadirlik arttıkça hüzme büyür, parıltı çoğalır.</summary>
        public IEnumerator RevealFind(FindResult find)
        {
            Color tierColor = RarityStyle.TierColor(find.Tier);
            itemRoot.gameObject.SetActive(true);
            itemRoot.anchoredPosition = Vector2.zero;
            itemImage.sprite = SpriteLibrary.FindIcon(find);
            itemImage.color = SpriteLibrary.FindIconTint(find);

            raysImage.color = new Color(tierColor.r, tierColor.g, tierColor.b, find.Tier >= 3 ? 0.9f : 0.45f);
            raysImage.rectTransform.localScale = Vector3.one * (0.6f + 0.12f * find.Tier);
            glowImage.color = new Color(tierColor.r, tierColor.g, tierColor.b, 0.8f);

            // Fırlayarak çık: küçükten büyüğe, biraz fazla büyüyüp otur
            const float duration = 0.28f;
            float t = 0f;
            while (t < duration)
            {
                t += FxTime.Delta;
                float k = Mathf.Clamp01(t / duration);
                float scale = k < 0.7f ? Mathf.Lerp(0f, 1.25f, k / 0.7f) : Mathf.Lerp(1.25f, 1f, (k - 0.7f) / 0.3f);
                itemRoot.localScale = Vector3.one * scale;
                itemRoot.anchoredPosition = new Vector2(0f, Mathf.Sin(k * Mathf.PI) * 60f);
                yield return null;
            }
            itemRoot.localScale = Vector3.one;
            itemRoot.anchoredPosition = Vector2.zero;

            particles.Spawn(itemRoot.position, tierColor, 8 + find.Tier * 5, 380f + find.Tier * 40f,
                            gravity: -400f, minSize: 5f, maxSize: 12f, lifetime: 0.9f);

            // Destansı (sarı) ve üstü: bulgudan tavana uzanan renkli ganimet ışını (Diablo tarzı)
            if (find.Tier >= 5)
            {
                lootBeam.enabled = true;
                lootBeam.color = new Color(tierColor.r, tierColor.g, tierColor.b, 0.6f);
                lootBeam.rectTransform.localScale = new Vector3(0.6f + 0.1f * find.Tier, 1f, 1f);
                StartCoroutine(UIFx.Shake(sceneRoot, 6f + find.Tier, 0.3f, Vector2.zero));
            }
            yield return UIFx.Wait(0.2f + 0.04f * find.Tier);
        }

        /// <summary>Bulgu, koleksiyon alanına doğru küçülerek uçar.</summary>
        public IEnumerator FlyItemTo(RectTransform target)
        {
            Vector3 start = itemRoot.position;
            const float duration = 0.35f;
            float t = 0f;
            while (t < duration)
            {
                t += FxTime.Delta;
                float k = Mathf.Clamp01(t / duration);
                float ease = k * k;
                itemRoot.position = Vector3.Lerp(start, target.position, ease) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 80f;
                itemRoot.localScale = Vector3.one * Mathf.Lerp(1f, 0.3f, ease);
                yield return null;
            }
            HideItem();
        }

        public void HideItem()
        {
            lootBeam.enabled = false;
            itemRoot.gameObject.SetActive(false);
            itemRoot.localScale = Vector3.one;
            itemRoot.anchoredPosition = Vector2.zero;
        }

        // ---------------- Savaş ----------------

        /// <summary>Elde tutulan kazma (null = başlangıç kazması).</summary>
        public void SetPickaxe(GearItem pickaxe)
        {
            pickaxeImage.sprite = GearArt.PickaxeSprite(pickaxe != null ? pickaxe.baseId : null);
        }

        /// <summary>Elde tutulan silah (null = silahsız, yumrukla dövüşür).</summary>
        public void SetWeapon(GearItem weapon)
        {
            weaponRoot.gameObject.SetActive(weapon != null);
            if (weapon != null) weaponImage.sprite = GearArt.WeaponSprite(weapon);
            _attackFx.SetWeapon(weapon);
        }

        /// <summary>Oyuncu can barı: yeşilden kırmızıya.</summary>
        public void SetPlayerHealth(double hp, double max)
        {
            float k = max > 0 ? Mathf.Clamp01((float)(hp / max)) : 0f;
            playerHpFill.anchorMax = new Vector2(k, 1f);
            playerHpFillImage.color = Color.Lerp(new Color(0.9f, 0.2f, 0.15f), new Color(0.25f, 0.8f, 0.3f), k);
            playerHpText.text = UIStrings.PlayerHealthLabel(hp, max);
        }

        /// <summary>Canavar delikten/karanlıktan çıkar. Boss daha büyük, aurası var, sahne sarsılır.</summary>
        public IEnumerator ShowMonster(MonsterInstance m, bool givesXp)
        {
            monsterRoot.gameObject.SetActive(true);
            monsterImage.sprite = MonsterArt.SpriteFor(m.Definition.id);
            monsterImage.color = Color.white;
            monsterNameText.text = UIStrings.MonsterNameLine(m, givesXp);   // Ad zorluk renginde
            UpdateMonsterHealth(m);

            bool boss = m.Kind != MonsterKind.Normal;
            monsterAura.enabled = boss;
            _auraColor = m.Kind == MonsterKind.Boss ? new Color(1f, 0.3f, 0.2f) : new Color(1f, 0.8f, 0.2f);
            float targetScale = m.Kind == MonsterKind.Boss ? 1.35f : m.Kind == MonsterKind.MiniBoss ? 1.18f : 1f;

            // Karanlıktan yaklaşır: küçük ve kararmış başlar
            const float duration = 0.4f;
            float t = 0f;
            while (t < duration)
            {
                t += FxTime.Delta;
                float k = Mathf.Clamp01(t / duration);
                float ease = 1f - (1f - k) * (1f - k);
                monsterBody.localScale = Vector3.one * Mathf.Lerp(0.3f, targetScale, ease);
                float b = Mathf.Lerp(0.1f, 1f, ease);
                monsterImage.color = new Color(b, b, b, 1f);
                yield return null;
            }
            monsterBody.localScale = Vector3.one * targetScale;
            monsterImage.color = Color.white;

            if (boss)
            {
                // Kükreme
                StartCoroutine(UIFx.Shake(sceneRoot, 22f, 0.5f, Vector2.zero));
                particles.Spawn(monsterBody.position, _auraColor, 30, 600f, gravity: -300f, minSize: 6f, maxSize: 14f, lifetime: 0.9f);
                yield return UIFx.Wait(0.3f);
            }
        }

        public void UpdateMonsterHealth(MonsterInstance m)
        {
            float k = m.MaxHealth > 0 ? Mathf.Clamp01((float)(m.Health / m.MaxHealth)) : 0f;
            monsterHpFill.anchorMax = new Vector2(k, 1f);
            monsterHpText.text = UIStrings.Health(m.Health, m.MaxHealth);
        }

        /// <summary>Silahla vuruş (kazmadan hızlı).</summary>
        public IEnumerator WeaponSwing()
        {
            if (!weaponRoot.gameObject.activeSelf)
            {
                // Silahsız: kısa bir "yumruk" sarsıntısı
                yield return UIFx.Wait(0.08f);
                yield break;
            }
            // Silahın türü (savuruş / ok / büyü) ve kalitesi (nadirlik + güçlendirme) animasyonu belirler
            yield return _attackFx.Play(WeaponRest, WeaponWindup, WeaponStrike);
        }

        /// <summary>Canavar vuruldu: kırmızı parlama, geri tepme, kan/parça.</summary>
        public void MonsterHit(MonsterInstance m, bool crit)
        {
            StartCoroutine(HitFlash());
            StartCoroutine(UIFx.Shake(monsterBody, crit ? 22f : 10f, 0.15f, Vector2.zero));
            Color c = MonsterArt.MainColor(m.Definition.id);
            particles.Spawn(monsterBody.position, c, crit ? 14 : 6, crit ? 520f : 320f,
                            gravity: -1400f, minSize: 6f, maxSize: 14f, lifetime: 0.6f);
            if (crit) particles.Spawn(monsterBody.position, new Color(1f, 0.95f, 0.5f), 10, 600f,
                                      gravity: -800f, minSize: 4f, maxSize: 9f, lifetime: 0.4f);
            UpdateMonsterHealth(m);
        }

        private IEnumerator HitFlash()
        {
            monsterImage.color = HitTint;
            yield return UIFx.Wait(0.07f);
            monsterImage.color = Color.white;
        }

        /// <summary>Canavar saldırır: kameraya doğru atılır (büyür), pençe izi, sahne sarsılır.</summary>
        public IEnumerator MonsterLunge(MonsterInstance m)
        {
            float baseScale = monsterBody.localScale.x;
            const float duration = 0.18f;
            float t = 0f;
            while (t < duration)
            {
                t += FxTime.Delta;
                float k = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
                monsterBody.localScale = Vector3.one * baseScale * (1f + 0.3f * k);
                yield return null;
            }
            monsterBody.localScale = Vector3.one * baseScale;

            StartCoroutine(UIFx.Shake(sceneRoot, m.Kind == MonsterKind.Normal ? 14f : 24f, 0.2f, Vector2.zero));
            // Pençe izi: üç kırmızı çizgi gibi parçacık
            particles.Spawn(monsterBody.position + new Vector3(0f, -60f, 0f), new Color(0.9f, 0.1f, 0.1f), 12, 300f,
                            gravity: -600f, minSize: 6f, maxSize: 16f, lifetime: 0.45f);
        }

        /// <summary>Canavar ölür: beyaz parlar, küçülerek söner, parçalara ayrılır.</summary>
        public IEnumerator MonsterDeath(MonsterInstance m)
        {
            Color c = MonsterArt.MainColor(m.Definition.id);
            float baseScale = monsterBody.localScale.x;
            bool boss = m.Kind != MonsterKind.Normal;
            int burst = boss ? 50 : 22;

            particles.Spawn(monsterBody.position, c, burst, boss ? 900f : 600f, gravity: -1200f, minSize: 8f, maxSize: 22f, lifetime: 0.9f);
            particles.Spawn(monsterBody.position, Color.white, burst / 2, 500f, gravity: -400f, minSize: 4f, maxSize: 10f, lifetime: 0.6f);
            if (boss) StartCoroutine(UIFx.Shake(sceneRoot, 30f, 0.5f, Vector2.zero));

            const float duration = 0.35f;
            float t = 0f;
            while (t < duration)
            {
                t += FxTime.Delta;
                float k = Mathf.Clamp01(t / duration);
                monsterBody.localScale = Vector3.one * baseScale * Mathf.Lerp(1f, 0.2f, k);
                monsterImage.color = new Color(1f, 1f, 1f, 1f - k);
                yield return null;
            }
            HideMonster();
        }

        /// <summary>Boss öfke çubuğu: progress 0..1 bir sonraki öfkeye kalan, stacks mevcut öfke.</summary>
        public void SetEnrage(float progress, int stacks)
        {
            if (!monsterRoot.gameObject.activeSelf) return;
            enrageBar.gameObject.SetActive(true);
            enrageFill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            enrageText.text = stacks > 0 ? $"ÖFKE {stacks}" : "öfke";
        }

        public void HideMonster()
        {
            enrageBar.gameObject.SetActive(false);
            monsterRoot.gameObject.SetActive(false);
            monsterBody.localScale = Vector3.one;
            monsterImage.color = Color.white;
        }

        /// <summary>Canavarlı katta duvar yerine canavar durur; savaş bitince duvar kaybolmuş olarak kalır.</summary>
        public void HideWall()
        {
            wallRoot.gameObject.SetActive(false);
        }

        // ---------------- Göçük ----------------

        /// <summary>Göçük: tavan iner, taşlar sahneyi doldurur, her şey kararır.</summary>
        public IEnumerator Collapse()
        {
            Color rockColor = DepthTheme.WallColor(_digStep);
            const float duration = 0.9f;
            float t = 0f;
            float nextRock = 0f;
            while (t < duration)
            {
                t += FxTime.Delta;
                if (t >= nextRock)
                {
                    nextRock = t + 0.03f;
                    StartCoroutine(FallingRock(rockColor * UnityEngine.Random.Range(0.5f, 1f)));
                }
                SetAlpha(darkOverlay, Mathf.Lerp(0f, 0.85f, t / duration));
                yield return null;
            }
        }

        /// <summary>Tepeden düşüp dipte YIĞILAN taş (göçük yığını oluşur).</summary>
        private IEnumerator FallingRock(Color color)
        {
            var go = new GameObject("Rock", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(fallenRocksRoot, false);
            _fallenRocks.Add(go);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            RectTransform rt = (RectTransform)go.transform;
            Rect area = fallenRocksRoot.rect;
            float size = UnityEngine.Random.Range(40f, 110f);
            rt.sizeDelta = new Vector2(size, size * UnityEngine.Random.Range(0.6f, 1f));
            rt.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 90f));
            float x = UnityEngine.Random.Range(area.xMin, area.xMax);
            float y = area.yMax + size;
            float floor = area.yMin + UnityEngine.Random.Range(0f, area.height * 0.7f);  // Yığın yükselir
            rt.anchoredPosition = new Vector2(x, y);

            float velocity = -200f;
            while (rt != null && rt.anchoredPosition.y > floor)
            {
                velocity -= 3000f * FxTime.Delta;
                rt.anchoredPosition += new Vector2(0f, velocity * FxTime.Delta);
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = new Vector2(x, floor);
        }

        // ---------------- Yardımcılar ----------------

        private IEnumerator RotatePickaxe(float from, float to, float duration, bool easeIn) =>
            Rotate(pickaxeRoot, from, to, duration, easeIn);

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

        /// <summary>Sahnenin üst kenarında, soldan sağa oran (0..1) olarak bir nokta (dünya koordinatı).</summary>
        private Vector3 TopEdgePoint(float fraction)
        {
            var corners = new Vector3[4];
            ((RectTransform)transform).GetWorldCorners(corners);  // 0 sol-alt, 1 sol-üst, 2 sağ-üst, 3 sağ-alt
            return Vector3.Lerp(corners[1], corners[2], fraction);
        }

        private static void SetAlpha(Graphic g, float alpha)
        {
            Color c = g.color;
            c.a = alpha;
            g.color = c;
        }
    }
}
