using UnityEngine;

public sealed class PlayerBodyAnimation : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private SpellRangePreview _reviveZone;
    [SerializeField] private DownedEffectData _downedEffect;
    private Vector3 _previousPosition;
    private EffectHandler _effects;
    private NetworkActorVitals _vitals;
    private float _movingUntil;

    private void Awake()
    {
        _effects = GetComponent<EffectHandler>();
        _vitals = GetComponent<NetworkActorVitals>();
        _previousPosition = transform.position;
    }

    private void OnEnable()
    {
        _previousPosition = transform.position;
        _movingUntil = 0f;
    }

    private void LateUpdate()
    {
        Vector3 offset = transform.position - _previousPosition;
        _previousPosition = transform.position;
        bool downed = _effects != null && _effects.IsDowned;
        if (downed)
            _reviveZone.Show(_downedEffect.ReviveRadius);
        else
            _reviveZone.Hide();
        _animator.SetBool("IsDowned", downed);
        _animator.SetFloat("ReviveProgress", downed ? _vitals.ReviveProgress : 0f, 0.1f, Time.deltaTime);
        bool stunned = _effects != null && _effects.IsStunned;
        if (!stunned && offset.sqrMagnitude > 0.000001f)
            _movingUntil = Time.time + 0.12f;
        _animator.SetBool("IsMoving", !stunned && Time.time < _movingUntil);
        _animator.speed = stunned && !downed ? 0f : 1f;
    }
}
