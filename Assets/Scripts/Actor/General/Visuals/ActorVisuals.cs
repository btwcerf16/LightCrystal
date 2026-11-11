using UnityEngine;

public sealed class ActorVisuals : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] _renderers = new SpriteRenderer[0];
    [SerializeField] private ParticleSystem _hitParticles;
    [SerializeField] private Color _hitColor = new Color(1f, 0.92f, 0.72f);
    [SerializeField, Min(0.01f)] private float _hitDuration = 0.15f;

    private static readonly int EffectColor = Shader.PropertyToID("_EffectColor");
    private static readonly int EffectAmount = Shader.PropertyToID("_EffectAmount");
    private static readonly int HitColor = Shader.PropertyToID("_HitColor");
    private static readonly int HitAmount = Shader.PropertyToID("_HitAmount");
    private static readonly int OutlineOpacity = Shader.PropertyToID("_OutlineOpacity");
    private MaterialPropertyBlock _properties;
    private EffectHandler _effects;
    private Animator[] _animators;
    private Color _effectColor = Color.white;
    private float _effectAmount;
    private float _hitAmount;
    private float _hitEndsAt;
    private float _outlineOpacity = 1f;

    private void Awake()
    {
        _properties = new MaterialPropertyBlock();
        _effects = GetComponent<EffectHandler>();
        _animators = GetComponentsInChildren<Animator>(true);
    }

    private void OnEnable()
    {
        if (_effects != null)
            _effects.StatusChanged += UpdateStatus;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        bool stunned = _effects != null && _effects.IsStunned;
        foreach (Animator animator in _animators)
            if (animator != null)
                animator.speed = stunned ? 0f : 1f;
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

    public void SetOutlineOpacity(float opacity)
    {
        _outlineOpacity = opacity;
        Apply();
    }

    public void FlashHit()
    {
        if (_hitParticles != null)
        {
            _hitParticles.Play();
            _hitParticles.Emit(6);
        }
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

    public void ShowDamage(float damage, EDamageType type = EDamageType.Physical, bool critical = false)
    {
        FloatingDamage.Show(transform.position, damage, type, critical);
    }

    public void Explosion(float radius)
    {
        if (_hitParticles == null)
            return;
        _hitParticles.Play();
        for (int i = 0; i < 20; i++)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            ParticleSystem.EmitParams particle = new ParticleSystem.EmitParams
            {
                startColor = new Color(1f, 0.45f, 0.08f),
                startLifetime = 0.3f,
                startSize = 0.07f,
                velocity = direction * radius * 3f
            };
            _hitParticles.Emit(particle, 1);
        }
    }

    public void ClearHit()
    {
        _hitAmount = 0f;
        Apply();
    }

    private void OnDisable()
    {
        if (_effects != null)
            _effects.StatusChanged -= UpdateStatus;
        foreach (Animator animator in _animators)
            if (animator != null)
                animator.speed = 1f;
        ClearHit();
        if (_hitParticles != null)
            _hitParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
            _properties.SetFloat(OutlineOpacity, _outlineOpacity);
            renderer.SetPropertyBlock(_properties);
        }
    }
}
