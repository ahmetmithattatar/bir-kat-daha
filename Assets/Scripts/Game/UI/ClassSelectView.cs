using System;
using RiskLoop.Core;
using RiskLoop.Game.Art;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Sınıf seçim ekranı (ilk girişte ve her Yeni Çağda): 3 kart yan yana (Savaşçı, Büyücü, Okçu).
    /// Her kartta sınıf kıyafetiyle karakter, ikon, ad, açıklama ve "SEÇ" butonu.
    /// Seçim yapılana kadar ekranın üstünde kalır (arkadaki butonlara tıklanamaz).
    /// </summary>
    public class ClassSelectView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private AvatarView[] previews;      // Kart sırası = Cards dizisi
        [SerializeField] private Image[] icons;
        [SerializeField] private TMP_Text[] names;
        [SerializeField] private TMP_Text[] descriptions;
        [SerializeField] private Button[] chooseButtons;

        /// <summary>Kartların sırası.</summary>
        public static readonly HeroClass[] Cards = { HeroClass.Warrior, HeroClass.Mage, HeroClass.Archer };

        public event Action<HeroClass> Chosen;

        public bool IsOpen => canvasGroup.alpha > 0f;

        private void Awake()
        {
            for (int i = 0; i < Cards.Length && i < chooseButtons.Length; i++)
            {
                HeroClass c = Cards[i];
                chooseButtons[i].onClick.AddListener(() => Chosen?.Invoke(c));
            }
            Hide();
        }

        public void Show()
        {
            titleText.text = UIStrings.ClassSelectTitle;
            hintText.text = UIStrings.ClassSelectHint;
            for (int i = 0; i < Cards.Length; i++)
            {
                previews[i].Preview(Cards[i]);
                icons[i].sprite = IconArt.Class(Cards[i]);
                names[i].text = UIStrings.ClassName(Cards[i]).ToUpper();
                descriptions[i].text = UIStrings.ClassDescription(Cards[i]);
            }
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        public void Hide()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }
}
