using UnityEngine;

public sealed class ChurchFireProjectile : Projectile
{
    private ChurchBlacklistItem _source;
    public void SetSource(ChurchBlacklistItem source) => _source = source;
    protected override bool CanHit(Collider2D target)
    {
        return base.CanHit(target) && (target.GetComponentInParent<Enemy>() != null ||
            (!target.isTrigger && target.GetComponentInParent<IDamageable>() == null));
    }

    public override void OnHit(Collider2D target)
    {
        if (!IsFlying || !CanHit(target)) return;
        Enemy enemy = target.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            ActorHealth health = enemy.GetComponent<ActorHealth>();
            health.TakeDamage(Data.Damage, Data.DamageType, Data.Attacker);
            if (!health.IsDead && _source != null) _source.ApplyBrand(health, Data);
        }
        Finish();
    }

    protected override void OnDisable()
    {
        _source = null;
        base.OnDisable();
    }
}
