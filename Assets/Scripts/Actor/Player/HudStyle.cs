using UnityEngine;
using UnityEngine.UI;

public static class HudStyle
{
    public static readonly Color Surface = new Color32(22, 24, 25, 255);
    public static readonly Color Slot = new Color32(33, 36, 37, 255);
    public static readonly Color Line = new Color32(66, 71, 72, 255);
    public static readonly Color Text = new Color32(239, 233, 222, 255);
    public static readonly Color Muted = new Color32(165, 166, 158, 255);

    public static Text Binding(Transform parent, string value)
    {
        Image badge = new GameObject("BindingBadge", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        badge.transform.SetParent(parent, false);
        badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = badge.rectTransform.pivot = new Vector2(0f, 1f);
        badge.rectTransform.anchoredPosition = new Vector2(4f, -4f);
        badge.color = Surface;
        badge.raycastTarget = false;
        Text label = new GameObject("Binding", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        label.transform.SetParent(badge.transform, false);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 12;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Text;
        label.raycastTarget = false;
        SetBinding(label, value);
        return label;
    }

    public static void SetBinding(Text label, string value)
    {
        label.text = value;
        ((RectTransform)label.transform.parent).sizeDelta = new Vector2(Mathf.Clamp(label.preferredWidth + 8f, 24f, 56f), 20f);
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 10;
        label.resizeTextMaxSize = 12;
    }
}
