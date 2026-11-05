public enum ResolutionReason
{
    Dead,
    Escaped
}

public readonly struct EnemyResolved
{
    public int SessionId { get; }
    public int WaveId { get; }
    public int SpawnId { get; }
    public ResolutionReason Reason { get; }
    public int Bounty { get; }      // Tiền thưởng khi diệt quái (nếu Dead)
    public int LeakDamage { get; }  // Số mạng bị trừ (nếu Escaped)

    public EnemyResolved(int sessionId, int waveId, int spawnId, ResolutionReason reason, int bounty, int leakDamage)
    {
        SessionId = sessionId;
        WaveId = waveId;
        SpawnId = spawnId;
        Reason = reason;
        Bounty = bounty;
        LeakDamage = leakDamage;
    }
}
