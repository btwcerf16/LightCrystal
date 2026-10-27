using Cinemachine;
using UnityEngine;

public sealed class CinemachineShake : CinemachineExtension
{
    private float _intensity;
    private float _duration;
    private float _endsAt;

    public void ShakeCamera(float intensity, float duration)
    {
        if (intensity <= 0f || duration <= 0f)
            return;
        float remaining = Mathf.Max(0f, _endsAt - Time.unscaledTime);
        float currentIntensity = _duration > 0f ? _intensity * remaining / _duration : 0f;
        _intensity = Mathf.Max(currentIntensity, intensity);
        _duration = Mathf.Max(remaining, duration);
        _endsAt = Time.unscaledTime + _duration;
    }

    public void StopShake()
    {
        _intensity = 0f;
        _endsAt = 0f;
    }

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Noise || _duration <= 0f)
            return;
        float remaining = _endsAt - Time.unscaledTime;
        if (remaining <= 0f)
            return;

        float intensity = _intensity * remaining / _duration;
        float time = Time.unscaledTime * 35f;
        Vector2 offset = new Vector2(Mathf.PerlinNoise(time, 0f), Mathf.PerlinNoise(0f, time));
        offset = (offset - Vector2.one * 0.5f) * 2f * intensity;
        state.PositionCorrection += new Vector3(offset.x, offset.y, 0f);
    }

    private void OnDisable()
    {
        StopShake();
    }
}
