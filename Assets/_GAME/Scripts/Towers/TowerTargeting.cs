using System.Collections.Generic;
using UnityEngine;

public sealed class TowerTargeting
{
    private readonly List<EnemyHandle> buffer = new List<EnemyHandle>();
    private float remainingScan;
    public EnemyHandle Current { get; private set; }

    public static bool IsValid(EnemyHandle handle, int session, Vector3 origin, float range)
    {
        Vector2 offset = handle.Position - origin;
        return handle.IsAlive && handle.Identity.SessionId == session && offset.sqrMagnitude <= range * range;
    }

    public void Reset() { Current = default; remainingScan = 0f; buffer.Clear(); }

    public EnemyHandle Tick(EnemyRegistry registry, int session, Vector3 origin, float range, float deltaTime)
    {
        remainingScan -= deltaTime;
        if (remainingScan > 0f && IsValid(Current, session, origin, range))
            return Current;
        remainingScan = 0.1f;
        registry.CopyAliveTo(buffer);
        EnemyHandle best = default;
        foreach (var candidate in buffer)
        {
            if (!IsValid(candidate, session, origin, range)) continue;
            if (!best.IsAlive || candidate.Progress > best.Progress ||
                (candidate.Progress == best.Progress && candidate.Identity.SpawnId < best.Identity.SpawnId))
                best = candidate;
        }
        Current = best;
        return Current;
    }
}
