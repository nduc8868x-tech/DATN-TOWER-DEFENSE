public readonly struct ShotContext
{
    public readonly EnemyHandle Target;
    public readonly int SessionId, Damage;
    public readonly float Speed, Lifetime;
    public ShotContext(EnemyHandle target, int sessionId, int damage, float speed, float lifetime)
    {
        Target = target; SessionId = sessionId; Damage = damage; Speed = speed; Lifetime = lifetime;
    }
}
