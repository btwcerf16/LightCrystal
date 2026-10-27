using Cinemachine;
using UnityEngine;

public class LocalCameraController : MonoBehaviour
{
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private CinemachineVirtualCamera _virtualCamera;
    [SerializeField, Min(0f)] private float _damageTakenIntensity = 0.16f;
    [SerializeField, Min(0f)] private float _damageTakenDuration = 0.2f;
    [SerializeField, Min(0f)] private float _damageDealtIntensity = 0.06f;
    [SerializeField, Min(0f)] private float _damageDealtDuration = 0.12f;
    private CinemachineShake _shake;

    public Camera WorldCamera => _worldCamera;

    private void Awake()
    {
        _shake = _virtualCamera.GetComponent<CinemachineShake>();
        if (_shake == null)
            _shake = _virtualCamera.gameObject.AddComponent<CinemachineShake>();
    }

    public void Follow(Transform target)
    {
        _shake.StopShake();
        _virtualCamera.Follow = target;
    }

    public void ShakeOnDamage(bool receivedDamage)
    {
        if (receivedDamage)
            _shake.ShakeCamera(_damageTakenIntensity, _damageTakenDuration);
        else
            _shake.ShakeCamera(_damageDealtIntensity, _damageDealtDuration);
    }
}
