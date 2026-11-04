using UnityEngine;

using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.Collections.Generic;

[RequireComponent(typeof(Inventory))]
public sealed class InventoryUI : NetworkBehaviour
{
    [SerializeField, Min(32f)] private float _slotSize = 64f;
    [SerializeField, Min(1)] private int _columns = 3;
    private GameObject _canvasObject;
    private PlayerSlotBindings _slotBindings;
    private readonly List<Text> _bindingLabels = new List<Text>();

    public float Width => Mathf.Max(1, _columns) * _slotSize + 16f;
    public float Height => Mathf.CeilToInt((float)GetComponent<Inventory>().Size / Mathf.Max(1, _columns)) * _slotSize + 32f;

    public override void OnNetworkSpawn()
    {
        GetComponent<Inventory>().SizeChanged += Rebuild;
        if (IsClient && IsOwner)
            CreateInventoryUI(GetComponent<Inventory>().Size);
    }

    public override void OnGainedOwnership()
    {
        if (IsClient)
            CreateInventoryUI(GetComponent<Inventory>().Size);
    }

    public override void OnLostOwnership() => RemoveInventoryUI();
    public override void OnNetworkDespawn()
    {
        GetComponent<Inventory>().SizeChanged -= Rebuild;
        RemoveInventoryUI();
    }

    private void Rebuild()
    {
        if (_canvasObject == null)
            return;
        _slotBindings.BindingsChanged -= RefreshBindings;
        foreach (Transform child in _canvasObject.transform)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        _bindingLabels.Clear();
        _canvasObject = null;
        GetComponent<PlayerHUD>().RefreshLayout();
        CreateInventoryUI(GetComponent<Inventory>().Size);
    }

    public void CreateInventoryUI(int size)
    {
        if (!IsSpawned || !IsClient || !IsOwner || _canvasObject != null)
            return;

        _slotBindings = GetComponent<PlayerSlotBindings>();
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        int columns = Mathf.Max(1, _columns);
        int rows = Mathf.CeilToInt((float)size / columns);
        float width = columns * _slotSize;
        float height = rows * _slotSize;

        RectTransform panel = GetComponent<PlayerHUD>().InventoryPanel;
        _canvasObject = panel.gameObject;
        AddImage(panel, HudStyle.Surface);

        RectTransform grid = CreateRect("Slots", panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(width, height));
        GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(_slotSize, _slotSize);
        layout.spacing = Vector2.zero;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = columns;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        for (int i = 0; i < size; i++)
        {
            RectTransform slot = CreateRect("Slot_" + i, grid, Vector2.zero, Vector2.zero, Vector2.zero, layout.cellSize);
            AddImage(slot, HudStyle.Slot);
            int index = i;
            Inventory inventory = GetComponent<Inventory>();
            slot.gameObject.AddComponent<ItemSlotUI>().Bind(() => inventory.GetItem(index), inventory, index,
                inventory.RequestChangeItemSlot, inventory.RequestDropItem, inventory.RequestUseItem);
            _bindingLabels.Add(HudStyle.Binding(slot, _slotBindings.GetBindingLabel(false, i)));
        }
        _slotBindings.BindingsChanged += RefreshBindings;
        GetComponent<PlayerHUD>().RefreshLayout();
    }

    private void RefreshBindings()
    {
        for (int i = 0; i < _bindingLabels.Count; i++)
            HudStyle.SetBinding(_bindingLabels[i], _slotBindings.GetBindingLabel(false, i));
    }


    private void RemoveInventoryUI()
    {
        if (_slotBindings != null)
            _slotBindings.BindingsChanged -= RefreshBindings;
        _bindingLabels.Clear();
        if (_canvasObject == null)
            return;
        _canvasObject.SetActive(false);
        Destroy(_canvasObject);
        _canvasObject = null;
    }

    private RectTransform CreateRect(string objectName, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private void AddImage(RectTransform rect, Color color)
    {
        Image image = rect.GetComponent<Image>();
        if (image == null)
            image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
    }

}
