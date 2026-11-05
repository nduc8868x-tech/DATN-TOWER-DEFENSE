using UnityEngine.Splines;

public readonly struct EnemySpawnRequest
{
    public int SessionId { get; }
    public int WaveId { get; }
    public string EnemyId { get; }
    public SplineContainer Path { get; }

    public EnemySpawnRequest(int sessionId, int waveId, string enemyId, SplineContainer path)
    {
        SessionId = sessionId;
        WaveId = waveId;
        EnemyId = enemyId;
        Path = path;
    }
}
