using UnityEngine;
using UnityEngine.UI;

public sealed class HoverTooltipUI : MonoBehaviour
{
    private RectTransform _area;
    private RectTransform _panel;
    private Text _title;
    private Text _body;
    private Outline _border;
    private ItemRarityBorder _ornament;
    private object _target;
    private float _showAt;

    public static HoverTooltipUI Create(Transform parent, string canvasName, string panelName)
    {
        GameObject root = new GameObject(canvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.transform.SetParent(parent, false);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        HoverTooltipUI view = root.AddComponent<HoverTooltipUI>();
        view._area = (RectTransform)root.transform;
        view._panel = Rect(panelName, view._area, new Vector2(340f, 160f));
        view._panel.anchorMin = view._panel.anchorMax = new Vector2(0.5f, 0.5f);
        Image background = view._panel.gameObject.AddComponent<Image>();
        background.color = new Color(0.025f, 0.024f, 0.022f, 0.98f);
        background.raycastTarget = false;
        view._border = view._panel.gameObject.AddComponent<Outline>();
        view._border.effectColor = new Color(0.4f, 0.32f, 0.19f);
        view._border.effectDistance = new Vector2(2f, -2f);
        view._title = view.Label("Title", 18, new Vector2(14f, -12f));
        view._body = view.Label("Description", 14, new Vector2(14f, -48f));
        view._ornament = ItemRarityBorder.Create(view._panel);
        view._ornament.enabled = false;
        view._panel.gameObject.SetActive(false);
        return view;
    }

    public void Show(object target, string title, string description, Vector2 pointer)
    {
        Show(target, title, description, pointer, 0f);
    }

    public void Show(object target, string title, string description, Vector2 pointer, float delay)
    {
        if (!Equals(_target, target))
        {
            _target = target;
            _showAt = Time.unscaledTime + Mathf.Max(0f, delay);
            _panel.gameObject.SetActive(false);
        }
        if (Time.unscaledTime < _showAt)
            return;
        _title.text = title;
        _body.text = description;
        _panel.gameObject.SetActive(true);
        Move(pointer);
    }

    public void SetRarity(EItemRarity rarity)
    {
        _border.effectColor = ItemRarityColors.Get(rarity);
        _ornament.SetRarity(rarity);
    }

    public void SetBorderColor(Color color)
    {
        _border.effectColor = color;
        _ornament.enabled = false;
    }

    public void Move(Vector2 pointer)
    {
        if (!_panel.gameObject.activeSelf)
            return;
        Rect area = _area.rect;
        float width = Mathf.Max(60f, Mathf.Min(340f, area.width - 32f));
        _title.rectTransform.sizeDelta = new Vector2(width - 28f, 28f);
        _body.rectTransform.sizeDelta = new Vector2(width - 28f, 600f);
        float bodyHeight = Mathf.Clamp(_body.preferredHeight, 0f, Mathf.Max(0f, area.height - 94f));
        float height = bodyHeight + 62f;
        _body.rectTransform.sizeDelta = new Vector2(width - 28f, bodyHeight);
        _panel.sizeDelta = new Vector2(width, height);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, pointer, null, out Vector2 position);
        position.x += position.x + width + 16f > area.xMax ? -width - 16f : 16f;
        position.y -= 16f;
        if (position.y - height < area.yMin)
            position.y += height + 32f;
        position.x = Mathf.Clamp(position.x, area.xMin + 8f, area.xMax - width - 8f);
        position.y = Mathf.Clamp(position.y, area.yMin + height + 8f, area.yMax - 8f);
        _panel.anchoredPosition = position;
    }

    public void Hide()
    {
        _target = null;
        _panel.gameObject.SetActive(false);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        return rect;
    }

    private Text Label(string name, int size, Vector2 position)
    {
        Text label = Rect(name, _panel, Vector2.zero).gameObject.AddComponent<Text>();
        label.rectTransform.anchoredPosition = position;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = size;
        label.color = new Color(0.87f, 0.83f, 0.74f);
        label.alignment = TextAnchor.UpperLeft;
        label.supportRichText = false;
        label.raycastTarget = false;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = size - 4;
        label.resizeTextMaxSize = size;
        return label;
    }
}
