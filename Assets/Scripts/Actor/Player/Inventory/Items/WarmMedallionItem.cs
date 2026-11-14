using UnityEngine;

public sealed class WarmMedallionItem : Item
{
    [SerializeField] private float _sanityBonus = 10f;
    [SerializeField] private float _madnessResistance = 0.1f;
    public override string DisplayName => "Медальон с потёртой картинкой";
    public float SanityBonus => _sanityBonus;
    public float MadnessResistancePercent => _madnessResistance * 100f;

    public override void Equip()
    {
        if (IsEquipped)
            return;
        base.Equip();
        if (!IsServer)
            return;
        _ownerStats.MaxSanity.AddModifier(new StatModifier(_sanityBonus, StatModifierType.Flat, this));
        _ownerStats.MadnessResistance.AddModifier(new StatModifier(_madnessResistance, StatModifierType.Flat, this));
    }

    public override void Drop()
    {
        if (IsServer)
        {
            _ownerStats.MaxSanity.RemoveModifiersFrom(this);
            _ownerStats.MadnessResistance.RemoveModifiersFrom(this);
        }
        base.Drop();
    }

    public override void Use() { }
}
