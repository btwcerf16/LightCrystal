using UnityEngine;

public sealed class ActorVisuals : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] _renderers = new SpriteRenderer[0];
    [SerializeField] private Color _hitColor = new Color(1f, 0.92f, 0.72f);
    [SerializeField, Min(0.01f)] private float _hitDuration = 0.15f;

    private static readonly int EffectColor = Shader.PropertyToID("_EffectColor");
    private static readonly int EffectAmount = Shader.PropertyToID("_EffectAmount");
    private static readonly int HitColor = Shader.PropertyToID("_HitColor");
    private static readonly int HitAmount = Shader.PropertyToID("_HitAmount");
    private MaterialPropertyBlock _properties;
    private Color _effectColor = Color.white;
    private float _effectAmount;
    private float _hitAmount;
    private float _hitEndsAt;

    private void Awake()
    {
        _properties = new MaterialPropertyBlock();
    }

    public void SetEffectTint(Color color, float amount)
    {
        _effectColor = color;
        _effectAmount = Mathf.Clamp01(amount);
        Apply();
    }

    public void ClearEffectTint()
    {
        _effectAmount = 0f;
        Apply();
    }

    public void FlashHit()
    {
        _hitAmount = 1f;
        _hitEndsAt = Time.time + _hitDuration;
        Apply();
    }

    private void Update()
    {
        if (_hitAmount <= 0f)
            return;
        _hitAmount = Mathf.Clamp01((_hitEndsAt - Time.time) / _hitDuration);
        Apply();
    }

    public void ClearHit()
    {
        _hitAmount = 0f;
        Apply();
    }

    private void OnDisable()
    {
        ClearHit();
    }

    private void Apply()
    {
        foreach (SpriteRenderer renderer in _renderers)
        {
            if (renderer == null)
                continue;
            renderer.GetPropertyBlock(_properties);
            _properties.SetColor(EffectColor, _effectColor);
            _properties.SetFloat(EffectAmount, _effectAmount);
            _properties.SetColor(HitColor, _hitColor);
            _properties.SetFloat(HitAmount, _hitAmount);
            renderer.SetPropertyBlock(_properties);
        }
    }
}
