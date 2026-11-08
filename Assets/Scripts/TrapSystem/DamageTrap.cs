using UnityEngine;

public sealed class DamageTrap : Trap
{
    protected override void OnCaught(Collider2D targetCollider)
    {
        ActorHealth caughtHealth = targetCollider.GetComponentInParent<ActorHealth>();
        float dealtDamage = DamageCalculator.DealCriticalDamage(caughtHealth, TrapParameters.Damage,
            TrapParameters.DamageType, _owner);
        if (dealtDamage > 0f && TrapParameters.SourceWeapon != null)
            TrapParameters.SourceWeapon.NotifyHit(caughtHealth);
        base.OnCaught(targetCollider);
    }
}
