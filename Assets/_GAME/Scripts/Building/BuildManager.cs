using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BuildManager : MonoBehaviour
{
    [SerializeField] private GameSession gameSession;
    [SerializeField] private EconomyManager economyManager;
    [SerializeField] private FlowBootstrapper bootstrapper;
    [SerializeField] private TowerCatalog catalog;
    private readonly List<BuildPoint> trackedPoints = new List<BuildPoint>();
    private bool transaction;
    public event Action<BuildPoint> PointChanged;
    private void Awake()
    {
        if (!gameSession) gameSession = GetComponent<GameSession>();
        if (!economyManager) economyManager = GetComponent<EconomyManager>();
        if (!bootstrapper) bootstrapper = GetComponent<FlowBootstrapper>();
    }
    public ITowerFactory GetTowerFactory() => bootstrapper != null ? bootstrapper.TowerFactory : null;
    public bool TryGetBuildCost(string id, out int cost) => TryGetTowerCost(id, 1, out cost);
    public bool TryGetUpgradeCost(string id, int level, out int cost) => TryGetTowerCost(id, level, out cost);
    public bool TryGetTowerCost(string id, int level, out int cost)
    {
        cost = 0;
        if (!catalog || !catalog.TryGet(id, out var data) || !data.TryGetLevel(level, out var stats, out _)) return false;
        cost = stats.cost;
        return true;
    }
    public static string GetDisplayName(string id) => id == "arrow" ? "Arrow" : id == "rock" ? "Rock" : id;
    public static int CalculateSellRefund(int invested) => (int)(Math.Max(0, invested) * 7L / 10L);
    private bool Validate(BuildPoint point, bool occupied, out string error)
    {
        error = "Gameplay is locked, point unavailable or transaction in progress.";
        return gameSession && gameSession.BuildAllowed && economyManager && !transaction && point &&
            point.isActiveAndEnabled && !point.TransactionInProgress && point.IsOccupied == occupied;
    }
    public bool TryBuild(BuildPoint point, string id, out string error)
    {
        if (!Validate(point, false, out error)) return false;
        if (id != "arrow") { error = "Only Arrow is available in this integration milestone."; return false; }
        var factory = GetTowerFactory();
        if (factory == null || !TryGetBuildCost(id, out var cost) || !economyManager.CanAfford(cost))
        { error = "Missing factory/data or insufficient gold."; return false; }
        transaction = true; point.BeginTransaction();
        TowerHandle created = null;
        bool committed = false, reserved = false;
        try
        {
            if (!factory.TryCreate(id, 1, point.AnchorPosition, out created) || created == null)
            { error = "Tower factory rejected creation; gold and point unchanged."; return false; }
            reserved = economyManager.ReserveSpend(cost);
            if (!reserved) { error = "Cannot reserve purchase cost."; return false; }
            point.SetOccupied(created, id, 1, cost);
            if (!created.Activate()) { error = "Tower activation rejected; purchase cancelled."; return false; }
            if (!trackedPoints.Contains(point)) trackedPoints.Add(point);
            committed = true; error = string.Empty;
            return true;
        }
        finally
        {
            if (!committed) { created?.Suspend(); created?.Dispose(); point.ClearOccupancy(); }
            if (reserved) economyManager.FinishSpend(committed);
            point.EndTransaction(); transaction = false;
            if (committed) PointChanged?.Invoke(point);
        }
    }
    public bool TryUpgrade(BuildPoint point, out string error)
    {
        if (!Validate(point, true, out error)) return false;
        var handle = point.CurrentTowerHandle;
        var before = handle.ReadSnapshot();
        if (before.IsDisposed || !before.CanUpgrade || !TryGetUpgradeCost(before.TowerId, before.Level + 1, out var cost))
        { error = "No valid next level."; return false; }
        transaction = true; point.BeginTransaction();
        bool committed = false, reserved = false;
        try
        {
            reserved = economyManager.ReserveSpend(cost);
            if (!reserved) { error = "Insufficient gold."; return false; }
            // TryApplyLevel validates everything before mutation. Failure needs no level rollback.
            if (!handle.TryApplyLevel(before.Level + 1))
            { error = "Upgrade rejected; stats, level and gold unchanged."; return false; }
            point.AddInvestment(cost, before.Level + 1);
            committed = true; error = string.Empty;
            return true;
        }
        finally
        {
            if (reserved) economyManager.FinishSpend(committed);
            point.EndTransaction(); transaction = false;
            if (committed) PointChanged?.Invoke(point);
        }
    }
    public bool TrySell(BuildPoint point, out int refund, out string error)
    {
        refund = 0;
        if (!Validate(point, true, out error)) return false;
        transaction = true; point.BeginTransaction();
        try
        {
            var handle = point.CurrentTowerHandle;
            refund = CalculateSellRefund(point.TotalInvested);
            handle.Suspend(); handle.Dispose(); point.ClearOccupancy();
            economyManager.AddGold(refund);
            error = string.Empty;
        }
        finally { point.EndTransaction(); transaction = false; }
        PointChanged?.Invoke(point);
        return true;
    }
    public void ClearAllTowers()
    {
        foreach (var point in trackedPoints)
        {
            if (!point) continue;
            point.CurrentTowerHandle?.Suspend(); point.CurrentTowerHandle?.Dispose();
            point.ClearOccupancy(); point.EndTransaction(); point.SetSelected(false);
            PointChanged?.Invoke(point);
        }
        trackedPoints.Clear(); transaction = false;
    }
}
