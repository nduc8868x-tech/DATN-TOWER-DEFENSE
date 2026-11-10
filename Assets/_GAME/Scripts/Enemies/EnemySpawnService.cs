using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

[DisallowMultipleComponent]
public class EnemySpawnService : MonoBehaviour
{
    [SerializeField] private EnemyCatalog catalog;
    [SerializeField] private CombatResolver resolver;

    private readonly List<Enemy> spawnedEnemies = new List<Enemy>();
    
    public event System.Action<EnemyResolved> Resolved;
    // The owner of a spawn schedule must cancel it before instances are removed.
    public event System.Action<int> SessionCleaningUp;

private readonly Dictionary<EnemyIdentity, SpawnInfo> spawnInfo =
    new Dictionary<EnemyIdentity, SpawnInfo>();

private readonly struct SpawnInfo
{
    public int WaveId { get; }
    public int Bounty { get; }
    public int LeakDamage { get; }

    public SpawnInfo(int waveId, int bounty, int leakDamage)
    {
        WaveId = waveId;
        Bounty = bounty;
        LeakDamage = leakDamage;
    }
}

    public bool TrySpawn(
        int sessionId,
        int waveId,
        string enemyId,
        SplineContainer path,
        out EnemyHandle handle)
    {
        handle = default;

        if (!Application.isPlaying || !isActiveAndEnabled)
            return false;

        if (resolver == null || catalog == null)
        {
            Debug.LogError("Spawn Service chưa được gán Catalog hoặc Resolver.", this);
            return false;
        }

        if (sessionId <= 0 || sessionId != resolver.SessionId)
        {
            Debug.LogWarning("Yêu cầu sinh quái thuộc lượt chơi không hợp lệ.", this);
            return false;
        }

        if (waveId <= 0)
        {
            Debug.LogWarning("Wave Id phải lớn hơn 0.", this);
            return false;
        }

        if (!catalog.TryValidate(out string error))
        {
            Debug.LogError(error, catalog);
            return false;
        }

        if (!catalog.TryGet(enemyId, out EnemyData data))
        {
            Debug.LogWarning($"Không tìm thấy Enemy Id '{enemyId}'.", this);
            return false;
        }

        if (path == null || path.Spline == null || path.Spline.Count < 2)
        {
            Debug.LogWarning("Đường đi cần có ít nhất hai knot.", this);
            return false;
        }

        if (data.speed <= 0f ||
            float.IsNaN(data.speed) ||
            float.IsInfinity(data.speed))
        {
            Debug.LogError($"{data.name}: Speed phải hữu hạn và lớn hơn 0.", data);
            return false;
        }

        if (!data.prefab.activeSelf ||
            !data.prefab.GetComponent<Enemy>().enabled ||
            !data.prefab.GetComponent<EnemyHealth>().enabled ||
            !data.prefab.GetComponent<EnemyMovement>().enabled)
        {
            Debug.LogError(
                $"{data.prefab.name}: Bật object gốc và các component " +
                "Enemy, EnemyHealth, EnemyMovement trước khi sinh.",
                data.prefab
            );
            return false;
        }

        GameObject instance = Instantiate(data.prefab);
        Enemy enemy = instance.GetComponent<Enemy>();
        EnemyHealth health = instance.GetComponent<EnemyHealth>();
        EnemyMovement movement = instance.GetComponent<EnemyMovement>();

        // Mỗi lần thử hiện tại tạo một instance mới.
        // Awake của Enemy khởi tạo Alive và Animator bắt đầu từ mặc định.
        health.Initialize(data);

        if (!movement.Initialize(path, data.speed))
        {
            instance.SetActive(false);
            Destroy(instance);
            return false;
        }

        resolver.RegisterEnemy(enemy);
        handle = new EnemyHandle(enemy);

        if (!handle.IsAlive)
        {
            resolver.Registry.Remove(enemy.Identity);
            instance.SetActive(false);
            Destroy(instance);
            handle = default;

            Debug.LogError("Không đăng ký được quái vừa sinh.", this);
            return false;
        }

        instance.name = $"{enemyId}_Wave{waveId}_Spawn{enemy.Identity.SpawnId}";
        spawnedEnemies.Add(enemy);
        
        spawnInfo.Add(
    enemy.Identity,
    new SpawnInfo(
        waveId,
        Mathf.Max(0, data.bounty),
        Mathf.Max(1, data.leakDamage)
    )
);

enemy.Resolved += HandleEnemyResolved;

        Debug.Log(
            $"{instance.name}: Sinh thành công, ID {enemy.Identity}, đang đứng chờ.",
            enemy
        );

        return true;
    }

    public bool BeginMovement(EnemyHandle handle)
    {
        if (resolver == null ||
            !handle.IsAlive ||
            handle.Identity.SessionId != resolver.SessionId ||
            !spawnedEnemies.Contains(handle.Target))
        {
            return false;
        }

        EnemyMovement movement = handle.Target.GetComponent<EnemyMovement>();

        if (movement == null || !movement.isActiveAndEnabled)
            return false;

        movement.StartMovement();
        return movement.IsMoving;
    }

    public void CleanupSession(int sessionId)
    {
        SessionCleaningUp?.Invoke(sessionId);

        // Also remove metadata for destroyed objects whose Unity reference is now null.
        var identities = new List<EnemyIdentity>(spawnInfo.Keys);
        foreach (EnemyIdentity identity in identities)
        {
            if (identity.SessionId == sessionId)
                spawnInfo.Remove(identity);
        }

        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = spawnedEnemies[i];

            if (enemy == null)
            {
                spawnedEnemies.RemoveAt(i);
                continue;
            }

            if (enemy.Identity.SessionId != sessionId)
                continue;

            enemy.Resolved -= HandleEnemyResolved;
            if (resolver != null)
                resolver.Registry.Remove(enemy.Identity);
            // OnDisable dừng movement và bỏ quái khỏi registry.
            enemy.gameObject.SetActive(false);
            Destroy(enemy.gameObject);
            spawnedEnemies.RemoveAt(i);
        }
    }
    private void HandleEnemyResolved(Enemy enemy)
{
    if (enemy == null)
        return;

    EnemyIdentity identity = enemy.Identity;

    if (!spawnInfo.TryGetValue(identity, out SpawnInfo info))
        return;

    if (enemy.State != EnemyState.Dead &&
        enemy.State != EnemyState.Escaped)
    {
        return;
    }

    // Bỏ bản ghi trước khi gửi thông báo để tránh xử lý lặp.
    spawnInfo.Remove(identity);
    enemy.Resolved -= HandleEnemyResolved;

    if (resolver == null || identity.SessionId != resolver.SessionId)
        return;

    bool died = enemy.State == EnemyState.Dead;

    var result = new EnemyResolved(
        identity,
        info.WaveId,
        enemy.State,
        died ? info.Bounty : 0,
        died ? 0 : info.LeakDamage
    );

    Resolved?.Invoke(result);
}
}
