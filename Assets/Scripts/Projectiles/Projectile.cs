using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Projectile : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float _radius = 0.06f;
    private readonly List<RaycastHit2D> _hits = new();
    private Vector2 _direction;
    private float _remaining;
    private bool _isFlying;
    private bool _isFinished;
    protected bool IsFlying => _isFlying;

    protected ProjectileData Data { get; private set; }
    public event Action Finished;

    public virtual void Launch(ProjectileData data)
    {
        Data = data;
        _direction = data.Direction.normalized;
        _remaining = data.LifeTime;
        _isFlying = true;
        _isFinished = false;
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void FixedUpdate()
    {
        if (!_isFlying)
            return;
        float deltaTime = Mathf.Min(Time.fixedDeltaTime, _remaining);
        if (deltaTime <= 0f)
        {
            OnLifetimeExpired();
            return;
        }

        _remaining -= deltaTime;
        float distance = Mathf.Max(0f, Data.Speed) * deltaTime;
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Data.LayerMask);
        filter.useTriggers = true;
        Physics2D.CircleCast(transform.position, _radius, _direction, filter, _hits, distance);
        Collider2D closest = null;
        float closestDistance = float.PositiveInfinity;
        foreach (RaycastHit2D hit in _hits)
        {
            if (!CanHit(hit.collider) || hit.distance >= closestDistance)
                continue;
            closest = hit.collider;
            closestDistance = hit.distance;
        }
        if (closest != null)
            OnHit(closest);
        else
            transform.position += (Vector3)(_direction * distance);
        if (_isFlying && _remaining <= 0f)
            OnLifetimeExpired();
    }

    protected virtual bool CanHit(Collider2D collider)
    {
        if (collider.isTrigger && collider.GetComponentInParent<IDamageable>() == null)
            return false;
        return Data.Attacker == null || collider.GetComponentInParent<ActorStats>() != Data.Attacker;
    }

    public virtual void OnHit(Collider2D collider)
    {
        if (!_isFlying)
            return;
        IDamageable target = collider.GetComponentInParent<IDamageable>();
        if (target != null)
            target.TakeDamage(Data.Damage, Data.DamageType, Data.Attacker);
        Finish();
    }

    protected virtual void OnLifetimeExpired() => Finish();
    protected void StopFlight() => _isFlying = false;

    protected void Finish()
    {
        if (_isFinished)
            return;
        _isFinished = true;
        _isFlying = false;
        Finished?.Invoke();
    }

    protected virtual void OnDisable()
    {
        _isFlying = false;
        _direction = Vector2.zero;
        _remaining = 0f;
        _hits.Clear();
        Data = default;
    }
}
