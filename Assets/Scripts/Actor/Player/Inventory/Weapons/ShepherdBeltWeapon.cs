using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class ShepherdBeltWeapon : Weapon
{
    [SerializeField] private SpriteRenderer _weaponRenderer;
    [SerializeField] private Projectile _projectilePrefab;
    [SerializeField] private Transform _thrustPoint;
    [SerializeField] private EffectData _projectileEffect;
    [SerializeField, Min(0.01f)] private float _meleeRadius = 0.2f;
    [SerializeField, Min(0f)] private float _meleePointSpacing = 0.18f;
    [SerializeField, Min(0f)] private float _projectileDamage = 1f;
    [SerializeField, Min(0.1f)] private float _projectileSpeed = 16f;
    [SerializeField, Min(0.1f)] private float _projectileRange = 3f;
    [SerializeField, Min(1)] private int _projectilesPerThrow = 1;
    [SerializeField, Range(0f, 180f)] private float _throwSpreadAngle;
    [SerializeField, Min(0f)] private float _throwHealthCost;
    private readonly HashSet<IDamageable> _meleeHitTargets = new();
    private bool _hasThrownProjectile;
    public override string DisplayName => "Потрёпанный ремень пастуха";

    public override float GetRange(ActorStats owner) => owner != null ? owner.GetProjectileRange(_projectileRange) : Mathf.Max(0f, _projectileRange);
    public override string GetWeaponTooltip(bool detailed) => base.GetWeaponTooltip(detailed)
        + $"\n\nЛКМ: {FormatDamage(_damage, EDamageType.Physical, detailed)} физ. урона. Целей: {TargetLimit}."
        + $"\nСкорость атаки: {FormatAttackSpeed(PrimaryAttackType, detailed)}"
        + $"\n\nПКМ: {_projectilesPerThrow} × ({FormatDamage(_projectileDamage, EDamageType.Physical, detailed)}) физ. урона"
        + $"\n{FormatProjectile(_projectileSpeed, _projectileRange, detailed)}"
        + $"\nКД: {FormatTooltipValue(BaseCooldown, GetReducedCooldown(BaseCooldown), detailed)} с"
        + (_throwHealthCost > 0f ? $"\nЗатраты здоровья: {_throwHealthCost:0.#}." : "");

    public override void Equip()
    {
        base.Equip();
        RefreshWeaponVisual();
    }

    public override void Drop()
    {
        CancelUse();
        base.Drop();
        _weaponRenderer.enabled = false;
    }

    public override void SetAvailable(bool available)
    {
        base.SetAvailable(available);
        RefreshWeaponVisual();
    }

    private void RefreshWeaponVisual()
    {
        _weaponRenderer.enabled = IsEquipped && IsAvailable;
    }

    public override bool BeginPrimaryUse()
    {
        if (!IsEquipped || !IsAvailable || IsOwnerStunned || _isAttacking)
            return false;
        _isAttacking = true;
        _meleeHitTargets.Clear();
        SetAttackType(PrimaryAttackType);
        _animator.SetTrigger("Thrust");
        return true;
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
        Transform thrustPoint = _thrustPoint != null ? _thrustPoint : _attackPoint;
        Vector2 attackPosition = thrustPoint.position + _attackPoint.right * ((attackPointIndex - 2) * _meleePointSpacing);
        foreach (Collider2D targetCollider in Physics2D.OverlapCircleAll(attackPosition, _meleeRadius, DamageCalculator.GetTargetMask(OwnerStats, _targetMask)))
        {
            if (_meleeHitTargets.Count >= TargetLimit)
                break;
            IDamageable hitTarget = targetCollider.GetComponentInParent<IDamageable>();
            if (hitTarget == null || hitTarget.IsDead || _meleeHitTargets.Contains(hitTarget))
                continue;
            if (ActorArsenal.DealWeaponDamage(hitTarget, _damage, EDamageType.Physical, OwnerStats, isMelee: true) <= 0f)
                continue;
            _meleeHitTargets.Add(hitTarget);
            NotifyMeleeHit(hitTarget, targetCollider.ClosestPoint(attackPosition));
        }
    }

    public void ThrowProjectiles()
    {
        if (!IsServer || !IsEquipped || IsOwnerStunned || !_isAttacking || _hasThrownProjectile)
            return;
        _hasThrownProjectile = true;
        NetworkObjectPool projectilePool = OwnerStats.GetComponent<NetworkObject>().NetworkManager.GetComponent<NetworkObjectPool>();
        Vector2 throwDirection = _hand.AimDirection;
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
