using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class ProjectileTest : MonoBehaviour
{
    public CombatResolver resolver;
    public EnemySpawnService spawnService;
    public SplineContainer path;
    public TowerData towerData;
    public Projectile prefab;
    [TextArea(5, 30)] public string report = "Not run";
    public bool Running { get; private set; }
    private int checks, failures;
    private Tower testTower;
    private readonly Dictionary<EnemyIdentity, int> deaths = new Dictionary<EnemyIdentity, int>();
    private void Resolved(EnemyResolved result)
    {
        if (result.State != EnemyState.Dead) return;
        deaths.TryGetValue(result.Identity, out int count); deaths[result.Identity] = count + 1;
    }
    private void Check(bool ok, string description)
    { checks++; if (!ok) failures++; report += "\n" + (ok ? "PASS " : "FAIL ") + description; }
    private EnemyHandle Spawn()
    {
        if (!spawnService.TrySpawn(resolver.SessionId, 1000, "normal", path, out var handle))
            throw new System.InvalidOperationException("Spawn failed");
        handle.Target.GetComponent<SplineAnimate>().enabled = false;
        handle.Target.transform.position = new Vector3(0, 0, 0);
        return handle;
    }
    private Projectile Fire(EnemyHandle target, int damage = 16, float speed = 8f, float lifetime = 5f)
    {
        var projectile = Instantiate(prefab, target.Position + Vector3.left * 2f, Quaternion.identity);
        if (!projectile.Initialize(new ShotContext(target, resolver.SessionId, damage, speed, lifetime), resolver))
            throw new System.InvalidOperationException("Projectile initialization failed");
        return projectile;
    }
    private int HP(EnemyHandle handle) => handle.Target.GetComponent<EnemyHealth>().CurrentHP;
    [ContextMenu("Session 10/Run Projectile Checks")]
    public void RunVerification()
    {
        if (!Application.isPlaying || Running) return;
        Running = true; StartCoroutine(Verify());
    }
    private IEnumerator Verify()
    {
        report = "Session 10 verification"; checks = failures = 0; deaths.Clear();
        spawnService.Resolved += Resolved;
        try
        {
            Time.timeScale = 1;
            spawnService.CleanupSession(resolver.SessionId);
            var enemy = Spawn();
            var hpSequence = new List<int> { HP(enemy) };
            for (int i = 0; i < 4; i++)
            {
                var p = Fire(enemy);
                Check(HP(enemy) == Mathf.Max(0, 60 - 16 * i), "Arrow " + (i + 1) + ": HP unchanged at launch");
                yield return new WaitForSeconds(0.4f);
                hpSequence.Add(HP(enemy));
                Check(p == null && HP(enemy) == Mathf.Max(0, 60 - 16 * (i + 1)), "Arrow " + (i + 1) + ": one 16-damage hit then destroyed");
            }
            report += "\nHP: " + string.Join(" -> ", hpSequence);
            Check(deaths[enemy.Identity] == 1, "Four hits emit one Dead result");
            enemy = Spawn(); Fire(enemy, 40); Fire(enemy, 40);
            yield return new WaitForSeconds(0.4f);
            Check(HP(enemy) == 0 && deaths[enemy.Identity] == 1, "Two simultaneous lethal arrows: one Dead");
            enemy = Spawn(); var other = Spawn();
            var slow = Fire(enemy, 16, 0.1f);
            enemy.Target.RequestHit(60);
            yield return new WaitForSeconds(0.1f);
            Check(slow == null && HP(other) == 60, "Dead target cancels arrow, never retargets");
            enemy = Spawn(); var orphan = Fire(enemy, 16, 0.1f);
            Destroy(enemy.Target.gameObject);
            yield return new WaitForSeconds(0.1f);
            Check(orphan == null, "Destroyed target cancels arrow safely");
            enemy = Spawn(); var expiredHandle = Fire(enemy, 16, 0.1f);
            var oldId = enemy.Identity;
            enemy.Target.BindCombat(resolver, new EnemyIdentity(resolver.SessionId, oldId.SpawnId + 10000));
            yield return new WaitForSeconds(0.1f);
            Check(expiredHandle == null && HP(enemy) == 60, "Changed identity invalidates in-flight handle");
            enemy.Target.BindCombat(resolver, oldId);
            var paused = Fire(enemy, 16, 4f, 1f);
            Vector3 position = paused.transform.position; float remaining = paused.RemainingLifetime;
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(2.5f);
            Check(paused != null && paused.transform.position == position && paused.RemainingLifetime == remaining && HP(enemy) == 60,
                "Pause 2.5 seconds freezes movement, lifetime and hit");
            Time.timeScale = 1;
            yield return new WaitForSeconds(0.7f);
            Check(paused == null && HP(enemy) == 44, "Resume continues flight and hits once");
            var timeout = Fire(enemy, 16, 0.1f, 0.1f);
            yield return new WaitForSeconds(0.2f);
            Check(timeout == null && HP(enemy) == 44, "Lifetime expiration destroys without hit");

            spawnService.CleanupSession(resolver.SessionId);
            enemy = Spawn();
            testTower = Instantiate(towerData.prefab, new Vector3(-2, 0, 0), Quaternion.identity).GetComponent<Tower>();
            testTower.Initialize(towerData, 1, resolver.Registry, resolver, out _);
            yield return new WaitForSeconds(0.2f);
            Check(resolver.ProjectileCount == 0, "Unactivated tower creates no arrows");
            testTower.ActivateTargeting();
            float deadline = Time.time + 2f;
            while (testTower.ShotCount == 0 && Time.time < deadline) yield return null;
            testTower.Suspend();
            Check(resolver.ProjectileCount == 1 && HP(enemy) == 60, "Tower launches one real arrow from Muzzle before damage");
            testTower.Initialize(towerData, 2, resolver.Registry, resolver, out _);
            yield return new WaitForSeconds(0.5f);
            Check(HP(enemy) == 44, "Level change after launch preserves original damage 16");
            int shots = testTower.ShotCount;
            yield return new WaitForSeconds(0.8f);
            Check(testTower.ShotCount == shots && resolver.ProjectileCount == 0, "Suspend prevents new projectiles");
            testTower.ActivateTargeting();
            deadline = Time.time + 2f;
            while (testTower.ShotCount == shots && Time.time < deadline) yield return null;
            Destroy(testTower.gameObject);
            yield return new WaitForSeconds(0.5f);
            Check(HP(enemy) == 16, "Destroy tower after launch: independent arrow still hits for level-II damage 28");

            var clean1 = Fire(enemy, 16, 0.1f); var targetGone = Spawn(); var clean2 = Fire(targetGone, 16, 0.1f);
            Time.timeScale = 0;
            Destroy(targetGone.Target.gameObject);
            spawnService.CleanupSession(resolver.SessionId);
            yield return null;
            Check(clean1 == null && clean2 == null && resolver.ProjectileCount == 0,
                "Session cleanup destroys all arrows including missing-target arrows while paused");
            Time.timeScale = 1;

            // Isolated resolver proves old-session hits are ignored without disturbing the sandbox runner.
            enemy = Spawn(); oldId = enemy.Identity;
            var temporary = new GameObject("OldSessionTest");
            var isolated = temporary.AddComponent<CombatResolver>(); isolated.BeginSession(); isolated.RegisterEnemy(enemy.Target);
            var previous = new EnemyHandle(enemy.Target);
            var oldArrow = Instantiate(prefab, previous.Position + Vector3.left * 2, Quaternion.identity);
            oldArrow.Initialize(new ShotContext(previous, isolated.SessionId, 16, 1f, 5f), isolated);
            isolated.RequestHit(new HitRequest(previous.Target, previous.Identity, 60));
            isolated.BeginSession(); isolated.RegisterEnemy(enemy.Target);
            isolated.RequestHit(new HitRequest(previous.Target, previous.Identity, 60));
            yield return null; yield return null;
            Check(oldArrow == null && HP(enemy) == 60, "New session destroys old arrows and rejects queued/late old-identity hits");
            Destroy(temporary); enemy.Target.BindCombat(resolver, oldId);
        }
        finally
        {
            Time.timeScale = 1;
            if (testTower != null) Destroy(testTower.gameObject);
            spawnService.CleanupSession(resolver.SessionId);
            spawnService.Resolved -= Resolved;
            Running = false;
            report += $"\nTOTAL {checks - failures}/{checks} passed";
            Debug.Log(report, this);
        }
    }
    private void OnDisable()
    {
        if (!Running) return;
        Time.timeScale = 1;
        StopAllCoroutines();
        if (testTower != null) Destroy(testTower.gameObject);
        spawnService.CleanupSession(resolver.SessionId);
        spawnService.Resolved -= Resolved;
        Running = false;
    }
}
