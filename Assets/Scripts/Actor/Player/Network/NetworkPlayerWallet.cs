using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(ActorWallet))]
public sealed class NetworkPlayerWallet : NetworkBehaviour
{
    private readonly NetworkVariable<int> _currency = new(0,
        NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
    private ActorWallet _wallet;

    public event Action<string> HintRequested;

    private void Awake()
    {
        _wallet = GetComponent<ActorWallet>();
    }

    public override void OnNetworkSpawn()
    {
        _currency.OnValueChanged += Receive;
        if (IsServer)
        {
            _wallet.CurrencyChanged += Publish;
            Publish(_wallet.Currency);
        }
        else
            _wallet.SetCurrency(_currency.Value);
    }

    public override void OnNetworkDespawn()
    {
        _currency.OnValueChanged -= Receive;
        _wallet.CurrencyChanged -= Publish;
    }

    private void Publish(int currency)
    {
        _currency.Value = currency;
    }

    private void Receive(int previous, int current)
    {
        if (!IsServer)
            _wallet.SetCurrency(current);
    }

    public void ShowInsufficientFunds()
    {
        ShowHint("Недостаточно средств");
    }

    public void ShowHint(string message)
    {
        if (IsSpawned && IsServer)
            ShowHintRpc(message);
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void ShowHintRpc(string message)
    {
        HintRequested?.Invoke(message);
    }
}
