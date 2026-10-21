using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class ItemSlotUI : MonoBehaviour
{
    private Func<Item> _getItem;
    private Item _shownItem;
    private Image _icon;
    private Text _name;

    public void Bind(Func<Item> getItem)
    {
        _getItem = getItem;
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
        Refresh(_getItem());
    }

    private void LateUpdate()
    {
        Item item = _getItem?.Invoke();
        if (item != _shownItem)
            Refresh(item);
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
