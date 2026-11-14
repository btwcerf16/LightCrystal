using UnityEngine;

public sealed class DamageTrap : Trap
{
    protected override void OnCaught(Collider2D targetCollider)
    {
        ActorHealth caughtHealth = targetCollider.GetComponentInParent<ActorHealth>();
        float dealtDamage = DamageCalculator.DealCriticalDamage(caughtHealth, TrapParameters.Damage, TrapParameters.DamageType, _owner);

        if (dealtDamage > 0f && TrapParameters.SourceWeapon != null && TrapParameters.SourceWeapon.OwnerStats == _owner)
            TrapParameters.SourceWeapon.NotifyHit(caughtHealth, EAttackType.Range);
        base.OnCaught(targetCollider);
    }
}
