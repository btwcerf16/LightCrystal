using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class Projectile : MonoBehaviour
{
    [FormerlySerializedAs("_radius")]
    [SerializeField, Min(0.01f)] private float _collisionRadius = 0.06f;
    private readonly List<RaycastHit2D> _collisionHits = new();
    private static readonly Comparison<RaycastHit2D> CompareHitsByDistance = (firstHit, secondHit) => firstHit.distance.CompareTo(secondHit.distance);
    private Vector2 _flightDirection;
    private float _remainingLifetime;
    private bool _isFlying;
    private bool _hasFinishedFlight;
    protected bool IsFlying => _isFlying;

    protected ProjectileData Data { get; private set; }
    protected int ExtraPiercing { get; private set; }
    private readonly HashSet<IDamageable> _hitTargets = new();
    public event Action Finished;
    public static event Action<Projectile> Launched;
    public static event Action<Projectile, ActorHealth, float> TargetHit;
    public ProjectileData ShotData => Data;
    public void SetPiercing(int piercingCount) => ExtraPiercing = Mathf.Max(0, piercingCount);

    public virtual void Launch(ProjectileData projectileData)
    {
        ExtraPiercing = 0;
        _hitTargets.Clear();
        if (projectileData.Attacker != null && !projectileData.StatsApplied)
        {
            ActorStats attackerStats = projectileData.Attacker;
            float flightRange = Mathf.Max(0f, projectileData.Speed * projectileData.LifeTime + attackerStats.ProjectileRange.Value);
            projectileData.Speed = Mathf.Max(0.01f, (projectileData.Speed + attackerStats.ProjectileSpeed.Value) * attackerStats.ProjectileSpeedMultiplier.Value);
            projectileData.LifeTime = flightRange / projectileData.Speed;
            if (projectileData.DamageType == EDamageType.Physical ||
                (projectileData.DamageType == EDamageType.Magic && UnityEngine.Random.value < 0.2f))
                ExtraPiercing = Mathf.Max(0, Mathf.FloorToInt(attackerStats.ProjectilePiercing.Value));
        }
        if (projectileData.SourceWeapon != null)
            ExtraPiercing += projectileData.SourceWeapon.TargetLimit - 1;
        projectileData.StatsApplied = true;
        Data = projectileData;
        SetDirection(projectileData.Direction);
        _remainingLifetime = projectileData.LifeTime;
        _isFlying = true;
        _hasFinishedFlight = false;

        if (!projectileData.IgnoreItemEffects) Launched?.Invoke(this);
    }

    protected void SetDirection(Vector2 flightDirection)
    {
        _flightDirection = flightDirection.normalized;
        float flightAngle = Mathf.Atan2(_flightDirection.y, _flightDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, flightAngle);
    }

    private void FixedUpdate()
    {
        if (!_isFlying)
            return;
        float simulationDeltaTime = Mathf.Min(Time.fixedDeltaTime, _remainingLifetime);
        if (simulationDeltaTime <= 0f)
        {
            OnLifetimeExpired();
            return;
        }

        _remainingLifetime -= simulationDeltaTime;
        float movementDistance = Mathf.Max(0f, Data.Speed) * simulationDeltaTime;
        ContactFilter2D collisionFilter = new ContactFilter2D();
        collisionFilter.SetLayerMask(Data.LayerMask);
        collisionFilter.useTriggers = true;
        Physics2D.CircleCast(transform.position, _collisionRadius, _flightDirection, collisionFilter, _collisionHits, movementDistance);
        _collisionHits.Sort(CompareHitsByDistance);
        Vector3 movementStartPosition = transform.position;
        foreach (RaycastHit2D collisionHit in _collisionHits)
        {
            if (!CanHit(collisionHit.collider))
                continue;
            transform.position = movementStartPosition + (Vector3)(_flightDirection * collisionHit.distance);
            OnHit(collisionHit.collider);
            if (!_isFlying)
                return;
        }
        transform.position = movementStartPosition + (Vector3)(_flightDirection * movementDistance);
        if (_isFlying && _remainingLifetime <= 0f)
            OnLifetimeExpired();
    }

    protected virtual bool CanHit(Collider2D targetCollider)
    {
        if (targetCollider.isTrigger && targetCollider.GetComponentInParent<IDamageable>() == null)
            return false;
        IDamageable damageableTarget = targetCollider.GetComponentInParent<IDamageable>();
        if (damageableTarget != null && (damageableTarget.IsDead || _hitTargets.Contains(damageableTarget)))
            return false;
        return Data.Attacker == null || targetCollider.GetComponentInParent<ActorStats>() != Data.Attacker;
    }

    public virtual void OnHit(Collider2D targetCollider)
    {
        if (!_isFlying)
            return;
        IDamageable damageableTarget = targetCollider.GetComponentInParent<IDamageable>();
        if (damageableTarget != null)
        {
            _hitTargets.Add(damageableTarget);
            float dealtDamage = DamageCalculator.DealCriticalDamage(damageableTarget, Data.Damage, Data.DamageType, Data.Attacker);
            if (dealtDamage > 0f)
            {
                if (!damageableTarget.IsDead && Data.EffectOnHit != null)
                    targetCollider.GetComponentInParent<EffectHandler>()?.AddEffect(Data.EffectOnHit);
                if (Data.SourceWeapon != null)
                    Data.SourceWeapon.NotifyHit(damageableTarget);
            }
            if (!Data.IgnoreItemEffects && damageableTarget is ActorHealth targetHealth)
                TargetHit?.Invoke(this, targetHealth, dealtDamage);
            if (_hitTargets.Count <= ExtraPiercing)
                return;
        }
        Finish();
    }

    protected virtual void OnLifetimeExpired() => Finish();
    protected void StopFlight() => _isFlying = false;

    protected void Finish()
    {
        if (_hasFinishedFlight)
            return;
        _hasFinishedFlight = true;
        _isFlying = false;
        Finished?.Invoke();
    }

    protected virtual void OnDisable()
    {
        _isFlying = false;
        _flightDirection = Vector2.zero;
        _remainingLifetime = 0f;
        _collisionHits.Clear();
        _hitTargets.Clear();
        ExtraPiercing = 0;
        Data = default;
    }
}
