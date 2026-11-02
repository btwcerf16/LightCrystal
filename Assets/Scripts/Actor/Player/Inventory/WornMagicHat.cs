using UnityEngine;

public sealed class WornMagicHat : Item
{
    [SerializeField] private float _sanityBonus = 10f;
    [SerializeField] private float _magicDamageBonus = 0.05f;
    public override string DisplayName => "Потёртая магическая шляпа";
    public float SanityBonus => _sanityBonus;
    public float MagicDamageBonusPercent => _magicDamageBonus * 100f;

    public override void Equip()
    {
        if (IsEquipped)
            return;
        base.Equip();
        if (!IsServer)
            return;
        _ownerStats.MaxSanity.AddModifier(new StatModifier(_sanityBonus, StatModifierType.Flat, this));
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
