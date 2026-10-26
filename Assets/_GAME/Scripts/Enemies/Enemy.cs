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
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyMovement movement;

    [Header("Runtime")]
    [SerializeField] private EnemyState state = EnemyState.Alive;

    private EnemyHealth health;

    private static readonly int DieTrigger =
        Animator.StringToHash("Die");

    public EnemyState State => state;
    public bool IsAlive => state == EnemyState.Alive;

    private void Awake()
    {
        state = EnemyState.Alive;

        health = GetComponent<EnemyHealth>();

        if (movement == null)
            movement = GetComponent<EnemyMovement>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.Died += HandleDeath;

        if (movement != null)
            movement.ReachedEnd += HandleReachedEnd;
    }

    private void OnDisable()
    {
        if (health != null)
            health.Died -= HandleDeath;

        if (movement != null)
            movement.ReachedEnd -= HandleReachedEnd;
    }

    private void HandleDeath()
    {
        if (!IsAlive)
            return;

        state = EnemyState.Dead;

        if (movement != null)
            movement.StopMovement();

        if (animator != null)
            animator.SetTrigger(DieTrigger);
        else
            Debug.LogWarning($"{name}: Chưa gán Animator.", this);

        Debug.Log($"{name}: Đã chuyển sang trạng thái Dead.", this);
    }

    private void HandleReachedEnd(EnemyMovement source)
    {
        if (!IsAlive)
            return;

        state = EnemyState.Escaped;

        if (movement != null)
            movement.StopMovement();

        Debug.Log(
            $"{name}: Đã tới đích, chuyển sang Escaped.",
            this
        );

        // Tạm ẩn quái khi đã thoát.
        // Chưa trừ mạng căn cứ ở bước này.
        gameObject.SetActive(false);
    }
}