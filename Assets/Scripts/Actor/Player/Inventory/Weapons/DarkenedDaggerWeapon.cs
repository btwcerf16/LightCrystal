using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class DarkenedDaggerWeapon : Weapon
{
    [SerializeField] private EffectData _criticalSpeedEffect;
    private const float CriticalChanceBonus = 0.1f;

    public override int TargetLimit => Mathf.Min(2, base.TargetLimit);

    [SerializeField] private SpriteRenderer _weaponRenderer;
    [SerializeField] private Projectile _projectilePrefab;
    [SerializeField] private Transform _thrustPoint;
    [SerializeField, Min(0.01f)] private float _meleeRadius = 0.2f;
    [SerializeField, Min(0f)] private float _projectileDamage = 1f;
    [SerializeField, Min(0.1f)] private float _projectileSpeed = 16f;
    [SerializeField, Min(0.1f)] private float _projectileRange = 3f;
    private readonly HashSet<IDamageable> _meleeHitTargets = new();
    private bool _hasThrownProjectile;
    public override string DisplayName => "Потемневший кинжал";

    public override float GetRange(ActorStats owner) => owner != null ? owner.GetProjectileRange(_projectileRange) : Mathf.Max(0f, _projectileRange);
    public override string GetWeaponTooltip(bool detailed) => Description
        + $"\n\nЛКМ: {FormatDamage(_damage, EDamageType.Physical, detailed)} физ. урона"
        + $"\nСкорость атаки: {FormatAttackSpeed(PrimaryAttackType, detailed)}\nЦелей за удар: {TargetLimit}"
        + $"\nПКМ: бросок на {FormatDamage(_projectileDamage, EDamageType.Physical, detailed)} физ. урона. Возврат через {FormatTooltipValue(BaseCooldown, GetReducedCooldown(BaseCooldown), detailed)} с."
        + "\n+10% к шансу крита. Критическая тычка даёт +20% к скорости движения на 3 с (до 2 стаков). Добивание тычкой даёт +5 золота.";

    public override void Equip()
    {
        base.Equip();
        RefreshWeaponVisual();
        if (IsServer && !_bonusApplied)
        {
            OwnerStats.CriticalChance.AddModifier(new StatModifier(CriticalChanceBonus, StatModifierType.Flat, this));
            _bonusApplied = true;
        }
    }

    public override void Drop()
    {
        CancelUse();
        if (_bonusApplied)
        {
            OwnerStats.CriticalChance.RemoveModifiersFrom(this);
            OwnerStats.GetComponent<EffectHandler>().PurgeEffect(_criticalSpeedEffect);
            _bonusApplied = false;
        }
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
        Vector2 attackPosition = _thrustPoint.position;
        foreach (Collider2D targetCollider in Physics2D.OverlapCircleAll(attackPosition, _meleeRadius, DamageCalculator.GetTargetMask(OwnerStats, _targetMask)))
        {
            if (_meleeHitTargets.Count >= TargetLimit)
                break;
            IDamageable hitTarget = targetCollider.GetComponentInParent<IDamageable>();
            if (hitTarget == null || hitTarget.IsDead || _meleeHitTargets.Contains(hitTarget))
                continue;
            if (ActorArsenal.DealWeaponDamage(hitTarget, _damage, EDamageType.Physical, OwnerStats, out bool critical, isMelee: true) <= 0f)
                continue;
            _meleeHitTargets.Add(hitTarget);
            OnMeleeDamage(hitTarget, critical);
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
        Projectile thrownProjectile = projectilePool.GetObject(_projectilePrefab, _attackPoint.position, Quaternion.identity);
        thrownProjectile.Launch(new ProjectileData
        {
            Attacker = OwnerStats, SourceWeapon = this,
            Damage = _projectileDamage, DamageType = EDamageType.Physical,
            Direction = throwDirection,
            Speed = _projectileSpeed, LifeTime = _projectileRange / _projectileSpeed, LayerMask = _targetMask
        });
        thrownProjectile.GetComponent<NetworkObject>().Spawn();
        StartReturnTimer();
    }

    private void LateUpdate()
    {
        if (_hand == null)
            return;
        Vector3 weaponScale = transform.localScale;
        weaponScale.y = Mathf.Abs(weaponScale.y) * (_hand.AimDirection.x < 0f ? -1f : 1f);
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

    private void StartReturnTimer()
    {
        NetworkObject owner = OwnerStats.GetComponent<NetworkObject>();
        float cooldown = CooldownDuration;
        SetCooldown(cooldown, owner.NetworkManager.ServerTime.Time + cooldown);
        SetAvailable(false);
        owner.StartCoroutine(ReturnToHand(cooldown));
    }

    private IEnumerator ReturnToHand(float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
        if (this != null)
            SetAvailable(true);
    }

    private void OnMeleeDamage(IDamageable target, bool critical)
    {
        if (critical)
        {
            EffectHandler effects = OwnerStats.GetComponent<EffectHandler>();
            Effect oldestStack = null;
            int stacks = 0;
            foreach (Effect effect in effects.ActiveEffects)
            {
                if (effect.EffectData != _criticalSpeedEffect)
                    continue;
                stacks++;
                if (oldestStack == null || effect.TimeRemaining < oldestStack.TimeRemaining)
                    oldestStack = effect;
            }
            if (stacks >= 2)
                effects.RemoveEffect(oldestStack);
            effects.AddEffect(_criticalSpeedEffect);
        }
        if (target.IsDead && target is ActorHealth health && health.GetComponent<Enemy>() != null)
            OwnerStats.GetComponent<ActorWallet>().AddCurrency(5, health.transform.position);
    }
}
