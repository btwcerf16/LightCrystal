using UnityEngine;

public sealed class TrollSinewItem : Item
{
    [SerializeField] private float _projectileSpeed = 10f;
    public override string DisplayName => "Тетива из жил тролля";
    public float ProjectileSpeedBonus => _projectileSpeed;

    public override void Equip()
    {
        if (IsEquipped)
            return;
        base.Equip();
        if (!IsServer)
            return;
        _ownerStats.ProjectileSpeed.AddModifier(new StatModifier(_projectileSpeed, StatModifierType.Flat, this));
    }

    public override void Drop()
    {
        if (IsServer)
        {
            _ownerStats.ProjectileSpeed.RemoveModifiersFrom(this);
        }
        base.Drop();
    }

    public override void Use() { }
}
