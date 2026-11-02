using UnityEngine;

public readonly struct EnemyHandle
{
    public Enemy Target { get; }
    public EnemyIdentity Identity { get; }

    public EnemyHandle(Enemy target)
    {
        Target = target;
        Identity = target != null ? target.Identity : default;
    }

    public bool IsAlive =>
        Identity.IsValid
        && Target != null
        && Target.isActiveAndEnabled
        && Target.IsAlive
        && Target.Identity.Equals(Identity);

    public Vector3 Position =>
        IsAlive ? Target.transform.position : Vector3.zero;

    public float Progress =>
        IsAlive ? Target.Progress : 0f;
}