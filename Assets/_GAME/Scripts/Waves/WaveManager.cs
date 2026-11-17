using UnityEngine;
using UnityEngine.Splines;

[DisallowMultipleComponent]
public sealed class WaveManager : MonoBehaviour
{
    [SerializeField] private WaveData[] waves;
    [SerializeField] private SplineContainer path;
    [SerializeField] private RouteBinding[] routes;
    [System.Serializable] public sealed class RouteBinding { public string routeId; public SplineContainer container; }
    private WaveGroup[] groups;
    private int groupIndex, groupSpawned;
    public int CompletedWaves { get; private set; }
    private SplineContainer Route(string id)
    {
        if (routes != null) foreach (var route in routes)
            if (route.routeId == id && route.container && route.container.Splines.Count == 1) return route.container;
        return id == "route_left" && (routes == null || routes.Length == 0) ? path : null;
    }
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
        waveIndex = -1; CompletedWaves = 0; Spawned = 0; SpawnFinished = false; rewarded = false; LastError = "";
        session.BatchDrained += EvaluateCompletion; session.StateChanged += StateChanged;
        Changed?.Invoke();
    }
    public bool StartNextWave()
    {
        if (!session || !session.CanStartWave || scheduled || waveIndex + 1 >= TotalWaves) return false;
        var next = waves[waveIndex + 1];
        if (!next || !next.IsValid) { Fail("Invalid wave data."); return false; }
        groups = next.GetGroups();
        foreach (var group in groups) if (!Route(group.routeId)) { Fail("Missing single-spline route: " + group.routeId); return false; }
        waveIndex++; Spawned = 0; failures = 0; SpawnFinished = false; rewarded = false;
        groupIndex = groupSpawned = 0;
        remaining = groups[0].delayBeforeGroup; scheduled = true;
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
        var data = groups[groupIndex];
        remaining = data.interval; // One spawn per frame; no catch-up after pause.
        var request = new EnemySpawnRequest(sessionId, waveIndex + 1, data.enemyId, Route(data.routeId));
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
        groupSpawned++;
        if (groupSpawned == data.count)
        {
            groupIndex++; groupSpawned = 0;
            if (groupIndex == groups.Length) { SpawnFinished = true; scheduled = false; }
            else remaining = groups[groupIndex].delayBeforeGroup;
        }
        Changed?.Invoke();
    }
    private void EvaluateCompletion()
    {
        if (!session || session.SessionId != sessionId || session.State != GameState.Running ||
            session.Lives <= 0 || !SpawnFinished || session.AliveEnemies != 0 || rewarded) return;
        rewarded = true; CompletedWaves = waveIndex + 1; flow.Economy.AddGold(20);
        Debug.Log($"LEVEL01 WAVE {CompletedWaves}: gold={flow.Economy.CurrentGold}, lives={session.Lives}, spawned={Spawned}, killed={session.WaveKills}, leaked={session.WaveLeaks}", this);
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
