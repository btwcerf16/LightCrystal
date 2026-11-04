public sealed class BloodlustEffect : Effect
{
    public override void EffectStart(ActorStats owner)
    {
        BloodlustData data = (BloodlustData)EffectData;
        owner.MoveSpeed.AddModifier(new StatModifier(data.MoveSpeedBonus, StatModifierType.AdditivePercent, this));
        owner.PhysicalDamageMultiplier.AddModifier(new StatModifier(data.PhysicalDamageBonus, StatModifierType.AdditivePercent, this));
    }

    public override void EffectEnd(ActorStats owner)
    {
        owner.MoveSpeed.RemoveModifiersFrom(this);
        owner.PhysicalDamageMultiplier.RemoveModifiersFrom(this);
    }
}
