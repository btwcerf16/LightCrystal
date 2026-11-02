using UnityEngine;

public sealed class OctarineCore : Item
{
    [SerializeField] private float _healthBonus = 20f;
    [SerializeField] private float _sanityBonus = 30f;
    [SerializeField, Range(0f, 0.9f)] private float _spellCooldownReduction = 0.2f;
    public override string DisplayName => "Октариновое ядро";
    public float HealthBonus => _healthBonus;
    public float SanityBonus => _sanityBonus;
    public float SpellCooldownReduction => _spellCooldownReduction;
    public float SpellCooldownReductionPercent => _spellCooldownReduction * 100f;

    public override void Equip()
    {
        if (IsEquipped)
            return;
        base.Equip();
        if (!IsServer)
            return;
        _ownerStats.MaxHealth.AddModifier(new StatModifier(_healthBonus, StatModifierType.Flat, this));
        _ownerStats.MaxSanity.AddModifier(new StatModifier(_sanityBonus, StatModifierType.Flat, this));
    }

    public override void Drop()
    {
        if (IsServer)
        {
            _ownerStats.MaxHealth.RemoveModifiersFrom(this);
            _ownerStats.MaxSanity.RemoveModifiersFrom(this);
        }
        base.Drop();
    }

    public override void Use() { }
}
