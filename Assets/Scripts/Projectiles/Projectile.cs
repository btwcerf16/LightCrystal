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

    protected ProjectileData Data { get; private set; }
    public event Action Finished;

    public virtual void Launch(ProjectileData data)
    {
        Data = data;
        _direction = data.Direction.normalized;
        _remaining = data.LifeTime;
        _isFlying = true;
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void FixedUpdate()
    {
        if (!_isFlying)
            return;
        _remaining -= Time.fixedDeltaTime;
        if (_remaining <= 0f)
        {
            Finish();
            return;
        }

        float distance = Mathf.Max(0f, Data.Speed) * Time.fixedDeltaTime;
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Data.LayerMask);
        filter.useTriggers = false;
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
    }

    protected virtual bool CanHit(Collider2D collider)
    {
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

    protected void Finish()
    {
        if (!_isFlying)
            return;
        _isFlying = false;
        Finished?.Invoke();
    }

    private void OnDisable()
    {
        _isFlying = false;
    }
}
