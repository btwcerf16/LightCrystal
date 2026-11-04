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
    public bool HasEffect(EffectData data)
    {
        foreach (Effect effect in _activeEffects)
            if (effect.EffectData == data)
                return true;
        return false;
    }

    public event Action EffectsChanged;
    public event Action StatusChanged;
    public EActorStatus Status { get; private set; }
    public bool IsStunned => (Status & EActorStatus.Stunned) != 0;
    public bool IsSilenced => (Status & EActorStatus.Silenced) != 0;

    internal void SetStatus(EActorStatus status)
    {
        if (Status == status)
            return;
        Status = status;
        StatusChanged?.Invoke();
    }

    private void RefreshStatus()
    {
        EActorStatus status = EActorStatus.None;
        foreach (Effect effect in _activeEffects)
            status |= effect.EffectData.Status;
        SetStatus(status);
    }

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

    public Effect AddEffect(EffectData effectData)
    {
        if (!CanProcess || !isActiveAndEnabled || effectData == null || (!effectData.IsPermanent && effectData.EffectDuration <= 0f))
            return null;

        float duration = effectData.IsPermanent ? float.PositiveInfinity : effectData.EffectDuration;
        if (!effectData.IsBuff && effectData.Status != EActorStatus.None)
            duration *= 1f - Mathf.Clamp01(_ownerStats.EffectResistance.Value);
        if (duration <= 0f)
            return null;

        if (!effectData.Stackable)
        {
            Effect existing = _activeEffects.Find(effect => effect.EffectData == effectData);
            if (existing != null)
            {
                existing.TimeRemaining = duration;
                if (effectData.RestartOnReapply)
                {
                    existing.EffectEnd(_ownerStats);
                    existing.EffectStart(_ownerStats);
                }
                RefreshStatus();
                EffectsChanged?.Invoke();
                return existing;
            }
        }

        Effect newEffect = effectData.CreateEffect(gameObject);
        newEffect.TimeRemaining = duration;
        _activeEffects.Add(newEffect);
        newEffect.EffectStart(_ownerStats);
        RefreshStatus();
        EffectsChanged?.Invoke();
        return newEffect;
    }

    public void RemoveEffect(Effect effect)
    {
        if (!CanProcess || !_activeEffects.Remove(effect))
            return;

        effect.EffectEnd(_ownerStats);
        Destroy(effect);
        RefreshStatus();
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
        RefreshStatus();
        EffectsChanged?.Invoke();
    }
}
