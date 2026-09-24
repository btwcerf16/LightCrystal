using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class ActorVision : MonoBehaviour
{
    [SerializeField] private Camera _worldCamera;
    [SerializeField, Range(0f, 1f)] private float _innerRadiusPercent = 0.8f;
    [SerializeField, Range(0f, 1f)] private float _innerAnglePercent = 0.85f;
    [SerializeField] private float _rotationOffset = -90f;

    private ActorStats _stats;
    private PlayerInputReader _inputReader;
    private Light2D _visionLight;

    private Vector2 _lastDirection = Vector2.right;

    private void Awake()
    {
        _stats = GetComponentInParent<ActorStats>();
        _inputReader = GetComponentInParent<PlayerInputReader>();
        _visionLight = GetComponent<Light2D>();

        if (_stats == null || _inputReader == null || _worldCamera == null)
        {
            Debug.LogError($"ActorVision on {name} is missing a required reference.", this);
            enabled = false;
            return;
        }

        _visionLight.lightType = Light2D.LightType.Point;
    }

    private void OnEnable()
    {
        if (_stats == null)
            return;

        _stats.ViewDistance.ValueChanged += SetViewDistance;
        _stats.ViewAngle.ValueChanged += SetViewAngle;

        SetViewDistance(_stats.ViewDistance.Value);
        SetViewAngle(_stats.ViewAngle.Value);
    }

    private void OnDisable()
    {
        if (_stats == null)
            return;

        _stats.ViewDistance.ValueChanged -= SetViewDistance;
        _stats.ViewAngle.ValueChanged -= SetViewAngle;
    }

    private void LateUpdate()
    {
        UpdateDirection();
    }

    private void UpdateDirection()
    {
        Vector3 screenPosition = _inputReader.AimScreenPosition;
        screenPosition.z = Mathf.Abs(_worldCamera.transform.position.z - transform.position.z);

        Vector2 worldPosition = _worldCamera.ScreenToWorldPoint(screenPosition);
        Vector2 direction = worldPosition - (Vector2)transform.position;

        if (direction.sqrMagnitude > 0.0001f)
            _lastDirection = direction.normalized;

        float angle = Mathf.Atan2(_lastDirection.y, _lastDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + _rotationOffset);
    }

    private void SetViewDistance(float distance)
    {
        _visionLight.pointLightOuterRadius = distance;
        _visionLight.pointLightInnerRadius = distance * _innerRadiusPercent;
    }

    private void SetViewAngle(float angle)
    {
        _visionLight.pointLightOuterAngle = angle;
        _visionLight.pointLightInnerAngle = angle * _innerAnglePercent;
    }
    public void SetCamera(Camera worldCamera)
    {
        _worldCamera = worldCamera;
    }
}