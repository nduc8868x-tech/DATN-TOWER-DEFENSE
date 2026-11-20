// Value-only contract: no mutable ScriptableObject or prefab references escape.
public readonly struct TowerSnapshot
{
    public readonly string TowerId;
    public readonly int InstanceId, Level, Damage, Cost, NextLevelCost, ShotCount;
    public readonly float Range, Cooldown, ProjectileSpeed, RemainingCooldown, Radius;
    public readonly bool Active, CanUpgrade;
    public readonly int MaxLevel;
    public bool IsDisposed => InstanceId == 0;
    public bool IsActive => Active;
    public float AttackInterval => Cooldown;

    // Sandbox uses the same value contract; no alternate gameplay definition.
    public TowerSnapshot(string towerId, int level, int maxLevel, bool active, bool disposed,
        int damage, float cooldown, float radius)
    {
        this = default;
        TowerId = towerId; Level = level; MaxLevel = maxLevel;
        InstanceId = disposed ? 0 : -1; Active = active; Damage = damage;
        Cooldown = cooldown; Radius = radius; CanUpgrade = !disposed && level < maxLevel;
    }

    internal TowerSnapshot(Tower tower)
    {
        var state = tower.ReadSnapshot();
        TowerId = tower.Data.towerId;
        MaxLevel = tower.Data.LevelCount;
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

