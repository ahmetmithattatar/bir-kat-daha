using System.Collections.Generic;
using RiskLoop.Core;
using TMPro;
using UnityEngine;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Bu turun bulguları: kart ızgarası. Turda biriken her şey burada görünür,
    /// oyuncu "bu tur ne kadar değerli oldu" sorusunun cevabını bir bakışta görür.
    ///
    /// Aynı tür + aynı nadirlikteki bulgular TEK kartta birikir (×adet). Kartlar en nadirden başlayarak sıralanır.
    /// Kartlar sahnede hazır durur (sahne kurucu oluşturur); burada sadece doldurulur ve gizlenir.
    /// </summary>
    public class FindCollectionView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private FindMiniCardView[] slots;  // Sabit sayıda kutu (yer israfı ve taşma olmasın)

        // Bir grubun özeti
        private struct Group
        {
            public FindType Type;
            public int Tier;
            public int Count;
            public double Total;       // Para/bonus için toplam
            public double SafetyKeep;  // Sağlam kaya için: kalan risk çarpanı (çarpımsal)
            public int SlotKey;        // Ekipmanlar yuvaya göre de ayrılır (diğerlerinde -1)
            public FindResult Sample;  // Gruptaki son bulgu (ikon için)
        }

        private static int SlotKeyOf(FindResult f) => f.Type == FindType.Gear && f.Item != null ? (int)f.Item.slot : -1;

        /// <summary>Izgarayı yeniler. highlight verilirse o bulgunun kartı "pat" diye büyüyüp söner.</summary>
        public void Refresh(RunState run, FindResult? highlight = null)
        {
            List<FindResult> finds = run != null ? run.Finds : new List<FindResult>();
            titleText.text = UIStrings.CollectionTitle(finds.Count);

            List<Group> groups = BuildGroups(finds);

            // Yer yetmezse son kutu "+N daha" olur
            bool overflow = groups.Count > slots.Length;
            int visible = overflow ? slots.Length - 1 : groups.Count;

            for (int i = 0; i < slots.Length; i++)
            {
                FindMiniCardView slot = slots[i];
                if (i < visible)
                {
                    Group g = groups[i];
                    double shown = g.Type == FindType.Safety ? 1.0 - g.SafetyKeep : g.Total;
                    slot.gameObject.SetActive(true);
                    slot.Set(g.Sample, shown, g.Count);

                    if (highlight.HasValue && highlight.Value.Type == g.Type && highlight.Value.Tier == g.Tier &&
                        SlotKeyOf(highlight.Value) == g.SlotKey)
                    {
                        StartCoroutine(UIFx.Punch(slot.transform, 0.25f, 0.3f));
                    }
                }
                else if (overflow && i == slots.Length - 1)
                {
                    slot.gameObject.SetActive(true);
                    slot.SetOverflow(groups.Count - visible);
                }
                else
                {
                    slot.gameObject.SetActive(false);
                }
            }
        }

        private static List<Group> BuildGroups(List<FindResult> finds)
        {
            var groups = new List<Group>();
            foreach (FindResult f in finds)
            {
                int slotKey = SlotKeyOf(f);
                int index = groups.FindIndex(g => g.Type == f.Type && g.Tier == f.Tier && g.SlotKey == slotKey);
                Group group = index >= 0 ? groups[index] : new Group { Type = f.Type, Tier = f.Tier, SafetyKeep = 1.0, SlotKey = slotKey };

                group.Sample = f;
                group.Count++;
                group.Total += f.Value;
                if (f.Type == FindType.Safety) group.SafetyKeep *= 1.0 - f.Value;

                if (index >= 0) groups[index] = group;
                else groups.Add(group);
            }

            // En nadir önce; eşitse tür sırası
            groups.Sort((a, b) => a.Tier != b.Tier ? b.Tier.CompareTo(a.Tier) : a.Type.CompareTo(b.Type));
            return groups;
        }
    }
}
