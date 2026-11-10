using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

// Session 8 only: explicit preview calls, no targeting, damage, factory or economy.
public class TowerDefinitionTest : MonoBehaviour
{
    [SerializeField] private TowerData data;
    [SerializeField] private Tower towerA;
    [SerializeField] private Tower towerB;
    [SerializeField] private CombatResolver resolver;
    [SerializeField] private EnemySpawnService spawnService;
    [SerializeField] private SplineContainer path;
    [TextArea(4, 20)] [SerializeField] private string report = "Not run";
    public string Report => report;
    public bool Running { get; private set; }
    private int checks, failed;

    private IEnumerator Start()
    {
        // EnemySpawnTest owns session initialization in CombatSandbox.
        yield return null;
        if (towerA == null || towerB == null || data == null || resolver == null || spawnService == null || path == null)
        {
            Debug.LogError("TowerDefinitionTest: assign both towers, data, resolver, spawn service and path.", this);
            yield break;
        }
        if (!InitializePair(1, 2))
            yield break;
        Debug.Log($"Session 8 ready (suspended): A {towerA.ReadSnapshot()}; B {towerB.ReadSnapshot()}", this);
    }

    private bool InitializePair(int levelA, int levelB)
    {
        bool a = towerA.Initialize(data, levelA, resolver.Registry, resolver, out string errorA);
        bool b = towerB.Initialize(data, levelB, resolver.Registry, resolver, out string errorB);
        if (!a || !b)
            Debug.LogError($"TowerDefinitionTest: {errorA} {errorB}", this);
        return a && b;
    }

    private void Check(bool ok, string description)
    {
        checks++;
        if (!ok) failed++;
        string line = (ok ? "PASS: " : "FAIL: ") + description;
        report += "\n" + line;
        Debug.Log(line, this);
    }

    [ContextMenu("Session 8/01 - Verify Definition And Lifecycle")]
    public void RunVerification()
    {
        if (!Application.isPlaying || Running || resolver == null || resolver.SessionId <= 0)
            return;
        Running = true;
        StartCoroutine(Verify());
    }

    private IEnumerator Verify()
    {
        report = "Session 8 verification";
        checks = failed = 0;
        string originalData = JsonUtility.ToJson(data);
        try
        {
            Check(data.TryValidate(out _), "Arrow definition and prefab references are valid");
            Check(data.TryGetLevel(1, out var one, out _) && one.damage == 16 &&
                Mathf.Approximately(one.cooldown, 0.8f) && one.range == 3f && one.cost == 100,
                "Level I: damage 16, cooldown 0.8, range 3, purchase 100");
            Check(data.TryGetLevel(2, out var two, out _) && two.damage == 28 &&
                Mathf.Approximately(two.cooldown, 0.65f) && two.range == 3f && two.cost == 100,
                "Level II: damage 28, cooldown 0.65, range 3, upgrade 100");
            Check(!data.TryGetLevel(3, out _, out string missingLevel) && missingLevel.Contains("3"),
                "Missing level returns a named configuration error");
            var incomplete = ScriptableObject.CreateInstance<TowerData>();
            incomplete.name = "MissingLevelII";
            incomplete.prefab = data.prefab;
            incomplete.icon = data.icon;
            JsonUtility.FromJsonOverwrite("{\"levels\":[{}]}", incomplete);
            Check(!incomplete.TryValidate(out string missingII) && missingII.Contains("level II") &&
                missingII.Contains("MissingLevelII"), "Missing level II names the invalid asset and rejects it");
            Destroy(incomplete);
            Check(InitializePair(1, 1) && !towerA.IsActive && !towerB.IsActive,
                "Two instances share data but Initialize leaves both suspended");
            Check(towerA.Muzzle != null && towerA.Muzzle.IsChildOf(towerA.transform) &&
                towerA.Muzzle.localPosition.y > 1f && towerA.Muzzle.localPosition.x > 0f,
                "Muzzle is a child at the elevated bow position");
            yield return new WaitForSeconds(0.9f);
            Check(!towerA.TryPreviewShot() && towerA.ReadSnapshot().PreviewShots == 0 &&
                towerA.ReadSnapshot().RemainingCooldown == 0f, "No preview shot or cooldown before Activate");
            Check(!towerA.TryValidateForActivation(out string projectileError) &&
                projectileError.Contains("projectilePrefab") && !towerA.Activate(),
                "Real activation rejects missing projectile data (explicit preview required)");
            Check(towerA.Activate(true) && towerA.TryPreviewShot() && !towerB.TryPreviewShot(),
                "Explicit preview activates only A; B stays suspended");
            float firstCooldown = towerA.ReadSnapshot().RemainingCooldown;
            yield return new WaitForSeconds(0.25f);
            var previewAnimator = towerA.GetComponentInChildren<Animator>();
            Check(previewAnimator != null && previewAnimator.GetCurrentAnimatorStateInfo(0).IsName("Preview"),
                "Preview shot plays the supplied archer frame animation");
            Check(towerA.ReadSnapshot().RemainingCooldown < firstCooldown &&
                towerB.ReadSnapshot().RemainingCooldown == 0f && towerB.ReadSnapshot().PreviewShots == 0,
                "Runtime cooldown and shot count are independent on instances with the same data");
            Check(towerB.Activate(true) && towerB.TryPreviewShot() &&
                towerB.ReadSnapshot().RemainingCooldown > towerA.ReadSnapshot().RemainingCooldown,
                "B starts its own full cooldown without resetting A");
            towerA.Suspend();
            float frozen = towerA.ReadSnapshot().RemainingCooldown;
            yield return new WaitForSeconds(0.9f);
            Check(!towerA.TryPreviewShot() && Mathf.Approximately(frozen, towerA.ReadSnapshot().RemainingCooldown),
                "Suspend blocks new preview shots and freezes this instance's cooldown");
            Check(towerB.Initialize(data, 2, resolver.Registry, resolver, out _) &&
                towerB.ReadSnapshot().Damage == 28 && !towerB.IsActive &&
                towerA.ReadSnapshot().Damage == 16 && towerB.ReadSnapshot().PreviewShots == 0,
                "Initialize level II resets B and leaves A at level I");
            Vector3 origin = towerA.transform.position;
            Check(towerA.ContainsPosition(origin + Vector3.right * 2.99f) &&
                !towerA.ContainsPosition(origin + Vector3.right * 3.01f),
                "Range boundary is 3 world units around the same center as the Gizmo");
            var invalid = new GameObject("Session8_InvalidTower");
            var invalidTower = invalid.AddComponent<Tower>();
            Check(!invalidTower.Initialize(data, 1, resolver.Registry, resolver, out string muzzleError) &&
                muzzleError.Contains("Muzzle") && !invalidTower.Activate(true),
                "Missing Muzzle/body fails initialization and activation safely");
            Destroy(invalid);

            Check(spawnService.TrySpawn(resolver.SessionId, 800, "normal", path, out EnemyHandle enemy) &&
                spawnService.BeginMovement(enemy), "Existing normal enemy spawns and begins movement for range observation");
            bool wasInside = false, wasOutside = false, stayedHealthy = true;
            float deadline = Time.time + 30f;
            while (enemy.IsAlive && Time.time < deadline)
            {
                bool inside = towerA.ContainsPosition(enemy.Position);
                wasInside |= inside;
                wasOutside |= !inside;
                stayedHealthy &= enemy.Target.GetComponent<EnemyHealth>().CurrentHP == 60;
                yield return null;
            }
            Check(wasInside && wasOutside && !enemy.IsAlive && stayedHealthy,
                "Real spline movement crosses the range circle; session 8 causes no damage");
            Check(JsonUtility.ToJson(data) == originalData,
                "Runtime initialization, previews and suspension never mutate TowerData");
        }
        finally
        {
            towerA.Suspend();
            towerB.Suspend();
            spawnService.CleanupSession(resolver.SessionId);
            InitializePair(1, 2);
            Running = false;
            report += $"\nTOTAL: {checks - failed}/{checks} passed";
            Debug.Log(report, this);
        }
    }

    [ContextMenu("Session 8/02 - Activate Preview A")]
    private void ActivateA() { if (Application.isPlaying) towerA.Activate(true); }
    [ContextMenu("Session 8/03 - Preview Shot A (No Damage)")]
    private void PreviewA() { if (Application.isPlaying) towerA.TryPreviewShot(); }
    [ContextMenu("Session 8/04 - Suspend A")]
    private void SuspendA() { if (Application.isPlaying) towerA.Suspend(); }
    [ContextMenu("Session 8/05 - Read Snapshots")]
    private void ShowSnapshots()
    {
        if (Application.isPlaying)
            Debug.Log($"A {towerA.ReadSnapshot()}\nB {towerB.ReadSnapshot()}", this);
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        if (towerA != null) towerA.Suspend();
        if (towerB != null) towerB.Suspend();
        Running = false;
    }
}
