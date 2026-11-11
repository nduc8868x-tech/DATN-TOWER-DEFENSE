using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EconomyManager : MonoBehaviour
{
    [Header("Runtime State")]
    [SerializeField] private int currentGold;

    public int CurrentGold => currentGold;

    public event Action<int> GoldChanged;

    public void Initialize(int initialGold)
    {
        currentGold = Mathf.Max(0, initialGold);
        GoldChanged?.Invoke(currentGold);
    }

    public bool CanAfford(int amount)
    {
        return amount >= 0 && currentGold >= amount;
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0 || currentGold < amount)
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
