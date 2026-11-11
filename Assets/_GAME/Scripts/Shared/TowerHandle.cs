public abstract class TowerHandle
{
    public abstract TowerSnapshot ReadSnapshot();
    public abstract void Activate();
    public abstract void Suspend();
    public abstract bool TryApplyLevel(int targetLevel);
    public abstract void Dispose();
}
