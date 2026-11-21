using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;

public sealed class RustySwordWeapon : Weapon
{
    private const float SecondaryDamageMultiplier = 0.5f;
    [SerializeField] private SpriteRenderer _swordRenderer;
    [SerializeField] private float _attackRadius = 0.4f;
    [SerializeField] private TrailRenderer _trail;
    [SerializeField] private Transform _attackPointThrust;
    [SerializeField, Min(0.01f)] private float _thrustPointSpacing = 0.8f;
    private ActorStats _stats;
    private readonly HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();

    public override string DisplayName => "Ржавый меч";
    public float SecondaryDamagePercent => SecondaryDamageMultiplier * 100f;
    public float PrimaryDamage => DamageCalculator.CalculateDamage(CurrentDamage, EDamageType.Physical, _stats, null);
    public float SecondaryDamage => PrimaryDamage * SecondaryDamageMultiplier;
    public float AttackSpeed => _stats != null ? _stats.AttackSpeed.Value : 1f;
    public override string GetWeaponTooltip(bool detailed) => base.GetWeaponTooltip(detailed)
        + $"\n\nЛКМ — укол: {FormatDamage(_damage, EDamageType.Physical, detailed, true)} физ. урона, целей {TargetLimit}"
        + $"\nПКМ — взмах: {FormatDamage(_damage, EDamageType.Physical, detailed, true, SecondaryDamageMultiplier)} физ. урона, целей {SplashTargetLimit}"
        + $"\nСкорость атаки: {FormatAttackSpeed(PrimaryAttackType, detailed)}";

    private void Awake()
    {
        _stats = GetComponentInParent<ActorStats>();
    }

    public override void Use() { }

    public override void Equip()
    {
        base.Equip();
        _stats = GetComponentInParent<ActorStats>();
        if (!_bonusApplied && _stats != null)
        {
            _stats.PhysicalDamage.AddModifier(new StatModifier(_damage, StatModifierType.Flat, this));
            _bonusApplied = true;
        }
        _swordRenderer.enabled = true;
    }

    public override void Drop()
    {
        CancelUse();
        base.Drop();
        if (_bonusApplied)
        {
            _stats.PhysicalDamage.RemoveModifiersFrom(this);
            _bonusApplied = false;
        }
        _swordRenderer.enabled = false;
        StopTrail();
    }

    public override bool BeginPrimaryUse()
    {
        if (IsOwnerStunned || !IsEquipped || _isAttacking)
            return false;

        _isAttacking = true;
        _hitTargets.Clear();
        SetAttackType(PrimaryAttackType);
        _animator.SetTrigger("ThrustHit");
        return true;
    }
    public override void EndPrimaryUse() { }
    public override bool BeginSecondaryUse()
    {
        if (IsOwnerStunned || !IsEquipped || _isAttacking)
            return false;

        _isAttacking = true;
        _hitTargets.Clear();
        SetAttackType(SecondaryAttackType);
        _animator.SetTrigger("SweepHit");
        return true;
    }
    public override void EndSecondaryUse() { }

    public override void CancelUse()
    {
        _animator.ResetTrigger("ThrustHit");
        _animator.ResetTrigger("SweepHit");
        _animator.Play("Idle", 0, 0f);
        FinishAttack();
    }

    public void FinishAttack()
    {
        _isAttacking = false;
        StopTrail();
    }

    public void StartThrust(int attackPointIndex)
    {
        if (attackPointIndex < 1 || attackPointIndex > 3)
            return;
        Vector2 position = _attackPointThrust.position;
        position += (Vector2)_attackPointThrust.up * (attackPointIndex - 2) * _thrustPointSpacing;
        Attack(position, CurrentDamage, TargetLimit);
    }

    public void StartSweep(int attackPointIndex)
    {
        if (attackPointIndex < 1 || attackPointIndex > 3)
            return;

        Vector2 position = _attackPoint.position;
        position += (Vector2)_attackPoint.right * (attackPointIndex - 2);
        Attack(position, CurrentDamage * SecondaryDamageMultiplier, SplashTargetLimit);
    }

    private float CurrentDamage => PhysicalDamage;

    public void StartTrail()
    {
        if (_trail == null)
            return;

        _trail.Clear();
        _trail.emitting = true;
    }

    public void StopTrail()
    {
        if (_trail != null)
            _trail.emitting = false;
    }

    private void Attack(Vector2 position, float damage, int targetLimit)
    {
        if (IsOwnerStunned || !IsEquipped || !_isAttacking || OwnerNetworkObject == null || !OwnerNetworkObject.IsSpawned || !OwnerNetworkObject.NetworkManager.IsServer)
            return;

        Collider2D[] targets = Physics2D.OverlapCircleAll(position, _attackRadius, DamageCalculator.GetTargetMask(OwnerStats, _targetMask));
        foreach (Collider2D target in targets)
        {
            if (_hitTargets.Count >= targetLimit)
                return;
            if (target.transform.root == OwnerNetworkObject.transform.root)
                continue;

            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null && _hitTargets.Add(damageable))
            {
                float dealt = ActorArsenal.DealWeaponDamage(damageable, damage, EDamageType.Physical, OwnerStats, isMelee: true);
                if (dealt > 0f)
                    NotifyMeleeHit(damageable, target.ClosestPoint(position));
                else
                    _hitTargets.Remove(damageable);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_attackPoint != null)
        {
            Gizmos.color = Color.red;
            for (int i = -1; i <= 1; i++)
                Gizmos.DrawWireSphere(_attackPoint.position + _attackPoint.right * i, _attackRadius);
        }
        if (_attackPointThrust != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = -1; i <= 1; i++)
                Gizmos.DrawWireSphere(_attackPointThrust.position + _attackPointThrust.up * i * _thrustPointSpacing, _attackRadius);
        }
    }
}
