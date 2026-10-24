using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RiskLoop.EditorTools
{
    /// <summary>
    /// Menü: RiskLoop > Windows Sürümü Al (zip)
    /// Oyunu Windows için derler ve arkadaşına gönderilecek tek bir zip dosyası hazırlar:
    ///   Builds/Windows/BirKatDaha.exe (+ yanındaki veri klasörleri)
    ///   Builds/BirKatDaha_Windows.zip   <- bunu gönder. Arkadaşın zip'i açıp BirKatDaha.exe'ye çift tıklar.
    /// Oyunun pencere ayarları (1600x900, yeniden boyutlandırılabilir, arka planda çalışma) burada bir kez ayarlanır.
    /// </summary>
    public static class BuildTools
    {
        private const string ProductName = "Bir Kat Daha";   // Pencere başlığı ve kayıt klasörü adı
        private const string ExeName = "BirKatDaha";
        private const string CompanyName = "MT";
        private const string ScenePath = "Assets/Scenes/Prototype.unity";
        private const string BuildFolder = "Builds/Windows";
        private const string ZipPath = "Builds/BirKatDaha_Windows.zip";

        [MenuItem("RiskLoop/Windows Sürümü Al (zip)")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Sahne yok", "Önce RiskLoop > Build Prototype Scene ile sahneyi kur.", "Tamam");
                return;
            }
            if (!EditorUtility.DisplayDialog("Windows sürümü",
                    "Oyun Windows için derlenecek ve gönderilecek bir zip hazırlanacak. Birkaç dakika sürebilir.", "Başla", "İptal"))
            {
                return;
            }

            ApplyPlayerSettings();

            string exePath = Path.Combine(BuildFolder, ExeName + ".exe");
            if (Directory.Exists(BuildFolder)) Directory.Delete(BuildFolder, true);   // Eski sürüm kalıntısı kalmasın

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog("Derleme başarısız",
                    $"Sonuç: {report.summary.result}. Ayrıntı için Console penceresine bak.", "Tamam");
                return;
            }

            ZipBuild();
            EditorUtility.RevealInFinder(ZipPath);
            EditorUtility.DisplayDialog("Hazır!",
                $"Windows sürümü hazır:\n{Path.GetFullPath(ZipPath)}\n\nBu zip'i arkadaşına gönder. Zip'i bir klasöre çıkarıp " +
                $"{ExeName}.exe'ye çift tıklaması yeterli.\n(Windows \"bilinmeyen yayıncı\" uyarısı verirse: Ek bilgi > Yine de çalıştır.)",
                "Tamam");
        }

        /// <summary>Oyun adı, şirket, pencere modu ve çözünürlük.</summary>
        private static void ApplyPlayerSettings()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;   // Oyun içi Ayarlar'dan tam ekran yapılabilir
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;                    // Pencere arkada kalınca oyun donmasın
            PlayerSettings.visibleInBackground = true;
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "0.1")
                PlayerSettings.bundleVersion = "0.1.0";
        }

        /// <summary>Derleme klasörünü zip'le. Sadece hata ayıklama için olan klasörler (gönderilmemeli) atlanır.</summary>
        private static void ZipBuild()
        {
            if (File.Exists(ZipPath)) File.Delete(ZipPath);
            string root = Path.GetFullPath(BuildFolder);
            using (ZipArchive zip = ZipFile.Open(ZipPath, ZipArchiveMode.Create))
            {
                foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    string relative = file.Substring(root.Length + 1).Replace('\\', '/');
                    if (relative.Contains("DoNotShip")) continue;   // *_BurstDebugInformation_DoNotShip vb.
                    zip.CreateEntryFromFile(file, $"{ExeName}/{relative}", System.IO.Compression.CompressionLevel.Optimal);
                }
            }
        }
    }
}
