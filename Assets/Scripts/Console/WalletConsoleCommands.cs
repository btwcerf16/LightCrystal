using System;
using UnityEngine;

[RequireComponent(typeof(ActorWallet))]
public sealed class WalletConsoleCommands : MonoBehaviour
{
    [ConsoleCommand]
    private string GiveMoney(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Количество должно быть больше нуля.");
        ActorWallet wallet = GetComponent<ActorWallet>();
        wallet.AddCurrency(amount);
        return $"Валюта: {wallet.Currency}.";
    }
}
