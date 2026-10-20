using System;
using RiskLoop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Yetenek ağacındaki tek düğüm: ad + "3/5". Tıklayınca 1 puan harcanır. Açıklaması fareyle.
    /// Renkler durumu bir bakışta söyler:
    ///   koyu gri = sırası kapalı, mavi çerçeve = öğrenilebilir (puan var), dolu yeşil = kısmen, altın = maksimum.
    /// </summary>
    public class TalentNodeView : MonoBehaviour
    {
        [SerializeField] private string talentId;
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image border;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TooltipTrigger tooltip;

        public event Action<string> Clicked;

        private static readonly Color LockedBack = new Color(0.10f, 0.10f, 0.13f, 0.95f);
        private static readonly Color OpenBack = new Color(0.16f, 0.17f, 0.22f, 0.95f);
        private static readonly Color LearnedBack = new Color(0.14f, 0.30f, 0.20f, 0.95f);
        private static readonly Color MaxedBack = new Color(0.38f, 0.30f, 0.08f, 0.95f);
        private static readonly Color CanLearnBorder = new Color(0.45f, 0.75f, 1f);
        private static readonly Color MaxedBorder = new Color(1f, 0.85f, 0.25f);
        private static readonly Color DimBorder = new Color(0.25f, 0.25f, 0.30f);

        private void Awake()
        {
            button.onClick.AddListener(() => Clicked?.Invoke(talentId));
        }

        public void Refresh(GameSession session)
        {
            TalentDefinition t = session.Balance.talents.Find(talentId);
            if (t == null)
            {
                gameObject.SetActive(false);
                return;
            }

            int rank = session.GetTalentRank(t);
            bool open = session.IsTalentRowOpen(t);
            bool maxed = rank >= t.maxRank;
            bool canLearn = session.CanLearnTalent(t);

            nameText.text = UIStrings.TalentName(t.id);
            rankText.text = $"{rank}/{t.maxRank}";
            nameText.color = open ? Color.white : new Color(0.5f, 0.5f, 0.55f);

            background.color = maxed ? MaxedBack : rank > 0 ? LearnedBack : open ? OpenBack : LockedBack;
            border.color = maxed ? MaxedBorder : canLearn ? CanLearnBorder : DimBorder;
            button.interactable = canLearn;

            tooltip.SetText(UIStrings.TalentTooltip(t, rank, open, session.Balance.talents.pointsPerRow));
        }
    }
}
