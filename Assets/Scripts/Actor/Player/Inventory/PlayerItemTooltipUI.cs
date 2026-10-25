using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class PlayerItemTooltipUI : NetworkBehaviour
{
    private readonly List<RaycastResult> _uiHits = new();
    private readonly List<Collider2D> _worldHits = new();
    private GameObject _canvasObject;
    private RectTransform _canvasRect;
    private RectTransform _panel;
    private Text _title;
    private Text _body;
    private PointerEventData _pointerData;
    private PlayerInputReader _input;
    private Camera _camera;
    private Item _shownItem;
    private EItemRarity _shownRarity;
    private Outline _border;
    private ItemRarityBorder _ornament;

    public override void OnNetworkSpawn() => Bind();
    public override void OnGainedOwnership() => Bind();
    public override void OnLostOwnership() => Unbind();
    public override void OnNetworkDespawn() => Unbind();
    private void OnEnable() => Bind();
    private void OnDisable() => Unbind();

    private void Bind()
    {
        if (!IsSpawned || !IsClient || !IsOwner || !isActiveAndEnabled || _canvasObject != null)
            return;

        _input = GetComponent<PlayerInputReader>();
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        _pointerData = new PointerEventData(EventSystem.current);
        _canvasObject = new GameObject("ItemTooltipCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        _canvasObject.transform.SetParent(transform, false);
        _canvasRect = (RectTransform)_canvasObject.transform;
        Canvas canvas = _canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;
        CanvasScaler scaler = _canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _panel = Rect("ItemTooltip", _canvasRect, new Vector2(340f, 160f));
        _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
        Image background = _panel.gameObject.AddComponent<Image>();
        background.color = new Color(0.025f, 0.024f, 0.022f, 0.98f);
        background.raycastTarget = false;
        _border = _panel.gameObject.AddComponent<Outline>();
        _border.effectDistance = new Vector2(2f, -2f);
        _title = Label("Title", 18);
        _title.rectTransform.anchoredPosition = new Vector2(14f, -12f);
        _title.rectTransform.sizeDelta = new Vector2(312f, 28f);
        _title.resizeTextForBestFit = true;
        _title.resizeTextMinSize = 12;
        _title.resizeTextMaxSize = 18;
        _body = Label("Description", 14);
        _body.rectTransform.anchoredPosition = new Vector2(14f, -48f);
        _body.resizeTextForBestFit = true;
        _body.resizeTextMinSize = 10;
        _body.resizeTextMaxSize = 14;
        _ornament = ItemRarityBorder.Create(_panel);
        _panel.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_canvasObject == null)
            return;

        if (Pointer.current == null || Pointer.current.press.isPressed || (_input != null && _input.IsMenuOpen))
        {
            Hide();
            return;
        }

        Vector2 pointer = Pointer.current.position.ReadValue();
        Item item = GetHoveredItem(pointer);
        if (item == null)
        {
            Hide();
            return;
        }

        if (item != _shownItem || item.Rarity != _shownRarity)
        {
            _shownItem = item;
            _shownRarity = item.Rarity;
            _border.effectColor = ItemRarityColors.Get(item.Rarity);
            _ornament.SetRarity(item.Rarity);
            _title.text = item.DisplayName;
            string description = item.Description;
            string stats = item.StatsDescription;
            _body.text = string.IsNullOrEmpty(stats) ? description : description + "\n\n" + stats;
            _panel.gameObject.SetActive(true);
        }
        FitAndPosition(pointer);
    }

    private Item GetHoveredItem(Vector2 pointer)
    {
        if (EventSystem.current != null)
        {
            _pointerData.position = pointer;
            _uiHits.Clear();
            EventSystem.current.RaycastAll(_pointerData, _uiHits);
            foreach (RaycastResult hit in _uiHits)
            {
                if (!(hit.module is GraphicRaycaster))
                    continue;
                ItemSlotUI slot = hit.gameObject.GetComponentInParent<ItemSlotUI>();
                return slot != null ? slot.CurrentItem : null;
            }
        }

        if (_camera == null)
            _camera = Camera.main;
        if (_camera == null)
            return null;

        Ray ray = _camera.ScreenPointToRay(pointer);
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        if (!plane.Raycast(ray, out float distance))
            return null;

        _worldHits.Clear();
        Physics2D.OverlapPoint(ray.GetPoint(distance), ContactFilter2D.noFilter, _worldHits);
        foreach (Collider2D hit in _worldHits)
        {
            WorldItem worldItem = hit.GetComponentInParent<WorldItem>();
            if (worldItem != null && worldItem.CanBePicked)
                return worldItem.ItemPrefab;
        }
        return null;
    }

    private void FitAndPosition(Vector2 pointer)
    {
        Rect area = _canvasRect.rect;
        float width = Mathf.Min(340f, area.width - 32f);
        _title.rectTransform.sizeDelta = new Vector2(width - 28f, 28f);
        _body.rectTransform.sizeDelta = new Vector2(width - 28f, 500f);
        float bodyHeight = Mathf.Min(_body.preferredHeight, area.height - 94f);
        float height = bodyHeight + 62f;
        _body.rectTransform.sizeDelta = new Vector2(width - 28f, bodyHeight);
        _panel.sizeDelta = new Vector2(width, height);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, pointer, null, out Vector2 position);
        if (position.x + width + 16f > area.xMax)
            position.x -= width + 16f;
        else
            position.x += 16f;
        position.y -= 16f;
        if (position.y - height < area.yMin)
            position.y += height + 32f;
        position.x = Mathf.Clamp(position.x, area.xMin + 8f, area.xMax - width - 8f);
        position.y = Mathf.Clamp(position.y, area.yMin + height + 8f, area.yMax - 8f);
        _panel.anchoredPosition = position;
    }

    private void Hide()
    {
        _shownItem = null;
        _panel.gameObject.SetActive(false);
    }

    private void Unbind()
    {
        _shownItem = null;
        if (_canvasObject != null)
        {
            _canvasObject.SetActive(false);
            Destroy(_canvasObject);
            _canvasObject = null;
        }
    }

    private RectTransform Rect(string name, Transform parent, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        return rect;
    }

    private Text Label(string name, int fontSize)
    {
        Text label = Rect(name, _panel, Vector2.zero).gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.color = new Color(0.84f, 0.8f, 0.72f);
        label.alignment = TextAnchor.UpperLeft;
        label.supportRichText = false;
        label.raycastTarget = false;
        return label;
    }
}
