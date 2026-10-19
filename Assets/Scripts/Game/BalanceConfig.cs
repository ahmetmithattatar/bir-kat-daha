using System.IO;
using RiskLoop.Core;
using UnityEngine;

namespace RiskLoop.Game
{
    /// <summary>
    /// Denge sayılarını Unity Inspector'da düzenlenebilir yapan asset.
    /// Asıl sayılar BalanceData içinde (Core). Bu sınıf sadece bir "kap".
    ///
    /// Inspector'da asset'e sağ tıkla (ya da sağ üstteki ⋮ menüsü):
    ///   - "Export JSON"  -> proje kökünde Balance/balance.json yazar (Python simülasyonu için)
    ///   - "Import JSON"  -> Python'da ayarladığın sayıları geri yükler
    /// </summary>
    [CreateAssetMenu(fileName = "BalanceConfig", menuName = "RiskLoop/Balance Config")]
    public class BalanceConfig : ScriptableObject
    {
        public BalanceData data = new BalanceData();

        // Application.dataPath = ".../My project/Assets", bir üstü proje kökü.
        private static string JsonPath => Path.Combine(Application.dataPath, "..", "Balance", "balance.json");

        [ContextMenu("Export JSON (Balance/balance.json)")]
        private void ExportJson()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(JsonPath));
            File.WriteAllText(JsonPath, JsonUtility.ToJson(data, true));
            Debug.Log($"Denge sayıları dışa aktarıldı: {Path.GetFullPath(JsonPath)}");
        }

        [ContextMenu("Import JSON (Balance/balance.json)")]
        private void ImportJson()
        {
            if (!File.Exists(JsonPath))
            {
                Debug.LogWarning($"Dosya bulunamadı: {Path.GetFullPath(JsonPath)}");
                return;
            }
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Import Balance JSON");
#endif
            JsonUtility.FromJsonOverwrite(File.ReadAllText(JsonPath), data);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            Debug.Log("Denge sayıları JSON'dan yüklendi.");
        }
    }
}
