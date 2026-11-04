using UnityEngine;

public sealed class SpicedHerbsItem : Item
{
    [SerializeField] private float _costReduction = 0.05f;
    [SerializeField] private float _madnessResistance = 0.1f;
    [SerializeField] private float _missingSanityRestore = 0.2f;
    [SerializeField] private RitualBuffData _aftertaste;
    public float CostReduction => _costReduction;
    public float MadnessResistance => _madnessResistance;
    public float MissingSanityRestore => _missingSanityRestore;
    public override string DisplayName => "Пучок пряных трав";

    public override void Equip()
    {
        if (IsEquipped) return;
        base.Equip();
        if (!IsServer) return;
        _ownerStats.SanityCostReduction.AddModifier(new StatModifier(_costReduction, StatModifierType.Flat, this));
        _ownerStats.MadnessResistance.AddModifier(new StatModifier(_madnessResistance, StatModifierType.Flat, this));
    }

    public override void Drop()
    {
        if (IsServer)
        {
            _ownerStats.SanityCostReduction.RemoveModifiersFrom(this);
            _ownerStats.MadnessResistance.RemoveModifiersFrom(this);
        }
        base.Drop();
    }

    protected override bool CanUse() => !DestroyOnDrop && _aftertaste != null;

    public override void Use()
    {
        if (!IsServer || DestroyOnDrop) return;
        ActorSanity sanity = _ownerStats.GetComponent<ActorSanity>();
        sanity.RestoreSanity((sanity.MaxSanity - sanity.CurrentSanity) * _missingSanityRestore);
        _ownerStats.GetComponent<EffectHandler>().AddEffect(_aftertaste);
        DestroyOnDrop = true;
    }
}
