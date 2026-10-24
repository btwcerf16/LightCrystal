using Unity.Netcode;
using UnityEngine;

public sealed class RustyHelmet : Item
{
    [SerializeField, Range(0f, 1f)] private float _physicalResistance = 0.1f;
    [SerializeField, Range(0f, 1f)] private float _resistancePerUse = 0.01f;

    private ActorStats _stats;
    private bool _isBonusApplied;
    private float _activeResistanceBonus;

    public override string DisplayName => "Ржавый шлем";
    public float PhysicalResistancePercent => _physicalResistance * 100f;
    public float ResistancePerUsePercent => _resistancePerUse * 100f;

    public override void Equip()
    {
        base.Equip();
        if (_isBonusApplied)
            return;

        _stats = GetComponentInParent<ActorStats>();
        NetworkObject player = GetComponentInParent<NetworkObject>();
        if (_stats == null || player == null || !player.IsSpawned || !player.NetworkManager.IsServer)
            return;

        _isBonusApplied = true;
        _stats.PhysicalResistance.AddModifier(new StatModifier(_physicalResistance + _activeResistanceBonus, StatModifierType.Flat, this));
    }

    public override void Drop()
    {
        base.Drop();
        if (!_isBonusApplied)
            return;

        _isBonusApplied = false;
        if (_stats != null)
            _stats.PhysicalResistance.RemoveModifiersFrom(this);
    }

    public override void Use()
    {
        if (!_isBonusApplied)
            return;

        _activeResistanceBonus += _resistancePerUse;
        _stats.PhysicalResistance.AddModifier(new StatModifier(_resistancePerUse, StatModifierType.Flat, this));
    }

    private void OnDestroy()
    {
        Drop();
    }
}
