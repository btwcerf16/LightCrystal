using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(EffectHandler))]
public sealed class NetworkEffectHandler : NetworkBehaviour
{
    [SerializeField] private EffectData[] _effectData = new EffectData[0];
    private NetworkList<NetworkEffectSlot> _effects;
    private EffectHandler _handler;

    public int Count => _effects.Count;
    public event Action EffectsChanged;
    public NetworkEffectSlot GetEffect(int index) => _effects[index];
    public EffectData GetData(int index) => index >= 0 && index < _effectData.Length ? _effectData[index] : null;

    private void Awake()
    {
        _effects = new NetworkList<NetworkEffectSlot>();
        _handler = GetComponent<EffectHandler>();
    }

    public override void OnNetworkSpawn()
    {
        _effects.OnListChanged += OnEffectsChanged;
        if (IsServer)
        {
            _handler.EffectsChanged += PublishEffects;
            PublishEffects();
        }
    }

    public override void OnNetworkDespawn()
    {
        _effects.OnListChanged -= OnEffectsChanged;
        _handler.EffectsChanged -= PublishEffects;
    }

    private void OnEffectsChanged(NetworkListEvent<NetworkEffectSlot> change)
    {
        EffectsChanged?.Invoke();
    }

    private void PublishEffects()
    {
        int count = 0;
        for (int i = 0; i < _handler.ActiveEffects.Count; i++)
        {
            Effect effect = _handler.ActiveEffects[i];
            int effectIndex = Array.IndexOf(_effectData, effect.EffectData);
            if (effectIndex < 0)
            {
                Debug.LogError($"Effect {effect.EffectData.name} is missing from NetworkEffectHandler's list.", this);
                continue;
            }

            NetworkEffectSlot slot = new NetworkEffectSlot
            {
                InstanceId = effect.GetInstanceID(),
                EffectIndex = effectIndex,
                EndsAt = NetworkManager.ServerTime.Time + effect.TimeRemaining
            };
            if (count >= _effects.Count)
                _effects.Add(slot);
            else if (!_effects[count].Equals(slot))
                _effects[count] = slot;
            count++;
        }

        while (_effects.Count > count)
            _effects.RemoveAt(_effects.Count - 1);
    }
}
