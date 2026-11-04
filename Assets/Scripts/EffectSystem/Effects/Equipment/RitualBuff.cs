public sealed class RitualBuff : Effect
{
    public override void EffectStart(ActorStats owner)
    {
        RitualBuffData data = (RitualBuffData)EffectData;
        owner.SanityCostReduction.AddModifier(new StatModifier(data.SanityCostReduction, StatModifierType.Flat, this));
        owner.MadnessResistance.AddModifier(new StatModifier(data.MadnessResistance, StatModifierType.Flat, this));
        owner.MaxSanity.AddModifier(new StatModifier(-data.MaxSanityPenalty, StatModifierType.AdditivePercent, this));
    }

    public override void EffectEnd(ActorStats owner)
    {
        owner.SanityCostReduction.RemoveModifiersFrom(this);
        owner.MadnessResistance.RemoveModifiersFrom(this);
        owner.MaxSanity.RemoveModifiersFrom(this);
        if (TimeRemaining <= 0f && !owner.GetComponent<ActorHealth>().IsDead)
            owner.GetComponent<ActorSanity>().RestoreSanity(owner.MaxSanity.Value * ((RitualBuffData)EffectData).RestoreSanityOnExpiry);
    }
}
