using System;
using UnityEngine;

[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(ActorStats))]
public sealed class ActorHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private LayerMask _damageSourceMask = ~0;
    [SerializeField] private EffectData _downedEffect;
    private ActorStats _stats;
    private EffectHandler _effects;

    public event Action<float, float> HealthChanged;
    public event Action<float> DamageTaken;
    public event Action<float, EDamageType, bool> DamageReceived;
    public event Action<float> DamageDealt;
    public event Action Died;
    public event Action<ActorHealth> EnemyKilled;
    public event Action<ActorHealth> EnemyHit;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => _stats.MaxHealth.Value;
    public bool IsDead { get; private set; }
    public float ReviveProgress { get; internal set; }
    public SanityShield Shield { get; internal set; }

    private void Awake()
    {
        _stats = GetComponent<ActorStats>();
        _effects = GetComponent<EffectHandler>();
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

    public float TakeDamage(float amount, EDamageType damageType, ActorStats attacker = null)
    {
        return TakeDamage(amount, damageType, attacker, false);
    }

    public float TakeDamage(float amount, EDamageType damageType, ActorStats attacker, bool critical)
    {
        if (amount <= 0f || IsDead || (_effects != null && (_effects.Status & EActorStatus.Invulnerable) != 0))
            return 0f;
        if (_damageSourceMask.value != -1 && (attacker == null
            || (_damageSourceMask.value & (1 << attacker.gameObject.layer)) == 0))
            return 0f;

        amount = DamageCalculator.CalculateDamage(amount, damageType, attacker, _stats);
        if (amount <= 0f)
            return 0f;

        float incomingDamage = amount;
        if (Shield != null)
            amount = Shield.AbsorbDamage(amount);
        float absorbedDamage = incomingDamage - amount;
        float previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0f);
        float dealtDamage = previousHealth - CurrentHealth + absorbedDamage;

        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        DamageReceived?.Invoke(dealtDamage, damageType, critical);
        DamageTaken?.Invoke(dealtDamage);
        if (attacker != null && attacker.gameObject != gameObject)
        {
            ActorHealth attackerHealth = attacker.GetComponent<ActorHealth>();
            if (attackerHealth != null)
                attackerHealth.DamageDealt?.Invoke(dealtDamage);
        }

        if (CurrentHealth <= 0f)
        {
            Kill();
            Enemy enemy = GetComponent<Enemy>();
            if (attacker != null && attacker.gameObject != gameObject && enemy != null)
            {
                enemy.GiveGold(attacker.GetComponent<ActorWallet>());
                ActorHealth killer = attacker.GetComponent<ActorHealth>();
                if (killer != null)
                    killer.EnemyKilled?.Invoke(this);
            }
        }

        if (attacker != null && attacker.gameObject != gameObject && GetComponent<Enemy>() != null)
            attacker.GetComponent<ActorHealth>()?.EnemyHit?.Invoke(this);
        return dealtDamage;
    }

    public void ResetHealth() => Revive(MaxHealth);

    public void Revive(float health)
    {
        IsDead = false;
        CurrentHealth = Mathf.Clamp(health, 1f, MaxHealth);
        if (_effects != null)
            for (int i = _effects.ActiveEffects.Count - 1; i >= 0; i--)
                if ((_effects.ActiveEffects[i].EffectData.Status & EActorStatus.Downed) != 0)
                    _effects.RemoveEffect(_effects.ActiveEffects[i]);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
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
        if (_downedEffect != null && _effects != null)
            _effects.AddEffect(_downedEffect);

        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        Died?.Invoke();
    }

    private void OnMaxHealthChanged(float maxHealth)
    {
        CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}
