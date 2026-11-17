using UnityEngine;

// Uses the same resolver registry/cleanup as arrows, with a fixed-point flight strategy.
[DisallowMultipleComponent]
public sealed class Projectile_Rock : Projectile
{
    [SerializeField, Min(0f)] private float arcHeight = 0.75f;
    [SerializeField] private Sprite dust;
    [SerializeField] private Sprite[] fragments;
    [SerializeField] private Sprite[] impactFrames;
    [SerializeField, Min(0.01f)] private float impactFrameDuration = 0.09f;

    private RockShotContext shot;
    private Vector3 launchPosition;
    private float flightDuration;
    private float flightElapsed;
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
        launchPosition = transform.position;
        flightDuration = Vector3.Distance(launchPosition, shot.ImpactPosition) / shot.Speed;
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
        // Preserve the existing speed-based flight duration while drawing a parabolic arc.
        flightElapsed = Mathf.Min(flightDuration, flightElapsed + Mathf.Min(dt, RemainingLifetime));
        RemainingLifetime = Mathf.Max(0f, RemainingLifetime - dt);
        if (flightElapsed >= flightDuration)
        {
            transform.position = shot.ImpactPosition;
            Explode();
            return;
        }

        float t = flightElapsed / flightDuration;
        transform.position = Vector3.Lerp(launchPosition, shot.ImpactPosition, t)
            + Vector3.up * (4f * arcHeight * t * (1f - t));
        if (RemainingLifetime <= 0f) Cancel();
    }

    private void Explode()
    {
        if (rockConsumed || combat == null || combat.SessionId != SessionId) return;
        rockConsumed = true; // Must precede visual/log/event, including reentrant callbacks.
        ExplosionCount++;
        var flyingStone = GetComponent<SpriteRenderer>();
        flyingStone.enabled = false;
        try
        {
            if (dust != null || (fragments != null && fragments.Length > 0) || (impactFrames != null && impactFrames.Length > 0))
                RockImpactEffect.Spawn(combat, shot.ImpactPosition, flyingStone, dust, fragments, impactFrames, impactFrameDuration);
            var alive = new System.Collections.Generic.List<EnemyHandle>();
            var hit = new System.Collections.Generic.HashSet<EnemyIdentity>();
            combat.Registry.CopyAliveTo(alive);
            foreach (var enemy in alive)
            {
                Vector2 offset = enemy.Position - shot.ImpactPosition;
                if (enemy.IsAlive && enemy.Identity.SessionId == SessionId &&
                    offset.sqrMagnitude <= shot.Radius * shot.Radius && hit.Add(enemy.Identity))
                    combat.RequestHit(new HitRequest(enemy.Target, enemy.Identity, shot.Damage));
            }
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
    private static bool Positive(float value) => value > 0f && Finite(value);
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
