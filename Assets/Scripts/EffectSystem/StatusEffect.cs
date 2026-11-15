public sealed class StatusEffect : Effect
{
    public override void EffectStart(ActorStats owner)
    {
        StatusEffectData data = (StatusEffectData)EffectData;
        if (data.ViewDistanceMultiplier != 1f)
            owner.ViewDistance.AddModifier(new StatModifier(data.ViewDistanceMultiplier, StatModifierType.Multiplier, this));
    }

    public override void EffectEnd(ActorStats owner)
    {
        owner.ViewDistance.RemoveModifiersFrom(this);
    }
}
