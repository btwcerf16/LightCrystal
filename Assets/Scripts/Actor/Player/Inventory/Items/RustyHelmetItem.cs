using Unity.Netcode;
using UnityEngine;

public sealed class RustyHelmetItem : Item
{
    [SerializeField, Range(0f, 1f)] private float _physicalResistance = 0.05f;
    [SerializeField, Range(0f, 1f)] private float _effectResistance = 0.10f;

    private ActorStats _stats;

    public override string DisplayName => "Ржавый шлем";
    public float PhysicalResistancePercent => _physicalResistance * 100f;
    public float EffectResistancePercent => _effectResistance * 100f;

    public override void Equip()
    {
        base.Equip();
        if (_bonusApplied)
            return;

        _stats = GetComponentInParent<ActorStats>();
        NetworkObject player = GetComponentInParent<NetworkObject>();
        if (_stats == null || player == null || !player.IsSpawned || !player.NetworkManager.IsServer)
            return;

        _bonusApplied = true;
        _stats.PhysicalResistance.AddModifier(new StatModifier(_physicalResistance, StatModifierType.Flat, this));
        _stats.EffectResistance.AddModifier(new StatModifier(_effectResistance, StatModifierType.Flat, this));
    }

    public override void Drop()
    {
        base.Drop();
        if (!_bonusApplied)
            return;

        _bonusApplied = false;
        if (_stats != null)
        {
            _stats.PhysicalResistance.RemoveModifiersFrom(this);
            _stats.EffectResistance.RemoveModifiersFrom(this);
        }
    }

    protected override bool CanUse() => false;
    public override void Use() { }

    private void OnDestroy()
    {
        Drop();
    }
}
