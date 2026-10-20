using System;
using RiskLoop.Core;
using RiskLoop.Game.Art;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Büyü çubuğundaki tek buton: ikon, mana bedeli, kısayol tuşu. Kullanılamıyorsa soluk.
    /// Açıklaması fareyle (ipucu).
    /// </summary>
    public class SpellButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image frame;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text hotkeyText;
        [SerializeField] private TooltipTrigger tooltip;

        public event Action<string> Clicked;

        private static readonly Color ReadyFrame = new Color(0.55f, 0.45f, 1f);
        private static readonly Color DimFrame = new Color(0.22f, 0.22f, 0.28f);

        private string _spellId;

        private void Awake()
        {
            button.onClick.AddListener(() => { if (_spellId != null) Clicked?.Invoke(_spellId); });
        }

        public void Set(SpellDefinition spell, bool canCast, int hotkey)
        {
            gameObject.SetActive(spell != null);
            if (spell == null) return;

            _spellId = spell.id;
            icon.sprite = IconArt.Spell(spell.id);
            icon.color = canCast ? Color.white : new Color(0.45f, 0.45f, 0.5f, 0.8f);
            frame.color = canCast ? ReadyFrame : DimFrame;
            costText.text = ((int)spell.manaCost).ToString();
            hotkeyText.text = hotkey.ToString();
            button.interactable = canCast;
            tooltip.SetText(UIStrings.SpellTooltip(spell, true, hotkey));
        }

        /// <summary>Büyü kullanılınca buton "pat" der.</summary>
        public void Punch()
        {
            StartCoroutine(UIFx.Punch(transform, 0.3f, 0.25f));
        }

        public string SpellId => _spellId;
    }
}
