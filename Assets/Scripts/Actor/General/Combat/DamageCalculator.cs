using System;
using UnityEngine;

public static class DamageCalculator
{
    public const int SensorLayer = 10;
    public const int CombatLayers = ~(1 << SensorLayer);

    public static int GetTargetMask(ActorStats attacker, int configuredMask)
    {
        int mask = configuredMask & CombatLayers;
        if (configuredMask != ~0 || attacker == null)
            return mask;
        int friendlyLayers = attacker.TryGetComponent<Enemy>(out _)
            ? LayerMask.GetMask("Enemy")
            : LayerMask.GetMask("Player", "LocalPlayer");
        return mask & ~friendlyLayers;
    }

    public static float RollCriticalDamage(float damage, ActorStats attacker, float additionalCriticalChance = 0f)
    {
        return RollCriticalDamage(damage, attacker, additionalCriticalChance, out _);
    }

    private static float RollCriticalDamage(float damage, ActorStats attacker, float additionalCriticalChance, out bool critical)
    {
        critical = false;
        if (attacker == null)
            return damage;

        float criticalChance = Mathf.Clamp01(attacker.CriticalChance.Value + additionalCriticalChance);
        critical = UnityEngine.Random.value < criticalChance || criticalChance >= 1f;
        return critical ? damage * attacker.CriticalDamageMultiplier.Value : damage;
    }

    public static float DealCriticalDamage(IDamageable target, float damage, EDamageType type, ActorStats attacker, bool isMelee = false, float additionalCriticalChance = 0f)
    {
        return DealCriticalDamage(target, damage, type, attacker, out _, isMelee, additionalCriticalChance);
    }

    public static float DealCriticalDamage(IDamageable target, float damage, EDamageType type, ActorStats attacker, out bool critical, bool isMelee = false, float additionalCriticalChance = 0f)
    {
        damage = RollCriticalDamage(damage, attacker, additionalCriticalChance, out critical);
        return target is ActorHealth health ? health.TakeDamage(damage, type, attacker, critical, isMelee)
            : target.TakeDamage(damage, type, attacker);
    }

    public static float CalculateDamage(float damage, EDamageType damageType, ActorStats attacker, ActorStats target, float additionalCriticalChance)
    {
        float criticalDamage = RollCriticalDamage(damage, attacker, additionalCriticalChance);
        return CalculateDamage(criticalDamage, damageType, attacker, target);
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
