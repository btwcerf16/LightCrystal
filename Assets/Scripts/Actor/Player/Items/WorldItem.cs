using Unity.Netcode;
using UnityEngine;

public class WorldItem : NetworkBehaviour
{
    [SerializeField] private GameObject _itemPrefab;
    [SerializeField] private ItemDatabase _itemDatabase;
    [SerializeField] private SpriteRenderer _itemVisualSpriteRenderer;
    [SerializeField] private BoxCollider2D _boxCollider;

    private readonly NetworkVariable<int> _itemId = new NetworkVariable<int>();
    private readonly NetworkVariable<ulong> _uid = new NetworkVariable<ulong>();
    private readonly NetworkVariable<bool> _isVisible = new NetworkVariable<bool>(true);
    private readonly NetworkVariable<Vector2> _position = new NetworkVariable<Vector2>();

    public int ItemId => _itemId.Value;
    public ulong Uid => _uid.Value;
    public Item ItemPrefab => _itemDatabase != null ? _itemDatabase.GetPrefab(ItemId) : null;
    public bool CanBePicked => IsSpawned && _isVisible.Value && ItemId > 0 && Uid != 0;

    private void Awake()
    {
        if (_itemVisualSpriteRenderer == null)
            _itemVisualSpriteRenderer = GetComponent<SpriteRenderer>();
        if (_boxCollider == null)
            _boxCollider = GetComponent<BoxCollider2D>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _position.Value = transform.position;
            if (ItemId == 0 && _itemPrefab != null)
                SetItem(_itemPrefab);
        }

        _itemId.OnValueChanged += OnItemChanged;
        _isVisible.OnValueChanged += SetVisible;
        _position.OnValueChanged += OnPositionChanged;
        RefreshSprite();
        SetVisible(_isVisible.Value, _isVisible.Value);
        OnPositionChanged(_position.Value, _position.Value);
    }

    public override void OnNetworkDespawn()
    {
        _itemId.OnValueChanged -= OnItemChanged;
        _isVisible.OnValueChanged -= SetVisible;
        _position.OnValueChanged -= OnPositionChanged;
    }

    public void SetItem(GameObject itemObject)
    {
        Item item = itemObject.GetComponent<Item>();
        if (item == null)
            return;

        _itemId.Value = item.ItemId;
        _uid.Value = item.Uid != 0 ? item.Uid : NetworkManager.GetComponent<ItemUidGenerator>().GenerateUid();
    }

    public void ShowAt(Vector2 position)
    {
        _position.Value = position;
        transform.position = position;
        _isVisible.Value = true;
    }

    public void HideWorldItem()
    {
        _isVisible.Value = false;
    }

    public bool IsUnderHand(Vector2 position)
    {
        return CanBePicked && _boxCollider.OverlapPoint(position);
    }

    private void OnItemChanged(int previousId, int newId)
    {
        RefreshSprite();
        SetVisible(_isVisible.Value, _isVisible.Value);
    }

    private void RefreshSprite()
    {
        Item prefab = _itemDatabase.GetPrefab(ItemId);
        _itemVisualSpriteRenderer.sprite = prefab != null ? prefab.Icon : null;
        if (_itemVisualSpriteRenderer.sprite != null)
        {
            _boxCollider.size = _itemVisualSpriteRenderer.sprite.bounds.size;
            _boxCollider.offset = _itemVisualSpriteRenderer.sprite.bounds.center;
        }
    }

    private void OnPositionChanged(Vector2 previousPosition, Vector2 newPosition)
    {
        transform.position = newPosition;
    }

    private void SetVisible(bool previousVisible, bool newVisible)
    {
        bool isVisible = newVisible && ItemId > 0;
        _itemVisualSpriteRenderer.enabled = isVisible;
        _boxCollider.enabled = isVisible;
    }
}
