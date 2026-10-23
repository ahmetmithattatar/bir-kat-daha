using System;
using System.Collections;
using RiskLoop.Core;
using RiskLoop.Game.Art;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Seçili eşyanın detayı: büyük ikon, ad (nadirlik renginde), taban/seviye, statlar.
    /// Envanterdeki eşyada: kuşanılıya göre stat farkı ve "savaş gücün X → Y" (eşyanın ilerlemeye etkisi tek sayıda).
    /// Butonlar: Kuşan / Çıkar, Güçlendir (+N, şans, ücret), Sat.
    /// </summary>
    public class ItemDetailView : MonoBehaviour
    {
        [SerializeField] private Image frame;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text compareText;   // Savaş gücü / duvar vuruşu karşılaştırması + güçlendirme riski
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button equipButton;
        [SerializeField] private TMP_Text equipLabel;
        [SerializeField] private Button enhanceButton;
        [SerializeField] private TMP_Text enhanceLabel;
        [SerializeField] private Button sellButton;
        [SerializeField] private TMP_Text sellLabel;
        [SerializeField] private Image equipIcon;
        [SerializeField] private Image enhanceIcon;
        [SerializeField] private Image sellIcon;
        [Tooltip("Değerli eşyayı satmak için ikinci basışa tanınan süre (sn).")]
        [SerializeField] private float sellConfirmSeconds = 3f;

        [Header("Güçlendirme görseli")]
        [SerializeField] private Image iconGlow;          // İkonun arkasında, güçlendirmeyle büyüyen parıltı
        [SerializeField] private TMP_Text enhanceBadge;   // "+7"
        [SerializeField] private FxLayerView fx;          // Kıvılcım, flaş, başlık

        public event Action<long> EquipClicked;
        public event Action<long> UnequipClicked;
        public event Action<long> EnhanceClicked;
        public event Action<long> SellClicked;

        private GearItem _item;
        private bool _equipped;
        private bool _sellNeedsConfirm;
        private float _sellConfirmUntil = -1f;
        private string _sellText = "";
        private float _glowAlpha;
        private Color _glowColor;

        private static readonly Color Gold = new Color(1f, 0.82f, 0.25f);
        private static readonly Color Spark = new Color(1f, 0.6f, 0.2f);

        private void Awake()
        {
            equipButton.onClick.AddListener(() =>
            {
                if (_item == null) return;
                if (_equipped) UnequipClicked?.Invoke(_item.uid);
                else EquipClicked?.Invoke(_item.uid);
            });
            enhanceButton.onClick.AddListener(() =>
            {
                if (_item != null) EnhanceClicked?.Invoke(_item.uid);
            });
            sellButton.onClick.AddListener(OnSell);
            iconGlow.sprite = PixelArt.Glow();

            // Butonların ne yaptığı resimden de belli olsun (yanlışlıkla satmayı önler)
            sellIcon.sprite = IconArt.Get("btn_sell");
            enhanceIcon.sprite = IconArt.Get("btn_enhance");
        }

        /// <summary>
        /// Sat: değerli eşyada (yeşil+, güçlendirilmiş ya da kuşanılandan iyi) iki basış ister:
        /// ilk basış "Emin misin?" der, birkaç saniye içinde tekrar basılırsa satar.
        /// </summary>
        private void OnSell()
        {
            if (_item == null || _equipped) return;
            if (_sellNeedsConfirm && Time.unscaledTime > _sellConfirmUntil)
            {
                _sellConfirmUntil = Time.unscaledTime + sellConfirmSeconds;
                sellLabel.text = UIStrings.SellConfirm;
                return;
            }
            _sellConfirmUntil = -1f;
            SellClicked?.Invoke(_item.uid);
        }

        public void Show(GameSession session, GearItem item)
        {
            _item = item;
            if (item == null)
            {
                frame.color = new Color(0.28f, 0.28f, 0.33f);
                icon.enabled = false;
                nameText.text = "";
                levelText.text = "";
                compareText.text = "";
                statsText.text = UIStrings.NoItemSelected;
                equipButton.gameObject.SetActive(false);
                enhanceButton.gameObject.SetActive(false);
                sellButton.gameObject.SetActive(false);
                SetEnhanceVisuals(null);
                return;
            }

            CombatBalance c = session.Balance.combat;
            _equipped = session.IsEquipped(item);
            GearItem current = _equipped ? null : session.GetEquipped(item.slot);

            frame.color = RarityStyle.TierColor(item.tier);
            icon.enabled = true;
            icon.color = Color.white;
            icon.sprite = GearArt.Icon(item);
            SetEnhanceVisuals(item);
            nameText.text = FantasyTexts.GearName(item);
            levelText.text = UIStrings.ItemLevelLine(item, _equipped);
            statsText.text = FantasyTexts.GearStatLines(item, current, c.enhanceStatBonus);

            // Bu eşya ne kadar güçlü? Tek sayı: Kudret (ve kuşanırsan toplam Kudret'in nasıl değişeceği; kazmada duvar vuruşu)
            string compare = UIStrings.KudretCompare(session.KudretOf(item), session.Kudret, session.KudretWith(item), _equipped);
            if (item.slot == GearSlot.Pickaxe && !_equipped)
            {
                int step = Mathf.Max(1, session.Progress.bestStep);
                compare += UIStrings.SwingsCompare(session.SwingsWith(null, step), session.SwingsWith(item, step));
            }
            if (session.IsUnlocked(Feature.Enhance)) compare += "\n" + UIStrings.EnhanceRiskNote(item.enhance, c.enhanceDowngradeFrom, c.enhanceBreakFrom);
            compareText.text = compare;

            equipButton.gameObject.SetActive(true);
            equipLabel.text = _equipped ? UIStrings.UnequipButton : UIStrings.EquipButton;
            equipIcon.sprite = IconArt.Get(_equipped ? "btn_unequip" : "btn_equip");
            // Başka sınıfın silahı: kuşanılamaz (satılabilir)
            bool usable = _equipped || session.CanUseItem(item);
            equipButton.interactable = usable;
            if (!usable)
            {
                GearBaseDefinition def = session.Balance.combat.FindGearBase(item.baseId);
                if (def != null) compareText.text += "\n" + UIStrings.WrongClassWeapon(def.heroClass);
            }

            // Güçlendirme seviyeyle açılır; o zamana kadar buton görünmez (kalabalık yapmasın)
            enhanceButton.gameObject.SetActive(session.IsUnlocked(Feature.Enhance));
            bool maxed = item.enhance >= c.enhanceMax;
            enhanceLabel.text = maxed ? UIStrings.EnhanceMaxed
                : UIStrings.EnhanceButton(item.enhance + 1, session.EnhanceChance(item), session.EnhanceCost(item));
            enhanceButton.interactable = session.CanEnhance(item);

            sellButton.gameObject.SetActive(!_equipped);
            _sellText = UIStrings.SellButton(session.SellValue(item));
            sellLabel.text = _sellText;
            // Değerli eşya: satış iki basış ister
            _sellNeedsConfirm = item.tier >= 4 || item.enhance > 0 || session.KudretWith(item) > session.Kudret;
            _sellConfirmUntil = -1f;
        }

        public void ShowMessage(string message)
        {
            messageText.text = message;
        }

        /// <summary>Güçlendirme gerilimi: çerçeve nadirlik renkleri arasında yanıp söner, sonra sonuç patlar.</summary>
        /// <summary>
        /// Güçlendirme sahnesi (ödüllendirici olsun diye):
        ///   1) Örs vuruşları: ikon sarsılır, kıvılcım saçar; seviye yükseldikçe vuruş sayısı ve gerilim artar
        ///   2) Başarı : ikon YENİ görünüşüne geçer (+4 altın parıltı, +7 cilalı metal, +10 altın kaplama), büyük "+N" patlar,
        ///               nadirlik renginde patlama, eşik seviyelerinde (4, 7, 10) ekranda başlık
        ///   3) Başarısız: gri sarsıntı, seviye düştüyse kırmızı "-1"
        ///   4) Kırıldı : parçalara ayrılır
        /// </summary>
        public IEnumerator PlayEnhance(EnhanceResult r)
        {
            if (!r.Attempted || _item == null) yield break;

            Color baseColor = frame.color;
            Color rarity = RarityStyle.TierColor(_item.tier);
            int strikes = 2 + Mathf.Min(_item.enhance, 9) / 3;   // Yüksek seviyede daha uzun gerilim

            // 1) Örs vuruşları: aralar uzar (gerilim), her vuruşta kıvılcım
            for (int i = 0; i < strikes; i++)
            {
                messageText.text = $"Güçlendiriliyor{new string('.', 1 + i % 3)}  (%{UIStrings.Pct(r.Chance)})";
                frame.color = Color.white;
                StartCoroutine(UIFx.Punch(icon.rectTransform, 0.25f, 0.18f));
                if (fx != null)
                {
                    fx.Burst(icon.rectTransform, Spark, 10 + i * 3, 420f + i * 60f);
                    fx.Shake(3f + i, 0.08f);
                }
                yield return UIFx.Wait(0.08f);
                frame.color = baseColor;
                yield return UIFx.Wait(0.18f + 0.07f * i);
            }

            if (r.Success)
            {
                // 2) Başarı: yeni görünüş + büyük "+N"
                icon.sprite = GearArt.Icon(_item);
                SetEnhanceVisuals(_item);
                messageText.text = UIStrings.EnhanceSuccess(r.NewLevel);
                enhanceBadge.transform.localScale = Vector3.one * 2.2f;
                StartCoroutine(ScaleTo(enhanceBadge.transform, 1f, 0.35f));
                StartCoroutine(UIFx.Punch(icon.rectTransform, 0.5f, 0.4f));
                if (fx != null)
                {
                    Color burst = GearArt.EnhanceBand(r.NewLevel) >= 3 ? Gold : rarity;
                    fx.Burst(icon.rectTransform, burst, 30 + r.NewLevel * 4, 700f);
                    fx.Burst(icon.rectTransform, Gold, 14, 450f);
                    fx.Flash(burst, 0.15f + 0.02f * r.NewLevel, 0.45f);
                    fx.FloatText($"+{r.NewLevel}!", Gold, 60f + r.NewLevel * 3f, icon.rectTransform);
                    string milestone = UIStrings.EnhanceMilestone(r.NewLevel);
                    if (milestone != null)
                    {
                        fx.Banner(milestone, Gold, 0.8f);
                        fx.Shake(10f + r.NewLevel, 0.35f);
                    }
                }
                yield return UIFx.Wait(0.6f);
            }
            else if (r.Destroyed)
            {
                // 4) Kırıldı: parçalara ayrılır
                messageText.text = UIStrings.EnhanceBroken;
                if (fx != null)
                {
                    fx.Burst(icon.rectTransform, new Color(0.55f, 0.55f, 0.6f), 40, 800f);
                    fx.Burst(icon.rectTransform, rarity, 20, 600f);
                    fx.Flash(new Color(0.9f, 0.15f, 0.1f), 0.4f, 0.6f);
                    fx.Banner(UIStrings.EnhanceBrokenTitle, new Color(1f, 0.3f, 0.25f), 0.8f);
                    fx.Shake(22f, 0.5f);
                }
                float k = 0f;
                while (k < 1f)
                {
                    k += FxTime.Delta * 2f;
                    icon.color = new Color(1f, 0.3f, 0.3f, 1f - k);
                    icon.rectTransform.localScale = Vector3.one * (1f + 0.6f * k);
                    yield return null;
                }
                icon.rectTransform.localScale = Vector3.one;
            }
            else
            {
                // 3) Başarısız
                messageText.text = r.Downgraded ? UIStrings.EnhanceDowngrade(r.NewLevel) : UIStrings.EnhanceFailSafe;
                if (r.Downgraded)
                {
                    icon.sprite = GearArt.Icon(_item);
                    SetEnhanceVisuals(_item);
                }
                if (fx != null)
                {
                    fx.Burst(icon.rectTransform, new Color(0.45f, 0.45f, 0.5f), 16, 380f);
                    if (r.Downgraded) fx.FloatText("-1", new Color(1f, 0.35f, 0.3f), 52f, icon.rectTransform);
                }
                yield return UIFx.Shake(icon.rectTransform, 12f, 0.3f, Vector2.zero);
            }
            yield return UIFx.Wait(0.3f);
        }

        /// <summary>İkonun arkasındaki parıltı ve "+N" rozeti (kademeyle büyür).</summary>
        private void SetEnhanceVisuals(GearItem item)
        {
            int band = item != null ? GearArt.EnhanceBand(item.enhance) : 0;
            enhanceBadge.text = item != null && item.enhance > 0 ? $"+{item.enhance}" : "";
            enhanceBadge.color = band >= 3 ? Gold : band >= 2 ? new Color(1f, 0.95f, 0.7f) : Color.white;
            _glowAlpha = band == 0 ? 0f : 0.2f + 0.18f * band;
            _glowColor = band >= 3 ? Gold : item != null ? RarityStyle.TierColor(item.tier) : Color.white;
            iconGlow.enabled = _glowAlpha > 0f;
            iconGlow.rectTransform.localScale = Vector3.one * (0.9f + 0.15f * band);
        }

        private void Update()
        {
            // Satış onayı süresi dolduysa buton normale döner
            if (_sellConfirmUntil > 0f && Time.unscaledTime > _sellConfirmUntil)
            {
                _sellConfirmUntil = -1f;
                sellLabel.text = _sellText;
            }
            if (!iconGlow.enabled) return;
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 3.5f);
            iconGlow.color = new Color(_glowColor.r, _glowColor.g, _glowColor.b, _glowAlpha * pulse);
        }

        private static IEnumerator ScaleTo(Transform t, float target, float duration)
        {
            Vector3 start = t.localScale;
            float k = 0f;
            while (k < 1f)
            {
                k += FxTime.Delta / duration;
                t.localScale = Vector3.Lerp(start, Vector3.one * target, 1f - (1f - k) * (1f - k));
                yield return null;
            }
            t.localScale = Vector3.one * target;
        }
    }
}
