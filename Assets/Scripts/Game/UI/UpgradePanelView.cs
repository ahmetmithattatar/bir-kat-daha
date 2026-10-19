using System;
using RiskLoop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Turlar arası ekran.
    ///   Sol sütun: son turun sonucu, (ekipman sekmesinde) seçili eşyanın detayı, "Kazıya başla".
    ///   Sağ sütun: sekmeler -> "Yükseltmeler" listesi ya da "Ekipman" (karakter yuvaları + envanter).
    /// (Kaydı sıfırla butonu yanlışlıkla basılmasın diye üst barda: TopBarView.)
    /// </summary>
    public class UpgradePanelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private UpgradeRowView[] rows;
        [SerializeField] private Button startRunButton;

        [Header("Sekmeler")]
        [SerializeField] private Button upgradesTab;
        [SerializeField] private Button equipmentTab;
        [SerializeField] private Image upgradesTabImage;
        [SerializeField] private Image equipmentTabImage;
        [SerializeField] private GameObject upgradesRoot;
        [SerializeField] private GameObject equipmentRoot;

        [Header("Ekipman")]
        [SerializeField] private EquipmentView equipment;
        [SerializeField] private ItemDetailView itemDetail;

        public event Action StartRunClicked;
        public event Action<UpgradeType> BuyClicked;
        public event Action<long> EquipClicked;
        public event Action<long> UnequipClicked;
        public event Action<long> SellClicked;
        public event Action AutoEquipClicked;

        private static readonly Color TabActive = new Color(0.22f, 0.45f, 0.80f);
        private static readonly Color TabInactive = new Color(0.25f, 0.25f, 0.30f);

        private GameSession _session;
        private long _selectedUid = -1;

        private void Awake()
        {
            startRunButton.onClick.AddListener(() => StartRunClicked?.Invoke());

            // Satırların tıklamalarını tek bir olayda topluyoruz
            foreach (UpgradeRowView row in rows)
            {
                row.BuyClicked += type => BuyClicked?.Invoke(type);
            }

            upgradesTab.onClick.AddListener(() => SetTab(equipment: false));
            equipmentTab.onClick.AddListener(() => SetTab(equipment: true));

            equipment.ItemSelected += OnItemSelected;
            equipment.AutoEquipClicked += () => AutoEquipClicked?.Invoke();
            itemDetail.EquipClicked += uid => EquipClicked?.Invoke(uid);
            itemDetail.UnequipClicked += uid => UnequipClicked?.Invoke(uid);
            itemDetail.SellClicked += uid =>
            {
                _selectedUid = -1;  // Satılan eşya artık yok
                SellClicked?.Invoke(uid);
            };

            SetTab(equipment: false);
        }

        public void Refresh(GameSession session)
        {
            _session = session;
            foreach (UpgradeRowView row in rows)
            {
                row.Refresh(session);
            }

            equipment.Refresh(session, _selectedUid);
            itemDetail.Show(session, _selectedUid >= 0 ? session.FindItem(_selectedUid) : null);
        }

        public void ShowResult(string message)
        {
            resultText.text = message;
        }

        public void ShowGearMessage(string message)
        {
            itemDetail.ShowMessage(message);
        }

        private void OnItemSelected(long uid)
        {
            _selectedUid = uid;
            itemDetail.ShowMessage("");
            if (_session != null) Refresh(_session);
        }

        private void SetTab(bool equipment)
        {
            upgradesRoot.SetActive(!equipment);
            equipmentRoot.SetActive(equipment);
            itemDetail.gameObject.SetActive(equipment);
            upgradesTabImage.color = equipment ? TabInactive : TabActive;
            equipmentTabImage.color = equipment ? TabActive : TabInactive;
            if (_session != null) Refresh(_session);
        }
    }
}
