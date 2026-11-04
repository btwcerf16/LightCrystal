using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Projectile : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float _radius = 0.06f;
    private readonly List<RaycastHit2D> _hits = new();
    private static readonly Comparison<RaycastHit2D> HitOrder = (first, second) => first.distance.CompareTo(second.distance);
    private Vector2 _direction;
    private float _remaining;
    private bool _isFlying;
    private bool _isFinished;
    protected bool IsFlying => _isFlying;

    protected ProjectileData Data { get; private set; }
    protected int ExtraPiercing { get; private set; }
    private readonly HashSet<IDamageable> _hitTargets = new();
    public event Action Finished;
    public static event Action<Projectile> Launched;
    public static event Action<Projectile, ActorHealth, float> TargetHit;
    public ProjectileData ShotData => Data;
    public void SetPiercing(int count) => ExtraPiercing = Mathf.Max(0, count);

    public virtual void Launch(ProjectileData data)
    {
        ExtraPiercing = 0;
        _hitTargets.Clear();
        if (data.Attacker != null && !data.StatsApplied)
        {
            ActorStats stats = data.Attacker;
            float range = Mathf.Max(0f, data.Speed * data.LifeTime + stats.ProjectileRange.Value);
            data.Speed = Mathf.Max(0.01f, (data.Speed + stats.ProjectileSpeed.Value) * stats.ProjectileSpeedMultiplier.Value);
            data.LifeTime = range / data.Speed;
            if (data.DamageType == EDamageType.Physical ||
                (data.DamageType == EDamageType.Magic && UnityEngine.Random.value < 0.2f))
                ExtraPiercing = Mathf.Max(0, Mathf.FloorToInt(stats.ProjectilePiercing.Value));
        }
        data.StatsApplied = true;
        Data = data;
        _direction = data.Direction.normalized;
        _remaining = data.LifeTime;
        _isFlying = true;
        _isFinished = false;
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        if (!data.IgnoreItemEffects) Launched?.Invoke(this);
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
        _hits.Sort(HitOrder);
        Vector3 origin = transform.position;
        foreach (RaycastHit2D hit in _hits)
        {
            if (!CanHit(hit.collider))
                continue;
            transform.position = origin + (Vector3)(_direction * hit.distance);
            OnHit(hit.collider);
            if (!_isFlying)
                return;
        }
        transform.position = origin + (Vector3)(_direction * distance);
        if (_isFlying && _remaining <= 0f)
            OnLifetimeExpired();
    }

    protected virtual bool CanHit(Collider2D collider)
    {
        if (collider.isTrigger && collider.GetComponentInParent<IDamageable>() == null)
            return false;
        IDamageable target = collider.GetComponentInParent<IDamageable>();
        if (target != null && (target.IsDead || _hitTargets.Contains(target)))
            return false;
        return Data.Attacker == null || collider.GetComponentInParent<ActorStats>() != Data.Attacker;
    }

    public virtual void OnHit(Collider2D collider)
    {
        if (!_isFlying)
            return;
        IDamageable target = collider.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            _hitTargets.Add(target);
            float damage = target.TakeDamage(DamageCalculator.RollCriticalDamage(Data.Damage, Data.Attacker), Data.DamageType, Data.Attacker);
            if (!Data.IgnoreItemEffects && target is ActorHealth health)
                TargetHit?.Invoke(this, health, damage);
            if (_hitTargets.Count <= ExtraPiercing)
                return;
        }
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
        _hitTargets.Clear();
        ExtraPiercing = 0;
        Data = default;
    }
}
