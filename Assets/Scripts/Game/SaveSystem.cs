using System;
using System.IO;
using RiskLoop.Core;
using UnityEngine;

namespace RiskLoop.Game
{
    /// <summary>
    /// PlayerProgress'i JSON dosyası olarak kaydeder ve yükler.
    /// Konum: Application.persistentDataPath/save.json
    ///   (Windows: C:\Users\<kullanıcı>\AppData\LocalLow\<şirket>\<ürün>\save.json)
    /// Bu yol Windows, Steam ve mobilde aynı kodla çalışır.
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "save.json";

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>Kayıt yoksa ya da bozuksa null döner.</summary>
        public static PlayerProgress Load()
        {
            if (!File.Exists(FilePath)) return null;

            try
            {
                return JsonUtility.FromJson<PlayerProgress>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Kayıt okunamadı, yeni oyun başlatılıyor. Hata: {e.Message}");
                return null;
            }
        }

        public static void Save(PlayerProgress progress)
        {
            // Önce geçici dosyaya yaz, sonra yerine taşı.
            // Yazma ortasında oyun kapanırsa eski kayıt bozulmamış kalır.
            string tempPath = FilePath + ".tmp";
            File.WriteAllText(tempPath, JsonUtility.ToJson(progress, true));
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(tempPath, FilePath);
        }

        public static void Delete()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }
    }
}
