public sealed class MadnessEffect : Effect
{
    private ActorStats _owner;
    private ActorSanity _sanity;
    public int Stage { get; private set; }

    public override void EffectStart(ActorStats owner)
    {
        _owner = owner;
        _sanity = owner.GetComponent<ActorSanity>();
        _sanity.SanityChanged += UpdateStage;
        UpdateStage(_sanity.CurrentSanity, _sanity.MaxSanity);
    }

    private void UpdateStage(float currentSanity, float maxSanity)
    {
        if (_owner == null)
            return;
        int stage = 1;
        if (currentSanity <= maxSanity * 0.1f)
            stage = 4;
        else if (currentSanity <= maxSanity * 0.2f)
            stage = 3;
        else if (currentSanity <= maxSanity * 0.3f)
            stage = 2;
        if (stage <= Stage)
            return;
        Stage = stage;
        MadnessEffectData data = (MadnessEffectData)EffectData;
        RemoveModifiers(_owner);
        _owner.ViewDistance.AddModifier(new StatModifier(1f - data.ViewDistancePenalty, StatModifierType.Multiplier, this));
        if (Stage >= 2)
        {
            _owner.PhysicalDamageMultiplier.AddModifier(new StatModifier(1f - data.PhysicalDamagePenalty, StatModifierType.Multiplier, this));
            _owner.MagicDamageMultiplier.AddModifier(new StatModifier(1f - data.MagicAmplificationPenalty, StatModifierType.Multiplier, this));
        }
        if (Stage >= 3)
        {
            _owner.PhysicalResistance.AddModifier(new StatModifier(-data.ResistancePenalty, StatModifierType.Flat, this));
            _owner.MagicResistance.AddModifier(new StatModifier(-data.ResistancePenalty, StatModifierType.Flat, this));
        }
        if (Stage >= 4)
            _owner.MoveSpeed.AddModifier(new StatModifier(1f - data.MoveSpeedPenalty, StatModifierType.Multiplier, this));
    }

    private void RemoveModifiers(ActorStats owner)
    {
        owner.ViewDistance.RemoveModifiersFrom(this);
        owner.PhysicalDamageMultiplier.RemoveModifiersFrom(this);
        owner.MagicDamageMultiplier.RemoveModifiersFrom(this);
        owner.PhysicalResistance.RemoveModifiersFrom(this);
        owner.MagicResistance.RemoveModifiersFrom(this);
        owner.MoveSpeed.RemoveModifiersFrom(this);
    }

    public override void EffectEnd(ActorStats owner)
    {
        _sanity.SanityChanged -= UpdateStage;
        _owner = null;
        Stage = 0;
        RemoveModifiers(owner);
    }
}
