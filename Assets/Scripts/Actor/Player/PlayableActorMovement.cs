using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayableActorMovement : MonoBehaviour, IMovable
{
    [SerializeField, Min(0f)]
    private float _moveSpeed = 5f;

    private Rigidbody2D _rigidbody;

    private Vector2 _moveDirection;

    private Vector2 _forceStartVelocity;
    private Vector2 _forceVelocity;

    private float _forceDuration;
    private float _forceElapsedTime;

    private bool _isForceActive;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void OnDisable()
    {
        Stop();
    }

    private void FixedUpdate()
    {
        UpdateForce();

        Vector2 movementVelocity = _moveDirection * _moveSpeed;

        _rigidbody.linearVelocity = movementVelocity + _forceVelocity;
    }

    public void Move(Vector2 direction)
    {
        _moveDirection = Vector2.ClampMagnitude(direction, 1f);
    }

    public void Stop()
    {
        _moveDirection = Vector2.zero;

        _forceStartVelocity = Vector2.zero;
        _forceVelocity = Vector2.zero;

        _forceDuration = 0f;
        _forceElapsedTime = 0f;

        _isForceActive = false;

        if (_rigidbody != null)
            _rigidbody.linearVelocity = Vector2.zero;
    }

    private void UpdateForce()
    {
        if (!_isForceActive)
            return;

        _forceElapsedTime += Time.fixedDeltaTime;

        float progress = Mathf.Clamp01(_forceElapsedTime / _forceDuration);

        _forceVelocity = Vector2.Lerp(_forceStartVelocity, Vector2.zero, progress);

        if (progress >= 1f)
        {
            _forceVelocity = Vector2.zero;
            _isForceActive = false;
        }
    }

    public void ApplyKnockback(Vector2 direction, float strength, float duration)
    {
        if (direction.sqrMagnitude == 0f)
            return;

        if (strength <= 0f || duration <= 0f)
            return;

        _forceStartVelocity =
            direction.normalized * strength;

        _forceVelocity = _forceStartVelocity;

        _forceDuration = duration;
        _forceElapsedTime = 0f;

        _isForceActive = true;
    }
}