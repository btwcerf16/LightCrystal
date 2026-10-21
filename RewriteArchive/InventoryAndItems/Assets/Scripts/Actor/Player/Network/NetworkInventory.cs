using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Inventory), typeof(ActorArsenal))]
public sealed class NetworkInventory : NetworkBehaviour
{
    private NetworkList<NetworkItemSlot> _items;
    private NetworkList<NetworkItemSlot> _weapons;
    private Inventory _inventory;
    private ActorArsenal _arsenal;
    private ActorHand _hand;
    private PlayerInputReader _input;
    private ActorHealth _health;
    private bool _subscribed;
    private bool _pendingReferences;

    private void Awake()
    {
        _items = new NetworkList<NetworkItemSlot>();
        _weapons = new NetworkList<NetworkItemSlot>();
        _inventory = GetComponent<Inventory>();
        _arsenal = GetComponent<ActorArsenal>();
        _hand = GetComponentInChildren<ActorHand>();
        _input = GetComponent<PlayerInputReader>();
        _health = GetComponent<ActorHealth>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _items.Clear();
            _weapons.Clear();
            for (int i = 0; i < _inventory.ItemsSize; i++)
                _items.Add(default);
            for (int i = 0; i < _arsenal.WeaponSize; i++)
                _weapons.Add(default);
        }
        _items.OnListChanged += OnContentsChanged;
        _weapons.OnListChanged += OnContentsChanged;
        ApplyContents();
        BindInput();
    }

    public override void OnGainedOwnership() => BindInput();
    public override void OnLostOwnership() => UnbindInput();

    public override void OnNetworkDespawn()
    {
        UnbindInput();
        _items.OnListChanged -= OnContentsChanged;
        _weapons.OnListChanged -= OnContentsChanged;
        for (int i = 0; i < _inventory.ItemsSize; i++)
        {
            Item item = _inventory.GetItem(i);
            _inventory.SetItem(i, null);
            Release(item);
        }
        for (int i = 0; i < _arsenal.WeaponSize; i++)
        {
            Item item = _arsenal.GetWeapon(i);
            _arsenal.SetWeapon(i, null);
            Release(item);
        }
    }

    private void BindInput()
    {
        if (!IsSpawned || !IsOwner || !IsClient || _subscribed)
            return;
        _input.InteractPressed += RequestPickup;
        _input.DropWeaponPressed += RequestDropActiveWeapon;
        _subscribed = true;
    }

    private void UnbindInput()
    {
        if (!_subscribed)
            return;
        _input.InteractPressed -= RequestPickup;
        _input.DropWeaponPressed -= RequestDropActiveWeapon;
        _subscribed = false;
    }

    private void Update()
    {
        if (IsSpawned && _pendingReferences)
            ApplyContents();
    }

    private void RequestPickup()
    {
        if (_hand.TryGetPickupItem(out WorldItem item))
            PickupRpc(new NetworkObjectReference(item.NetworkObject));
    }

    private void RequestDropActiveWeapon()
    {
        DropActiveWeaponRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void PickupRpc(NetworkObjectReference reference)
    {
        if (_health.IsDead || !reference.TryGet(out NetworkObject networkObject, NetworkManager))
            return;
        WorldItem worldItem = networkObject.GetComponent<WorldItem>();
        if (worldItem == null || !worldItem.IsTouchedBy(_hand))
            return;
        NetworkList<NetworkItemSlot> slots = worldItem.Item is Weapon ? _weapons : _items;
        int empty = -1;
        for (int i = 0; i < slots.Count; i++)
            if (!slots[i].IsOccupied) { empty = i; break; }
        if (empty < 0 || !worldItem.TryClaim())
            return;
        slots[empty] = new NetworkItemSlot(worldItem.NetworkObjectId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void DropActiveWeaponRpc()
    {
        if (_health.IsDead)
            return;

        int index = _arsenal.ActiveWeaponIndex;
        Weapon weapon = _arsenal.GetWeapon(index);

        if (weapon == null)
            return;

        WorldItem worldItem = weapon.GetComponentInParent<WorldItem>();

        if (worldItem == null)
            return;

        _arsenal.SetWeapon(index, null);
        worldItem.Release(_hand.Position);
        _weapons[index] = default;
    }

    private void OnContentsChanged(NetworkListEvent<NetworkItemSlot> change) => ApplyContents();

    private void ApplyContents()
    {
        _pendingReferences = false;
        for (int i = 0; i < _items.Count && i < _inventory.ItemsSize; i++)
            if (TryResolveItem(_items[i], out Item item))
            {
                Item previousItem = _inventory.GetItem(i);
                if (item != null)
                    item.SetOwner(gameObject);
                else
                    previousItem?.SetOwner(null);
                _inventory.SetItem(i, item);
            }
        for (int i = 0; i < _weapons.Count && i < _arsenal.WeaponSize; i++)
            if (TryResolveItem(_weapons[i], out Item item))
            {
                Weapon previousWeapon = _arsenal.GetWeapon(i);
                if (item != null)
                    item.SetOwner(gameObject);
                else
                    previousWeapon?.SetOwner(null);
                _arsenal.SetWeapon(i, item as Weapon);
            }
    }

    private bool TryResolveItem(NetworkItemSlot slot, out Item item)
    {
        item = null;
        if (!slot.IsOccupied)
            return true;
        ulong id = slot.NetworkObjectId;
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject networkObject))
        {
            _pendingReferences = true;
            return false;
        }
        item = networkObject.GetComponentInChildren<Item>(true);
        return true;
    }

    private void Release(Item item)
    {
        if (item == null || item.Owner != gameObject)
            return;
        if (IsServer && NetworkManager.IsListening)
            item.GetComponentInParent<WorldItem>().Release(transform.position);
        else
            item.SetOwner(null);
    }

    private struct NetworkItemSlot : INetworkSerializable, IEquatable<NetworkItemSlot>
    {
        public bool IsOccupied;
        public ulong NetworkObjectId;

        public NetworkItemSlot(ulong networkObjectId)
        {
            IsOccupied = true;
            NetworkObjectId = networkObjectId;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref IsOccupied);
            serializer.SerializeValue(ref NetworkObjectId);
        }

        public bool Equals(NetworkItemSlot other)
        {
            return IsOccupied == other.IsOccupied && NetworkObjectId == other.NetworkObjectId;
        }

        public override bool Equals(object obj)
        {
            return obj is NetworkItemSlot other && Equals(other);
        }

        public override int GetHashCode()
        {
            return NetworkObjectId.GetHashCode() ^ IsOccupied.GetHashCode();
        }
    }
}
