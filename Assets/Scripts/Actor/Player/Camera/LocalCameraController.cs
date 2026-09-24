using Cinemachine;
using UnityEngine;

public sealed class LocalCameraController : MonoBehaviour
{
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private CinemachineVirtualCamera _virtualCamera;

    public Camera WorldCamera => _worldCamera;

    public void Follow(Transform target)
    {
        _virtualCamera.Follow = target;
    }
}