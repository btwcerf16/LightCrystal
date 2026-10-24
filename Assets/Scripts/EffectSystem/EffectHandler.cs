using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(ActorStats))]
[DisallowMultipleComponent]
public class EffectHandler : MonoBehaviour
{
    private readonly List<Effect> _activeEffects = new();
    private ActorStats _ownerStats;
    private NetworkObject _networkObject;

    public IReadOnlyList<Effect> ActiveEffects => _activeEffects;
    public event Action EffectsChanged;

    private bool CanProcess => _networkObject == null ||
        (_networkObject.IsSpawned && _networkObject.NetworkManager.IsServer);

    private void Awake()
    {
        _ownerStats = GetComponent<ActorStats>();
        _networkObject = GetComponent<NetworkObject>();
    }

    private void Update()
    {
        if (!CanProcess)
            return;

        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            if (i >= _activeEffects.Count)
                continue;

            Effect effect = _activeEffects[i];
            float deltaTime = Mathf.Min(Time.deltaTime, effect.TimeRemaining);
            effect.TimeRemaining -= deltaTime;
            effect.EffectTick(_ownerStats, deltaTime);

            if (effect.TimeRemaining <= 0f)
                RemoveEffect(effect);
        }
    }

    public void AddEffect(EffectData effectData)
    {
        if (!CanProcess || !isActiveAndEnabled || effectData == null || effectData.EffectDuration <= 0f)
            return;

        if (!effectData.Stackable)
        {
            Effect existing = _activeEffects.Find(effect => effect.EffectData == effectData);
            if (existing != null)
            {
                existing.TimeRemaining = effectData.EffectDuration;
                if (effectData.RestartOnReapply)
                {
                    existing.EffectEnd(_ownerStats);
                    existing.EffectStart(_ownerStats);
                }
                EffectsChanged?.Invoke();
                return;
            }
        }

        Effect newEffect = effectData.CreateEffect(gameObject);
        _activeEffects.Add(newEffect);
        newEffect.EffectStart(_ownerStats);
        EffectsChanged?.Invoke();
    }

    public void RemoveEffect(Effect effect)
    {
        if (!CanProcess || !_activeEffects.Remove(effect))
            return;

        effect.EffectEnd(_ownerStats);
        Destroy(effect);
        EffectsChanged?.Invoke();
    }

    public void PurgeEffect(EffectData effectData)
    {
        if (!CanProcess || effectData == null || !effectData.CanPurged)
            return;

        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            if (_activeEffects[i].EffectData == effectData)
                RemoveEffect(_activeEffects[i]);
        }
    }

    private void OnDisable()
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            Effect effect = _activeEffects[i];
            _activeEffects.RemoveAt(i);
            effect.EffectEnd(_ownerStats);
            Destroy(effect);
        }
        EffectsChanged?.Invoke();
    }
}
