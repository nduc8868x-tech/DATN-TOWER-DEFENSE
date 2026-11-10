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
    private static readonly int PreviewShot = Animator.StringToHash("PreviewShot");

    public TowerData Data => data;
    public Transform Muzzle => muzzle;
    public bool IsActive => initialized && activeCombat && isActiveAndEnabled &&
        resolver != null && resolver.SessionId == sessionId;

    // Nested here deliberately: no new Shared contract is imposed on B.
    public readonly struct RuntimeSnapshot
    {
        public readonly int Level, Damage, Cost, PreviewShots;
        public readonly float Cooldown, Range, ProjectileSpeed, RemainingCooldown;
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
            RemainingCooldown = tower.remainingCooldown;
            Initialized = tower.initialized;
            Active = tower.IsActive;
        }
        public override string ToString() => $"Level {Level} | damage {Damage} | cooldown {Cooldown} | " +
            $"range {Range} | speed {ProjectileSpeed} | cost {Cost} | active {Active} | " +
            $"remaining {RemainingCooldown:F3} | preview shots {PreviewShots}";
    }

    public RuntimeSnapshot ReadSnapshot() => new RuntimeSnapshot(this);

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
        else if (!preview && stats.projectilePrefab == null)
            error = $"{name}: projectilePrefab is missing. Session 8 supports explicit preview only.";
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
        activeCombat = true;
        return true;
    }

    public void Suspend() => activeCombat = false;

    private void OnDisable() => Suspend();

    private void Update()
    {
        if (IsActive)
            remainingCooldown = Mathf.Max(0f, remainingCooldown - Time.deltaTime);
    }

    // Called only by the debug caller. Targeting and real projectile spawning are later sessions.
    public bool TryPreviewShot()
    {
        if (!IsActive || !previewOnly || remainingCooldown > 0f)
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
