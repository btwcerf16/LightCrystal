using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerInventory: NetworkBehaviour
{
    private NetworkList<NetworkInventorySlot> _slots ;
    private Inventory _inventory;
    private ItemFactory _itemFactory;


    private void Awake()
    {
        _slots = new NetworkList<NetworkInventorySlot>(readPerm:NetworkVariableReadPermission.Everyone,
            writePerm:NetworkVariableWritePermission.Server);
        _inventory = GetComponent<Inventory>();
    }
    public override void OnNetworkSpawn()
    {
        _itemFactory = NetworkManager.GetComponent<ItemFactory>();
        _slots.OnListChanged += OnSlotsChanged;

        if (IsOwner || IsServer)
        {
            _inventory.SlotChangeRequested += OnSlotChangeRequested;
            _inventory.ItemUseRequested += OnItemUseRequested;
        }

        if (_inventory == null) return;

        if (!IsServer)
        {
            OnSlotsChanged(new NetworkListEvent<NetworkInventorySlot>());
            return;
        }

        _slots.Clear();
        for (int i = 0; i < _inventory.Size; i++)
        {
            _itemFactory.AssignUid(_inventory.GetItem(i));
            _slots.Add(new NetworkInventorySlot { SlotIndex = i, IsConquered = false});
        }
        
        _inventory.ContentsChanged += PublishContents;
        PublishContents();

    }
    public override void OnNetworkDespawn()
    {
        _slots.OnListChanged -= OnSlotsChanged;
        _inventory.SlotChangeRequested -= OnSlotChangeRequested;
        _inventory.ItemUseRequested -= OnItemUseRequested;

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

            Item item = _itemFactory.GetOrCreateItem(slot.ItemId, slot.Uid, transform);
            if (item != null)
            {
                item.SetCooldown(slot.CooldownDuration, slot.CooldownEndsAt);
                _inventory.SetItem(slot.SlotIndex, slot.Uid);
            }
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
            float cooldownDuration = isOccupied ? item.CooldownDuration : 0f;
            double cooldownEndsAt = isOccupied ? item.CooldownEndsAt : 0d;

            if (slot.IsConquered != isOccupied || slot.ItemId != itemId || slot.Uid != uid
                || slot.CooldownDuration != cooldownDuration || slot.CooldownEndsAt != cooldownEndsAt)
            {
                slot.IsConquered = isOccupied;
                slot.ItemId = itemId;
                slot.Uid = uid;
                slot.CooldownDuration = cooldownDuration;
                slot.CooldownEndsAt = cooldownEndsAt;
                _slots[i] = slot;
            }

        }
    }
    private void OnSlotChangeRequested(int previousIndex, int newIndex)
    {
        if (IsServer)
            _inventory.ChangeItemSlot(previousIndex, newIndex);
        else
            ChangeInventorySlotsRpc(previousIndex, newIndex);
    }

    private void OnItemUseRequested(int index)
    {
        if (IsServer)
            _inventory.UseItem(index);
        else
            UseItemRpc(index);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void UseItemRpc(int index)
    {
        _inventory.UseItem(index);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void ChangeInventorySlotsRpc(int previousIndex, int newIndex)
    {
        _inventory.ChangeItemSlot(previousIndex, newIndex);
    }
}
