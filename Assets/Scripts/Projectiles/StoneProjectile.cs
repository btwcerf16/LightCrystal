using UnityEngine;

public sealed class StoneProjectile : Projectile
{
    protected override bool CanHit(Collider2D targetCollider)
    {
        return base.CanHit(targetCollider) && (targetCollider.GetComponentInParent<Enemy>() != null
            || (!targetCollider.isTrigger && targetCollider.GetComponentInParent<IDamageable>() == null));
    }
}
