using UnityEngine;

public sealed class SwiftBootsItem : Item
{
    [SerializeField] private float _moveSpeed = 0.3f;
    public override string DisplayName => "Сапоги-скороходы";
    public float MoveSpeedBonus => _moveSpeed;

    public override void Equip()
    {
        if (IsEquipped)
            return;
        base.Equip();
        if (!IsServer)
            return;
        _ownerStats.MoveSpeed.AddModifier(new StatModifier(_moveSpeed, StatModifierType.AdditivePercent, this));
    }

    public override void Drop()
    {
        if (IsServer)
        {
            _ownerStats.MoveSpeed.RemoveModifiersFrom(this);
        }
        base.Drop();
    }

    public override void Use() { }
}
