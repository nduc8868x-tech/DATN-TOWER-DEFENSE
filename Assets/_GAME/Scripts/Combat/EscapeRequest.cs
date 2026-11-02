public readonly struct EscapeRequest
{
    public Enemy Target { get; }
    public EnemyIdentity Identity { get; }

    public EscapeRequest(
        Enemy target,
        EnemyIdentity identity)
    {
        Target = target;
        Identity = identity;
    }
}