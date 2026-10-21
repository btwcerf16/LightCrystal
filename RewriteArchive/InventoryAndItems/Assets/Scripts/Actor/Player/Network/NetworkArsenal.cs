using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(ActorArsenal))]
public sealed class NetworkArsenal : NetworkBehaviour
{
    private readonly NetworkVariable<int> _activeIndex = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private ActorArsenal _arsenal;

    private void Awake() => _arsenal = GetComponent<ActorArsenal>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _arsenal.ActiveWeaponChanged += Publish;
            _activeIndex.Value = _arsenal.ActiveWeaponIndex;
        }
        else
        {
            _activeIndex.OnValueChanged += Receive;
            _arsenal.TrySelectWeapon(_activeIndex.Value);
        }
        _arsenal.ActiveWeapon?.Equip();
        Inventory inventory = GetComponent<Inventory>();
        for (int i = 0; i < inventory.ItemsSize; i++)
            inventory.GetItem(i)?.Equip();
    }

    public override void OnNetworkDespawn()
    {
        _arsenal.ActiveWeaponChanged -= Publish;
        _activeIndex.OnValueChanged -= Receive;
        _arsenal.ActiveWeapon?.Drop();
        Inventory inventory = GetComponent<Inventory>();
        for (int i = 0; i < inventory.ItemsSize; i++)
            inventory.GetItem(i)?.Drop();
    }

    public void Select(Weapon weapon)
    {
        if (!IsSpawned || !IsOwner)
            return;
        for (int i = 0; i < _arsenal.WeaponSize; i++)
            if (_arsenal.GetWeapon(i) == weapon)
            {
                SelectRpc(i);
                return;
            }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SelectRpc(int index)
    {
        if (GetComponent<ActorHealth>().IsDead)
            return;
        _arsenal.TrySelectWeapon(index);
    }

    private void Publish(int index, Weapon weapon) => _activeIndex.Value = index;
    private void Receive(int previous, int current) => _arsenal.TrySelectWeapon(current);
}
