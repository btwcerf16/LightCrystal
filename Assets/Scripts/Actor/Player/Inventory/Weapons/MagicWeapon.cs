using UnityEngine;

public abstract class MagicWeapon : Weapon
{
    [SerializeField] protected Transform _grip;
    [SerializeField] private string _displayName;
    [SerializeField] private float _sanityBonus;
    [SerializeField] private float _castSpeedBonus;
    [SerializeField, Min(0.1f)] protected float _range = 3f;
    [SerializeField, Min(0f)] protected float _primarySanityCost;

    private EffectHandler _effects;
    private ActorSanity _sanity;

    protected NetworkPlayerArsenal NetworkArsenal { get; private set; }
    protected bool CanAttack => IsServer && IsEquipped && IsAvailable && _hand != null
        && !IsOwnerStunned && (_effects == null || (!_effects.IsDowned && !_effects.IsSilenced));
    protected bool CanStartAttack => CanAttack && !_isAttacking;
    protected bool CanAnimate => IsEquipped && _hand != null && _grip != null
        && NetworkArsenal != null && NetworkArsenal.IsClient;
    protected float CastSpeed => Mathf.Max(0.01f, OwnerStats?.CastSpeed.Value ?? 1f);
    public override string DisplayName => _displayName;

    public override void Equip()
    {
        if (IsEquipped)
            return;

        base.Equip();
        _effects = OwnerStats.GetComponent<EffectHandler>();
        _sanity = OwnerStats.GetComponent<ActorSanity>();
        NetworkArsenal = OwnerStats.GetComponent<NetworkPlayerArsenal>();
        if (!IsServer)
            return;

        OwnerStats.MaxSanity.AddModifier(new StatModifier(_sanityBonus, StatModifierType.AdditivePercent, this));
        if (_castSpeedBonus != 0f)
            OwnerStats.CastSpeed.AddModifier(new StatModifier(_castSpeedBonus, StatModifierType.AdditivePercent, this));
    }

    public override void Drop()
    {
        CancelUse();
        if (OwnerStats != null)
        {
            OwnerStats.MaxSanity.RemoveModifiersFrom(this);
            OwnerStats.CastSpeed.RemoveModifiersFrom(this);
        }
        base.Drop();
    }

    protected bool CheckSanity(float cost, bool spend)
    {
        if (cost <= 0f || (_sanity != null && (spend ? _sanity.TrySpendSanity(cost) : _sanity.CurrentSanity >= cost)))
            return true;

        OwnerNetworkObject.GetComponent<NetworkPlayerWallet>()?.ShowHint("Недостаточно рассудка");
        return false;
    }

    public override void EndPrimaryUse() { }
    public override void EndSecondaryUse() { }
    public override void Use() { }
}
