using UnityEngine;
using UnityEngine.UI;

public sealed class HoverTooltipUI : MonoBehaviour
{
    private sealed class EnchantView
    {
        public RectTransform Panel;
        public Text Title;
        public Text Body;
    }

    private readonly EnchantView[] _enchants = new EnchantView[3];
    private int _enchantCount;
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

    public void ClearEnchantments()
    {
        _enchantCount = 0;
        foreach (EnchantView view in _enchants)
            if (view != null)
                view.Panel.gameObject.SetActive(false);
    }

    public void SetEnchantments(EnchantState state, EItemRarity rarity, EnchantDatabase database, ActorStats owner)
    {
        if (database == null)
        {
            ClearEnchantments();
            return;
        }
        _enchantCount = 0;
        for (int i = 0; i < 3; i++)
        {
            int id = state.GetId(i);
            if (id == 0 || (i > 0 && state.First == id) || (i > 1 && state.Second == id))
                continue;
            EnchantData data = database.GetEnchant(id);
            if (data == null)
                continue;
            int stacks = 0;
            for (int j = 0; j < 3; j++)
                if (state.GetId(j) == id)
                    stacks++;
            EnchantView view = _enchants[_enchantCount];
            if (view == null)
            {
                RectTransform panel = Rect("Enchant_" + _enchantCount, _panel, Vector2.zero);
                Image background = panel.gameObject.AddComponent<Image>();
                background.color = new Color(0.045f, 0.038f, 0.028f, 0.98f);
                background.raycastTarget = false;
                Outline border = panel.gameObject.AddComponent<Outline>();
                border.effectDistance = new Vector2(1f, -1f);
                view = new EnchantView
                {
                    Panel = panel,
                    Title = Label("Title", 16, new Vector2(14f, -12f), panel),
                    Body = Label("Description", 14, new Vector2(14f, -48f), panel)
                };
                _enchants[_enchantCount] = view;
            }
            Color color = ItemRarityColors.Get(rarity);
            view.Panel.GetComponent<Outline>().effectColor = color;
            view.Title.color = color;
            view.Title.text = "ЧАРЫ · " + data.DisplayName + (stacks > 1 ? $" ×{stacks}" : "");
            view.Body.text = data.Description.Replace("{value}", data.GetTooltipValue(rarity, stacks, owner).ToString("0.##"));
            view.Panel.gameObject.SetActive(true);
            _enchantCount++;
        }
        for (int i = _enchantCount; i < _enchants.Length; i++)
            if (_enchants[i] != null)
                _enchants[i].Panel.gameObject.SetActive(false);
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
        float bodyHeight = _body.preferredHeight;
        float textHeight = bodyHeight;
        for (int i = 0; i < _enchantCount; i++)
        {
            EnchantView view = _enchants[i];
            view.Title.rectTransform.sizeDelta = new Vector2(width - 28f, 28f);
            view.Body.rectTransform.sizeDelta = new Vector2(width - 28f, 600f);
            textHeight += view.Body.preferredHeight;
        }
        float availableText = Mathf.Max(0f, area.height - 16f - 62f * (1 + _enchantCount));
        float scale = textHeight > 0f ? Mathf.Min(1f, availableText / textHeight) : 1f;
        bodyHeight *= scale;
        float height = bodyHeight + 62f;
        _body.rectTransform.sizeDelta = new Vector2(width - 28f, bodyHeight);
        _panel.sizeDelta = new Vector2(width, height);
        for (int i = 0; i < _enchantCount; i++)
        {
            EnchantView view = _enchants[i];
            float descriptionHeight = view.Body.preferredHeight * scale;
            view.Body.rectTransform.sizeDelta = new Vector2(width - 28f, descriptionHeight);
            view.Panel.anchoredPosition = new Vector2(0f, -height);
            view.Panel.sizeDelta = new Vector2(width, descriptionHeight + 62f);
            height += view.Panel.sizeDelta.y;
        }
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

    private Text Label(string name, int size, Vector2 position, Transform parent = null)
    {
        Text label = Rect(name, parent != null ? parent : _panel, Vector2.zero).gameObject.AddComponent<Text>();
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
