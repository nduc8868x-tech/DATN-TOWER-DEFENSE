using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

// Sandbox contract example only. B owns money, build slots and refund policy.
public sealed class FakeBuildCaller : MonoBehaviour
{
    public TowerFactory factory;
    public EnemySpawnService spawnService;
    public SplineContainer path;
    public Vector3 buildPosition = new Vector3(-2, 0, 0);
    [TextArea(8, 40)] public string report = "Not run";
    public bool Running { get; private set; }
    private TowerHandle current;
    private readonly List<TowerHandle> fixtures = new List<TowerHandle>();
    private int checks, failures;

    [ContextMenu("Session 11/1 Fake success - Create and Activate")]
    public void FakeSuccess()
    {
        if (!Application.isPlaying || Running) return;
        current?.Dispose();
        if (!factory.TryCreate("arrow", 1, buildPosition, out current)) return;
        current.ReadSnapshot(out var snapshot);
        Debug.Log($"Fake transaction SUCCESS; before Activate: active={snapshot.Active}, shots={snapshot.ShotCount}", this);
        current.Activate();
    }
    [ContextMenu("Session 11/2 Fake failure - Create and Rollback")]
    public void FakeFailure()
    {
        if (!Application.isPlaying || Running) return;
        if (!factory.TryCreate("arrow", 1, buildPosition, out var pending)) return;
        Debug.Log("Fake transaction FAILED; disposing pending tower, no money changed.", this);
        pending.Dispose();
    }
    [ContextMenu("Session 11/3 Upgrade to II")]
    public void Upgrade() { if (!Running) Debug.Log("Upgrade II: " + (current?.TryApplyLevel(2) ?? false), this); }
    [ContextMenu("Session 11/4 Invalid upgrade")]
    public void InvalidUpgrade() { if (!Running) Debug.Log("Upgrade 99: " + (current?.TryApplyLevel(99) ?? false), this); }
    [ContextMenu("Session 11/5 Sell - Suspend and Dispose")]
    public void Sell() { if (!Running) { current?.Suspend(); current?.Dispose(); } }
    [ContextMenu("Session 11/6 Dispose again")]
    public void DisposeAgain() { if (!Running) current?.Dispose(); }
    [ContextMenu("Session 11/7 Upgrade after Dispose")]
    public void UpgradeDisposed() { if (!Running) { current?.Dispose(); Upgrade(); } }
    [ContextMenu("Session 11/Run All Checks")]
    public void RunVerification()
    {
        if (!Application.isPlaying || Running || factory == null || spawnService == null || path == null) return;
        current?.Dispose(); Running = true; StartCoroutine(Verify());
    }
    private TowerHandle Create()
    {
        if (!factory.TryCreate("arrow", 1, buildPosition, out var handle))
            throw new System.InvalidOperationException("Fixture creation failed");
        fixtures.Add(handle); return handle;
    }
    private void Check(bool ok, string message)
    { checks++; if (!ok) failures++; report += "\n" + (ok ? "PASS " : "FAIL ") + message; }
    private static bool Same(TowerSnapshot a, TowerSnapshot b) => a.Level == b.Level && a.Damage == b.Damage &&
        a.Cooldown == b.Cooldown && a.Range == b.Range && a.ProjectileSpeed == b.ProjectileSpeed &&
        a.RemainingCooldown == b.RemainingCooldown && a.Active == b.Active;
    private IEnumerator Verify()
    {
        report = "Session 11 verification"; checks = failures = 0;
        var resolver = factory.resolver;
        try
        {
            Time.timeScale = 1;
            spawnService.CleanupSession(resolver.SessionId);
            if (!spawnService.TrySpawn(resolver.SessionId, 1100, "normal", path, out var enemy))
                throw new System.InvalidOperationException("Enemy fixture failed");
            enemy.Target.GetComponent<SplineAnimate>().enabled = false;
            enemy.Target.transform.position = Vector3.zero;
            var health = enemy.Target.GetComponent<EnemyHealth>();
            int baseline = FindObjectsByType<Tower>(FindObjectsSortMode.None).Length;
            Check(!factory.TryCreate("missing", 1, buildPosition, out _) &&
                !factory.TryCreate("arrow", 0, buildPosition, out _) &&
                !factory.TryCreate("arrow", 99, buildPosition, out _) &&
                !factory.TryCreate("arrow", 1, new Vector3(float.NaN, 0, 0), out _) &&
                FindObjectsByType<Tower>(FindObjectsSortMode.None).Length == baseline,
                "Bad ID/level/position return false without objects");
            var savedResolver = factory.resolver;
            factory.resolver = null;
            bool missingDependency = !factory.TryCreate("arrow", 1, buildPosition, out _);
            factory.resolver = savedResolver;
            Check(missingDependency, "Missing resolver rejected before Instantiate");
            var rollback = Create();
            rollback.ReadSnapshot(out var pending);
            Debug.Log("Fake transaction FAILED: rollback pending inactive tower.", this);
            var rollbackObject = rollback.Instance;
            rollback.Dispose(); rollback.Dispose();
            yield return null;
            Check(!pending.Active && rollbackObject == null &&
                FindObjectsByType<Tower>(FindObjectsSortMode.None).Length == baseline,
                "Failed transaction rollback destroys tower, repeated Dispose safe");
            Check(!rollback.TryApplyLevel(2) && !rollback.Activate() && !rollback.Suspend() && !rollback.ReadSnapshot(out _),
                "Disposed handle rejects all operations");
            var first = Create(); var second = Create();
            yield return new WaitForSeconds(0.25f);
            first.ReadSnapshot(out var initial); second.ReadSnapshot(out var secondBefore);
            Check(!initial.Active && initial.ShotCount == 0 && resolver.ProjectileCount == 0 && health.CurrentHP == 60,
                "Create stays inactive with target in range");
            Check(initial.TowerId == "arrow" && initial.Cost == 100 && initial.NextLevelCost == 100 && initial.CanUpgrade,
                "Snapshot exposes ID and shared-data prices by value");
            Debug.Log("Fake transaction SUCCESS: Activate after commit.", this);
            Check(first.Activate(), "Activate succeeds");
            float deadline = Time.time + 2;
            while (first.Instance.ShotCount == 0 && Time.time < deadline) yield return null;
            Check(first.Instance.ShotCount == 1 && resolver.ProjectileCount == 1 && health.CurrentHP == 60,
                "Activated tower launches real arrow before damage");
            first.ReadSnapshot(out var before);
            Check(first.TryApplyLevel(2), "Upgrade I to II while old arrow is flying");
            first.ReadSnapshot(out var after);
            Check(after.Active && after.Damage == 28 && after.Cooldown == 0.65f && after.Range == 3 &&
                Mathf.Abs(after.RemainingCooldown - Mathf.Clamp01(before.RemainingCooldown / before.Cooldown) * after.Cooldown) < 0.00001f,
                $"Active upgrade preserves ratio: {before.RemainingCooldown:F4}/{before.Cooldown} -> {after.RemainingCooldown:F4}/{after.Cooldown}; damage=28 range=3");
            first.Suspend();
            yield return new WaitForSeconds(0.5f);
            Check(health.CurrentHP == 44, "Pre-upgrade arrow retains 16 damage (60 -> 44)");
            second.ReadSnapshot(out var secondAfter);
            Check(Same(secondBefore, secondAfter), "Upgrading first tower leaves second unchanged");
            factory.catalog.TryGet("arrow", out var definition);
            definition.TryGetLevel(1, out var levelOne, out _);
            Check(levelOne.damage == 16 && levelOne.cooldown == 0.8f, "Shared ScriptableObject unchanged");
            first.ReadSnapshot(out before);
            bool rejected = !first.TryApplyLevel(1) && !first.TryApplyLevel(2) && !first.TryApplyLevel(99);
            first.ReadSnapshot(out after);
            Check(rejected && Same(before, after), "Invalid upgrades preserve level/stats/cooldown/state atomically");
            first.Activate();
            deadline = Time.time + 2;
            while (first.Instance.ShotCount < 2 && Time.time < deadline) yield return null;
            var firstObject = first.Instance;
            first.Suspend(); first.Dispose(); first.Dispose();
            yield return new WaitForSeconds(0.5f);
            Check(firstObject == null && health.CurrentHP == 16, "Post-upgrade arrow uses 28 damage and survives tower disposal (44 -> 16)");
            yield return new WaitForSeconds(0.8f);
            Check(health.CurrentHP == 16 && resolver.ProjectileCount == 0, "Suspend/Dispose leaves no invisible firing tower");

            // Explicit preview mode lets this timing test run without killing the stationary fixture.
            second.Instance.Activate(true); second.Instance.TryPreviewShot();
            while (second.Instance.ReadSnapshot().RemainingCooldown > 0.4f) yield return null;
            second.Suspend(); second.ReadSnapshot(out before);
            bool upgraded = second.TryApplyLevel(2);
            second.ReadSnapshot(out after);
            Check(upgraded && !after.Active && after.RemainingCooldown > 0 &&
                Mathf.Abs(after.RemainingCooldown - before.RemainingCooldown / 0.8f * 0.65f) < 0.00001f,
                $"Mid-cooldown suspended upgrade: {before.RemainingCooldown:F6}/0.8 -> {after.RemainingCooldown:F6}/0.65 (ideal 0.4 -> 0.325)");
            yield return new WaitForSeconds(0.3f);
            second.ReadSnapshot(out var stillSuspended);
            Check(Same(after, stillSuspended), "Upgrade preserves Suspend and frozen cooldown");
            second.Dispose();
            var externallyDestroyed = Create();
            Destroy(externallyDestroyed.Instance.gameObject); yield return null;
            Check(!externallyDestroyed.ReadSnapshot(out _) && !externallyDestroyed.TryApplyLevel(2) &&
                !externallyDestroyed.Activate() && !externallyDestroyed.Suspend(), "Externally destroyed instance safely rejected");
            externallyDestroyed.Dispose();

            spawnService.CleanupSession(resolver.SessionId);
            if (!spawnService.TrySpawn(resolver.SessionId, 1101, "normal", path, out enemy))
                throw new System.InvalidOperationException("Timing enemy fixture failed");
            enemy.Target.GetComponent<SplineAnimate>().enabled = false;
            enemy.Target.transform.position = Vector3.zero;
            var timing = Create();
            var shots = new List<float>();
            timing.Instance.ShotFired += shot => shots.Add(shot.GameTime);
            timing.Activate();
            deadline = Time.time + 2;
            while (shots.Count == 0 && Time.time < deadline) yield return null;
            while (timing.Instance.ReadSnapshot().RemainingCooldown > 0.4f) yield return null;
            timing.ReadSnapshot(out before);
            float upgradeTime = Time.time;
            timing.TryApplyLevel(2);
            timing.ReadSnapshot(out after);
            deadline = Time.time + 3;
            while (shots.Count < 3 && Time.time < deadline) yield return null;
            timing.Suspend();
            float tolerance = Mathf.Max(0.06f, Time.deltaTime * 2);
            Check(shots.Count == 3 && Mathf.Abs(shots[1] - upgradeTime - after.RemainingCooldown) <= tolerance &&
                Mathf.Abs(shots[2] - shots[1] - after.Cooldown) <= tolerance,
                shots.Count == 3 ? $"Real shots after mid-cycle upgrade: wait={shots[1] - upgradeTime:F6}s expected={after.RemainingCooldown:F6}s; next interval={shots[2] - shots[1]:F6}s expected=0.65s" : "Timing test did not collect 3 shots");
            timing.Dispose();
        }
        finally
        {
            Cleanup(); Running = false;
            report += $"\nRESULT: {checks - failures}/{checks} passed; failures={failures}";
            Debug.Log(report, this);
        }
    }
    private void Cleanup()
    {
        Time.timeScale = 1;
        foreach (var handle in fixtures) handle.Dispose();
        fixtures.Clear();
        if (factory != null && factory.resolver != null && spawnService != null)
            spawnService.CleanupSession(factory.resolver.SessionId);
    }
    private void OnDisable()
    {
        current?.Dispose();
        if (Running) { StopAllCoroutines(); Cleanup(); Running = false; }
    }
}
