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

        // Oyunun eski adı. Ad değişince kayıt klasörü de değişti (LocalLow/MT/RiskLoop -> LocalLow/MT/Bir Kat Daha).
        private const string OldProductFolder = "RiskLoop";

        /// <summary>
        /// Eski addaki klasörde kayıt varsa ve yenisinde yoksa kaydı yeni yere taşır (ilerleme kaybolmasın).
        /// Eski dosya ".migrated" diye yeniden adlandırılır: kayıt sıfırlanınca eski kayıt geri gelmez.
        /// </summary>
        private static void MigrateOldSave()
        {
            if (File.Exists(FilePath)) return;
            try
            {
                string parent = Path.GetDirectoryName(Application.persistentDataPath);
                if (string.IsNullOrEmpty(parent)) return;
                string oldPath = Path.Combine(parent, OldProductFolder, FileName);
                if (oldPath == FilePath || !File.Exists(oldPath)) return;
                Directory.CreateDirectory(Application.persistentDataPath);
                File.Copy(oldPath, FilePath);
                File.Move(oldPath, oldPath + ".migrated");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Eski kayıt taşınamadı: {e.Message}");
            }
        }

        /// <summary>Kayıt yoksa ya da bozuksa null döner.</summary>
        public static PlayerProgress Load()
        {
            MigrateOldSave();
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

        /// <summary>Kayıt dosyası var mı? (Ana menüde "Devam Et" görünsün mü.)</summary>
        public static bool Exists
        {
            get
            {
                MigrateOldSave();
                return File.Exists(FilePath);
            }
        }

        public static void Delete()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }
    }
}
