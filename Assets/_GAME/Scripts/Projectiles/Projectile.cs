using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class Projectile : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float lifetime = 5f;
    [SerializeField, Min(0f)] private float hitRadius = 0.05f;
    private ShotContext context;
    private CombatResolver resolver;
    private bool initialized, consumed;
    public float Lifetime => lifetime;
    public float RemainingLifetime { get; protected set; }
    public virtual int SessionId => context.SessionId;
    public int HitRequests { get; private set; }
    public ShotContext Context => context;

    public virtual bool TryValidate(out string error)
    {
        error = !gameObject.activeSelf || !enabled || !Positive(lifetime) ||
            hitRadius < 0f || float.IsNaN(hitRadius) || float.IsInfinity(hitRadius) ||
            GetComponent<SpriteRenderer>().sprite == null
            ? $"{name}: projectile requires active component, sprite, positive finite lifetime and finite hit radius." : string.Empty;
        return error.Length == 0;
    }
    private static bool Positive(float value) => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);

    public virtual bool Initialize(ShotContext shot, CombatResolver combatResolver)
    {
        if (initialized || consumed || !Application.isPlaying || !TryValidate(out _) ||
            combatResolver == null || shot.SessionId != combatResolver.SessionId ||
            !shot.Target.IsAlive || shot.Target.Identity.SessionId != shot.SessionId ||
            shot.Damage <= 0 || !Positive(shot.Speed) || !Positive(shot.Lifetime)) return false;
        context = shot; resolver = combatResolver;
        RemainingLifetime = shot.Lifetime;
        initialized = true;
        resolver.RegisterProjectile(this);
        return true;
    }

    protected virtual void Update()
    {
        if (!initialized || consumed) return;
        if (resolver == null || resolver.SessionId != context.SessionId ||
            !context.Target.IsAlive || context.Target.Identity.SessionId != context.SessionId)
        { Cancel(); return; }
        if (Time.timeScale <= 0f || Time.deltaTime <= 0f) return;
        RemainingLifetime -= Time.deltaTime;
        if (RemainingLifetime <= 0f) { Cancel(); return; }
        Vector3 destination = context.Target.Position;
        destination.z = transform.position.z; // Sorting depth must not change flight or hit distance.
        Vector3 direction = destination - transform.position;
        if (direction.sqrMagnitude > 0.000001f)
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        float step = context.Speed * Time.deltaTime;
        if (direction.sqrMagnitude <= Mathf.Max(step, hitRadius) * Mathf.Max(step, hitRadius))
        {
            consumed = true; // Guard before enqueue; only this path can send a hit.
            transform.position = destination;
            HitRequests++;
            resolver.RequestHit(new HitRequest(context.Target.Target, context.Target.Identity, context.Damage));
            Retire();
            return;
        }
        transform.position = Vector3.MoveTowards(transform.position, destination, step);
    }

    public virtual void Cancel() { consumed = true; Retire(); }
    private void Retire()
    {
        if (resolver != null) resolver.UnregisterProjectile(this);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
    protected virtual void OnDisable()
    {
        consumed = true;
        if (resolver != null) resolver.UnregisterProjectile(this);
    }
}
