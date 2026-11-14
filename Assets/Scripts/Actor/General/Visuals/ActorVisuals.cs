using DG.Tweening;
using UnityEngine;

public sealed class ActorVisuals : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] _renderers = new SpriteRenderer[0];
    [SerializeField] private ParticleSystem _hitParticles;
    [SerializeField] private Transform _stunPivot;
    [SerializeField] private float _stunTilt = 30f;
    [SerializeField] private Sprite _stunStar;
    [SerializeField] private Material _stunStarMaterial;
    [SerializeField] private Color _hitColor = new Color(1f, 0.92f, 0.72f);
    [SerializeField, Min(0.01f)] private float _hitDuration = 0.15f;

    private static readonly int EffectColor = Shader.PropertyToID("_EffectColor");
    private static readonly int EffectAmount = Shader.PropertyToID("_EffectAmount");
    private static readonly int HitColor = Shader.PropertyToID("_HitColor");
    private static readonly int HitAmount = Shader.PropertyToID("_HitAmount");
    private static readonly int OutlineOpacity = Shader.PropertyToID("_OutlineOpacity");
    private MaterialPropertyBlock _properties;
    private EffectHandler _effects;
    private Quaternion _restRotation;
    private Tween _stunMotion;
    private bool _showStun;
    private bool _isAlive = true;
    private SpriteRenderer[] _stunStars;
    private float _stunStartedAt;
    private Color _effectColor = Color.white;
    private float _effectAmount;
    private float _hitAmount;
    private float _hitEndsAt;
    private float _outlineOpacity = 1f;

    private void Awake()
    {
        _properties = new MaterialPropertyBlock();
        _effects = GetComponent<EffectHandler>();
        if (_stunPivot != null)
            _restRotation = _stunPivot.localRotation;
    }

    private void OnEnable()
    {
        if (_effects != null)
            _effects.StatusChanged += UpdateStatus;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (_stunPivot == null || _effects == null)
            return;

        bool stunned = _isAlive && _effects.IsStunned && !_effects.IsDowned;
        if (stunned == _showStun)
            return;

        _showStun = stunned;
        SetStunStarsVisible(stunned);
        _stunMotion?.Kill();
        if (!stunned)
        {
            _stunMotion = _stunPivot.DOLocalRotateQuaternion(_restRotation, 0.14f).SetEase(Ease.OutQuad);
            return;
        }

        Quaternion tilt = _restRotation * Quaternion.Euler(0f, 0f, -_stunTilt);
        _stunMotion = _stunPivot.DOLocalRotateQuaternion(tilt, 0.12f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            Quaternion sway = _restRotation * Quaternion.Euler(0f, 0f, -_stunTilt + 9f);
            _stunMotion = _stunPivot.DOLocalRotateQuaternion(sway, 0.22f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        });
    }

    public void SetAlive(bool isAlive)
    {
        _isAlive = isAlive;
        UpdateStatus();
    }

    private void SetStunStarsVisible(bool visible)
    {
        if (visible && _stunStars == null && _stunStar != null)
        {
            _stunStars = new SpriteRenderer[3];
            for (int i = 0; i < _stunStars.Length; i++)
            {
                SpriteRenderer star = new GameObject("StunStar").AddComponent<SpriteRenderer>();
                star.transform.SetParent(transform, false);
                star.sprite = _stunStar;
                star.sharedMaterial = _stunStarMaterial;
                star.sortingOrder = 4;
                _stunStars[i] = star;
            }
        }

        if (_stunStars == null)
            return;

        foreach (SpriteRenderer star in _stunStars)
            star.enabled = visible;
        if (visible)
            _stunStartedAt = Time.time;
    }

    private void LateUpdate()
    {
        if (!_showStun || _stunStars == null)
            return;

        float top = transform.position.y;
        float halfWidth = 0.25f;
        foreach (SpriteRenderer renderer in _renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            Bounds bounds = renderer.bounds;
            top = Mathf.Max(top, bounds.max.y);
            halfWidth = Mathf.Max(halfWidth, bounds.extents.x);
        }

        float spin = (Time.time - _stunStartedAt) * 5f;
        for (int i = 0; i < _stunStars.Length; i++)
        {
            float phase = spin + i * (2f * Mathf.PI / _stunStars.Length);
            float depth = Mathf.Sin(phase);
            Transform star = _stunStars[i].transform;
            star.position = new Vector3(transform.position.x + Mathf.Cos(phase) * (halfWidth + 0.12f) * 0.7f,
                top + 0.22f + depth * 0.07f, transform.position.z);
            star.localScale = Vector3.one * (0.25f + depth * 0.05f);
            star.localRotation = Quaternion.Euler(0f, 0f, Mathf.Cos(phase) * 22f);
        }
    }

    public void SetRenderers(SpriteRenderer[] renderers)
    {
        _renderers = renderers;
        Apply();
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
        _stunMotion?.Kill();
        _stunMotion = null;
        _showStun = false;
        SetStunStarsVisible(false);
        if (_stunPivot != null)
            _stunPivot.localRotation = _restRotation;
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
