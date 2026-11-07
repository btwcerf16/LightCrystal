using System.Collections.Generic;
using UnityEngine;

public sealed class DawnWaveProjectile : Projectile
{
    private FirstDawnSwordWeapon _source;
    private EffectData _stun;
    private int _targetLimit;
    private readonly HashSet<IDamageable> _hitTargets = new();

    public void SetSource(FirstDawnSwordWeapon source, EffectData stun, int targetLimit)
    {
        _source = source;
        _stun = stun;
        _targetLimit = targetLimit;
        _hitTargets.Clear();
    }

    protected override bool CanHit(Collider2D collider)
    {
        if (!base.CanHit(collider))
            return false;
        IDamageable target = collider.GetComponentInParent<IDamageable>();
        if (target == null)
            return !collider.isTrigger;
        return collider.GetComponentInParent<Enemy>() != null && !target.IsDead && !_hitTargets.Contains(target);
    }

    public override void OnHit(Collider2D collider)
    {
        if (!IsFlying || !CanHit(collider))
            return;
        IDamageable target = collider.GetComponentInParent<IDamageable>();
        if (target == null)
        {
            Finish();
            return;
        }
        _hitTargets.Add(target);
        if (DamageCalculator.DealCriticalDamage(target, Data.Damage, Data.DamageType, Data.Attacker) > 0f)
        {
            if (!target.IsDead)
                collider.GetComponentInParent<EffectHandler>()?.AddEffect(_stun);
            if (_source != null)
                _source.NotifyHit(target);
        }
        if (_hitTargets.Count >= _targetLimit + ExtraPiercing)
            Finish();
    }

    protected override void OnDisable()
    {
        _source = null;
        _stun = null;
        _targetLimit = 0;
        _hitTargets.Clear();
        base.OnDisable();
    }
}
