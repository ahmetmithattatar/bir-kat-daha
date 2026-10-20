using System;
using System.Collections.Generic;
using RiskLoop.Core;
using TMPro;
using UnityEngine;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Tur ekranında büyü çubuğu: mana çubuğu + bilinen büyülerin butonları (kısayol 1-6).
    /// Sınıf seçilmemişse ya da hiç büyü yoksa görünmez.
    /// </summary>
    public class SpellBarView : MonoBehaviour
    {
        [SerializeField] private RectTransform manaFill;
        [SerializeField] private TMP_Text manaText;
        [SerializeField] private SpellButtonView[] buttons;

        public event Action<string> CastClicked;

        private void Awake()
        {
            foreach (SpellButtonView b in buttons) b.Clicked += id => CastClicked?.Invoke(id);
        }

        public void Refresh(GameSession session)
        {
            List<SpellDefinition> known = session.HasClass ? session.KnownSpells : new List<SpellDefinition>();
            gameObject.SetActive(known.Count > 0 && session.CurrentRun != null);
            if (!gameObject.activeSelf) return;

            RunState run = session.CurrentRun;
            manaFill.anchorMax = new Vector2((float)(run.Mana / Math.Max(run.MaxMana, 1.0)), 1f);
            manaText.text = UIStrings.ManaText(run.Mana, run.MaxMana);

            for (int i = 0; i < buttons.Length; i++)
            {
                SpellDefinition s = i < known.Count ? known[i] : null;
                buttons[i].Set(s, s != null && session.CanCast(s), i + 1);
            }
        }

        /// <summary>Kullanılan büyünün butonunu vurgula.</summary>
        public void PunchSpell(string id)
        {
            foreach (SpellButtonView b in buttons)
            {
                if (b.gameObject.activeSelf && b.SpellId == id) b.Punch();
            }
        }
    }
}
