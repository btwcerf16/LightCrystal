using System.Collections.Generic;
using UnityEngine;

public sealed class CourierWhistleSpell : Spell
{
    protected override void Cast(SpellCastData data)
    {
        var enemies = new HashSet<Enemy>();
        foreach (Collider2D target in Physics2D.OverlapCircleAll(transform.position, ((CourierWhistleConfig)SpellData).Radius))
        {
            Enemy enemy = target.GetComponentInParent<Enemy>();
            if (enemy != null && enemies.Add(enemy) && !enemy.GetComponent<ActorHealth>().IsDead)
            {
                enemy.CancelAttack();
                enemy.SetTemporaryTarget(transform);
            }
        }
    }
}
