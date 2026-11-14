using System.Collections.Generic;
using UnityEngine;

public sealed class PullEnchant : Enchant
{
    private readonly HashSet<Enemy> _pulledEnemies = new();
    private readonly List<Collider2D> _hits = new();

    public override void OnHit(IDamageable target, EAttackType attackType, Vector2 impactPoint)
    {
        if (attackType != EAttackType.Melee || !TryStartCooldown())
            return;

        PullEnchantData data = (PullEnchantData)Data;
        _pulledEnemies.Clear();
        ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(data.EnemyMask);
        Physics2D.OverlapCircle(impactPoint, data.Radius, filter, _hits);
        foreach (Collider2D hit in _hits)
        {
            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy == null || !enemy.isActiveAndEnabled || !_pulledEnemies.Add(enemy))
                continue;

            Vector2 direction = impactPoint - (Vector2)enemy.transform.position;
            float strength = Mathf.Min(Value, direction.magnitude / data.PullDuration);
            enemy.ApplyKnockback(direction, strength, data.PullDuration);
        }
    }
}
