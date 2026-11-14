using UnityEngine;

public sealed class PiercingStoneItem : Item
{
    [SerializeField] private float _projectilePiercing = 3f;
    [SerializeField] private float _projectileSpeedMultiplier = 0.2f;
    public override string DisplayName => "Магический камень пронзания";
    public float ProjectilePiercingBonus => _projectilePiercing;
    public float ProjectileSpeedMultiplierBonus => _projectileSpeedMultiplier;

    public override void Equip()
    {
        if (IsEquipped)
            return;
        base.Equip();
        if (!IsServer)
            return;
        _ownerStats.ProjectilePiercing.AddModifier(new StatModifier(_projectilePiercing, StatModifierType.Flat, this));
        _ownerStats.ProjectileSpeedMultiplier.AddModifier(new StatModifier(_projectileSpeedMultiplier, StatModifierType.AdditivePercent, this));
    }

    public override void Drop()
    {
        if (IsServer)
        {
            _ownerStats.ProjectilePiercing.RemoveModifiersFrom(this);
            _ownerStats.ProjectileSpeedMultiplier.RemoveModifiersFrom(this);
        }
        base.Drop();
    }

    public override void Use() { }
}
