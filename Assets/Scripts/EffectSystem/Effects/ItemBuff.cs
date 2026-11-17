public sealed class ItemBuff : Effect
{
    public override void EffectStart(ActorStats owner)
    {
        ItemBuffData data = (ItemBuffData)EffectData;
        if (data.MoveSpeed != 0f)
            owner.MoveSpeed.AddModifier(new StatModifier(data.MoveSpeed, StatModifierType.AdditivePercent, this));
        if (data.CriticalDamage != 0f)
            owner.CriticalDamageMultiplier.AddModifier(new StatModifier(data.CriticalDamage, StatModifierType.Flat, this));
        if (data.AttackSpeed != 0f)
            owner.AttackSpeed.AddModifier(new StatModifier(data.AttackSpeed, StatModifierType.Flat, this));
        if (data.CastSpeedBonus != 0f)
            owner.CastSpeed.AddModifier(new StatModifier(data.CastSpeedBonus, StatModifierType.AdditivePercent, this));
        if (data.CriticalChance != 0f)
            owner.CriticalChance.AddModifier(new StatModifier(data.CriticalChance, StatModifierType.Flat, this));
        if (data.CameraDistance != 0f)
            owner.CameraDistance.AddModifier(new StatModifier(data.CameraDistance, StatModifierType.AdditivePercent, this));
        if (data.MadnessResistance != 0f)
            owner.MadnessResistance.AddModifier(new StatModifier(data.MadnessResistance, StatModifierType.Flat, this));
        if (data.ViewDistance != 0f)
            owner.ViewDistance.AddModifier(new StatModifier(data.ViewDistance, StatModifierType.Flat, this));
        if (data.ViewAngleBonus != 0f)
            owner.ViewAngle.AddModifier(new StatModifier(data.ViewAngleBonus, StatModifierType.Flat, this));
        if (data.ViewAngleMultiplier != 1f)
            owner.ViewAngle.AddModifier(new StatModifier(data.ViewAngleMultiplier, StatModifierType.Multiplier, this));
        if (data.MoveSpeedMultiplier != 1f)
            owner.MoveSpeed.AddModifier(new StatModifier(data.MoveSpeedMultiplier, StatModifierType.Multiplier, this));
        if (data.ProjectileSpeed != 0f)
            owner.ProjectileSpeed.AddModifier(new StatModifier(data.ProjectileSpeed, StatModifierType.Flat, this));
        if (data.ProjectileSpeedBonus != 0f)
            owner.ProjectileSpeedMultiplier.AddModifier(new StatModifier(data.ProjectileSpeedBonus, StatModifierType.AdditivePercent, this));
        if (data.ProjectileSpeedMultiplier != 1f)
            owner.ProjectileSpeedMultiplier.AddModifier(new StatModifier(data.ProjectileSpeedMultiplier, StatModifierType.Multiplier, this));
        if (data.ProjectileRange != 0f)
            owner.ProjectileRange.AddModifier(new StatModifier(data.ProjectileRange, StatModifierType.Flat, this));
        if (data.ProjectileRangeMultiplier != 1f)
            owner.ProjectileRangeMultiplier.AddModifier(new StatModifier(data.ProjectileRangeMultiplier, StatModifierType.Multiplier, this));
        if (data.ProjectilePiercing != 0f)
            owner.ProjectilePiercing.AddModifier(new StatModifier(data.ProjectilePiercing, StatModifierType.Flat, this));
        if (data.FreeSpellCooldowns)
            owner.GetComponent<SpellHolder>()?.ResetCooldowns();
    }

    public override void EffectEnd(ActorStats owner)
    {
        owner.MoveSpeed.RemoveModifiersFrom(this);
        owner.CriticalDamageMultiplier.RemoveModifiersFrom(this);
        owner.AttackSpeed.RemoveModifiersFrom(this);
        owner.CastSpeed.RemoveModifiersFrom(this);
        owner.CriticalChance.RemoveModifiersFrom(this);
        owner.CameraDistance.RemoveModifiersFrom(this);
        owner.MadnessResistance.RemoveModifiersFrom(this);
        owner.ViewDistance.RemoveModifiersFrom(this);
        owner.ViewAngle.RemoveModifiersFrom(this);
        owner.ProjectileSpeed.RemoveModifiersFrom(this);
        owner.ProjectileSpeedMultiplier.RemoveModifiersFrom(this);
        owner.ProjectileRange.RemoveModifiersFrom(this);
        owner.ProjectileRangeMultiplier.RemoveModifiersFrom(this);
        owner.ProjectilePiercing.RemoveModifiersFrom(this);
    }
}
