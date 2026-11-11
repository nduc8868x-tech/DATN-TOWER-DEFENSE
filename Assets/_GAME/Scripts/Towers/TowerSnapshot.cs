// Value-only contract: no mutable ScriptableObject or prefab references escape.
public readonly struct TowerSnapshot
{
    public readonly string TowerId;
    public readonly int InstanceId, Level, Damage, Cost, NextLevelCost, ShotCount;
    public readonly float Range, Cooldown, ProjectileSpeed, RemainingCooldown, Radius;
    public readonly bool Active, CanUpgrade;

    internal TowerSnapshot(Tower tower)
    {
        var state = tower.ReadSnapshot();
        TowerId = tower.Data.towerId;
        InstanceId = tower.GetInstanceID();
        Level = state.Level; Damage = state.Damage; Range = state.Range;
        Cooldown = state.Cooldown; ProjectileSpeed = state.ProjectileSpeed;
        Radius = state.Radius;
        RemainingCooldown = state.RemainingCooldown; Active = state.Active; ShotCount = tower.ShotCount;
        Cost = tower.Data.TryGetLevel(Level, out var current, out _) ? current.cost : 0;
        CanUpgrade = tower.TryValidateCombatLevel(tower.Data, Level + 1, out var next, out _);
        NextLevelCost = CanUpgrade ? next.cost : 0;
    }
}
