using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(NetworkActorVitals))]
public sealed class PlayerVitalsUI : NetworkBehaviour
{
    [SerializeField, Min(200f)] private float _width = 560f;
    [SerializeField, Min(0f)] private float _bottomMargin = 24f;
    [SerializeField] private Color _healthColor = new Color(0.19f, 0.34f, 0.23f, 1f);
    [SerializeField] private Color _sanityColor = new Color(0.72f, 0.32f, 0.075f, 1f);

    public float RightEdge => _width * 0.5f;
    public float BottomMargin => _bottomMargin;

    private NetworkActorVitals _vitals;
    private GameObject _canvasObject;
    private RectTransform _healthFill;
    private RectTransform _sanityFill;
    private Text _healthText;
    private Text _sanityText;
    private Font _font;

    private void Awake()
    {
        _vitals = GetComponent<NetworkActorVitals>();
    }

    private void OnEnable() => Show();
    private void OnDisable() => Hide();
    public override void OnNetworkSpawn() => Show();
    public override void OnNetworkDespawn() => Hide();
    public override void OnGainedOwnership() => Show();
    public override void OnLostOwnership() => Hide();

    private void Show()
    {
        if (!isActiveAndEnabled || !IsSpawned || !IsClient || !IsOwner || _canvasObject != null)
            return;

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _canvasObject = new GameObject("VitalsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        _canvasObject.transform.SetParent(transform, false);
        Canvas canvas = _canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = _canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform panel = CreateRect("VitalsPanel", _canvasObject.transform);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, _bottomMargin);
        panel.sizeDelta = new Vector2(_width, 68f);
        AddImage(panel, new Color(0.025f, 0.024f, 0.022f, 1f));
        Outline border = panel.gameObject.AddComponent<Outline>();
        border.effectColor = new Color(0.16f, 0.145f, 0.12f, 1f);
        border.effectDistance = new Vector2(1f, -1f);

        CreateBar(panel, "Health", "Здоровье", -8f, 26f, _healthColor, out _healthFill, out _healthText);
        CreateBar(panel, "Sanity", "Рассудок", -42f, 18f, _sanityColor, out _sanityFill, out _sanityText);
        _vitals.Changed += UpdateVitals;
        UpdateVitals(_vitals.State);
    }

    private void Hide()
    {
        if (_vitals != null)
            _vitals.Changed -= UpdateVitals;
        if (_canvasObject == null)
            return;

        _canvasObject.SetActive(false);
        Destroy(_canvasObject);
        _canvasObject = null;
    }

    private void UpdateVitals(Vector4 state)
    {
        UpdateBar(_healthFill, _healthText, state.x, state.y);
        UpdateBar(_sanityFill, _sanityText, state.z, state.w);
    }

    private void UpdateBar(RectTransform fill, Text value, float current, float maximum)
    {
        float fraction = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
        fill.anchorMax = new Vector2(fraction, 1f);
        value.text = $"{current:0.#} / {maximum:0.#}";
    }

    private void CreateBar(RectTransform panel, string objectName, string title, float top, float height, Color color, out RectTransform fill, out Text value)
    {
        RectTransform track = CreateRect(objectName, panel);
        track.anchorMin = new Vector2(0f, 1f);
        track.anchorMax = Vector2.one;
        track.pivot = new Vector2(0.5f, 1f);
        track.anchoredPosition = new Vector2(0f, top);
        track.sizeDelta = new Vector2(-16f, height);
        AddImage(track, new Color(0.045f, 0.04f, 0.035f, 1f));

        fill = CreateRect("Fill", track);
        Stretch(fill);
        AddImage(fill, color);
        Text label = CreateText(track, "Title", title, TextAnchor.MiddleLeft, 11);
        label.rectTransform.offsetMin = new Vector2(10f, 0f);
        label.rectTransform.offsetMax = new Vector2(-10f, 0f);
        value = CreateText(track, "Value", "", TextAnchor.MiddleCenter, 12);
    }

    private RectTransform CreateRect(string objectName, Transform parent)
    {
        RectTransform rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void AddImage(RectTransform rect, Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private Text CreateText(Transform parent, string objectName, string content, TextAnchor alignment, int fontSize)
    {
        RectTransform rect = CreateRect(objectName, parent);
        Stretch(rect);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = _font;
        text.fontSize = fontSize;
        text.text = content;
        text.alignment = alignment;
        text.color = new Color(0.87f, 0.83f, 0.74f, 1f);
        text.raycastTarget = false;
        Shadow shadow = rect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        shadow.effectDistance = new Vector2(1f, -1f);
        return text;
    }
}
