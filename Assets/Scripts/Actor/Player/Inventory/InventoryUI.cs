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
    [SerializeField] private Color _panelColor = new Color(0.025f, 0.024f, 0.022f, 1f);
    [SerializeField] private Color _slotColor = new Color(0.055f, 0.05f, 0.042f, 1f);
    private GameObject _canvasObject;
    private Font _font;
    private PlayerSlotBindings _slotBindings;
    private readonly List<Text> _bindingLabels = new List<Text>();

    public float Width => Mathf.Max(1, _columns) * _slotSize + 16f;
    public float Height => Mathf.CeilToInt((float)GetComponent<Inventory>().Size / Mathf.Max(1, _columns)) * _slotSize + 40f;

    public override void OnNetworkSpawn()
    {
        if (IsClient && IsOwner)
            CreateInventoryUI(GetComponent<Inventory>().Size);
    }

    public override void OnGainedOwnership()
    {
        if (IsClient)
            CreateInventoryUI(GetComponent<Inventory>().Size);
    }

    public override void OnLostOwnership() => RemoveInventoryUI();
    public override void OnNetworkDespawn() => RemoveInventoryUI();

    public void CreateInventoryUI(int size)
    {
        if (!IsSpawned || !IsClient || !IsOwner || _canvasObject != null)
            return;

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _slotBindings = GetComponent<PlayerSlotBindings>();
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        int columns = Mathf.Max(1, _columns);
        int rows = Mathf.CeilToInt((float)size / columns);
        float width = columns * _slotSize;
        float height = rows * _slotSize;

        RectTransform panel = GetComponent<PlayerHUD>().InventoryPanel;
        _canvasObject = panel.gameObject;
        AddImage(panel, _panelColor);
        AddBorder(panel);
        CreateLabel(panel, "ИНВЕНТАРЬ", new Vector2(8f, -14f), 11, new Color(0.67f, 0.61f, 0.49f)).rectTransform.sizeDelta = new Vector2(width, 18f);

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
            AddImage(slot, _slotColor);
            AddBorder(slot);
            _bindingLabels.Add(CreateLabel(slot, _slotBindings.GetBindingLabel(false, i), new Vector2(7f, -11f), 10, new Color(0.57f, 0.53f, 0.45f)));
            int index = i;
            Inventory inventory = GetComponent<Inventory>();
            slot.gameObject.AddComponent<ItemSlotUI>().Bind(() => inventory.GetItem(index), inventory, index,
                inventory.RequestChangeItemSlot, inventory.RequestDropItem, inventory.RequestUseItem);
        }
        _slotBindings.BindingsChanged += RefreshBindings;
    }

    private void RefreshBindings()
    {
        for (int i = 0; i < _bindingLabels.Count; i++)
            _bindingLabels[i].text = _slotBindings.GetBindingLabel(false, i);
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
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
    }

    private void AddBorder(RectTransform rect)
    {
        Outline border = rect.gameObject.AddComponent<Outline>();
        border.effectColor = new Color(0.16f, 0.145f, 0.12f, 1f);
        border.effectDistance = new Vector2(1f, -1f);
    }

    private Text CreateLabel(Transform parent, string value, Vector2 position, int fontSize, Color color)
    {
        RectTransform rect = CreateRect("Label", parent, new Vector2(0f, 1f), new Vector2(0f, 0.5f), position, new Vector2(_slotSize - 12f, 18f));
        Text label = rect.gameObject.AddComponent<Text>();
        label.font = _font;
        label.text = value;
        label.fontSize = fontSize;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 7;
        label.resizeTextMaxSize = fontSize;
        label.color = color;
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
        return label;
    }
}

