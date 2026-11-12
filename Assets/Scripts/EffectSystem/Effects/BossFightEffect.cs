public sealed class BossFightEffect : Effect
{
    public override void EffectStart(ActorStats owner)
    {
        BossFightData data = (BossFightData)EffectData;
        owner.ReviveSpeed.AddModifier(new StatModifier(1f - data.ReviveSlow, StatModifierType.Multiplier, this));
    }

    public override void EffectEnd(ActorStats owner)
    {
        owner.ReviveSpeed.RemoveModifiersFrom(this);
    }
}
