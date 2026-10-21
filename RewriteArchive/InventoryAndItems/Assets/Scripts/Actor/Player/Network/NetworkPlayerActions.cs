using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerInputReader), typeof(ActorArsenal))]
public sealed class NetworkPlayerActions : NetworkBehaviour
{
    private PlayerInputReader _input;
    private ActorArsenal _arsenal;
    private ActorHealth _health;
    private ActorHand _hand;
    private NetworkPlayerHand _networkHand;
    private Weapon _primaryActionWeapon;
    private Weapon _secondaryActionWeapon;
    private bool _inputSubscribed;
    private bool _serverSubscribed;

    private void Awake()
    {
        _input = GetComponent<PlayerInputReader>();
        _arsenal = GetComponent<ActorArsenal>();
        _health = GetComponent<ActorHealth>();
        _hand = GetComponentInChildren<ActorHand>();
        _networkHand = GetComponentInChildren<NetworkPlayerHand>();
    }

    public override void OnNetworkSpawn() => Bind();
    private void OnEnable() => Bind();
    public override void OnNetworkDespawn() => Unbind();

    private void OnDisable()
    {
        SendCancel();
        Unbind();
    }

    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        Unbind();
        Bind();
    }

    private void Bind()
    {
        if (!IsSpawned || !isActiveAndEnabled)
            return;

        if (IsServer && !_serverSubscribed)
        {
            _arsenal.ActiveWeaponChanged += OnActiveWeaponChanged;
            if (_health != null)
                _health.Died += CancelOnServer;
            _serverSubscribed = true;
        }
        if (!IsClient || !IsOwner || _inputSubscribed)
            return;

        _input.PrimaryPressed += OnPrimaryPressed;
        _input.PrimaryReleased += OnPrimaryReleased;
        _input.SecondaryPressed += OnSecondaryPressed;
        _input.SecondaryReleased += OnSecondaryReleased;
        _input.ActionsCanceled += SendCancel;
        _inputSubscribed = true;
    }

    private void Unbind()
    {
        if (_inputSubscribed)
        {
            _input.PrimaryPressed -= OnPrimaryPressed;
            _input.PrimaryReleased -= OnPrimaryReleased;
            _input.SecondaryPressed -= OnSecondaryPressed;
            _input.SecondaryReleased -= OnSecondaryReleased;
            _input.ActionsCanceled -= SendCancel;
            _inputSubscribed = false;
        }
        if (_serverSubscribed)
        {
            _arsenal.ActiveWeaponChanged -= OnActiveWeaponChanged;
            if (_health != null)
                _health.Died -= CancelOnServer;
            CancelOnServer();
            _serverSubscribed = false;
        }
    }

    private void OnPrimaryPressed() => SendUse(false, true);
    private void OnPrimaryReleased() => SendUse(false, false);
    private void OnSecondaryPressed() => SendUse(true, true);
    private void OnSecondaryReleased() => SendUse(true, false);

    private void SendUse(bool secondary, bool pressed)
    {
        if (IsSpawned && IsClient && IsOwner && isActiveAndEnabled)
            SubmitUseRpc(secondary, pressed, _networkHand.GetAimOffset());
    }

    private void SendCancel()
    {
        if (IsSpawned && IsClient && IsOwner)
            CancelUseRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SubmitUseRpc(bool secondary, bool pressed, Vector2 aim, RpcParams rpcParams = default)
    {
        if (!IsSpawned || !isActiveAndEnabled || rpcParams.Receive.SenderClientId != OwnerClientId)
            return;
        if (_health != null && _health.IsDead)
        {
            CancelOnServer();
            return;
        }

        Weapon actionWeapon = secondary ? _secondaryActionWeapon : _primaryActionWeapon;
        if (pressed)
        {
            if (actionWeapon != null)
                return;
            Weapon weapon = _arsenal.ActiveWeapon;
            if (weapon == null || !weapon.isActiveAndEnabled)
                return;

            weapon.SetAim(_hand.ClampOffset(aim));
            if (secondary)
            {
                _secondaryActionWeapon = weapon;
                weapon.BeginSecondaryUse();
            }
            else
            {
                _primaryActionWeapon = weapon;
                weapon.BeginPrimaryUse();
            }
        }
        else
        {
            if (secondary)
                _secondaryActionWeapon = null;
            else
                _primaryActionWeapon = null;

            if (actionWeapon == null)
                return;
            if (actionWeapon != _arsenal.ActiveWeapon || !actionWeapon.isActiveAndEnabled)
            {
                actionWeapon.CancelUse();
                return;
            }
            if (secondary)
                actionWeapon.EndSecondaryUse();
            else
                actionWeapon.EndPrimaryUse();
        }
    }


    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void CancelUseRpc(RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId == OwnerClientId)
            CancelOnServer();
    }

    private void OnActiveWeaponChanged(int index, Weapon weapon)
    {
        CancelOnServer();
    }

    private void CancelOnServer()
    {
        Weapon primary = _primaryActionWeapon;
        Weapon secondary = _secondaryActionWeapon;
        _primaryActionWeapon = null;
        _secondaryActionWeapon = null;

        _arsenal.ActiveWeapon?.CancelUse();
        if (primary != null && primary != _arsenal.ActiveWeapon)
            primary.CancelUse();
        if (secondary != null && secondary != primary)
            secondary.CancelUse();
    }
}
