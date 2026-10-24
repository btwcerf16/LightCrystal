using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(ActorArsenal))]
public sealed class NetworkPlayerArsenal : NetworkBehaviour
{
    private NetworkList<NetworkInventorySlot> _slots;
    private readonly NetworkVariable<int> _activeWeaponIndex = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private ActorArsenal _arsenal;
    private ActorHand _hand;
    private ItemFactory _itemFactory;

    private void Awake()
    {
        _arsenal = GetComponent<ActorArsenal>();
        _hand = GetComponentInChildren<ActorHand>();
        _slots = new NetworkList<NetworkInventorySlot>(readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server);
    }

    public override void OnNetworkSpawn()
    {
        _itemFactory = NetworkManager.GetComponent<ItemFactory>();
        _slots.OnListChanged += OnSlotsChanged;
        _activeWeaponIndex.OnValueChanged += OnActiveWeaponChanged;

        if (IsServer || IsOwner)
        {
            _arsenal.SlotChangeRequested += OnSlotChangeRequested;
            _arsenal.WeaponSelectionRequested += OnWeaponSelectionRequested;
        }

        if (IsServer)
        {
            _slots.Clear();
            for (int i = 0; i < _arsenal.WeaponSize; i++)
            {
                _itemFactory.AssignUid(_arsenal.GetWeapon(i));
                _slots.Add(new NetworkInventorySlot { SlotIndex = i });
            }

            _arsenal.ContentsChanged += PublishContents;
            _arsenal.ActiveWeaponChanged += PublishActiveWeapon;
            PublishContents();
            PublishActiveWeapon(_arsenal.ActiveWeaponIndex, _arsenal.ActiveWeapon);
        }
        else
        {
            OnSlotsChanged(new NetworkListEvent<NetworkInventorySlot>());
            _arsenal.TrySelectWeapon(_activeWeaponIndex.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        _slots.OnListChanged -= OnSlotsChanged;
        _activeWeaponIndex.OnValueChanged -= OnActiveWeaponChanged;
        _arsenal.SlotChangeRequested -= OnSlotChangeRequested;
        _arsenal.WeaponSelectionRequested -= OnWeaponSelectionRequested;
        _arsenal.ContentsChanged -= PublishContents;
        _arsenal.ActiveWeaponChanged -= PublishActiveWeapon;
    }

    private void PublishContents()
    {
        for (int i = 0; i < _arsenal.WeaponSize; i++)
        {
            var slot = _slots[i];
            var weapon = _arsenal.GetWeapon(i);
            bool isOccupied = weapon != null;
            int itemId = isOccupied ? weapon.ItemId : 0;
            ulong uid = isOccupied ? weapon.Uid : 0;
            float cooldownDuration = isOccupied ? weapon.CooldownDuration : 0f;
            double cooldownEndsAt = isOccupied ? weapon.CooldownEndsAt : 0d;

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

    private void OnSlotsChanged(NetworkListEvent<NetworkInventorySlot> change)
    {
        if (IsServer)
            return;

        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (!slot.IsConquered)
            {
                _arsenal.SetWeapon(slot.SlotIndex, 0UL);
                continue;
            }

            if (slot.Uid == 0)
            {
                Debug.LogError("Received an arsenal weapon without a UID.", this);
                continue;
            }

            Item item = _itemFactory.GetOrCreateItem(slot.ItemId, slot.Uid, _hand.HandTransform);
            if (item is Weapon)
            {
                item.SetCooldown(slot.CooldownDuration, slot.CooldownEndsAt);
                _arsenal.SetWeapon(slot.SlotIndex, slot.Uid);
            }
        }
    }

    private void PublishActiveWeapon(int index, Weapon weapon)
    {
        _activeWeaponIndex.Value = index;
    }

    private void OnActiveWeaponChanged(int previousIndex, int newIndex)
    {
        if (!IsServer)
            _arsenal.TrySelectWeapon(newIndex);
    }

    private void OnSlotChangeRequested(int previousIndex, int newIndex)
    {
        if (IsServer)
            _arsenal.ChangeWeaponSlot(previousIndex, newIndex);
        else
            ChangeWeaponSlotsRpc(previousIndex, newIndex);
    }

    private void OnWeaponSelectionRequested(int index)
    {
        if (IsServer)
            _arsenal.TrySelectWeapon(index);
        else
            SelectWeaponRpc(index);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void ChangeWeaponSlotsRpc(int previousIndex, int newIndex)
    {
        _arsenal.ChangeWeaponSlot(previousIndex, newIndex);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SelectWeaponRpc(int index)
    {
        _arsenal.TrySelectWeapon(index);
    }
}
