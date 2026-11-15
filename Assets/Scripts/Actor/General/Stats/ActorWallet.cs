using System;
using UnityEngine;

public sealed class ActorWallet : MonoBehaviour
{
    [SerializeField, Min(0)] private int _startingCurrency;

    public int Currency { get; private set; }
    public event Action<int> CurrencyChanged;
    public event Action<int, Vector2> CurrencyGained;

    private void Awake()
    {
        Currency = Mathf.Max(0, _startingCurrency);
    }

    public bool CanAfford(int amount)
    {
        return amount >= 0 && Currency >= amount;
    }

    public bool TrySpend(int amount)
    {
        if (!CanAfford(amount))
            return false;
        SetCurrency(Currency - amount);
        return true;
    }

    public void AddCurrency(int amount, Vector2? sourcePosition = null)
    {
        int gained = Mathf.Min(amount, int.MaxValue - Currency);
        if (gained <= 0)
            return;

        SetCurrency(Currency + gained);
        CurrencyGained?.Invoke(gained, sourcePosition ?? (Vector2)transform.position);
    }

    public void SetCurrency(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (Currency == amount)
            return;
        Currency = amount;
        CurrencyChanged?.Invoke(Currency);
    }
}
