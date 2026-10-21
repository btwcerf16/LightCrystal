using Unity.Netcode;
using UnityEngine;

public sealed class RustyHelmet : Item
{
    private ActorStats _stats;
    private NetworkObject _ownerNetwork;
    private bool _isBonusApplied;
    public override string DisplayName => "Ржавый шлем";

    public override void SetOwner(GameObject owner)
    {
        base.SetOwner(owner);
        _stats = owner != null ? owner.GetComponent<ActorStats>() : null;
        _ownerNetwork = owner != null ? owner.GetComponent<NetworkObject>() : null;
    }

    public override void Equip()
    {
        base.Equip();
        if (_isBonusApplied || _ownerNetwork == null || !_ownerNetwork.IsSpawned || !_ownerNetwork.NetworkManager.IsServer)
            return;
        _stats.PhysicalResistance.AddModifier(new StatModifier(0.1f, StatModifierType.Flat, this));
        _isBonusApplied = true;
    }

    public override void Drop()
    {
        base.Drop();
        if (!_isBonusApplied)
            return;
        _isBonusApplied = false;
        _stats.PhysicalResistance.RemoveModifiersFrom(this);
    }

    public override void Use() { }
    private void OnDisable() => Drop();
}
