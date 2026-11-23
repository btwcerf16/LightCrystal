using System.Collections.Generic;
using UnityEngine;

public sealed class MagicWandWeapon : MagicWeapon
{
    [SerializeField] private string _beamEffectId = "wand-beam";
    [SerializeField] private string _chargedBeamEffectId = "wand-beam-charged";
    [SerializeField, Min(0.01f)] private float _primaryCastTime = 0.18f;
    [SerializeField, Min(0.1f)] private float _chargeTime = 0.65f;
    [SerializeField, Min(0f)] private float _chargedSanityCost;
    [SerializeField, Min(0f)] private float _chargedDamage = 8f;
    [SerializeField, Min(0.1f)] private float _chargedRange = 6f;

    private readonly List<RaycastHit2D> _hits = new();
    private readonly HashSet<IDamageable> _targets = new();
    private float _chargeEndsAt;
    private float _castDuration;
    private bool _chargedCast;

    public override float GetRange(ActorStats owner) => _range;
    public override float ChargeProgress => _isAttacking
        ? 1f - Mathf.Clamp01((_chargeEndsAt - Time.time) / _castDuration)
        : -1f;

    public override string GetWeaponTooltip(bool detailed) => base.GetWeaponTooltip(detailed)
        + $"\n\nСкорость каста: {FormatAttackSpeed(PrimaryAttackType, detailed)}"
        + $"\nЛКМ: луч на {_range:0.##}, {FormatDamage(_damage, EDamageType.Magic, detailed)} маг. урона всем целям на пути."
        + $" Время каста: {FormatTooltipValue(_primaryCastTime, _primaryCastTime / CastSpeed, detailed)} с. Рассудок: {_primarySanityCost:0.##}"
        + $"\n\nПКМ: удерживайте {FormatTooltipValue(_chargeTime, _chargeTime / CastSpeed, detailed)} с. Луч на {_chargedRange:0.##}, "
        + $"{FormatDamage(_chargedDamage, EDamageType.Magic, detailed)} маг. урона всем целям на пути."
        + $" Рассудок: {_chargedSanityCost:0.##}";

    public override bool BeginPrimaryUse()
    {
        if (!IsEquipped || (IsServer && !BeginCast(charged: false)))
            return false;

        _animator.Play("Fire", 0, 0f);
        return true;
    }

    public override bool BeginSecondaryUse()
    {
        if (!IsEquipped || (IsServer && !BeginCast(charged: true)))
            return false;

        _animator.Play("Charge", 0, 0f);
        return true;
    }

    private bool BeginCast(bool charged)
    {
        float cost = charged ? _chargedSanityCost : _primarySanityCost;
        if (!CanStartAttack || !CheckSanity(cost, spend: false))
            return false;

        _chargedCast = charged;
        _castDuration = (charged ? _chargeTime : _primaryCastTime) / CastSpeed;
        _chargeEndsAt = Time.time + _castDuration;
        _isAttacking = true;
        return true;
    }

    private void Update()
    {
        if (!_isAttacking)
            return;
        if (!CanAttack)
        {
            CancelUse();
            return;
        }
        if (Time.time < _chargeEndsAt)
            return;

        bool charged = _chargedCast;
        _isAttacking = false;
        if (CheckSanity(charged ? _chargedSanityCost : _primarySanityCost, spend: true))
            Fire(charged);
    }

    private void Fire(bool charged)
    {
        float damage = charged ? _chargedDamage : _damage;
        float range = charged ? _chargedRange : _range;
        Vector2 origin = _attackPoint.position;
        Vector2 aim = _hand.AimDirection;
        Vector2 end = origin + aim * range;

        ContactFilter2D filter = new() { useTriggers = true };
        filter.SetLayerMask(DamageCalculator.GetTargetMask(OwnerStats, _targetMask));
        Physics2D.Raycast(origin, aim, filter, _hits, range);
        _hits.Sort(static (first, second) => first.distance.CompareTo(second.distance));

        foreach (RaycastHit2D hit in _hits)
        {
            Collider2D collider = hit.collider;
            if (collider == null || collider.GetComponentInParent<ActorStats>() == OwnerStats)
                continue;

            IDamageable target = collider.GetComponentInParent<IDamageable>();
            if (target == null)
            {
                if (collider.isTrigger)
                    continue;

                end = hit.point;
                break;
            }

            if (target.IsDead || (target is ActorHealth health && !health.AcceptsDamageFrom(OwnerStats)) || !_targets.Add(target))
                continue;

            if (DamageCalculator.DealCriticalDamage(target, damage, EDamageType.Magic, OwnerStats) > 0f)
                NotifyHit(target, EAttackType.Magic, hit.point);
        }

        _hits.Clear();
        _targets.Clear();
        float flashSize = charged ? 0.18f : 0.105f;
        Vector2 side = Vector2.Perpendicular(aim) * (flashSize * 0.9f);
        Vector2 back = origin - aim * (flashSize * 0.7f);
        NetworkEffects.Play(OwnerNetworkObject.NetworkManager, charged ? _chargedBeamEffectId : _beamEffectId,
            origin, path: new[] { origin, end }, detail: new[] { back + side, origin, back - side });
    }

    public override void EndSecondaryUse() => CancelUse();

}
