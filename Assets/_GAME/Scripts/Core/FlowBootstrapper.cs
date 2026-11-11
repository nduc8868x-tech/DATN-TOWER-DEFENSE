using UnityEngine;

[DisallowMultipleComponent, DefaultExecutionOrder(11000)]
public class FlowBootstrapper : MonoBehaviour
{
    [SerializeField] private GameSession gameSession;
    [SerializeField] private EconomyManager economyManager;
    [SerializeField] private MonoBehaviour enemySpawnServiceProvider;
    [SerializeField] private MonoBehaviour towerFactoryProvider;
    [SerializeField] private CombatResolver combatResolver;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private int startingLives = 20;
    [SerializeField] private int startingGold = 240;
    [SerializeField] private int totalWaves = 3;
    private IEnemySpawnService enemySpawnService;
    private ITowerFactory towerFactory;
    private bool isWired, restarting;
    public GameSession Session => gameSession;
    public EconomyManager Economy => economyManager;
    public IEnemySpawnService EnemySpawnService => enemySpawnService;
    public ITowerFactory TowerFactory => towerFactory;
    public bool IsWired => isWired;
    private void Awake() => ResolveDependencies();
    private void Start() { if (isWired) StartNewSession(); }
    // Resolver is the only combat runner (order 10000); drain all results afterwards.
    private void LateUpdate() { if (isWired && !restarting) gameSession.DrainResolvedBatch(); }
    public bool ResolveDependencies()
    {
        if (enemySpawnService != null && gameSession != null) enemySpawnService.Resolved -= gameSession.EnqueueResolved;
        if (!gameSession) gameSession = GetComponent<GameSession>();
        if (!economyManager) economyManager = GetComponent<EconomyManager>();
        if (!waveManager) waveManager = GetComponent<WaveManager>();
        enemySpawnService = enemySpawnServiceProvider as IEnemySpawnService;
        towerFactory = towerFactoryProvider as ITowerFactory;
        isWired = gameSession && economyManager && enemySpawnService != null && towerFactory != null;
        if (isWired) enemySpawnService.Resolved += gameSession.EnqueueResolved;
        else Debug.LogError("FlowBootstrapper: missing session, economy or providers.", this);
        return isWired;
    }
    public void StartNewSession()
    {
        if (restarting || (!isWired && !ResolveDependencies())) return;
        restarting = true;
        try
        {
            int old = gameSession.SessionId;
            gameSession.Invalidate();
            if (combatResolver) combatResolver.InvalidateSession();
            waveManager?.CancelSchedule();
            GetComponent<BuildInputController>()?.ClearSelection();
            GetComponent<BuildManager>()?.ClearAllTowers();
            if (old > 0) enemySpawnService.CleanupSession(old);
            if (combatResolver) combatResolver.BeginSession();
            Time.timeScale = 1f;
            gameSession.Initialize(startingLives, startingGold, waveManager ? waveManager.TotalWaves : totalWaves,
                combatResolver ? combatResolver.SessionId : 0);
            if (enemySpawnService is FakeEnemySpawnService fake) fake.SetActiveSessionId(gameSession.SessionId);
            waveManager?.Initialize(this);
        }
        finally { restarting = false; }
    }
    private void OnDestroy()
    {
        if (enemySpawnService != null && gameSession) enemySpawnService.Resolved -= gameSession.EnqueueResolved;
        if (!gameSession) return;
        int old = gameSession.SessionId;
        gameSession.Invalidate(); waveManager?.CancelSchedule();
        GetComponent<BuildManager>()?.ClearAllTowers();
        if (old > 0) enemySpawnService?.CleanupSession(old);
        Time.timeScale = 1f;
    }
}
