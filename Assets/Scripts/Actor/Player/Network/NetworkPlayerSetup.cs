using Unity.Netcode;
using UnityEngine;

public sealed class NetworkPlayerSetup : NetworkBehaviour
{
    [SerializeField] private PlayerInputReader _inputReader;
    [SerializeField] private ActorVision _actorVision;
    [SerializeField] private GameObject _visionObject;
    [SerializeField] private Transform _cameraTarget;
    private ActorHealth _health;
    private LocalCameraController _cameraController;

    public override void OnNetworkSpawn()
    {
        _health = GetComponent<ActorHealth>();
        if (IsServer)
        {
            _health.DamageTaken += OnDamageTaken;
            _health.DamageDealt += OnDamageDealt;
        }
        _inputReader.enabled = false;
        _visionObject.SetActive(false);

        if (!IsOwner)
            return;

        _cameraController = NetworkManager.GetComponent<LocalCameraController>();

        _cameraController.Follow(_cameraTarget);
        _actorVision.SetCamera(_cameraController.WorldCamera);

        _inputReader.enabled = true;
        _visionObject.SetActive(true);
    }

    public override void OnNetworkDespawn()
    {
        _health.DamageTaken -= OnDamageTaken;
        _health.DamageDealt -= OnDamageDealt;
    }

    private void OnDamageTaken(float damage)
    {
        ShakeCameraRpc(true);
    }

    private void OnDamageDealt(float damage)
    {
        ShakeCameraRpc(false);
    }

    [Rpc(SendTo.Owner)]
    private void ShakeCameraRpc(bool receivedDamage)
    {
        if (_cameraController != null)
            _cameraController.ShakeOnDamage(receivedDamage);
    }
}
