using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EconomyManager : MonoBehaviour
{
    [Header("Runtime State")]
    [SerializeField] private int currentGold;

    public int CurrentGold => currentGold;

    public event Action<int> GoldChanged;
    private bool spending;
    private int reserved;
    // Reserve silently before changing a tower. No callbacks between reserve and commit.
    public bool ReserveSpend(int amount)
    {
        if (spending || amount < 0 || currentGold < amount) return false;
        spending = true; reserved = amount; currentGold -= amount;
        return true;
    }
    public void FinishSpend(bool success)
    {
        if (!spending) return;
        if (!success) currentGold += reserved;
        spending = false; reserved = 0;
        if (success) GoldChanged?.Invoke(currentGold);
    }

    public void Initialize(int initialGold)
    {
        currentGold = Mathf.Max(0, initialGold);
        spending = false; reserved = 0;
        GoldChanged?.Invoke(currentGold);
    }

    public bool CanAfford(int amount)
    {
        return amount >= 0 && currentGold >= amount;
    }

    public bool TrySpend(int amount)
    {
        if (spending || amount < 0 || currentGold < amount)
            return false;

        currentGold -= amount;
        GoldChanged?.Invoke(currentGold);
        return true;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        currentGold += amount;
        GoldChanged?.Invoke(currentGold);
    }
}
