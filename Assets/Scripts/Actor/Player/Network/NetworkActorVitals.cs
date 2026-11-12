using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(ActorHealth))]
public sealed class NetworkActorVitals : NetworkBehaviour
{
    private readonly NetworkVariable<Vector4> _state = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<byte> _reviveProgress = new();
    private ActorHealth _health;
    private ActorSanity _sanity;

    public Vector4 State => _state.Value;
    public float ReviveProgress => _reviveProgress.Value / 100f;
    public event Action<Vector4> Changed;

    private void Awake()
    {
        _health = GetComponent<ActorHealth>();
        _sanity = GetComponent<ActorSanity>();
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer)
            return;
        _reviveProgress.Value = (byte)Mathf.RoundToInt(_health.ReviveProgress * 100f);
        if (_sanity != null)
            _sanity.ProcessDarkness(Time.deltaTime);
    }

    public override void OnNetworkSpawn()
    {
        _state.OnValueChanged += Receive;
        if (IsServer)
        {
            _health.HealthChanged += Publish;
            if (_sanity != null)
            {
                _sanity.SanityChanged += Publish;
                NetworkManager.GetComponent<NetworkWaveDirector>()?.ApplyBossFightEffect(gameObject);
            }
            Publish(0f, 0f);
        }
        Changed?.Invoke(State);
    }

    public override void OnNetworkDespawn()
    {
        _state.OnValueChanged -= Receive;
        if (_health != null)
            _health.HealthChanged -= Publish;
        if (_sanity != null)
            _sanity.SanityChanged -= Publish;
    }

    private void Publish(float current, float maximum)
    {
        _state.Value = new Vector4(_health.CurrentHealth, _health.MaxHealth,
            _sanity != null ? _sanity.CurrentSanity : 0f, _sanity != null ? _sanity.MaxSanity : 0f);
    }

    private void Receive(Vector4 previous, Vector4 current)
    {
        Changed?.Invoke(current);
    }
}
