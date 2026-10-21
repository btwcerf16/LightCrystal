using Unity.Netcode;
using UnityEngine;

public sealed class ActorHand : MonoBehaviour
{
    [SerializeField] private Transform _handTransform;
    [SerializeField] private SpriteRenderer _visualHandSpriteRender;
    [SerializeField] private WorldItem _worldItemPrefab;
    [SerializeField, Min(0f)] private float _maxRadius = 1f;

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

    public WorldItem DropItem(WorldItem worldItem, GameObject itemPrefab)
    {
        NetworkObject player = GetComponentInParent<NetworkObject>();
        if (player == null || !player.IsSpawned || !player.NetworkManager.IsServer || itemPrefab == null)
            return null;

        if (worldItem == null)
        {
            if (_worldItemPrefab == null)
                return null;

            worldItem = Instantiate(_worldItemPrefab, _handTransform.position, Quaternion.identity);
            worldItem.SetItem(itemPrefab);
            worldItem.NetworkObject.Spawn();
            return worldItem;
        } 

        worldItem.transform.position = _handTransform.position;
        worldItem.ThrowAway();
        return worldItem;
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

}
