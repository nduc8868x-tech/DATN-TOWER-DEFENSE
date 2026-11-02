public readonly struct HitRequest
{
    public Enemy Target { get; }
    public EnemyIdentity Identity { get; }
    public int RawDamage { get; }

    public HitRequest(
        Enemy target,
        EnemyIdentity identity,
        int rawDamage)
    {
        Target = target;
        Identity = identity;
        RawDamage = rawDamage;
    }
}