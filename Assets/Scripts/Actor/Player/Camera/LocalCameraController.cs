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
    private ActorStats _stats;
    private float _baseSize;

    public Camera WorldCamera => _worldCamera;

    private void Awake()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        BindCameras();
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) => BindCameras();

    private void BindCameras()
    {
        if (_virtualCamera == null)
            _virtualCamera = FindFirstObjectByType<CinemachineVirtualCamera>();
        if (_worldCamera == null)
            _worldCamera = Camera.main;
        if (_virtualCamera == null) return;
        _baseSize = _virtualCamera.m_Lens.OrthographicSize;
        _shake = _virtualCamera.GetComponent<CinemachineShake>();
        if (_shake == null)
            _shake = _virtualCamera.gameObject.AddComponent<CinemachineShake>();
    }

    public void Follow(Transform target)
    {
        if (_virtualCamera == null) BindCameras();
        _shake.StopShake();
        if (_stats != null)
            _stats.CameraDistance.ValueChanged -= UpdateDistance;
        _virtualCamera.Follow = target;
        _stats = target != null ? target.GetComponentInParent<ActorStats>() : null;
        if (_stats != null)
            _stats.CameraDistance.ValueChanged += UpdateDistance;
        UpdateDistance(_stats != null ? _stats.CameraDistance.Value : 1f);
    }

    private void UpdateDistance(float multiplier)
    {
        _virtualCamera.m_Lens.OrthographicSize = _baseSize * multiplier;
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_stats != null)
            _stats.CameraDistance.ValueChanged -= UpdateDistance;
    }

    public void ShakeWeaponImpact(bool heavy)
    {
        _shake.ShakeCamera(heavy ? 0.25f : 0.14f, heavy ? 0.22f : 0.16f);
    }

    public void ShakeOnDamage(bool receivedDamage)
    {
        if (receivedDamage)
            _shake.ShakeCamera(_damageTakenIntensity, _damageTakenDuration);
        else
            _shake.ShakeCamera(_damageDealtIntensity, _damageDealtDuration);
    }
}
