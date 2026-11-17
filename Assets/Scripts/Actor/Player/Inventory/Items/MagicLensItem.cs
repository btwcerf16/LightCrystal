using UnityEngine;

public sealed class MagicLensItem : Item
{
    [SerializeField] private float _projectileRange = 2f;
    [SerializeField] private float _cameraDistanceBonus = 0.1f;
    [SerializeField] private EffectData _focus;
    public override string DisplayName => "Магическая линза";
    public float ProjectileRangeBonus => _projectileRange;
    public float CameraDistanceBonus => _cameraDistanceBonus;
    public EffectData Focus => _focus;

    public override void Equip()
    {
        if (IsEquipped)
            return;
        base.Equip();
        if (IsServer)
        {
            _ownerStats.ProjectileRange.AddModifier(new StatModifier(_projectileRange, StatModifierType.Flat, this));
            _ownerStats.CameraDistance.AddModifier(new StatModifier(_cameraDistanceBonus, StatModifierType.AdditivePercent, this));
        }
    }

    public override void Drop()
    {
        if (IsServer)
        {
            _ownerStats.ProjectileRange.RemoveModifiersFrom(this);
            _ownerStats.CameraDistance.RemoveModifiersFrom(this);
        }
        base.Drop();
    }

    public override void Use()
    {
        if (IsServer)
            _ownerStats.GetComponent<EffectHandler>().AddEffect(_focus);
    }
}
