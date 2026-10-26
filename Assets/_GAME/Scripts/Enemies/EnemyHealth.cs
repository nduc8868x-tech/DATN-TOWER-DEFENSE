using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField, Min(1)] private int maxHP = 60;

    [Header("Runtime - inspect during Play Mode")]
    [SerializeField] private int currentHP;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;

    private void Awake()
    {
        ResetHealth();
    }

    public void ResetHealth()
    {
        maxHP = Mathf.Max(1, maxHP);
        currentHP = maxHP;
    }

    [ContextMenu("Debug/Set HP To 30")]
    private void DebugSetHPTo30()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "Enter Play Mode before testing HP.",
                this
            );
            return;
        }

        currentHP = Mathf.Min(30, maxHP);

        Debug.Log(
            $"{name}: HP = {currentHP}/{maxHP}",
            this
        );
    }

    [ContextMenu("Debug/Reset HP")]
    private void DebugResetHP()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ResetHealth();
    }
}