using System.Collections.Generic;
using System.IO;
using RiskLoop.Core;
using RiskLoop.Game;
using RiskLoop.Game.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace RiskLoop.EditorTools
{
    /// <summary>
    /// Menü: RiskLoop > Build Prototype Scene
    /// Prototip sahnesini (Canvas, metinler, butonlar) sıfırdan kurar ve tüm script referanslarını bağlar.
    /// Sadece editörde çalışır, oyunun build'ine girmez ("Editor" klasöründe olduğu için).
    ///
    /// Düzen PC için YATAY (16:9, referans 1920x1080):
    ///   Üst bar:        Para | Seviye + XP | Rekor | Kaydı sıfırla (yarı saydam, her zaman üstte)
    ///   Tur ekranı:     TAM EKRAN kazı sahnesi + üstünde yarı saydam HUD (sol istatistik, sağ bulgular/butonlar, alt mesaj)
    ///   Yükseltme ekr.: sol sütun sonuç + başla   | sağ sütun sekmeler (Yükseltmeler, Yetenekler, Ekipman, Yeni Çağ)
    ///
    /// Sahneyi elle değiştirdikten sonra bunu tekrar çalıştırırsan değişikliklerin silinir.
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Prototype.unity";
        private const string ConfigFolder = "Assets/Config";
        private const string ConfigPath = ConfigFolder + "/BalanceConfig.asset";

        // Referans çözünürlük (16:9 PC ekranı)
        private const float RefWidth = 1920;
        private const float RefHeight = 1080;

        // Renkler
        private static readonly Color BackgroundColor = new Color(0.10f, 0.10f, 0.13f);
        private static readonly Color PanelColor = new Color(0.16f, 0.16f, 0.20f);
        private static readonly Color AdvanceColor = new Color(0.85f, 0.45f, 0.15f);
        private static readonly Color CashOutColor = new Color(0.20f, 0.62f, 0.35f);
        private static readonly Color BuyColor = new Color(0.22f, 0.45f, 0.80f);
        private static readonly Color MutedColor = new Color(0.35f, 0.35f, 0.40f);
        private static readonly Color BarBackColor = new Color(0.25f, 0.25f, 0.30f);
        private static readonly Color HintColor = new Color(0.6f, 0.6f, 0.68f);
        private static readonly Color HudColor = new Color(0.04f, 0.04f, 0.07f, 0.62f);   // Sahnenin üstündeki yarı saydam paneller

        // Tam ekran HUD ölçüleri
        private const float TopBarHeight = 90;
        private const float HudSideWidth = 460;
        private const float GaugeHeight = 178;
        private const float GaugeWidth = 760;   // Dar tutulur: kazma ve silah iki yanında görünsün

        [MenuItem("RiskLoop/Build Prototype Scene")]
        public static void Build()
        {
            // TextMeshPro'nun temel dosyaları yoksa metinler görünmez
            if (Resources.Load<TMP_Settings>("TMP Settings") == null &&
                !EditorUtility.DisplayDialog("TextMeshPro eksik olabilir",
                    "TMP temel dosyaları bulunamadı. Önce Window > TextMeshPro > Import TMP Essential Resources " +
                    "menüsünü çalıştırman önerilir, yoksa metinler görünmeyebilir.",
                    "Yine de devam", "İptal"))
            {
                return;
            }

            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Sahne zaten var",
                    $"{ScenePath} yeniden oluşturulacak. Sahnede elle yaptığın değişiklikler silinir.",
                    "Devam", "İptal"))
            {
                return;
            }

            // Açık sahnede kaydedilmemiş değişiklik varsa kullanıcıya sor
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // Önce sahneyi aç, SONRA config'i yükle: NewScene kullanılmayan asset'leri bellekten atar,
            // önceden yüklenmiş referans boşa düşer ve GameController'a null bağlanır.
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            BalanceConfig config = LoadOrCreateConfig();

            CreateEventSystem();
            BuildUI(config);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            AddSceneToBuildSettings();

            Debug.Log($"Prototip sahnesi hazır: {ScenePath}. Game penceresinde 1920x1080 (16:9) seçip Play'e bas.");
        }

        // ---------------- Kurulum adımları ----------------

        private static BalanceConfig LoadOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<BalanceConfig>(ConfigPath);
            if (config != null) return config;  // Var olan ayarlarını ezmiyoruz

            Directory.CreateDirectory(ConfigFolder);
            config = ScriptableObject.CreateInstance<BalanceConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static void CreateEventSystem()
        {
            // EventSystem: UI'ın tıklamaları alabilmesi için gerekli
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        private static void BuildUI(BalanceConfig config)
        {
            // --- Canvas ---
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Expand: tuval her zaman en az 1920x1080 birim olur. 16:10, 21:9 gibi oranlarda
            // fazladan boşluk kalır ama hiçbir şey ekrandan taşmaz.
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            Transform canvas = canvasGo.transform;

            RectTransform background = CreateUIObject("Background", canvas);
            Stretch(background);
            AddImage(background, BackgroundColor).raycastTarget = false;

            // --- Dünya: sarsıntı efekti bunun hepsini sallar ---
            RectTransform world = CreateUIObject("World", canvas);
            Stretch(world);

            // Tur ekranı TAM EKRAN: kazı sahnesi tüm ekranı kaplar, istatistik ve butonlar üstünde yarı saydam paneller
            RunPanelView runPanel = BuildRunPanel(world);

            // Yükseltme ekranı: üst barın altında, ortada 1920 genişliğinde (geniş ekranda kenarlar boş kalır)
            RectTransform screens = CreateUIObject("Screens", world);
            screens.anchorMin = new Vector2(0.5f, 0f);
            screens.anchorMax = new Vector2(0.5f, 1f);
            screens.offsetMin = new Vector2(-(RefWidth - 60) / 2f, 30f);
            screens.offsetMax = new Vector2((RefWidth - 60) / 2f, -(TopBarHeight + 20f));
            UpgradePanelView upgradePanel = BuildUpgradePanel(screens);

            // Üst bar en son: her iki ekranın da üstünde (tur ekranında yarı saydam şerit)
            TopBarView topBar = BuildTopBar(world);

            // Üst katmanlar (sıra önemli: sonra eklenen üstte çizilir)
            ClassSelectView classSelect = BuildClassSelect(canvas);
            RevealPopupView popup = BuildRevealPopup(canvas);
            FxLayerView fx = BuildFxLayer(canvas, world);
            MainMenuView mainMenu = BuildMainMenu(canvas);
            PauseMenuView pauseMenu = BuildPauseMenu(canvas);
            SettingsView settingsMenu = BuildSettingsMenu(canvas);   // Ana menü ve duraklatmanın üstünde açılır
            HowToPlayView howToPlay = BuildHowToPlay(canvas);
            BuildTooltip(canvas);   // En üstte: her şeyin üzerinde görünsün

            // --- GameController + hissiyat yönetmeni ---
            var controllerGo = new GameObject("GameController");
            var director = controllerGo.AddComponent<RunFeedbackDirector>();
            SetRef(director, "fx", fx);
            SetRef(director, "popup", popup);
            SetRef(director, "runPanel", runPanel);
            SetRef(director, "digView", runPanel.GetComponentInChildren<DigView>(true));
            SetRef(director, "topBar", topBar);

            var controller = controllerGo.AddComponent<GameController>();
            SetRef(controller, "balanceConfig", config);
            SetRef(controller, "topBar", topBar);
            SetRef(controller, "runPanel", runPanel);
            SetRef(controller, "upgradePanel", upgradePanel);
            SetRef(controller, "feedback", director);
            SetRef(controller, "classSelect", classSelect);
            SetRef(controller, "mainMenu", mainMenu);
            SetRef(controller, "pauseMenu", pauseMenu);
            SetRef(controller, "settingsMenu", settingsMenu);
            SetRef(controller, "howToPlay", howToPlay);

            // Ses efektleri (kodla sentezlenir)
            new GameObject("Audio", typeof(Sfx));

            // Eşya detayı güçlendirme efektlerini en üst efekt katmanında oynatır
            SetRef(upgradePanel.GetComponentInChildren<ItemDetailView>(true), "fx", fx);
        }

        /// <summary>
        /// Üst bar (ekranın en üstü, yarı saydam şerit): para | seviye + XP çubuğu | rekor | kaydı sıfırla.
        /// Sıfırla sağ üst köşede, kazı butonlarından uzakta.
        /// </summary>
        private static TopBarView BuildTopBar(Transform parent)
        {
            RectTransform bar = CreateUIObject("TopBar", parent);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0f, TopBarHeight);
            AddImage(bar, HudColor).raycastTarget = false;
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 30;
            layout.padding = new RectOffset(30, 30, 8, 8);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            float h = TopBarHeight - 16;
            TMP_Text money = CreateText("MoneyText", bar, "Para: 0", 46, h, TextAlignmentOptions.MidlineLeft);
            money.fontStyle = FontStyles.Bold;
            FillRemainingWidth(money);

            // Seviye: "Seviye 7" + altında ince XP çubuğu ve "XP 120 / 300"
            RectTransform level = CreateUIObject("Level", bar);
            SetHeight(level, h);
            SetFixedWidth(level, 420);
            AddVerticalLayout(level, spacing: 2, padding: 0);
            TMP_Text levelText = CreateText("LevelText", level, "Seviye 1", 30, 36, TextAlignmentOptions.MidlineLeft);
            levelText.fontStyle = FontStyles.Bold;
            levelText.color = new Color(0.55f, 0.85f, 1f);
            RectTransform xpBar = CreateUIObject("XpBar", level);
            SetHeight(xpBar, 12);
            AddImage(xpBar, BarBackColor).raycastTarget = false;
            RectTransform xpFill = CreateUIObject("Fill", xpBar);
            xpFill.anchorMin = Vector2.zero;
            xpFill.anchorMax = new Vector2(0.1f, 1f);
            xpFill.offsetMin = Vector2.zero;
            xpFill.offsetMax = Vector2.zero;
            AddImage(xpFill, new Color(0.35f, 0.7f, 1f)).raycastTarget = false;
            TMP_Text xpText = CreateText("XpText", level, "", 16, 18, TextAlignmentOptions.MidlineLeft);
            xpText.color = HintColor;

            TMP_Text record = CreateText("RecordText", bar, "Rekor", 30, h, TextAlignmentOptions.MidlineRight);
            SetFixedWidth(record, 300);

            Button menu = CreateButton("MenuButton", bar, UIStrings.TopBarMenu, MutedColor, 56, 22, out TMP_Text menuText);
            SetFixedWidth(menu, 200);

            var view = bar.gameObject.AddComponent<TopBarView>();
            SetRef(view, "moneyText", money);
            SetRef(view, "recordText", record);
            SetRef(view, "levelText", levelText);
            SetRef(view, "xpText", xpText);
            SetRef(view, "xpBarFill", xpFill);
            SetRef(view, "menuButton", menu);
            SetRef(view, "menuButtonText", menuText);
            return view;
        }

        /// <summary>
        /// Tam ekran tur ekranı. Kazı sahnesi tüm ekranı kaplar; üstünde yarı saydam HUD panelleri:
        ///   sol üst: karakter, derinlik, turun değeri, 12 stat kartı, risk çubuğu, kısa satırlar
        ///   sağ üst: bu turun bulguları     sağ alt: KAZ / Yüzeye dön butonları     alt orta: mesaj
        /// Ortası (duvar, canavar, bulgu) hep açık kalır: oyuncunun görüşünü kapatmaz.
        /// </summary>
        private static RunPanelView BuildRunPanel(Transform parent)
        {
            RectTransform panel = CreateUIObject("RunPanel", parent);
            Stretch(panel);

            DigView digView = BuildDigView(panel);

            // Kalp atışı: riskle nabız gibi atan kırmızı ekran kenarı (sahnenin üstünde, HUD'ların altında; tıklamayı engellemez)
            RectTransform heartbeatRt = CreateUIObject("Heartbeat", panel);
            Stretch(heartbeatRt);
            Image heartbeat = AddImage(heartbeatRt, new Color(0.9f, 0.05f, 0.05f, 0f));
            heartbeat.raycastTarget = false;

            // ---- Sol HUD: istatistikler ----
            RectTransform left = CreateHudPanel("LeftHud", panel, new Vector2(0f, 1f), new Vector2(20f, -(TopBarHeight + 14f)), HudSideWidth);

            // Üst satır: karakter + derinlik + turun değeri (az yazı, büyük sayı)
            RectTransform top = CreateUIObject("Top", left);
            SetHeight(top, 136);
            AddHorizontalLayout(top, spacing: 12);
            AvatarView avatar = BuildAvatar(top, new Vector2(102, 136));
            RectTransform topTexts = CreateUIObject("TopTexts", top);
            SetHeight(topTexts, 136);
            FillRemainingWidth(topTexts.GetComponent<LayoutElement>());
            AddVerticalLayout(topTexts, spacing: 2, padding: 0);
            TMP_Text depth = CreateText("DepthText", topTexts, "0. KAT", 44, 46, TextAlignmentOptions.MidlineLeft);
            depth.fontStyle = FontStyles.Bold;
            TMP_Text runValue = CreateText("RunValueText", topTexts, "0", 46, 52, TextAlignmentOptions.MidlineLeft);
            runValue.fontStyle = FontStyles.Bold;
            runValue.color = new Color(1f, 0.88f, 0.23f);
            TMP_Text breakdown = CreateText("RunValueBreakdownText", topTexts, "", 18, 34, TextAlignmentOptions.MidlineLeft);

            // Sade kartlar (2 sütun, en fazla 4). Sadece açık özellikler görünür; ızgara görünenlere göre boy alır.
            RectTransform chipGrid = CreateUIObject("StatChips", left);
            var chipLayout = chipGrid.gameObject.AddComponent<GridLayoutGroup>();
            chipLayout.cellSize = new Vector2(205, 58);
            chipLayout.spacing = new Vector2(6, 6);
            chipLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            chipLayout.constraintCount = 2;
            chipLayout.childAlignment = TextAnchor.UpperLeft;
            var chips = new StatChipView[RunPanelView.ChipCount];
            for (int i = 0; i < chips.Length; i++) chips[i] = BuildStatChip(chipGrid, i);

            // Tur etkileri (sadece varsa görünür, açıklaması fareyle)
            TMP_Text buffs = CreateTipText("BuffsText", left, 18, 30, out TooltipTrigger buffsTip);

            // ---- Sağ üst HUD: bu turun bulguları ----
            RectTransform rightTop = CreateHudPanel("CollectionHud", panel, new Vector2(1f, 1f), new Vector2(-20f, -(TopBarHeight + 14f)), HudSideWidth);
            FindCollectionView collection = BuildFindCollection(rightTop);

            // ---- Sağ alt HUD: butonlar ----
            RectTransform rightBottom = CreateHudPanel("ButtonsHud", panel, new Vector2(1f, 0f), new Vector2(-20f, 20f), HudSideWidth);
            Button advance = CreateButton("AdvanceButton", rightBottom, UIStrings.AdvanceButton, AdvanceColor, 104, 52, out TMP_Text advanceLabel);
            Button cashOut = CreateButton("CashOutButton", rightBottom, UIStrings.CashOutButton, CashOutColor, 70, 32, out TMP_Text cashOutLabel);
            TMP_Text hint = CreateText("KeyHintText", rightBottom, UIStrings.KeyHintRun, 16, 42);
            hint.color = HintColor;

            // ---- Sol alt HUD: büyü çubuğu (mana + 6 büyü, kısayol 1-6) ----
            SpellBarView spellBar = BuildSpellBar(panel);

            // ---- Alt orta: Cesaret terazisi (sonraki kata geçme şansı, büyük) ----
            RiskGaugeView gauge = BuildRiskGauge(panel, heartbeat);

            // ---- Mesaj: terazinin hemen üstünde ----
            RectTransform messageRt = CreateUIObject("MessageText", panel);
            messageRt.anchorMin = messageRt.anchorMax = new Vector2(0.5f, 0f);
            messageRt.pivot = new Vector2(0.5f, 0f);
            messageRt.sizeDelta = new Vector2(GaugeWidth, 66);
            messageRt.anchoredPosition = new Vector2(0f, 20f + GaugeHeight + 10f);
            AddImage(messageRt, new Color(0f, 0f, 0f, 0.35f)).raycastTarget = false;
            RectTransform messageTextRt = CreateUIObject("Text", messageRt);
            Stretch(messageTextRt);
            messageTextRt.offsetMin = new Vector2(16, 4);
            messageTextRt.offsetMax = new Vector2(-16, -4);
            var message = messageTextRt.gameObject.AddComponent<TextMeshProUGUI>();
            message.fontSize = 24;
            message.alignment = TextAlignmentOptions.Center;
            message.color = Color.white;
            message.raycastTarget = false;
            message.enableAutoSizing = true;
            message.fontSizeMax = 24;
            message.fontSizeMin = 15;

            var view = panel.gameObject.AddComponent<RunPanelView>();
            SetRef(view, "avatar", avatar);
            SetRef(view, "depthText", depth);
            SetRef(view, "runValueText", runValue);
            SetRef(view, "runValueBreakdownText", breakdown);
            SetRefArray(view, "chips", chips);
            SetRef(view, "buffsText", buffs);
            SetRef(view, "buffsTip", buffsTip);
            SetRef(view, "riskGauge", gauge);
            SetRef(view, "spellBar", spellBar);
            SetRef(view, "messageText", message);
            SetRef(view, "collection", collection);
            SetRef(view, "digView", digView);
            SetRef(view, "advanceButton", advance);
            SetRef(view, "cashOutButton", cashOut);
            SetRef(view, "keyHintText", hint);
            SetRef(view, "advanceLabel", advanceLabel);
            SetRef(view, "cashOutLabel", cashOutLabel);
            return view;
        }

        /// <summary>
        /// Cesaret terazisi: alt ortada yarı saydam panel.
        ///   üst satır : (kehanet notu) | büyük geçme şansı "%82" | (boş)
        ///   çubuk     : risk kadar dolu, renk yeşil → sarı → kırmızı
        ///   alt satır : "KAZANIRSAN +1.2K" | "KAYBEDERSEN -8.4K"
        /// heartbeat: tam ekran kırmızı kenar (nabız), terazi yönetir.
        /// </summary>
        private static RiskGaugeView BuildRiskGauge(Transform parent, Image heartbeat)
        {
            RectTransform root = CreateHudPanel("RiskGauge", parent, new Vector2(0.5f, 0f), new Vector2(0f, 20f), GaugeWidth);
            var fitter = root.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            root.sizeDelta = new Vector2(GaugeWidth, GaugeHeight);
            root.pivot = new Vector2(0.5f, 0f);
            var layout = root.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 8, 10);
            layout.spacing = 6;

            RectTransform top = CreateUIObject("Top", root);
            SetHeight(top, 72);
            AddHorizontalLayout(top, spacing: 10);
            TMP_Text title = CreateText("Title", top, "", 40, 72, TextAlignmentOptions.MidlineLeft);   // Turun toplam RİSK BONUSU
            title.color = HintColor;
            FillRemainingWidth(title);
            TMP_Text risk = CreateText("Risk", top, "%0", 64, 72);
            risk.fontStyle = FontStyles.Bold;
            SetFixedWidth(risk, 230);
            TMP_Text verdict = CreateText("Verdict", top, "", 30, 72, TextAlignmentOptions.MidlineRight);
            verdict.fontStyle = FontStyles.Bold;
            FillRemainingWidth(verdict);

            RectTransform bar = CreateUIObject("Bar", root);
            SetHeight(bar, 16);
            AddImage(bar, BarBackColor).raycastTarget = false;
            RectTransform fill = CreateUIObject("Fill", bar);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0.1f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            Image fillImage = AddImage(fill, Color.green);
            fillImage.raycastTarget = false;

            RectTransform bottom = CreateUIObject("Bottom", root);
            SetHeight(bottom, 58);
            AddHorizontalLayout(bottom, spacing: 10);
            TMP_Text win = CreateText("Win", bottom, "", 30, 58, TextAlignmentOptions.MidlineLeft);
            FillRemainingWidth(win);
            TMP_Text lose = CreateText("Lose", bottom, "", 30, 58, TextAlignmentOptions.MidlineRight);
            FillRemainingWidth(lose);

            var tip = root.gameObject.AddComponent<TooltipTrigger>();
            var view = root.gameObject.AddComponent<RiskGaugeView>();
            SetRef(view, "root", root);
            SetRef(view, "titleText", title);
            SetRef(view, "riskText", risk);
            SetRef(view, "verdictText", verdict);
            SetRef(view, "barFill", fill);
            SetRef(view, "barFillImage", fillImage);
            SetRef(view, "winText", win);
            SetRef(view, "loseText", lose);
            SetRef(view, "heartbeat", heartbeat);
            SetRef(view, "tooltip", tip);
            return view;
        }

        /// <summary>
        /// Yarı saydam HUD paneli: verilen köşeye yapışır, yüksekliği içeriğine göre (ContentSizeFitter).
        /// Arka planı tıklamayı yakalar: panele tıklamak yanlışlıkla kazma vurmaz.
        /// </summary>
        private static RectTransform CreateHudPanel(string name, Transform parent, Vector2 corner, Vector2 offset, float width)
        {
            RectTransform rt = CreateUIObject(name, parent);
            rt.anchorMin = rt.anchorMax = corner;
            rt.pivot = corner;
            rt.sizeDelta = new Vector2(width, 0f);
            rt.anchoredPosition = offset;
            AddImage(rt, HudColor);
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = rt.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rt;
        }

        /// <summary>Stat kartı: koyu kutu + solda renkli şerit + üstte soluk etiket + altta büyük değer + ipucu.</summary>
        private static StatChipView BuildStatChip(Transform parent, int index)
        {
            RectTransform root = CreateUIObject($"Chip_{index}", parent);
            AddImage(root, BackgroundColor);   // raycastTarget açık: ipucu için

            RectTransform accentRt = CreateUIObject("Accent", root);
            accentRt.anchorMin = new Vector2(0f, 0f);
            accentRt.anchorMax = new Vector2(0f, 1f);
            accentRt.pivot = new Vector2(0f, 0.5f);
            accentRt.sizeDelta = new Vector2(6f, 0f);
            Image accent = AddImage(accentRt, Color.white);
            accent.raycastTarget = false;

            TMP_Text label = AddChipText(root, "Label", 15, new Vector2(16, 34), new Vector2(-8, -4), TextAlignmentOptions.TopLeft);
            label.color = HintColor;
            TMP_Text value = AddChipText(root, "Value", 28, new Vector2(16, 2), new Vector2(-8, -22), TextAlignmentOptions.BottomLeft);
            value.fontStyle = FontStyles.Bold;

            var tip = root.gameObject.AddComponent<TooltipTrigger>();
            var view = root.gameObject.AddComponent<StatChipView>();
            SetRef(view, "label", label);
            SetRef(view, "value", value);
            SetRef(view, "accent", accent);
            SetRef(view, "tooltip", tip);
            return view;
        }

        private static TMP_Text AddChipText(RectTransform parent, string name, float size, Vector2 offsetMin, Vector2 offsetMax, TextAlignmentOptions align)
        {
            RectTransform rt = CreateUIObject(name, parent);
            Stretch(rt);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = size;
            tmp.fontSizeMin = size * 0.6f;
            return tmp;
        }

        /// <summary>Üstüne gelince açıklama veren tek satır metin.</summary>
        private static TMP_Text CreateTipText(string name, Transform parent, float size, float height, out TooltipTrigger tip)
        {
            TMP_Text text = CreateText(name, parent, "", size, height, TextAlignmentOptions.MidlineLeft);
            text.raycastTarget = true;   // Metnin kutusu fareyi yakalasın
            tip = text.gameObject.AddComponent<TooltipTrigger>();
            return text;
        }

        /// <summary>
        /// Karakter: aura + hüzme (arkada), salınan kök içinde katmanlar (pelerin, sırtta kazma, gövde, pantolon, çizme,
        /// zırh, omuzluk, eldiven, kolye, boynuz, kask, elde silah). size en-boy 24:32 olmalı.
        /// </summary>
        private static AvatarView BuildAvatar(Transform parent, Vector2 size)
        {
            RectTransform root = CreateUIObject("Avatar", parent);
            SetHeight(root, size.y);
            SetFixedWidth(root, size.x);
            root.sizeDelta = size;

            Image aura = AddCentered("Aura", root, size * 1.7f);
            Image rays = AddCentered("Rays", root, size * 1.9f);

            // Kudret rütbesi: ayak altında yatay ışık halkası
            RectTransform groundRt = CreateUIObject("GroundGlow", root);
            groundRt.anchorMin = groundRt.anchorMax = new Vector2(0.5f, 0.06f);
            groundRt.sizeDelta = new Vector2(size.x * 1.6f, size.x * 1.6f);
            Image groundGlow = AddImage(groundRt, Color.white);
            groundGlow.raycastTarget = false;

            RectTransform bob = CreateUIObject("BobRoot", root);
            Stretch(bob);

            Image Layer(string n)
            {
                RectTransform rt = CreateUIObject(n, bob);
                Stretch(rt);
                Image img = AddImage(rt, Color.white);
                img.raycastTarget = false;
                return img;
            }

            Image quiver = Layer("Quiver");   // Okçu: sırtta sadak (gövdenin arkasında)
            Image cape = Layer("Cape");

            // Sırtta kazma (gövdenin arkasında, sol omuzdan çapraz)
            RectTransform pickRt = CreateUIObject("Pickaxe", bob);
            pickRt.anchorMin = pickRt.anchorMax = new Vector2(0.27f, 0.62f);
            pickRt.sizeDelta = new Vector2(size.x * 0.62f, size.x * 0.62f);
            pickRt.localRotation = Quaternion.Euler(0f, 0f, 35f);
            Image pickaxe = AddImage(pickRt, Color.white);
            pickaxe.raycastTarget = false;
            pickaxe.preserveAspect = true;

            Image body = Layer("Body");
            Image robe = Layer("Robe");       // Büyücü cübbesi
            Image pants = Layer("Pants");
            Image boots = Layer("Boots");
            Image armor = Layer("Armor");
            Image pauldrons = Layer("Pauldrons");
            Image gloves = Layer("Gloves");
            Image necklace = Layer("Necklace");
            Image classHat = Layer("ClassHat");   // Şapka / başlık / alın bandı
            Image horns = Layer("Horns");
            Image helmet = Layer("Helmet");

            // Silahın arkasında güçlendirme parıltısı (silahtan ÖNCE eklenir ki arkada çizilsin)
            RectTransform weaponGlowRt = CreateUIObject("WeaponGlow", bob);
            weaponGlowRt.anchorMin = weaponGlowRt.anchorMax = new Vector2(0.85f, 0.6f);
            weaponGlowRt.sizeDelta = new Vector2(size.x * 1.1f, size.x * 1.1f);
            Image weaponGlow = AddImage(weaponGlowRt, Color.white);
            weaponGlow.raycastTarget = false;

            // Sağ elde silah (kabzadan tutulur, ağız yukarı-dışa)
            RectTransform weaponRt = CreateUIObject("Weapon", bob);
            weaponRt.anchorMin = weaponRt.anchorMax = new Vector2(0.75f, 0.45f);
            weaponRt.pivot = new Vector2(0.15f, 0.12f);
            weaponRt.sizeDelta = new Vector2(size.x * 0.7f, size.x * 0.7f);
            weaponRt.localRotation = Quaternion.Euler(0f, 0f, -15f);
            Image weapon = AddImage(weaponRt, Color.white);
            weapon.raycastTarget = false;
            weapon.preserveAspect = true;

            var view = root.gameObject.AddComponent<AvatarView>();
            SetRef(view, "bobRoot", bob);
            SetRef(view, "aura", aura);
            SetRef(view, "rays", rays);
            SetRef(view, "groundGlow", groundGlow);
            SetRef(view, "quiver", quiver);
            SetRef(view, "robe", robe);
            SetRef(view, "classHat", classHat);
            SetRef(view, "weaponGlow", weaponGlow);
            SetRef(view, "cape", cape);
            SetRef(view, "pickaxe", pickaxe);
            SetRef(view, "body", body);
            SetRef(view, "pants", pants);
            SetRef(view, "boots", boots);
            SetRef(view, "armor", armor);
            SetRef(view, "pauldrons", pauldrons);
            SetRef(view, "gloves", gloves);
            SetRef(view, "necklace", necklace);
            SetRef(view, "horns", horns);
            SetRef(view, "helmet", helmet);
            SetRef(view, "weapon", weapon);
            return view;
        }

        /// <summary>
        /// Büyü çubuğu (sol alt HUD): üstte mana çubuğu, altta 6 büyü butonu (kısayol 1-6).
        /// Sınıf seçilmeden ya da büyü yokken görünmez (SpellBarView kendini gizler).
        /// </summary>
        private static SpellBarView BuildSpellBar(Transform parent)
        {
            const int count = 6;
            const float button = 64;

            RectTransform root = CreateHudPanel("SpellHud", parent, new Vector2(0f, 0f), new Vector2(20f, 20f), HudSideWidth);

            RectTransform manaRow = CreateUIObject("ManaRow", root);
            SetHeight(manaRow, 26);
            AddHorizontalLayout(manaRow, spacing: 10);
            TMP_Text manaLabel = CreateText("ManaLabel", manaRow, UIStrings.ManaLabel, 18, 26, TextAlignmentOptions.MidlineLeft);
            manaLabel.color = new Color(0.45f, 0.65f, 1f);
            manaLabel.fontStyle = FontStyles.Bold;
            SetFixedWidth(manaLabel, 70);
            RectTransform bar = CreateUIObject("ManaBar", manaRow);
            SetHeight(bar, 20);
            FillRemainingWidth(bar);
            AddImage(bar, BarBackColor).raycastTarget = false;
            RectTransform fill = CreateUIObject("Fill", bar);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0.5f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            AddImage(fill, new Color(0.3f, 0.5f, 1f)).raycastTarget = false;
            RectTransform manaTextRt = CreateUIObject("ManaText", bar);
            Stretch(manaTextRt);
            var manaText = manaTextRt.gameObject.AddComponent<TextMeshProUGUI>();
            manaText.fontSize = 15;
            manaText.fontStyle = FontStyles.Bold;
            manaText.alignment = TextAlignmentOptions.Center;
            manaText.raycastTarget = false;

            RectTransform row = CreateUIObject("Buttons", root);
            SetHeight(row, button);
            AddHorizontalLayout(row, spacing: 8);
            var buttons = new SpellButtonView[count];
            for (int i = 0; i < count; i++) buttons[i] = BuildSpellButton(row, i, button);

            var view = root.gameObject.AddComponent<SpellBarView>();
            SetRef(view, "manaFill", fill);
            SetRef(view, "manaText", manaText);
            SetRefArray(view, "buttons", buttons);
            return view;
        }

        /// <summary>Kare büyü butonu: çerçeve, ikon, sol üstte kısayol, sağ altta mana bedeli.</summary>
        private static SpellButtonView BuildSpellButton(Transform parent, int index, float size)
        {
            RectTransform root = CreateUIObject($"Spell_{index}", parent);
            SetHeight(root, size);
            SetFixedWidth(root, size);
            Image frame = AddImage(root, Color.gray);   // raycastTarget açık: tıklama + ipucu

            RectTransform innerRt = CreateUIObject("Inner", root);
            Stretch(innerRt);
            innerRt.offsetMin = new Vector2(3, 3);
            innerRt.offsetMax = new Vector2(-3, -3);
            AddImage(innerRt, new Color(0.07f, 0.06f, 0.1f)).raycastTarget = false;

            RectTransform iconRt = CreateUIObject("Icon", root);
            Stretch(iconRt);
            iconRt.offsetMin = new Vector2(8, 8);
            iconRt.offsetMax = new Vector2(-8, -8);
            Image icon = AddImage(iconRt, Color.white);
            icon.raycastTarget = false;
            icon.preserveAspect = true;

            TMP_Text hotkey = AddChipText(root, "Hotkey", 14, new Vector2(5, 30), new Vector2(-5, -3), TextAlignmentOptions.TopLeft);
            hotkey.color = new Color(0.8f, 0.8f, 0.85f);
            TMP_Text cost = AddChipText(root, "Cost", 14, new Vector2(5, 3), new Vector2(-5, -30), TextAlignmentOptions.BottomRight);
            cost.color = new Color(0.5f, 0.7f, 1f);
            cost.fontStyle = FontStyles.Bold;

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.transition = Selectable.Transition.None;   // Renkleri SpellButtonView yönetir
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var tip = root.gameObject.AddComponent<TooltipTrigger>();
            var view = root.gameObject.AddComponent<SpellButtonView>();
            SetRef(view, "button", button);
            SetRef(view, "frame", frame);
            SetRef(view, "icon", icon);
            SetRef(view, "costText", cost);
            SetRef(view, "hotkeyText", hotkey);
            SetRef(view, "tooltip", tip);
            return view;
        }

        /// <summary>Büyü Kitabı sekmesi: üstte sınıf ikonu + başlık, altında 6 büyü satırı (ikon, seviye rozeti, açıklama).</summary>
        private static SpellBookView BuildSpellBook(Transform parent)
        {
            const int rows = 6;
            const float rowHeight = 92;

            RectTransform root = CreateUIObject("SpellsRoot", parent);
            Stretch(root);
            AddVerticalLayout(root, spacing: 8, padding: 0);

            RectTransform header = CreateUIObject("Header", root);
            SetHeight(header, 84);
            AddHorizontalLayout(header, spacing: 14);
            Image classIcon = BuildIconBox(header, 80);
            TMP_Text headerText = CreateText("HeaderText", header, "", 26, 84, TextAlignmentOptions.MidlineLeft);
            FillRemainingWidth(headerText);

            var icons = new Image[rows];
            var badges = new TMP_Text[rows];
            var texts = new TMP_Text[rows];
            var tips = new TooltipTrigger[rows];
            for (int i = 0; i < rows; i++)
            {
                RectTransform row = CreateUIObject($"SpellRow_{i}", root);
                SetHeight(row, rowHeight);
                AddImage(row, new Color(0f, 0f, 0f, 0.25f));   // raycastTarget açık: ipucu
                AddHorizontalLayout(row, spacing: 14);
                row.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(8, 8, 4, 4);
                tips[i] = row.gameObject.AddComponent<TooltipTrigger>();

                icons[i] = BuildIconBox(row, rowHeight - 12);
                badges[i] = CreateText("Level", row, "", 22, rowHeight - 12);
                badges[i].fontStyle = FontStyles.Bold;
                SetFixedWidth(badges[i], 70);
                // SpellBookView satırı gizlemek için metnin ebeveynini (satırı) kullanır
                texts[i] = CreateText("Text", row, "", 24, rowHeight - 12, TextAlignmentOptions.MidlineLeft);
                FillRemainingWidth(texts[i]);
            }

            var view = root.gameObject.AddComponent<SpellBookView>();
            SetRef(view, "classIcon", classIcon);
            SetRef(view, "headerText", headerText);
            SetRefArray(view, "icons", icons);
            SetRefArray(view, "levelBadges", badges);
            SetRefArray(view, "rowTexts", texts);
            SetRefArray(view, "tooltips", tips);
            return view;
        }

        /// <summary>
        /// Sınıf seçim ekranı: tam ekran koyu perde, başlık, 3 kart (karakter önizlemesi, ikon, ad, açıklama, SEÇ).
        /// Başta gizli; GameController sınıf yoksa açar.
        /// </summary>
        private static ClassSelectView BuildClassSelect(Transform canvas)
        {
            RectTransform root = CreateUIObject("ClassSelect", canvas);
            Stretch(root);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            AddImage(root, new Color(0.03f, 0.03f, 0.05f, 0.94f));   // Tıklamayı yutar

            RectTransform titleRt = CreateUIObject("Title", root);
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(1400, 90);
            titleRt.anchoredPosition = new Vector2(0, -70);
            var title = titleRt.gameObject.AddComponent<TextMeshProUGUI>();
            title.fontSize = 72;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(1f, 0.88f, 0.23f);
            title.raycastTarget = false;

            RectTransform hintRt = CreateUIObject("Hint", root);
            hintRt.anchorMin = hintRt.anchorMax = new Vector2(0.5f, 1f);
            hintRt.pivot = new Vector2(0.5f, 1f);
            hintRt.sizeDelta = new Vector2(1400, 50);
            hintRt.anchoredPosition = new Vector2(0, -165);
            var hint = hintRt.gameObject.AddComponent<TextMeshProUGUI>();
            hint.fontSize = 26;
            hint.alignment = TextAlignmentOptions.Center;
            hint.color = HintColor;
            hint.raycastTarget = false;

            RectTransform cards = CreateUIObject("Cards", root);
            cards.anchorMin = cards.anchorMax = new Vector2(0.5f, 0.5f);
            cards.sizeDelta = new Vector2(1500, 720);
            cards.anchoredPosition = new Vector2(0, -60);
            var cardsLayout = cards.gameObject.AddComponent<HorizontalLayoutGroup>();
            cardsLayout.spacing = 40;
            cardsLayout.childControlWidth = true;
            cardsLayout.childControlHeight = true;
            cardsLayout.childForceExpandWidth = true;
            cardsLayout.childForceExpandHeight = true;   // Bu grup bir düzenin İÇİNDE değil: güvenli

            int n = ClassSelectView.Cards.Length;
            var previews = new AvatarView[n];
            var icons = new Image[n];
            var names = new TMP_Text[n];
            var descriptions = new TMP_Text[n];
            var buttons = new Button[n];
            for (int i = 0; i < n; i++)
            {
                RectTransform card = CreateUIObject($"Card_{ClassSelectView.Cards[i]}", cards);
                AddImage(card, PanelColor).raycastTarget = false;
                AddVerticalLayout(card, spacing: 12, padding: 24);
                card.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;

                RectTransform avatarHolder = CreateUIObject("AvatarHolder", card);
                SetHeight(avatarHolder, 260);
                previews[i] = BuildAvatar(avatarHolder, new Vector2(165, 220));
                RectTransform avatarRt = (RectTransform)previews[i].transform;
                avatarRt.anchorMin = avatarRt.anchorMax = new Vector2(0.5f, 0.5f);
                avatarRt.anchoredPosition = Vector2.zero;
                avatarRt.GetComponent<LayoutElement>().ignoreLayout = true;

                RectTransform nameRow = CreateUIObject("NameRow", card);
                SetHeight(nameRow, 64);
                AddHorizontalLayout(nameRow, spacing: 12);
                nameRow.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
                icons[i] = BuildIconBox(nameRow, 60);
                names[i] = CreateText("Name", nameRow, "", 44, 64, TextAlignmentOptions.MidlineLeft);
                names[i].fontStyle = FontStyles.Bold;
                SetFixedWidth(names[i], 260);

                descriptions[i] = CreateText("Description", card, "", 24, 220, TextAlignmentOptions.Top);
                CreateFlexibleSpacer(card);
                buttons[i] = CreateButton("ChooseButton", card, UIStrings.ChooseButton, AdvanceColor, 80, 40, out _);
            }

            var view = root.gameObject.AddComponent<ClassSelectView>();
            SetRef(view, "canvasGroup", group);
            SetRef(view, "titleText", title);
            SetRef(view, "hintText", hint);
            SetRefArray(view, "previews", previews);
            SetRefArray(view, "icons", icons);
            SetRefArray(view, "names", names);
            SetRefArray(view, "descriptions", descriptions);
            SetRefArray(view, "chooseButtons", buttons);

            // Başta gizli (ClassSelectView.Awake de gizler)
            group.alpha = 0f;
            group.blocksRaycasts = false;
            return view;
        }

        // ---------------- Menüler ----------------

        /// <summary>
        /// Tam ekran menü iskeleti: koyu perde (tıklamayı yutar) + ortada panel + başlık + dikey içerik.
        /// İçeriği (butonlar) çağıran ekler. Başta gizli (MenuPanel.Awake de gizler).
        /// </summary>
        private static RectTransform BuildMenuFrame(string name, Transform canvas, string title, Vector2 size, float dim,
                                                    out CanvasGroup group, out RectTransform root, out TMP_Text titleText)
        {
            root = CreateUIObject(name, canvas);
            Stretch(root);
            group = root.gameObject.AddComponent<CanvasGroup>();
            AddImage(root, new Color(0.02f, 0.02f, 0.04f, dim));

            RectTransform panel = CreateUIObject("Panel", root);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = size;
            AddImage(panel, new Color(0.11f, 0.11f, 0.15f, 0.97f)).raycastTarget = false;
            AddVerticalLayout(panel, spacing: 14, padding: 36);

            titleText = CreateText("Title", panel, title, 56, 80);
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(1f, 0.88f, 0.23f);

            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            return panel;
        }

        private static Button MenuButton(Transform parent, string name, string label, Color color, out TMP_Text text, float height = 72)
        {
            return CreateButton(name, parent, label, color, height, 32, out text);
        }

        /// <summary>Ana menü: büyük oyun adı, alt başlık, 5 buton, köşede sürüm.</summary>
        private static MainMenuView BuildMainMenu(Transform canvas)
        {
            RectTransform panel = BuildMenuFrame("MainMenu", canvas, UIStrings.GameTitle, new Vector2(720, 760), 0.85f,
                                                 out CanvasGroup group, out RectTransform root, out TMP_Text title);
            title.fontSize = 96;
            title.enableAutoSizing = true;   // Uzun oyun adı panele sığsın
            title.fontSizeMin = 56;
            title.fontSizeMax = 96;
            LayoutElement titleLe = title.GetComponent<LayoutElement>();
            titleLe.minHeight = titleLe.preferredHeight = 120;
            TMP_Text subtitle = CreateText("Subtitle", panel, UIStrings.GameSubtitle, 26, 44);
            subtitle.color = HintColor;
            CreateUIObject("Gap", panel).gameObject.AddComponent<LayoutElement>().minHeight = 20;

            Button cont = MenuButton(panel, "ContinueButton", UIStrings.MenuContinue, CashOutColor, out _);
            Button newGame = MenuButton(panel, "NewGameButton", UIStrings.MenuNewGame, AdvanceColor, out TMP_Text newGameLabel);
            Button howTo = MenuButton(panel, "HowToPlayButton", UIStrings.MenuHowToPlay, BuyColor, out _);
            Button settings = MenuButton(panel, "SettingsButton", UIStrings.MenuSettings, MutedColor, out _);
            Button quit = MenuButton(panel, "QuitButton", UIStrings.MenuQuit, new Color(0.45f, 0.18f, 0.16f), out _);

            RectTransform versionRt = CreateUIObject("Version", root);
            versionRt.anchorMin = versionRt.anchorMax = new Vector2(1f, 0f);
            versionRt.pivot = new Vector2(1f, 0f);
            versionRt.sizeDelta = new Vector2(400, 40);
            versionRt.anchoredPosition = new Vector2(-20, 12);
            var version = versionRt.gameObject.AddComponent<TextMeshProUGUI>();
            version.fontSize = 20;
            version.alignment = TextAlignmentOptions.BottomRight;
            version.color = HintColor;
            version.raycastTarget = false;

            var view = root.gameObject.AddComponent<MainMenuView>();
            SetRef(view, "canvasGroup", group);
            SetRef(view, "titleText", title);
            SetRef(view, "subtitleText", subtitle);
            SetRef(view, "versionText", version);
            SetRef(view, "continueButton", cont);
            SetRef(view, "newGameButton", newGame);
            SetRef(view, "newGameLabel", newGameLabel);
            SetRef(view, "howToPlayButton", howTo);
            SetRef(view, "settingsButton", settings);
            SetRef(view, "quitButton", quit);
            return view;
        }

        /// <summary>Duraklatma menüsü: Devam, Nasıl Oynanır, Ayarlar, Ana Menü, Oyundan Çık.</summary>
        private static PauseMenuView BuildPauseMenu(Transform canvas)
        {
            RectTransform panel = BuildMenuFrame("PauseMenu", canvas, UIStrings.PauseTitle, new Vector2(620, 620), 0.7f,
                                                 out CanvasGroup group, out RectTransform root, out _);
            Button resume = MenuButton(panel, "ResumeButton", UIStrings.MenuResume, CashOutColor, out _);
            Button howTo = MenuButton(panel, "HowToPlayButton", UIStrings.MenuHowToPlay, BuyColor, out _);
            Button settings = MenuButton(panel, "SettingsButton", UIStrings.MenuSettings, MutedColor, out _);
            Button mainMenu = MenuButton(panel, "MainMenuButton", UIStrings.MenuMainMenu, MutedColor, out TMP_Text mainMenuLabel);
            Button quit = MenuButton(panel, "QuitButton", UIStrings.MenuQuitGame, new Color(0.45f, 0.18f, 0.16f), out _);

            var view = root.gameObject.AddComponent<PauseMenuView>();
            SetRef(view, "canvasGroup", group);
            SetRef(view, "resumeButton", resume);
            SetRef(view, "howToPlayButton", howTo);
            SetRef(view, "settingsButton", settings);
            SetRef(view, "mainMenuButton", mainMenu);
            SetRef(view, "mainMenuLabel", mainMenuLabel);
            SetRef(view, "quitButton", quit);
            return view;
        }

        /// <summary>Ayarlar: ses kaydırıcısı, 3 aç/kapa butonu, kaydı sıfırla, Geri.</summary>
        private static SettingsView BuildSettingsMenu(Transform canvas)
        {
            RectTransform panel = BuildMenuFrame("SettingsMenu", canvas, UIStrings.SettingsTitle, new Vector2(700, 720), 0.6f,
                                                 out CanvasGroup group, out RectTransform root, out _);

            // Ses: etiket + kaydırıcı + değer
            RectTransform volumeRow = CreateUIObject("VolumeRow", panel);
            SetHeight(volumeRow, 64);
            AddHorizontalLayout(volumeRow, spacing: 16);
            TMP_Text volumeLabel = CreateText("Label", volumeRow, UIStrings.SettingsVolume, 30, 64, TextAlignmentOptions.MidlineLeft);
            SetFixedWidth(volumeLabel, 110);
            Slider slider = BuildSlider(volumeRow);
            TMP_Text volumeValue = CreateText("Value", volumeRow, "%70", 28, 64, TextAlignmentOptions.MidlineRight);
            SetFixedWidth(volumeValue, 90);

            Button fullscreen = MenuButton(panel, "FullscreenButton", "", BarBackColor, out TMP_Text fullscreenLabel, 64);
            Button shake = MenuButton(panel, "ShakeButton", "", BarBackColor, out TMP_Text shakeLabel, 64);
            Button flash = MenuButton(panel, "FlashButton", "", BarBackColor, out TMP_Text flashLabel, 64);
            foreach (TMP_Text l in new[] { fullscreenLabel, shakeLabel, flashLabel }) l.fontStyle = FontStyles.Normal;
            CreateFlexibleSpacer(panel);
            Button reset = MenuButton(panel, "ResetButton", UIStrings.SettingsReset, new Color(0.45f, 0.18f, 0.16f), out TMP_Text resetLabel, 64);
            resetLabel.fontSize = 24;
            Button back = MenuButton(panel, "BackButton", UIStrings.SettingsBack, CashOutColor, out _);

            var view = root.gameObject.AddComponent<SettingsView>();
            SetRef(view, "canvasGroup", group);
            SetRef(view, "volumeSlider", slider);
            SetRef(view, "volumeValue", volumeValue);
            SetRef(view, "fullscreenButton", fullscreen);
            SetRef(view, "fullscreenLabel", fullscreenLabel);
            SetRef(view, "shakeButton", shake);
            SetRef(view, "shakeLabel", shakeLabel);
            SetRef(view, "flashButton", flash);
            SetRef(view, "flashLabel", flashLabel);
            SetRef(view, "resetButton", reset);
            SetRef(view, "resetLabel", resetLabel);
            SetRef(view, "backButton", back);
            return view;
        }

        /// <summary>Basit yatay kaydırıcı (arka plan + dolgu + tutamaç). Kalan genişliği kaplar.</summary>
        private static Slider BuildSlider(Transform parent)
        {
            RectTransform root = CreateUIObject("Slider", parent);
            SetHeight(root, 64);
            FillRemainingWidth(root);

            RectTransform bg = CreateUIObject("Background", root);
            bg.anchorMin = new Vector2(0f, 0.4f);
            bg.anchorMax = new Vector2(1f, 0.6f);
            bg.offsetMin = bg.offsetMax = Vector2.zero;
            AddImage(bg, BarBackColor);

            RectTransform fillArea = CreateUIObject("FillArea", root);
            fillArea.anchorMin = new Vector2(0f, 0.4f);
            fillArea.anchorMax = new Vector2(1f, 0.6f);
            fillArea.offsetMin = fillArea.offsetMax = Vector2.zero;
            RectTransform fill = CreateUIObject("Fill", fillArea);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0.7f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            AddImage(fill, new Color(1f, 0.82f, 0.25f)).raycastTarget = false;

            RectTransform handleArea = CreateUIObject("HandleArea", root);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(12, 0);
            handleArea.offsetMax = new Vector2(-12, 0);
            RectTransform handle = CreateUIObject("Handle", handleArea);
            handle.sizeDelta = new Vector2(26, 40);
            Image handleImage = AddImage(handle, Color.white);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            return slider;
        }

        /// <summary>Nasıl oynanır: başlık, metin, sayfa numarası, Önceki / Sonraki / Kapat.</summary>
        private static HowToPlayView BuildHowToPlay(Transform canvas)
        {
            RectTransform panel = BuildMenuFrame("HowToPlay", canvas, "", new Vector2(1100, 720), 0.8f,
                                                 out CanvasGroup group, out RectTransform root, out TMP_Text title);
            title.fontSize = 44;
            TMP_Text body = CreateText("Body", panel, "", 30, 400, TextAlignmentOptions.TopLeft);
            body.enableAutoSizing = true;
            body.fontSizeMin = 20;
            TMP_Text page = CreateText("Page", panel, "", 22, 30);
            page.color = HintColor;

            RectTransform buttons = CreateUIObject("Buttons", panel);
            SetHeight(buttons, 72);
            AddHorizontalLayout(buttons, spacing: 14);
            Button prev = MenuButton(buttons, "PrevButton", UIStrings.HowToPlayPrev, MutedColor, out _);
            FillRemainingWidth(prev);
            Button close = MenuButton(buttons, "CloseButton", UIStrings.HowToPlayClose, BarBackColor, out _);
            FillRemainingWidth(close);
            Button next = MenuButton(buttons, "NextButton", UIStrings.HowToPlayNext, CashOutColor, out TMP_Text nextLabel);
            FillRemainingWidth(next);

            var view = root.gameObject.AddComponent<HowToPlayView>();
            SetRef(view, "canvasGroup", group);
            SetRef(view, "titleText", title);
            SetRef(view, "bodyText", body);
            SetRef(view, "pageText", page);
            SetRef(view, "prevButton", prev);
            SetRef(view, "nextButton", next);
            SetRef(view, "nextLabel", nextLabel);
            SetRef(view, "closeButton", close);
            return view;
        }

        /// <summary>Fareyi takip eden açıklama kutusu (Canvas'ın en üstünde, tıklamaları engellemez).</summary>
        private static void BuildTooltip(Transform canvas)
        {
            RectTransform root = CreateUIObject("Tooltip", canvas);
            Stretch(root);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            RectTransform panel = CreateUIObject("Panel", root);
            panel.anchorMin = panel.anchorMax = new Vector2(0f, 0f);
            panel.pivot = new Vector2(0f, 1f);
            AddImage(panel, new Color(0.06f, 0.06f, 0.09f, 0.96f)).raycastTarget = false;
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 12, 12);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform textRt = CreateUIObject("Text", panel);
            var text = textRt.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = 22;
            text.color = Color.white;
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.TopLeft;
            var le = textRt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 440;

            var view = root.gameObject.AddComponent<TooltipView>();
            SetRef(view, "panel", panel);
            SetRef(view, "text", text);
            SetRef(view, "group", group);
        }

        /// <summary>
        /// Birinci şahıs kazı sahnesi. Katmanlar (alttan üste):
        /// tünel halkaları, tavan çatlağı, delik, duvar (+çatlak +kat yazısı), bulgu (+hüzme +parıltı),
        /// karartma, göçük taşları, parçacıklar, kazma | sahne dışında: kenar karartma, tıklama alanı.
        /// Sprite'lar oyun açılınca kodla üretilir (DigView.Awake / SetDepth), burada sadece yerleri kurulur.
        /// </summary>
        private static DigView BuildDigView(Transform parent)
        {
            // Tüm ekranı kaplar (HUD panelleri üstünde durur)
            RectTransform view = CreateUIObject("DigView", parent);
            Stretch(view);
            AddImage(view, Color.black).raycastTarget = false;
            view.gameObject.AddComponent<RectMask2D>();  // Taşan parçacık ve hüzmeleri keser

            RectTransform scene = CreateUIObject("SceneRoot", view);
            Stretch(scene);

            // Tünel: iç içe 4 çerçeve, içe doğru koyulaşır (derinlik hissi)
            RectTransform tunnel = CreateUIObject("TunnelRoot", scene);
            Stretch(tunnel);
            float[] insets = { 0f, 110f, 210f, 300f };
            var rings = new Image[insets.Length];
            for (int i = 0; i < insets.Length; i++)
            {
                RectTransform ring = CreateUIObject($"Ring_{i}", tunnel);
                Stretch(ring);
                ring.offsetMin = new Vector2(insets[i], insets[i] * 0.9f);
                ring.offsetMax = new Vector2(-insets[i], -insets[i] * 0.9f);
                rings[i] = AddImage(ring, Color.white);
                rings[i].raycastTarget = false;
            }

            RectTransform ceiling = CreateUIObject("CeilingCrack", scene);
            ceiling.anchorMin = new Vector2(0f, 1f);
            ceiling.anchorMax = new Vector2(1f, 1f);
            ceiling.pivot = new Vector2(0.5f, 1f);
            ceiling.sizeDelta = new Vector2(0f, 170f);
            Image ceilingImage = AddImage(ceiling, new Color(1f, 1f, 1f, 0f));
            ceilingImage.raycastTarget = false;

            // Meşale ışığı: duvarı aydınlatan sıcak, titreyen parıltı (renk ve titreme DigView.Update'te)
            Image torchGlow = AddCentered("TorchGlow", scene, new Vector2(1150, 950));

            Image hole = AddCentered("Hole", scene, new Vector2(520, 520));

            // Ganimet ışını: bulgudan yukarı uzanan dikey parıltı (nadir bulgularda açılır)
            RectTransform beamRt = CreateUIObject("LootBeam", scene);
            beamRt.anchorMin = beamRt.anchorMax = new Vector2(0.5f, 0.5f);
            beamRt.pivot = new Vector2(0.5f, 0f);
            beamRt.sizeDelta = new Vector2(150, 800);
            beamRt.anchoredPosition = new Vector2(0f, -60f);
            Image lootBeam = AddImage(beamRt, Color.white);
            lootBeam.raycastTarget = false;

            // Duvar
            RectTransform wall = CreateUIObject("WallRoot", scene);
            wall.anchorMin = wall.anchorMax = new Vector2(0.5f, 0.5f);
            wall.sizeDelta = new Vector2(640, 600);
            RectTransform wallImgRt = CreateUIObject("WallImage", wall);
            Stretch(wallImgRt);
            Image wallImage = AddImage(wallImgRt, Color.white);
            wallImage.raycastTarget = false;
            RectTransform crackRt = CreateUIObject("CrackImage", wall);
            Stretch(crackRt);
            Image crackImage = AddImage(crackRt, Color.white);
            crackImage.raycastTarget = false;
            crackImage.enabled = false;

            RectTransform labelRt = CreateUIObject("DepthLabel", wall);
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.pivot = new Vector2(0.5f, 1f);
            labelRt.sizeDelta = new Vector2(0f, 60f);
            labelRt.anchoredPosition = new Vector2(0f, -12f);
            var depthLabel = labelRt.gameObject.AddComponent<TextMeshProUGUI>();
            depthLabel.fontSize = 44;
            depthLabel.fontStyle = FontStyles.Bold;
            depthLabel.alignment = TextAlignmentOptions.Center;
            depthLabel.color = new Color(1f, 1f, 1f, 0.35f);  // Kayaya kazınmış gibi soluk
            depthLabel.raycastTarget = false;

            // Canavar: kap (ölçeklenmez) > gövde (aura + görsel, büyür/sarsılır) + üstte isim ve can barı
            RectTransform monster = CreateUIObject("MonsterRoot", scene);
            monster.anchorMin = monster.anchorMax = new Vector2(0.5f, 0.5f);
            monster.sizeDelta = new Vector2(440, 440);
            monster.anchoredPosition = new Vector2(0f, -40f);
            RectTransform monsterBody = CreateUIObject("MonsterBody", monster);
            Stretch(monsterBody);
            Image aura = AddCentered("Aura", monsterBody, new Vector2(700, 700));
            RectTransform monsterImgRt = CreateUIObject("MonsterImage", monsterBody);
            Stretch(monsterImgRt);
            Image monsterImage = AddImage(monsterImgRt, Color.white);
            monsterImage.preserveAspect = true;
            monsterImage.raycastTarget = false;

            RectTransform monsterName = CreateUIObject("MonsterName", monster);
            monsterName.anchorMin = monsterName.anchorMax = new Vector2(0.5f, 1f);
            monsterName.sizeDelta = new Vector2(620, 50);
            monsterName.anchoredPosition = new Vector2(0f, 90f);
            var monsterNameText = monsterName.gameObject.AddComponent<TextMeshProUGUI>();
            monsterNameText.fontSize = 32;
            monsterNameText.fontStyle = FontStyles.Bold;
            monsterNameText.alignment = TextAlignmentOptions.Center;
            monsterNameText.color = Color.white;
            monsterNameText.raycastTarget = false;
            monsterNameText.enableAutoSizing = true;
            monsterNameText.fontSizeMin = 20;
            monsterNameText.fontSizeMax = 32;

            Image monsterHpFill = BuildBar("MonsterHpBar", monster, new Vector2(360, 26), new Vector2(0f, 50f),
                                           new Color(0.85f, 0.15f, 0.15f), out TMP_Text monsterHpText);
            // Boss öfke çubuğu: can barının hemen altında, turuncu
            Image enrageFill = BuildBar("EnrageBar", monster, new Vector2(300, 18), new Vector2(0f, 26f),
                                        new Color(1f, 0.55f, 0.1f), out TMP_Text enrageText);

            // Bulgu (delikten çıkan)
            RectTransform item = CreateUIObject("ItemRoot", scene);
            item.anchorMin = item.anchorMax = new Vector2(0.5f, 0.5f);
            item.sizeDelta = new Vector2(240, 240);
            Image rays = AddCentered("Rays", item, new Vector2(540, 540));
            Image glow = AddCentered("Glow", item, new Vector2(320, 320));
            Image itemImage = AddCentered("Item", item, new Vector2(210, 210));
            itemImage.preserveAspect = true;

            RectTransform darkRt = CreateUIObject("DarkOverlay", scene);
            Stretch(darkRt);
            Image dark = AddImage(darkRt, new Color(0f, 0f, 0f, 0f));
            dark.raycastTarget = false;

            RectTransform rocks = CreateUIObject("FallenRocks", scene);
            Stretch(rocks);

            // Parçacık üretici + şablonu
            RectTransform particlesRt = CreateUIObject("Particles", scene);
            Stretch(particlesRt);
            RectTransform templateRt = CreateUIObject("ParticleTemplate", particlesRt);
            templateRt.sizeDelta = new Vector2(12, 12);
            Image template = AddImage(templateRt, Color.white);
            template.raycastTarget = false;
            templateRt.gameObject.SetActive(false);
            var spawner = particlesRt.gameObject.AddComponent<UIParticleSpawner>();
            SetRef(spawner, "template", template);

            // Kazma: sağ altta, sağ HUD panelinin solunda (panelin altında kalmasın). Dönme noktası sapın ucu
            RectTransform pickaxe = CreateUIObject("PickaxeRoot", scene);
            pickaxe.anchorMin = pickaxe.anchorMax = new Vector2(1f, 0f);
            pickaxe.pivot = new Vector2(0.5f, 0.08f);
            pickaxe.sizeDelta = new Vector2(380, 380);
            pickaxe.anchoredPosition = new Vector2(-(HudSideWidth + 230f), -50f);
            RectTransform pickaxeImgRt = CreateUIObject("PickaxeImage", pickaxe);
            Stretch(pickaxeImgRt);
            Image pickaxeImage = AddImage(pickaxeImgRt, Color.white);
            pickaxeImage.preserveAspect = true;
            pickaxeImage.raycastTarget = false;

            // Silah: sol altta, sol HUD panelinin sağında (diğer el), kazmanın aynası
            RectTransform weapon = CreateUIObject("WeaponRoot", scene);
            weapon.anchorMin = weapon.anchorMax = new Vector2(0f, 0f);
            // Dönme noktası kabzada (kılıç çiziminde sol alt), ekranın biraz yukarısında: kalkık durur, vuruşta canavara iner
            weapon.pivot = new Vector2(0.15f, 0.12f);
            weapon.sizeDelta = new Vector2(380, 380);
            weapon.anchoredPosition = new Vector2(HudSideWidth + 110f, 40f);
            RectTransform weaponImgRt = CreateUIObject("WeaponImage", weapon);
            Stretch(weaponImgRt);
            Image weaponImage = AddImage(weaponImgRt, Color.white);
            weaponImage.preserveAspect = true;
            weaponImage.raycastTarget = false;

            // Sahne dışı (sarsılmayan) katmanlar
            RectTransform vignetteRt = CreateUIObject("Vignette", view);
            Stretch(vignetteRt);
            Image vignette = AddImage(vignetteRt, Color.black);
            vignette.raycastTarget = false;

            // Oyuncu can barı: üst barın hemen altında, ortada
            Image playerHpFill = BuildBar("PlayerHpBar", view, new Vector2(520, 32), Vector2.zero,
                                          new Color(0.25f, 0.8f, 0.3f), out TMP_Text playerHpText);
            RectTransform playerBar = (RectTransform)playerHpFill.transform.parent;
            playerBar.anchorMin = playerBar.anchorMax = new Vector2(0.5f, 1f);
            playerBar.pivot = new Vector2(0.5f, 1f);
            playerBar.anchoredPosition = new Vector2(0f, -(TopBarHeight + 16f));

            RectTransform catcherRt = CreateUIObject("ClickCatcher", view);
            Stretch(catcherRt);
            Image catcherImage = AddImage(catcherRt, new Color(0f, 0f, 0f, 0f));  // Görünmez ama tıklanır
            var catcher = catcherRt.gameObject.AddComponent<Button>();
            catcher.transition = Selectable.Transition.None;
            catcher.navigation = new Navigation { mode = Navigation.Mode.None };
            catcher.targetGraphic = catcherImage;

            var dig = view.gameObject.AddComponent<DigView>();
            SetRef(dig, "sceneRoot", scene);
            SetRefArray(dig, "tunnelRings", rings);
            SetRef(dig, "tunnelRoot", tunnel);
            SetRef(dig, "ceilingCrack", ceilingImage);
            SetRef(dig, "wallRoot", wall);
            SetRef(dig, "wallImage", wallImage);
            SetRef(dig, "crackImage", crackImage);
            SetRef(dig, "depthLabel", depthLabel);
            SetRef(dig, "holeImage", hole);
            SetRef(dig, "itemRoot", item);
            SetRef(dig, "raysImage", rays);
            SetRef(dig, "glowImage", glow);
            SetRef(dig, "itemImage", itemImage);
            SetRef(dig, "pickaxeRoot", pickaxe);
            SetRef(dig, "pickaxeImage", pickaxeImage);
            SetRef(dig, "vignette", vignette);
            SetRef(dig, "darkOverlay", dark);
            SetRef(dig, "fallenRocksRoot", rocks);
            SetRef(dig, "particles", spawner);
            SetRef(dig, "clickCatcher", catcher);
            SetRef(dig, "monsterRoot", monster);
            SetRef(dig, "monsterBody", monsterBody);
            SetRef(dig, "monsterImage", monsterImage);
            SetRef(dig, "monsterAura", aura);
            SetRef(dig, "monsterNameText", monsterNameText);
            SetRef(dig, "monsterHpFill", monsterHpFill.rectTransform);
            SetRef(dig, "monsterHpText", monsterHpText);
            SetRef(dig, "weaponRoot", weapon);
            SetRef(dig, "weaponImage", weaponImage);
            SetRef(dig, "playerHpFill", playerHpFill.rectTransform);
            SetRef(dig, "playerHpFillImage", playerHpFill);
            SetRef(dig, "playerHpText", playerHpText);
            SetRef(dig, "torchGlow", torchGlow);
            SetRef(dig, "lootBeam", lootBeam);
            SetRef(dig, "enrageBar", (RectTransform)enrageFill.transform.parent);
            SetRef(dig, "enrageFill", enrageFill.rectTransform);
            SetRef(dig, "enrageText", enrageText);
            return dig;
        }

        /// <summary>
        /// Can barı: koyu arka plan + soldan dolan renkli şerit + ortada metin.
        /// Dolu kısmın Image'ını döndürür (genişliği anchorMax.x ile ayarlanır).
        /// </summary>
        private static Image BuildBar(string name, Transform parent, Vector2 size, Vector2 position, Color fillColor, out TMP_Text text)
        {
            RectTransform bar = CreateUIObject(name, parent);
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 1f);
            bar.sizeDelta = size;
            bar.anchoredPosition = position;
            AddImage(bar, new Color(0.08f, 0.08f, 0.1f, 0.9f)).raycastTarget = false;

            RectTransform fill = CreateUIObject("Fill", bar);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(3, 3);
            fill.offsetMax = new Vector2(-3, -3);
            Image fillImage = AddImage(fill, fillColor);
            fillImage.raycastTarget = false;

            RectTransform textRt = CreateUIObject("Text", bar);
            Stretch(textRt);
            text = textRt.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = size.y * 0.7f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            return fillImage;
        }

        /// <summary>Yan yana dizilimde koyu kare kutu + içinde pixel art ikon (yükseltme, Kadim satırları). İkonu döndürür.</summary>
        private static Image BuildIconBox(Transform parent, float size)
        {
            RectTransform box = CreateUIObject("IconBox", parent);
            SetHeight(box, size);
            SetFixedWidth(box, size);
            AddImage(box, new Color(0.06f, 0.06f, 0.09f)).raycastTarget = false;
            RectTransform iconRt = CreateUIObject("Icon", box);
            Stretch(iconRt);
            iconRt.offsetMin = new Vector2(8, 8);
            iconRt.offsetMax = new Vector2(-8, -8);
            Image icon = AddImage(iconRt, Color.white);
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            return icon;
        }

        /// <summary>Ebeveyninin ortasında sabit boyutlu, tıklanmayan Image.</summary>
        private static Image AddCentered(string name, Transform parent, Vector2 size)
        {
            RectTransform rt = CreateUIObject(name, parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            Image image = AddImage(rt, Color.white);
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Bu turun bulguları: başlık + 3x3 kart ızgarası (9 kutu, fazlası "+N daha").</summary>
        private static FindCollectionView BuildFindCollection(Transform parent)
        {
            const int columns = 3;
            const int slotCount = 9;
            const float gridHeight = 3 * 100 + 2 * 8;

            RectTransform root = CreateUIObject("FindCollection", parent);
            SetHeight(root, 34 + 8 + gridHeight);
            AddVerticalLayout(root, spacing: 8, padding: 0);

            TMP_Text title = CreateText("CollectionTitle", root, "", 22, 34);
            title.color = HintColor;

            RectTransform grid = CreateUIObject("Grid", root);
            SetHeight(grid, gridHeight);
            var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(136, 100);
            gridLayout.spacing = new Vector2(8, 8);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = columns;
            gridLayout.childAlignment = TextAnchor.UpperCenter;

            var slots = new FindMiniCardView[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                slots[i] = BuildMiniCard(grid, i);
            }

            var view = root.gameObject.AddComponent<FindCollectionView>();
            SetRef(view, "titleText", title);
            SetRefArray(view, "slots", slots);
            return view;
        }

        /// <summary>Küçük bulgu kartı: renkli çerçeve + koyu iç + tür adı + değer + sağ üstte adet.</summary>
        private static FindMiniCardView BuildMiniCard(Transform parent, int index)
        {
            RectTransform card = CreateUIObject($"MiniCard_{index}", parent);
            Image frame = AddImage(card, Color.white);
            // raycastTarget açık: fareyle üstüne gelince ipucu (ne verdiği)

            RectTransform inner = CreateUIObject("Inner", card);
            Stretch(inner);
            inner.offsetMin = new Vector2(5, 5);
            inner.offsetMax = new Vector2(-5, -5);
            AddImage(inner, BackgroundColor).raycastTarget = false;
            AddVerticalLayout(inner, spacing: 0, padding: 3);
            inner.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;

            RectTransform iconRt = CreateUIObject("Icon", inner);
            SetHeight(iconRt, 38);
            Image icon = AddImage(iconRt, Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            TMP_Text type = CreateText("TypeText", inner, "", 20, 22);
            type.fontStyle = FontStyles.Bold;
            TMP_Text value = CreateText("ValueText", inner, "", 18, 22);

            // Adet rozeti: sağ üst köşe, düzen grubunun dışında
            RectTransform badge = CreateUIObject("CountText", card);
            badge.anchorMin = badge.anchorMax = new Vector2(1, 1);
            badge.pivot = new Vector2(1, 1);
            badge.sizeDelta = new Vector2(60, 30);
            badge.anchoredPosition = new Vector2(-7, -5);
            var count = badge.gameObject.AddComponent<TextMeshProUGUI>();
            count.fontSize = 22;
            count.fontStyle = FontStyles.Bold;
            count.alignment = TextAlignmentOptions.TopRight;
            count.color = Color.white;
            count.raycastTarget = false;

            var view = card.gameObject.AddComponent<FindMiniCardView>();
            SetRef(view, "frame", frame);
            SetRef(view, "icon", icon);
            SetRef(view, "typeText", type);
            SetRef(view, "valueText", value);
            SetRef(view, "countText", count);
            SetRef(view, "tooltip", card.gameObject.AddComponent<TooltipTrigger>());
            card.gameObject.SetActive(false);  // Bulgu gelene kadar gizli
            return view;
        }

        /// <summary>Ortada açılan büyük kart + arkasında karartma. Açıkken arkadaki butonlara tıklanamaz.</summary>
        private static RevealPopupView BuildRevealPopup(Transform canvas)
        {
            RectTransform root = CreateUIObject("RevealPopup", canvas);
            Stretch(root);
            var group = root.gameObject.AddComponent<CanvasGroup>();

            RectTransform dim = CreateUIObject("Dim", root);
            Stretch(dim);
            AddImage(dim, new Color(0f, 0f, 0f, 0.65f));  // raycastTarget açık: tıklamayı yutar

            RectTransform card = CreateUIObject("Card", root);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(980, 660);
            card.anchoredPosition = Vector2.zero;
            Image frame = AddImage(card, Color.white);
            frame.raycastTarget = false;

            RectTransform inner = CreateUIObject("Inner", card);
            Stretch(inner);
            inner.offsetMin = new Vector2(12, 12);
            inner.offsetMax = new Vector2(-12, -12);
            AddImage(inner, PanelColor).raycastTarget = false;
            AddVerticalLayout(inner, spacing: 10, padding: 24);

            TMP_Text header = CreateText("HeaderText", inner, "", 34, 50);
            header.color = new Color(1f, 0.88f, 0.23f);
            header.fontStyle = FontStyles.Bold;

            // İkon alanı: arkada dönen hüzme, önde pixel art (düzen grubu sadece yüksekliği ayırır)
            RectTransform iconArea = CreateUIObject("IconArea", inner);
            SetHeight(iconArea, 170);
            Image iconRays = AddCentered("IconRays", iconArea, new Vector2(360, 360));
            Image icon = AddCentered("Icon", iconArea, new Vector2(170, 170));
            icon.preserveAspect = true;

            TMP_Text title = CreateText("TitleText", inner, "", 60, 80);
            title.fontStyle = FontStyles.Bold;
            TMP_Text description = CreateText("DescriptionText", inner, "", 36, 60);
            TMP_Text hype = CreateText("HypeText", inner, "", 34, 110);
            CreateFlexibleSpacer(inner);
            TMP_Text cont = CreateText("ContinueText", inner, "", 26, 44);
            cont.color = HintColor;

            var view = root.gameObject.AddComponent<RevealPopupView>();
            SetRef(view, "canvasGroup", group);
            SetRef(view, "card", card);
            SetRef(view, "frame", frame);
            SetRef(view, "headerText", header);
            SetRef(view, "titleText", title);
            SetRef(view, "descriptionText", description);
            SetRef(view, "hypeText", hype);
            SetRef(view, "continueText", cont);
            SetRef(view, "iconImage", icon);
            SetRef(view, "iconRays", iconRays);

            // Başlangıçta gizli (RevealPopupView.Awake de gizler)
            group.alpha = 0f;
            group.blocksRaycasts = false;
            return view;
        }

        /// <summary>En üst katman: flaş, uçan yazılar, parçacıklar, büyük yazı. Tıklamaları engellemez.</summary>
        private static FxLayerView BuildFxLayer(Transform canvas, RectTransform shakeTarget)
        {
            RectTransform layer = CreateUIObject("FxLayer", canvas);
            Stretch(layer);

            RectTransform flashRt = CreateUIObject("Flash", layer);
            Stretch(flashRt);
            Image flash = AddImage(flashRt, new Color(1f, 1f, 1f, 0f));
            flash.raycastTarget = false;

            // Uçan yazı şablonu (kapalı; kopyaları kullanılır)
            RectTransform floatRt = CreateUIObject("FloatingTextTemplate", layer);
            floatRt.sizeDelta = new Vector2(1200, 100);
            var floating = floatRt.gameObject.AddComponent<TextMeshProUGUI>();
            floating.fontSize = 44;
            floating.fontStyle = FontStyles.Bold;
            floating.alignment = TextAlignmentOptions.Center;
            floating.raycastTarget = false;
            floatRt.gameObject.SetActive(false);

            // Parçacık şablonu: küçük kare
            RectTransform particleRt = CreateUIObject("ParticleTemplate", layer);
            particleRt.sizeDelta = new Vector2(16, 16);
            Image particle = AddImage(particleRt, Color.white);
            particle.raycastTarget = false;
            particleRt.gameObject.SetActive(false);

            // Ortadaki büyük yazı
            RectTransform bannerRt = CreateUIObject("BannerText", layer);
            bannerRt.anchorMin = bannerRt.anchorMax = new Vector2(0.5f, 0.5f);
            // Açılır kartın (660 yüksekliğinde, ortada) hemen üstünde dursun ki ikisi aynı anda görünebilsin
            bannerRt.sizeDelta = new Vector2(1800, 160);
            bannerRt.anchoredPosition = new Vector2(0, 430);
            var banner = bannerRt.gameObject.AddComponent<TextMeshProUGUI>();
            banner.fontSize = 96;
            banner.fontStyle = FontStyles.Bold;
            banner.alignment = TextAlignmentOptions.Center;
            banner.raycastTarget = false;
            banner.alpha = 0f;

            var view = layer.gameObject.AddComponent<FxLayerView>();
            SetRef(view, "shakeTarget", shakeTarget);
            SetRef(view, "flashImage", flash);
            SetRef(view, "floatingTextTemplate", floating);
            SetRef(view, "particleTemplate", particle);
            SetRef(view, "bannerText", banner);
            return view;
        }

        private static UpgradePanelView BuildUpgradePanel(Transform parent)
        {
            RectTransform panel = CreatePanelWithColumns("UpgradePanel", parent);

            // ---- Sol sütun: son turun sonucu, seçili eşya detayı, geçit seçici, başla butonu ----
            RectTransform left = CreateColumn("LeftColumn", panel, flexibleWidth: 1);
            TMP_Text result = CreateText("ResultText", left, "", 26, 140);
            ItemDetailView detail = BuildItemDetail(left);
            CreateFlexibleSpacer(left);
            TMP_Text nextUnlock = CreateText("NextUnlockText", left, "", 22, 36);
            nextUnlock.color = new Color(0.55f, 0.85f, 1f);
            Button startStep = CreateButton("StartStepButton", left, UIStrings.StartFrom(0), BarBackColor, 52, 24, out TMP_Text startStepLabel);
            Button startRun = CreateButton("StartRunButton", left, UIStrings.StartRunButton, AdvanceColor, 120, 56, out _);
            TMP_Text hint = CreateText("KeyHintText", left, UIStrings.KeyHintUpgrade, 22, 36);
            hint.color = HintColor;

            // ---- Sağ sütun: sekmeler + içerik ----
            RectTransform right = CreateColumn("RightColumn", panel, flexibleWidth: 1.3f);

            RectTransform tabs = CreateUIObject("Tabs", right);
            SetHeight(tabs, 60);
            AddHorizontalLayout(tabs, spacing: 10);
            Button upgradesTab = CreateButton("UpgradesTab", tabs, UIStrings.TabUpgrades, BuyColor, 60, 26, out _);
            FillRemainingWidth(upgradesTab);
            Button talentsTab = CreateButton("TalentsTab", tabs, UIStrings.TabTalents, MutedColor, 60, 26, out TMP_Text talentsTabLabel);
            FillRemainingWidth(talentsTab);
            Button spellsTab = CreateButton("SpellsTab", tabs, UIStrings.TabSpells, MutedColor, 60, 26, out TMP_Text spellsTabLabel);
            FillRemainingWidth(spellsTab);
            Button equipmentTab = CreateButton("EquipmentTab", tabs, UIStrings.TabEquipment, MutedColor, 60, 26, out TMP_Text equipmentTabLabel);
            FillRemainingWidth(equipmentTab);
            Button prestigeTab = CreateButton("PrestigeTab", tabs, UIStrings.TabPrestige, MutedColor, 60, 26, out TMP_Text prestigeTabLabel);
            FillRemainingWidth(prestigeTab);

            RectTransform content = CreateUIObject("TabContent", right);
            content.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

            // Yükseltmeler sekmesi: her tür için bir satır (enum sırasıyla). Yeni tür eklenince otomatik gelir.
            RectTransform upgradesRoot = CreateUIObject("UpgradesRoot", content);
            Stretch(upgradesRoot);
            AddVerticalLayout(upgradesRoot, spacing: 10, padding: 0);
            var rows = new List<UpgradeRowView>();
            foreach (UpgradeType type in System.Enum.GetValues(typeof(UpgradeType)))
            {
                rows.Add(BuildUpgradeRow(upgradesRoot, type));
            }

            // Yetenek, Ekipman ve Yeni Çağ sekmeleri
            TalentTreeView talents = BuildTalentTree(content);
            SpellBookView spellBook = BuildSpellBook(content);
            EquipmentView equipment = BuildEquipmentView(content);
            PrestigePanelView prestige = BuildPrestigePanel(content);

            var view = panel.gameObject.AddComponent<UpgradePanelView>();
            SetRef(view, "resultText", result);
            SetRefArray(view, "rows", rows.ToArray());
            SetRef(view, "startRunButton", startRun);
            SetRef(view, "startStepButton", startStep);
            SetRef(view, "startStepLabel", startStepLabel);
            SetRef(view, "nextUnlockText", nextUnlock);
            SetRef(view, "talentsTab", talentsTab);
            SetRef(view, "talentsTabImage", talentsTab.GetComponent<Image>());
            SetRef(view, "talentsTabLabel", talentsTabLabel);
            SetRef(view, "equipmentTabLabel", equipmentTabLabel);
            SetRef(view, "prestigeTabLabel", prestigeTabLabel);
            SetRef(view, "talentsRoot", talents.gameObject);
            SetRef(view, "talents", talents);
            SetRef(view, "spellsTab", spellsTab);
            SetRef(view, "spellsTabImage", spellsTab.GetComponent<Image>());
            SetRef(view, "spellsTabLabel", spellsTabLabel);
            SetRef(view, "spellsRoot", spellBook.gameObject);
            SetRef(view, "spellBook", spellBook);
            SetRef(view, "upgradesTab", upgradesTab);
            SetRef(view, "equipmentTab", equipmentTab);
            SetRef(view, "prestigeTab", prestigeTab);
            SetRef(view, "upgradesTabImage", upgradesTab.GetComponent<Image>());
            SetRef(view, "equipmentTabImage", equipmentTab.GetComponent<Image>());
            SetRef(view, "prestigeTabImage", prestigeTab.GetComponent<Image>());
            SetRef(view, "upgradesRoot", upgradesRoot.gameObject);
            SetRef(view, "equipmentRoot", equipment.gameObject);
            SetRef(view, "prestigeRoot", prestige.gameObject);
            SetRef(view, "equipment", equipment);
            SetRef(view, "itemDetail", detail);
            SetRef(view, "prestige", prestige);
            return view;
        }

        /// <summary>
        /// Yetenekler sekmesi: üstte puan, 3 dal yan yana (her dalda 4 sıra düğüm), altta sıfırla.
        /// Düğümler TalentBalance'taki varsayılan listeden kurulur; yeni yetenek eklenince sahneyi yeniden kur.
        /// </summary>
        private static TalentTreeView BuildTalentTree(Transform parent)
        {
            const float nodeWidth = 150;
            const float nodeHeight = 78;
            var balance = new TalentBalance();

            RectTransform root = CreateUIObject("TalentsRoot", parent);
            Stretch(root);
            AddVerticalLayout(root, spacing: 10, padding: 0);

            TMP_Text points = CreateText("PointsText", root, "", 30, 44);
            TMP_Text message = CreateText("MessageText", root, "", 20, 28);
            message.color = new Color(1f, 0.88f, 0.23f);

            var branches = (TalentBranch[])System.Enum.GetValues(typeof(TalentBranch));
            int maxRow = 0;
            foreach (TalentDefinition t in balance.nodes) maxRow = Mathf.Max(maxRow, t.row);

            RectTransform columns = CreateUIObject("Branches", root);
            SetHeight(columns, 70 + (maxRow + 1) * (nodeHeight + 12));
            AddHorizontalLayout(columns, spacing: 14);

            var headers = new TMP_Text[branches.Length];
            var nodes = new List<TalentNodeView>();
            for (int b = 0; b < branches.Length; b++)
            {
                RectTransform column = CreateColumn($"Branch_{branches[b]}", columns, flexibleWidth: 1);
                AddImage(column, new Color(0f, 0f, 0f, 0.25f)).raycastTarget = false;
                headers[b] = CreateText("Header", column, UIStrings.BranchName(branches[b]), 24, 60);

                for (int r = 0; r <= maxRow; r++)
                {
                    RectTransform row = CreateUIObject($"Row_{r}", column);
                    SetHeight(row, nodeHeight);
                    AddHorizontalLayout(row, spacing: 10);
                    row.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
                    foreach (TalentDefinition t in balance.nodes)
                    {
                        if (t.branch == branches[b] && t.row == r) nodes.Add(BuildTalentNode(row, t, new Vector2(nodeWidth, nodeHeight)));
                    }
                }
            }

            CreateFlexibleSpacer(root);
            Button respec = CreateButton("RespecButton", root, "", MutedColor, 56, 24, out TMP_Text respecLabel);

            // Kilit perdesi: yetenekler açılmadan önce ağacın üstünü örter (düzenin dışında)
            RectTransform lockRt = CreateUIObject("LockedOverlay", root);
            lockRt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Stretch(lockRt);
            AddImage(lockRt, new Color(0.05f, 0.05f, 0.08f, 0.88f));   // Tıklamayı yutar
            RectTransform lockTextRt = CreateUIObject("Text", lockRt);
            Stretch(lockTextRt);
            var lockText = lockTextRt.gameObject.AddComponent<TextMeshProUGUI>();
            lockText.fontSize = 34;
            lockText.alignment = TextAlignmentOptions.Center;
            lockText.color = HintColor;
            lockText.raycastTarget = false;

            var view = root.gameObject.AddComponent<TalentTreeView>();
            SetRef(view, "pointsText", points);
            SetRef(view, "messageText", message);
            SetRefArray(view, "branchHeaders", headers);
            SetRefArray(view, "nodes", nodes.ToArray());
            SetRef(view, "respecButton", respec);
            SetRef(view, "respecLabel", respecLabel);
            SetRef(view, "lockedOverlay", lockRt.gameObject);
            SetRef(view, "lockedText", lockText);
            return view;
        }

        /// <summary>Tek yetenek düğümü: renkli çerçeve + koyu iç + ad + "0/5". Tıklanır, ipucu verir.</summary>
        private static TalentNodeView BuildTalentNode(Transform parent, TalentDefinition t, Vector2 size)
        {
            RectTransform root = CreateUIObject($"Talent_{t.id}", parent);
            SetHeight(root, size.y);
            SetFixedWidth(root, size.x);
            Image border = AddImage(root, Color.gray);   // raycastTarget açık: tıklama + ipucu

            RectTransform innerRt = CreateUIObject("Inner", root);
            Stretch(innerRt);
            innerRt.offsetMin = new Vector2(4, 4);
            innerRt.offsetMax = new Vector2(-4, -4);
            Image background = AddImage(innerRt, BackgroundColor);
            background.raycastTarget = false;

            RectTransform talentIconRt = CreateUIObject("Icon", root);
            talentIconRt.anchorMin = talentIconRt.anchorMax = new Vector2(0.5f, 1f);
            talentIconRt.pivot = new Vector2(0.5f, 1f);
            talentIconRt.sizeDelta = new Vector2(34, 34);
            talentIconRt.anchoredPosition = new Vector2(0f, -6f);
            Image talentIcon = AddImage(talentIconRt, Color.white);
            talentIcon.raycastTarget = false;
            talentIcon.preserveAspect = true;

            TMP_Text nameText = AddChipText(root, "Name", 16, new Vector2(4, 22), new Vector2(-4, -40), TextAlignmentOptions.Center);
            nameText.fontStyle = FontStyles.Bold;
            TMP_Text rankText = AddChipText(root, "Rank", 16, new Vector2(4, 3), new Vector2(-4, -58), TextAlignmentOptions.Center);
            rankText.color = new Color(1f, 0.88f, 0.23f);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = border;
            button.transition = Selectable.Transition.None;   // Renkleri TalentNodeView yönetir
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var tip = root.gameObject.AddComponent<TooltipTrigger>();
            var view = root.gameObject.AddComponent<TalentNodeView>();
            SetString(view, "talentId", t.id);
            SetRef(view, "button", button);
            SetRef(view, "background", background);
            SetRef(view, "border", border);
            SetRef(view, "nameText", nameText);
            SetRef(view, "rankText", rankText);
            SetRef(view, "icon", talentIcon);
            SetRef(view, "tooltip", tip);
            return view;
        }

        /// <summary>Ekipman sekmesi: güç + stat özeti, 9 karakter yuvası, envanter ızgarası, "Çantayı sat".</summary>
        private static EquipmentView BuildEquipmentView(Transform parent)
        {
            const int inventorySlots = 32;   // CombatBalance.inventoryCap ile aynı tut
            const float cell = 96;
            var slotEnum = (GearSlot[])System.Enum.GetValues(typeof(GearSlot));

            RectTransform root = CreateUIObject("EquipmentRoot", parent);
            Stretch(root);
            AddVerticalLayout(root, spacing: 10, padding: 0);

            // Üst satır: büyük karakter (giydiklerin üstünde) + güç ve stat özeti
            RectTransform top = CreateUIObject("Top", root);
            SetHeight(top, 220);
            AddHorizontalLayout(top, spacing: 20);
            AvatarView avatar = BuildAvatar(top, new Vector2(165, 220));
            TMP_Text stats = CreateText("StatsText", top, "", 22, 220, TextAlignmentOptions.MidlineLeft);
            FillRemainingWidth(stats);

            RectTransform doll = CreateUIObject("EquippedSlots", root);
            SetHeight(doll, cell);
            var dollGrid = doll.gameObject.AddComponent<GridLayoutGroup>();
            dollGrid.cellSize = new Vector2(cell, cell);
            dollGrid.spacing = new Vector2(6, 6);
            dollGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            dollGrid.constraintCount = slotEnum.Length;
            dollGrid.childAlignment = TextAnchor.UpperCenter;
            var equipped = new GearSlotView[slotEnum.Length];
            for (int i = 0; i < slotEnum.Length; i++) equipped[i] = BuildGearSlot(doll, $"Slot_{slotEnum[i]}", withLabel: true);

            TMP_Text invTitle = CreateText("InventoryTitle", root, "", 22, 30);
            invTitle.color = HintColor;

            const float invCell = 80;
            RectTransform inv = CreateUIObject("Inventory", root);
            SetHeight(inv, 4 * invCell + 3 * 6);
            var invGrid = inv.gameObject.AddComponent<GridLayoutGroup>();
            invGrid.cellSize = new Vector2(invCell, invCell);
            invGrid.spacing = new Vector2(6, 6);
            invGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            invGrid.constraintCount = 8;
            invGrid.childAlignment = TextAnchor.UpperCenter;
            var invSlots = new GearSlotView[inventorySlots];
            for (int i = 0; i < inventorySlots; i++) invSlots[i] = BuildGearSlot(inv, $"Inv_{i}", withLabel: false);

            RectTransform buttons = CreateUIObject("Buttons", root);
            SetHeight(buttons, 56);
            AddHorizontalLayout(buttons, spacing: 12);
            Button sellAll = CreateButton("SellAllButton", buttons, UIStrings.SellAllButton, AdvanceColor, 56, 22, out TMP_Text sellAllLabel);
            FillRemainingWidth(sellAll);

            var view = root.gameObject.AddComponent<EquipmentView>();
            SetRef(view, "avatar", avatar);
            SetRef(view, "statsText", stats);
            SetRefArray(view, "equippedSlots", equipped);
            SetRef(view, "inventoryTitle", invTitle);
            SetRefArray(view, "inventorySlots", invSlots);
            SetRef(view, "sellAllButton", sellAll);
            SetRef(view, "sellAllLabel", sellAllLabel);
            return view;
        }

        /// <summary>Yeni Çağ sekmesi: çağ + Yadigar, açıklama, 5 Kadim yükseltme, "Yeni Çağ başlat".</summary>
        private static PrestigePanelView BuildPrestigePanel(Transform parent)
        {
            RectTransform root = CreateUIObject("PrestigeRoot", parent);
            Stretch(root);
            AddVerticalLayout(root, spacing: 10, padding: 0);

            TMP_Text header = CreateText("HeaderText", root, "", 34, 50);
            TMP_Text info = CreateText("InfoText", root, "", 22, 110);

            var rows = new List<PrestigeRowView>();
            foreach (PrestigeType type in System.Enum.GetValues(typeof(PrestigeType)))
            {
                const float rowHeight = 84;
                RectTransform row = CreateUIObject($"PrestigeRow_{type}", root);
                SetHeight(row, rowHeight);
                AddHorizontalLayout(row, spacing: 20);
                Image prIcon = BuildIconBox(row, rowHeight - 10);
                TMP_Text rowInfo = CreateText("InfoText", row, "", 26, rowHeight, TextAlignmentOptions.MidlineLeft);
                FillRemainingWidth(rowInfo);
                Button buy = CreateButton("BuyButton", row, "Al", new Color(0.55f, 0.35f, 0.75f), rowHeight, 26, out TMP_Text buyText);
                SetFixedWidth(buy, 260);

                var rowView = row.gameObject.AddComponent<PrestigeRowView>();
                SetEnum(rowView, "type", (int)type);
                SetRef(rowView, "infoText", rowInfo);
                SetRef(rowView, "buyButton", buy);
                SetRef(rowView, "buyText", buyText);
                SetRef(rowView, "icon", prIcon);
                rows.Add(rowView);
            }

            CreateFlexibleSpacer(root);
            Button prestigeButton = CreateButton("PrestigeButton", root, "", new Color(0.70f, 0.20f, 0.25f), 70, 26, out TMP_Text prestigeLabel);

            var view = root.gameObject.AddComponent<PrestigePanelView>();
            SetRef(view, "headerText", header);
            SetRef(view, "infoText", info);
            SetRefArray(view, "rows", rows.ToArray());
            SetRef(view, "prestigeButton", prestigeButton);
            SetRef(view, "prestigeLabel", prestigeLabel);
            return view;
        }

        /// <summary>Tek eşya kutusu: nadirlik çerçevesi + koyu iç + ikon (+ istenirse altta yuva adı). Tıklanabilir.</summary>
        private static GearSlotView BuildGearSlot(Transform parent, string name, bool withLabel)
        {
            RectTransform root = CreateUIObject(name, parent);
            Image frame = AddImage(root, Color.gray);

            RectTransform innerRt = CreateUIObject("Inner", root);
            Stretch(innerRt);
            innerRt.offsetMin = new Vector2(4, 4);
            innerRt.offsetMax = new Vector2(-4, -4);
            Image inner = AddImage(innerRt, BackgroundColor);
            inner.raycastTarget = false;

            RectTransform slotGlowRt = CreateUIObject("EnhanceGlow", root);
            Stretch(slotGlowRt);
            Image slotGlow = AddImage(slotGlowRt, Color.white);
            slotGlow.raycastTarget = false;

            RectTransform iconRt = CreateUIObject("Icon", root);
            Stretch(iconRt);
            iconRt.offsetMin = new Vector2(12, withLabel ? 22 : 12);
            iconRt.offsetMax = new Vector2(-12, -10);
            Image icon = AddImage(iconRt, Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TMP_Text label = null;
            if (withLabel)
            {
                RectTransform labelRt = CreateUIObject("Label", root);
                labelRt.anchorMin = new Vector2(0f, 0f);
                labelRt.anchorMax = new Vector2(1f, 0f);
                labelRt.pivot = new Vector2(0.5f, 0f);
                labelRt.sizeDelta = new Vector2(0f, 22f);
                labelRt.anchoredPosition = new Vector2(0f, 4f);
                label = labelRt.gameObject.AddComponent<TextMeshProUGUI>();
                label.fontSize = 16;
                label.alignment = TextAlignmentOptions.Center;
                label.color = HintColor;
                label.raycastTarget = false;
            }

            RectTransform plusRt = CreateUIObject("EnhanceText", root);
            plusRt.anchorMin = plusRt.anchorMax = new Vector2(1f, 1f);
            plusRt.pivot = new Vector2(1f, 1f);
            plusRt.sizeDelta = new Vector2(44, 24);
            plusRt.anchoredPosition = new Vector2(-5, -3);
            var plus = plusRt.gameObject.AddComponent<TextMeshProUGUI>();
            plus.fontSize = 18;
            plus.fontStyle = FontStyles.Bold;
            plus.alignment = TextAlignmentOptions.TopRight;
            plus.raycastTarget = false;

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var view = root.gameObject.AddComponent<GearSlotView>();
            SetRef(view, "glow", slotGlow);
            SetRef(view, "enhanceText", plus);
            SetRef(view, "frame", frame);
            SetRef(view, "inner", inner);
            SetRef(view, "icon", icon);
            if (label != null) SetRef(view, "label", label);
            SetRef(view, "button", button);
            return view;
        }

        /// <summary>Seçili eşyanın detayı: ikon + ad + seviye, statlar (kuşanılıyla farkı), mesaj, Kuşan/Sat.</summary>
        private static ItemDetailView BuildItemDetail(Transform parent)
        {
            RectTransform root = CreateUIObject("ItemDetail", parent);
            SetHeight(root, 420);
            AddImage(root, BackgroundColor).raycastTarget = false;
            AddVerticalLayout(root, spacing: 8, padding: 12);

            // Üst satır: ikon kutusu + ad/seviye
            RectTransform top = CreateUIObject("Top", root);
            SetHeight(top, 100);
            AddHorizontalLayout(top, spacing: 14);

            RectTransform iconBox = CreateUIObject("IconBox", top);
            SetHeight(iconBox, 100);
            Image frame = AddImage(iconBox, Color.gray);
            frame.raycastTarget = false;
            RectTransform innerRt = CreateUIObject("Inner", iconBox);
            Stretch(innerRt);
            innerRt.offsetMin = new Vector2(5, 5);
            innerRt.offsetMax = new Vector2(-5, -5);
            AddImage(innerRt, PanelColor).raycastTarget = false;
            RectTransform detailGlowRt = CreateUIObject("EnhanceGlow", iconBox);
            Stretch(detailGlowRt);
            detailGlowRt.offsetMin = new Vector2(-20, -20);
            detailGlowRt.offsetMax = new Vector2(20, 20);
            Image detailGlow = AddImage(detailGlowRt, Color.white);
            detailGlow.raycastTarget = false;

            RectTransform iconRt = CreateUIObject("Icon", iconBox);
            Stretch(iconRt);
            iconRt.offsetMin = new Vector2(12, 12);
            iconRt.offsetMax = new Vector2(-12, -12);
            Image icon = AddImage(iconRt, Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            RectTransform badgeRt = CreateUIObject("EnhanceBadge", iconBox);
            badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(1f, 0f);
            badgeRt.pivot = new Vector2(1f, 0f);
            badgeRt.sizeDelta = new Vector2(70, 34);
            badgeRt.anchoredPosition = new Vector2(-2, 2);
            var badge = badgeRt.gameObject.AddComponent<TextMeshProUGUI>();
            badge.fontSize = 28;
            badge.fontStyle = FontStyles.Bold;
            badge.alignment = TextAlignmentOptions.BottomRight;
            badge.raycastTarget = false;

            SetFixedWidth(frame, 100);

            RectTransform texts = CreateUIObject("Texts", top);
            SetHeight(texts, 100);
            AddVerticalLayout(texts, spacing: 4, padding: 0);
            texts.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            FillRemainingWidth(texts.GetComponent<LayoutElement>());
            TMP_Text nameText = CreateText("NameText", texts, "", 28, 54, TextAlignmentOptions.MidlineLeft);
            nameText.fontStyle = FontStyles.Bold;
            TMP_Text levelText = CreateText("LevelText", texts, "", 20, 36, TextAlignmentOptions.MidlineLeft);

            TMP_Text compare = CreateText("CompareText", root, "", 20, 56, TextAlignmentOptions.MidlineLeft);
            TMP_Text stats = CreateText("StatsText", root, "", 20, 120, TextAlignmentOptions.TopLeft);
            TMP_Text message = CreateText("MessageText", root, "", 20, 30, TextAlignmentOptions.MidlineLeft);
            message.color = new Color(1f, 0.88f, 0.23f);

            // Butonlar: Sat (küçük, solda, kırmızımsı) | Güçlendir | Kuşan (büyük, sağda, yeşil).
            // Kuşan ile Sat birbirinden uzak ve farklı boyda: yanlışlıkla iyi eşyayı satmak zorlaşır. Her butonda ikon var.
            RectTransform buttons = CreateUIObject("Buttons", root);
            SetHeight(buttons, 60);
            AddHorizontalLayout(buttons, spacing: 10);
            Button sell = CreateIconButton("SellButton", buttons, "Sat", new Color(0.55f, 0.22f, 0.18f), 60, 17, out TMP_Text sellLabel, out Image sellIcon);
            SetFixedWidth(sell, 120);
            Button enhance = CreateIconButton("EnhanceButton", buttons, "Güçlendir", new Color(0.55f, 0.35f, 0.75f), 60, 17, out TMP_Text enhanceLabel, out Image enhanceIcon);
            FillRemainingWidth(enhance);
            Button equip = CreateIconButton("EquipButton", buttons, UIStrings.EquipButton, CashOutColor, 60, 24, out TMP_Text equipLabel, out Image equipIcon);
            FillRemainingWidth(equip);
            equip.GetComponent<LayoutElement>().flexibleWidth = 1.4f;   // Kuşan en geniş buton

            var view = root.gameObject.AddComponent<ItemDetailView>();
            SetRef(view, "frame", frame);
            SetRef(view, "icon", icon);
            SetRef(view, "nameText", nameText);
            SetRef(view, "levelText", levelText);
            SetRef(view, "compareText", compare);
            SetRef(view, "statsText", stats);
            SetRef(view, "messageText", message);
            SetRef(view, "equipButton", equip);
            SetRef(view, "equipLabel", equipLabel);
            SetRef(view, "enhanceButton", enhance);
            SetRef(view, "enhanceLabel", enhanceLabel);
            SetRef(view, "sellButton", sell);
            SetRef(view, "sellLabel", sellLabel);
            SetRef(view, "iconGlow", detailGlow);
            SetRef(view, "enhanceBadge", badge);
            SetRef(view, "equipIcon", equipIcon);
            SetRef(view, "enhanceIcon", enhanceIcon);
            SetRef(view, "sellIcon", sellIcon);
            return view;
        }

        private static UpgradeRowView BuildUpgradeRow(Transform parent, UpgradeType type)
        {
            const float rowHeight = 90;
            RectTransform row = CreateUIObject($"UpgradeRow_{type}", parent);
            SetHeight(row, rowHeight);
            AddHorizontalLayout(row, spacing: 20);

            Image icon = BuildIconBox(row, rowHeight - 10);

            TMP_Text info = CreateText("InfoText", row, UIStrings.UpgradeName(type), 28, rowHeight, TextAlignmentOptions.MidlineLeft);
            FillRemainingWidth(info);

            Button buy = CreateButton("BuyButton", row, "Al", BuyColor, rowHeight, 34, out TMP_Text buyText);
            SetFixedWidth(buy, 260);

            var view = row.gameObject.AddComponent<UpgradeRowView>();
            SetEnum(view, "upgradeType", (int)type);
            SetRef(view, "infoText", info);
            SetRef(view, "buyButton", buy);
            SetRef(view, "icon", icon);
            SetRef(view, "buyButtonText", buyText);
            return view;
        }

        private static void AddSceneToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));  // İlk açılan sahne olsun
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------------- UI oluşturma yardımcıları ----------------

        private static RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Ebeveyninin tamamını kaplasın.</summary>
        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Image AddImage(RectTransform rt, Color color)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        /// <summary>Ekranı kaplayan panel; içine yan yana sütunlar konur. Sütunlar tam yüksekliği kullanır.</summary>
        private static RectTransform CreatePanelWithColumns(string name, Transform parent)
        {
            RectTransform panel = CreateUIObject(name, parent);
            Stretch(panel);
            AddImage(panel, PanelColor).raycastTarget = false;

            var layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 40;
            layout.padding = new RectOffset(40, 40, 30, 30);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            // Bu panel bir düzen grubunun İÇİNDE değil, o yüzden burada genişletmek güvenli.
            layout.childForceExpandHeight = true;
            return panel;
        }

        /// <summary>Dikey sütun. flexibleWidth sütunların genişlik oranını belirler.</summary>
        private static RectTransform CreateColumn(string name, Transform parent, float flexibleWidth)
        {
            RectTransform column = CreateUIObject(name, parent);
            var element = column.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = flexibleWidth;
            // Metinlerin tek satırlık doğal genişliği sütunu büyütmesin: genişlik sadece orana göre
            element.minWidth = 0;
            element.preferredWidth = 0;
            AddVerticalLayout(column, spacing: 10, padding: 0);
            return column;
        }

        private static void AddVerticalLayout(RectTransform rt, float spacing, int padding)
        {
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        /// <summary>
        /// Yan yana dizilim (üst bar, yükseltme satırı).
        /// DİKKAT: childForceExpandHeight = true olursa grup kendini "esnek yükseklik" ilan eder ve
        /// dikey bir düzenin içindeyse ekranın yarısını kaplar (ilk sürümdeki üst bar hatası buydu).
        /// </summary>
        private static void AddHorizontalLayout(RectTransform rt, float spacing)
        {
            var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        /// <summary>Sabit yükseklik. flexibleHeight = 0: düzen ne derse desin büyümesin.</summary>
        private static LayoutElement SetHeight(RectTransform rt, float height)
        {
            var element = rt.gameObject.GetComponent<LayoutElement>();
            if (element == null) element = rt.gameObject.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = 0;
            return element;
        }

        /// <summary>Yan yana dizilimde sabit genişlik (ne büyür ne küçülür).</summary>
        private static void SetFixedWidth(Component c, float width)
        {
            LayoutElement element = c.GetComponent<LayoutElement>();
            element.minWidth = width;
            element.preferredWidth = width;
            element.flexibleWidth = 0;
        }

        /// <summary>Yan yana dizilimde kalan tüm genişliği kaplar; metin uzunluğu genişliği etkilemez.</summary>
        private static void FillRemainingWidth(Component c)
        {
            LayoutElement element = c.GetComponent<LayoutElement>();
            element.minWidth = 0;
            element.preferredWidth = 0;
            element.flexibleWidth = 1;
        }

        private static void CreateFlexibleSpacer(Transform parent)
        {
            CreateUIObject("FlexibleSpacer", parent).gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
        }

        private static TMP_Text CreateText(string name, Transform parent, string text, float fontSize, float height,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            RectTransform rt = CreateUIObject(name, parent);
            SetHeight(rt, height);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            // Metin kutuya sığmazsa yazı boyutu kendiliğinden küçülür (en fazla %40). Taşma olmaz.
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = fontSize;
            tmp.fontSizeMin = fontSize * 0.6f;
            return tmp;
        }

        /// <summary>Solda ikon, sağda yazı olan buton (eşya işlemleri: ne yaptığı resimden de belli olsun).</summary>
        private static Button CreateIconButton(string name, Transform parent, string label, Color color,
            float height, float fontSize, out TMP_Text labelText, out Image icon)
        {
            Button button = CreateButton(name, parent, label, color, height, fontSize, out labelText);
            RectTransform iconRt = CreateUIObject("Icon", button.transform);
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.sizeDelta = new Vector2(height - 16, height - 16);
            iconRt.anchoredPosition = new Vector2(8f, 0f);
            icon = AddImage(iconRt, Color.white);
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            // Yazı ikonun sağında kalsın
            RectTransform labelRt = labelText.rectTransform;
            labelRt.offsetMin = new Vector2(height - 4, 0f);
            labelText.enableAutoSizing = true;
            labelText.fontSizeMax = fontSize;
            labelText.fontSizeMin = fontSize * 0.6f;
            return button;
        }

        private static Button CreateButton(string name, Transform parent, string label, Color color,
            float height, float fontSize, out TMP_Text labelText)
        {
            RectTransform rt = CreateUIObject(name, parent);
            SetHeight(rt, height);
            Image image = AddImage(rt, color);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            // Klavye kısayolları ok tuşlarını kullanıyor; UI'ın ok tuşlarıyla butonlar arasında
            // gezinip yanlış butonu seçili bırakmasını istemiyoruz.
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            RectTransform textRt = CreateUIObject("Label", rt);
            Stretch(textRt);
            var tmp = textRt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;

            rt.gameObject.AddComponent<ClickSound>();   // Tık sesi

            labelText = tmp;
            return button;
        }

        // ---------------- Inspector alanlarını koddan doldurma ----------------
        // [SerializeField] private alanlara dışarıdan erişemeyiz; SerializedObject ile
        // Inspector'da sürükle-bırak yapmışız gibi atıyoruz.

        private static void SetRef(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"Alan bulunamadı: {target.GetType().Name}.{fieldName}");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRefArray(Object target, string fieldName, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"Alan bulunamadı: {target.GetType().Name}.{fieldName}");
                return;
            }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string fieldName, string value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"Alan bulunamadı: {target.GetType().Name}.{fieldName}");
                return;
            }
            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(Object target, string fieldName, int enumIndex)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"Alan bulunamadı: {target.GetType().Name}.{fieldName}");
                return;
            }
            prop.enumValueIndex = enumIndex;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
