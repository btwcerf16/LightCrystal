using System.Collections.Generic;
using UnityEngine;

public sealed class KnightStrikeSpell : Spell
{
    private readonly HashSet<IDamageable> _hitTargets = new();

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
            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null && _hitTargets.Add(damageable))
                damageable.TakeDamage(config.Damage, EDamageType.Physical, _ownerStats);
        }
    }
}
