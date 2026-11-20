using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class EnemySpawnTest : MonoBehaviour
{
    [SerializeField] private CombatResolver resolver;
    [SerializeField] private EnemySpawnService spawnService;
    [SerializeField] private SplineContainer path;
    [SerializeField] private string enemyId = "normal";

    [Header("Wave test")]
    [SerializeField, Min(1)] private int enemyCount = 5;
    [SerializeField, Min(0.01f)] private float spawnInterval = 0.8f;

    private EnemyHandle lastSpawned;
    private int sessionId;
    private int nextWaveId;
    private bool spawning;
    private Coroutine waveRoutine;

    private readonly List<EnemyHandle> recordedEnemies =
        new List<EnemyHandle>();

    private readonly List<EnemyHandle> aliveBuffer =
        new List<EnemyHandle>();

    private void Start()
    {
        if (resolver == null || spawnService == null)
        {
            Debug.LogError("Hãy gán Resolver và Spawn Service.", this);
            enabled = false;
            return;
        }

        resolver.BeginSession();
        sessionId = resolver.SessionId;
    }

    private void OnEnable()
    {
        if (spawnService == null)
            return;
        spawnService.Resolved += HandleResolved;
        spawnService.SessionCleaningUp += CancelSchedule;
    }

    private void HandleResolved(EnemyResolved result)
    {
        Debug.Log($"Resolved | ID {result.Identity} | Wave {result.WaveId} | " +
            $"{result.State} | Bounty {result.Bounty} | LeakDamage {result.LeakDamage}", this);
    }

    private void CancelSchedule(int cleaningSessionId)
    {
        if (cleaningSessionId != sessionId)
            return;
        if (waveRoutine != null)
            StopCoroutine(waveRoutine);
        waveRoutine = null;
        spawning = false;
        recordedEnemies.Clear();
        lastSpawned = default;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        spawning = false;
        waveRoutine = null;
        if (spawnService != null)
        {
            spawnService.CleanupSession(sessionId);
            spawnService.Resolved -= HandleResolved;
            spawnService.SessionCleaningUp -= CancelSchedule;
        }
    }

    private bool CanRunTest()
    {
        return Application.isPlaying
            && isActiveAndEnabled
            && resolver != null
            && spawnService != null
            && sessionId > 0
            && sessionId == resolver.SessionId;
    }

    private bool SpawnAndRecord(int waveId, out EnemyHandle handle)
    {
        if (!spawnService.TrySpawn(
            sessionId, waveId, enemyId, path, out handle))
        {
            return false;
        }

        // Ghi nhận trước khi cho quái di chuyển.
        recordedEnemies.Add(handle);
        spawnTimes.Add(Time.time);
        lastSpawned = handle;

        Debug.Log(
            $"Wave {waveId}: Đã ghi nhận ID {handle.Identity}. " +
            $"Tổng đã ghi nhận: {recordedEnemies.Count}.",
            this
        );

        return true;
    }

    [ContextMenu("Debug/01 - Spawn And Wait")]
    private void SpawnAndWait()
    {
        if (!CanRunTest() || spawning)
            return;

        SpawnAndRecord(++nextWaveId, out _);
    }

    [ContextMenu("Debug/02 - Begin Movement")]
    private void BeginMovement()
    {
        if (!CanRunTest() || spawning)
            return;

        bool started = spawnService.BeginMovement(lastSpawned);
        Debug.Log($"Bắt đầu di chuyển: {started}", this);
    }

    [ContextMenu("Debug/03 - Show Alive Count")]
    private void ShowAliveCount()
    {
        if (!CanRunTest())
            return;

        resolver.Registry.CopyAliveTo(aliveBuffer);

        Debug.Log(
            $"Đã ghi nhận: {recordedEnemies.Count}. " +
            $"Registry còn sống: {aliveBuffer.Count}.",
            this
        );

        foreach (EnemyHandle handle in aliveBuffer)
        {
            Debug.Log(
                $"{handle.Target.name} | ID {handle.Identity}",
                handle.Target
            );
        }
    }

    [ContextMenu("Debug/04 - Cleanup Session")]
    private void CleanupSession()
    {
        if (!CanRunTest())
            return;

        // Dừng lịch sinh trước khi dọn các quái đã sinh.
        spawnService.CleanupSession(sessionId);
        recordedEnemies.Clear();
        lastSpawned = default;

        Debug.Log("Đã dừng lịch sinh và dọn quái của lượt thử.", this);
    }

    [ContextMenu("Debug/05 - Spawn Wave")]
    private void SpawnWave()
    {
        if (!CanRunTest() || spawning)
            return;

        spawning = true;
        waveRoutine = StartCoroutine(SpawnWaveRoutine(++nextWaveId));
    }

    private IEnumerator SpawnWaveRoutine(int waveId)
    {
        int count = Mathf.Max(1, enemyCount);
        float interval = Mathf.Max(0.01f, spawnInterval);

        for (int i = 0; i < count; i++)
        {
            if (!CanRunTest())
            {
                spawning = false;
                yield break;
            }

            if (!SpawnAndRecord(waveId, out EnemyHandle handle))
            {
                Debug.LogWarning(
                    $"Wave {waveId}: Dừng lịch sinh tại con {i + 1}.",
                    this
                );

                spawning = false;
                yield break;
            }

            if (!spawnService.BeginMovement(handle))
            {
                Debug.LogError(
                    $"Không bắt đầu di chuyển được cho ID {handle.Identity}.",
                    this
                );

                spawning = false;
                yield break;
            }

            if (i < count - 1)
                yield return new WaitForSeconds(interval);
        }

        spawning = false;
        Debug.Log($"Wave {waveId}: Đã sinh đủ {count} quái.", this);
    }

    public bool VerificationRunning { get; private set; }
    public string VerificationReport { get; private set; } = "Not run";
    private readonly List<EnemyResolved> verificationResults = new List<EnemyResolved>();
    private readonly List<float> spawnTimes = new List<float>();
    private int checks;
    private int failures;

    private void Check(bool condition, string description)
    {
        checks++;
        if (!condition)
            failures++;
        string line = (condition ? "PASS: " : "FAIL: ") + description;
        VerificationReport += "\n" + line;
        Debug.Log(line, this);
    }

    private void CaptureResult(EnemyResolved result) => verificationResults.Add(result);

    [ContextMenu("Debug/07 - Verify Session 7")]
    public void RunVerification()
    {
        if (!CanRunTest() || VerificationRunning)
            return;
        CleanupSession();
        VerificationRunning = true;
        StartCoroutine(VerifyRoutine());
    }

    private IEnumerator VerifyRoutine()
    {
        VerificationReport = "Session 7 verification";
        checks = failures = 0;
        verificationResults.Clear();
        spawnService.Resolved += CaptureResult;
        try
        {
            int wave = ++nextWaveId;
            bool spawned = spawnService.TrySpawn(sessionId, wave, "normal", path, out EnemyHandle first);
            Check(spawned, "TrySpawn normal succeeds");
            if (!spawned)
                yield break;
            var health = first.Target.GetComponent<EnemyHealth>();
            var movement = first.Target.GetComponent<EnemyMovement>();
            resolver.Registry.CopyAliveTo(aliveBuffer);
            Check(health.CurrentHP == 60 && health.MaxHP == 60 && first.Identity.IsValid &&
                aliveBuffer.Count == 1 && aliveBuffer[0].Identity.Equals(first.Identity),
                "60 HP, valid identity, registered once");
            Vector3 position = first.Position;
            yield return new WaitForSeconds(1f);
            Check(!movement.IsMoving && first.Progress == 0f &&
                Vector3.Distance(position, first.Position) < 0.001f, "TrySpawn waits for BeginMovement");
            Check(spawnService.BeginMovement(first), "BeginMovement succeeds");
            yield return new WaitForSeconds(0.3f);
            Check(movement.IsMoving && first.Progress > 0f &&
                Vector3.Distance(position, first.Position) > 0.01f, "Position advances after BeginMovement");

            // Lethal damage wins against escape regardless of enqueue order.
            resolver.RequestEscape(new EscapeRequest(first.Target, first.Identity));
            first.Target.RequestHit(60);
            first.Target.RequestHit(60);
            resolver.RequestEscape(new EscapeRequest(first.Target, first.Identity));
            yield return null;
            yield return null;
            Check(verificationResults.Count == 1 && verificationResults[0].Identity.Equals(first.Identity) &&
                verificationResults[0].WaveId == wave && verificationResults[0].State == EnemyState.Dead &&
                verificationResults[0].Bounty == 8 && verificationResults[0].LeakDamage == 0,
                "Duplicate hits + escapes produce one Dead result with all payload fields");

            spawnService.TrySpawn(sessionId, ++nextWaveId, "normal", path, out EnemyHandle second);
            Check(second.IsAlive && !second.Identity.Equals(first.Identity), "Each spawn has a unique identity");
            if (!second.IsAlive)
                yield break;
            int escapedWave = nextWaveId;
            resolver.RequestEscape(new EscapeRequest(second.Target, second.Identity));
            resolver.RequestEscape(new EscapeRequest(second.Target, second.Identity));
            yield return null;
            yield return null;
            Check(verificationResults.Count == 2 && verificationResults[1].Identity.Equals(second.Identity) &&
                verificationResults[1].WaveId == escapedWave && verificationResults[1].State == EnemyState.Escaped &&
                verificationResults[1].Bounty == 0 && verificationResults[1].LeakDamage == 1,
                "Duplicate escape produces one Escaped result with all payload fields");
            CleanupSession();
            yield return null;

            spawnTimes.Clear();
            enemyCount = 5;
            spawnInterval = 0.8f;
            SpawnWave();
            float timeout = Time.time + 8f;
            while (spawning && Time.time < timeout)
                yield return null;
            resolver.Registry.CopyAliveTo(aliveBuffer);
            var uniqueIds = new HashSet<EnemyIdentity>();
            bool allHealthy = aliveBuffer.Count == 5;
            foreach (EnemyHandle handle in aliveBuffer)
            {
                uniqueIds.Add(handle.Identity);
                allHealthy &= handle.Target.GetComponent<EnemyHealth>().CurrentHP == 60;
            }
            Check(recordedEnemies.Count == 5 && allHealthy && uniqueIds.Count == 5,
                "Wave spawns five unique registered enemies with 60 HP");
            bool intervalsValid = spawnTimes.Count == 5;
            for (int i = 1; i < spawnTimes.Count; i++)
                intervalsValid &= spawnTimes[i] - spawnTimes[i - 1] >= 0.79f &&
                    spawnTimes[i] - spawnTimes[i - 1] < 1.05f;
            Check(intervalsValid, "Wave intervals are 0.8 seconds (frame tolerance)");
            VerificationReport += "\nSpawn times: " + string.Join(", ", spawnTimes);

            // Let the actual spline finish, rather than only simulating escape requests.
            timeout = Time.time + 30f;
            while (verificationResults.Count < 7 && Time.time < timeout)
                yield return null;
            var resolvedIds = new HashSet<EnemyIdentity>();
            bool naturalEscapes = verificationResults.Count == 7;
            for (int i = 2; i < verificationResults.Count; i++)
            {
                EnemyResolved result = verificationResults[i];
                naturalEscapes &= result.State == EnemyState.Escaped && result.Bounty == 0 &&
                    result.LeakDamage == 1 && result.WaveId == nextWaveId && resolvedIds.Add(result.Identity);
            }
            resolver.Registry.CopyAliveTo(aliveBuffer);
            Check(naturalEscapes && aliveBuffer.Count == 0, "Five natural spline completions resolve once and empty registry");
            CleanupSession();
            yield return null;

            int resultsBeforeCleanup = verificationResults.Count;
            SpawnWave();
            yield return new WaitForSeconds(0.9f);
            var cleanedObjects = new List<Enemy>();
            foreach (EnemyHandle handle in recordedEnemies)
            {
                cleanedObjects.Add(handle.Target);
                handle.Target.RequestHit(60);
                resolver.RequestEscape(new EscapeRequest(handle.Target, handle.Identity));
            }
            Check(cleanedObjects.Count == 2, "Cleanup test interrupts an unfinished wave");
            // Call the service directly to verify that it cancels the schedule owner too.
            spawnService.CleanupSession(sessionId);
            yield return new WaitForSeconds(3.5f);
            resolver.Registry.CopyAliveTo(aliveBuffer);
            bool destroyed = true;
            foreach (Enemy enemy in cleanedObjects)
                destroyed &= enemy == null;
            Check(!spawning && recordedEnemies.Count == 0 && aliveBuffer.Count == 0 && destroyed,
                "Cleanup cancels schedule, destroys instances and empties registry");
            Check(verificationResults.Count == resultsBeforeCleanup,
                "Cleanup with pending hit/escape requests emits no result");
            spawnService.CleanupSession(sessionId);
            Check(spawnService.TrySpawn(sessionId, ++nextWaveId, "normal", path, out EnemyHandle afterCleanup) &&
                afterCleanup.IsAlive && !uniqueIds.Contains(afterCleanup.Identity),
                "Cleanup is repeatable and subsequent spawn receives a fresh ID");
        }
        finally
        {
            spawnService.Resolved -= CaptureResult;
            CleanupSession();
            VerificationRunning = false;
            VerificationReport += $"\nTOTAL: {checks - failures}/{checks} passed";
            Debug.Log(VerificationReport, this);
        }
    }
}
