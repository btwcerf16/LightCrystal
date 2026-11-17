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
    public event Action<int, Vector2> CurrencyGainReceived;

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
            _wallet.CurrencyGained += ShowCurrencyGainRpc;
            Publish(_wallet.Currency);
        }
        else
            _wallet.SetCurrency(_currency.Value);
    }

    public override void OnNetworkDespawn()
    {
        _currency.OnValueChanged -= Receive;
        _wallet.CurrencyChanged -= Publish;
        _wallet.CurrencyGained -= ShowCurrencyGainRpc;
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

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void ShowCurrencyGainRpc(int amount, Vector2 sourcePosition)
    {
        if (TryGetComponent(out WorldAudio audio))
            audio.Play((int)WorldSoundId.GoldReceived, transform.position);
        CurrencyGainReceived?.Invoke(amount, sourcePosition);
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
