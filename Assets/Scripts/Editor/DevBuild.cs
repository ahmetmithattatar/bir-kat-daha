using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RiskLoop.EditorTools
{
    /// <summary>
    /// Geliştirici aracı: otomatik ekran görüntüsü için test sürümü. Unity'yi komut satırından (batchmode) çalıştırmak için:
    ///   Unity.exe -batchmode -quit -projectPath &lt;proje&gt; -executeMethod RiskLoop.EditorTools.DevBuild.BuildAutoShot -devOut &lt;klasör&gt;
    /// Sahneyi kurar ve &lt;klasör&gt;/Game.exe üretir. Sonra: Game.exe -autoshot &lt;görüntü klasörü&gt; (bkz. DevAutoShot).
    /// Ürün adı farklıdır ("... AutoTest"): gerçek oyunun kaydına ve ayarlarına dokunmaz.
    /// </summary>
    public static class DevBuild
    {
        public static void BuildAutoShot()
        {
            string outDir = Arg("-devOut") ?? "Builds/AutoTest";
            PrototypeSceneBuilder.BuildScene();

            string oldName = PlayerSettings.productName;
            PlayerSettings.productName = "Bir Kat Daha AutoTest";
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.runInBackground = true;
            try
            {
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/Scenes/Prototype.unity" },
                    locationPathName = System.IO.Path.Combine(outDir, "Game.exe"),
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None,
                });
                Debug.Log($"AutoShot derlemesi: {report.summary.result}");
                if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
            }
            finally
            {
                PlayerSettings.productName = oldName;
            }
        }

        private static string Arg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
