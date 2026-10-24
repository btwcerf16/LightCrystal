using System;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkInteractions : NetworkBehaviour
{
    private PlayerInputReader _inputReader;

    private ActorArsenal _actorArsenal;
    private Inventory _actorInventory;
    private ActorHand _hand;
    private ItemFactory _itemFactory;

    private Weapon _lastUsedWeapon;
    private int _lastUsedWeaponIndex;
    private EInputType _lastInputType;

    

    private bool _isSubscribed;

    private void Awake()
    {
        _inputReader = GetComponent<PlayerInputReader>();
        _actorArsenal = GetComponent<ActorArsenal>();
        _actorInventory = GetComponent<Inventory>();
        _hand = GetComponentInChildren<ActorHand>();
    }
    public override void OnNetworkSpawn()
    {
        _itemFactory = NetworkManager.GetComponent<ItemFactory>();
        if (IsServer || IsOwner)
        {
            _actorArsenal.WeaponDropRequested += OnWeaponDropRequested;
            _actorInventory.ItemDropRequested += OnItemDropRequested;
        }
        if (!IsOwner || _isSubscribed)
            return;
        base.OnNetworkSpawn();

        _inputReader.PrimaryPressed += PrimaryUseRpc;
        _inputReader.PrimaryReleased += PrimaryCancelRpc;

        _inputReader.SecondaryPressed += SecondaryUseRpc;
        _inputReader.SecondaryReleased += SecondaryCancelRpc;

        _inputReader.ActionsCanceled += CancelWeaponActivitiesRpc;
        _inputReader.DropWeaponPressed += OnDropWeaponPressed;
        _inputReader.InteractPressed += OnInteractPressed;

        _isSubscribed = true;
    }
    public override void OnNetworkDespawn()
    {
        _actorArsenal.WeaponDropRequested -= OnWeaponDropRequested;
        _actorInventory.ItemDropRequested -= OnItemDropRequested;
        if (!IsOwner || !_isSubscribed)
            return;
        base.OnNetworkDespawn();
        
        _inputReader.PrimaryPressed -= PrimaryUseRpc;
        _inputReader.PrimaryReleased -= PrimaryCancelRpc;

        _inputReader.SecondaryPressed -= SecondaryUseRpc;
        _inputReader.SecondaryReleased -= SecondaryCancelRpc;

        _inputReader.ActionsCanceled -= CancelWeaponActivitiesRpc;
        _inputReader.DropWeaponPressed -= OnDropWeaponPressed;
        _inputReader.InteractPressed -= OnInteractPressed;

        _isSubscribed = false;
    }

    private void OnDropWeaponPressed()
    {
        _actorArsenal.RequestDropWeapon(_actorArsenal.ActiveWeaponIndex);
    }

    private void OnWeaponDropRequested(int index)
    {
        Weapon weapon = _actorArsenal.GetWeapon(index);
        if (weapon != null)
        {
            if (IsServer)
                DropItemOnServer(weapon.Uid, true);
            else
                DropItemRpc(weapon.Uid, true);
        }
    }

    private void OnItemDropRequested(int index)
    {
        Item item = _actorInventory.GetItem(index);
        if (item != null)
        {
            if (IsServer)
                DropItemOnServer(item.Uid, false);
            else
                DropItemRpc(item.Uid, false);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void DropItemRpc(ulong uid, bool isWeapon)
    {
        DropItemOnServer(uid, isWeapon);
    }

    private void DropItemOnServer(ulong uid, bool isWeapon)
    {
        if (uid == 0)
            return;

        int size = isWeapon ? _actorArsenal.WeaponSize : _actorInventory.Size;
        for (int i = 0; i < size; i++)
        {
            Item item = isWeapon ? _actorArsenal.GetWeapon(i) : _actorInventory.GetItem(i);
            if (item == null || item.Uid != uid)
                continue;

            WorldItem worldItem = item.WorldItem != null ? item.WorldItem.GetComponent<WorldItem>() : null;
            worldItem = _hand.DropItem(worldItem, item.gameObject);
            if (worldItem == null)
                return;

            item.SetWorldItem(worldItem.gameObject);
            if (item == _lastUsedWeapon)
                CancelWeaponActivities();
            if (isWeapon)
                _actorArsenal.DropWeapon(i);
            else
                _actorInventory.DropItem(i);
            item.gameObject.SetActive(false);
            return;
        }
    }

    private void OnInteractPressed()
    {
        foreach (Collider2D target in Physics2D.OverlapPointAll(_hand.Position))
        {
            WorldItem worldItem = target.GetComponentInParent<WorldItem>();
            if (worldItem != null && worldItem.CanBePicked)
            {
                PickupRpc(worldItem.NetworkObjectId);
                return;
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void PickupRpc(ulong worldItemId)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(worldItemId, out NetworkObject networkObject))
            return;

        WorldItem worldItem = networkObject.GetComponent<WorldItem>();
        if (worldItem == null || !worldItem.IsUnderHand(_hand.Position))
            return;

        Item prefab = _itemFactory.GetPrefab(worldItem.ItemId);
        if (prefab == null)
            return;
        bool isWeapon = prefab is Weapon;
        int index = isWeapon ? _actorArsenal.FindEmptySlot() : _actorInventory.FindEmptySlot();
        if (index < 0)
            return;

        Transform parent = isWeapon ? _hand.HandTransform : transform;
        Item item = _itemFactory.GetOrCreateItem(worldItem.ItemId, worldItem.Uid, parent);
        if (item == null)
            return;

        worldItem.HideWorldItem();
        item.SetWorldItem(worldItem.gameObject);
        if (isWeapon)
            _actorArsenal.SetWeapon(index, (Weapon)item);
        else
            _actorInventory.EquipItem(item);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void PrimaryUseRpc()
    {
        if (_lastInputType != EInputType.None)
            return;
        _lastUsedWeapon = _actorArsenal.ActiveWeapon;

        if (_actorArsenal.CanUseWeapons && _lastUsedWeapon != null && _lastUsedWeapon.IsEquipped)
        {
            _lastUsedWeapon.BeginPrimaryUse();
            _lastInputType = EInputType.Primary;
            _lastUsedWeaponIndex = _actorArsenal.ActiveWeaponIndex;
            PlayWeaponActionRpc(_lastUsedWeaponIndex, EInputType.Primary);
        }
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void PrimaryCancelRpc()
    {   
        if(_lastInputType == EInputType.Primary)
        {
            _lastInputType = EInputType.None;
            _lastUsedWeapon.EndPrimaryUse();
        }
        
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SecondaryUseRpc()
    {
        if(_lastInputType != EInputType.None)
            return;

        _lastUsedWeapon = _actorArsenal.ActiveWeapon;
        if (_actorArsenal.CanUseWeapons && _lastUsedWeapon != null && _lastUsedWeapon.IsEquipped)
        {
            _lastInputType = EInputType.Secondary;
            _lastUsedWeapon.BeginSecondaryUse();
            _lastUsedWeaponIndex = _actorArsenal.ActiveWeaponIndex;
            PlayWeaponActionRpc(_lastUsedWeaponIndex, EInputType.Secondary);
        }
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SecondaryCancelRpc()
    {
        if(_lastInputType == EInputType.Secondary) 
        { 
            _lastInputType = EInputType.None; 
            _lastUsedWeapon.EndSecondaryUse(); 
        }
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void CancelWeaponActivitiesRpc()
    {
        CancelWeaponActivities();
    }

    private void CancelWeaponActivities()
    {
        if(_lastUsedWeapon != null)
        {
            _lastUsedWeapon.CancelUse();
            PlayWeaponActionRpc(_lastUsedWeaponIndex, EInputType.None);
        }
        _lastInputType = EInputType.None;
    }

    [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
    private void PlayWeaponActionRpc(int weaponIndex, EInputType inputType)
    {
        Weapon weapon = _actorArsenal.GetWeapon(weaponIndex);
        if (weapon == null)
            return;

        switch (inputType)
        {
            case EInputType.Primary:
                weapon.BeginPrimaryUse();
                break;
            case EInputType.Secondary:
                weapon.BeginSecondaryUse();
                break;
            case EInputType.None:
                weapon.CancelUse();
                break;
        }
    }

    private enum EInputType
    {
        None,
        Primary,
        Secondary
    }
}
