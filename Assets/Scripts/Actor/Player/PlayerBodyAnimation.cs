using UnityEngine;

public sealed class PlayerBodyAnimation : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    private Vector3 _previousPosition;
    private EffectHandler _effects;
    private float _movingUntil;

    private void Awake()
    {
        _effects = GetComponent<EffectHandler>();
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
        bool stunned = _effects != null && _effects.IsStunned;
        if (!stunned && offset.sqrMagnitude > 0.000001f)
            _movingUntil = Time.time + 0.12f;
        _animator.SetBool("IsMoving", !stunned && Time.time < _movingUntil);
        _animator.speed = stunned ? 0f : 1f;
    }
}
