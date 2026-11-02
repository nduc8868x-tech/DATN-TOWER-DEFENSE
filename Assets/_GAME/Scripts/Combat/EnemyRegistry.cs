using System.Collections.Generic;

public sealed class EnemyRegistry
{
    private readonly Dictionary<EnemyIdentity, EnemyHandle> entries =
        new Dictionary<EnemyIdentity, EnemyHandle>();

    public void Add(Enemy enemy)
    {
        var handle = new EnemyHandle(enemy);

        if (handle.IsAlive)
            entries[handle.Identity] = handle;
    }

    public void Remove(EnemyIdentity identity)
    {
        entries.Remove(identity);
    }

    public void ClearSession()
    {
        entries.Clear();
    }

    public void CopyAliveTo(List<EnemyHandle> results)
    {
        results.Clear();

        foreach (EnemyHandle handle in entries.Values)
        {
            if (handle.IsAlive)
                results.Add(handle);
        }
    }
}