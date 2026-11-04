public sealed class ItemBuff : Effect
{
    public override void EffectStart(ActorStats owner)
    {
        ItemBuffData data = (ItemBuffData)EffectData;
        owner.MoveSpeed.AddModifier(new StatModifier(data.MoveSpeed, StatModifierType.AdditivePercent, this));
        owner.CriticalDamageMultiplier.AddModifier(new StatModifier(data.CriticalDamage, StatModifierType.Flat, this));
        owner.AttackSpeed.AddModifier(new StatModifier(data.AttackSpeed, StatModifierType.Flat, this));
        owner.CriticalChance.AddModifier(new StatModifier(data.CriticalChance, StatModifierType.Flat, this));
        owner.CameraDistance.AddModifier(new StatModifier(data.CameraDistance, StatModifierType.AdditivePercent, this));
        owner.MadnessResistance.AddModifier(new StatModifier(data.MadnessResistance, StatModifierType.Flat, this));
        if (data.FreeSpellCooldowns)
            owner.GetComponent<SpellHolder>()?.ResetCooldowns();
    }

    public override void EffectEnd(ActorStats owner)
    {
        owner.MoveSpeed.RemoveModifiersFrom(this);
        owner.CriticalDamageMultiplier.RemoveModifiersFrom(this);
        owner.AttackSpeed.RemoveModifiersFrom(this);
        owner.CriticalChance.RemoveModifiersFrom(this);
        owner.CameraDistance.RemoveModifiersFrom(this);
        owner.MadnessResistance.RemoveModifiersFrom(this);
    }
}
