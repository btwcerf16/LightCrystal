using System.Collections.Generic;
using UnityEngine;

public sealed class SpearProjectile : Projectile
{
    private Spear _source;
    private int _targetLimit;
    private Vector2 _origin;
    private float _hitDistance;
    private bool _cooldownStarted;
    private readonly HashSet<IDamageable> _hitTargets = new();

    public void SetSource(Spear source)
    {
        _source = source;
        _targetLimit = source.TargetLimit;
        _hitTargets.Clear();
        _origin = transform.position;
        _hitDistance = -1f;
        _cooldownStarted = false;
    }

    protected override bool CanHit(Collider2D collider)
    {
        return base.CanHit(collider) && !_hitTargets.Contains(collider.GetComponentInParent<IDamageable>());
    }

    public override void OnHit(Collider2D collider)
    {
        if (!IsFlying)
            return;
        IDamageable target = collider.GetComponentInParent<IDamageable>();
        if (target == null)
        {
            StartCooldown(Vector2.Distance(_origin, collider.ClosestPoint(_origin)));
            StopFlight();
            return;
        }
        if (!_hitTargets.Add(target))
            return;
        _hitDistance = Vector2.Distance(_origin, collider.ClosestPoint(_origin));
        if (target.TakeDamage(Data.Damage, Data.DamageType, Data.Attacker) > 0f && _source != null)
            _source.NotifyHit(target);
        if (_hitTargets.Count >= _targetLimit)
            ReturnToOwner();
    }

    protected override void OnLifetimeExpired()
    {
        if (_hitTargets.Count > 0)
            ReturnToOwner();
        else
        {
            StartCooldown(Data.Speed * Data.LifeTime);
            StopFlight();
        }
    }

    private void StartCooldown(float distance)
    {
        if (_cooldownStarted || _source == null)
            return;
        _cooldownStarted = true;
        _source.SetThrowCooldown(_hitDistance >= 0f ? _hitDistance : distance);
    }

    private void Update()
    {
        if (_source == null)
        {
            Finish();
            return;
        }
        if (!IsFlying && Data.Attacker != null
            && ((Vector2)(Data.Attacker.transform.position - transform.position)).sqrMagnitude <= 1.44f)
            ReturnToOwner();
    }

    private void ReturnToOwner()
    {
        StartCooldown(Vector2.Distance(_origin, transform.position));
        if (_source != null)
            _source.SetAvailable(true);
        Finish();
    }

    protected override void OnDisable()
    {
        if (_source != null)
            _source.SetAvailable(true);
        _source = null;
        _hitTargets.Clear();
        base.OnDisable();
    }
}
