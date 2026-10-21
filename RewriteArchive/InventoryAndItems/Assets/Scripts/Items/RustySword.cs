using Unity.Netcode;
using UnityEngine;

public sealed class RustySword : Weapon
{
    private ActorStats _stats;
    private ActorArsenal _arsenal;
    private NetworkObject _ownerNetwork;
    private bool _isBonusApplied;

    public override string DisplayName => "Ржавый меч";
    private bool IsActiveWeapon => Owner != null && _arsenal != null && _arsenal.ActiveWeapon == this;

    [SerializeField] private Transform[] _sweepPoints;
    [SerializeField] private float _attackRadius = 0.4f;

    [SerializeField] private TrailRenderer _trail;

    public override void SetOwner(GameObject owner)
    {
        base.SetOwner(owner);
        _stats = owner != null ? owner.GetComponent<ActorStats>() : null;
        _arsenal = owner != null ? owner.GetComponent<ActorArsenal>() : null;
        _ownerNetwork = owner != null ? owner.GetComponent<NetworkObject>() : null;
    }

    public override void Equip()
    {
        if (!IsActiveWeapon)
            return;
        base.Equip();
        if (_isBonusApplied || _ownerNetwork == null || !_ownerNetwork.IsSpawned || !_ownerNetwork.NetworkManager.IsServer)
            return;
        _stats.PhysicalDamage.AddModifier(new StatModifier(_damage, StatModifierType.Flat, this));
        _isBonusApplied = true;
    }

    public override void Drop()
    {
        base.Drop();
        if (!_isBonusApplied)
            return;
        _isBonusApplied = false;
        _stats.PhysicalDamage.RemoveModifiersFrom(this);
    }

    public override void Use()
    {
        if (Owner != null)
            Owner.GetComponent<NetworkArsenal>().Select(this);
    }

    public override void BeginPrimaryUse() 
    {
        _animator.SetTrigger("ThrustHit");
    }
    public override void EndPrimaryUse() { }
    public override void BeginSecondaryUse() 
    {
        _animator.SetTrigger("SweepHit");
    }
    public override void EndSecondaryUse() { }
    public override void CancelUse() { }


    public void StartThrust()
    {
        Collider2D[] targets = Physics2D.OverlapCircleAll(_attackPoint.position, _attackRadius);

        foreach (Collider2D target in targets)
        {
            if (!target.CompareTag("Enemy"))
                continue;

            target.GetComponent<IDamageable>()?.TakeDamage(_damage);
        }
    }
    public void StartSweep(int attackPointIndex)
    {
        Transform attackPoint = _sweepPoints[attackPointIndex];
        Collider2D[] targets = Physics2D.OverlapCircleAll(attackPoint.position, _attackRadius);

        foreach (Collider2D target in targets)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();

            if (damageable == null)
                continue;

            damageable.TakeDamage(_damage * 0.5f);
        }
    }

    public void StartTrail()
    {
        _trail.Clear();
        _trail.emitting = true;
    }

    public void StopTrail()
    {
        _trail.emitting = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        if (_attackPoint != null)
            Gizmos.DrawWireSphere(_attackPoint.position, _attackRadius);

        if (_sweepPoints == null)
            return;

        foreach (Transform sweepPoint in _sweepPoints)
        {
            if (sweepPoint != null)
                Gizmos.DrawWireSphere(sweepPoint.position, _attackRadius);
        }
    }
}
