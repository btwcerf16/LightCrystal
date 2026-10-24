using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class ItemSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    private Func<Item> _getItem;
    private Item _shownItem;
    private Image _icon;
    private Text _name;
    private Component _container;
    private int _index;
    private Action<int, int> _swap;
    private Action<int> _drop;
    private Action<int> _select;
    private Image _dragIcon;
    private GameObject _cooldownPanel;
    private RectTransform _cooldownFill;
    private Text _cooldownText;
    public Item CurrentItem => _getItem?.Invoke();

    public void Bind(Func<Item> getItem, Component container, int index, Action<int, int> swap, Action<int> drop, Action<int> select = null)
    {
        _getItem = getItem;
        _container = container;
        _index = index;
        _swap = swap;
        _drop = drop;
        _select = select;
        RectTransform icon = Child("Icon", new Vector2(0f, 0.26f), new Vector2(1f, 0.92f));
        _icon = icon.gameObject.AddComponent<Image>();
        _icon.preserveAspect = true;
        _icon.raycastTarget = false;
        RectTransform nameRect = Child("ItemName", Vector2.zero, new Vector2(1f, 0.26f));
        _name = nameRect.gameObject.AddComponent<Text>();
        _name.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _name.fontSize = 9;
        _name.alignment = TextAnchor.MiddleCenter;
        _name.color = new Color(0.84f, 0.8f, 0.72f);
        _name.raycastTarget = false;

        RectTransform cooldown = Child("Cooldown", new Vector2(0f, 0.26f), new Vector2(1f, 0.92f));
        _cooldownPanel = cooldown.gameObject;
        Image shade = new GameObject("Shade", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        shade.transform.SetParent(cooldown, false);
        shade.color = new Color(0f, 0f, 0f, 0.75f);
        shade.raycastTarget = false;
        _cooldownFill = shade.rectTransform;
        _cooldownFill.anchorMin = Vector2.zero;
        _cooldownFill.anchorMax = Vector2.one;
        _cooldownFill.offsetMin = _cooldownFill.offsetMax = Vector2.zero;

        RectTransform timer = new GameObject("Time", typeof(RectTransform)).GetComponent<RectTransform>();
        timer.SetParent(cooldown, false);
        timer.anchorMin = Vector2.zero;
        timer.anchorMax = Vector2.one;
        timer.offsetMin = timer.offsetMax = Vector2.zero;
        _cooldownText = timer.gameObject.AddComponent<Text>();
        _cooldownText.font = _name.font;
        _cooldownText.fontSize = 18;
        _cooldownText.alignment = TextAnchor.MiddleCenter;
        _cooldownText.color = new Color(0.95f, 0.91f, 0.82f);
        _cooldownText.raycastTarget = false;
        Refresh(_getItem());
        RefreshCooldown(_getItem());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && !eventData.dragging)
            _select?.Invoke(_index);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || _getItem() == null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        _dragIcon = new GameObject("DraggedItem", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        _dragIcon.transform.SetParent(canvas.transform, false);
        _dragIcon.rectTransform.sizeDelta = ((RectTransform)transform).rect.size;
        _dragIcon.sprite = _getItem().Icon;
        _dragIcon.preserveAspect = true;
        _dragIcon.raycastTarget = false;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragIcon != null)
            _dragIcon.transform.position = eventData.position;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;
        ItemSlotUI source = eventData.pointerDrag.GetComponent<ItemSlotUI>();
        if (source != null && source._dragIcon != null && source._container == _container && source._index != _index)
            _swap(source._index, _index);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragIcon == null)
            return;
        Destroy(_dragIcon.gameObject);
        _dragIcon = null;
        if (eventData.pointerCurrentRaycast.gameObject == null)
            _drop(_index);
    }

    private void OnDisable()
    {
        if (_dragIcon != null)
            Destroy(_dragIcon.gameObject);
    }

    private void LateUpdate()
    {
        Item item = _getItem?.Invoke();
        if (item != _shownItem)
            Refresh(item);
        RefreshCooldown(item);
    }

    private void RefreshCooldown(Item item)
    {
        if (_cooldownPanel == null)
            return;

        float remaining = item != null ? item.CooldownRemaining : 0f;
        bool isOnCooldown = remaining > 0f;
        if (_cooldownPanel.activeSelf != isOnCooldown)
            _cooldownPanel.SetActive(isOnCooldown);
        if (!isOnCooldown)
            return;

        float progress = item.CooldownDuration > 0f ? Mathf.Clamp01(remaining / item.CooldownDuration) : 0f;
        _cooldownFill.anchorMax = new Vector2(1f, progress);
        _cooldownText.text = remaining >= 1f ? Mathf.CeilToInt(remaining).ToString() : remaining.ToString("0.0");
    }

    private void Refresh(Item item)
    {
        _shownItem = item;
        _icon.sprite = item != null ? item.Icon : null;
        _icon.enabled = _icon.sprite != null;
        _name.text = item != null ? item.DisplayName : "";
    }

    private RectTransform Child(string name, Vector2 min, Vector2 max)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = new Vector2(3f, 0f);
        rect.offsetMax = new Vector2(-3f, 0f);
        return rect;
    }
}
