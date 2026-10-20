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
    ///   Sağ sütun: sekmeler -> "Yükseltmeler" | "Yetenekler" | "Ekipman" | "Yeni Çağ".
    /// Sekmeler ve geçit seçici seviyeyle açılır; kilitliyken "Sv X" yazar (oyuncu neyin geleceğini görür, merak eder).
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
        [SerializeField] private TMP_Text nextUnlockText;   // "Sıradaki özellik: Güçlendirme (Sv 6)"

        [Header("Sekmeler")]
        [SerializeField] private Button upgradesTab;
        [SerializeField] private Button equipmentTab;
        [SerializeField] private Button talentsTab;
        [SerializeField] private Button prestigeTab;
        [SerializeField] private Image upgradesTabImage;
        [SerializeField] private Image equipmentTabImage;
        [SerializeField] private Image talentsTabImage;
        [SerializeField] private Image prestigeTabImage;
        [SerializeField] private TMP_Text equipmentTabLabel;
        [SerializeField] private TMP_Text talentsTabLabel;
        [SerializeField] private TMP_Text prestigeTabLabel;
        [SerializeField] private GameObject upgradesRoot;
        [SerializeField] private GameObject equipmentRoot;
        [SerializeField] private GameObject talentsRoot;
        [SerializeField] private GameObject prestigeRoot;

        [Header("Ekipman ve Yeni Çağ")]
        [SerializeField] private EquipmentView equipment;
        [SerializeField] private ItemDetailView itemDetail;
        [SerializeField] private PrestigePanelView prestige;
        [SerializeField] private TalentTreeView talents;

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
        public event Action<string> LearnTalentClicked;
        public event Action RespecClicked;

        private enum Tab { Upgrades, Talents, Equipment, Prestige }

        private static readonly Color TabActive = new Color(0.22f, 0.45f, 0.80f);
        private static readonly Color TabInactive = new Color(0.25f, 0.25f, 0.30f);
        private static readonly Color TabLocked = new Color(0.14f, 0.14f, 0.17f);

        private Tab _tab = Tab.Upgrades;

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
            talentsTab.onClick.AddListener(() => SetTab(Tab.Talents));
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
            talents.LearnClicked += id => LearnTalentClicked?.Invoke(id);
            talents.RespecClicked += () => RespecClicked?.Invoke();

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
            talents.Refresh(session);
            RefreshTabs(session);

            Feature? next = session.NextLockedFeature;
            nextUnlockText.text = UIStrings.NextUnlockLine(next, next.HasValue ? session.UnlockLevel(next.Value) : 0);

            // Geçit: seçili kat artık açık değilse (Yeni Çağ sonrası) yüzeye dön
            if (!session.AvailableStartSteps.Contains(_startStep)) _startStep = 0;
            startStepLabel.text = UIStrings.StartFrom(_startStep);
            startStepButton.gameObject.SetActive(session.IsUnlocked(Feature.Checkpoints));
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

        public void ShowTalentMessage(string message)
        {
            talents.ShowMessage(message);
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
            // Kilitli sekmeye geçilmez
            if (_session != null && !IsTabUnlocked(_session, tab)) return;
            _tab = tab;
            upgradesRoot.SetActive(tab == Tab.Upgrades);
            talentsRoot.SetActive(tab == Tab.Talents);
            equipmentRoot.SetActive(tab == Tab.Equipment);
            prestigeRoot.SetActive(tab == Tab.Prestige);
            itemDetail.gameObject.SetActive(tab == Tab.Equipment);
            if (_session != null) Refresh(_session);
        }

        private static bool IsTabUnlocked(GameSession session, Tab tab)
        {
            switch (tab)
            {
                case Tab.Talents: return session.IsUnlocked(Feature.Talents);
                case Tab.Equipment: return session.IsUnlocked(Feature.Equipment);
                case Tab.Prestige: return session.IsUnlocked(Feature.Prestige);
                default: return true;
            }
        }

        /// <summary>Sekme renkleri ve etiketleri. Kilitli sekme "Sv X" der; puan bekleyen yetenek sekmesi "(3)" der.</summary>
        private void RefreshTabs(GameSession session)
        {
            // Yeni Çağ sonrası ya da kayıt sıfırlanınca açık sekme kilitlenmiş olabilir
            if (!IsTabUnlocked(session, _tab)) SetTab(Tab.Upgrades);

            upgradesTabImage.color = _tab == Tab.Upgrades ? TabActive : TabInactive;
            SetTabVisual(session, Tab.Talents, Feature.Talents, talentsTabImage, talentsTabLabel,
                         session.TalentPointsAvailable > 0 ? $"{UIStrings.TabTalents} <color=#FFE03A>({session.TalentPointsAvailable})</color>" : UIStrings.TabTalents);
            SetTabVisual(session, Tab.Equipment, Feature.Equipment, equipmentTabImage, equipmentTabLabel, UIStrings.TabEquipment);
            SetTabVisual(session, Tab.Prestige, Feature.Prestige, prestigeTabImage, prestigeTabLabel, UIStrings.TabPrestige);
        }

        private void SetTabVisual(GameSession session, Tab tab, Feature feature, Image image, TMP_Text label, string title)
        {
            bool unlocked = session.IsUnlocked(feature);
            image.color = !unlocked ? TabLocked : _tab == tab ? TabActive : TabInactive;
            label.text = unlocked ? title : $"<color=#666677>{title} ({UIStrings.LockedShort(session.UnlockLevel(feature))})</color>";
        }
    }
}
