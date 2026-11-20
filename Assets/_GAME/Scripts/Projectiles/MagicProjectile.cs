using UnityEngine;

public sealed class MagicProjectile : Projectile
{
    [SerializeField, Range(0f, 0.95f)] private float slowFraction;
    [SerializeField, Min(0f)] private float slowDuration;
    [SerializeField, Min(1)] private int maximumTargets = 1;
    [SerializeField, Min(0f)] private float chainRadius;
    [SerializeField, Range(0f, 1f)] private float chainMultiplier = 1f;

    protected override void OnHit(ShotContext shot, CombatResolver resolver)
    {
        if (slowFraction > 0f && slowDuration > 0f && shot.Target.IsAlive)
        {
            var slow = shot.Target.Target.GetComponent<EnemySlowEffect>();
            if (slow == null) slow = shot.Target.Target.gameObject.AddComponent<EnemySlowEffect>();
            slow.Apply(slowFraction, slowDuration);
        }

        var candidates = new System.Collections.Generic.List<EnemyHandle>();
        var visited = new System.Collections.Generic.HashSet<EnemyIdentity> { shot.Target.Identity };
        resolver.Registry.CopyAliveTo(candidates);
        Vector3 origin = shot.Target.Position;
        float damage = shot.Damage;
        for (int hop = 1; hop < maximumTargets; hop++)
        {
            EnemyHandle nearest = default;
            float nearestDistance = chainRadius * chainRadius;
            foreach (var candidate in candidates)
            {
                if (!candidate.IsAlive || candidate.Identity.SessionId != shot.SessionId || visited.Contains(candidate.Identity))
                    continue;
                float distance = ((Vector2)(candidate.Position - origin)).sqrMagnitude;
                if (distance > nearestDistance) continue;
                nearest = candidate;
                nearestDistance = distance;
            }
            if (!nearest.IsAlive) break;
            visited.Add(nearest.Identity);
            damage *= chainMultiplier;
            resolver.RequestHit(new HitRequest(nearest.Target, nearest.Identity, Mathf.Max(1, Mathf.RoundToInt(damage))));
            origin = nearest.Position;
        }
    }
}
