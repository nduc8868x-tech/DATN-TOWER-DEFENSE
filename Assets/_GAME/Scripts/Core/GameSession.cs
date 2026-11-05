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

    public void Initialize(int startingLives = 20, int startingGold = 240, int maxWaves = 10)
    {
        sessionId = ++nextSessionId;
        lives = startingLives;
        currentWave = 1;
        totalWaves = maxWaves;
        aliveEnemies = 0;
        resolvedQueue.Clear();
        processedSpawnIds.Clear();

        if (economyManager == null)
            economyManager = GetComponent<EconomyManager>();

        if (economyManager != null)
            economyManager.Initialize(startingGold);

        SetState(GameState.Preparing);
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

    public void SetAliveEnemies(int count)
    {
        aliveEnemies = Mathf.Max(0, count);
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
        while (resolvedQueue.Count > 0)
        {
            EnemyResolved item = resolvedQueue.Dequeue();

            if (item.SessionId != sessionId)
                continue;

            // Kiểm tra trùng lặp (Dedup)
            if (!processedSpawnIds.Add(item.SpawnId))
                continue;

            if (item.Reason == ResolutionReason.Dead)
            {
                if (economyManager != null && item.Bounty > 0)
                    economyManager.AddGold(item.Bounty);
            }
            else if (item.Reason == ResolutionReason.Escaped)
            {
                lives = Mathf.Max(0, lives - item.LeakDamage);
                LivesChanged?.Invoke(lives);
            }

            aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
        }

        NotifySnapshot();

        // Xét điều kiện thua trước
        if (lives <= 0 && state != GameState.Defeat)
        {
            SetState(GameState.Defeat);
        }
    }

    public SessionSnapshot GetSnapshot()
    {
        int gold = economyManager != null ? economyManager.CurrentGold : 0;
        return new SessionSnapshot(state, gold, lives, currentWave, totalWaves, aliveEnemies);
    }

    private void NotifySnapshot()
    {
        SnapshotChanged?.Invoke(GetSnapshot());
    }
}
