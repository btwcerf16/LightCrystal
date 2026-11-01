using Unity.Netcode;
using UnityEngine;

public class WorldItem : NetworkBehaviour, ITooltipSource
{
    [SerializeField] private GameObject _itemPrefab;
    [SerializeField] private ItemDatabase _itemDatabase;
    [SerializeField] private SpriteRenderer _itemVisualSpriteRenderer;
    [SerializeField] private BoxCollider2D _boxCollider;

    [SerializeField] private TextMesh _priceText;
    private Material _priceMaterial;
    private bool _hasPendingItem;
    private bool _initializeForSale;

    private readonly NetworkVariable<EItemRarity> _rarity = new();
    private readonly NetworkVariable<EnchantState> _enchants = new();
    public EItemRarity Rarity => _rarity.Value;
    public EnchantState Enchants => _enchants.Value;

    private readonly NetworkVariable<bool> _isForSale = new(false);
    private readonly NetworkVariable<int> _itemId = new NetworkVariable<int>();
    private readonly NetworkVariable<ulong> _uid = new NetworkVariable<ulong>();
    private readonly NetworkVariable<bool> _isVisible = new NetworkVariable<bool>(true);
    private readonly NetworkVariable<Vector2> _position = new NetworkVariable<Vector2>();

    public string TooltipTitle => ItemPrefab != null ? ItemPrefab.DisplayName : "Предмет";
    public float TooltipDelay => 0f;
    public string TooltipDescription
    {
        get
        {
            Item prefab = ItemPrefab;
            if (prefab == null)
                return "";
            return prefab.Description + "\n\n" + prefab.StatsDescription;
        }
    }

    public bool IsForSale => _isForSale.Value;
    public int Price => ItemPrefab != null ? Mathf.Max(0, ItemPrefab.Price) : 0;
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
        if (_priceText != null)
        {
            var renderer = _priceText.GetComponent<MeshRenderer>();
            _priceMaterial = new Material(renderer.sharedMaterial);
            renderer.sharedMaterial = _priceMaterial;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _position.Value = transform.position;
            if (_itemPrefab != null && (ItemId == 0 || _hasPendingItem))
            {
                SetItem(_itemPrefab, _initializeForSale);
                _hasPendingItem = false;
            }
        }

        _isForSale.OnValueChanged += OnSaleChanged;
        Font.textureRebuilt += RefreshPriceFont;
        _itemId.OnValueChanged += OnItemChanged;
        _isVisible.OnValueChanged += SetVisible;
        _position.OnValueChanged += OnPositionChanged;
        RefreshSprite();
        SetVisible(_isVisible.Value, _isVisible.Value);
        OnPositionChanged(_position.Value, _position.Value);
    }

    public override void OnNetworkDespawn()
    {
        _isForSale.OnValueChanged -= OnSaleChanged;
        Font.textureRebuilt -= RefreshPriceFont;
        if (_priceText != null)
            _priceText.gameObject.SetActive(false);
        _itemId.OnValueChanged -= OnItemChanged;
        _isVisible.OnValueChanged -= SetVisible;
        _position.OnValueChanged -= OnPositionChanged;
    }

    public void SetItem(GameObject itemObject, bool isForSale = false)
    {
        Item item = itemObject.GetComponent<Item>();
        if (item == null)
            return;

        if (!IsSpawned)
        {
            _itemPrefab = itemObject;
            _initializeForSale = isForSale;
            _hasPendingItem = true;
            return;
        }
        if (!IsServer)
            return;

        _rarity.Value = item.Rarity;
        _enchants.Value = item is Weapon weapon ? weapon.GetEnchantState() : default;
        _isForSale.Value = isForSale;
        _itemId.Value = item.ItemId;
        _uid.Value = item.Uid != 0 ? item.Uid : NetworkManager.GetComponent<ItemUidGenerator>().GenerateUid();
    }

    public bool TryPickUp(ActorWallet wallet)
    {
        if (!IsServer || !CanBePicked)
            return false;
        if (IsForSale && (wallet == null || !wallet.TrySpend(Price)))
            return false;

        _isForSale.Value = false;
        HideWorldItem();
        return true;
    }

    private void OnSaleChanged(bool previous, bool current)
    {
        RefreshPrice();
    }

    private void RefreshPrice()
    {
        if (_priceText == null)
            return;
        _priceText.gameObject.SetActive(IsSpawned && IsForSale && _isVisible.Value && ItemId > 0);
        _priceText.text = $"{Price}$";
        if (_itemVisualSpriteRenderer.sprite != null)
        {
            Bounds bounds = _itemVisualSpriteRenderer.sprite.bounds;
            _priceText.transform.localPosition = new Vector3(bounds.center.x, bounds.min.y - 0.12f, 0f);
        }
        RefreshPriceFont(_priceText.font);
    }

    private void RefreshPriceFont(Font font)
    {
        if (_priceMaterial != null && _priceText != null && font == _priceText.font)
            _priceMaterial.mainTexture = font.material.mainTexture;
    }

    public override void OnDestroy()
    {
        Font.textureRebuilt -= RefreshPriceFont;
        if (_priceMaterial != null)
            Destroy(_priceMaterial);
        base.OnDestroy();
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
        RefreshPrice();
    }
}
