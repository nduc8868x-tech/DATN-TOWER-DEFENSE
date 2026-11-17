using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy), typeof(EnemyMovement), typeof(Animator))]
public sealed class EnemyAnimationDriver : MonoBehaviour
{
    [SerializeField] private bool useRunAnimation;

    private Enemy enemy;
    private EnemyMovement movement;
    private EnemyHealth health;
    private Animator animator;
    private int previousHealth;

    private static readonly int Moving = Animator.StringToHash("Moving");
    private static readonly int Running = Animator.StringToHash("Running");
    private static readonly int Alive = Animator.StringToHash("Alive");
    private static readonly int Hurt = Animator.StringToHash("Hurt");

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        movement = GetComponent<EnemyMovement>();
        health = GetComponent<EnemyHealth>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (health == null) return;
        previousHealth = health.CurrentHP;
        health.HealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        if (health != null) health.HealthChanged -= OnHealthChanged;
    }

    private void Update()
    {
        animator.SetBool(Alive, enemy.IsAlive);
        animator.SetBool(Moving, enemy.IsAlive && movement.IsMoving);
        animator.SetBool(Running, useRunAnimation);
    }

    private void OnHealthChanged(int current, int maximum)
    {
        if (current > 0 && current < previousHealth && enemy.IsAlive)
            animator.SetTrigger(Hurt);
        previousHealth = current;
    }
}
