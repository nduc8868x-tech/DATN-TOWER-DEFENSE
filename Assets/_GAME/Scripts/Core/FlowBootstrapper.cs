using UnityEngine;

[DisallowMultipleComponent]
public class FlowBootstrapper : MonoBehaviour
{
    [Header("Core Managers")]
    [SerializeField] private GameSession gameSession;
    [SerializeField] private EconomyManager economyManager;

    [Header("Providers (Assign Fake or Real Component)")]
    [SerializeField] private MonoBehaviour enemySpawnServiceProvider;
    [SerializeField] private MonoBehaviour towerFactoryProvider;
    [SerializeField] private CombatResolver combatResolver;

    [Header("Initial Match Settings")]
    [SerializeField, Min(1)] private int startingLives = 20;
    [SerializeField, Min(0)] private int startingGold = 240;
    [SerializeField, Min(1)] private int totalWaves = 10;

    private IEnemySpawnService enemySpawnService;
    private ITowerFactory towerFactory;
    private bool isWired;

    public GameSession Session => gameSession;
    public EconomyManager Economy => economyManager;
    public IEnemySpawnService EnemySpawnService => enemySpawnService;
    public ITowerFactory TowerFactory => towerFactory;
    public bool IsWired => isWired;

    private void Awake()
    {
        ResolveDependencies();
    }

    private void Start()
    {
        if (!isWired)
            return;

        StartNewSession();
    }

    private void LateUpdate()
    {
        if (!isWired || gameSession == null)
            return;

        if (gameSession.State == GameState.Paused ||
            gameSession.State == GameState.Victory ||
            gameSession.State == GameState.Defeat)
        {
            return;
        }

        if (combatResolver != null)
            combatResolver.ResolvePendingBatch();

        gameSession.DrainResolvedBatch();
    }

    private void OnDestroy()
    {
        if (enemySpawnService != null && gameSession != null)
            enemySpawnService.Resolved -= gameSession.EnqueueResolved;
    }

    public bool ResolveDependencies()
    {
        isWired = false;

        if (gameSession == null)
            gameSession = GetComponent<GameSession>();

        if (economyManager == null)
            economyManager = GetComponent<EconomyManager>();

        if (gameSession == null)
        {
            Debug.LogError("[FlowBootstrapper] Thiếu tham chiếu GameSession.", this);
            return false;
        }

        if (economyManager == null)
        {
            Debug.LogError("[FlowBootstrapper] Thiếu tham chiếu EconomyManager.", this);
            return false;
        }

        if (enemySpawnServiceProvider == null)
        {
            Debug.LogError("[FlowBootstrapper] Chưa gán enemySpawnServiceProvider trong Inspector.", this);
            return false;
        }

        enemySpawnService = enemySpawnServiceProvider as IEnemySpawnService;
        if (enemySpawnService == null)
        {
            Debug.LogError(
                $"[FlowBootstrapper] '{enemySpawnServiceProvider.name}' ({enemySpawnServiceProvider.GetType().Name}) không implement interface IEnemySpawnService.",
                this
            );
            return false;
        }

        if (towerFactoryProvider != null)
        {
            towerFactory = towerFactoryProvider as ITowerFactory;
            if (towerFactory == null)
            {
                Debug.LogError(
                    $"[FlowBootstrapper] '{towerFactoryProvider.name}' ({towerFactoryProvider.GetType().Name}) không implement interface ITowerFactory.",
                    this
                );
                return false;
            }
        }

        enemySpawnService.Resolved -= gameSession.EnqueueResolved;
        enemySpawnService.Resolved += gameSession.EnqueueResolved;

        isWired = true;
        return true;
    }

    public void StartNewSession()
    {
        if (!isWired && !ResolveDependencies())
            return;

        int oldSessionId = gameSession.SessionId;

        // Khởi tạo session mới trước (đổi sessionId) để callback trễ của trận cũ bị tự động loại bỏ.
        gameSession.Initialize(startingLives, startingGold, totalWaves);

        if (oldSessionId > 0 && enemySpawnService != null)
            enemySpawnService.CleanupSession(oldSessionId);

        if (enemySpawnService is FakeEnemySpawnService fakeSpawn)
            fakeSpawn.SetActiveSessionId(gameSession.SessionId);

        if (combatResolver != null)
            combatResolver.BeginSession();
    }
}
