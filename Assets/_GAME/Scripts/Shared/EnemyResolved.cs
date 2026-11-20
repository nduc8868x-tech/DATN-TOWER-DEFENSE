public enum ResolutionReason { Dead, Escaped }

public readonly struct EnemyResolved
{
    public EnemyIdentity Identity { get; }
    public int WaveId { get; }
    public EnemyState State { get; }
    public int Bounty { get; }
    public int LeakDamage { get; }
    public int SessionId => Identity.SessionId;
    public int SpawnId => Identity.SpawnId;
    public ResolutionReason Reason => State == EnemyState.Dead ? ResolutionReason.Dead : ResolutionReason.Escaped;
    public EnemyResolved(int sessionId, int waveId, int spawnId, ResolutionReason reason, int bounty, int leakDamage)
        : this(new EnemyIdentity(sessionId, spawnId), waveId,
            reason == ResolutionReason.Dead ? EnemyState.Dead : EnemyState.Escaped, bounty, leakDamage) { }

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
