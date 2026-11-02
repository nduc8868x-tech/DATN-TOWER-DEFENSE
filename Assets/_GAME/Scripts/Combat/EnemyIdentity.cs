using System;

public readonly struct EnemyIdentity : IEquatable<EnemyIdentity>
{
    public int SessionId { get; }
    public int SpawnId { get; }

    public bool IsValid => SessionId > 0 && SpawnId > 0;

    public EnemyIdentity(int sessionId, int spawnId)
    {
        SessionId = sessionId;
        SpawnId = spawnId;
    }

    public bool Equals(EnemyIdentity other)
    {
        return SessionId == other.SessionId
            && SpawnId == other.SpawnId;
    }

    public override bool Equals(object obj)
    {
        return obj is EnemyIdentity other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (SessionId * 397) ^ SpawnId;
        }
    }

    public override string ToString()
    {
        return $"{SessionId}:{SpawnId}";
    }
}