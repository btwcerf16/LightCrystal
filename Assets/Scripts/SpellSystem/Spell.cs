using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public abstract class Spell : MonoBehaviour
{
    protected GameObject _owner;
    protected ActorStats _ownerStats;
    private NetworkObject _networkObject;
    protected ActorSanity _ownerSanity;
    protected EffectHandler _ownerEffects;

    public SpellConfig SpellData { get; private set; }
    public bool IsActive { get; private set; }
    public ActorStats OwnerStats => _ownerStats;
    public string Description => TooltipText.Format(this, SpellData.SpellDescription, SpellData);
    public string StatsDescription => TooltipText.Format(this, SpellData.StatsDescription, SpellData);
    public double CooldownEndsAt { get; private set; }
    public virtual int MaxCharges => SpellData.IsPassive ? 0 : Mathf.Max(1, SpellData.MaxCharges);
    public int AvailableCharges { get; private set; }
    private bool _isCasting;
    private float _castRemaining;
    public float CastProgress => _isCasting ? Mathf.Clamp01(1f - _castRemaining / SpellData.CastTime) : -1f;
    public float CastTime => SpellData.CastTime / _ownerStats.CastSpeed.Value;
    public virtual float SanityCost => GetReducedSanityCost(SpellData.SanityCost);
    protected float GetReducedSanityCost(float baseCost) => baseCost * (1f - _ownerStats.SanityCostReduction.Value);
    public float CooldownTime
    {
        get
        {
            float reduction = _ownerStats.CooldownReduction.Value + _ownerStats.SpellCooldownReduction.Value;
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
        ResetCharges();
    }

    public void SetOwner(GameObject owner)
    {
        _owner = owner;
        _ownerStats = owner.GetComponent<ActorStats>();
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
        if (!IsActive || !isActiveAndEnabled || SpellData.IsPassive || _isCasting)
            return false;
        RefreshCharges();
        if (AvailableCharges == 0)
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

        SpendCharge();
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

    private void SpendCharge()
    {
        float rechargeTime = CooldownTime;
        if (rechargeTime <= 0f)
            return;

        AvailableCharges--;
        if (CooldownEndsAt == 0d)
            CooldownEndsAt = CurrentTime + rechargeTime;
    }

    internal void RefreshCharges()
    {
        if (CooldownEndsAt == 0d)
            return;

        double now = CurrentTime;
        while (CooldownEndsAt <= now)
        {
            AvailableCharges++;
            if (AvailableCharges >= MaxCharges)
            {
                ResetCharges();
                return;
            }

            float rechargeTime = CooldownTime;
            if (rechargeTime <= 0f)
            {
                ResetCharges();
                return;
            }
            CooldownEndsAt += rechargeTime;
        }
    }

    internal void ResetCharges()
    {
        AvailableCharges = MaxCharges;
        CooldownEndsAt = 0d;
    }

    internal void ApplyChargeState(NetworkSpellChargeState state)
    {
        AvailableCharges = state.AvailableCharges;
        CooldownEndsAt = state.RechargesAt;
    }

    public virtual bool CanCast(SpellCastData data) => true;
    protected abstract void Cast(SpellCastData data);
}
