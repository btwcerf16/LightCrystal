using System;

public interface IDamageable
{
    event Action<float, float> HealthChanged;
    event Action Died;

    float CurrentHealth { get; }
    float MaxHealth { get; }
    bool IsDead { get; }

    float TakeDamage(float amount, EDamageType damageType, ActorStats attacker = null);
    float Heal(float amount);
    void Kill();
}
