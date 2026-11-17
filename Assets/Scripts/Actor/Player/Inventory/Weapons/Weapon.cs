using UnityEngine;
using System;

public abstract class Weapon : Item
{
    [SerializeField] protected float _damage;
    [SerializeField] protected Transform _attackPoint;
    [SerializeField] protected Animator _animator;
    [SerializeField, Min(1)] private int _maxTargets = 1;
    [SerializeField] private EAttackType _primaryAttackType;
    [SerializeField] private EAttackType _secondaryAttackType;
    protected ActorHand _hand;
    protected bool _isAttacking;
    private Stat _animationSpeedStat;
    private float? _physicalDamageOverride;
    public EAttackType PrimaryAttackType => _primaryAttackType;
    public EAttackType SecondaryAttackType => _secondaryAttackType;
    public virtual float ChargeProgress => -1f;

    public bool IsAvailable { get; private set; } = true;
    public bool IsAttacking => _isAttacking;
    public delegate void TargetLimitModifier(ref int count);
    public event TargetLimitModifier ModifyTargetLimit;
    public event Action Equipped;
    public event Action Unequipped;
    public event Action<IDamageable, EAttackType, Vector2> Hit;
    public event Action<ActorHealth> Killed;

    public virtual int TargetLimit
    {
        get
        {
            int count = _maxTargets;
            ModifyTargetLimit?.Invoke(ref count);
            return Mathf.Max(1, count);
        }
    }

    public virtual void SetAvailable(bool available)
    {
        if (IsAvailable == available)
            return;
        IsAvailable = available;
        StateChanged?.Invoke();
    }

    public ActorStats OwnerStats => _ownerStats;
    public event Action StateChanged;
    protected internal void NotifyStateChanged() => StateChanged?.Invoke();

    public void NotifyMeleeHit(IDamageable target, Vector2 impactPoint)
    {
        NotifyHit(target, EAttackType.Melee, impactPoint);
        if (IsServer)
            OwnerStats.GetComponent<ActorArsenal>()?.NotifyMeleeHit(target);
    }

    public void NotifyHit(IDamageable target, EAttackType attackType, Vector2 impactPoint = default)
    {
        if (!IsServer)
            return;
        Hit?.Invoke(target, attackType, impactPoint);
        if (target.IsDead && target is ActorHealth health)
            NotifyKill(health);
    }

    public void NotifyKill(ActorHealth target)
    {
        if (IsServer)
            Killed?.Invoke(target);
    }

    public float Damage => _damage;

    public override string TooltipDescription => GetWeaponTooltip(false);

    public virtual string GetWeaponTooltip(bool detailed) => base.TooltipDescription;

    protected string FormatDamage(float damage, EDamageType type, bool detailed, bool usesActorDamage = false, float attackMultiplier = 1f)
    {
        float rawDamage = usesActorDamage && OwnerStats != null ? OwnerStats.PhysicalDamage.Value : damage;
        float total = DamageCalculator.CalculateDamage(rawDamage * attackMultiplier, type, OwnerStats, null);
        return FormatTooltipValue(damage * attackMultiplier, total, detailed);
    }

    protected string FormatProjectile(float speed, float range, bool detailed)
    {
        float totalSpeed = OwnerStats != null ? OwnerStats.GetProjectileSpeed(speed) : speed;
        float totalRange = OwnerStats != null ? OwnerStats.GetProjectileRange(range) : range;
        return $"Дальность: {FormatTooltipValue(range, totalRange, detailed)}\nСкорость снаряда: {FormatTooltipValue(speed, totalSpeed, detailed)}";
    }

    protected string FormatAttackSpeed(EAttackType type, bool detailed)
    {
        Stat speed = null;
        if (OwnerStats != null)
            speed = type == EAttackType.Magic ? OwnerStats.CastSpeed : OwnerStats.AttackSpeed;
        return speed == null ? "1" : FormatTooltipValue(speed.BaseValue, speed.Value, detailed);
    }

    protected static string FormatTooltipValue(float baseValue, float total, bool detailed)
    {
        if (!detailed)
            return $"{total:0.##}";

        float bonus = total - baseValue;
        string sign = bonus < 0f ? "−" : "+";
        return $"{baseValue:0.##} {sign} {Mathf.Abs(bonus):0.##} = {total:0.##}";
    }

    public override void Equip()
    {
        base.Equip();
        _physicalDamageOverride = null;
        _hand = GetComponentInParent<ActorHand>();
        Equipped?.Invoke();
        SetAttackType(_primaryAttackType);
    }

    internal void BindForUse(ActorStats owner, ActorHand hand, float physicalDamage)
    {
        BindOwner(owner.transform);
        _hand = hand;
        _physicalDamageOverride = physicalDamage;
        SetAttackType(_primaryAttackType);
    }

    protected float PhysicalDamage => _physicalDamageOverride ?? OwnerStats?.PhysicalDamage.Value ?? _damage;

    internal void UnbindForUse()
    {
        CancelUse();
        ClearAnimationSpeed();
        base.Drop();
    }

    public override void Drop()
    {
        Unequipped?.Invoke();
        ClearAnimationSpeed();
        base.Drop();
    }

    private void ClearAnimationSpeed()
    {
        if (_animationSpeedStat != null)
            _animationSpeedStat.ValueChanged -= UpdateAnimationSpeed;
        _animationSpeedStat = null;
        UpdateAnimationSpeed(1f);
    }

    protected void SetAttackType(EAttackType attackType)
    {
        if (_animationSpeedStat != null)
            _animationSpeedStat.ValueChanged -= UpdateAnimationSpeed;
        _animationSpeedStat = null;
        if (OwnerStats != null)
        {
            _animationSpeedStat = OwnerStats.AttackSpeed;
            if (attackType == EAttackType.Magic)
                _animationSpeedStat = OwnerStats.CastSpeed;
            _animationSpeedStat.ValueChanged += UpdateAnimationSpeed;
        }
        UpdateAnimationSpeed(_animationSpeedStat != null ? _animationSpeedStat.Value : 1f);
    }

    private void UpdateAnimationSpeed(float value)
    {
        if (_animator != null)
            _animator.SetFloat("AttackSpeed", Mathf.Max(0f, value));
    }

    protected virtual void OnDestroy()
    {
        if (_animationSpeedStat != null)
            _animationSpeedStat.ValueChanged -= UpdateAnimationSpeed;
    }

    public abstract bool BeginPrimaryUse();
    public abstract void EndPrimaryUse();
    public virtual bool BeginSecondaryUse() => false;
    public abstract void EndSecondaryUse();
    public abstract void CancelUse();
    public virtual void SetAim(Vector2 aim) { }
}

