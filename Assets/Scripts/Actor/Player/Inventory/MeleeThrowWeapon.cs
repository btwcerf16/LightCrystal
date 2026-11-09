using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class MeleeThrowWeapon : Weapon
{
    [SerializeField] private SpriteRenderer _weaponRenderer;
    [SerializeField] private Projectile _projectilePrefab;
    [SerializeField] private EffectData _projectileEffect;
    [SerializeField, Min(0.01f)] private float _meleeRadius = 0.2f;
    [SerializeField, Min(0f)] private float _meleePointSpacing = 0.18f;
    [SerializeField, Min(0f)] private float _projectileDamage = 1f;
    [SerializeField, Min(0.1f)] private float _projectileSpeed = 16f;
    [SerializeField, Min(0.1f)] private float _projectileRange = 3f;
    [SerializeField, Min(1)] private int _projectilesPerThrow = 1;
    [SerializeField, Range(0f, 180f)] private float _throwSpreadAngle;
    [SerializeField, Min(0f)] private float _throwHealthCost;
    [SerializeField] private LayerMask _targetMask;
    private readonly HashSet<IDamageable> _meleeHitTargets = new();
    private ActorHand _actorHand;
    private bool _isAttacking;
    private bool _hasThrownProjectile;
    public override float GetRange(ActorStats owner) => Mathf.Max(0f, _projectileRange + (owner != null ? owner.ProjectileRange.Value : 0f));
    public float MeleeDamage => DamageCalculator.CalculateDamage(_damage, EDamageType.Physical, OwnerStats, null);
    public float ProjectileRange => Mathf.Max(0f, _projectileRange + (OwnerStats != null ? OwnerStats.ProjectileRange.Value : 0f));
    public float ProjectileDamage => DamageCalculator.CalculateDamage(_projectileDamage, EDamageType.Physical, OwnerStats, null);
    public override string TooltipDescription => base.TooltipDescription
        + $"\n\nЛКМ: {MeleeDamage:0.#} физ. урона. Целей: {TargetLimit}."
        + $"\nПКМ: {ProjectileDamage:0.#} физ. урона за снаряд. Дальность: {ProjectileRange:0.#}."
        + $"\nКД: {BaseCooldown * (1f - (OwnerStats != null ? OwnerStats.CooldownReduction.Value : 0f)):0.#} с."
        + (_throwHealthCost > 0f ? $"\nЗатраты здоровья: {_throwHealthCost:0.#}." : "");

    public override void Equip()
    {
        base.Equip();
        _actorHand = GetComponentInParent<ActorHand>();
        _actorHand.VisualRenderer.enabled = false;
        _weaponRenderer.enabled = true;
    }

    public override void Drop()
    {
        CancelUse();
        base.Drop();
        _weaponRenderer.enabled = false;
        if (_actorHand != null)
            _actorHand.VisualRenderer.enabled = true;
    }

    public override void BeginPrimaryUse()
    {
        if (!IsEquipped || !IsAvailable || IsOwnerStunned || _isAttacking)
            return;
        _isAttacking = true;
        _meleeHitTargets.Clear();
        SetAttackType(PrimaryAttackType);
        _animator.SetTrigger("Thrust");
    }

    public override bool BeginSecondaryUse()
    {
        if (!IsEquipped || !IsAvailable || IsOwnerStunned || _isAttacking)
            return false;
        if (!IsServer)
        {
            BeginThrow();
            return true;
        }
        if (OwnerStats.GetComponent<EffectHandler>().IsSilenced)
        {
            OwnerStats.GetComponent<NetworkPlayerWallet>().ShowHint("Вы под действием безмолвия");
            return false;
        }
        return TryUse();
    }

    public override void Use()
    {
        BeginThrow();
        NotifyStateChanged();
    }

    private void BeginThrow()
    {
        _isAttacking = true;
        _hasThrownProjectile = false;
        SetAttackType(SecondaryAttackType);
        _animator.SetTrigger("Throw");
    }

    public void Strike(int attackPointIndex)
    {
        if (!IsServer || !IsEquipped || IsOwnerStunned || !_isAttacking)
            return;
        Vector2 attackPosition = _attackPoint.position + _attackPoint.right * ((attackPointIndex - 2) * _meleePointSpacing);
        foreach (Collider2D targetCollider in Physics2D.OverlapCircleAll(attackPosition, _meleeRadius, _targetMask))
        {
            if (_meleeHitTargets.Count >= TargetLimit)
                break;
            IDamageable hitTarget = targetCollider.GetComponentInParent<IDamageable>();
            if (hitTarget == null || hitTarget.IsDead || _meleeHitTargets.Contains(hitTarget))
                continue;
            if (DamageCalculator.DealCriticalDamage(hitTarget, _damage, EDamageType.Physical, OwnerStats) <= 0f)
                continue;
            _meleeHitTargets.Add(hitTarget);
            NotifyMeleeHit(hitTarget);
        }
    }

    public void ThrowProjectiles()
    {
        if (!IsServer || !IsEquipped || IsOwnerStunned || !_isAttacking || _hasThrownProjectile)
            return;
        _hasThrownProjectile = true;
        NetworkObjectPool projectilePool = OwnerStats.GetComponent<NetworkObject>().NetworkManager.GetComponent<NetworkObjectPool>();
        Vector2 throwDirection = GetProjectileDirection(_actorHand.AimDirection);
        for (int projectileIndex = 0; projectileIndex < _projectilesPerThrow; projectileIndex++)
        {
            float spreadAngle = _projectilesPerThrow == 1 ? 0f
                : Mathf.Lerp(-_throwSpreadAngle * 0.5f, _throwSpreadAngle * 0.5f, (float)projectileIndex / (_projectilesPerThrow - 1));
            Projectile thrownProjectile = projectilePool.GetObject(_projectilePrefab, _attackPoint.position, Quaternion.identity);
            thrownProjectile.Launch(new ProjectileData
            {
                Attacker = OwnerStats, SourceWeapon = this, EffectOnHit = _projectileEffect,
                Damage = _projectileDamage, DamageType = EDamageType.Physical,
                Direction = Quaternion.Euler(0f, 0f, spreadAngle) * throwDirection,
                Speed = _projectileSpeed, LifeTime = _projectileRange / _projectileSpeed, LayerMask = _targetMask
            });
            thrownProjectile.GetComponent<NetworkObject>().Spawn();
        }
        if (_throwHealthCost > 0f)
            OwnerStats.GetComponent<ActorHealth>().TakeDamage(_throwHealthCost, EDamageType.Pure);
    }

    private void LateUpdate()
    {
        if (_actorHand == null)
            return;
        Vector3 weaponScale = transform.localScale;
        weaponScale.y = Mathf.Abs(weaponScale.y) * (_actorHand.AimDirection.x < 0f ? -1f : 1f);
        transform.localScale = weaponScale;
    }

    public void FinishAttack() => _isAttacking = false;
    public override void EndPrimaryUse() { }
    public override void EndSecondaryUse() { }
    public override void CancelUse()
    {
        _isAttacking = false;
        _animator.ResetTrigger("Thrust");
        _animator.ResetTrigger("Throw");
        _animator.Play("Idle", 0, 0f);
    }
}
