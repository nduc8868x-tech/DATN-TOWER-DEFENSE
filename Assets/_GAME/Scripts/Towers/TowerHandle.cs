using UnityEngine;

// A reference type deliberately shares disposal state between callers.
public sealed class TowerHandle : System.IDisposable
{
    private Tower instance;
    private readonly int sessionId;
    internal Tower Instance => instance;
    public bool IsValid => instance != null && instance.HasCurrentSession && instance.SessionId == sessionId;
    internal TowerHandle(Tower tower) { instance = tower; sessionId = tower.SessionId; }
    public bool ReadSnapshot(out TowerSnapshot snapshot)
    {
        snapshot = default;
        if (!IsValid) return false;
        snapshot = new TowerSnapshot(instance);
        return true;
    }
    public bool Activate() => IsValid && instance.Activate();
    public bool Suspend()
    {
        if (!IsValid) return false;
        instance.Suspend();
        return true;
    }
    public bool TryApplyLevel(int nextLevel) => IsValid && instance.TryApplyLevel(nextLevel, out _);
    public void Dispose()
    {
        if (instance != null)
        {
            instance.Suspend();
            instance.gameObject.SetActive(false);
            Object.Destroy(instance.gameObject);
        }
        instance = null;
    }
}
