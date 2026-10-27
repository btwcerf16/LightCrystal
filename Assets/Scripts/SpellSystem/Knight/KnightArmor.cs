public sealed class KnightArmor : PassiveSpell
{
    protected override void Apply()
    {
        _ownerStats.PhysicalResistance.AddModifier(new StatModifier(
            ((KnightArmorConfig)SpellData).Resistance, StatModifierType.Flat, this));
    }

    protected override void Remove()
    {
        _ownerStats.PhysicalResistance.RemoveModifiersFrom(this);
    }
}
