using UnityEngine;

public sealed class DwarfStabilizerItem : Item
{
    [SerializeField, Min(0f)] private float _projectileSpeedBonus = 0.2f;

    public override string DisplayName => "Дворфийский стабилизатор";
    public float ProjectileSpeedBonusPercent => _projectileSpeedBonus * 100f;

    public override void Equip()
    {
        if (IsEquipped)
            return;
        base.Equip();
        if (IsServer)
            _ownerStats.ProjectileSpeedMultiplier.AddModifier(new StatModifier(_projectileSpeedBonus, StatModifierType.AdditivePercent, this));
    }

    public override void Drop()
    {
        if (IsServer)
            _ownerStats.ProjectileSpeedMultiplier.RemoveModifiersFrom(this);
        base.Drop();
    }

    public override void Use() { }
}
