using Unity.Netcode;
using UnityEngine;

public sealed class NetworkPlayerSetup : NetworkBehaviour
{
    [SerializeField] private PlayerInputReader _inputReader;
    [SerializeField] private ActorVision _actorVision;
    [SerializeField] private GameObject _visionObject;
    [SerializeField] private Transform _cameraTarget;

    public override void OnNetworkSpawn()
    {
        _inputReader.enabled = false;
        _visionObject.SetActive(false);

        if (!IsOwner)
            return;

        LocalCameraController cameraController = NetworkManager.GetComponent<LocalCameraController>();

        cameraController.Follow(_cameraTarget);
        _actorVision.SetCamera(cameraController.WorldCamera);

        _inputReader.enabled = true;
        _visionObject.SetActive(true);
    }
}