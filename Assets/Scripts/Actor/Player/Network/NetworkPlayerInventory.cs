using System;
using Unity.Netcode;
using UnityEngine;
using static UnityEditor.Progress;

public class NetworkPlayerInventory: NetworkBehaviour
{
    private NetworkList<NetworkInventorySlot> _slots ;
    private Inventory _inventory;


    private void Awake()
    {
        _slots = new NetworkList<NetworkInventorySlot>(readPerm:NetworkVariableReadPermission.Everyone,
            writePerm:NetworkVariableWritePermission.Server);
        _inventory = GetComponent<Inventory>();
    }
    public override void OnNetworkSpawn()
    {
        _slots.OnListChanged += OnSlotsChanged;

        if (_inventory == null) return;

        if (!IsServer)
        {
            OnSlotsChanged(new NetworkListEvent<NetworkInventorySlot>());
            return;
        }

        for (int i = 0; i < _inventory.Size; i++)
        {
            _slots.Add(new NetworkInventorySlot { SlotIndex = i, IsConquered = false});
        }
        
        _inventory.ContentsChanged += PublishContents;
        PublishContents();

    }
    public override void OnNetworkDespawn()
    {
        _slots.OnListChanged -= OnSlotsChanged;

        if (!IsServer || _inventory == null) return;
        
        _inventory.ContentsChanged -= PublishContents;
    }

    private void OnSlotsChanged(NetworkListEvent<NetworkInventorySlot> change)
    {
        if (IsServer || _inventory == null)
            return;

        for (int i = 0; i < _slots.Count; i++)
        {
            NetworkInventorySlot slot = _slots[i];
            if (!slot.IsConquered)
            {
                _inventory.SetItem(slot.SlotIndex, 0);
                continue;
            }

            if (slot.Uid == 0)
            {
                Debug.LogError("Received an inventory item without a UID.", this);
                continue;
            }

            _inventory.SetItem(slot.SlotIndex, slot.Uid);
        }
    }

    private void PublishContents()
    {
        for (int i = 0; i < _inventory.Size; i++)
        {

            var slot = _slots[i];
            var item = _inventory.GetItem(i);
            bool isOccupied = item != null;
            int itemId = isOccupied ? item.ItemId : 0;
            ulong uid = isOccupied ? item.Uid : 0;

            if (slot.IsConquered != isOccupied || slot.ItemId != itemId || slot.Uid != uid)
            {
                slot.IsConquered = isOccupied;
                slot.ItemId = itemId;
                slot.Uid = uid;
                _slots[i] = slot;
            }

        }
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void ChangeInventorySlotsRpc(int previousIndex, int newIndex)
    {
        _inventory.ChangeItemSlot(previousIndex, newIndex);
    }
}
