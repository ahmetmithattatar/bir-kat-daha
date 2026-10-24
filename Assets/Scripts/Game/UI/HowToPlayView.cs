using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Nasıl oynanır: birkaç sayfalık kısa rehber (Önceki / Sonraki / Anladım). İlk oyunda otomatik açılır.
    /// Metinler UIStrings.HowToPlayPages içinde.
    /// </summary>
    public class HowToPlayView : MenuPanel
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text pageText;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text nextLabel;
        [SerializeField] private Button closeButton;

        public event Action Closed;

        private int _page;

        protected override void Awake()
        {
            base.Awake();
            prevButton.onClick.AddListener(() => ShowPage(_page - 1));
            nextButton.onClick.AddListener(() =>
            {
                if (_page >= UIStrings.HowToPlayPages.Length - 1) Finish();
                else ShowPage(_page + 1);
            });
            closeButton.onClick.AddListener(Finish);
        }

        public override void Open()
        {
            base.Open();
            ShowPage(0);
        }

        private void ShowPage(int page)
        {
            int count = UIStrings.HowToPlayPages.Length;
            _page = Mathf.Clamp(page, 0, count - 1);
            titleText.text = UIStrings.HowToPlayPages[_page].title;
            bodyText.text = UIStrings.HowToPlayPages[_page].body;
            pageText.text = $"{_page + 1} / {count}";
            prevButton.interactable = _page > 0;
            nextLabel.text = _page >= count - 1 ? UIStrings.HowToPlayDone : UIStrings.HowToPlayNext;
        }

        private void Finish()
        {
            GameSettings.TutorialSeen = true;
            Close();
            Closed?.Invoke();
        }
    }
}
