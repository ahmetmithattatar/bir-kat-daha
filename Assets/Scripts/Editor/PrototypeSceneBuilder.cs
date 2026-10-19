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
    ///   Üst bar:        Para | Rekor | Kaydı sıfırla
    ///   Tur ekranı:     sol sütun istatistikler  | sağ sütun bulgu kartı, mesaj, butonlar
    ///   Yükseltme ekr.: sol sütun sonuç + başla   | sağ sütun 7 yükseltme
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

            // --- Kök düzen: ortada 1920 genişliğinde alan. Üstte bar, altta ekranlar ---
            RectTransform root = CreateUIObject("Root", canvas);
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.sizeDelta = new Vector2(RefWidth, 0);
            root.anchoredPosition = Vector2.zero;
            AddVerticalLayout(root, spacing: 20, padding: 30);

            TopBarView topBar = BuildTopBar(root);

            RectTransform screens = CreateUIObject("Screens", root);
            screens.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

            RunPanelView runPanel = BuildRunPanel(screens);
            UpgradePanelView upgradePanel = BuildUpgradePanel(screens);

            // Üst katmanlar (sıra önemli: sonra eklenen üstte çizilir)
            RevealPopupView popup = BuildRevealPopup(canvas);
            FxLayerView fx = BuildFxLayer(canvas, root);

            // --- GameController + hissiyat yönetmeni ---
            var controllerGo = new GameObject("GameController");
            var director = controllerGo.AddComponent<RunFeedbackDirector>();
            SetRef(director, "fx", fx);
            SetRef(director, "popup", popup);
            SetRef(director, "runPanel", runPanel);
            SetRef(director, "digView", runPanel.GetComponentInChildren<DigView>(true));

            var controller = controllerGo.AddComponent<GameController>();
            SetRef(controller, "balanceConfig", config);
            SetRef(controller, "topBar", topBar);
            SetRef(controller, "runPanel", runPanel);
            SetRef(controller, "upgradePanel", upgradePanel);
            SetRef(controller, "feedback", director);
        }

        /// <summary>Üst bar: para | rekor | kaydı sıfırla (sağ üst köşe, kazı butonlarından uzakta).</summary>
        private static TopBarView BuildTopBar(Transform parent)
        {
            const float barHeight = 80;
            RectTransform bar = CreateUIObject("TopBar", parent);
            SetHeight(bar, barHeight);
            AddHorizontalLayout(bar, spacing: 30);

            TMP_Text money = CreateText("MoneyText", bar, "Para: 0", 52, barHeight, TextAlignmentOptions.MidlineLeft);
            money.fontStyle = FontStyles.Bold;
            FillRemainingWidth(money);

            TMP_Text record = CreateText("RecordText", bar, "Rekor", 34, barHeight, TextAlignmentOptions.MidlineRight);
            SetFixedWidth(record, 320);

            Button reset = CreateButton("ResetSaveButton", bar, UIStrings.ResetSaveButton, MutedColor, barHeight, 26, out TMP_Text resetText);
            SetFixedWidth(reset, 220);

            var view = bar.gameObject.AddComponent<TopBarView>();
            SetRef(view, "moneyText", money);
            SetRef(view, "recordText", record);
            SetRef(view, "resetButton", reset);
            SetRef(view, "resetButtonText", resetText);
            return view;
        }

        private static RunPanelView BuildRunPanel(Transform parent)
        {
            RectTransform panel = CreatePanelWithColumns("RunPanel", parent);

            // ---- Sol sütun: istatistikler ----
            RectTransform left = CreateColumn("LeftColumn", panel, flexibleWidth: 0.9f);

            // Yükseklik bütçesi ~860: sıkı ama sığar. Metinler sığmazsa auto-size küçültür.
            TMP_Text depth = CreateText("DepthText", left, "Derinlik", 48, 70);
            depth.fontStyle = FontStyles.Bold;
            TMP_Text runValue = CreateText("RunValueText", left, "Turun değeri", 40, 84);
            runValue.fontStyle = FontStyles.Bold;
            TMP_Text breakdown = CreateText("RunValueBreakdownText", left, "", 24, 60);
            TMP_Text buffs = CreateText("BuffsText", left, "Baret: 0", 22, 60);
            TMP_Text combatStats = CreateText("CombatStatsText", left, "", 22, 36);
            TMP_Text power = CreateText("PowerText", left, "", 22, 56);

            TMP_Text risk = CreateText("RiskText", left, "Risk", 34, 48);
            risk.fontStyle = FontStyles.Bold;

            // Risk çubuğu: gri arka plan + soldan dolan renkli şerit
            RectTransform bar = CreateUIObject("RiskBar", left);
            AddImage(bar, BarBackColor).raycastTarget = false;
            SetHeight(bar, 26);
            RectTransform fill = CreateUIObject("Fill", bar);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0.1f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            Image fillImage = AddImage(fill, Color.green);
            fillImage.raycastTarget = false;

            TMP_Text gamble = CreateText("GambleText", left, "", 22, 56);
            TMP_Text floorsLeft = CreateText("FloorsLeftText", left, "", 20, 36);
            TMP_Text nextReward = CreateText("NextRewardText", left, "Sonraki kat", 22, 70);
            TMP_Text insurance = CreateText("InsuranceText", left, "Sigorta", 20, 40);
            TMP_Text milestone = CreateText("MilestoneText", left, "Sonraki keşif", 22, 84);
            CreateFlexibleSpacer(left);

            // ---- Orta sütun: birinci şahıs kazı sahnesi + mesaj ----
            RectTransform center = CreateColumn("CenterColumn", panel, flexibleWidth: 1.5f);
            DigView digView = BuildDigView(center);
            TMP_Text message = CreateText("MessageText", center, "", 30, 110);

            // ---- Sağ sütun: bulgu koleksiyonu, oto-kazı, butonlar ----
            RectTransform right = CreateColumn("RightColumn", panel, flexibleWidth: 1f);

            FindCollectionView collection = BuildFindCollection(right);
            AutoDigView auto = BuildAutoDig(right);
            CreateFlexibleSpacer(right);
            Button advance = CreateButton("AdvanceButton", right, UIStrings.AdvanceButton, AdvanceColor, 110, 54, out TMP_Text advanceLabel);
            Button cashOut = CreateButton("CashOutButton", right, UIStrings.CashOutButton, CashOutColor, 76, 36, out TMP_Text cashOutLabel);
            TMP_Text hint = CreateText("KeyHintText", right, UIStrings.KeyHintRun, 18, 46);
            hint.color = HintColor;

            var view = panel.gameObject.AddComponent<RunPanelView>();
            SetRef(view, "depthText", depth);
            SetRef(view, "powerText", power);
            SetRef(view, "gambleText", gamble);
            SetRef(view, "floorsLeftText", floorsLeft);
            SetRef(view, "autoDig", auto);
            SetRef(view, "runValueText", runValue);
            SetRef(view, "runValueBreakdownText", breakdown);
            SetRef(view, "buffsText", buffs);
            SetRef(view, "riskText", risk);
            SetRef(view, "nextRewardText", nextReward);
            SetRef(view, "insuranceText", insurance);
            SetRef(view, "milestoneText", milestone);
            SetRef(view, "messageText", message);
            SetRef(view, "riskBarFill", fill);
            SetRef(view, "riskBarFillImage", fillImage);
            SetRef(view, "collection", collection);
            SetRef(view, "digView", digView);
            SetRef(view, "advanceButton", advance);
            SetRef(view, "cashOutButton", cashOut);
            SetRef(view, "combatStatsText", combatStats);
            SetRef(view, "keyHintText", hint);
            SetRef(view, "advanceLabel", advanceLabel);
            SetRef(view, "cashOutLabel", cashOutLabel);
            return view;
        }

        /// <summary>Oto-kazı paneli: aç/kapat butonu + 2x2 durma kuralı butonu.</summary>
        private static AutoDigView BuildAutoDig(Transform parent)
        {
            RectTransform root = CreateUIObject("AutoDig", parent);
            SetHeight(root, 56 + 8 + 2 * 44 + 8);
            AddVerticalLayout(root, spacing: 8, padding: 0);

            Button toggle = CreateButton("AutoToggle", root, UIStrings.AutoOff, MutedColor, 56, 28, out TMP_Text toggleLabel);

            RectTransform grid = CreateUIObject("Rules", root);
            SetHeight(grid, 2 * 44 + 8);
            var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(236, 44);
            gridLayout.spacing = new Vector2(8, 8);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;
            gridLayout.childAlignment = TextAnchor.UpperCenter;

            Button shields = CreateButton("StopShields", grid, "", BarBackColor, 44, 18, out TMP_Text shieldsLabel);
            Button risk = CreateButton("StopRisk", grid, "", BarBackColor, 44, 18, out TMP_Text riskLabel);
            Button tier = CreateButton("StopTier", grid, "", BarBackColor, 44, 18, out TMP_Text tierLabel);
            Button fight = CreateButton("AutoFight", grid, "", BarBackColor, 44, 18, out TMP_Text fightLabel);
            foreach (TMP_Text l in new[] { shieldsLabel, riskLabel, tierLabel, fightLabel }) l.fontStyle = FontStyles.Normal;

            var view = root.gameObject.AddComponent<AutoDigView>();
            SetRef(view, "toggleButton", toggle);
            SetRef(view, "toggleImage", toggle.GetComponent<Image>());
            SetRef(view, "toggleLabel", toggleLabel);
            SetRef(view, "shieldsButton", shields);
            SetRef(view, "shieldsLabel", shieldsLabel);
            SetRef(view, "riskButton", risk);
            SetRef(view, "riskLabel", riskLabel);
            SetRef(view, "tierButton", tier);
            SetRef(view, "tierLabel", tierLabel);
            SetRef(view, "fightButton", fight);
            SetRef(view, "fightLabel", fightLabel);
            return view;
        }

        /// <summary>
        /// Birinci şahıs kazı sahnesi. Katmanlar (alttan üste):
        /// tünel halkaları, tavan çatlağı, delik, duvar (+çatlak +kat yazısı), bulgu (+hüzme +parıltı), baret,
        /// karartma, göçük taşları, parçacıklar, kazma | sahne dışında: kenar karartma, tıklama alanı.
        /// Sprite'lar oyun açılınca kodla üretilir (DigView.Awake / SetDepth), burada sadece yerleri kurulur.
        /// </summary>
        private static DigView BuildDigView(Transform parent)
        {
            RectTransform view = CreateUIObject("DigView", parent);
            SetHeight(view, 700);
            AddImage(view, Color.black).raycastTarget = false;
            view.gameObject.AddComponent<RectMask2D>();  // Taşan parçacık ve hüzmeleri keser

            RectTransform scene = CreateUIObject("SceneRoot", view);
            Stretch(scene);

            // Tünel: iç içe 4 çerçeve, içe doğru koyulaşır (derinlik hissi)
            RectTransform tunnel = CreateUIObject("TunnelRoot", scene);
            Stretch(tunnel);
            float[] insets = { 0f, 50f, 95f, 135f };
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
            ceiling.sizeDelta = new Vector2(0f, 110f);
            Image ceilingImage = AddImage(ceiling, new Color(1f, 1f, 1f, 0f));
            ceilingImage.raycastTarget = false;

            Image hole = AddCentered("Hole", scene, new Vector2(380, 380));

            // Duvar
            RectTransform wall = CreateUIObject("WallRoot", scene);
            wall.anchorMin = wall.anchorMax = new Vector2(0.5f, 0.5f);
            wall.sizeDelta = new Vector2(470, 440);
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
            depthLabel.fontSize = 34;
            depthLabel.fontStyle = FontStyles.Bold;
            depthLabel.alignment = TextAlignmentOptions.Center;
            depthLabel.color = new Color(1f, 1f, 1f, 0.35f);  // Kayaya kazınmış gibi soluk
            depthLabel.raycastTarget = false;

            // Canavar: kap (ölçeklenmez) > gövde (aura + görsel, büyür/sarsılır) + üstte isim ve can barı
            RectTransform monster = CreateUIObject("MonsterRoot", scene);
            monster.anchorMin = monster.anchorMax = new Vector2(0.5f, 0.5f);
            monster.sizeDelta = new Vector2(320, 320);
            monster.anchoredPosition = new Vector2(0f, -30f);
            RectTransform monsterBody = CreateUIObject("MonsterBody", monster);
            Stretch(monsterBody);
            Image aura = AddCentered("Aura", monsterBody, new Vector2(520, 520));
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

            // Bulgu (delikten çıkan)
            RectTransform item = CreateUIObject("ItemRoot", scene);
            item.anchorMin = item.anchorMax = new Vector2(0.5f, 0.5f);
            item.sizeDelta = new Vector2(200, 200);
            Image rays = AddCentered("Rays", item, new Vector2(440, 440));
            Image glow = AddCentered("Glow", item, new Vector2(260, 260));
            Image itemImage = AddCentered("Item", item, new Vector2(170, 170));
            itemImage.preserveAspect = true;

            Image helmet = AddCentered("Helmet", scene, new Vector2(170, 170));
            helmet.preserveAspect = true;

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

            // Kazma: sağ alt köşe, dönme noktası sapın ucu
            RectTransform pickaxe = CreateUIObject("PickaxeRoot", scene);
            pickaxe.anchorMin = pickaxe.anchorMax = new Vector2(1f, 0f);
            pickaxe.pivot = new Vector2(0.5f, 0.08f);
            pickaxe.sizeDelta = new Vector2(300, 300);
            pickaxe.anchoredPosition = new Vector2(-170f, -40f);
            RectTransform pickaxeImgRt = CreateUIObject("PickaxeImage", pickaxe);
            Stretch(pickaxeImgRt);
            Image pickaxeImage = AddImage(pickaxeImgRt, Color.white);
            pickaxeImage.preserveAspect = true;
            pickaxeImage.raycastTarget = false;

            // Silah: sol alt köşe (diğer el), kazmanın aynası
            RectTransform weapon = CreateUIObject("WeaponRoot", scene);
            weapon.anchorMin = weapon.anchorMax = new Vector2(0f, 0f);
            // Dönme noktası kabzada (kılıç çiziminde sol alt), ekranın biraz yukarısında: kalkık durur, vuruşta canavara iner
            weapon.pivot = new Vector2(0.15f, 0.12f);
            weapon.sizeDelta = new Vector2(300, 300);
            weapon.anchoredPosition = new Vector2(70f, 40f);
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

            // Oyuncu can barı: sahnenin üst kenarı (kazma ve silahın üstüne binmesin)
            Image playerHpFill = BuildBar("PlayerHpBar", view, new Vector2(420, 30), Vector2.zero,
                                          new Color(0.25f, 0.8f, 0.3f), out TMP_Text playerHpText);
            RectTransform playerBar = (RectTransform)playerHpFill.transform.parent;
            playerBar.anchorMin = playerBar.anchorMax = new Vector2(0.5f, 1f);
            playerBar.pivot = new Vector2(0.5f, 1f);
            playerBar.anchoredPosition = new Vector2(0f, -14f);

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
            SetRef(dig, "helmetImage", helmet);
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
            const float gridHeight = 3 * 108 + 2 * 8;

            RectTransform root = CreateUIObject("FindCollection", parent);
            SetHeight(root, 40 + 8 + gridHeight);
            AddVerticalLayout(root, spacing: 8, padding: 0);

            TMP_Text title = CreateText("CollectionTitle", root, "", 26, 40);
            title.color = HintColor;

            RectTransform grid = CreateUIObject("Grid", root);
            SetHeight(grid, gridHeight);
            var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(150, 108);
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
            frame.raycastTarget = false;

            RectTransform inner = CreateUIObject("Inner", card);
            Stretch(inner);
            inner.offsetMin = new Vector2(5, 5);
            inner.offsetMax = new Vector2(-5, -5);
            AddImage(inner, BackgroundColor).raycastTarget = false;
            AddVerticalLayout(inner, spacing: 0, padding: 3);
            inner.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;

            RectTransform iconRt = CreateUIObject("Icon", inner);
            SetHeight(iconRt, 44);
            Image icon = AddImage(iconRt, Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            TMP_Text type = CreateText("TypeText", inner, "", 22, 24);
            type.fontStyle = FontStyles.Bold;
            TMP_Text value = CreateText("ValueText", inner, "", 20, 24);

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
            TMP_Text result = CreateText("ResultText", left, "", 26, 160);
            ItemDetailView detail = BuildItemDetail(left);
            CreateFlexibleSpacer(left);
            Button startStep = CreateButton("StartStepButton", left, UIStrings.StartFrom(0), BarBackColor, 52, 24, out TMP_Text startStepLabel);
            Button startRun = CreateButton("StartRunButton", left, UIStrings.StartRunButton, AdvanceColor, 120, 56, out _);
            TMP_Text hint = CreateText("KeyHintText", left, UIStrings.KeyHintUpgrade, 22, 36);
            hint.color = HintColor;

            // ---- Sağ sütun: sekmeler + içerik ----
            RectTransform right = CreateColumn("RightColumn", panel, flexibleWidth: 1.3f);

            RectTransform tabs = CreateUIObject("Tabs", right);
            SetHeight(tabs, 60);
            AddHorizontalLayout(tabs, spacing: 10);
            Button upgradesTab = CreateButton("UpgradesTab", tabs, UIStrings.TabUpgrades, BuyColor, 60, 28, out _);
            FillRemainingWidth(upgradesTab);
            Button equipmentTab = CreateButton("EquipmentTab", tabs, UIStrings.TabEquipment, MutedColor, 60, 28, out _);
            FillRemainingWidth(equipmentTab);
            Button prestigeTab = CreateButton("PrestigeTab", tabs, UIStrings.TabPrestige, MutedColor, 60, 28, out _);
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

            // Ekipman ve Yeni Çağ sekmeleri
            EquipmentView equipment = BuildEquipmentView(content);
            PrestigePanelView prestige = BuildPrestigePanel(content);

            var view = panel.gameObject.AddComponent<UpgradePanelView>();
            SetRef(view, "resultText", result);
            SetRefArray(view, "rows", rows.ToArray());
            SetRef(view, "startRunButton", startRun);
            SetRef(view, "startStepButton", startStep);
            SetRef(view, "startStepLabel", startStepLabel);
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

        /// <summary>Ekipman sekmesi: güç + stat özeti, 9 karakter yuvası, envanter ızgarası, "En iyileri kuşan" ve "Çantayı sat".</summary>
        private static EquipmentView BuildEquipmentView(Transform parent)
        {
            const int inventorySlots = 32;   // CombatBalance.inventoryCap ile aynı tut
            const float cell = 96;
            var slotEnum = (GearSlot[])System.Enum.GetValues(typeof(GearSlot));

            RectTransform root = CreateUIObject("EquipmentRoot", parent);
            Stretch(root);
            AddVerticalLayout(root, spacing: 10, padding: 0);

            TMP_Text stats = CreateText("StatsText", root, "", 20, 84);

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

            TMP_Text invTitle = CreateText("InventoryTitle", root, "", 24, 36);
            invTitle.color = HintColor;

            RectTransform inv = CreateUIObject("Inventory", root);
            SetHeight(inv, 4 * cell + 3 * 6);
            var invGrid = inv.gameObject.AddComponent<GridLayoutGroup>();
            invGrid.cellSize = new Vector2(cell, cell);
            invGrid.spacing = new Vector2(6, 6);
            invGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            invGrid.constraintCount = 8;
            invGrid.childAlignment = TextAnchor.UpperCenter;
            var invSlots = new GearSlotView[inventorySlots];
            for (int i = 0; i < inventorySlots; i++) invSlots[i] = BuildGearSlot(inv, $"Inv_{i}", withLabel: false);

            RectTransform buttons = CreateUIObject("Buttons", root);
            SetHeight(buttons, 56);
            AddHorizontalLayout(buttons, spacing: 12);
            Button auto = CreateButton("AutoEquipButton", buttons, UIStrings.AutoEquipButton, CashOutColor, 56, 26, out _);
            FillRemainingWidth(auto);
            Button sellAll = CreateButton("SellAllButton", buttons, UIStrings.SellAllButton, AdvanceColor, 56, 22, out TMP_Text sellAllLabel);
            FillRemainingWidth(sellAll);

            var view = root.gameObject.AddComponent<EquipmentView>();
            SetRef(view, "statsText", stats);
            SetRefArray(view, "equippedSlots", equipped);
            SetRef(view, "inventoryTitle", invTitle);
            SetRefArray(view, "inventorySlots", invSlots);
            SetRef(view, "autoEquipButton", auto);
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
                TMP_Text rowInfo = CreateText("InfoText", row, "", 26, rowHeight, TextAlignmentOptions.MidlineLeft);
                FillRemainingWidth(rowInfo);
                Button buy = CreateButton("BuyButton", row, "Al", new Color(0.55f, 0.35f, 0.75f), rowHeight, 26, out TMP_Text buyText);
                SetFixedWidth(buy, 260);

                var rowView = row.gameObject.AddComponent<PrestigeRowView>();
                SetEnum(rowView, "type", (int)type);
                SetRef(rowView, "infoText", rowInfo);
                SetRef(rowView, "buyButton", buy);
                SetRef(rowView, "buyText", buyText);
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

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var view = root.gameObject.AddComponent<GearSlotView>();
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
            RectTransform iconRt = CreateUIObject("Icon", iconBox);
            Stretch(iconRt);
            iconRt.offsetMin = new Vector2(12, 12);
            iconRt.offsetMax = new Vector2(-12, -12);
            Image icon = AddImage(iconRt, Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
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

            RectTransform buttons = CreateUIObject("Buttons", root);
            SetHeight(buttons, 56);
            AddHorizontalLayout(buttons, spacing: 10);
            Button equip = CreateButton("EquipButton", buttons, UIStrings.EquipButton, CashOutColor, 56, 24, out TMP_Text equipLabel);
            FillRemainingWidth(equip);
            Button enhance = CreateButton("EnhanceButton", buttons, "Güçlendir", new Color(0.55f, 0.35f, 0.75f), 56, 18, out TMP_Text enhanceLabel);
            FillRemainingWidth(enhance);
            Button sell = CreateButton("SellButton", buttons, "Sat", AdvanceColor, 56, 22, out TMP_Text sellLabel);
            FillRemainingWidth(sell);

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
            return view;
        }

        private static UpgradeRowView BuildUpgradeRow(Transform parent, UpgradeType type)
        {
            const float rowHeight = 90;
            RectTransform row = CreateUIObject($"UpgradeRow_{type}", parent);
            SetHeight(row, rowHeight);
            AddHorizontalLayout(row, spacing: 20);

            TMP_Text info = CreateText("InfoText", row, UIStrings.UpgradeName(type), 28, rowHeight, TextAlignmentOptions.MidlineLeft);
            FillRemainingWidth(info);

            Button buy = CreateButton("BuyButton", row, "Al", BuyColor, rowHeight, 34, out TMP_Text buyText);
            SetFixedWidth(buy, 260);

            var view = row.gameObject.AddComponent<UpgradeRowView>();
            SetEnum(view, "upgradeType", (int)type);
            SetRef(view, "infoText", info);
            SetRef(view, "buyButton", buy);
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
