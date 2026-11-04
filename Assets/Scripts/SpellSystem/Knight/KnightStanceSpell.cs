using UnityEngine;

public sealed class KnightStanceSpell : PassiveSpell
{
    private ActorHealth _health;
    private ActorSanity _sanity;
    private int _healthSteps = -1;
    private int _sanitySteps = -1;

    protected override void Apply()
    {
        _health = GetComponent<ActorHealth>();
        _sanity = GetComponent<ActorSanity>();
        _health.HealthChanged += Refresh;
        _sanity.SanityChanged += Refresh;
        Refresh(0f, 0f);
    }

    private void Refresh(float current, float maximum)
    {
        KnightStanceConfig config = (KnightStanceConfig)SpellData;
        int healthSteps = Mathf.FloorToInt((1f - _health.CurrentHealth / Mathf.Max(1f, _health.MaxHealth)) * 10f + 0.0001f);
        int sanitySteps = Mathf.FloorToInt((1f - _sanity.CurrentSanity / Mathf.Max(1f, _sanity.MaxSanity)) * 10f + 0.0001f);
        if (healthSteps != _healthSteps)
        {
            _healthSteps = healthSteps;
            _ownerStats.PhysicalResistance.RemoveModifiersFrom(this);
            _ownerStats.PhysicalResistance.AddModifier(new StatModifier(healthSteps * config.ResistancePerStep, StatModifierType.Flat, this));
        }
        if (sanitySteps != _sanitySteps)
        {
            _sanitySteps = sanitySteps;
            _ownerStats.AttackSpeed.RemoveModifiersFrom(this);
            _ownerStats.AttackSpeed.AddModifier(new StatModifier(sanitySteps * config.AttackSpeedPerStep, StatModifierType.Flat, this));
        }
    }

    protected override void Remove()
    {
        _health.HealthChanged -= Refresh;
        _sanity.SanityChanged -= Refresh;
        _ownerStats.PhysicalResistance.RemoveModifiersFrom(this);
        _ownerStats.AttackSpeed.RemoveModifiersFrom(this);
        _healthSteps = _sanitySteps = -1;
    }
}
