using UnityEngine;

public sealed class DimLamp : Item
{
    [SerializeField] private EffectData _restorationEffect;
    [SerializeField] private float _madnessResistance = 0.15f;
    public override string DisplayName => "Тусклая лампа";
    public float MadnessResistancePercent => _madnessResistance * 100f;
    public EffectData RestorationEffect => _restorationEffect;

    public override void Equip()
    {
        if (IsEquipped)
            return;
        base.Equip();
        if (IsServer)
            _ownerStats.MadnessResistance.AddModifier(new StatModifier(_madnessResistance, StatModifierType.Flat, this));
    }

    public override void Drop()
    {
        if (IsServer)
            _ownerStats.MadnessResistance.RemoveModifiersFrom(this);
        base.Drop();
    }

    public override void Use()
    {
        if (IsServer)
            _ownerStats.GetComponent<EffectHandler>().AddEffect(_restorationEffect);
    }
}
