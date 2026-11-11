using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FakeEnemySpawnService : MonoBehaviour, IEnemySpawnService
{
    [Serializable]
    public class FakeEnemyEntry
    {
        public int sessionId;
        public int waveId;
        public int spawnId;
        public string enemyId;
        public int bounty;
        public int leakDamage;
        public bool movementStarted;
        public bool isResolved;
        public bool isCleanedUp;
        public EnemyHandle handle;
        public GameObject gameObject;
    }

    [Header("Debug Controls")]
    [SerializeField] private bool failNextSpawn;
    [SerializeField] private int selectedIndex;
    [SerializeField] private int defaultBounty = 8;
    [SerializeField] private int defaultLeakDamage = 1;
    [SerializeField] private bool spawnVisualMarkers = true;

    [Header("Runtime Inspection")]
    [SerializeField] private int activeSessionId;
    [SerializeField] private List<FakeEnemyEntry> entries = new List<FakeEnemyEntry>();

    private int nextSpawnId;
    private EnemyResolved? lastEmittedEvent;

    public bool FailNextSpawn
    {
        get => failNextSpawn;
        set => failNextSpawn = value;
    }

    public int SelectedIndex
    {
        get => selectedIndex;
        set => selectedIndex = Mathf.Max(0, value);
    }

    public IReadOnlyList<FakeEnemyEntry> Entries => entries;
    public int ActiveCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (!entries[i].isResolved && !entries[i].isCleanedUp)
                    count++;
            }
            return count;
        }
    }

    public event Action<EnemyResolved> Resolved;
    public string LastError { get; private set; } = "";

    public void SetActiveSessionId(int sessionId)
    {
        activeSessionId = sessionId;
    }

    public bool TrySpawn(EnemySpawnRequest request, out EnemyHandle handle)
    {
        LastError = "";
        if (failNextSpawn)
        {
            LastError = "Injected fake spawn failure.";
            failNextSpawn = false;
            handle = default;
            Debug.LogWarning("[FakeEnemySpawnService] FailNextSpawn đang bật -> TrySpawn trả về false.");
            return false;
        }

        if (request.SessionId <= 0 || string.IsNullOrEmpty(request.EnemyId))
        {
            LastError = "Invalid fake spawn request.";
            handle = default;
            Debug.LogError("[FakeEnemySpawnService] EnemySpawnRequest không hợp lệ (thiếu SessionId hoặc EnemyId).");
            return false;
        }

        activeSessionId = request.SessionId;
        int spawnId = ++nextSpawnId;

        GameObject go = new GameObject($"FakeEnemy_{spawnId}_{request.EnemyId}");
        go.transform.SetParent(transform, false);
        int slot = entries.Count;
        go.transform.position = new Vector3(0.5f + (slot % 6) * 1.1f, 2.6f - (slot / 6) * 1.0f, 0f);
        go.transform.localScale = Vector3.one * 0.65f;

        if (spawnVisualMarkers)
        {
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateEnemySprite();
            sr.color = Color.yellow; // Đang chờ BeginMovement
            sr.sortingOrder = 4;
        }

        Enemy enemyComp = go.AddComponent<Enemy>();
        var identity = new EnemyIdentity(request.SessionId, spawnId);
        enemyComp.BindCombat(null, identity);

        handle = new EnemyHandle(enemyComp);

        GetDefaultsForEnemyId(request.EnemyId, out int bounty, out int leakDamage);

        entries.Add(new FakeEnemyEntry
        {
            sessionId = request.SessionId,
            waveId = request.WaveId,
            spawnId = spawnId,
            enemyId = request.EnemyId,
            bounty = bounty,
            leakDamage = leakDamage,
            movementStarted = false,
            isResolved = false,
            isCleanedUp = false,
            handle = handle,
            gameObject = go
        });

        return true;
    }

    public bool BeginMovement(EnemyHandle handle)
    {
        if (!handle.Identity.IsValid)
            return false;

        for (int i = 0; i < entries.Count; i++)
        {
            FakeEnemyEntry entry = entries[i];
            if (entry.sessionId == handle.Identity.SessionId && entry.spawnId == handle.Identity.SpawnId)
            {
                if (entry.isCleanedUp || entry.isResolved)
                    return false;

                entry.movementStarted = true;
                if (entry.gameObject != null && entry.gameObject.TryGetComponent<SpriteRenderer>(out SpriteRenderer sr))
                    sr.color = Color.green; // Đã bắt đầu di chuyển
                return true;
            }
        }
        return false;
    }

    public void CleanupSession(int sessionId)
    {
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            FakeEnemyEntry entry = entries[i];
            if (entry.sessionId == sessionId)
            {
                entry.isCleanedUp = true;
                if (entry.gameObject != null)
                {
                    entry.gameObject.SetActive(false);
                    Destroy(entry.gameObject);
                }
            }
        }

        if (activeSessionId == sessionId)
            entries.RemoveAll(e => e.sessionId == sessionId);
    }

    public bool ResolveDead(int index)
    {
        return ResolveAtIndex(index, ResolutionReason.Dead);
    }

    public bool ResolveEscaped(int index)
    {
        return ResolveAtIndex(index, ResolutionReason.Escaped);
    }

    public bool KillFirstAlive()
    {
        int idx = FindFirstAliveIndex();
        return idx >= 0 && ResolveDead(idx);
    }

    public bool EscapeFirstAlive()
    {
        int idx = FindFirstAliveIndex();
        return idx >= 0 && ResolveEscaped(idx);
    }

    private int FindFirstAliveIndex()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (!entries[i].isResolved && !entries[i].isCleanedUp)
                return i;
        }
        return -1;
    }

    private bool ResolveAtIndex(int index, ResolutionReason reason)
    {
        if (index < 0 || index >= entries.Count)
        {
            Debug.LogWarning($"[FakeEnemySpawnService] Index {index} không hợp lệ.");
            return false;
        }

        FakeEnemyEntry entry = entries[index];
        if (entry.isCleanedUp || entry.isResolved)
        {
            Debug.LogWarning($"[FakeEnemySpawnService] Quái #{entry.spawnId} đã kết thúc từ trước (không phát lần 2).");
            return false;
        }

        entry.isResolved = true;

        if (entry.gameObject != null)
        {
            if (entry.gameObject.TryGetComponent<SpriteRenderer>(out SpriteRenderer sr))
                sr.color = reason == ResolutionReason.Dead ? Color.gray : Color.red;

            entry.gameObject.SetActive(false);
        }

        var payload = new EnemyResolved(
            entry.sessionId,
            entry.waveId,
            entry.spawnId,
            reason,
            reason == ResolutionReason.Dead ? entry.bounty : 0,
            reason == ResolutionReason.Escaped ? entry.leakDamage : 0
        );

        lastEmittedEvent = payload;
        Resolved?.Invoke(payload);
        return true;
    }

    [ContextMenu("Fake/Spawn Five")]
    public void DebugSpawnFive()
    {
        if (!Application.isPlaying)
            return;

        int sid = activeSessionId > 0 ? activeSessionId : 1;
        for (int i = 0; i < 5; i++)
        {
            var req = new EnemySpawnRequest(sid, 1, "normal", null);
            if (TrySpawn(req, out EnemyHandle handle))
                BeginMovement(handle);
        }
    }

    [ContextMenu("Fake/Kill Selected")]
    public void DebugKillSelected()
    {
        if (!Application.isPlaying)
            return;

        if (!ResolveDead(selectedIndex))
            KillFirstAlive();
    }

    [ContextMenu("Fake/Escape Selected")]
    public void DebugEscapeSelected()
    {
        if (!Application.isPlaying)
            return;

        if (!ResolveEscaped(selectedIndex))
            EscapeFirstAlive();
    }

    [ContextMenu("Fake/Emit Duplicate")]
    public void DebugEmitDuplicate()
    {
        if (!Application.isPlaying)
            return;

        if (!lastEmittedEvent.HasValue)
        {
            Debug.LogWarning("[FakeEnemySpawnService] Chưa có sự kiện nào được phát để test trùng lặp.");
            return;
        }

        Debug.Log($"[FakeEnemySpawnService] Cố ý phát trùng sự kiện SpawnId={lastEmittedEvent.Value.SpawnId} ({lastEmittedEvent.Value.Reason}).");
        Resolved?.Invoke(lastEmittedEvent.Value);
    }

    [ContextMenu("Fake/Fail Next Spawn")]
    public void DebugFailNextSpawn()
    {
        failNextSpawn = true;
        Debug.Log("[FakeEnemySpawnService] Đã bật FailNextSpawn = true.");
    }

    [ContextMenu("Fake/Clear Session")]
    public void DebugClearSession()
    {
        if (!Application.isPlaying)
            return;

        CleanupSession(activeSessionId);
        Debug.Log($"[FakeEnemySpawnService] Đã dọn sạch session {activeSessionId} không phát thưởng/trừ mạng.");
    }

    private void GetDefaultsForEnemyId(string enemyId, out int bounty, out int leakDamage)
    {
        switch (enemyId)
        {
            case "runner":
                bounty = 7;
                leakDamage = 1;
                break;
            case "armored":
                bounty = 20;
                leakDamage = 2;
                break;
            case "normal":
            default:
                bounty = defaultBounty;
                leakDamage = defaultLeakDamage;
                break;
        }
    }

    private static Sprite cachedEnemySprite;

    private static Sprite GetOrCreateEnemySprite()
    {
        if (cachedEnemySprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            cachedEnemySprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                tex.width
            );
        }

        return cachedEnemySprite;
    }
}

