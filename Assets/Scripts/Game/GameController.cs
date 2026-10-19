using System.Collections;
using RiskLoop.Core;
using RiskLoop.Game.UI;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RiskLoop.Game
{
    /// <summary>
    /// Köprü: UI'dan gelen tıklamaları GameSession'a iletir, sonucu ekrana yansıtır, kaydı yönetir.
    /// Oyun kuralı burada YOK, hepsi Core/GameSession içinde.
    ///
    /// İki kilit var:
    ///   _swinging   : kısa vuruş animasyonu (kazma ya da silah). Yeni vuruş yok, butonlar sönmez.
    ///   _inSequence : uzun sahne (sonuç, canavar saldırısı, zafer, pencere). Her şey kilitli, canavar sayacı durur.
    /// Canavarın vuruş zamanını burası sayar (Core zaman bilmez).
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [SerializeField] private BalanceConfig balanceConfig;

        [Header("UI")]
        [SerializeField] private TopBarView topBar;
        [SerializeField] private RunPanelView runPanel;
        [SerializeField] private UpgradePanelView upgradePanel;
        [SerializeField] private RunFeedbackDirector feedback;

        [Header("Test")]
        [Tooltip("0 = her seferinde farklı. Başka bir sayı = aynı zar dizisi (hata ayıklamak için).")]
        [SerializeField] private int randomSeed = 0;

        [Header("Klavye")]
        [Tooltip("Tur bittikten sonra kazı tuşunun bu kadar saniye yok sayılması. " +
                 "Tuşa seri basarken sonuç ekranını görmeden yeni tura başlamayı önler.")]
        [SerializeField] private float keyLockAfterRunEnd = 0.6f;

        [Header("Savaş")]
        [Tooltip("Canavar çıktıktan sonra ilk vuruşundan önce oyuncuya tanınan ek süre (sn).")]
        [SerializeField] private float monsterFirstAttackGrace = 0.8f;

        private GameSession _session;
        private float _keysLockedUntil;
        private bool _swinging;
        private bool _inSequence;
        private float _monsterTimer;       // Canavarın bir sonraki vuruşuna kalan süre
        private float _nextAttackTime;     // Oyuncunun bir sonraki vuruşu en erken ne zaman

        private bool InputLocked => _swinging || _inSequence;

        private void Awake()
        {
            if (balanceConfig == null)
            {
                Debug.LogError("GameController: 'Balance Config' alanı boş. Inspector'da Assets/Config/BalanceConfig'i bu alana sürükle.", this);
                enabled = false;
                return;
            }
            if (topBar == null || runPanel == null || upgradePanel == null || feedback == null)
            {
                Debug.LogError("GameController: UI referanslarından biri boş. Sahneyi RiskLoop > Build Prototype Scene ile yeniden kur.", this);
                enabled = false;
                return;
            }

            PlayerProgress progress = SaveSystem.Load() ?? CreateNewProgress();
            CreateSession(progress);

            runPanel.AdvanceClicked += OnAdvanceClicked;
            runPanel.CashOutClicked += OnCashOutClicked;
            upgradePanel.StartRunClicked += OnStartRunClicked;
            upgradePanel.BuyClicked += OnBuyClicked;
            upgradePanel.EquipClicked += OnEquipClicked;
            upgradePanel.UnequipClicked += OnUnequipClicked;
            upgradePanel.SellClicked += OnSellClicked;
            upgradePanel.AutoEquipClicked += OnAutoEquipClicked;
            topBar.ResetConfirmed += OnResetConfirmed;
        }

        private void Start()
        {
            upgradePanel.ShowResult(UIStrings.Welcome);
            ShowUpgradeScreen();
        }

        /// <summary>
        /// Her kare: seçili butonu temizle, canavar sayacını işlet, klavyeyi oku.
        ///   ↓ / S / Boşluk : kaz ya da saldır (BASILI TUTUNCA art arda). Yükseltme ekranında: kazıya başla (tek basış)
        ///   ↑ / W          : yüzeye dön (savaşta: kaç)
        /// </summary>
        private void Update()
        {
            // Tıklanan buton "seçili" kalırsa Boşluk/Enter onu tekrar tetikler (çift vuruş). Seçimi hep temizle.
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            if (_session == null) return;

            TickMonster();

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (InputLocked || Time.unscaledTime < _keysLockedUntil) return;

            if (_session.IsRunActive)
            {
                bool digHeld = keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed || keyboard.spaceKey.isPressed;
                bool surfacePressed = keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame;

                if (surfacePressed && _session.CurrentRun.Step > 0) OnCashOutClicked();
                else if (digHeld) OnAdvanceClicked();
            }
            else
            {
                // Yeni tur sadece YENİ basışla başlar: göçükte tuşu basılı tutan oyuncu sonuç ekranını atlamasın
                bool startPressed = keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame ||
                                    keyboard.spaceKey.wasPressedThisFrame;
                if (startPressed) OnStartRunClicked();
            }
#endif
        }

        /// <summary>Savaştaysak canavarın sayacı işler; dolunca canavar vurur. Uzun sahnelerde durur.</summary>
        private void TickMonster()
        {
            if (!_session.InCombat || _inSequence) return;

            _monsterTimer -= Time.unscaledDeltaTime;
            if (_monsterTimer <= 0f) StartCoroutine(MonsterAttackRoutine());
        }

        private void OnDestroy()
        {
            if (_session == null) return;  // Awake erken çıktıysa abonelik de yok
            runPanel.AdvanceClicked -= OnAdvanceClicked;
            runPanel.CashOutClicked -= OnCashOutClicked;
            upgradePanel.StartRunClicked -= OnStartRunClicked;
            upgradePanel.BuyClicked -= OnBuyClicked;
            upgradePanel.EquipClicked -= OnEquipClicked;
            upgradePanel.UnequipClicked -= OnUnequipClicked;
            upgradePanel.SellClicked -= OnSellClicked;
            upgradePanel.AutoEquipClicked -= OnAutoEquipClicked;
            topBar.ResetConfirmed -= OnResetConfirmed;
        }

        // Mobilde uygulama arka plana atılınca OnApplicationQuit çağrılmayabilir, o yüzden burada da kaydediyoruz.
        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        // ---------------- Tur butonları ----------------

        private void OnStartRunClicked()
        {
            if (InputLocked || _session.IsRunActive) return;
            _session.StartRun();
            feedback.OnRunStarted(_session);
            runPanel.ShowMessage(UIStrings.RunStarted);
            ShowRunScreen();
        }

        /// <summary>Ana eylem: savaşta saldır, değilse kaz.</summary>
        private void OnAdvanceClicked()
        {
            if (InputLocked || !_session.IsRunActive) return;

            if (_session.InCombat)
            {
                if (Time.unscaledTime < _nextAttackTime) return;
                _nextAttackTime = Time.unscaledTime + (float)_session.Balance.combat.playerAttackCooldown;
                StartCoroutine(AttackRoutine());
            }
            else
            {
                StartCoroutine(DigRoutine());
            }
        }

        private void OnCashOutClicked()
        {
            if (InputLocked || !_session.IsRunActive || _session.CurrentRun.Step == 0) return;
            StartCoroutine(CashOutRoutine());
        }

        // ---------------- Kazı ----------------

        /// <summary>
        /// Bir kazma vuruşu. Duvar henüz yıkılmadıysa burada biter.
        /// Son vuruşta: zar (GameSession) -> sonucu sahneleme. Vuruşlar ve bekleme sadece sunum; kural aynı.
        /// </summary>
        private IEnumerator DigRoutine()
        {
            _swinging = true;
            yield return feedback.PlaySwing(_session.NextBustChance);
            _swinging = false;
            if (!feedback.WallBroken) yield break;

            SetSequence(true);
            double valueBefore = _session.CurrentRun.TotalValue;
            AdvanceResult result = _session.Advance();

            if (result.Busted)
            {
                Save();
                yield return feedback.PlayBust();
                upgradePanel.ShowResult(UIStrings.Busted(result, _session.CurrentRun));
                SetSequence(false);
                ShowUpgradeScreen();
                yield break;
            }

            // Kese ya da ilk keşif ödülü doğrudan cebe girdiyse hemen kaydet (oyun kapanırsa kaybolmasın)
            if (result.MilestoneBonus > 0 || (result.HasFind && result.Find.Type == FindType.SafeCache)) Save();

            yield return feedback.PlayStep(result, _session, valueBefore);
            if (result.Encounter != null) _monsterTimer = (float)_session.MonsterAttackInterval + monsterFirstAttackGrace;

            topBar.Refresh(_session.Progress);
            runPanel.Refresh(_session);
            SetSequence(false);
        }

        // ---------------- Savaş ----------------

        /// <summary>Oyuncu vurur. Canavar ölürse zafer sahnesi.</summary>
        private IEnumerator AttackRoutine()
        {
            _swinging = true;
            MonsterInstance monster = _session.CurrentRun.Monster;
            AttackResult hit = _session.Attack();
            yield return feedback.PlayAttack(hit, monster);
            _swinging = false;

            if (!hit.Killed || !_session.IsRunActive) yield break;

            SetSequence(true);
            yield return feedback.PlayVictory(hit.Victory, _session);
            if (hit.Victory.HasFind && hit.Victory.Find.Type == FindType.SafeCache) Save();
            topBar.Refresh(_session.Progress);
            runPanel.Refresh(_session);
            SetSequence(false);
        }

        /// <summary>Canavar vurur. Can biterse (baret yoksa) tur biter.</summary>
        private IEnumerator MonsterAttackRoutine()
        {
            SetSequence(true);
            MonsterInstance monster = _session.CurrentRun.Monster;
            RunState run = _session.CurrentRun;
            MonsterHitResult hit = _session.MonsterAttack();
            yield return feedback.PlayMonsterHit(hit, monster, run);

            if (hit.PlayerDied)
            {
                Save();
                yield return feedback.PlayDefeat();
                upgradePanel.ShowResult(UIStrings.Defeated(hit, run, FantasyTexts.MonsterName(monster.Definition.id)));
                SetSequence(false);
                ShowUpgradeScreen();
                yield break;
            }

            _monsterTimer = (float)_session.MonsterAttackInterval;
            runPanel.Refresh(_session);
            SetSequence(false);
        }

        // ---------------- Tur sonu ----------------

        private IEnumerator CashOutRoutine()
        {
            SetSequence(true);
            bool fled = _session.InCombat;
            double payout = _session.CashOut();
            Save();
            topBar.Refresh(_session.Progress);
            yield return feedback.PlayCashOut(payout);
            upgradePanel.ShowResult(UIStrings.CashedOut(_session.CurrentRun, fled));
            SetSequence(false);
            ShowUpgradeScreen();
        }

        private void SetSequence(bool on)
        {
            _inSequence = on;
            runPanel.SetButtonsInteractable(!on);
        }

        // ---------------- Yükseltme ve ekipman ----------------

        private void OnBuyClicked(UpgradeType type)
        {
            if (_session.TryBuyUpgrade(type))
            {
                Save();
                RefreshAll();
            }
        }

        private void OnEquipClicked(long uid)
        {
            if (_session.Equip(uid)) AfterGearChange();
        }

        private void OnUnequipClicked(long uid)
        {
            if (_session.Unequip(uid)) AfterGearChange();
        }

        private void OnSellClicked(long uid)
        {
            double value = _session.Sell(uid);
            if (value > 0)
            {
                upgradePanel.ShowGearMessage(UIStrings.Sold(value));
                AfterGearChange();
            }
        }

        private void OnAutoEquipClicked()
        {
            int changed = _session.AutoEquip();
            upgradePanel.ShowGearMessage(UIStrings.AutoEquipped(changed));
            AfterGearChange();
        }

        private void AfterGearChange()
        {
            Save();
            RefreshAll();
        }

        private void OnResetConfirmed()
        {
            SaveSystem.Delete();
            CreateSession(CreateNewProgress());
            upgradePanel.ShowResult(UIStrings.SaveReset);
            ShowUpgradeScreen();
        }

        // ---------------- Yardımcılar ----------------

        private PlayerProgress CreateNewProgress()
        {
            return new PlayerProgress { money = balanceConfig.data.startingMoney };
        }

        private void CreateSession(PlayerProgress progress)
        {
            IRandomSource random = randomSeed != 0 ? new SystemRandomSource(randomSeed) : new SystemRandomSource();
            // Not: balanceConfig.data'yı doğrudan veriyoruz. Play modunda Inspector'dan sayı
            // değiştirirsen anında etki eder (denge denemeleri için pratik).
            _session = new GameSession(balanceConfig.data, progress, random);
        }

        private void ShowRunScreen()
        {
            upgradePanel.gameObject.SetActive(false);
            runPanel.gameObject.SetActive(true);
            topBar.SetResetVisible(false);  // Tur sırasında sıfırlama yok
            RefreshAll();
        }

        private void ShowUpgradeScreen()
        {
            _keysLockedUntil = Time.unscaledTime + keyLockAfterRunEnd;
            runPanel.gameObject.SetActive(false);
            upgradePanel.gameObject.SetActive(true);
            topBar.SetResetVisible(true);
            RefreshAll();
        }

        private void RefreshAll()
        {
            topBar.Refresh(_session.Progress);
            runPanel.Refresh(_session);
            upgradePanel.Refresh(_session);
        }

        private void Save()
        {
            if (_session != null) SaveSystem.Save(_session.Progress);
        }
    }
}
