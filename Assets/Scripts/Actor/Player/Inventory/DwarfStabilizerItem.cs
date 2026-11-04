using UnityEngine;

public sealed class DwarfStabilizerItem : Item
{
    [SerializeField] private float _stability = 15f;
    public override string DisplayName => "Дворфийский стабилизатор";
    public float StabilityBonus => _stability;

    public override void Equip()
    {
        if (IsEquipped)
            return;
        base.Equip();
        if (!IsServer)
            return;
        _ownerStats.Stability.AddModifier(new StatModifier(_stability, StatModifierType.Flat, this));
    }

    public override void Drop()
    {
        if (IsServer)
        {
            _ownerStats.Stability.RemoveModifiersFrom(this);
        }
        base.Drop();
    }

    public override void Use() { }
}
