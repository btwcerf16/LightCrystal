using System.Collections.Generic;
using UnityEngine;

public sealed class KnightStrikeSpell : Spell
{
    private readonly HashSet<ActorHealth> _hitTargets = new();

    public float Damage => DamageCalculator.CalculateDamage(((KnightStrikeConfig)SpellData).Damage, EDamageType.Physical, _ownerStats, null);

    protected override void Cast(SpellCastData data)
    {
        KnightStrikeConfig config = (KnightStrikeConfig)SpellData;
        _hitTargets.Clear();
        foreach (Collider2D target in Physics2D.OverlapCircleAll(_owner.transform.position, config.Radius))
        {
            Enemy enemy = target.GetComponentInParent<Enemy>();
            if (enemy == null)
                continue;
            ActorHealth health = enemy.GetComponent<ActorHealth>();
            if (health != null && _hitTargets.Add(health))
                health.TakeDamage(config.Damage, EDamageType.Physical, _ownerStats, critical: false, isMelee: true);
        }
    }
}
