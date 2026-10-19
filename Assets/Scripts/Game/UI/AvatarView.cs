using RiskLoop.Core;
using RiskLoop.Game.Art;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Karakter: kuşanılan eşyalar üstünde görünür. Oyuncu ilerledikçe karakterin tipi değişir:
    ///   - her parça malzemesinin renginde (bez → deri → zincir → plaka → mithril → ejder pulu → hiçlik)
    ///   - yeşil (Seçkin) ve üstü kask boynuz, zırh omuzluk kazanır
    ///   - kırmızı (Mitik) ve üstü zırh pelerin kazanır (rengi nadirlikten)
    ///   - en nadir parçanın renginde nabız gibi atan aura, kırmızı ve morda dönen ışık hüzmeleri
    /// Elde silah, sırtta kazma. Hafifçe nefes alır (yukarı aşağı salınır).
    /// </summary>
    public class AvatarView : MonoBehaviour
    {
        [SerializeField] private RectTransform bobRoot;    // Salınan kısım (karakter + eşyalar)
        [SerializeField] private Image aura;
        [SerializeField] private Image rays;
        [SerializeField] private Image cape;
        [SerializeField] private Image pickaxe;
        [SerializeField] private Image body;
        [SerializeField] private Image pants;
        [SerializeField] private Image boots;
        [SerializeField] private Image armor;
        [SerializeField] private Image pauldrons;
        [SerializeField] private Image gloves;
        [SerializeField] private Image necklace;
        [SerializeField] private Image horns;
        [SerializeField] private Image helmet;
        [SerializeField] private Image weapon;

        private const int FancyTier = 4;      // Yeşil ve üstü: boynuz, omuzluk
        private const int LegendTier = 7;     // Kırmızı ve üstü: pelerin, hüzme

        private Color _auraColor;
        private bool _auraOn;

        private void Awake()
        {
            body.sprite = CharacterArt.Body();
            aura.sprite = PixelArt.Glow();
            rays.sprite = PixelArt.Rays();
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            bobRoot.anchoredPosition = new Vector2(0f, Mathf.Sin(t * 2.2f) * 3f);   // Nefes
            if (_auraOn)
            {
                aura.color = new Color(_auraColor.r, _auraColor.g, _auraColor.b, 0.35f + 0.2f * Mathf.Sin(t * 3f));
            }
            if (rays.enabled) rays.rectTransform.Rotate(0f, 0f, -25f * Time.unscaledDeltaTime);
        }

        public void Refresh(GameSession session)
        {
            if (body.sprite == null) body.sprite = CharacterArt.Body();

            int maxTier = -1;
            SetPiece(session.GetEquipped(GearSlot.Helmet), helmet, "helmet", ref maxTier);
            SetPiece(session.GetEquipped(GearSlot.Armor), armor, "armor", ref maxTier);
            SetPiece(session.GetEquipped(GearSlot.Pants), pants, "pants", ref maxTier);
            SetPiece(session.GetEquipped(GearSlot.Boots), boots, "boots", ref maxTier);
            SetPiece(session.GetEquipped(GearSlot.Gloves), gloves, "gloves", ref maxTier);
            SetPiece(session.GetEquipped(GearSlot.Necklace), necklace, "necklace", ref maxTier);

            // Nadirliğe göre havalı ekler
            GearItem helm = session.GetEquipped(GearSlot.Helmet);
            SetExtra(helm, horns, "horns", helm != null && helm.tier >= FancyTier);
            GearItem chest = session.GetEquipped(GearSlot.Armor);
            SetExtra(chest, pauldrons, "pauldrons", chest != null && chest.tier >= FancyTier);

            // Pelerin: rengi zırhın nadirliğinden
            bool hasCape = chest != null && chest.tier >= LegendTier;
            cape.enabled = hasCape;
            if (hasCape)
            {
                Color c = RarityStyle.TierColor(chest.tier);
                cape.sprite = CharacterArt.Layer("cape", CharacterArt.ToHex(c * 0.8f), CharacterArt.ToHex(c * 0.5f), CharacterArt.ToHex(c));
            }

            // Silah ve kazma
            GearItem w = session.GetEquipped(GearSlot.Weapon);
            weapon.enabled = w != null;
            if (w != null)
            {
                weapon.sprite = GearArt.WeaponSprite(w.baseId);
                maxTier = Mathf.Max(maxTier, w.tier);
            }
            GearItem p = session.GetEquipped(GearSlot.Pickaxe);
            pickaxe.sprite = GearArt.PickaxeSprite(p != null ? p.baseId : null);
            if (p != null) maxTier = Mathf.Max(maxTier, p.tier);
            GearItem ring = session.GetEquipped(GearSlot.Ring);
            if (ring != null) maxTier = Mathf.Max(maxTier, ring.tier);

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
        }

        private static void SetPiece(GearItem item, Image image, string layer, ref int maxTier)
        {
            image.enabled = item != null;
            if (item == null) return;
            GearArt.TryGetMaterial(FantasyTexts.MaterialId(item), out string main, out string shade, out string accent);
            image.sprite = CharacterArt.Layer(layer, main, shade, accent);
            maxTier = Mathf.Max(maxTier, item.tier);
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
