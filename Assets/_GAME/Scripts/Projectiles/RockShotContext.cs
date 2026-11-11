using UnityEngine;

// Value-only launch snapshot. Intentionally no EnemyHandle or mutable TowerData reference.
public readonly struct RockShotContext
{
    public readonly Vector3 ImpactPosition;
    public readonly int SessionId, Damage;
    public readonly float Radius, Speed, Lifetime;
    public RockShotContext(Vector3 impactPosition, int sessionId, int damage, float radius, float speed, float lifetime)
    {
        ImpactPosition = impactPosition; SessionId = sessionId; Damage = damage;
        Radius = radius; Speed = speed; Lifetime = lifetime;
    }
}
