public readonly struct TowerSnapshot
{
    public string TowerId { get; }
    public int Level { get; }
    public int MaxLevel { get; }
    public bool IsActive { get; }
    public bool IsDisposed { get; }
    public int Damage { get; }
    public float AttackInterval { get; }
    public float Radius { get; }

    public TowerSnapshot(
        string towerId,
        int level,
        int maxLevel,
        bool isActive,
        bool isDisposed,
        int damage,
        float attackInterval,
        float radius)
    {
        TowerId = towerId;
        Level = level;
        MaxLevel = maxLevel;
        IsActive = isActive;
        IsDisposed = isDisposed;
        Damage = damage;
        AttackInterval = attackInterval;
        Radius = radius;
    }
}
