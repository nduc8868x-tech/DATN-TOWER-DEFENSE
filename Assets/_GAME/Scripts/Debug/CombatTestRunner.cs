using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CombatResolver))]
public class CombatTestRunner : MonoBehaviour
{
    [SerializeField] private Enemy[] enemies;
    [SerializeField] private Enemy testTarget;

    private CombatResolver resolver;

    private void Awake()
    {
        resolver = GetComponent<CombatResolver>();
    }

    private void Start()
    {
        resolver.BeginSession();

        if (enemies == null || enemies.Length == 0)
        {
            Debug.LogError("Hãy gán danh sách Enemies trên CombatTestRunner.", this);
            return;
        }

        foreach (Enemy enemy in enemies)
            resolver.RegisterEnemy(enemy);
    }

    private void LateUpdate()
    {
        resolver.ResolvePendingBatch();
    }

    [ContextMenu("Debug/Escape Then Hit 16")]
    private void EscapeThenHit()
    {
        QueueTest(true);
    }

    [ContextMenu("Debug/Hit 16 Then Escape")]
    private void HitThenEscape()
    {
        QueueTest(false);
    }

    private void QueueTest(bool escapeFirst)
    {
        if (!Application.isPlaying)
            return;

        if (testTarget == null || !testTarget.Identity.IsValid || !testTarget.IsAlive)
        {
            Debug.LogWarning("Test Target phải là quái đã đăng ký và còn sống.", this);
            return;
        }

        var escape = new EscapeRequest(testTarget, testTarget.Identity);
        var hit = new HitRequest(testTarget, testTarget.Identity, 16);

        if (escapeFirst)
        {
            resolver.RequestEscape(escape);
            resolver.RequestHit(hit);
        }
        else
        {
            resolver.RequestHit(hit);
            resolver.RequestEscape(escape);
        }
    }
    private bool HasLiveTestTarget()
{
    if (!Application.isPlaying)
        return false;

    if (resolver == null || testTarget == null ||
        !testTarget.isActiveAndEnabled || !testTarget.IsAlive ||
        !testTarget.Identity.IsValid)
    {
        Debug.LogWarning("Hãy chọn một Test Target đang sống và đã đăng ký.", this);
        return false;
    }

    return true;
}

[ContextMenu("Debug/01 - Duplicate Escape")]
private void TestDuplicateEscape()
{
    if (!HasLiveTestTarget())
        return;

    var request = new EscapeRequest(testTarget, testTarget.Identity);

    resolver.RequestEscape(request);
    resolver.RequestEscape(request);
}

[ContextMenu("Debug/02 - Hit From Callback")]
private void TestHitFromCallback()
{
    if (!HasLiveTestTarget())
        return;

    Enemy target = testTarget;
    EnemyIdentity identity = target.Identity;
    EnemyHealth health = target.GetComponent<EnemyHealth>();

    if (health == null || health.CurrentHP != 60)
    {
        Debug.LogWarning("Chạy ca thử này với quái có đúng 60 HP.", this);
        return;
    }

    void OnSecondHit(int hp, int maxHP)
    {
        health.HealthChanged -= OnSecondHit;
        Debug.Log($"Hit 2 | frame {Time.frameCount} | HP {hp}/{maxHP}", target);
    }

    void OnFirstHit(int hp, int maxHP)
    {
        health.HealthChanged -= OnFirstHit;
        Debug.Log($"Hit 1 | frame {Time.frameCount} | HP {hp}/{maxHP}", target);

        health.HealthChanged += OnSecondHit;
        resolver.RequestHit(new HitRequest(target, identity, 16));
    }

    health.HealthChanged += OnFirstHit;
    resolver.RequestHit(new HitRequest(target, identity, 16));
}

[ContextMenu("Debug/03 - Hit From Old Session")]
private void TestOldSession()
{
    if (!HasLiveTestTarget())
        return;

    EnemyIdentity oldIdentity = testTarget.Identity;

    // Yêu cầu này đang đợi khi lượt chơi mới bắt đầu.
    resolver.RequestHit(new HitRequest(testTarget, oldIdentity, 16));

    resolver.BeginSession();
    foreach (Enemy enemy in enemies)
        resolver.RegisterEnemy(enemy);

    Debug.Log($"Mã quái trước {oldIdentity}, sau {testTarget.Identity}.", testTarget);
}

[ContextMenu("Debug/04 - Removed Target")]
private void TestRemovedTarget()
{
    if (!HasLiveTestTarget())
        return;

    resolver.RequestHit(new HitRequest(testTarget, testTarget.Identity, 16));
    testTarget.gameObject.SetActive(false);
}
}