using System;
using UnityEngine;

public static class DamageCalculator
{
    public static float CalculateDamage(float damage, EDamageType damageType, ActorStats attacker, ActorStats target)
    {
        damage = Mathf.Max(0f, damage);
        float multiplier = 1f;
        float resistance = 0f;

        switch (damageType)
        {
            case EDamageType.Physical:
                if (attacker != null)
                    multiplier = attacker.PhysicalDamageMultiplier.Value;
                if (target != null)
                    resistance = target.PhysicalResistance.Value;
                break;

            case EDamageType.Magic:
                if (attacker != null)
                    multiplier = attacker.MagicDamageMultiplier.Value;
                if (target != null)
                    resistance = target.MagicResistance.Value;
                break;

            case EDamageType.Pure:
                return damage;

            default:
                throw new ArgumentOutOfRangeException(nameof(damageType));
        }

        return damage * multiplier * (1f - resistance);
    }
}
