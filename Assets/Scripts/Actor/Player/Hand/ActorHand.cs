using UnityEngine;

public sealed class ActorHand : MonoBehaviour
{
    [SerializeField] private Transform _handTransform;
    [SerializeField] private SpriteRenderer _visualHandSpriteRender;
    [SerializeField, Min(0f)] private float _maxRadius = 1f;

    public Vector2 Offset { get; private set; }

    private void LateUpdate()
    {
        bool shouldFlip = _handTransform.localPosition.x < 0f;

        if (_visualHandSpriteRender.flipX != shouldFlip)
            _visualHandSpriteRender.flipX = shouldFlip;
    }

    public void SetOffset(Vector2 offset)
    {
        Offset = Vector2.ClampMagnitude(offset, _maxRadius);
        _handTransform.localPosition = Offset;

        if (Offset.sqrMagnitude < 0.000001f)
            return;

        float angle = Mathf.Atan2(Offset.y, Offset.x) * Mathf.Rad2Deg;
        _handTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
