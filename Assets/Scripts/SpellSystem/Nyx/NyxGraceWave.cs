using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public sealed class NyxGraceWave : NetworkBehaviour
{
    [SerializeField, Min(0.1f)] private float _radius = 1.65f;
    [SerializeField, Min(0.05f)] private float _duration = 0.25f;
    [SerializeField] private bool _fullCircle;
    [SerializeField] private Material _waveMaterial;
    private float _elapsed;
    private LineRenderer _wave;

    public float Radius => _radius;

    public override void OnNetworkSpawn()
    {
        if (!IsClient)
            return;

        _wave = gameObject.AddComponent<LineRenderer>();
        _wave.sharedMaterial = _waveMaterial;
        _wave.sortingOrder = 4;
        _wave.useWorldSpace = false;
        _wave.positionCount = _fullCircle ? 25 : 13;
        _wave.widthMultiplier = 0.38f;
        _wave.widthCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        DrawWave(0f);
    }

    private void Update()
    {
        if (!IsSpawned)
            return;
        _elapsed += Time.deltaTime;
        if (_wave != null)
            DrawWave(Mathf.Clamp01(_elapsed / _duration));
        if (IsServer && _elapsed >= _duration)
            NetworkObject.Despawn();
    }

    private void DrawWave(float progress)
    {
        const float sweepEndProgress = 0.75f;
        const float visualOffsetFromCaster = 0.1f;
        float visualCenterOffset = _fullCircle ? 0f : visualOffsetFromCaster - _radius;
        float sweepProgress = Mathf.Clamp01(progress / sweepEndProgress);
        float leadingAngle = Mathf.Lerp(-90f, _fullCircle ? 270f : 90f, sweepProgress);
        float opacity = 1f - Mathf.InverseLerp(sweepEndProgress, 1f, progress);
        _wave.startColor = _wave.endColor = new Color(0.72f, 0.91f, 1f, opacity);
        for (int i = 0; i < _wave.positionCount; i++)
        {
            float angle = Mathf.Lerp(-90f, leadingAngle, (float)i / (_wave.positionCount - 1)) * Mathf.Deg2Rad;
            _wave.SetPosition(i, new Vector3(visualCenterOffset + Mathf.Cos(angle) * _radius, Mathf.Sin(angle) * _radius));
        }
    }
}
