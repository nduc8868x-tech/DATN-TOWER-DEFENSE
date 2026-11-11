using UnityEngine;
using UnityEngine.Splines;

[DisallowMultipleComponent]
public sealed class WaveManager : MonoBehaviour
{
    [SerializeField] private WaveData[] waves;
    [SerializeField] private SplineContainer path;
    private FlowBootstrapper flow;
    private GameSession session;
    private int waveIndex = -1, sessionId, failures;
    private bool scheduled, rewarded;
    private float remaining;
    public int TotalWaves => waves == null ? 0 : waves.Length;
    public int Spawned { get; private set; }
    public bool SpawnFinished { get; private set; }
    public string LastError { get; private set; } = "";
    public event System.Action Changed;
    public void Initialize(FlowBootstrapper owner)
    {
        if (session) { session.BatchDrained -= EvaluateCompletion; session.StateChanged -= StateChanged; }
        CancelSchedule(); flow = owner; session = owner.Session; sessionId = session.SessionId;
        waveIndex = -1; Spawned = 0; SpawnFinished = false; rewarded = false; LastError = "";
        session.BatchDrained += EvaluateCompletion; session.StateChanged += StateChanged;
        Changed?.Invoke();
    }
    public bool StartNextWave()
    {
        if (!session || !session.CanStartWave || scheduled || waveIndex + 1 >= TotalWaves) return false;
        var next = waves[waveIndex + 1];
        if (!next || !next.IsValid || !path) { Fail("Invalid wave data or missing spline."); return false; }
        waveIndex++; Spawned = 0; failures = 0; SpawnFinished = false; rewarded = false;
        remaining = 0; scheduled = true;
        session.SetWave(waveIndex + 1, TotalWaves);
        session.TrySetState(GameState.Running);
        Changed?.Invoke();
        return true;
    }
    private void Update()
    {
        if (!scheduled || !session || !session.CanSimulate || session.SessionId != sessionId || Time.deltaTime <= 0) return;
        remaining -= Time.deltaTime;
        if (remaining > 0) return;
        var data = waves[waveIndex];
        remaining = data.interval; // One spawn per frame; no catch-up after pause.
        var request = new EnemySpawnRequest(sessionId, waveIndex + 1, data.enemyId, path);
        if (!flow.EnemySpawnService.TrySpawn(request, out var handle))
        {
            failures++;
            LastError = $"Wave {waveIndex + 1}: spawn failed ({failures}/3): {flow.EnemySpawnService.LastError}";
            Debug.LogWarning(LastError, this); Changed?.Invoke();
            if (failures >= 3) Fail(LastError);
            return;
        }
        if (!session.RegisterEnemySpawned(handle, waveIndex + 1)) { Fail("Spawn identity registration rejected."); return; }
        Spawned++; // Successful spawn, identity/count registered BEFORE movement.
        if (!flow.EnemySpawnService.BeginMovement(handle)) { Fail("BeginMovement rejected registered enemy."); return; }
        if (Spawned == data.count) { SpawnFinished = true; scheduled = false; }
        Changed?.Invoke();
    }
    private void EvaluateCompletion()
    {
        if (!session || session.SessionId != sessionId || session.State != GameState.Running ||
            session.Lives <= 0 || !SpawnFinished || session.AliveEnemies != 0 || rewarded) return;
        rewarded = true; flow.Economy.AddGold(20);
        if (waveIndex == TotalWaves - 1) session.MarkVictory();
        else session.TrySetState(GameState.Preparing);
        Changed?.Invoke();
    }
    private void Fail(string reason)
    {
        LastError = reason; scheduled = false;
        Debug.LogWarning("Wave aborted: " + reason, this);
        session?.ForceDefeat(); Changed?.Invoke();
    }
    private void StateChanged(GameState state)
    {
        if (state == GameState.Defeat || state == GameState.Victory) scheduled = false;
    }
    public void CancelSchedule() { scheduled = false; remaining = 0; }
    private void OnDestroy()
    {
        if (session) { session.BatchDrained -= EvaluateCompletion; session.StateChanged -= StateChanged; }
    }
}
