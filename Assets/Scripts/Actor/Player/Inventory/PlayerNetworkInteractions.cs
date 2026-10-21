using System;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine.Android;

public class PlayerNetworkInteractions : NetworkBehaviour
{
    private PlayerInputReader _inputReader;

    private ActorArsenal _actorArsenal;
    private Inventory _actorInventory;

    private Weapon _lastUsedWeapon;
    private EInputType _lastInputType;

    

    private bool _isSubscribed;

    private void Awake()
    {
        _inputReader = GetComponent<PlayerInputReader>();
        _actorArsenal = GetComponent<ActorArsenal>();
        _actorInventory = GetComponent<Inventory>();
    }
    public override void OnNetworkSpawn()
    {
        if (!IsOwner || _isSubscribed)
            return;
        base.OnNetworkSpawn();

        _inputReader.PrimaryPressed += PrimaryUseRpc;
        _inputReader.PrimaryReleased += PrimaryCancelRpc;

        _inputReader.SecondaryPressed += SecondaryUseRpc;
        _inputReader.SecondaryReleased += SecondaryCancelRpc;

        _inputReader.ActionsCanceled += CancelWeaponActivitiesRpc;

        _isSubscribed = true;
    }
    public override void OnNetworkDespawn()
    {
        if (!IsOwner || !_isSubscribed)
            return;
        base.OnNetworkDespawn();
        
        _inputReader.PrimaryPressed -= PrimaryUseRpc;
        _inputReader.PrimaryReleased -= PrimaryCancelRpc;

        _inputReader.SecondaryPressed -= SecondaryUseRpc;
        _inputReader.SecondaryReleased -= SecondaryCancelRpc;

        _inputReader.ActionsCanceled -= CancelWeaponActivitiesRpc;

        _isSubscribed = false;
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
        if(_lastUsedWeapon != null)
        {
            switch (_lastInputType) 
            {
                case EInputType.Primary:
                    _lastUsedWeapon.EndPrimaryUse();
                    _lastInputType = EInputType.None;
                    break;
                case EInputType.Secondary:
                    _lastUsedWeapon.EndSecondaryUse();
                    _lastInputType = EInputType.None;
                    break;
                case EInputType.None:
                    break;
            }
        }

    }

    private enum EInputType
    {
        None,
        Primary,
        Secondary
    }
}
