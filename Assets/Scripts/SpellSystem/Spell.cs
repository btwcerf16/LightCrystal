using System;
using Unity.Netcode;
using UnityEngine;

public abstract class Spell : MonoBehaviour
{
    protected GameObject _owner;
    protected ActorStats _ownerStats;
    private NetworkObject _networkObject;
    private ActorSanity _ownerSanity;

    public SpellConfig SpellData { get; private set; }
    public bool IsActive { get; private set; }
    public double CooldownEndsAt { get; private set; }
    public float CooldownTime => SpellData.CooldownTime * (1f - _ownerStats.CooldownReduction.Value);
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
        _networkObject = owner.GetComponent<NetworkObject>();
        _ownerSanity = owner.GetComponent<ActorSanity>();
    }

    public virtual void Activate()
    {
        IsActive = true;
    }

    public virtual void Deactivate()
    {
        IsActive = false;
    }

    public bool TryCast(SpellCastData data)
    {
        data.Caster = _owner;
        if (!IsActive || !isActiveAndEnabled || SpellData.IsPassive || CooldownRemaining > 0f || !CanCast(data))
            return false;

        if (SpellData.SanityCost > 0f && (_ownerSanity == null || !_ownerSanity.TrySpendSanity(SpellData.SanityCost)))
            return false;

        CooldownEndsAt = CurrentTime + CooldownTime;
        Cast(data);
        OnCast?.Invoke(data);
        return true;
    }

    public void SetCooldown(double endsAt)
    {
        CooldownEndsAt = endsAt;
    }

    public virtual bool CanCast(SpellCastData data) => true;
    protected abstract void Cast(SpellCastData data);
}
