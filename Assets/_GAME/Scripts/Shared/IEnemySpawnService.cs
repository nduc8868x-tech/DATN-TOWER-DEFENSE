using System;

public interface IEnemySpawnService
{
    string LastError { get; }
    event Action<EnemyResolved> Resolved;
    bool TrySpawn(EnemySpawnRequest request, out EnemyHandle handle);
    bool BeginMovement(EnemyHandle handle);
    void CleanupSession(int sessionId);
}
