public readonly struct EnemyResolved
{
    public EnemyIdentity Identity { get; }
    public int WaveId { get; }
    public EnemyState State { get; }
    public int Bounty { get; }
    public int LeakDamage { get; }

    public EnemyResolved(
        EnemyIdentity identity,
        int waveId,
        EnemyState state,
        int bounty,
        int leakDamage)
    {
        Identity = identity;
        WaveId = waveId;
        State = state;
        Bounty = bounty;
        LeakDamage = leakDamage;
    }
}
