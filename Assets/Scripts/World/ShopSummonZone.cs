using UnityEngine;

public sealed class ShopSummonZone : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float _radius = 4f;
    [SerializeField, Min(0.1f)] private float _fillDuration = 20f;
    [SerializeField, Min(0.1f)] private float _duration = 30f;
    [SerializeField] private Renderer _visual;
    private MaterialPropertyBlock _properties;
    private float _elapsed;
    private float _filledTime;

    public float Radius => _radius;
    public float Capacity { get; private set; }
    public bool IsComplete { get; private set; }

    private void Awake()
    {
        _properties = new MaterialPropertyBlock();
        ResizeVisual();
    }

    private void OnValidate()
    {
        ResizeVisual();
    }

    private void ResizeVisual()
    {
        if (_visual != null)
            _visual.transform.localScale = new Vector3(_radius * 2f, _radius * 2f, 1f);
    }

    public void Process(float deltaTime, bool occupied)
    {
        if (IsComplete || deltaTime <= 0f)
            return;
        deltaTime = Mathf.Min(deltaTime, Mathf.Max(0f, _duration - _elapsed));
        _elapsed += deltaTime;
        if (occupied)
            _filledTime += deltaTime;
        Capacity = Mathf.Clamp01(_filledTime / _fillDuration) * 100f;
        IsComplete = Capacity >= 100f || _elapsed >= _duration;
    }

    public void SetVisuals(float capacity, float phase)
    {
        if (_visual == null)
            return;
        _properties.SetFloat("_Progress", Mathf.Clamp01(capacity / 100f));
        _properties.SetFloat("_Phase", phase);
        _visual.SetPropertyBlock(_properties);
    }
}
