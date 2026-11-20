using UnityEngine;

[DisallowMultipleComponent]
public class FlowSandboxDebugPanel : MonoBehaviour
{
    private static readonly Rect PanelRect = new Rect(16, 16, 410, 720);

    [Header("References")]
    [SerializeField] private FlowBootstrapper bootstrapper;
    [SerializeField] private FakeEnemySpawnService fakeEnemyService;
    [SerializeField] private FakeTowerFactory fakeTowerFactory;
    [SerializeField] private BuildInputController buildInput;
    [SerializeField] private BuildManager buildManager;

    private string lastStatusMessage = "Sẵn sàng test FlowSandbox (B02 - B08).";
    private SessionSnapshot latestSnapshot;

    private void Awake()
    {
        if (bootstrapper == null)
            bootstrapper = GetComponent<FlowBootstrapper>();

        if (fakeEnemyService == null)
            fakeEnemyService = GetComponent<FakeEnemySpawnService>();

        if (fakeTowerFactory == null)
            fakeTowerFactory = GetComponent<FakeTowerFactory>();

        if (buildInput == null)
            buildInput = GetComponent<BuildInputController>();

        if (buildManager == null)
            buildManager = GetComponent<BuildManager>();
    }

    private void OnEnable()
    {
        if (bootstrapper != null && bootstrapper.Session != null)
        {
            bootstrapper.Session.SnapshotChanged += HandleSnapshotChanged;
            latestSnapshot = bootstrapper.Session.GetSnapshot();
        }

        BuildInputController.ExternalPointerBlocker = IsPointerOverPanel;
    }

    private void Start()
    {
        if (bootstrapper != null && bootstrapper.Session != null)
            latestSnapshot = bootstrapper.Session.GetSnapshot();
    }

    private void OnDisable()
    {
        if (bootstrapper != null && bootstrapper.Session != null)
            bootstrapper.Session.SnapshotChanged -= HandleSnapshotChanged;

        if (BuildInputController.ExternalPointerBlocker == IsPointerOverPanel)
            BuildInputController.ExternalPointerBlocker = null;
    }

    private static bool IsPointerOverPanel(Vector2 screenPos)
    {
        Vector2 guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
        return PanelRect.Contains(guiPos);
    }

    private void HandleSnapshotChanged(SessionSnapshot snapshot)
    {
        latestSnapshot = snapshot;
    }

    private void OnGUI()
    {
        if (bootstrapper == null || bootstrapper.Session == null)
            return;

        GameSession session = bootstrapper.Session;

        GUILayout.BeginArea(PanelRect, GUI.skin.box);
        GUILayout.Label("<b>FLOW SANDBOX DEBUG (B02 - B08)</b>");
        GUILayout.Space(4);

        GUILayout.Label($"Session ID: {session.SessionId}   |   State: {latestSnapshot.State}");
        GUILayout.Label($"Vàng (Gold): {latestSnapshot.Gold}   |   Mạng (Lives): {latestSnapshot.Lives}");
        GUILayout.Label($"Wave: {latestSnapshot.CurrentWave}/{latestSnapshot.TotalWaves}   |   Quái sống (Alive): {latestSnapshot.AliveEnemies}");
        GUILayout.Label($"Trạng thái: {lastStatusMessage}");
        GUILayout.Space(8);

        DrawEnemySection(session);
        GUILayout.Space(10);
        DrawBuildingSection();
        GUILayout.Space(10);
        DrawSessionSection(session);

        GUILayout.EndArea();
    }

    private void DrawEnemySection(GameSession session)
    {
        GUILayout.Label("--- TEST QUÁI & MẠNG (B02 / B04) ---");

        if (GUILayout.Button("1. Spawn 5 Quái Normal (M1 Test)"))
            SpawnBatch(5);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Giết 1 (+8g)"))
        {
            if (fakeEnemyService != null && fakeEnemyService.KillFirstAlive())
                lastStatusMessage = "Đã giết 1 quái (chờ LateUpdate cộng 8 vàng).";
            else
                lastStatusMessage = "Không có quái sống để giết.";
        }

        if (GUILayout.Button("Cho 1 Lọt (-1 mạng)"))
        {
            if (fakeEnemyService != null && fakeEnemyService.EscapeFirstAlive())
                lastStatusMessage = "Đã cho 1 quái lọt (chờ LateUpdate trừ 1 mạng).";
            else
                lastStatusMessage = "Không có quái sống để cho lọt.";
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Emit Duplicate (Chống Trùng)"))
        {
            fakeEnemyService?.DebugEmitDuplicate();
            lastStatusMessage = "Đã phát trùng event — Vàng/Mạng không được đổi lần 2.";
        }

        string failSpawnLabel = fakeEnemyService != null && fakeEnemyService.FailNextSpawn
            ? "Fail Next Spawn: BẬT"
            : "Fail Next Spawn: TẮT";

        if (GUILayout.Button(failSpawnLabel) && fakeEnemyService != null)
        {
            fakeEnemyService.FailNextSpawn = !fakeEnemyService.FailNextSpawn;
            lastStatusMessage = $"FailNextSpawn = {fakeEnemyService.FailNextSpawn}";
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Clear Session (Dọn quái không thưởng/trừ mạng)"))
        {
            fakeEnemyService?.CleanupSession(session.SessionId);
            session.ResetAliveCount();
            lastStatusMessage = "Đã cleanup toàn bộ quái fake, không đổi vàng/mạng.";
        }
    }

    private void DrawBuildingSection()
    {
        GUILayout.Label("--- ĐIỂM XÂY & MUA / NÂNG / BÁN (B06 - B08) ---");

        BuildPoint selected = buildInput != null ? buildInput.SelectedPoint : null;

        if (selected == null)
        {
            GUILayout.Label("Ô đang chọn: <i>Chưa chọn — Click chuột vào ô vuông bên phải!</i>");
        }
        else if (!selected.IsOccupied)
        {
            GUILayout.Label($"Ô đang chọn: <b>{selected.PointId}</b> (Ô trống)");
            GUILayout.BeginHorizontal();

            buildManager.TryGetBuildCost("arrow", out int buildCost);
            if (GUILayout.Button($"Xây Arrow ({buildCost}g)"))
                HandleBuildClick(selected, "arrow");

            GUILayout.EndHorizontal();
        }
        else
        {
            TowerSnapshot snap = selected.CurrentTowerHandle.ReadSnapshot();
            string towerName = BuildManager.GetDisplayName(snap.TowerId);
            GUILayout.Label($"Ô đang chọn: <b>{selected.PointId}</b> — {towerName} (Lv {snap.Level}/{snap.MaxLevel})");
            GUILayout.Label($"Damage: {snap.Damage} | Chu kỳ: {snap.AttackInterval:0.##}s | Radius: {snap.Radius:0.##} | Vốn: {selected.TotalInvested}g");

            GUILayout.BeginHorizontal();

            if (snap.Level < snap.MaxLevel)
            {
                buildManager.TryGetTowerCost(snap.TowerId, snap.Level + 1, out int upgCost);
                if (GUILayout.Button($"Nâng cấp Lv{snap.Level + 1} ({upgCost}g)"))
                    HandleUpgradeClick(selected);
            }
            else
            {
                GUI.enabled = false;
                GUILayout.Button("ĐÃ CẤP TỐI ĐA");
                GUI.enabled = true;
            }

            int refundPreview = BuildManager.CalculateSellRefund(selected.TotalInvested);
            if (GUILayout.Button($"Bán (+{refundPreview}g)"))
                HandleSellClick(selected);

            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();
        string failCreateLabel = fakeTowerFactory != null && fakeTowerFactory.FailCreate
            ? "Fail Create: BẬT"
            : "Fail Create: TẮT";
        if (GUILayout.Button(failCreateLabel) && fakeTowerFactory != null)
        {
            fakeTowerFactory.FailCreate = !fakeTowerFactory.FailCreate;
            lastStatusMessage = $"FailCreate = {fakeTowerFactory.FailCreate}";
        }

        string failUpgradeLabel = fakeTowerFactory != null && fakeTowerFactory.FailUpgrade
            ? "Fail Upgrade: BẬT"
            : "Fail Upgrade: TẮT";
        if (GUILayout.Button(failUpgradeLabel) && fakeTowerFactory != null)
        {
            fakeTowerFactory.FailUpgrade = !fakeTowerFactory.FailUpgrade;
            lastStatusMessage = $"FailUpgrade = {fakeTowerFactory.FailUpgrade}";
        }
        GUILayout.EndHorizontal();
    }

    private void DrawSessionSection(GameSession session)
    {
        GUILayout.Label("--- TEST STATE / PAUSE / RESTART (B03 / B11) ---");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Pause"))
        {
            bool ok = session.Pause();
            lastStatusMessage = ok ? "Đã Pause (timeScale = 0)." : "Không thể Pause ở state này.";
        }

        if (GUILayout.Button("Resume"))
        {
            bool ok = session.Resume();
            lastStatusMessage = ok ? "Đã Resume về state trước đó." : "Chỉ Resume được khi đang Paused.";
        }

        if (GUILayout.Button("Restart Trận"))
            RestartMatch();
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Test Chuyển Defeat (Trừ hết mạng)"))
        {
            if (fakeEnemyService != null && session.Lives > 0)
            {
                if (session.State == GameState.Preparing) session.TrySetState(GameState.Running);
                var req = new EnemySpawnRequest(session.SessionId, session.CurrentWave, "normal", null);
                if (fakeEnemyService.TrySpawn(req, out EnemyHandle h))
                {
                    session.RegisterEnemySpawned(h, session.CurrentWave);
                    fakeEnemyService.BeginMovement(h);
                    session.EnqueueResolved(new EnemyResolved(session.SessionId, session.CurrentWave, h.Identity.SpawnId, ResolutionReason.Escaped, 0, session.Lives));
                    lastStatusMessage = "Đã kích hoạt quái gây sát thương bằng đúng số mạng còn lại.";
                }
                else
                {
                    lastStatusMessage = "TrySpawn thất bại (FailNextSpawn đang bật).";
                }
            }
        }
    }

    private void HandleBuildClick(BuildPoint point, string towerId)
    {
        if (buildManager == null)
        {
            lastStatusMessage = "Thiếu BuildManager.";
            return;
        }

        if (buildManager.TryBuild(point, towerId, out string error))
            lastStatusMessage = $"Đã xây {BuildManager.GetDisplayName(towerId)} tại {point.PointId}.";
        else
            lastStatusMessage = error;
    }

    private void HandleUpgradeClick(BuildPoint point)
    {
        if (buildManager == null)
            return;

        if (buildManager.TryUpgrade(point, out string error))
            lastStatusMessage = $"Đã nâng cấp tháp tại {point.PointId} lên Lv {point.CurrentLevel}.";
        else
            lastStatusMessage = error;
    }

    private void HandleSellClick(BuildPoint point)
    {
        if (buildManager == null)
            return;

        if (buildManager.TrySell(point, out int refund, out string error))
            lastStatusMessage = $"Đã bán tháp tại {point.PointId} (+{refund}g).";
        else
            lastStatusMessage = error;
    }

    private void SpawnBatch(int count)
    {
        GameSession session = bootstrapper.Session;
        IEnemySpawnService service = bootstrapper.EnemySpawnService;

        if (service == null)
            return;

        if (session.State == GameState.Preparing)
            session.TrySetState(GameState.Running);

        if (!session.CanSimulate)
        {
            lastStatusMessage = $"Không thể sinh quái ở state {session.State}.";
            return;
        }

        int spawned = 0;
        for (int i = 0; i < count; i++)
        {
            var req = new EnemySpawnRequest(session.SessionId, session.CurrentWave, "normal", null);
            if (service.TrySpawn(req, out EnemyHandle handle))
            {
                session.RegisterEnemySpawned(handle, session.CurrentWave);
                service.BeginMovement(handle);
                spawned++;
            }
        }

        lastStatusMessage = $"Đã sinh {spawned}/{count} quái fake.";
    }

    private void RestartMatch()
    {
        bootstrapper.StartNewSession();
        lastStatusMessage = $"Đã Restart -> Session mới ID {bootstrapper.Session.SessionId} (240 vàng, 20 mạng).";
    }
}

