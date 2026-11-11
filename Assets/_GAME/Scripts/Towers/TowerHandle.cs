using UnityEngine;

// A reference type deliberately shares disposal state between callers.
public class TowerHandle : System.IDisposable
{
    private Tower instance;
    private readonly int sessionId;
    internal Tower Instance => instance;
    public virtual bool IsValid => instance != null && instance.HasCurrentSession && instance.SessionId == sessionId;
    protected TowerHandle() { }
    internal TowerHandle(Tower tower) { instance = tower; sessionId = tower.SessionId; }
    public bool ReadSnapshot(out TowerSnapshot snapshot)
    {
        snapshot = ReadSnapshot();
        return !snapshot.IsDisposed;
    }
    public virtual TowerSnapshot ReadSnapshot() => IsValid ? new TowerSnapshot(instance) : default;
    public virtual bool Activate() => IsValid && instance.Activate();
    public virtual bool Suspend()
    {
        if (!IsValid) return false;
        instance.Suspend();
        return true;
    }
    public virtual bool TryApplyLevel(int nextLevel) => IsValid && instance.TryApplyLevel(nextLevel, out _);
    public virtual void Dispose()
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
