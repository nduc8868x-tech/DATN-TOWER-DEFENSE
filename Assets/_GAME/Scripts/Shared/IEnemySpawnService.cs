using System;

public interface IEnemySpawnService
{
    event Action<EnemyResolved> Resolved;
    bool TrySpawn(EnemySpawnRequest request, out EnemyHandle handle);
    void BeginMovement(EnemyHandle handle);
    void CleanupSession(int sessionId);
}
