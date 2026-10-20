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

        public event Action<long> EquipClicked;
        public event Action<long> UnequipClicked;
        public event Action<long> EnhanceClicked;
        public event Action<long> SellClicked;

        private GearItem _item;
        private bool _equipped;

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
            sellButton.onClick.AddListener(() =>
            {
                if (_item != null && !_equipped) SellClicked?.Invoke(_item.uid);
            });
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
                return;
            }

            CombatBalance c = session.Balance.combat;
            _equipped = session.IsEquipped(item);
            GearItem current = _equipped ? null : session.GetEquipped(item.slot);

            frame.color = RarityStyle.TierColor(item.tier);
            icon.enabled = true;
            icon.color = Color.white;
            icon.sprite = GearArt.Icon(item);
            nameText.text = FantasyTexts.GearName(item);
            levelText.text = UIStrings.ItemLevelLine(item, _equipped);
            statsText.text = FantasyTexts.GearStatLines(item, current, c.enhanceStatBonus);

            // Bu eşya ilerlemeyi ne kadar değiştirir? (tek sayı: savaş gücü; kazmada duvar vuruşu)
            int powerNow = session.CurrentPowerLevel;
            string compare = _equipped
                ? $"Savaş gücü: {powerNow}. kat seviyesi"
                : UIStrings.PowerCompare(powerNow, session.PowerLevelWith(item));
            if (item.slot == GearSlot.Pickaxe && !_equipped)
            {
                int step = Mathf.Max(1, session.Progress.bestStep);
                compare += UIStrings.SwingsCompare(session.SwingsWith(null, step), session.SwingsWith(item, step));
            }
            if (session.IsUnlocked(Feature.Enhance)) compare += "\n" + UIStrings.EnhanceRiskNote(item.enhance, c.enhanceDowngradeFrom, c.enhanceBreakFrom);
            compareText.text = compare;

            equipButton.gameObject.SetActive(true);
            equipLabel.text = _equipped ? UIStrings.UnequipButton : UIStrings.EquipButton;

            // Güçlendirme seviyeyle açılır; o zamana kadar buton görünmez (kalabalık yapmasın)
            enhanceButton.gameObject.SetActive(session.IsUnlocked(Feature.Enhance));
            bool maxed = item.enhance >= c.enhanceMax;
            enhanceLabel.text = maxed ? UIStrings.EnhanceMaxed
                : UIStrings.EnhanceButton(item.enhance + 1, session.EnhanceChance(item), session.EnhanceCost(item));
            enhanceButton.interactable = session.CanEnhance(item);

            sellButton.gameObject.SetActive(!_equipped);
            sellLabel.text = UIStrings.SellButton(session.SellValue(item));
        }

        public void ShowMessage(string message)
        {
            messageText.text = message;
        }

        /// <summary>Güçlendirme gerilimi: çerçeve nadirlik renkleri arasında yanıp söner, sonra sonuç patlar.</summary>
        public IEnumerator PlayEnhance(EnhanceResult r)
        {
            if (!r.Attempted) yield break;

            Color baseColor = frame.color;
            const float duration = 1.0f;
            float t = 0f;
            int tick = 0;
            while (t < duration)
            {
                t += FxTime.Delta;
                tick++;
                // Yavaşlayan yanıp sönme: başta hızlı, sona doğru seyrek (rulet hissi)
                if (tick % (2 + (int)(t * 8)) == 0) frame.color = RarityStyle.TierColor(UnityEngine.Random.Range(0, 9));
                messageText.text = $"Güçlendiriliyor{new string('.', 1 + (tick / 10) % 3)}  (%{UIStrings.Pct(r.Chance)})";
                yield return null;
            }
            frame.color = baseColor;

            if (r.Success)
            {
                messageText.text = UIStrings.EnhanceSuccess(r.NewLevel);
                yield return UIFx.Punch(icon.rectTransform, 0.4f, 0.35f);
            }
            else if (r.Destroyed)
            {
                messageText.text = UIStrings.EnhanceBroken;
                float k = 0f;
                while (k < 1f)
                {
                    k += FxTime.Delta * 2f;
                    icon.color = new Color(1f, 0.3f, 0.3f, 1f - k);
                    yield return null;
                }
            }
            else
            {
                messageText.text = r.Downgraded ? UIStrings.EnhanceDowngrade(r.NewLevel) : UIStrings.EnhanceFailSafe;
                yield return UIFx.Shake(icon.rectTransform, 12f, 0.3f, Vector2.zero);
            }
            yield return UIFx.Wait(0.4f);
        }
    }
}
