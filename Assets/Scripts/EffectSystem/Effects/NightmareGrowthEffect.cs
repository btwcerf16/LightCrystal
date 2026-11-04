public sealed class NightmareGrowthEffect : Effect
{
    public override void EffectStart(ActorStats owner)
    {
        NightmareGrowthData data = (NightmareGrowthData)EffectData;
        ActorHealth health = owner.GetComponent<ActorHealth>();
        float previousMaxHealth = health.MaxHealth;
        owner.MaxHealth.AddModifier(new StatModifier(data.HealthBonus, StatModifierType.AdditivePercent, this));
        owner.PhysicalDamage.AddModifier(new StatModifier(data.BaseDamageBonus, StatModifierType.AdditivePercent, this));
        owner.PhysicalResistance.AddModifier(new StatModifier(data.PhysicalResistanceBonus, StatModifierType.Flat, this));
        owner.MagicResistance.AddModifier(new StatModifier(data.MagicResistanceBonus, StatModifierType.Flat, this));
        health.Heal(health.MaxHealth - previousMaxHealth);
    }

    public override void EffectEnd(ActorStats owner)
    {
        owner.MaxHealth.RemoveModifiersFrom(this);
        owner.PhysicalDamage.RemoveModifiersFrom(this);
        owner.PhysicalResistance.RemoveModifiersFrom(this);
        owner.MagicResistance.RemoveModifiersFrom(this);
    }
}
