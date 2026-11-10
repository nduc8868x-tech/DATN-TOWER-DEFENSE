using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FlowSandboxDebugPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FlowBootstrapper bootstrapper;
    [SerializeField] private FakeEnemySpawnService fakeEnemyService;
    [SerializeField] private FakeTowerFactory fakeTowerFactory;

    private readonly List<(TowerHandle handle, int totalInvested)> builtTowers = new List<(TowerHandle, int)>();
    private string lastStatusMessage = "San sang test FlowSandbox (Mục B02 - B05).";
    private SessionSnapshot latestSnapshot;

    private void Awake()
    {
        if (bootstrapper == null)
            bootstrapper = GetComponent<FlowBootstrapper>();

        if (fakeEnemyService == null)
            fakeEnemyService = GetComponent<FakeEnemySpawnService>();

        if (fakeTowerFactory == null)
            fakeTowerFactory = GetComponent<FakeTowerFactory>();
    }

    private void OnEnable()
    {
        if (bootstrapper != null && bootstrapper.Session != null)
        {
            bootstrapper.Session.SnapshotChanged += HandleSnapshotChanged;
            latestSnapshot = bootstrapper.Session.GetSnapshot();
        }
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
        EconomyManager economy = bootstrapper.Economy;

        GUILayout.BeginArea(new Rect(16, 16, 400, 660), GUI.skin.box);
        GUILayout.Label("<b>FLOW SANDBOX DEBUG (B02 - B05)</b>");
        GUILayout.Space(4);

        GUILayout.Label($"Session ID: {session.SessionId}   |   State: {latestSnapshot.State}");
        GUILayout.Label($"Vàng (Gold): {latestSnapshot.Gold}   |   Mạng (Lives): {latestSnapshot.Lives}");
        GUILayout.Label($"Wave: {latestSnapshot.CurrentWave}/{latestSnapshot.TotalWaves}   |   Quái sống (Alive): {latestSnapshot.AliveEnemies}");
        GUILayout.Label($"Trạng thái: {lastStatusMessage}");
        GUILayout.Space(8);

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
        if (GUILayout.Button("Emit Duplicate (Test Chống Trùng)"))
        {
            fakeEnemyService?.DebugEmitDuplicate();
            lastStatusMessage = "Đã cố ý phát trùng event — Vàng/Mạng không được đổi lần 2.";
        }

        string failSpawnLabel = fakeEnemyService != null && fakeEnemyService.FailNextSpawn
            ? "Fail Next Spawn: BẬT"
            : "Fail Next Spawn: TẮT";

        if (GUILayout.Button(failSpawnLabel))
        {
            if (fakeEnemyService != null)
            {
                fakeEnemyService.FailNextSpawn = !fakeEnemyService.FailNextSpawn;
                lastStatusMessage = $"FailNextSpawn = {fakeEnemyService.FailNextSpawn}";
            }
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Clear Session (Dọn quái không thưởng/trừ mạng)"))
        {
            fakeEnemyService?.CleanupSession(session.SessionId);
            session.ResetAliveCount();
            lastStatusMessage = "Đã cleanup toàn bộ quái fake, không đổi vàng/mạng.";
        }

        GUILayout.Space(10);
        GUILayout.Label("--- TEST XÂY / NÂNG / BÁN THÁP (B02 / B05) ---");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Mua Cung I (100g)"))
            TryBuildFakeArcher(session, economy);

        if (GUILayout.Button("Nâng Cung II (100g)"))
            TryUpgradeLastTower(session, economy);

        if (GUILayout.Button("Bán Tháp Cuối (70%)"))
            TrySellLastTower(session, economy);
        GUILayout.EndHorizontal();

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

        GUILayout.Space(10);
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
                var req = new EnemySpawnRequest(session.SessionId, session.CurrentWave, "normal", null);
                if (fakeEnemyService.TrySpawn(req, out EnemyHandle h))
                {
                    session.RegisterEnemySpawned();
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

        GUILayout.EndArea();
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
                session.RegisterEnemySpawned();
                service.BeginMovement(handle);
                spawned++;
            }
        }

        lastStatusMessage = $"Đã sinh {spawned}/{count} quái fake.";
    }

    private void TryBuildFakeArcher(GameSession session, EconomyManager economy)
    {
        if (!session.BuildAllowed)
        {
            lastStatusMessage = $"Bị từ chối: Không được xây ở state {session.State}.";
            return;
        }

        const int cost = 100;
        if (!economy.CanAfford(cost))
        {
            lastStatusMessage = $"Không đủ vàng ({economy.CurrentGold}/{cost}).";
            return;
        }

        Vector3 pos = new Vector3(-3.5f + (builtTowers.Count % 6) * 1.4f, -1.5f, 0f);
        if (bootstrapper.TowerFactory == null || !bootstrapper.TowerFactory.TryCreate("archer", 1, pos, out TowerHandle handle))
        {
            lastStatusMessage = "Factory báo lỗi tạo tháp -> Vàng giữ nguyên (Rollback thành công).";
            return;
        }

        if (!economy.TrySpend(cost))
        {
            handle.Dispose();
            lastStatusMessage = "Lỗi trừ tiền -> Đã Dispose tháp thử.";
            return;
        }

        handle.Activate();
        builtTowers.Add((handle, cost));
        lastStatusMessage = $"Đã xây Cung I (-{cost}g). Số tháp: {builtTowers.Count}";
    }

    private void TryUpgradeLastTower(GameSession session, EconomyManager economy)
    {
        if (!session.BuildAllowed || builtTowers.Count == 0)
        {
            lastStatusMessage = "Không có tháp hoặc state không cho phép nâng.";
            return;
        }

        int idx = builtTowers.Count - 1;
        var (handle, invested) = builtTowers[idx];
        TowerSnapshot snap = handle.ReadSnapshot();

        if (snap.Level >= snap.MaxLevel)
        {
            lastStatusMessage = "Tháp đã đạt cấp tối đa.";
            return;
        }

        const int upgradeCost = 100;
        if (!economy.CanAfford(upgradeCost))
        {
            lastStatusMessage = $"Không đủ vàng để nâng ({economy.CurrentGold}/{upgradeCost}).";
            return;
        }

        if (!handle.TryApplyLevel(snap.Level + 1))
        {
            lastStatusMessage = "Factory lỗi nâng cấp -> Vàng và cấp giữ nguyên!";
            return;
        }

        economy.TrySpend(upgradeCost);
        builtTowers[idx] = (handle, invested + upgradeCost);
        lastStatusMessage = $"Đã nâng lên cấp {snap.Level + 1} (-{upgradeCost}g). Tổng vốn: {invested + upgradeCost}";
    }

    private void TrySellLastTower(GameSession session, EconomyManager economy)
    {
        if (!session.BuildAllowed || builtTowers.Count == 0)
        {
            lastStatusMessage = "Không có tháp hoặc state không cho phép bán.";
            return;
        }

        int idx = builtTowers.Count - 1;
        var (handle, invested) = builtTowers[idx];
        builtTowers.RemoveAt(idx);

        handle.Suspend();
        handle.Dispose();

        int refund = Mathf.FloorToInt(0.7f * invested);
        economy.AddGold(refund);
        lastStatusMessage = $"Đã bán tháp (vốn {invested}) -> Hoàn {refund}g (70%).";
    }

    private void RestartMatch()
    {
        for (int i = 0; i < builtTowers.Count; i++)
            builtTowers[i].handle?.Dispose();

        builtTowers.Clear();
        bootstrapper.StartNewSession();
        lastStatusMessage = $"Đã Restart -> Session mới ID {bootstrapper.Session.SessionId} (240 vàng, 20 mạng).";
    }
}
