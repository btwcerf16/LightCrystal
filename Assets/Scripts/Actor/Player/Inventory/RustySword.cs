using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public sealed class RustySword : Weapon
{
    [SerializeField] private SpriteRenderer _swordRenderer;
    [SerializeField] private float _attackRadius = 0.4f;
    [SerializeField] private TrailRenderer _trail;
    [SerializeField] private NetworkAnimator _networkAnimator;

    private ActorHand _hand;
    private ActorStats _stats;
    private NetworkObject _ownerNetworkObject;
    private bool _isBonusApplied;

    public override string DisplayName => "Ржавый меч";

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
        _hand ??= GetComponentInParent<ActorHand>();
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

    public override void BeginPrimaryUse() => _networkAnimator.SetTrigger("ThrustHit");
    public override void EndPrimaryUse() { }
    public override void BeginSecondaryUse() => _networkAnimator.SetTrigger("SweepHit");
    public override void EndSecondaryUse() { }

    public override void CancelUse()
    {
        _networkAnimator.ResetTrigger("ThrustHit");
        _networkAnimator.ResetTrigger("SweepHit");
        _animator.Play("Idle", 0, 0f);
        StopTrail();
    }

    public void StartThrust() => Attack(_attackPoint.position, CurrentDamage);

    public void StartSweep(int attackPointIndex)
    {
        if (attackPointIndex < 1 || attackPointIndex > 3)
            return;

        Vector2 position = _attackPoint.position;
        position += (Vector2)_attackPoint.right * (attackPointIndex - 2);
        Attack(position, CurrentDamage * 0.5f);
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
        if (_ownerNetworkObject != null && _ownerNetworkObject.IsSpawned && !_ownerNetworkObject.NetworkManager.IsServer)
            return;

        Collider2D[] targets = Physics2D.OverlapCircleAll(position, _attackRadius);
        foreach (Collider2D target in targets)
        {
            if (target.transform.root == transform.root)
                continue;

            target.GetComponentInParent<IDamageable>()?.TakeDamage(damage);
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
