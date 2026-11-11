using UnityEngine;

[DisallowMultipleComponent]
public class Tower : MonoBehaviour
{
    [SerializeField] private TowerData data;
    [SerializeField] private Transform muzzle;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Animator animator;
    [Header("Runtime (per instance)")]
    [SerializeField] private int level;
    [SerializeField] private bool initialized;
    [SerializeField] private bool activeCombat;
    [SerializeField] private float remainingCooldown;
    [SerializeField] private int previewShotCount;

    private TowerLevelStats stats;
    private EnemyRegistry registry;
    private CombatResolver resolver;
    private int sessionId;
    private bool previewOnly;
    private bool automaticTargeting;
    private readonly TowerTargeting targeting = new TowerTargeting();
    private int shotId;
    public EnemyHandle CurrentTarget => targeting.Current;
    public int ShotCount => shotId;
    public readonly struct ShotRecord
    {
        public readonly int ShotId, TowerId, Frame;
        public readonly EnemyHandle Target;
        public readonly float GameTime;
        public readonly Vector3 Origin;
        public ShotRecord(Tower tower, EnemyHandle target)
        {
            ShotId = tower.shotId; TowerId = tower.GetInstanceID(); Frame = Time.frameCount;
            Target = target; GameTime = Time.time; Origin = tower.muzzle.position;
        }
    }
    // Observation only: Tower itself creates exactly one projectile before this event.
    public event System.Action<ShotRecord> ShotFired;
    private static readonly int PreviewShot = Animator.StringToHash("PreviewShot");

    public TowerData Data => data;
    public Transform Muzzle => muzzle;
    public bool IsActive => initialized && activeCombat && isActiveAndEnabled &&
        resolver != null && resolver.SessionId == sessionId;

    // Nested here deliberately: no new Shared contract is imposed on B.
    public readonly struct RuntimeSnapshot
    {
        public readonly int Level, Damage, Cost, PreviewShots;
        public readonly float Cooldown, Range, ProjectileSpeed, RemainingCooldown, Radius;
        public readonly bool Initialized, Active;
        public RuntimeSnapshot(Tower tower)
        {
            Level = tower.level;
            Damage = tower.stats.damage;
            Cost = tower.stats.cost;
            PreviewShots = tower.previewShotCount;
            Cooldown = tower.stats.cooldown;
            Range = tower.stats.range;
            ProjectileSpeed = tower.stats.projectileSpeed;
            Radius = tower.stats.radius;
            RemainingCooldown = tower.remainingCooldown;
            Initialized = tower.initialized;
            Active = tower.IsActive;
        }
        public override string ToString() => $"Level {Level} | damage {Damage} | cooldown {Cooldown} | " +
            $"range {Range} | speed {ProjectileSpeed} | cost {Cost} | active {Active} | " +
            $"remaining {RemainingCooldown:F3} | preview shots {PreviewShots}";
    }

    public RuntimeSnapshot ReadSnapshot() => new RuntimeSnapshot(this);

    internal int SessionId => sessionId;
    internal bool HasCurrentSession => initialized && resolver != null && resolver.SessionId == sessionId;

    // Also used on the prefab before Factory instantiates anything.
    public bool TryValidateCombatLevel(TowerData definition, int requestedLevel, out TowerLevelStats selected, out string error)
    {
        selected = default;
        error = "Missing tower data.";
        if (definition == null || !definition.TryValidate(out error) ||
            !definition.TryGetLevel(requestedLevel, out selected, out error)) return false;
        if (!enabled || !gameObject.activeSelf || muzzle == null || !muzzle.IsChildOf(transform) ||
            body == null || !body.transform.IsChildOf(transform) || selected.bodySprite == null || selected.damage <= 0)
        { error = "Tower requires enabled prefab, child Muzzle/body, sprite and positive damage."; return false; }
        var projectile = selected.projectilePrefab == null ? null : selected.projectilePrefab.GetComponent<Projectile>();
        if (projectile == null) { error = "Missing Projectile prefab."; return false; }
        if (!projectile.TryValidate(out error)) return false;
        if (projectile is Projectile_Rock && (selected.radius <= 0f ||
            (selected.range + Vector3.Distance(transform.position, muzzle.position)) / selected.projectileSpeed >= projectile.Lifetime))
        { error = "Rock requires positive radius and lifetime exceeding maximum range flight from Muzzle."; return false; }
        return true;
    }

    public bool TryApplyLevel(int nextLevel, out string error)
    {
        error = "Upgrade requires a current instance and the next level.";
        if (!HasCurrentSession || nextLevel != level + 1 ||
            !stats.TryValidate(out _) || !TryValidateCombatLevel(data, nextLevel, out var next, out error)) return false;
        float nextRemaining = Mathf.Clamp01(remainingCooldown / stats.cooldown) * next.cooldown;
        // Commit only after every check; active/preview state and existing ShotContexts stay unchanged.
        stats = next;
        level = nextLevel;
        remainingCooldown = nextRemaining;
        body.sprite = next.bodySprite;
        targeting.Reset();
        error = string.Empty;
        return true;
    }

    private void Awake()
    {
        if (body == null)
            body = GetComponentInChildren<SpriteRenderer>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public bool Initialize(TowerData definition, int initialLevel, EnemyRegistry enemyRegistry,
        CombatResolver combatResolver, out string error)
    {
        Suspend();
        initialized = false;
        if (!Application.isPlaying || definition == null || enemyRegistry == null || combatResolver == null)
        {
            error = $"{name}: Initialize requires Play Mode, data, registry and resolver.";
            return false;
        }
        if (!definition.TryValidate(out error) || !definition.TryGetLevel(initialLevel, out var selected, out error))
            return false;
        if (muzzle == null || !muzzle.IsChildOf(transform) || body == null ||
            combatResolver.SessionId <= 0 || !ReferenceEquals(enemyRegistry, combatResolver.Registry))
        {
            error = $"{name}: missing child Muzzle/body or mismatched/unstarted combat session.";
            return false;
        }
        data = definition;
        stats = selected;
        level = initialLevel;
        registry = enemyRegistry;
        resolver = combatResolver;
        sessionId = resolver.SessionId;
        remainingCooldown = 0f;
        previewShotCount = 0;
        previewOnly = false;
        automaticTargeting = false;
        targeting.Reset();
        if (stats.bodySprite != null)
            body.sprite = stats.bodySprite;
        initialized = true;
        error = string.Empty;
        return true;
    }

    public bool TryValidateForActivation(out string error, bool preview = false)
    {
        if (!initialized || !isActiveAndEnabled || resolver == null || registry == null ||
            resolver.SessionId != sessionId || muzzle == null)
            error = $"{name}: tower is not initialized in the current session, enabled, or has no Muzzle.";
        else if (!preview && (stats.projectilePrefab == null || stats.projectilePrefab.GetComponent<Projectile>() == null))
            error = $"{name}: projectilePrefab must have a Projectile component.";
        else if (!preview && !stats.projectilePrefab.GetComponent<Projectile>().TryValidate(out error))
            return false;
        else
            error = string.Empty;
        return error.Length == 0;
    }

    // preview=true is an explicit sandbox mode: animation/log only, never damage.
    public bool Activate(bool preview = false)
    {
        if (!TryValidateForActivation(out _, preview))
            return false;
        previewOnly = preview;
        automaticTargeting = !preview;
        activeCombat = true;
        return true;
    }

    public void Suspend() => activeCombat = false;

    public bool ActivateTargeting(bool preview = false)
    {
        if (!Activate(preview)) return false;
        automaticTargeting = true;
        targeting.Reset();
        return true;
    }

    private void OnDisable() => Suspend();

    private void Update()
    {
        if (!IsActive || Time.timeScale <= 0f || Time.deltaTime <= 0f) return;
        remainingCooldown = Mathf.Max(0f, remainingCooldown - Time.deltaTime);
        if (!automaticTargeting) return;
        var target = targeting.Tick(registry, sessionId, transform.position, stats.range, Time.deltaTime);
        if (remainingCooldown > 0f || muzzle == null ||
            !TowerTargeting.IsValid(target, sessionId, transform.position, stats.range)) return;
        if (!previewOnly)
        {
            if (!TryValidateForActivation(out _)) { Suspend(); return; }
            var instance = Instantiate(stats.projectilePrefab, muzzle.position, Quaternion.identity);
            var projectile = instance.GetComponent<Projectile>();
            bool launched;
            if (projectile is Projectile_Rock rock)
            {
                // Exactly one position read at launch; rock never keeps a target reference.
                Vector3 impactPosition = target.Position;
                impactPosition.z = muzzle.position.z;
                launched = rock.Initialize(new RockShotContext(impactPosition, sessionId, stats.damage,
                    stats.radius, stats.projectileSpeed, projectile.Lifetime), resolver);
            }
            else
            {
                var context = new ShotContext(target, sessionId, stats.damage, stats.projectileSpeed, projectile.Lifetime);
                launched = projectile.Initialize(context, resolver);
            }
            if (!launched)
            { Destroy(instance); Suspend(); return; }
        }
        remainingCooldown = stats.cooldown;
        shotId++;
        var shot = new ShotRecord(this, target);
        if (animator != null) animator.SetTrigger(PreviewShot);
        if (previewOnly)
        {
            Debug.DrawLine(muzzle.position, target.Position, Color.yellow, 0.15f);
            Debug.Log($"SHOT {shot.ShotId} | tower {shot.TowerId} | target {target.Identity} | gameTime {shot.GameTime:F4}", this);
        }
        ShotFired?.Invoke(shot);
    }

    // Called only by the debug caller. Targeting and real projectile spawning are later sessions.
    public bool TryPreviewShot()
    {
        if (!IsActive || !previewOnly || automaticTargeting || Time.timeScale <= 0f || remainingCooldown > 0f)
            return false;
        remainingCooldown = stats.cooldown;
        previewShotCount++;
        if (animator != null)
            animator.SetTrigger(PreviewShot);
        if (previewShotCount <= 3)
            Debug.Log($"{name}: PREVIEW {previewShotCount} at {muzzle.position}; no projectile or damage.", this);
        return true;
    }

    public bool ContainsPosition(Vector3 position)
    {
        Vector2 offset = position - transform.position;
        return initialized && offset.sqrMagnitude <= stats.range * stats.range;
    }

    private void OnDrawGizmos()
    {
        float radius = initialized ? stats.range : 0f;
        if (!initialized && data != null && data.TryGetLevel(1, out var initial, out _))
            radius = initial.range;
        if (radius <= 0f)
            return;
        Gizmos.color = IsActive ? Color.yellow : Color.cyan;
        Vector3 center = transform.position;
        Vector3 previous = center + Vector3.right * radius;
        for (int i = 1; i <= 96; i++)
        {
            float angle = i * Mathf.PI * 2f / 96;
            Vector3 point = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            Gizmos.DrawLine(previous, point);
            previous = point;
        }
        if (muzzle != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(muzzle.position, 0.06f);
        }
    }
}
