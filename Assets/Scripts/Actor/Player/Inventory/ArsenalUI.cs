using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(ActorArsenal))]
public sealed class ArsenalUI : NetworkBehaviour
{
    [SerializeField, Min(32f)] private float _slotSize = 64f;
    [SerializeField, Min(1)] private int _maxColumns = 1;
    private GameObject _canvasObject;
    private Font _font;
    private PlayerSlotBindings _slotBindings;
    private readonly List<Text> _bindingLabels = new List<Text>();

    private int Columns => Mathf.Min(GetComponent<ActorArsenal>().WeaponSize, Mathf.Max(1, _maxColumns));
    public float Width => Columns * _slotSize + 16f;
    public float Height => Columns > 0 ? Mathf.CeilToInt((float)GetComponent<ActorArsenal>().WeaponSize / Columns) * _slotSize + 32f : 0f;

    public override void OnNetworkSpawn() => Show();
    public override void OnGainedOwnership() => Show();
    public override void OnLostOwnership() => Hide();
    public override void OnNetworkDespawn() => Hide();
    private void OnEnable() => Show();
    private void OnDisable() => Hide();

    private void Show()
    {
        if (!isActiveAndEnabled || !IsSpawned || !IsClient || !IsOwner || _canvasObject != null)
            return;
        int count = GetComponent<ActorArsenal>().WeaponSize;
        if (count <= 0)
            return;

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _slotBindings = GetComponent<PlayerSlotBindings>();
        int columns = Columns;
        int rows = Mathf.CeilToInt((float)count / columns);
        float width = columns * _slotSize;
        float height = rows * _slotSize;

        RectTransform panel = GetComponent<PlayerHUD>().ArsenalPanel;
        _canvasObject = panel.gameObject;
        Paint(panel, HudStyle.Surface);
        Label(panel, "Арсенал", new Vector2(8f, -12f), width, 12);

        RectTransform grid = Rect("WeaponSlots", panel, Vector2.zero, Vector2.zero, new Vector2(8f, 8f), new Vector2(width, height));
        GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = Vector2.one * _slotSize;
        layout.spacing = Vector2.zero;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = columns;
        layout.childAlignment = TextAnchor.UpperCenter;
        for (int i = 0; i < count; i++)
        {
            RectTransform slot = Rect("WeaponSlot_" + i, grid, Vector2.zero, Vector2.zero, Vector2.zero, layout.cellSize);
            Paint(slot, HudStyle.Slot);
            int index = i;
            ActorArsenal arsenal = GetComponent<ActorArsenal>();
            slot.gameObject.AddComponent<ItemSlotUI>().Bind(() => arsenal.GetWeapon(index), arsenal, index,
                arsenal.RequestChangeWeaponSlot, arsenal.RequestDropWeapon, arsenal.RequestSelectWeapon);
            _bindingLabels.Add(HudStyle.Binding(slot, _slotBindings.GetBindingLabel(true, i)));
        }
        _slotBindings.BindingsChanged += RefreshBindings;
    }

    private void RefreshBindings()
    {
        for (int i = 0; i < _bindingLabels.Count; i++)
            HudStyle.SetBinding(_bindingLabels[i], _slotBindings.GetBindingLabel(true, i));
    }

    private void Hide()
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

    private RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private void Paint(RectTransform rect, Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
    }

    private Text Label(Transform parent, string value, Vector2 position, float width, int size)
    {
        RectTransform rect = Rect("Label", parent, new Vector2(0f, 1f), new Vector2(0f, 0.5f), position, new Vector2(width, 18f));
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = _font;
        text.text = value;
        text.fontSize = size;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = size;
        text.color = HudStyle.Muted;
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        return text;
    }
}
