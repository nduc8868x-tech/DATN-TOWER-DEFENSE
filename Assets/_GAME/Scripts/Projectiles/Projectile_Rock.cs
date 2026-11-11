using UnityEngine;

// Uses the same resolver registry/cleanup as arrows, with a fixed-point flight strategy.
[DisallowMultipleComponent]
public sealed class Projectile_Rock : Projectile
{
    private RockShotContext shot;
    private CombatResolver combat;
    private bool launched, rockConsumed;
    public RockShotContext RockContext => shot;
    public override int SessionId => shot.SessionId;
    public int ExplosionCount { get; private set; }
    public bool Consumed => rockConsumed;
    // Observation only, never a gameplay damage callback in session 13.
    public event System.Action<RockShotContext> DebugExploded;

    public bool Initialize(RockShotContext snapshot, CombatResolver resolver)
    {
        if (launched || rockConsumed || !Application.isPlaying || !TryValidate(out _) ||
            resolver == null || snapshot.SessionId <= 0 || snapshot.SessionId != resolver.SessionId ||
            snapshot.Damage <= 0 || !Positive(snapshot.Radius) || !Positive(snapshot.Speed) || !Positive(snapshot.Lifetime) ||
            !Finite(snapshot.ImpactPosition.x) || !Finite(snapshot.ImpactPosition.y) || !Finite(snapshot.ImpactPosition.z)) return false;
        shot = snapshot; combat = resolver; RemainingLifetime = snapshot.Lifetime; launched = true;
        combat.RegisterProjectile(this);
        return true;
    }

    // Prevent accidental use of the direct-hit/homing API on this projectile.
    public override bool Initialize(ShotContext snapshot, CombatResolver resolver) => false;

    protected override void Update()
    {
        if (!launched || rockConsumed) return;
        if (combat == null || combat.SessionId != SessionId) { Cancel(); return; }
        float dt = Time.deltaTime;
        if (Time.timeScale <= 0f || dt <= 0f) return;
        // Flight is clipped to remaining lifetime; a long frame cannot overshoot expiry.
        float flightTime = Mathf.Min(dt, RemainingLifetime);
        float distance = Vector3.Distance(transform.position, shot.ImpactPosition);
        if (distance / shot.Speed <= flightTime)
        {
            RemainingLifetime = Mathf.Max(0f, RemainingLifetime - distance / shot.Speed);
            transform.position = shot.ImpactPosition;
            Explode();
            return;
        }
        transform.position = Vector3.MoveTowards(transform.position, shot.ImpactPosition, shot.Speed * flightTime);
        RemainingLifetime = Mathf.Max(0f, RemainingLifetime - dt);
        if (RemainingLifetime <= 0f) Cancel();
    }

    private void Explode()
    {
        if (rockConsumed || combat == null || combat.SessionId != SessionId) return;
        rockConsumed = true; // Must precede visual/log/event, including reentrant callbacks.
        ExplosionCount++;
        try
        {
            DrawCircle(shot.ImpactPosition, shot.Radius, 1.5f);
            Debug.Log($"ROCK DEBUG EXPLODE | session {SessionId} | center {shot.ImpactPosition:F4} | radius {shot.Radius} | damage snapshot {shot.Damage} | count {ExplosionCount} | NO DAMAGE", this);
            DebugExploded?.Invoke(shot);
        }
        finally { RetireRock(); }
    }

    public override void Cancel()
    {
        if (rockConsumed) return;
        rockConsumed = true;
        RetireRock();
    }
    private void RetireRock()
    {
        if (combat != null) combat.UnregisterProjectile(this);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
    protected override void OnDisable()
    {
        rockConsumed = true;
        if (combat != null) combat.UnregisterProjectile(this);
    }
    private void OnDrawGizmos()
    {
        if (!launched || rockConsumed) return;
        Gizmos.color = Color.yellow;
        Vector3 previous = shot.ImpactPosition + Vector3.right * shot.Radius;
        for (int i = 1; i <= 96; i++)
        {
            float angle = i * Mathf.PI * 2f / 96;
            Vector3 point = shot.ImpactPosition + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * shot.Radius;
            Gizmos.DrawLine(previous, point); previous = point;
        }
    }
    private static void DrawCircle(Vector3 center, float radius, float duration)
    {
        Vector3 previous = center + Vector3.right * radius;
        for (int i = 1; i <= 96; i++)
        {
            float angle = i * Mathf.PI * 2f / 96;
            Vector3 point = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            Debug.DrawLine(previous, point, Color.yellow, duration, false); previous = point;
        }
    }
    private static bool Positive(float value) => value > 0f && Finite(value);
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
