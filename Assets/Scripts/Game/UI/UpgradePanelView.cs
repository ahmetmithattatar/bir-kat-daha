using System;
using System.Collections;
using System.Collections.Generic;
using RiskLoop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Turlar arası ekran.
    ///   Sol sütun: son turun sonucu, (ekipman sekmesinde) seçili eşyanın detayı, geçit seçici, "Kazıya başla".
    ///   Sağ sütun: sekmeler -> "Yükseltmeler" | "Ekipman" | "Yeni Çağ".
    /// (Kaydı sıfırla butonu yanlışlıkla basılmasın diye üst barda: TopBarView.)
    /// </summary>
    public class UpgradePanelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private UpgradeRowView[] rows;
        [SerializeField] private Button startRunButton;

        [Header("Geçit (başlangıç katı)")]
        [SerializeField] private Button startStepButton;
        [SerializeField] private TMP_Text startStepLabel;

        [Header("Sekmeler")]
        [SerializeField] private Button upgradesTab;
        [SerializeField] private Button equipmentTab;
        [SerializeField] private Button prestigeTab;
        [SerializeField] private Image upgradesTabImage;
        [SerializeField] private Image equipmentTabImage;
        [SerializeField] private Image prestigeTabImage;
        [SerializeField] private GameObject upgradesRoot;
        [SerializeField] private GameObject equipmentRoot;
        [SerializeField] private GameObject prestigeRoot;

        [Header("Ekipman ve Yeni Çağ")]
        [SerializeField] private EquipmentView equipment;
        [SerializeField] private ItemDetailView itemDetail;
        [SerializeField] private PrestigePanelView prestige;

        public event Action StartRunClicked;
        public event Action<UpgradeType> BuyClicked;
        public event Action<long> EquipClicked;
        public event Action<long> UnequipClicked;
        public event Action<long> SellClicked;
        public event Action<long> EnhanceClicked;
        public event Action AutoEquipClicked;
        public event Action SellAllClicked;
        public event Action PrestigeClicked;
        public event Action<PrestigeType> BuyPrestigeClicked;

        private enum Tab { Upgrades, Equipment, Prestige }

        private static readonly Color TabActive = new Color(0.22f, 0.45f, 0.80f);
        private static readonly Color TabInactive = new Color(0.25f, 0.25f, 0.30f);

        private GameSession _session;
        private long _selectedUid = -1;
        private int _startStep;   // Seçili başlangıç katı (0 = yüzey)

        /// <summary>Oyuncunun seçtiği başlangıç katı (geçit). GameController tura buradan başlatır.</summary>
        public int SelectedStartStep => _startStep;

        private void Awake()
        {
            startRunButton.onClick.AddListener(() => StartRunClicked?.Invoke());
            startStepButton.onClick.AddListener(CycleStartStep);

            // Satırların tıklamalarını tek bir olayda topluyoruz
            foreach (UpgradeRowView row in rows)
            {
                row.BuyClicked += type => BuyClicked?.Invoke(type);
            }

            upgradesTab.onClick.AddListener(() => SetTab(Tab.Upgrades));
            equipmentTab.onClick.AddListener(() => SetTab(Tab.Equipment));
            prestigeTab.onClick.AddListener(() => SetTab(Tab.Prestige));

            equipment.ItemSelected += OnItemSelected;
            equipment.AutoEquipClicked += () => AutoEquipClicked?.Invoke();
            equipment.SellAllClicked += () =>
            {
                _selectedUid = -1;
                SellAllClicked?.Invoke();
            };
            itemDetail.EquipClicked += uid => EquipClicked?.Invoke(uid);
            itemDetail.UnequipClicked += uid => UnequipClicked?.Invoke(uid);
            itemDetail.EnhanceClicked += uid => EnhanceClicked?.Invoke(uid);
            itemDetail.SellClicked += uid =>
            {
                _selectedUid = -1;  // Satılan eşya artık yok
                SellClicked?.Invoke(uid);
            };
            prestige.PrestigeClicked += () => PrestigeClicked?.Invoke();
            prestige.BuyClicked += t => BuyPrestigeClicked?.Invoke(t);

            SetTab(Tab.Upgrades);
        }

        public void Refresh(GameSession session)
        {
            _session = session;
            foreach (UpgradeRowView row in rows)
            {
                row.Refresh(session);
            }

            // Seçili eşya kırıldı/satıldıysa seçimi bırak
            if (_selectedUid >= 0 && session.FindItem(_selectedUid) == null) _selectedUid = -1;

            equipment.Refresh(session, _selectedUid);
            itemDetail.Show(session, _selectedUid >= 0 ? session.FindItem(_selectedUid) : null);
            prestige.Refresh(session);

            // Geçit: seçili kat artık açık değilse (Yeni Çağ sonrası) yüzeye dön
            if (!session.AvailableStartSteps.Contains(_startStep)) _startStep = 0;
            startStepLabel.text = UIStrings.StartFrom(_startStep);
            startStepButton.interactable = session.AvailableStartSteps.Count > 1;
        }

        public void ShowResult(string message)
        {
            resultText.text = message;
        }

        public void ShowGearMessage(string message)
        {
            itemDetail.ShowMessage(message);
        }

        /// <summary>Güçlendirme animasyonu (GameController bekler).</summary>
        public IEnumerator PlayEnhance(EnhanceResult result)
        {
            yield return itemDetail.PlayEnhance(result);
            if (result.Destroyed) _selectedUid = -1;
        }

        private void CycleStartStep()
        {
            if (_session == null) return;
            List<int> steps = _session.AvailableStartSteps;
            int index = steps.IndexOf(_startStep);
            _startStep = steps[(index + 1) % steps.Count];
            startStepLabel.text = UIStrings.StartFrom(_startStep);
        }

        private void OnItemSelected(long uid)
        {
            _selectedUid = uid;
            itemDetail.ShowMessage("");
            if (_session != null) Refresh(_session);
        }

        private void SetTab(Tab tab)
        {
            upgradesRoot.SetActive(tab == Tab.Upgrades);
            equipmentRoot.SetActive(tab == Tab.Equipment);
            prestigeRoot.SetActive(tab == Tab.Prestige);
            itemDetail.gameObject.SetActive(tab == Tab.Equipment);
            upgradesTabImage.color = tab == Tab.Upgrades ? TabActive : TabInactive;
            equipmentTabImage.color = tab == Tab.Equipment ? TabActive : TabInactive;
            prestigeTabImage.color = tab == Tab.Prestige ? TabActive : TabInactive;
            if (_session != null) Refresh(_session);
        }
    }
}
