using UnityEngine;

public sealed class CheatSheetItem : Item
{
    [SerializeField] private float _castSpeed = 0.2f;
    public override string DisplayName => "Шпаргалка";
    public float CastSpeedBonus => _castSpeed;

    public override void Equip()
    {
        if (IsEquipped)
            return;
        base.Equip();
        if (!IsServer)
            return;
        _ownerStats.CastSpeed.AddModifier(new StatModifier(_castSpeed, StatModifierType.AdditivePercent, this));
    }

    public override void Drop()
    {
        if (IsServer)
        {
            _ownerStats.CastSpeed.RemoveModifiersFrom(this);
        }
        base.Drop();
    }

    public override void Use() { }
}
