using System.Collections.Generic;
using UnityEngine;

public sealed class ActorHand : MonoBehaviour
{
    [SerializeField] private Transform _handTransform;
    [SerializeField] private SpriteRenderer _visualHandSpriteRender;
    [SerializeField, Min(0f)] private float _maxRadius = 1f;
    private readonly List<WorldItem> _pickupItems = new();

    public Vector2 CurrentOffset => _handTransform.localPosition;
    public Transform HandTransform => _handTransform;
    public Vector2 Position => _handTransform.position;
    public Vector2 ClampOffset(Vector2 offset)
    {
        if (float.IsNaN(offset.x) || float.IsNaN(offset.y) || float.IsInfinity(offset.x) || float.IsInfinity(offset.y))
            return Vector2.zero;
        return Vector2.ClampMagnitude(offset, _maxRadius);
    }

    public Vector2 AimDirection => _handTransform.right;
    public SpriteRenderer VisualRenderer => _visualHandSpriteRender;

    public Vector2 Offset { get; private set; }

    private void LateUpdate()
    {
        bool shouldFlip = _handTransform.localPosition.x < 0f;

        if (_visualHandSpriteRender.flipX != shouldFlip)
            _visualHandSpriteRender.flipX = shouldFlip;
    }

    public void SetOffset(Vector2 offset)
    {
        Offset = ClampOffset(offset);
        _handTransform.localPosition = Offset;

        if (Offset.sqrMagnitude < 0.000001f)
            return;

        float angle = Mathf.Atan2(Offset.y, Offset.x) * Mathf.Rad2Deg;
        _handTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void AddPickupItem(WorldItem item)
    {
        if (item != null && !_pickupItems.Contains(item))
            _pickupItems.Add(item);
    }

    public void RemovePickupItem(WorldItem item) => _pickupItems.Remove(item);

    public bool TryGetPickupItem(out WorldItem item)
    {
        for (int i = _pickupItems.Count - 1; i >= 0; i--)
        {
            WorldItem candidate = _pickupItems[i];

            if (candidate == null || !candidate.IsAvailable)
            {
                _pickupItems.RemoveAt(i);
                continue;
            }

            item = candidate;
            return true;
        }

        item = null;
        return false;
    }
}
