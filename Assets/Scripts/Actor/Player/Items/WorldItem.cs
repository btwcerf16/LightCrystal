using Unity.Netcode;
using UnityEngine;
using UnityEngine.U2D;

public class WorldItem : NetworkBehaviour
{
    [SerializeField] private GameObject _itemPrefab;
    [SerializeField] private SpriteRenderer _itemVisualSpriteRenderer;
    [SerializeField] private BoxCollider2D _boxCollider;
    private NetworkVariable<bool> _isVisible = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public bool CanBePicked { get; private set; } = true;

    public override void OnNetworkSpawn()
    {
        if(_itemPrefab != null)
        {
            SetItem(_itemPrefab);
            _isVisible.OnValueChanged += SetVisible;
            SetVisible(_isVisible.Value, _isVisible.Value);
        }
    }
    public override void OnNetworkDespawn()
    {
        _isVisible.OnValueChanged -= SetVisible;
    }
    public void SetItem(GameObject itemPrefab)
    {
        _itemPrefab = itemPrefab;
        _itemVisualSpriteRenderer.sprite = itemPrefab.GetComponentInChildren<SpriteRenderer>().sprite;
    }
    public GameObject PickUp()
    {
        if(_itemPrefab != null && CanBePicked)
        {
            HideWorldItem();
 
            return _itemPrefab;
        }
        return null;
    }

    public void ThrowAway()
    {
        ShowUpWorldItem(true);
    }

    public void ShowUpWorldItem(bool afterShowupCanBePick)
    {
        _isVisible.Value = true;
    }

    public void HideWorldItem()
    {
        
        _isVisible.Value = false;
    }

    public void SetVisible(bool previousVisible, bool newVisible)
    {
        _itemVisualSpriteRenderer.enabled = newVisible;
        _boxCollider.enabled = newVisible;
        ChangeCanBePicked(newVisible);

    }

    public void ChangeCanBePicked(bool isPickable)
    {
        CanBePicked = isPickable;
    }
}
