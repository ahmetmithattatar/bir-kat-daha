using RiskLoop.Core;
using RiskLoop.Game.Art;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Büyü Kitabı sekmesi: sınıfın 6 büyüsü, açılış seviyesine göre. Öğrenilmemiş olanlar "???" (ne olduğu sürpriz).
    /// Her satırda ikon, seviye rozeti ve açıklama.
    /// </summary>
    public class SpellBookView : MonoBehaviour
    {
        [SerializeField] private Image classIcon;
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private Image[] icons;
        [SerializeField] private TMP_Text[] levelBadges;
        [SerializeField] private TMP_Text[] rowTexts;
        [SerializeField] private TooltipTrigger[] tooltips;

        private static readonly Color LockedTint = new Color(0.08f, 0.08f, 0.1f, 1f);   // Siluet

        public void Refresh(GameSession session)
        {
            classIcon.sprite = IconArt.Class(session.CurrentClass);
            headerText.text = UIStrings.SpellBookHeader(session.CurrentClass, session.MaxMana,
                                                        session.Balance.classes.manaPerFloor, session.Balance.classes.manaPerKill);

            var spells = session.ClassSpells;
            for (int i = 0; i < rowTexts.Length; i++)
            {
                bool has = i < spells.Count;
                rowTexts[i].transform.parent.gameObject.SetActive(has);
                if (!has) continue;

                SpellDefinition s = spells[i];
                bool known = session.IsSpellKnown(s);
                icons[i].sprite = IconArt.Spell(s.id);
                icons[i].color = known ? Color.white : LockedTint;   // Öğrenilmemiş: kara siluet (merak)
                levelBadges[i].text = $"Sv {s.unlockLevel}";
                levelBadges[i].color = known ? new Color(0.55f, 0.85f, 1f) : new Color(0.45f, 0.45f, 0.5f);
                rowTexts[i].text = UIStrings.SpellBookRow(s, known);
                tooltips[i].SetText(UIStrings.SpellTooltip(s, known, known ? session.KnownSpells.IndexOf(s) + 1 : 0));
            }
        }
    }
}
