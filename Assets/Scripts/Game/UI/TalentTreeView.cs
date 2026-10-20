using System;
using RiskLoop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Yetenekler sekmesi: 3 dal (Cüce Madenci | Kılıç Ustası | Ejder Avcısı), her dalda 4 sıra.
    /// Bir dala 5 puan harcayınca o dalın sonraki sırası açılır; en alttaki tek düğüm dalın "imza" yeteneği.
    /// Üstte kalan puan, altta sıfırlama (ücretli, farklı build denemek için).
    /// </summary>
    public class TalentTreeView : MonoBehaviour
    {
        [SerializeField] private TMP_Text pointsText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text[] branchHeaders;   // TalentBranch sırasıyla
        [SerializeField] private TalentNodeView[] nodes;
        [SerializeField] private Button respecButton;
        [SerializeField] private TMP_Text respecLabel;
        [SerializeField] private GameObject lockedOverlay;   // Yetenekler açılmadıysa
        [SerializeField] private TMP_Text lockedText;

        public event Action<string> LearnClicked;
        public event Action RespecClicked;

        private void Awake()
        {
            foreach (TalentNodeView node in nodes) node.Clicked += id => LearnClicked?.Invoke(id);
            respecButton.onClick.AddListener(() => RespecClicked?.Invoke());
        }

        public void Refresh(GameSession session)
        {
            bool unlocked = session.IsUnlocked(Feature.Talents);
            lockedOverlay.SetActive(!unlocked);
            lockedText.text = UIStrings.TalentsLocked(session.UnlockLevel(Feature.Talents));

            pointsText.text = UIStrings.TalentPoints(session.TalentPointsAvailable, session.TalentPointsSpent);
            for (int i = 0; i < branchHeaders.Length; i++)
            {
                var branch = (TalentBranch)i;
                branchHeaders[i].text = $"<b>{UIStrings.BranchName(branch)}</b>  ({session.PointsInBranch(branch)})\n" +
                                        $"<size=70%><color=#9AA0B4>{UIStrings.BranchHint(branch)}</color></size>";
            }
            foreach (TalentNodeView node in nodes) node.Refresh(session);

            respecLabel.text = UIStrings.RespecButton(session.RespecCost);
            respecButton.interactable = session.CanRespec;
        }

        public void ShowMessage(string message)
        {
            messageText.text = message;
        }
    }
}
