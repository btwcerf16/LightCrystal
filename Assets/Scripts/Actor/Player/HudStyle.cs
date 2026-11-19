using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public static class HudStyle
{
    private static Font _pixelFont;
    private static Sprite _silenceMask;
    private static Sprite _stunAnvil;
    public static Font PixelFont => _pixelFont ??= Resources.Load<Font>("Tiny5-Regular");

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
        label.font = PixelFont;
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

    public static UnityEngine.UI.Text CreateActionLock(Transform parent, float iconSize = 24f)
    {
        var shade = new GameObject("ActionLock", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        shade.transform.SetParent(parent, false);
        shade.rectTransform.anchorMin = Vector2.zero;
        shade.rectTransform.anchorMax = Vector2.one;
        shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
        shade.color = new Color(0.08f, 0.03f, 0.05f, 0.78f);
        shade.raycastTarget = false;
        var mask = new GameObject("Mask", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        mask.transform.SetParent(shade.transform, false);
        mask.rectTransform.sizeDelta = Vector2.one * iconSize;
        mask.rectTransform.anchoredPosition = new Vector2(0f, 5f);
        mask.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        mask.sprite = _silenceMask ??= Resources.Load<Sprite>("SilenceMask");
        mask.preserveAspect = true;
        mask.raycastTarget = false;
        DOVirtual.Float(0f, 1f, 0.5f, progress =>
            {
                bool secondFrame = progress >= 0.5f;
                bool stunned = mask.sprite == _stunAnvil;
                mask.rectTransform.localRotation = Quaternion.Euler(0f, 0f, stunned ? 0f : (secondFrame ? -45f : 45f));
                mask.rectTransform.anchoredPosition = new Vector2(0f, stunned ? (secondFrame ? 1f : 11f) : 5f);
            })
            .SetEase(Ease.Linear)
            .SetLoops(-1)
            .SetUpdate(true)
            .SetLink(shade.gameObject, LinkBehaviour.PauseOnDisableRestartOnEnable);
        var label = new GameObject("Reason", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>();
        label.transform.SetParent(shade.transform, false);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.right;
        label.rectTransform.pivot = new Vector2(0.5f, 0f);
        label.rectTransform.sizeDelta = new Vector2(-2f, 18f);
        label.rectTransform.anchoredPosition = new Vector2(0f, 3f);
        label.font = PixelFont;
        label.fontSize = iconSize > 24f ? 16 : 11;
        label.color = Text;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        shade.gameObject.SetActive(false);
        return label;
    }

    public static void SetActionLock(UnityEngine.UI.Text label, bool stunned, bool silenced = false)
    {
        GameObject overlay = label.transform.parent.gameObject;
        bool blocked = stunned || silenced;
        string reason = stunned ? "СТАН" : "Безмолвие";
        if (overlay.activeSelf == blocked && label.text == reason)
            return;
        label.text = reason;
        var icon = overlay.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>();
        icon.sprite = stunned
            ? (_stunAnvil ??= Resources.Load<Sprite>("StunAnvil"))
            : (_silenceMask ??= Resources.Load<Sprite>("SilenceMask"));
        icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, stunned ? 0f : 45f);
        icon.rectTransform.anchoredPosition = new Vector2(0f, stunned ? 11f : 5f);
        overlay.SetActive(blocked);
    }
}
