public sealed class NyxBackstabBuff : Effect
{
    public override void EffectStart(ActorStats owner)
    {
        var data = (NyxBackstabBuffData)EffectData;
        owner.CriticalChance.AddModifier(new StatModifier(data.CriticalChance * StackCount, StatModifierType.Flat, this));
        owner.CriticalDamageMultiplier.AddModifier(new StatModifier(data.CriticalDamage * StackCount, StatModifierType.Flat, this));
    }

    public override void EffectEnd(ActorStats owner)
    {
        owner.CriticalChance.RemoveModifiersFrom(this);
        owner.CriticalDamageMultiplier.RemoveModifiersFrom(this);
    }
}
