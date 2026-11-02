using UnityEngine;

public sealed class Carrot : Item
{
    [SerializeField] private float _viewBonus = 0.05f;
    public override string DisplayName => "Морковь";
    public float ViewBonusPercent => _viewBonus * 100f;

    public override void Use()
    {
        if (!IsServer || DestroyOnDrop)
            return;
        _ownerStats.ViewDistance.AddModifier(new StatModifier(_viewBonus, StatModifierType.AdditivePercent, _ownerStats));
        DestroyOnDrop = true;
    }
}
