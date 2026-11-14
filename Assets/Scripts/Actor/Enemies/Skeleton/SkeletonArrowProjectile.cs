using Unity.Netcode;
using UnityEngine;

public sealed class SkeletonArrowProjectile : Projectile
{
    public override void OnHit(Collider2D collider)
    {
        if (!IsFlying)
            return;

        NetworkWorldAudio.Play(GetComponent<NetworkObject>().NetworkManager, WorldSoundId.SkeletonArrowImpact, collider.ClosestPoint(transform.position));
        base.OnHit(collider);
    }

    protected override bool CanHit(Collider2D collider)
    {
        return base.CanHit(collider) && collider.GetComponentInParent<Enemy>() == null;
    }
}
