using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GameSession : MonoBehaviour
{
    private static int nextSessionId;

    [Header("Dependencies")]
    [SerializeField] private EconomyManager economyManager;

    [Header("Runtime State")]
    [SerializeField] private int sessionId;
    [SerializeField] private GameState state = GameState.Preparing;
    [SerializeField] private int lives = 20;
    [SerializeField] private int currentWave = 1;
    [SerializeField] private int totalWaves = 10;
    [SerializeField] private int aliveEnemies = 0;

    private readonly Queue<EnemyResolved> resolvedQueue = new Queue<EnemyResolved>();
    private readonly HashSet<int> processedSpawnIds = new HashSet<int>();
    private readonly Dictionary<EnemyIdentity, int> registered = new Dictionary<EnemyIdentity, int>();
    private bool accepting;
    private GameState stateBeforePause = GameState.Preparing;

    public int SessionId => sessionId;
    public GameState State => state;
    public int Lives => lives;
    public int CurrentWave => currentWave;
    public int TotalWaves => totalWaves;
    public int AliveEnemies => aliveEnemies;

    public bool BuildAllowed => accepting && (state == GameState.Preparing || state == GameState.Running);
    public bool CanStartWave => accepting && state == GameState.Preparing;
    public bool CanSimulate => accepting && state == GameState.Running;

    public event Action<GameState> StateChanged;
    public event Action<int> LivesChanged;
    public event Action<SessionSnapshot> SnapshotChanged;
    public event Action BatchDrained;

    private void OnDestroy()
    {
        if (economyManager != null)
            economyManager.GoldChanged -= HandleGoldChanged;

        Time.timeScale = 1f;
    }

    public void Initialize(int startingLives = 20, int startingGold = 240, int maxWaves = 10, int combatSessionId = 0)
    {
        Time.timeScale = 1f;
        sessionId = combatSessionId > 0 ? combatSessionId : ++nextSessionId;
        accepting = true;
        registered.Clear();
        lives = Mathf.Max(0, startingLives);
        currentWave = 1;
        totalWaves = Mathf.Max(1, maxWaves);
        aliveEnemies = 0;
        stateBeforePause = GameState.Preparing;

        resolvedQueue.Clear();
        processedSpawnIds.Clear();

        if (economyManager == null)
            economyManager = GetComponent<EconomyManager>();

        if (economyManager != null)
        {
            economyManager.GoldChanged -= HandleGoldChanged;
            economyManager.GoldChanged += HandleGoldChanged;
            economyManager.Initialize(startingGold);
        }

        SetState(GameState.Preparing);
        LivesChanged?.Invoke(lives);
        NotifySnapshot();
    }

    public bool TrySetState(GameState newState)
    {
        if (!accepting || state == GameState.Paused || state == GameState.Victory || state == GameState.Defeat ||
            (newState != GameState.Preparing && newState != GameState.Running))
            return false;

        if (state == newState)
            return false;

        SetState(newState);
        return true;
    }

    public bool Pause()
    {
        if (!accepting || (state != GameState.Preparing && state != GameState.Running))
            return false;

        stateBeforePause = state;
        Time.timeScale = 0f;
        SetState(GameState.Paused);
        return true;
    }

    public bool Resume()
    {
        if (!accepting || state != GameState.Paused)
            return false;

        Time.timeScale = 1f;
        SetState(stateBeforePause);
        return true;
    }

    public bool MarkVictory()
    {
        if (!accepting || state == GameState.Paused || state == GameState.Victory || state == GameState.Defeat || lives <= 0)
            return false;

        SetState(GameState.Victory);
        return true;
    }

    public bool ForceDefeat()
    {
        if (!accepting || state == GameState.Victory || state == GameState.Defeat)
            return false;

        SetState(GameState.Defeat);
        return true;
    }

    private void SetState(GameState newState)
    {
        state = newState;
        if (state == GameState.Victory || state == GameState.Defeat) Time.timeScale = 0f;
        StateChanged?.Invoke(state);
        NotifySnapshot();
    }

    public void SetWave(int waveIndex, int waveTotal)
    {
        currentWave = waveIndex;
        totalWaves = waveTotal;
        NotifySnapshot();
    }

    public bool RegisterEnemySpawned(EnemyHandle handle, int waveId)
    {
        if (!accepting || state != GameState.Running || waveId != currentWave || !handle.Identity.IsValid ||
            handle.Identity.SessionId != sessionId || registered.ContainsKey(handle.Identity)) return false;
        registered.Add(handle.Identity, waveId);
        aliveEnemies++;
        NotifySnapshot();
        return true;
    }

    public void Invalidate()
    {
        accepting = false;
        sessionId = 0;
        resolvedQueue.Clear(); registered.Clear(); processedSpawnIds.Clear();
    }

    public void SetAliveEnemies(int count)
    {
        aliveEnemies = Mathf.Max(0, count);
        NotifySnapshot();
    }

    public void ResetAliveCount()
    {
        aliveEnemies = 0;
        NotifySnapshot();
    }

    public void EnqueueResolved(EnemyResolved resolved)
    {
        if (!accepting || resolved.SessionId != sessionId)
            return;

        resolvedQueue.Enqueue(resolved);
    }

    public void DrainResolvedBatch()
    {
        if (!accepting || state == GameState.Paused || state == GameState.Victory || state == GameState.Defeat)
            return;

        bool anyProcessed = false;
        int batchSession = sessionId, bounty = 0, livesBefore = lives;

        while (resolvedQueue.Count > 0)
        {
            EnemyResolved item = resolvedQueue.Dequeue();

            if (item.SessionId != sessionId || item.WaveId != currentWave ||
                !registered.TryGetValue(item.Identity, out var wave) || wave != item.WaveId ||
                (item.State != EnemyState.Dead && item.State != EnemyState.Escaped))
                continue;

            // Chống nhận trùng lặp cùng SpawnId
            if (!processedSpawnIds.Add(item.SpawnId))
                continue;

            anyProcessed = true;

            if (item.Reason == ResolutionReason.Dead)
            {
                bounty += Mathf.Max(0, item.Bounty);
            }
            else if (item.Reason == ResolutionReason.Escaped)
            {
                lives = Mathf.Max(0, lives - Mathf.Max(0, item.LeakDamage));
            }

            aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
        }

        // Finish all bookkeeping before publishing. A listener may restart the match.
        if (bounty > 0 && economyManager != null) economyManager.AddGold(bounty);
        if (!accepting || sessionId != batchSession) return;
        if (lives != livesBefore) LivesChanged?.Invoke(lives);
        if (!accepting || sessionId != batchSession) return;
        if (anyProcessed) NotifySnapshot();
        if (!accepting || sessionId != batchSession) return;

        // Xét thua trước
        if (lives <= 0)
        {
            if (state != GameState.Defeat && state != GameState.Victory)
                SetState(GameState.Defeat);
            return;
        }

        BatchDrained?.Invoke();
    }

    public SessionSnapshot GetSnapshot()
    {
        int gold = economyManager != null ? economyManager.CurrentGold : 0;
        return new SessionSnapshot(state, gold, lives, currentWave, totalWaves, aliveEnemies);
    }

    private void HandleGoldChanged(int _)
    {
        NotifySnapshot();
    }

    private void NotifySnapshot()
    {
        SnapshotChanged?.Invoke(GetSnapshot());
    }
}
