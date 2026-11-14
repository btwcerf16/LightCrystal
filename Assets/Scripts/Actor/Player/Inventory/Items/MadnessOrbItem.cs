using UnityEngine;

public sealed class MadnessOrbItem : Item
{
    [SerializeField] private float _sanityPenalty = 0.4f;
    [SerializeField] private float _magicDamageBonus = 0.35f;
    public override string DisplayName => "Сфера безумия";
    public float SanityPenaltyPercent => _sanityPenalty * 100f;
    public float MagicDamageBonusPercent => _magicDamageBonus * 100f;

    public override void Equip()
    {
        if (IsEquipped)
            return;
        base.Equip();
        if (!IsServer)
            return;
        _ownerStats.MaxSanity.AddModifier(new StatModifier(1f - _sanityPenalty, StatModifierType.Multiplier, this));
        _ownerStats.MagicDamageMultiplier.AddModifier(new StatModifier(_magicDamageBonus, StatModifierType.Flat, this));
    }

    public override void Drop()
    {
        if (IsServer)
        {
            _ownerStats.MaxSanity.RemoveModifiersFrom(this);
            _ownerStats.MagicDamageMultiplier.RemoveModifiersFrom(this);
        }
        base.Drop();
    }

    public override void Use() { }
}
