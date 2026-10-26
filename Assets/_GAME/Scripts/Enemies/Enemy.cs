using UnityEngine;

public enum EnemyState
{
    Alive,
    Dead,
    Escaped
}

[DisallowMultipleComponent]
public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyState state = EnemyState.Alive;

    public EnemyState State => state;
    public bool IsAlive => state == EnemyState.Alive;

    private void Awake()
    {
        state = EnemyState.Alive;
    }
}