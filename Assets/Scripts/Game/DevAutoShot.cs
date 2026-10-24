using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game
{
    /// <summary>
    /// Geliştirici aracı: oyun komut satırından "-autoshot &lt;klasör&gt;" ile açılırsa kendi kendine oynar ve ekran görüntüsü alır,
    /// sonra kapanır. Arayüz değişikliklerini (okunabilirlik, çakışma, taşma) bir insan oynamadan görmek için.
    /// Normal oyunda hiçbir şey yapmaz (argüman yoksa nesne bile oluşmaz).
    ///
    /// Akış: ana menü → yeni oyun → nasıl oynanır → yükseltme ekranı → sınıf seçimi → tur (kazar, açılan pencereleri kapatır,
    /// belli aralıklarla görüntü alır) → duraklatma menüsü → ayarlar → çıkış.
    /// Butonlara isimleriyle basar (sahne kurucunun verdiği adlar: "NewGameButton", "AdvanceButton" ...).
    /// </summary>
    public class DevAutoShot : MonoBehaviour
    {
        public static string Folder { get; private set; }
        public static bool Active => Folder != null;

        private static bool _dismiss;
        private int _shot;

        /// <summary>Açılır pencereler (bulgu, seviye) "kapat" basışı için bunu sorar; true bir kez döner.</summary>
        public static bool ConsumeDismiss()
        {
            if (!_dismiss) return false;
            _dismiss = false;
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            Folder = null;
            _dismiss = false;
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-autoshot") Folder = args[i + 1];
            if (!Active) return;

            Directory.CreateDirectory(Folder);
            var go = new GameObject("DevAutoShot");
            DontDestroyOnLoad(go);
            go.AddComponent<DevAutoShot>();
        }

        private IEnumerator Start()
        {
            yield return Wait(2f);
            yield return Shot("menu");

            Click("NewGameButton");
            yield return Wait(1f);
            yield return Shot("howtoplay");
            for (int i = 0; i < 8 && Click("CloseButton"); i++) yield return Wait(0.3f);
            yield return Wait(0.5f);
            yield return Shot("upgrades");

            Click("StartRunButton");
            yield return Wait(1f);
            yield return Shot("class_select");
            Click("ChooseButton");
            yield return Wait(1f);
            Click("StartRunButton");
            yield return Wait(1.5f);
            yield return Shot("run_start");

            // Kaz: düğmeye bas, pencere varsa görüntüsünü al ve kapat, belli aralıklarla görüntü al
            float end = Time.realtimeSinceStartup + 75f;   // Seviye 2 (yetenekler) açılacak kadar oyna
            float nextShot = Time.realtimeSinceStartup + 0.9f;
            int runShots = 0;
            while (Time.realtimeSinceStartup < end && runShots < 40)
            {
                if (Time.realtimeSinceStartup >= nextShot)
                {
                    yield return Shot("run");
                    runShots++;
                    nextShot = Time.realtimeSinceStartup + 0.9f;
                    _dismiss = true;   // Açık pencere varsa görüntüsü alındı, kapat
                }
                if (!Click("AdvanceButton")) Click("StartRunButton");   // Tur bittiyse yenisine başla
                yield return Wait(0.25f);
            }

            // Tur sürüyorsa yüzeye dön (meşgulse tekrar dene), pencereleri kapat, yükseltme ekranı gelene kadar bekle
            for (int i = 0; i < 60 && !Clickable("StartRunButton"); i++)
            {
                Click("CashOutButton");
                _dismiss = true;
                yield return Wait(0.4f);
            }
            yield return Wait(0.5f);
            yield return Shot("upgrades_after_run");
            foreach (string tab in new[] { "EquipmentTab", "TalentsTab", "SpellsTab", "UpgradesTab" })
            {
                if (!Click(tab)) continue;
                yield return Wait(0.6f);
                yield return Shot(tab);
            }

            Click("MenuButton");
            yield return Wait(0.8f);
            yield return Shot("pause");
            Click("SettingsButton");
            yield return Wait(0.8f);
            yield return Shot("settings");
            Application.Quit();
        }

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until) yield return null;
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Folder, $"{++_shot:00}_{name}.png"));
            yield return null;
        }

        /// <summary>Bu isimde, görünür ve basılabilir bir butona basar. Basabildiyse true.</summary>
        private static bool Click(string name) => ClickWhere(b => b.gameObject.name == name);

        /// <summary>Bu isimde basılabilir bir buton görünüyor mu (basmadan)?</summary>
        private static bool Clickable(string name)
        {
            foreach (Button b in FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (b.gameObject.name == name && b.IsInteractable() && Reachable(b.transform)) return true;
            return false;
        }

        private static bool ClickWhere(Func<Button, bool> match)
        {
            foreach (Button b in FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!match(b) || !b.IsInteractable() || !Reachable(b.transform)) continue;
                b.onClick.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>Üstündeki bir CanvasGroup onu gizliyor ya da tıklanamaz yapıyorsa basılamaz.</summary>
        private static bool Reachable(Transform t)
        {
            for (; t != null; t = t.parent)
            {
                var group = t.GetComponent<CanvasGroup>();
                if (group != null && (!group.interactable || !group.blocksRaycasts || group.alpha < 0.01f)) return false;
            }
            return true;
        }
    }
}
