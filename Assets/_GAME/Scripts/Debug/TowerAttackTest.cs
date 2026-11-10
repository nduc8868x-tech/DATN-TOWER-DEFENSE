using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class TowerAttackTest : MonoBehaviour
{
    public Tower towerA, towerB;
    public EnemySpawnService spawnService;
    public CombatResolver resolver;
    public SplineContainer path;
    [TextArea(5, 30)] public string report = "Not run";
    public bool Running { get; private set; }
    private readonly List<Tower.ShotRecord> shotsA = new List<Tower.ShotRecord>();
    private readonly List<Tower.ShotRecord> shotsB = new List<Tower.ShotRecord>();
    private Vector3 originalB;
    private int checks, failures;
    private void CaptureA(Tower.ShotRecord shot) => shotsA.Add(shot);
    private void CaptureB(Tower.ShotRecord shot) => shotsB.Add(shot);
    private void Check(bool ok, string name)
    {
        checks++; if (!ok) failures++;
        report += "\n" + (ok ? "PASS " : "FAIL ") + name;
    }
    private EnemyHandle Spawn(float progress, float z = 0f)
    {
        if (!spawnService.TrySpawn(resolver.SessionId, 900, "normal", path, out var handle))
            throw new System.InvalidOperationException("Test spawn failed");
        var spline = handle.Target.GetComponent<SplineAnimate>();
        spline.NormalizedTime = progress;
        spline.enabled = false; // Stationary controlled fixture, created by the real spawn service.
        handle.Target.transform.position = towerA.transform.position + new Vector3(1f, 0f, z);
        return handle;
    }
    private void Initialize(int levelB = 2)
    {
        if (!towerA.Initialize(towerA.Data, 1, resolver.Registry, resolver, out var a) ||
            !towerB.Initialize(towerB.Data, levelB, resolver.Registry, resolver, out var b))
            throw new System.InvalidOperationException("Tower initialization failed: " + a);
    }
    [ContextMenu("Session 9/Run All Checks")]
    public void RunVerification()
    {
        if (!Application.isPlaying || Running || resolver.SessionId <= 0) return;
        Running = true;
        originalB = towerB.transform.position;
        StartCoroutine(Verify());
    }
    private IEnumerator Verify()
    {
        report = "Session 9 verification"; checks = failures = 0;
        shotsA.Clear(); shotsB.Clear();
        towerA.ShotFired += CaptureA; towerB.ShotFired += CaptureB;
        try
        {
            Time.timeScale = 1f;
            spawnService.CleanupSession(resolver.SessionId);
            towerB.transform.position = towerA.transform.position;
            Initialize();
            var first = Spawn(0.2f);
            var second = Spawn(0.7f);
            second.Target.transform.SetAsFirstSibling();
            yield return new WaitForSeconds(0.3f);
            Check(shotsA.Count == 0 && shotsB.Count == 0, "Initialize: no shots before Activate");
            towerA.ActivateTargeting();
            yield return new WaitForSeconds(0.2f);
            Check(towerA.CurrentTarget.Identity.Equals(second.Identity) && shotsA.Count == 1,
                "Highest Progress wins regardless of Hierarchy");
            Check(!towerA.TryPreviewShot(), "Manual preview cannot fire alongside automatic targeting");
            second.Target.GetComponent<SplineAnimate>().NormalizedTime = 0.2f;
            second.Target.transform.position = first.Position;
            resolver.Registry.Remove(first.Identity); resolver.Registry.Add(first.Target);
            yield return new WaitForSeconds(0.15f);
            Check(towerA.CurrentTarget.Identity.Equals(first.Identity), "Equal Progress: lower SpawnId wins despite registry order");
            first.Target.transform.position += Vector3.forward * 100f;
            yield return new WaitForSeconds(0.15f);
            Check(towerA.CurrentTarget.Identity.Equals(first.Identity), "Z sorting does not affect XY range");
            first.Target.transform.position += Vector3.right * 10f;
            yield return null; yield return null;
            Check(towerA.CurrentTarget.Identity.Equals(second.Identity), "Out-of-range target immediately replaced");
            second.Target.RequestHit(60);
            yield return null; yield return null; yield return null;
            Check(!second.IsAlive && !towerA.CurrentTarget.IsAlive, "Dead target rejected");
            first.Target.transform.position = towerA.transform.position + Vector3.right;
            yield return null; yield return null;
            Check(towerA.CurrentTarget.Identity.Equals(first.Identity), "Valid target reacquired after loss");
            var oldIdentity = first.Identity;
            first.Target.BindCombat(resolver, new EnemyIdentity(resolver.SessionId + 100, 1));
            resolver.Registry.Remove(oldIdentity); resolver.Registry.Add(first.Target);
            yield return null; yield return null;
            Check(!first.IsAlive && !towerA.CurrentTarget.IsAlive, "Expired handle and foreign-session candidate rejected");
            resolver.Registry.Remove(first.Target.Identity);
            first.Target.BindCombat(resolver, oldIdentity); resolver.Registry.Add(first.Target);
            towerA.Suspend(); int suspended = shotsA.Count;
            yield return new WaitForSeconds(1f);
            Check(shotsA.Count == suspended, "Suspend stops shots");

            Initialize(1); shotsA.Clear(); shotsB.Clear();
            towerA.ActivateTargeting(); yield return new WaitForSeconds(0.25f);
            towerB.ActivateTargeting(); yield return new WaitForSeconds(0.1f);
            Check(shotsA.Count == 1 && shotsB.Count == 1 &&
                towerA.ReadSnapshot().RemainingCooldown < towerB.ReadSnapshot().RemainingCooldown,
                "Two level-I instances sharing data have independent cooldowns");

            Initialize(); shotsA.Clear(); shotsB.Clear();
            towerA.ActivateTargeting(); towerB.ActivateTargeting();
            yield return new WaitForSeconds(10.5f);
            CheckCadence(shotsA, 0.8f, "Level I"); CheckCadence(shotsB, 0.65f, "Level II");
            Check(first.Target.GetComponent<EnemyHealth>().CurrentHP == 60, "Shots cause no real damage");
            int pausedA = shotsA.Count, pausedB = shotsB.Count;
            float cooldownA = towerA.ReadSnapshot().RemainingCooldown;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(2.5f);
            Check(shotsA.Count == pausedA && shotsB.Count == pausedB &&
                Mathf.Approximately(cooldownA, towerA.ReadSnapshot().RemainingCooldown),
                "2.5-second pause: no shots and cooldown frozen");
            Time.timeScale = 1f;
            yield return new WaitForSeconds(1.5f);
            Check(shotsA.Count - pausedA <= 2 && shotsB.Count - pausedB <= 3 &&
                NoBurst(shotsA, 0.8f) && NoBurst(shotsB, 0.65f), "Resume: no catch-up burst");
            first.Target.transform.position += Vector3.right * 20f;
            yield return new WaitForSeconds(1f);
            int emptyCount = shotsA.Count;
            yield return new WaitForSeconds(0.2f);
            Check(shotsA.Count == emptyCount && towerA.ReadSnapshot().RemainingCooldown == 0f,
                "No target: cooldown clamped at zero, no shots");
        }
        finally
        {
            Restore();
            report += $"\nTOTAL {checks - failures}/{checks} passed";
            Debug.Log(report, this);
        }
    }
    private bool NoBurst(List<Tower.ShotRecord> shots, float cooldown)
    {
        for (int i = 1; i < shots.Count; i++)
            if (shots[i].Frame == shots[i - 1].Frame || shots[i].GameTime - shots[i - 1].GameTime < cooldown - 0.002f)
                return false;
        return true;
    }
    private void CheckCadence(List<Tower.ShotRecord> shots, float cooldown, string label)
    {
        float min = float.MaxValue, max = 0f, sum = 0f;
        for (int i = 1; i < shots.Count; i++)
        {
            float delta = shots[i].GameTime - shots[i - 1].GameTime;
            min = Mathf.Min(min, delta); max = Mathf.Max(max, delta); sum += delta;
        }
        Check(shots.Count >= 12 && NoBurst(shots, cooldown) && max < cooldown + 0.1f,
            $"{label}: 10.5 seconds, shots={shots.Count}, interval min/mean/max={min:F4}/{sum / Mathf.Max(1, shots.Count - 1):F4}/{max:F4}s");
    }
    private void Restore()
    {
        Time.timeScale = 1f;
        towerA.Suspend(); towerB.Suspend();
        towerA.ShotFired -= CaptureA; towerB.ShotFired -= CaptureB;
        towerB.transform.position = originalB;
        spawnService.CleanupSession(resolver.SessionId);
        Running = false;
    }
    private void OnDisable() { if (Running) { StopAllCoroutines(); Restore(); } }
}
