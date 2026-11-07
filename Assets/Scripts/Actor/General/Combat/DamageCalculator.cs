using System;
using UnityEngine;

public static class DamageCalculator
{
    public static float RollCriticalDamage(float damage, ActorStats attacker)
    {
        return RollCriticalDamage(damage, attacker, out _);
    }

    private static float RollCriticalDamage(float damage, ActorStats attacker, out bool critical)
    {
        critical = attacker != null && UnityEngine.Random.value < attacker.CriticalChance.Value;
        return critical ? damage * attacker.CriticalDamageMultiplier.Value : damage;
    }

    public static float DealCriticalDamage(IDamageable target, float damage, EDamageType type, ActorStats attacker)
    {
        damage = RollCriticalDamage(damage, attacker, out bool critical);
        return target is ActorHealth health ? health.TakeDamage(damage, type, attacker, critical)
            : target.TakeDamage(damage, type, attacker);
    }

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
                return attacker != null && attacker.TryGetComponent(out EffectHandler effects) && effects.IsDowned
                    ? damage * 0.1f : damage;

            default:
                throw new ArgumentOutOfRangeException(nameof(damageType));
        }

        return damage * multiplier * (1f - resistance);
    }
}
