using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private EnemyData data;
    [SerializeField, Min(1)] private int maxHP = 60;
    [SerializeField, Range(0f, 1f)] private float armor;

    [Header("Runtime - inspect during Play Mode")]
    [SerializeField] private int currentHP;

    private Enemy enemy;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public bool IsDead => currentHP <= 0;

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        if (data != null)
        {
            Initialize(data);
        }
        else
        {
            ResetHealth();
        }
    }

    public void Initialize(EnemyData enemyData)
    {
        if (enemyData == null)
        {
            Debug.LogError($"{name}: Chưa gán EnemyData.", this);
            return;
        }

        data = enemyData;
        maxHP = Mathf.Max(1, data.maxHP);
        armor = Mathf.Clamp01(data.armor);

        ResetHealth();
    }

    public void ResetHealth()
    {
        maxHP = Mathf.Max(1, maxHP);
        currentHP = maxHP;

        HealthChanged?.Invoke(currentHP, maxHP);
    }

    public void TakeDamage(int rawDamage)
    {
        if (enemy == null)
            enemy = GetComponent<Enemy>();

        if (!enemy.IsAlive || currentHP <= 0)
            return;

        if (rawDamage <= 0)
            return;

        // Quy ước: đòn đánh hợp lệ gây tối thiểu 1 sát thương.
        int damage = Mathf.Max(
            1,
            Mathf.FloorToInt(rawDamage * (1f - armor) + 0.5f)
        );

        currentHP = Mathf.Max(0, currentHP - damage);

        Debug.Log(
            $"{name}: Nhận {damage} sát thương, " +
            $"còn {currentHP}/{maxHP} HP.",
            this
        );

        HealthChanged?.Invoke(currentHP, maxHP);

        if (currentHP == 0)
        {
            Debug.Log($"{name}: Đã hết máu.", this);
            Died?.Invoke();
        }
    }

    [ContextMenu("Debug/Deal 16 Damage")]
    private void DebugDeal16Damage()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Hãy nhấn Play trước khi thử.", this);
            return;
        }

        TakeDamage(16);
    }

    [ContextMenu("Debug/Set HP To 30")]
    private void DebugSetHPTo30()
    {
        if (!Application.isPlaying || currentHP <= 0)
            return;

        currentHP = Mathf.Min(30, maxHP);
        HealthChanged?.Invoke(currentHP, maxHP);

        Debug.Log($"{name}: HP = {currentHP}/{maxHP}", this);
    }

    [ContextMenu("Debug/Reset HP")]
    private void DebugResetHP()
    {
        if (!Application.isPlaying)
            return;

        ResetHealth();
    }
}