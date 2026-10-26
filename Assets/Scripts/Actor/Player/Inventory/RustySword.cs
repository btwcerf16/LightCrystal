using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;

public sealed class RustySword : Weapon
{
    private const float SecondaryDamageMultiplier = 0.5f;
    [SerializeField] private SpriteRenderer _swordRenderer;
    [SerializeField] private float _attackRadius = 0.4f;
    [SerializeField] private TrailRenderer _trail;

    private ActorHand _hand;
    private ActorStats _stats;
    private NetworkObject _ownerNetworkObject;
    private bool _isBonusApplied;
    private bool _isAttacking;
    private readonly HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();

    public override string DisplayName => "Ржавый меч";
    public float SecondaryDamagePercent => SecondaryDamageMultiplier * 100f;

    private void Awake()
    {
        _hand = GetComponentInParent<ActorHand>();
        _stats = GetComponentInParent<ActorStats>();
        _ownerNetworkObject = GetComponentInParent<NetworkObject>();
    }

    public override void Use() { }

    public override void Equip()
    {
        base.Equip();
        _hand = GetComponentInParent<ActorHand>();
        _stats = GetComponentInParent<ActorStats>();
        _ownerNetworkObject = GetComponentInParent<NetworkObject>();
        if (!_isBonusApplied && _stats != null)
        {
            _stats.PhysicalDamage.AddModifier(new StatModifier(_damage, StatModifierType.Flat, this));
            _isBonusApplied = true;
        }
        if (_hand != null)
            _hand.VisualRenderer.enabled = false;
        _swordRenderer.enabled = true;
    }

    public override void Drop()
    {
        CancelUse();
        base.Drop();
        if (_isBonusApplied)
        {
            _stats.PhysicalDamage.RemoveModifiersFrom(this);
            _isBonusApplied = false;
        }
        if (_hand != null)
            _hand.VisualRenderer.enabled = true;
        _swordRenderer.enabled = false;
        StopTrail();
    }

    public override void BeginPrimaryUse()
    {
        if (!IsEquipped || _isAttacking)
            return;

        _isAttacking = true;
        _hitTargets.Clear();
        _animator.SetTrigger("ThrustHit");
    }
    public override void EndPrimaryUse() { }
    public override void BeginSecondaryUse()
    {
        if (!IsEquipped || _isAttacking)
            return;

        _isAttacking = true;
        _hitTargets.Clear();
        _animator.SetTrigger("SweepHit");
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

    public void StartThrust() => Attack(_attackPoint.position, CurrentDamage);

    public void StartSweep(int attackPointIndex)
    {
        if (attackPointIndex < 1 || attackPointIndex > 3)
            return;

        Vector2 position = _attackPoint.position;
        position += (Vector2)_attackPoint.right * (attackPointIndex - 2);
        Attack(position, CurrentDamage * SecondaryDamageMultiplier);
    }

    private float CurrentDamage => _stats != null ? _stats.PhysicalDamage.Value : _damage;

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

    private void Attack(Vector2 position, float damage)
    {
        if (!IsEquipped || !_isAttacking || _ownerNetworkObject == null || !_ownerNetworkObject.IsSpawned || !_ownerNetworkObject.NetworkManager.IsServer)
            return;

        Collider2D[] targets = Physics2D.OverlapCircleAll(position, _attackRadius);
        foreach (Collider2D target in targets)
        {
            if (target.transform.root == transform.root)
                continue;

            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null && _hitTargets.Add(damageable))
            {
                damageable.TakeDamage(damage, EDamageType.Physical, _stats);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_attackPoint.position, _attackRadius);
        for (int i = -1; i <= 1; i++)
            Gizmos.DrawWireSphere(_attackPoint.position + _attackPoint.right * i, _attackRadius);
    }
}
