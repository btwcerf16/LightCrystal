using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public abstract class Spell : MonoBehaviour
{
    protected GameObject _owner;
    protected ActorStats _ownerStats;
    private NetworkObject _networkObject;
    private ActorSanity _ownerSanity;
    private Inventory _inventory;
    private EffectHandler _ownerEffects;

    public SpellConfig SpellData { get; private set; }
    public bool IsActive { get; private set; }
    public ActorStats OwnerStats => _ownerStats;
    public string Description => TooltipText.Format(this, SpellData.SpellDescription, SpellData);
    public string StatsDescription => TooltipText.Format(this, SpellData.StatsDescription, SpellData);
    public double CooldownEndsAt { get; private set; }
    private bool _isCasting;
    private float _castRemaining;
    public float CastProgress => _isCasting ? Mathf.Clamp01(1f - _castRemaining / SpellData.CastTime) : -1f;
    public float CastTime => SpellData.CastTime / _ownerStats.CastSpeed.Value;
    public virtual bool IsToggled => false;
    public virtual float SanityCost => SpellData.SanityCost * (1f - _ownerStats.SanityCostReduction.Value);
    private bool HasFreeCooldowns
    {
        get
        {
            if (_ownerEffects != null)
                foreach (Effect effect in _ownerEffects.ActiveEffects)
                    if (effect.EffectData is ItemBuffData buff && buff.FreeSpellCooldowns)
                        return true;
            NetworkEffectHandler network = _owner.GetComponent<NetworkEffectHandler>();
            if (network != null && network.IsSpawned && !network.IsServer)
                for (int i = 0; i < network.Count; i++)
                    if (network.GetData(network.GetEffect(i).EffectIndex) is ItemBuffData buff && buff.FreeSpellCooldowns)
                        return true;
            return false;
        }
    }

    public float CooldownTime
    {
        get
        {
            if (HasFreeCooldowns)
                return 0f;
            float reduction = _ownerStats.CooldownReduction.Value;
            if (_inventory != null)
                reduction += _inventory.SpellCooldownReduction;
            return SpellData.CooldownTime * (1f - Mathf.Clamp(reduction, 0f, 0.9f));
        }
    }
    public float CooldownRemaining => Mathf.Max(0f, (float)(CooldownEndsAt - CurrentTime));
    private double CurrentTime => _networkObject != null && _networkObject.IsSpawned
        ? _networkObject.NetworkManager.ServerTime.Time : Time.timeAsDouble;

    public event Action<SpellCastData> OnCast;

    public void Initialize(SpellConfig config)
    {
        SpellData = config;
    }

    public void SetOwner(GameObject owner)
    {
        _owner = owner;
        _ownerStats = owner.GetComponent<ActorStats>();
        _inventory = owner.GetComponent<Inventory>();
        _networkObject = owner.GetComponent<NetworkObject>();
        _ownerSanity = owner.GetComponent<ActorSanity>();
        _ownerEffects = owner.GetComponent<EffectHandler>();
    }

    public virtual void Activate()
    {
        IsActive = true;
    }

    public virtual void Deactivate()
    {
        IsActive = false;
        CancelCast();
    }

    public bool TryCast(SpellCastData data, out string error)
    {
        error = null;
        data.Caster = _owner;
        if (_ownerEffects != null && _ownerEffects.IsDowned)
            return false;
        if (!IsActive || !isActiveAndEnabled || SpellData.IsPassive || _isCasting || CooldownRemaining > 0f)
            return false;

        if (_ownerEffects != null && (_ownerEffects.IsStunned || _ownerEffects.IsSilenced))
        {
            error = _ownerEffects.IsStunned ? "Вы оглушены" : "Вы под действием безмолвия";
            return false;
        }

        if (!CanCast(data))
            return false;

        if (SanityCost > 0f && (_ownerSanity == null || !_ownerSanity.TrySpendSanity(SanityCost)))
        {
            error = "Недостаточно рассудка";
            return false;
        }

        CooldownEndsAt = CurrentTime + CooldownTime;
        if (CastTime > 0f)
            StartCoroutine(CompleteCast(data));
        else
        {
            Cast(data);
            OnCast?.Invoke(data);
        }
        return true;
    }

    private IEnumerator CompleteCast(SpellCastData data)
    {
        _isCasting = true;
        _castRemaining = SpellData.CastTime;
        ActorHealth health = _owner.GetComponent<ActorHealth>();
        while (_castRemaining > 0f)
        {
            yield return null;
            if (!IsActive || (health != null && health.IsDead) ||
                (_ownerEffects != null && (_ownerEffects.IsStunned || _ownerEffects.IsSilenced)))
            {
                _isCasting = false;
                yield break;
            }
            _castRemaining -= Time.deltaTime * _ownerStats.CastSpeed.Value;
        }
        Cast(data);
        OnCast?.Invoke(data);
        _isCasting = false;
    }

    private void OnDisable()
    {
        CancelCast();
    }

    public void CancelCast()
    {
        StopAllCoroutines();
        _isCasting = false;
    }

    public void SetCooldown(double endsAt)
    {
        CooldownEndsAt = endsAt;
    }

    public virtual bool CanCast(SpellCastData data) => true;
    protected abstract void Cast(SpellCastData data);
}
