using UnityEngine;

using UnityEngine.UI;
using Unity.Netcode;

[RequireComponent(typeof(Inventory))]
public sealed class InventoryUI : NetworkBehaviour
{
    [SerializeField, Min(32f)] private float _slotSize = 64f;
    [SerializeField, Min(0f)] private float _slotSpacing = 3f;
    [SerializeField, Min(0f)] private float _bottomMargin = 24f;
    [SerializeField, Min(1)] private int _columns = 3;
    [SerializeField] private float _horizontalOffset = 280f;
    [SerializeField] private Color _panelColor = new Color(0.025f, 0.024f, 0.022f, 1f);
    [SerializeField] private Color _slotColor = new Color(0.055f, 0.05f, 0.042f, 1f);
    private GameObject _canvasObject;
    private Font _font;

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
        _canvasObject = new GameObject("InventoryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        _canvasObject.transform.SetParent(transform, false);
        Canvas canvas = _canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = _canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        PlayerVitalsUI vitals = GetComponent<PlayerVitalsUI>();
        float horizontalOffset = vitals != null ? vitals.RightEdge : _horizontalOffset;
        float bottomMargin = vitals != null ? vitals.BottomMargin : _bottomMargin;

        int columns = Mathf.Max(1, _columns);
        int rows = Mathf.CeilToInt((float)size / columns);
        float width = columns * _slotSize + (columns - 1) * _slotSpacing;
        float height = rows > 0 ? rows * _slotSize + (rows - 1) * _slotSpacing : 0f;

        RectTransform panel = CreateRect("InventoryPanel", _canvasObject.transform, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(horizontalOffset, bottomMargin), new Vector2(width + 16f, height + 16f));
        AddImage(panel, _panelColor);
        AddBorder(panel);

        RectTransform grid = CreateRect("Slots", panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(width, height));
        GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(_slotSize, _slotSize);
        layout.spacing = new Vector2(_slotSpacing, _slotSpacing);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = columns;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        for (int i = 0; i < size; i++)
        {
            RectTransform slot = CreateRect("Slot_" + i, grid, Vector2.zero, Vector2.zero, Vector2.zero, layout.cellSize);
            AddImage(slot, _slotColor);
            AddBorder(slot);
            CreateLabel(slot, (i + 1).ToString(), new Vector2(7f, -11f), 11, new Color(0.57f, 0.53f, 0.45f));
            int index = i;
            Inventory inventory = GetComponent<Inventory>();
            slot.gameObject.AddComponent<ItemSlotUI>().Bind(() => inventory.GetItem(index));
        }
    }


    private void RemoveInventoryUI()
    {
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
        image.raycastTarget = false;
    }

    private void AddBorder(RectTransform rect)
    {
        Outline border = rect.gameObject.AddComponent<Outline>();
        border.effectColor = new Color(0.16f, 0.145f, 0.12f, 1f);
        border.effectDistance = new Vector2(1f, -1f);
    }

    private void CreateLabel(Transform parent, string value, Vector2 position, int fontSize, Color color)
    {
        RectTransform rect = CreateRect("Label", parent, new Vector2(0f, 1f), new Vector2(0f, 0.5f), position, new Vector2(24f, 18f));
        Text label = rect.gameObject.AddComponent<Text>();
        label.font = _font;
        label.text = value;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
    }
}

