using System.Collections.Generic;
using UnityEngine;

public sealed class ExplosionEnchant : Enchant
{
    private bool _isExploding;

    public override void OnKill(ActorHealth target)
    {
        if (_isExploding || target.GetComponent<Enemy>() == null)
            return;
        _isExploding = true;
        try
        {
            float radius = ((ExplosionEnchantData)Data).Radius;
            NetworkActorVisuals visuals = target.GetComponent<NetworkActorVisuals>();
            if (visuals != null)
                visuals.PlayExplosion(radius);
            HashSet<ActorHealth> hit = new();
            foreach (Collider2D collider in Physics2D.OverlapCircleAll(target.transform.position, radius))
            {
                ActorHealth health = collider.GetComponentInParent<ActorHealth>();
                if (health == null || health.IsDead || health.GetComponent<Enemy>() == null || !hit.Add(health))
                    continue;
                health.TakeDamage(Value, EDamageType.Magic, Weapon.OwnerStats);
                if (health.IsDead)
                    Weapon.NotifyKill(health);
            }
        }
        finally
        {
            _isExploding = false;
        }
    }
}
