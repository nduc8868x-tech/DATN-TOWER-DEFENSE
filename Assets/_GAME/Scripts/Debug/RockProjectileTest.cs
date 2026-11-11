using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

// Explicitly invoked sandbox checks. Never runs automatically in a gameplay scene.
public sealed class RockProjectileTest : MonoBehaviour
{
    public TowerFactory factory;
    public EnemySpawnService spawnService;
    public SplineContainer path;
    public Vector3 buildPosition = new Vector3(-2, 0, 0);
    [TextArea(8, 50)] public string report = "Not run";
    public bool Running { get; private set; }
    private readonly List<TowerHandle> towers = new List<TowerHandle>();
    private int checks, failures;
    private float originalTimeScale;
    private void Check(bool ok, string description)
    { checks++; if (!ok) failures++; report += "\n" + (ok ? "PASS " : "FAIL ") + description; }
    private EnemyHandle Spawn()
    {
        if (!spawnService.TrySpawn(factory.resolver.SessionId, 1300, "normal", path, out var enemy))
            throw new System.InvalidOperationException("Rock fixture spawn failed");
        enemy.Target.GetComponent<SplineAnimate>().enabled = false;
        enemy.Target.transform.position = Vector3.zero;
        return enemy;
    }
    private TowerHandle Create(int level)
    {
        if (!factory.TryCreate("rock", level, buildPosition, out var handle))
            throw new System.InvalidOperationException("Rock factory rejected level " + level);
        towers.Add(handle); return handle;
    }
    private Projectile_Rock Flying()
    {
        foreach (var projectile in FindObjectsByType<Projectile_Rock>(FindObjectsSortMode.None))
            if (projectile.SessionId == factory.resolver.SessionId) return projectile;
        throw new System.InvalidOperationException("No flying rock");
    }
    private IEnumerator WaitForShot(TowerHandle handle, int previous)
    {
        float deadline = Time.time + 4;
        while (handle.IsValid && handle.Instance.ShotCount <= previous && Time.time < deadline) yield return null;
        if (!handle.IsValid || handle.Instance.ShotCount <= previous)
            throw new System.InvalidOperationException("Timed out waiting for rock shot");
    }
    [ContextMenu("Session 13/Run All Rock Checks")]
    public void RunVerification()
    {
        if (!Application.isPlaying || Running || factory == null || factory.resolver == null || spawnService == null || path == null) return;
        Running = true; originalTimeScale = Time.timeScale;
        StartCoroutine(Verify());
    }
    private IEnumerator Verify()
    {
        checks = failures = 0; report = "Session 13 verification (isolated fixtures; run no other tests concurrently)";
        var resolver = factory.resolver;
        try
        {
            Time.timeScale = 1;
            spawnService.CleanupSession(resolver.SessionId);
            if (resolver.SessionId <= 0) resolver.BeginSession();
            var enemy = Spawn();
            var health = enemy.Target.GetComponent<EnemyHealth>();
            var first = Create(1); var second = Create(2);
            first.ReadSnapshot(out var one); second.ReadSnapshot(out var two);
            Check(one.TowerId == "rock" && one.Level == 1 && one.Damage == 30 && one.Cooldown == 2 && one.Radius == 1 &&
                one.Range == 3 && one.Cost == 140 && one.NextLevelCost == 140 && one.ProjectileSpeed == 4 && one.CanUpgrade,
                "Factory I / snapshot: rock, damage30 cooldown2 radius1 range3 speed4 cost140 next140");
            Check(two.Level == 2 && two.Damage == 50 && two.Cooldown == 1.8f && two.Radius == 1.25f &&
                two.Cost == 140 && !two.CanUpgrade && two.NextLevelCost == 0, "Factory II / snapshot: damage50 cooldown1.8 radius1.25 cost140 max level");
            second.Dispose();
            yield return new WaitForSeconds(0.3f);
            Check(first.Instance.ShotCount == 0 && resolver.ProjectileCount == 0 && !first.Instance.IsActive,
                "Create without Activate does not fire");
            var times = new List<float>();
            first.Instance.ShotFired += shot => times.Add(shot.GameTime);
            Check(first.Activate(), "Activate succeeds");
            yield return WaitForShot(first, 0);
            var old = Flying(); var original = old.RockContext;
            int exploded = 0; Vector3 observed = Vector3.zero; float observedRadius = 0;
            old.DebugExploded += shot =>
            {
                exploded++; observed = shot.ImpactPosition; observedRadius = shot.Radius;
                Check(old.Consumed && old.ExplosionCount == 1 && old.transform.position == shot.ImpactPosition,
                    "Consumed before callback; arrival exactly at impactPosition (no overshoot)");
                old.SendMessage("Explode"); // Reentrant observation must not produce a second explosion.
            };
            // Move every frame, all the way outside range; no destination updates are allowed.
            float moveUntil = Time.time + 1.05f;
            while (Time.time < moveUntil)
            {
                enemy.Target.transform.position += Vector3.up * (4f * Time.deltaTime);
                yield return null;
            }
            Check(old == null && exploded == 1 && observed == original.ImpactPosition && observedRadius == 1 && health.CurrentHP == 60,
                "Moving target: fixed center / radius1, one explosion, HP60 (visual circle still needs visual inspection)");
            Check(!first.Instance.ContainsPosition(enemy.Position), "Target moved outside range without cancelling old rock");
            enemy.Target.transform.position = Vector3.zero;
            yield return WaitForShot(first, 1);
            Check(times.Count >= 2 && Mathf.Abs(times[1] - times[0] - 2) <= Mathf.Max(0.06f, Time.deltaTime * 2),
                times.Count >= 2 ? $"Level I firing interval {times[1] - times[0]:F4}s expected2" : "Missing cooldown samples");
            first.Suspend(); var deathRock = Flying(); int deathExplosions = 0;
            deathRock.DebugExploded += shot => deathExplosions++;
            enemy.Target.RequestHit(999); // Explicit fixture kill, NOT explosion damage.
            yield return null; yield return null;
            Check(health.CurrentHP == 0 && deathRock != null, "Target dead after shot: flying rock is not cancelled");
            yield return new WaitForSeconds(1.1f);
            Check(deathRock == null && deathExplosions == 1, "Dead target: one explosion at saved point");

            enemy = Spawn(); health = enemy.Target.GetComponent<EnemyHealth>();
            first.Activate(); yield return WaitForShot(first, 2); first.Suspend();
            var disabledRock = Flying(); int disabledExplosions = 0;
            var disabledPoint = disabledRock.RockContext.ImpactPosition;
            disabledRock.DebugExploded += shot => { if (shot.ImpactPosition == disabledPoint) disabledExplosions++; };
            enemy.Target.gameObject.SetActive(false);
            yield return new WaitForSeconds(1.1f);
            Check(disabledRock == null && disabledExplosions == 1 && health.CurrentHP == 60,
                "Disabled alive target: fixed-point explosion still occurs, no damage");

            enemy = Spawn(); health = enemy.Target.GetComponent<EnemyHealth>();
            first.Activate(); yield return WaitForShot(first, 3); first.Suspend();
            var paused = Flying(); var position = paused.transform.position; float lifetime = paused.RemainingLifetime;
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(0.6f);
            Check(paused != null && paused.transform.position == position && paused.RemainingLifetime == lifetime,
                "Pause 0.6 real seconds freezes position and lifetime");
            Time.timeScale = 1;
            yield return new WaitForSeconds(1.1f);
            Check(paused == null && health.CurrentHP == 60, "Resume reaches destination without damage");

            first.Activate(); yield return WaitForShot(first, 4);
            var preUpgrade = Flying(); var saved = preUpgrade.RockContext;
            Check(first.TryApplyLevel(2) && preUpgrade.RockContext.Damage == 30 && preUpgrade.RockContext.Radius == 1 &&
                preUpgrade.RockContext.Speed == saved.Speed && preUpgrade.RockContext.Lifetime == saved.Lifetime &&
                preUpgrade.RockContext.ImpactPosition == saved.ImpactPosition, "Upgrade while flying preserves complete old snapshot");
            yield return WaitForShot(first, 5);
            var postUpgrade = Flying();
            Check(postUpgrade.RockContext.Damage == 50 && postUpgrade.RockContext.Radius == 1.25f,
                "New shot after upgrade snapshots damage50/radius1.25");
            int postExplosions = 0;
            postUpgrade.DebugExploded += shot => { if (shot.Radius == 1.25f) postExplosions++; };
            int beforeSecondLevelTwo = first.Instance.ShotCount;
            yield return WaitForShot(first, beforeSecondLevelTwo);
            Check(times.Count >= 2 && Mathf.Abs(times[times.Count - 1] - times[times.Count - 2] - 1.8f) <= Mathf.Max(0.06f, Time.deltaTime * 2),
                $"Level II firing interval {times[times.Count - 1] - times[times.Count - 2]:F4}s expected1.8");
            first.Suspend(); int shots = first.Instance.ShotCount;
            yield return new WaitForSeconds(1.1f);
            Check(first.Instance.ShotCount == shots && postExplosions == 1, "Suspend prevents new shots; radius1.25 rock completes");
            first.Activate(); yield return WaitForShot(first, shots);
            var afterDispose = Flying(); int disposedExplosions = 0;
            afterDispose.DebugExploded += shot => disposedExplosions++;
            first.Dispose();
            yield return new WaitForSeconds(1.1f);
            Check(afterDispose == null && disposedExplosions == 1, "Dispose tower does not destroy in-flight rock");

            factory.catalog.TryGet("rock", out var data);
            var prefab = data.prefab.GetComponent<Tower>();
            data.TryGetLevel(1, out var stats, out _);
            var rockPrefab = stats.projectilePrefab.GetComponent<Projectile_Rock>();
            var expires = Instantiate(rockPrefab, buildPosition, Quaternion.identity);
            int expiryEvents = 0; expires.DebugExploded += shot => expiryEvents++;
            Check(expires.Initialize(new RockShotContext(new Vector3(100, 0, 0), resolver.SessionId, 30, 1, 1, 0.1f), resolver), "Lifetime fixture initialized");
            yield return new WaitForSeconds(0.2f);
            Check(expires == null && expiryEvents == 0, "Lifetime expiry cancels without explosion callback");
            var clean = Instantiate(rockPrefab, buildPosition, Quaternion.identity); int cleanupEvents = 0;
            clean.Initialize(new RockShotContext(Vector3.zero, resolver.SessionId, 30, 1, 1, 3), resolver);
            clean.DebugExploded += shot => cleanupEvents++;
            factory.catalog.TryGet("arrow", out var arrowData); arrowData.TryGetLevel(1, out var arrowStats, out _);
            var arrow = Instantiate(arrowStats.projectilePrefab, buildPosition, Quaternion.identity).GetComponent<Projectile>();
            arrow.Initialize(new ShotContext(enemy, resolver.SessionId, 16, 0.1f, 5), resolver);
            int previousSession = resolver.SessionId;
            Time.timeScale = 0;
            spawnService.CleanupSession(previousSession); resolver.BeginSession();
            yield return null;
            Check(clean == null && arrow == null && resolver.ProjectileCount == 0 && cleanupEvents == 0 && resolver.SessionId != previousSession,
                "Paused cleanup/restart removes rocks AND arrows; no old explosion callback");
            Time.timeScale = 1; yield return new WaitForSeconds(0.8f);
            Check(cleanupEvents == 0, "No late callback in new session");
            enemy = Spawn(); health = enemy.Target.GetComponent<EnemyHealth>();
            if (!factory.TryCreate("arrow", 1, buildPosition, out var arrowTower)) throw new System.InvalidOperationException("Arrow factory failed");
            towers.Add(arrowTower); arrowTower.Activate(); yield return WaitForShot(arrowTower, 0); arrowTower.Suspend();
            yield return new WaitForSeconds(0.6f);
            Check(health.CurrentHP == 44, "Arrow regression: factory + shot still deals16 (60 -> 44)");
        }
        finally
        {
            Cleanup(); Running = false;
            report += $"\nRESULT {checks - failures}/{checks}; failures={failures}";
            Debug.Log(report, this);
        }
    }
    private void Cleanup()
    {
        Time.timeScale = originalTimeScale;
        foreach (var handle in towers) handle.Dispose(); towers.Clear();
        if (factory != null && factory.resolver != null && spawnService != null)
            spawnService.CleanupSession(factory.resolver.SessionId);
    }
    private void OnDisable()
    {
        if (!Running) return;
        StopAllCoroutines(); Cleanup(); Running = false;
    }
}
