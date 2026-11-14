using Unity.Netcode;
using UnityEngine;
public sealed class SpiderWebProjectile : Projectile
{
    protected override bool CanHit(Collider2D target) => base.CanHit(target) && target.GetComponentInParent<Enemy>() == null;
    public override void OnHit(Collider2D target)
    {
        if (!IsFlying)
            return;
        PlayerInputReader player = target.GetComponentInParent<PlayerInputReader>();
        if (player != null && !player.GetComponent<ActorHealth>().IsDead)
            player.GetComponent<EffectHandler>().AddEffect(Data.EffectOnHit);
        NetworkWorldAudio.Play(GetComponent<NetworkObject>().NetworkManager, WorldSoundId.SpiderWebImpact, target.ClosestPoint(transform.position));
        base.OnHit(target);
    }
}
