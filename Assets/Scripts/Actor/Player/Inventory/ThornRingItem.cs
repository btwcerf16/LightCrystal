using UnityEngine;

public sealed class ThornRingItem : Item
{
    [SerializeField] private float _madnessResistance = 0.15f;
    [SerializeField] private float _magicDamageBonus = 0.08f;
    [SerializeField] private float _healthPenalty = 20f;
    public override string DisplayName => "Кольцо с шипом";
    public float MadnessResistancePercent => _madnessResistance * 100f;
    public float MagicDamageBonusPercent => _magicDamageBonus * 100f;
    public float HealthPenalty => _healthPenalty;

    public override void Equip()
    {
        if (IsEquipped)
            return;
        base.Equip();
        if (!IsServer)
            return;
        _ownerStats.MadnessResistance.AddModifier(new StatModifier(_madnessResistance, StatModifierType.Flat, this));
        _ownerStats.MagicDamageMultiplier.AddModifier(new StatModifier(_magicDamageBonus, StatModifierType.Flat, this));
        _ownerStats.MaxHealth.AddModifier(new StatModifier(-_healthPenalty, StatModifierType.Flat, this));
    }

    public override void Drop()
    {
        if (IsServer)
        {
            _ownerStats.MadnessResistance.RemoveModifiersFrom(this);
            _ownerStats.MagicDamageMultiplier.RemoveModifiersFrom(this);
            _ownerStats.MaxHealth.RemoveModifiersFrom(this);
        }
        base.Drop();
    }

    public override void Use() { }
}
