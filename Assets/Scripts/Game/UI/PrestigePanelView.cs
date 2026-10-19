using System;
using RiskLoop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Yeni Çağ sekmesi: çağ ve Yadigar, açıklama, Kadim yükseltmeler, "Yeni Çağ başlat" (iki basış).
    /// </summary>
    public class PrestigePanelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private PrestigeRowView[] rows;
        [SerializeField] private Button prestigeButton;
        [SerializeField] private TMP_Text prestigeLabel;
        [SerializeField] private float confirmSeconds = 3f;

        public event Action PrestigeClicked;
        public event Action<PrestigeType> BuyClicked;

        private float _confirmUntil = -1f;
        private string _buttonText = "";

        private void Awake()
        {
            foreach (PrestigeRowView row in rows) row.BuyClicked += t => BuyClicked?.Invoke(t);
            prestigeButton.onClick.AddListener(OnPrestige);
        }

        private void Update()
        {
            if (_confirmUntil > 0 && Time.unscaledTime > _confirmUntil)
            {
                _confirmUntil = -1f;
                prestigeLabel.text = _buttonText;
            }
        }

        public void Refresh(GameSession session)
        {
            headerText.text = UIStrings.PrestigeHeader(session.Progress.age, session.Progress.relics);
            infoText.text = UIStrings.PrestigeInfo(session.Balance.prestige.minStep, session.Progress.bestStep, session.RelicsForPrestige);
            foreach (PrestigeRowView row in rows) row.Refresh(session);

            _buttonText = UIStrings.PrestigeButton(session.RelicsForPrestige);
            if (_confirmUntil < 0) prestigeLabel.text = _buttonText;
            prestigeButton.interactable = session.CanPrestige;
        }

        private void OnPrestige()
        {
            if (_confirmUntil > 0)
            {
                _confirmUntil = -1f;
                prestigeLabel.text = _buttonText;
                PrestigeClicked?.Invoke();
                return;
            }
            _confirmUntil = Time.unscaledTime + confirmSeconds;
            prestigeLabel.text = UIStrings.PrestigeConfirm;
        }
    }
}
