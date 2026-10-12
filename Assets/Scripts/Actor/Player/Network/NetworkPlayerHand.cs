using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(ActorHand))]
public sealed class NetworkPlayerHand : NetworkBehaviour
{
    private PlayerInputReader _inputReader;
    private ActorHand _actorHand;
    private Camera _camera;
    private bool _isSubscribed;

    private void Awake()
    {
        _inputReader = GetComponentInParent<PlayerInputReader>();
        _actorHand = GetComponent<ActorHand>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        LocalCameraController cameraController = NetworkManager.GetComponent<LocalCameraController>();
        _camera = cameraController.WorldCamera;

        _inputReader.AimChanged += SetHandOffset;
        _isSubscribed = true;

        SetHandOffset(_inputReader.AimScreenPosition);
    }

    public override void OnNetworkDespawn()
    {
        if (!_isSubscribed)
            return;

        _inputReader.AimChanged -= SetHandOffset;
        _isSubscribed = false;
    }

    private void SetHandOffset(Vector2 screenPosition)
    {
        Vector2 cursorWorldPosition = _camera.ScreenToWorldPoint(screenPosition);
        Vector2 offset = cursorWorldPosition - (Vector2)transform.position;

        _actorHand.SetOffset(offset);
    }
}
