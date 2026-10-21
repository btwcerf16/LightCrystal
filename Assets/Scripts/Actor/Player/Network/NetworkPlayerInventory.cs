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

        if (!IsServer || _inventory == null) return;

        for (int i = 0; i < _inventory.Size; i++)
        {
            _slots.Add(new NetworkInventorySlot { SlotIndex = i, IsConquered = false});
        }
        
        _inventory.ContentsChanged += PublishContents;

    }
    public override void OnNetworkDespawn()
    {
        _slots.OnListChanged -= OnSlotsChanged;

        if (!IsServer || _inventory == null) return;
        
        _inventory.ContentsChanged -= PublishContents;
    }

    private void OnSlotsChanged(NetworkListEvent<NetworkInventorySlot> change)
    {
        
    }

    private void PublishContents()
    {
        for (int i = 0; i < _inventory.Size; i++)
        {

            var slot = _slots[i];
            var item = _inventory.GetItem(i);
            bool isOccupied = item != null;
            int itemId = isOccupied ? item.ItemId : 0;

            if (slot.IsConquered != isOccupied || slot.ItemId != itemId)
            {
                slot.IsConquered = isOccupied;
                slot.ItemId = itemId;
                _slots[i] = slot;
            }

        }
    }
}
