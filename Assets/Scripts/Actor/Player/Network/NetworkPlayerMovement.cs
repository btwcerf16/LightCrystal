using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerInputReader), typeof(PlayableActorMovement))]
public sealed class NetworkPlayerMovement : NetworkBehaviour
{
    private PlayerInputReader _inputReader;
    private PlayableActorMovement _movement;
    private bool _isSubscribed;

    private void Awake()
    {
        _inputReader = GetComponent<PlayerInputReader>();
        _movement = GetComponent<PlayableActorMovement>();
    }

    public override void OnNetworkSpawn()
    {
        _movement.enabled = IsServer;

        if (!IsOwner)
            return;

        _inputReader.MoveChanged += SendMovementInput;
        _isSubscribed = true;

        SendMovementInput(_inputReader.MoveDirection);
    }

    public override void OnNetworkDespawn()
    {
        if (_isSubscribed)
        {
            _inputReader.MoveChanged -= SendMovementInput;
            _isSubscribed = false;
        }

        if (IsServer)
            _movement.Stop();
    }

    private void SendMovementInput(Vector2 direction)
    {
        SubmitMovementInputRpc(direction);
    }

    [Rpc(SendTo.Server)]
    private void SubmitMovementInputRpc(Vector2 direction)
    {
        _movement.Move(Vector2.ClampMagnitude(direction, 1f));
    }
}