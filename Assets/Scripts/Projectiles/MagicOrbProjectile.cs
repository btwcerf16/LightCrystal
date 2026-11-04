using System.Collections.Generic;
using UnityEngine;

public sealed class MagicOrbProjectile : Projectile
{
    [SerializeField, Min(0f)] private float _explosionRadius = 1.5f;
    private readonly HashSet<Enemy> _targets = new();
    public event System.Action<Vector3, float> Exploded;

    protected override bool CanHit(Collider2D collider)
    {
        if (!base.CanHit(collider))
            return false;
        return collider.GetComponentInParent<Enemy>() != null ||
            (!collider.isTrigger && collider.GetComponentInParent<IDamageable>() == null);
    }

    public override void OnHit(Collider2D collider)
    {
        if (!IsFlying || !CanHit(collider))
            return;
        Explode(collider.GetComponentInParent<Enemy>() != null);
    }

    protected override void OnLifetimeExpired() => Explode(true);

    private void Explode(bool dealDamage)
    {
        if (!IsFlying)
            return;
        if (dealDamage)
        {
            _targets.Clear();
            foreach (Collider2D target in Physics2D.OverlapCircleAll(transform.position, _explosionRadius, Data.LayerMask))
            {
                Enemy enemy = target.GetComponentInParent<Enemy>();
                if (enemy != null && _targets.Add(enemy))
                    enemy.GetComponent<ActorHealth>().TakeDamage(Data.Damage, EDamageType.Magic, Data.Attacker);
            }
            _targets.Clear();
        }
        Exploded?.Invoke(transform.position, _explosionRadius);
        Finish();
    }
}
