using UnityEngine;

public sealed class EmpiricalLearningSpell : PassiveSpell
{
    public const int MaxStacks = 20;
    private ActorHealth _health;
    private ActorSanity _sanity;
    private EffectHandler _effects;
    private float _partialStack;
    private EffectData StackEffect => ((EmpiricalLearningConfig)SpellData).StackEffect;

    public int Stacks
    {
        get
        {
            int count = 0;
            if (_effects == null)
                return count;
            foreach (Effect effect in _effects.ActiveEffects)
                if (effect.EffectData == StackEffect)
                    count++;
            return count;
        }
    }

    protected override void Apply()
    {
        _health = GetComponent<ActorHealth>();
        _sanity = GetComponent<ActorSanity>();
        _effects = GetComponent<EffectHandler>();
        _health.DamageReceived += OnDamageReceived;
        _sanity.SanityDrained += OnSanityDrained;
        _effects.EffectApplied += OnEffectApplied;
    }

    protected override void Remove()
    {
        _health.DamageReceived -= OnDamageReceived;
        _sanity.SanityDrained -= OnSanityDrained;
        _effects.EffectApplied -= OnEffectApplied;
        ClearStacks();
    }

    private void OnDamageReceived(float damage, EDamageType type, bool critical)
    {
        if (type == EDamageType.Magic && damage > 0f)
            AddStacks(3f);
    }

    private void OnSanityDrained(float amount) => AddStacks(amount * 0.5f);

    private void OnEffectApplied(EffectData data)
    {
        if (!data.IsBuff)
            AddStacks(2f);
    }

    public void AddStacks(float amount)
    {
        int remaining = MaxStacks - Stacks;
        if (_health.IsDead || remaining <= 0)
            return;

        float progress = Mathf.Min(remaining, _partialStack + amount);
        int wholeStacks = Mathf.FloorToInt(progress);
        _partialStack = progress - wholeStacks;
        for (int i = 0; i < wholeStacks; i++)
            _effects.AddEffect(StackEffect);
    }

    public bool SpendStacks()
    {
        if (Stacks < MaxStacks)
            return false;

        ClearStacks();
        return true;
    }

    private void ClearStacks()
    {
        _partialStack = 0f;
        for (int i = _effects.ActiveEffects.Count - 1; i >= 0; i--)
        {
            Effect effect = _effects.ActiveEffects[i];
            if (effect.EffectData == StackEffect)
                _effects.RemoveEffect(effect);
        }
    }
}
