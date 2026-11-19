using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class SilenceMarkVisual : NetworkBehaviour
{
    [SerializeField] private Vector2 _headOffset = new(0f, 0.7f);
    private readonly NetworkVariable<Vector2> _timing = new();
    private SpriteRenderer _rune;
    private Color _color;

    private void Awake()
    {
        _rune = GetComponent<SpriteRenderer>();
        _color = _rune.color;
    }

    public void Restart(float duration)
    {
        if (IsServer && IsSpawned)
            _timing.Value = new Vector2((float)NetworkManager.ServerTime.Time, duration);
    }

    private void LateUpdate()
    {
        if (!IsSpawned || transform.parent == null || _timing.Value.y <= 0f)
            return;

        transform.position = transform.parent.position + (Vector3)_headOffset;
        float elapsed = (float)NetworkManager.ServerTime.Time - _timing.Value.x;
        float progress = Mathf.Clamp01(elapsed / _timing.Value.y);
        Color color = Color.Lerp(_color, Color.white, progress * progress * 0.75f);
        color.a = Mathf.Lerp(0.3f, 1f, progress * progress);
        _rune.color = color;
        _rune.enabled = true;
    }
}
