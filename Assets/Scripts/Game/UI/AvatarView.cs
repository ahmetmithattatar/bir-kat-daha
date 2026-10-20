using System.Collections.Generic;
using RiskLoop.Core;
using RiskLoop.Game.Art;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Karakter: sınıfı, kuşandıkları, eşyaların nadirliği, güçlendirmesi ve Kudret'i GÖRÜNÜR.
    ///   Sınıf     : Büyücü cübbe + sivri şapka, Okçu başlık + sırtta sadak, Savaşçı alın bandı (kask/zırh yokken)
    ///   Parçalar  : malzeme renginde; lacivert+ süsler nadirlik renginde, turuncu+ kontur parlar,
    ///               +7 metal parlar, +10 altın kaplama (GearArt.Decorate)
    ///   Ekler     : yeşil+ kaskta boynuz, zırhta omuzluk; kırmızı+ zırhta pelerin
    ///   Aura      : en nadir parçanın renginde nabız; kırmızı+ dönen hüzme
    ///   Silah     : güçlendirmeyle arkasında büyüyen parıltı
    ///   Kudret    : rütbe yükseldikçe ayak altında ışık halkası ve etrafında yükselen kıvılcımlar
    /// Hafifçe nefes alır (yukarı aşağı salınır).
    /// </summary>
    public class AvatarView : MonoBehaviour
    {
        [SerializeField] private RectTransform bobRoot;    // Salınan kısım (karakter + eşyalar)
        [SerializeField] private Image aura;
        [SerializeField] private Image rays;
        [SerializeField] private Image groundGlow;         // Kudret rütbesi: ayak altı ışık
        [SerializeField] private Image quiver;             // Okçu: sırtta sadak
        [SerializeField] private Image cape;
        [SerializeField] private Image pickaxe;
        [SerializeField] private Image body;
        [SerializeField] private Image robe;               // Büyücü cübbesi (zırh yokken)
        [SerializeField] private Image pants;
        [SerializeField] private Image boots;
        [SerializeField] private Image armor;
        [SerializeField] private Image pauldrons;
        [SerializeField] private Image gloves;
        [SerializeField] private Image necklace;
        [SerializeField] private Image classHat;           // Şapka / başlık / alın bandı (kask yokken)
        [SerializeField] private Image horns;
        [SerializeField] private Image helmet;
        [SerializeField] private Image weaponGlow;         // Silah güçlendirme parıltısı
        [SerializeField] private Image weapon;

        private const int FancyTier = 4;      // Yeşil ve üstü: boynuz, omuzluk
        private const int LegendTier = 7;     // Kırmızı ve üstü: pelerin, hüzme
        private const int MaxSparkles = 14;

        private static readonly Color Gold = new Color(1f, 0.82f, 0.25f);

        private Color _auraColor;
        private bool _auraOn;
        private float _weaponGlowAlpha;
        private Color _weaponGlowColor;
        private float _groundAlpha;

        // Kıvılcımlar: Kudret ve güçlendirmeyle sayısı artar, karakterin etrafında yükselir
        private readonly List<Image> _sparkles = new List<Image>();
        private readonly List<float> _sparklePhase = new List<float>();
        private int _activeSparkles;
        private Color _sparkleColor = Color.white;

        private void Awake()
        {
            body.sprite = CharacterArt.Body();
            aura.sprite = PixelArt.Glow();
            rays.sprite = PixelArt.Rays();
            groundGlow.sprite = PixelArt.Glow();
            weaponGlow.sprite = PixelArt.Glow();
            CreateSparkles();
        }

        private void CreateSparkles()
        {
            var size = ((RectTransform)transform).sizeDelta;
            for (int i = 0; i < MaxSparkles; i++)
            {
                var go = new GameObject($"Sparkle_{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rt = (RectTransform)go.transform;
                rt.sizeDelta = Vector2.one * Mathf.Max(4f, size.x * 0.06f);
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                img.enabled = false;
                _sparkles.Add(img);
                _sparklePhase.Add(Random.value);
            }
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            bobRoot.anchoredPosition = new Vector2(0f, Mathf.Sin(t * 2.2f) * 3f);   // Nefes
            if (_auraOn) aura.color = new Color(_auraColor.r, _auraColor.g, _auraColor.b, 0.35f + 0.2f * Mathf.Sin(t * 3f));
            if (rays.enabled) rays.rectTransform.Rotate(0f, 0f, -25f * Time.unscaledDeltaTime);

            if (weaponGlow.enabled)
            {
                float pulse = 0.75f + 0.25f * Mathf.Sin(t * 4f);
                weaponGlow.color = new Color(_weaponGlowColor.r, _weaponGlowColor.g, _weaponGlowColor.b, _weaponGlowAlpha * pulse);
            }
            if (groundGlow.enabled)
            {
                Color g = groundGlow.color;
                g.a = _groundAlpha * (0.8f + 0.2f * Mathf.Sin(t * 1.7f));
                groundGlow.color = g;
            }
            UpdateSparkles();
        }

        /// <summary>Kıvılcımlar alttan yukarı süzülür, sönerek yeniden başlar.</summary>
        private void UpdateSparkles()
        {
            if (_activeSparkles == 0) return;
            var size = ((RectTransform)transform).sizeDelta;
            for (int i = 0; i < _activeSparkles; i++)
            {
                float p = _sparklePhase[i] + Time.unscaledDeltaTime * (0.25f + 0.1f * (i % 3));
                if (p > 1f) p -= 1f;
                _sparklePhase[i] = p;
                float x = Mathf.Sin(i * 2.39f) * size.x * 0.48f + Mathf.Sin(p * 6.28f + i) * size.x * 0.05f;
                float y = Mathf.Lerp(-size.y * 0.45f, size.y * 0.55f, p);
                _sparkles[i].rectTransform.anchoredPosition = new Vector2(x, y);
                _sparkles[i].color = new Color(_sparkleColor.r, _sparkleColor.g, _sparkleColor.b, Mathf.Sin(p * Mathf.PI) * 0.9f);
            }
        }

        /// <summary>Oyuncunun karakteri: sınıf + kuşanılanlar + Kudret.</summary>
        public void Refresh(GameSession session)
        {
            Draw(session.CurrentClass, session.GetEquipped, session.Kudret);
        }

        /// <summary>Sınıf seçim ekranı için: eşyasız, sadece sınıf kıyafeti.</summary>
        public void Preview(HeroClass heroClass)
        {
            Draw(heroClass, slot => null, 0);
        }

        private void Draw(HeroClass heroClass, System.Func<GearSlot, GearItem> equipped, double kudret)
        {
            if (body.sprite == null) body.sprite = CharacterArt.Body();

            int maxTier = -1;
            int enhanceSum = 0;
            GearItem helm = equipped(GearSlot.Helmet);
            GearItem chest = equipped(GearSlot.Armor);
            SetPiece(helm, helmet, "helmet", ref maxTier, ref enhanceSum);
            SetPiece(chest, armor, "armor", ref maxTier, ref enhanceSum);
            SetPiece(equipped(GearSlot.Pants), pants, "pants", ref maxTier, ref enhanceSum);
            SetPiece(equipped(GearSlot.Boots), boots, "boots", ref maxTier, ref enhanceSum);
            SetPiece(equipped(GearSlot.Gloves), gloves, "gloves", ref maxTier, ref enhanceSum);
            SetPiece(equipped(GearSlot.Necklace), necklace, "necklace", ref maxTier, ref enhanceSum);

            DrawClassOutfit(heroClass, helm, chest);

            // Nadirliğe göre havalı ekler
            SetExtra(helm, horns, "horns", helm != null && helm.tier >= FancyTier);
            SetExtra(chest, pauldrons, "pauldrons", chest != null && chest.tier >= FancyTier);

            // Pelerin: rengi zırhın nadirliğinden
            bool hasCape = chest != null && chest.tier >= LegendTier;
            cape.enabled = hasCape;
            if (hasCape)
            {
                Color c = RarityStyle.TierColor(chest.tier);
                cape.sprite = CharacterArt.Layer("cape", CharacterArt.ToHex(c * 0.8f), CharacterArt.ToHex(c * 0.5f), CharacterArt.ToHex(c));
            }

            // Silah: süslü görsel + güçlendirmeyle büyüyen parıltı
            GearItem w = equipped(GearSlot.Weapon);
            weapon.enabled = w != null;
            weaponGlow.enabled = false;
            if (w != null)
            {
                weapon.sprite = GearArt.WeaponSprite(w);
                maxTier = Mathf.Max(maxTier, w.tier);
                enhanceSum += w.enhance;
                int band = GearArt.EnhanceBand(w.enhance);
                if (band > 0 || w.tier >= 6)
                {
                    weaponGlow.enabled = true;
                    _weaponGlowAlpha = 0.25f + 0.2f * band;
                    _weaponGlowColor = band >= 3 ? Gold : RarityStyle.TierColor(w.tier);
                    weaponGlow.rectTransform.localScale = Vector3.one * (0.8f + 0.2f * band);
                }
            }

            GearItem p = equipped(GearSlot.Pickaxe);
            pickaxe.sprite = GearArt.PickaxeSprite(p != null ? p.baseId : null);
            if (p != null) maxTier = Mathf.Max(maxTier, p.tier);
            GearItem ring = equipped(GearSlot.Ring);
            if (ring != null)
            {
                maxTier = Mathf.Max(maxTier, ring.tier);
                enhanceSum += ring.enhance;
            }

            // Aura: en nadir parçanın renginde (lacivert ve üstü)
            _auraOn = maxTier >= 3;
            aura.enabled = _auraOn;
            if (_auraOn)
            {
                _auraColor = RarityStyle.TierColor(maxTier);
                aura.rectTransform.localScale = Vector3.one * (0.9f + 0.08f * maxTier);
            }
            rays.enabled = maxTier >= LegendTier;
            if (rays.enabled) rays.color = new Color(_auraColor.r, _auraColor.g, _auraColor.b, 0.6f);

            // Kudret rütbesi: ayak altı ışık + kıvılcımlar
            int rank = UIStrings.KudretRank(kudret);
            Color rankColor = UIStrings.KudretRankColor(rank);
            groundGlow.enabled = rank >= 1;
            _groundAlpha = 0.15f + 0.08f * rank;
            groundGlow.color = new Color(rankColor.r, rankColor.g, rankColor.b, _groundAlpha);
            groundGlow.rectTransform.localScale = new Vector3(0.7f + 0.08f * rank, 0.22f + 0.02f * rank, 1f);

            // Kıvılcım sayısı: rütbe + toplam güçlendirme. Çok güçlendirilmiş takım altın kıvılcım saçar
            _activeSparkles = Mathf.Clamp(Mathf.Max(0, rank - 1) * 2 + enhanceSum / 8, 0, MaxSparkles);
            _sparkleColor = enhanceSum >= 40 ? Gold : rankColor;
            for (int i = 0; i < _sparkles.Count; i++) _sparkles[i].enabled = i < _activeSparkles;
        }

        /// <summary>Sınıf kıyafeti: kask/zırh yokken görünür, sınıf her zaman belli olsun.</summary>
        private void DrawClassOutfit(HeroClass heroClass, GearItem helm, GearItem chest)
        {
            robe.enabled = false;
            quiver.enabled = false;
            classHat.enabled = false;

            switch (heroClass)
            {
                case HeroClass.Mage:
                    // Cübbe zırh yokken görünür; zırhın altında da etek gibi kalır (pantolonu kapatmaz)
                    robe.enabled = chest == null;
                    if (robe.enabled) robe.sprite = CharacterArt.Layer("robe", "5A3AA0", "3A2270", "E0C040");
                    if (helm == null)
                    {
                        classHat.enabled = true;
                        classHat.sprite = CharacterArt.Layer("wizard_hat", "5A3AA0", "3A2270", "E0C040");
                    }
                    break;
                case HeroClass.Archer:
                    quiver.enabled = true;
                    quiver.sprite = CharacterArt.Layer("quiver", "8A5A30", "5C3A1A", "E8E8E8");
                    if (helm == null)
                    {
                        classHat.enabled = true;
                        classHat.sprite = CharacterArt.Layer("hood", "3A7A40", "24502A", "C8B89A");
                    }
                    break;
                case HeroClass.Warrior:
                    if (helm == null)
                    {
                        classHat.enabled = true;
                        classHat.sprite = CharacterArt.Layer("headband", "C03030", "801818", "C03030");
                    }
                    break;
            }
        }

        private static void SetPiece(GearItem item, Image image, string layer, ref int maxTier, ref int enhanceSum)
        {
            image.enabled = item != null;
            if (item == null) return;
            GearArt.TryGetMaterial(FantasyTexts.MaterialId(item), out string main, out string shade, out string accent);
            image.sprite = CharacterArt.Layer(layer, main, shade, accent, item.tier, item.enhance);
            maxTier = Mathf.Max(maxTier, item.tier);
            enhanceSum += item.enhance;
        }

        private static void SetExtra(GearItem item, Image image, string layer, bool visible)
        {
            image.enabled = visible;
            if (!visible) return;
            // Ekler nadirlik renginde parlar
            Color c = RarityStyle.TierColor(item.tier);
            image.sprite = CharacterArt.Layer(layer, CharacterArt.ToHex(c), CharacterArt.ToHex(c * 0.6f), CharacterArt.ToHex(c));
        }
    }
}
