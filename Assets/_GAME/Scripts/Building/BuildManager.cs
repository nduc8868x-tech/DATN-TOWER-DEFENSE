using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BuildManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GameSession gameSession;
    [SerializeField] private EconomyManager economyManager;
    [SerializeField] private FlowBootstrapper bootstrapper;

    private readonly List<BuildPoint> trackedPoints = new List<BuildPoint>();

    public event Action<BuildPoint> PointChanged;

    private void Awake()
    {
        if (gameSession == null)
            gameSession = GetComponent<GameSession>();

        if (economyManager == null)
            economyManager = GetComponent<EconomyManager>();

        if (bootstrapper == null)
            bootstrapper = GetComponent<FlowBootstrapper>();
    }

    public ITowerFactory GetTowerFactory()
    {
        return bootstrapper != null ? bootstrapper.TowerFactory : null;
    }

    public bool TryGetBuildCost(string towerId, out int cost)
    {
        return TryGetTowerCost(towerId, 1, out cost);
    }

    public bool TryGetUpgradeCost(string towerId, int targetLevel, out int cost)
    {
        return TryGetTowerCost(towerId, targetLevel, out cost);
    }

    public static bool TryGetTowerCost(string towerId, int level, out int cost)
    {
        cost = 0;
        switch (towerId)
        {
            case "arrow":
            case "archer":
                if (level == 1 || level == 2) { cost = 100; return true; }
                return false;

            case "stone":
                if (level == 1 || level == 2) { cost = 140; return true; }
                return false;

            case "magic":
                if (level == 1 || level == 2) { cost = 90; return true; }
                return false;

            default:
                return false;
        }
    }

    public static string GetDisplayName(string towerId)
    {
        switch (towerId)
        {
            case "arrow":
            case "archer":
                return "Tháp Cung (Arrow)";
            case "stone":
                return "Tháp Đá (Stone)";
            case "magic":
                return "Tháp Phép (Magic)";
            default:
                return towerId;
        }
    }

    public static int CalculateSellRefund(int totalInvested)
    {
        return Mathf.FloorToInt(0.7f * Mathf.Max(0, totalInvested));
    }

    public bool TryBuild(BuildPoint point, string towerId, out string error)
    {
        if (gameSession == null || !gameSession.BuildAllowed)
        {
            error = $"Không thể xây ở trạng thái {(gameSession != null ? gameSession.State.ToString() : "Null")}.";
            return false;
        }

        if (economyManager == null)
        {
            error = "Thiếu EconomyManager.";
            return false;
        }

        ITowerFactory factory = GetTowerFactory();
        if (factory == null)
        {
            error = "Chưa có ITowerFactory từ FlowBootstrapper.";
            return false;
        }

        if (point == null)
        {
            error = "Điểm xây không tồn tại.";
            return false;
        }

        if (point.TransactionInProgress)
        {
            error = $"Ô {point.PointId} đang xử lý giao dịch khác.";
            return false;
        }

        if (point.IsOccupied)
        {
            error = $"Ô {point.PointId} đã có tháp.";
            return false;
        }

        if (!TryGetBuildCost(towerId, out int cost))
        {
            error = $"Không có dữ liệu giá cho tháp '{towerId}'.";
            return false;
        }

        if (!economyManager.CanAfford(cost))
        {
            error = $"Không đủ vàng để xây {GetDisplayName(towerId)} ({economyManager.CurrentGold}/{cost}).";
            return false;
        }

        point.BeginTransaction();
        try
        {
            if (!factory.TryCreate(towerId, 1, point.AnchorPosition, out TowerHandle handle) || handle == null)
            {
                error = $"Factory từ chối tạo '{towerId}' (tiền và ô giữ nguyên).";
                return false;
            }

            if (!economyManager.TrySpend(cost))
            {
                handle.Dispose();
                error = "Trừ vàng thất bại -> Đã dọn tháp thử.";
                return false;
            }

            point.SetOccupied(handle, towerId, 1, cost);
            if (!trackedPoints.Contains(point))
                trackedPoints.Add(point);

            handle.Activate();

            error = string.Empty;
            PointChanged?.Invoke(point);
            return true;
        }
        finally
        {
            point.EndTransaction();
        }
    }

    public bool TryUpgrade(BuildPoint point, out string error)
    {
        if (gameSession == null || !gameSession.BuildAllowed)
        {
            error = $"Không thể nâng cấp ở trạng thái {(gameSession != null ? gameSession.State.ToString() : "Null")}.";
            return false;
        }

        if (economyManager == null)
        {
            error = "Thiếu EconomyManager.";
            return false;
        }

        if (point == null || !point.IsOccupied)
        {
            error = "Ô đang chọn chưa có tháp để nâng cấp.";
            return false;
        }

        if (point.TransactionInProgress)
        {
            error = $"Ô {point.PointId} đang xử lý giao dịch khác.";
            return false;
        }

        TowerHandle handle = point.CurrentTowerHandle;
        TowerSnapshot snap = handle.ReadSnapshot();
        if (snap.IsDisposed || snap.Level >= snap.MaxLevel)
        {
            error = "Tháp đã đạt cấp tối đa.";
            return false;
        }

        int nextLevel = snap.Level + 1;
        if (!TryGetUpgradeCost(snap.TowerId, nextLevel, out int upgradeCost))
        {
            error = $"Không có dữ liệu nâng cấp cấp {nextLevel} cho '{snap.TowerId}'.";
            return false;
        }

        if (!economyManager.CanAfford(upgradeCost))
        {
            error = $"Không đủ vàng nâng cấp ({economyManager.CurrentGold}/{upgradeCost}).";
            return false;
        }

        point.BeginTransaction();
        try
        {
            if (!handle.TryApplyLevel(nextLevel))
            {
                error = "Factory từ chối nâng cấp (cấp và vàng giữ nguyên).";
                return false;
            }

            if (!economyManager.TrySpend(upgradeCost))
            {
                handle.TryApplyLevel(snap.Level);
                error = "Trừ vàng nâng cấp thất bại -> Đã hoàn cấp cũ.";
                return false;
            }

            point.AddInvestment(upgradeCost, nextLevel);

            error = string.Empty;
            PointChanged?.Invoke(point);
            return true;
        }
        finally
        {
            point.EndTransaction();
        }
    }

    public bool TrySell(BuildPoint point, out int refund, out string error)
    {
        refund = 0;

        if (gameSession == null || !gameSession.BuildAllowed)
        {
            error = $"Không thể bán ở trạng thái {(gameSession != null ? gameSession.State.ToString() : "Null")}.";
            return false;
        }

        if (economyManager == null)
        {
            error = "Thiếu EconomyManager.";
            return false;
        }

        if (point == null || !point.IsOccupied)
        {
            error = "Ô đang chọn chưa có tháp để bán.";
            return false;
        }

        if (point.TransactionInProgress)
        {
            error = $"Ô {point.PointId} đang xử lý giao dịch khác.";
            return false;
        }

        point.BeginTransaction();
        try
        {
            TowerHandle handle = point.CurrentTowerHandle;
            refund = CalculateSellRefund(point.TotalInvested);

            handle.Suspend();
            handle.Dispose();

            point.ClearOccupancy();

            if (refund > 0)
                economyManager.AddGold(refund);

            error = string.Empty;
            PointChanged?.Invoke(point);
            return true;
        }
        finally
        {
            point.EndTransaction();
        }
    }

    public void ClearAllTowers()
    {
        for (int i = 0; i < trackedPoints.Count; i++)
        {
            BuildPoint point = trackedPoints[i];
            if (point == null || !point.IsOccupied)
                continue;

            TowerHandle handle = point.CurrentTowerHandle;
            handle.Suspend();
            handle.Dispose();
            point.ClearOccupancy();
        }

        trackedPoints.Clear();
    }
}
