using UnityEngine;

public sealed class TrollSinewItem : Item
{
    [SerializeField] private float _cameraDistanceBonus = 0.2f;
    public override string DisplayName => "Тетива из жил тролля";
    public float CameraDistanceBonus => _cameraDistanceBonus;

    public override void Equip()
    {
        if (IsEquipped)
            return;
        base.Equip();
        if (!IsServer)
            return;
        _ownerStats.CameraDistance.AddModifier(new StatModifier(_cameraDistanceBonus, StatModifierType.AdditivePercent, this));
    }

    public override void Drop()
    {
        if (IsServer)
        {
            _ownerStats.CameraDistance.RemoveModifiersFrom(this);
        }
        base.Drop();
    }

    public override void Use() { }
}
