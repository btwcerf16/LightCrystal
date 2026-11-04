using UnityEngine;

public sealed class SkeletonArrowProjectile : Projectile
{
    protected override bool CanHit(Collider2D collider)
    {
        return base.CanHit(collider) && collider.GetComponentInParent<Enemy>() == null;
    }
}
