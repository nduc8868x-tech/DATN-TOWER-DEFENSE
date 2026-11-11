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
    private GameState stateBeforePause = GameState.Preparing;

    public int SessionId => sessionId;
    public GameState State => state;
    public int Lives => lives;
    public int CurrentWave => currentWave;
    public int TotalWaves => totalWaves;
    public int AliveEnemies => aliveEnemies;

    public bool BuildAllowed => state == GameState.Preparing || state == GameState.Running;
    public bool CanStartWave => state == GameState.Preparing;
    public bool CanSimulate => state == GameState.Running;

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

    public void Initialize(int startingLives = 20, int startingGold = 240, int maxWaves = 10)
    {
        Time.timeScale = 1f;
        sessionId = ++nextSessionId;
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
        if (state == GameState.Victory || state == GameState.Defeat)
            return false;

        if (state == newState)
            return false;

        SetState(newState);
        return true;
    }

    public bool Pause()
    {
        if (state != GameState.Preparing && state != GameState.Running)
            return false;

        stateBeforePause = state;
        Time.timeScale = 0f;
        SetState(GameState.Paused);
        return true;
    }

    public bool Resume()
    {
        if (state != GameState.Paused)
            return false;

        Time.timeScale = 1f;
        SetState(stateBeforePause);
        return true;
    }

    public bool MarkVictory()
    {
        if (state == GameState.Victory || state == GameState.Defeat || lives <= 0)
            return false;

        SetState(GameState.Victory);
        return true;
    }

    public bool ForceDefeat()
    {
        if (state == GameState.Victory || state == GameState.Defeat)
            return false;

        SetState(GameState.Defeat);
        return true;
    }

    private void SetState(GameState newState)
    {
        state = newState;
        StateChanged?.Invoke(state);
        NotifySnapshot();
    }

    public void SetWave(int waveIndex, int waveTotal)
    {
        currentWave = waveIndex;
        totalWaves = waveTotal;
        NotifySnapshot();
    }

    public void RegisterEnemySpawned()
    {
        aliveEnemies++;
        NotifySnapshot();
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
        if (resolved.SessionId != sessionId)
            return;

        resolvedQueue.Enqueue(resolved);
    }

    public void DrainResolvedBatch()
    {
        if (state == GameState.Paused || state == GameState.Victory || state == GameState.Defeat)
            return;

        bool anyProcessed = false;

        while (resolvedQueue.Count > 0)
        {
            EnemyResolved item = resolvedQueue.Dequeue();

            if (item.SessionId != sessionId)
                continue;

            // Chống nhận trùng lặp cùng SpawnId
            if (!processedSpawnIds.Add(item.SpawnId))
                continue;

            anyProcessed = true;

            if (item.Reason == ResolutionReason.Dead)
            {
                if (economyManager != null && item.Bounty > 0)
                    economyManager.AddGold(item.Bounty);
            }
            else if (item.Reason == ResolutionReason.Escaped)
            {
                lives = Mathf.Max(0, lives - Mathf.Max(0, item.LeakDamage));
                LivesChanged?.Invoke(lives);
            }

            aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
        }

        if (anyProcessed)
            NotifySnapshot();

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
