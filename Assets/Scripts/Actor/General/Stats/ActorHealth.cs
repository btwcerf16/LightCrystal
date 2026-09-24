using System;
using UnityEngine;

[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(ActorStats))]
public sealed class ActorHealth : MonoBehaviour, IDamageable
{
    private ActorStats _stats;

    public event Action<float, float> HealthChanged;
    public event Action Died;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => _stats.MaxHealth.Value;
    public bool IsDead { get; private set; }

    private void Awake()
    {
        _stats = GetComponent<ActorStats>();
        CurrentHealth = MaxHealth;
    }

    private void OnEnable()
    {
        _stats.MaxHealth.ValueChanged += OnMaxHealthChanged;
    }

    private void OnDisable()
    {
        _stats.MaxHealth.ValueChanged -= OnMaxHealthChanged;
    }

    public float TakeDamage(float amount)
    {
        if (amount <= 0f || IsDead)
            return 0f;

        float previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0f);
        float dealtDamage = previousHealth - CurrentHealth;

        HealthChanged?.Invoke(CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0f)
            Kill();

        return dealtDamage;
    }

    public float Heal(float amount)
    {
        if (amount <= 0f || IsDead)
            return 0f;

        float previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
        float restoredHealth = CurrentHealth - previousHealth;

        if (restoredHealth > 0f)
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);

        return restoredHealth;
    }
    public void Kill()
    {
        if (IsDead)
            return;

        CurrentHealth = 0f;
        IsDead = true;

        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        Died?.Invoke();
    }

    private void OnMaxHealthChanged(float maxHealth)
    {
        CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}