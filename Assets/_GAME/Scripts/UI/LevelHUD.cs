using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LevelHUD : MonoBehaviour
{
    [Header("Systems")]
    public FlowBootstrapper flow;
    public WaveManager waves;
    public BuildManager builds;
    public BuildInputController input;

    [Header("Resource HUD")]
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text waveText;

    [Header("Legacy Text References")]
    // Giữ lại tham chiếu cũ. Status không còn được cập nhật.
    public Text status;
    public Text selection;
    public Text message;
    public Text resultTitle;

    [Header("Control Buttons")]
    public Button startWave;
    public Button pause;
    public Button restart;

    [Header("Build Buttons")]
    public Button buy;
    public Button buyRock;
    public Button upgrade;
    public Button sell;

    [Header("Radial Build Menu")]
    [SerializeField] private RectTransform buildMenu;
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Button buyTower03;
    [SerializeField] private Button buyTower04;

    [Tooltip("Độ lệch menu theo đơn vị UI. Để 0,0 để bám tâm icon.")]
    [SerializeField] private Vector2 menuOffset = Vector2.zero;

    [Header("Result")]
    public GameObject resultPanel;
    public Button resultRestart;

    private GameSession boundSession;
    private bool eventsBound;

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        if (hudCanvas == null && buildMenu != null)
            hudCanvas = buildMenu.GetComponentInParent<Canvas>();

        // Không cho HUD tự tắt chính nó khi ẩn menu.
        if (buildMenu != null &&
            (transform == buildMenu.transform ||
             transform.IsChildOf(buildMenu.transform)))
        {
            Debug.LogError(
                "LevelHUD phải nằm ngoài BuildMenu. " +
                "Hãy đặt script trên đối tượng LevelHUD.",
                this
            );

            enabled = false;
            return;
        }

        SetMenuVisible(false);
    }

    private void OnEnable()
    {
        if (startWave != null)
            startWave.onClick.AddListener(StartWave);

        if (pause != null)
            pause.onClick.AddListener(TogglePause);

        if (restart != null)
            restart.onClick.AddListener(Restart);

        if (buy != null)
            buy.onClick.AddListener(Buy);

        if (buyRock != null)
            buyRock.onClick.AddListener(BuyRock);

        if (upgrade != null)
            upgrade.onClick.AddListener(Upgrade);

        if (sell != null)
            sell.onClick.AddListener(Sell);

        if (resultRestart != null)
            resultRestart.onClick.AddListener(Restart);

        TryBindEvents();
    }

    private void OnDisable()
    {
        if (startWave != null)
            startWave.onClick.RemoveListener(StartWave);

        if (pause != null)
            pause.onClick.RemoveListener(TogglePause);

        if (restart != null)
            restart.onClick.RemoveListener(Restart);

        if (buy != null)
            buy.onClick.RemoveListener(Buy);

        if (buyRock != null)
            buyRock.onClick.RemoveListener(BuyRock);

        if (upgrade != null)
            upgrade.onClick.RemoveListener(Upgrade);

        if (sell != null)
            sell.onClick.RemoveListener(Sell);

        if (resultRestart != null)
            resultRestart.onClick.RemoveListener(Restart);

        UnbindEvents();
        SetMenuVisible(false);
    }

    private void LateUpdate()
    {
        // Hỗ trợ trường hợp Session được tạo sau OnEnable của HUD,
        // hoặc Flow thay đối tượng Session khi restart.
        if (eventsBound &&
            (flow == null || flow.Session != boundSession))
        {
            UnbindEvents();
        }

        TryBindEvents();
        UpdateBuildMenuPosition();
    }

    private void TryBindEvents()
    {
        if (eventsBound)
            return;

        if (flow == null ||
            flow.Session == null ||
            waves == null ||
            builds == null ||
            input == null)
        {
            return;
        }

        boundSession = flow.Session;

        boundSession.SnapshotChanged += Refresh;
        input.SelectionChanged += SelectionChanged;
        builds.PointChanged += SelectionChanged;
        waves.Changed += RefreshWave;

        eventsBound = true;

        Refresh(boundSession.GetSnapshot());
    }

    private void UnbindEvents()
    {
        if (!eventsBound)
            return;

        if (boundSession != null)
            boundSession.SnapshotChanged -= Refresh;

        if (input != null)
            input.SelectionChanged -= SelectionChanged;

        if (builds != null)
            builds.PointChanged -= SelectionChanged;

        if (waves != null)
            waves.Changed -= RefreshWave;

        boundSession = null;
        eventsBound = false;
    }

    private void StartWave()
    {
        if (flow == null || flow.Session == null || waves == null)
            return;

        if (flow.Session.CanStartWave)
            waves.StartNextWave();
    }

    private void TogglePause()
    {
        if (flow == null || flow.Session == null)
            return;

        if (flow.Session.State == GameState.Victory ||
            flow.Session.State == GameState.Defeat)
        {
            return;
        }

        if (flow.Session.State == GameState.Paused)
            flow.Session.Resume();
        else
            flow.Session.Pause();
    }

    public void Restart()
    {
        if (flow == null)
            return;

        if (input != null)
            input.ClearSelection();

        SetMenuVisible(false);
        SetMessage("");

        flow.StartNewSession();

        // Gắn lại sự kiện nếu restart thay đối tượng Session.
        UnbindEvents();
        TryBindEvents();
    }

    private void Buy()
    {
        TryBuyTower("arrow", "Đã xây tháp cung");
    }

    private void BuyRock()
    {
        TryBuyTower("rock", "Đã xây tháp đá");
    }

    private void TryBuyTower(string towerId, string successMessage)
    {
        if (builds == null || input == null)
            return;

        if (input.SelectedPoint == null)
            return;

        if (!builds.TryBuild(
                input.SelectedPoint,
                towerId,
                out var error))
        {
            SetMessage(error);
            return;
        }

        SetMessage(successMessage);

        input.ClearSelection();
        SetMenuVisible(false);
    }

    private void Upgrade()
    {
        if (builds == null || input == null)
            return;

        if (input.SelectedPoint == null)
            return;

        if (!builds.TryUpgrade(input.SelectedPoint, out var error))
        {
            SetMessage(error);
        }
        else
        {
            SetMessage("Đã nâng cấp tháp");
        }

        RefreshCurrent();
    }

    private void Sell()
    {
        if (builds == null || input == null)
            return;

        if (input.SelectedPoint == null)
            return;

        if (!builds.TrySell(
                input.SelectedPoint,
                out var refund,
                out var error))
        {
            SetMessage(error);
            return;
        }

        SetMessage($"Đã bán tháp: +{refund} vàng");

        input.ClearSelection();
        SetMenuVisible(false);
    }

    private void SelectionChanged(BuildPoint _)
    {
        RefreshCurrent();
    }

    private void RefreshCurrent()
    {
        if (!eventsBound || boundSession == null)
            return;

        Refresh(boundSession.GetSnapshot());
    }

    private void RefreshWave()
    {
        if (!eventsBound || boundSession == null)
            return;

        SessionSnapshot snapshot = boundSession.GetSnapshot();

        if (!string.IsNullOrEmpty(waves.LastError))
        {
            SetMessage(waves.LastError);
        }
        else if (snapshot.State == GameState.Preparing)
        {
            // Các thông báo riêng của Level01 hiện tại.
            if (waves.CompletedWaves == 0)
            {
                SetMessage(
                    $"Bảo vệ cửa hầm — chống lại " +
                    $"{snapshot.TotalWaves} đợt quái"
                );
            }
            else if (waves.CompletedWaves == 2)
            {
                SetMessage(
                    "Đợt 3: cảnh báo tuyến phải — quái nhanh xuất hiện!"
                );
            }
            else if (waves.CompletedWaves == 3)
            {
                SetMessage("Đã mở tháp đá! Sẵn sàng cho đợt 4.");
            }
            else if (waves.CompletedWaves == snapshot.TotalWaves - 1)
            {
                SetMessage(
                    $"Đợt {snapshot.TotalWaves}: đợt cuối!"
                );
            }
            else
            {
                SetMessage(
                    "Sẵn sàng — bấm bắt đầu cho đợt tiếp theo."
                );
            }
        }

        Refresh(snapshot);
    }

    private void SetMessage(string text)
    {
        if (message != null)
            message.text = text;
    }

    private static void Label(Button button, string text)
    {
        if (button == null)
            return;

        TMP_Text tmpLabel =
            button.GetComponentInChildren<TMP_Text>(true);

        if (tmpLabel != null)
        {
            tmpLabel.text = text;
            return;
        }

        Text legacyLabel =
            button.GetComponentInChildren<Text>(true);

        if (legacyLabel != null)
            legacyLabel.text = text;
    }

    private void Refresh(SessionSnapshot snapshot)
    {
        RefreshResources(snapshot);
        RefreshControls(snapshot);
        RefreshBuildMenu(snapshot);
        RefreshResult(snapshot);
        UpdateBuildMenuPosition();
    }

    private void RefreshResources(SessionSnapshot snapshot)
    {
        if (livesText != null)
            livesText.text = snapshot.Lives.ToString();

        if (goldText != null)
            goldText.text = snapshot.Gold.ToString();

        if (waveText != null)
        {
            waveText.text =
                $"{snapshot.CurrentWave} / {snapshot.TotalWaves}";
        }
    }

    private void RefreshControls(SessionSnapshot snapshot)
    {
        if (startWave != null)
        {
            startWave.interactable = flow.Session.CanStartWave;

            bool nextWave =
                snapshot.State == GameState.Preparing &&
                waves.SpawnFinished;

            Label(startWave, nextWave ? "Next Wave" : "Start Wave");
        }

        if (pause != null)
        {
            Label(
                pause,
                snapshot.State == GameState.Paused
                    ? "Resume"
                    : "Pause"
            );

            pause.interactable =
                snapshot.State != GameState.Victory &&
                snapshot.State != GameState.Defeat;
        }
    }

    private void RefreshBuildMenu(SessionSnapshot snapshot)
    {
        BuildPoint point = input.SelectedPoint;

        bool hasPoint = point != null;
        bool occupied = hasPoint && point.IsOccupied;
        bool emptyPoint = hasPoint && !occupied;

        bool buildAllowed =
            flow.Session.BuildAllowed &&
            hasPoint &&
            !point.TransactionInProgress;

        if (selection != null)
        {
            selection.text = !hasPoint
                ? "Chọn một điểm xây"
                : occupied
                    ? $"{BuildManager.GetDisplayName(point.CurrentTowerId)} " +
                      $"Lv.{point.CurrentLevel}"
                    : "Chọn tháp";
        }

        RefreshPurchaseButton(
            buy,
            "arrow",
            emptyPoint,
            buildAllowed,
            snapshot.Gold
        );

        RefreshPurchaseButton(
            buyRock,
            "rock",
            emptyPoint,
            buildAllowed,
            snapshot.Gold
        );

        // Hai loại tháp còn lại chưa được cấu hình.
        RefreshLockedButton(buyTower03, emptyPoint);
        RefreshLockedButton(buyTower04, emptyPoint);

        if (upgrade != null)
        {
            upgrade.gameObject.SetActive(occupied);
            upgrade.interactable = false;
        }

        if (sell != null)
        {
            sell.gameObject.SetActive(occupied);
            sell.interactable = false;
        }

        if (!occupied)
            return;

        var towerSnapshot = point.CurrentTowerHandle.ReadSnapshot();

        bool hasUpgradePrice = builds.TryGetUpgradeCost(
            towerSnapshot.TowerId,
            towerSnapshot.Level + 1,
            out var upgradePrice
        );

        if (upgrade != null)
        {
            if (!towerSnapshot.CanUpgrade)
                Label(upgrade, "MAX");
            else if (!hasUpgradePrice)
                Label(upgrade, "—");
            else
                Label(upgrade, upgradePrice.ToString());

            upgrade.interactable =
                towerSnapshot.CanUpgrade &&
                hasUpgradePrice &&
                buildAllowed &&
                snapshot.Gold >= upgradePrice;
        }

        if (sell != null)
        {
            Label(
                sell,
                $"+{BuildManager.CalculateSellRefund(point.TotalInvested)}"
            );

            sell.interactable = buildAllowed;
        }
    }

    private void RefreshPurchaseButton(
        Button button,
        string towerId,
        bool visible,
        bool buildAllowed,
        int gold)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(visible);

        bool hasPrice =
            builds.TryGetBuildCost(towerId, out var price);

        bool unlocked = builds.IsUnlocked(towerId);

        Label(
            button,
            !hasPrice
                ? "—"
                : !unlocked
                    ? "Khóa"
                    : price.ToString()
        );

        button.interactable =
            visible &&
            buildAllowed &&
            hasPrice &&
            unlocked &&
            gold >= price;
    }

    private static void RefreshLockedButton(
        Button button,
        bool visible)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(visible);
        button.interactable = false;

        Label(button, "Khóa");
    }

    private void RefreshResult(SessionSnapshot snapshot)
    {
        bool victory = snapshot.State == GameState.Victory;
        bool defeat = snapshot.State == GameState.Defeat;

        if (resultPanel != null)
            resultPanel.SetActive(victory || defeat);

        if (resultTitle != null)
        {
            resultTitle.text = victory
                ? "VICTORY"
                : defeat
                    ? "DEFEAT"
                    : "";
        }
    }

    private void SetMenuVisible(bool visible)
    {
        if (buildMenu != null &&
            buildMenu.gameObject.activeSelf != visible)
        {
            buildMenu.gameObject.SetActive(visible);
        }
    }

    private void UpdateBuildMenuPosition()
    {
        if (buildMenu == null)
            return;

        BuildPoint point =
            input != null ? input.SelectedPoint : null;

        bool canShow =
            eventsBound &&
            point != null &&
            flow != null &&
            flow.Session != null &&
            flow.Session.BuildAllowed;

        if (!canShow)
        {
            SetMenuVisible(false);
            return;
        }

        if (hudCanvas == null)
            hudCanvas = buildMenu.GetComponentInParent<Canvas>();

        if (worldCamera == null)
            worldCamera = Camera.main;

        RectTransform parentRect =
            buildMenu.parent as RectTransform;

        if (hudCanvas == null ||
            worldCamera == null ||
            parentRect == null)
        {
            SetMenuVisible(false);
            return;
        }

        Vector3 screenPosition =
            worldCamera.WorldToScreenPoint(point.MenuPosition);

        if (screenPosition.z <= 0f)
        {
            SetMenuVisible(false);
            return;
        }

        Canvas rootCanvas = hudCanvas.rootCanvas;

        Camera uiCamera =
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;

        bool foundPosition =
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                new Vector2(screenPosition.x, screenPosition.y),
                uiCamera,
                out Vector2 localPosition
            );

        if (!foundPosition)
        {
            SetMenuVisible(false);
            return;
        }

        localPosition += menuOffset;

        buildMenu.localPosition = new Vector3(
            localPosition.x,
            localPosition.y,
            0f
        );

        SetMenuVisible(true);
    }
}