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
        base.OnHit(target);
    }
}
