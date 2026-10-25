using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[RequireComponent(typeof(NetworkEffectHandler))]
public sealed class PlayerEffectsUI : NetworkBehaviour
{
    private class EffectView
    {
        public int InstanceId;
        public EffectData Data;
        public double EndsAt;
        public RectTransform Rect;
        public Text Timer;
        public bool IsPresent;
    }

    private readonly List<EffectView> _views = new();
    private NetworkEffectHandler _effects;
    private GameObject _canvasObject;
    private RectTransform _canvasRect;
    private RectTransform _viewport;
    private RectTransform _row;
    private RectTransform _tooltip;
    private Outline _tooltipBorder;
    private Text _title;
    private Text _description;
    private Font _font;
    private EffectView _hovered;
    private bool _isDirty;

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

        _effects = GetComponent<NetworkEffectHandler>();
        _effects.EffectsChanged += MarkDirty;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        _canvasObject = new GameObject("EffectsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvasObject.transform.SetParent(transform, false);
        _canvasRect = (RectTransform)_canvasObject.transform;
        Canvas canvas = _canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        CanvasScaler scaler = _canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _viewport = Rect("Effects", _canvasRect, new Vector2(600f, 58f));
        _viewport.anchoredPosition = new Vector2(-16f, -16f);
        Background(_viewport, Color.clear, true);
        _viewport.gameObject.AddComponent<RectMask2D>();
        _row = Rect("Row", _viewport, _viewport.sizeDelta);
        ScrollRect scroll = _viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = _viewport;
        scroll.content = _row;
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 32f;

        _tooltip = Rect("EffectTooltip", _canvasRect, new Vector2(320f, 140f));
        _tooltip.anchoredPosition = new Vector2(-16f, -86f);
        Background(_tooltip, new Color(0.025f, 0.024f, 0.022f, 0.98f), false);
        _tooltipBorder = _tooltip.gameObject.AddComponent<Outline>();
        _tooltipBorder.effectDistance = new Vector2(2f, -2f);
        _title = Label("Title", _tooltip, 18, new Vector2(292f, 26f));
        _title.rectTransform.anchoredPosition = new Vector2(-14f, -12f);
        _title.resizeTextForBestFit = true;
        _title.resizeTextMinSize = 12;
        _title.resizeTextMaxSize = 18;
        _description = Label("Description", _tooltip, 14, new Vector2(292f, 90f));
        _description.rectTransform.anchoredPosition = new Vector2(-14f, -48f);
        _description.resizeTextForBestFit = true;
        _description.resizeTextMinSize = 10;
        _description.resizeTextMaxSize = 14;
        _tooltip.gameObject.SetActive(false);
        _isDirty = true;
    }

    private void MarkDirty() => _isDirty = true;

    private void LateUpdate()
    {
        if (_canvasObject == null)
            return;

        if (_isDirty)
        {
            RefreshEffects();
            _isDirty = false;
        }

        float width = Mathf.Min(600f, _canvasRect.rect.width - 32f);
        _viewport.sizeDelta = new Vector2(width, 58f);
        _row.sizeDelta = new Vector2(Mathf.Max(width, _views.Count * 56f), 58f);
        for (int i = 0; i < _views.Count; i++)
        {
            EffectView view = _views[i];
            view.Rect.anchoredPosition = new Vector2(-i * 56f, 0f);
            if (view.IsPresent)
            {
                float remaining = Mathf.Max(0f, (float)(view.EndsAt - NetworkManager.ServerTime.Time));
                view.Timer.text = Mathf.CeilToInt(remaining).ToString();
            }
        }
        _viewport.gameObject.SetActive(_views.Count > 0);

        PlayerInputReader input = GetComponent<PlayerInputReader>();
        if (Pointer.current == null || (input != null && input.IsMenuOpen))
        {
            HideTooltip();
            return;
        }

        Vector2 pointer = Pointer.current.position.ReadValue();
        if (_hovered != null && IsUnderPointer(_hovered, pointer))
        {
            FitTooltip();
            return;
        }

        HideTooltip();
        for (int i = 0; i < _views.Count; i++)
        {
            if (_views[i].IsPresent && IsUnderPointer(_views[i], pointer))
            {
                ShowTooltip(_views[i]);
                break;
            }
        }
    }

    private bool IsUnderPointer(EffectView view, Vector2 pointer)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(_viewport, pointer)
            && RectTransformUtility.RectangleContainsScreenPoint(view.Rect, pointer);
    }

    private void RefreshEffects()
    {
        foreach (EffectView view in _views)
            view.IsPresent = false;

        for (int i = 0; i < _effects.Count; i++)
        {
            NetworkEffectSlot slot = _effects.GetEffect(i);
            EffectData data = _effects.GetData(slot.EffectIndex);
            if (data == null)
                continue;

            EffectView view = _views.Find(candidate => candidate.InstanceId == slot.InstanceId);
            if (view == null)
            {
                view = CreateView(slot.InstanceId, data);
                _views.Add(view);
            }
            view.IsPresent = true;
            view.EndsAt = slot.EndsAt;
        }

        for (int i = _views.Count - 1; i >= 0; i--)
        {
            EffectView view = _views[i];
            if (view.IsPresent)
                continue;
            if (_hovered != null && !_hovered.IsPresent)
            {
                view.Rect.gameObject.SetActive(false);
                continue;
            }
            Destroy(view.Rect.gameObject);
            _views.RemoveAt(i);
        }
    }

    private EffectView CreateView(int instanceId, EffectData data)
    {
        RectTransform rect = Rect("Effect_" + instanceId, _row, new Vector2(48f, 56f));
        Background(rect, new Color(0.04f, 0.038f, 0.032f, 0.96f), true);
        Outline border = rect.gameObject.AddComponent<Outline>();
        border.effectColor = GetBorderColor(data);
        border.effectDistance = new Vector2(2f, -2f);
        RectTransform iconRect = Rect("Icon", rect, new Vector2(40f, 38f));
        iconRect.anchoredPosition = new Vector2(-4f, -3f);
        if (data.SpriteIcon != null)
        {
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = data.SpriteIcon;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }
        else
        {
            Text letter = Label("Letter", iconRect, 24, iconRect.sizeDelta);
            letter.text = string.IsNullOrEmpty(data.EffectName) ? "?" : data.EffectName.Substring(0, 1);
            letter.alignment = TextAnchor.MiddleCenter;
            letter.color = data.EffectColor;
        }
        Text timer = Label("Time", rect, 12, new Vector2(48f, 16f));
        timer.rectTransform.anchoredPosition = new Vector2(0f, -40f);
        timer.alignment = TextAnchor.MiddleCenter;
        return new EffectView { InstanceId = instanceId, Data = data, Rect = rect, Timer = timer };
    }

    private void ShowTooltip(EffectView view)
    {
        _hovered = view;
        _tooltipBorder.effectColor = GetBorderColor(view.Data);
        _title.text = view.Data.EffectName;
        string description = view.Data.FormatText(view.Data.EffectDescription);
        string stats = view.Data.FormatText(view.Data.EffectStats);
        _description.text = string.IsNullOrEmpty(stats) ? description : description + "\n\n" + stats;
        _tooltip.gameObject.SetActive(true);
        FitTooltip();
    }

    private Color GetBorderColor(EffectData data)
    {
        return data.IsBuff ? new Color(0.2f, 0.65f, 0.28f) : new Color(0.8f, 0.18f, 0.16f);
    }

    private void FitTooltip()
    {
        float width = Mathf.Min(320f, _canvasRect.rect.width - 32f);
        _title.rectTransform.sizeDelta = new Vector2(width - 28f, 26f);
        _description.rectTransform.sizeDelta = new Vector2(width - 28f, 500f);
        float bodyHeight = Mathf.Min(_description.preferredHeight, _canvasRect.rect.height - 164f);
        _description.rectTransform.sizeDelta = new Vector2(width - 28f, bodyHeight);
        _tooltip.sizeDelta = new Vector2(width, bodyHeight + 62f);
    }

    private void HideTooltip()
    {
        if (_hovered != null && !_hovered.IsPresent)
        {
            Destroy(_hovered.Rect.gameObject);
            _views.Remove(_hovered);
            _isDirty = true;
        }
        _hovered = null;
        _tooltip.gameObject.SetActive(false);
    }

    private void Unbind()
    {
        if (_effects != null)
            _effects.EffectsChanged -= MarkDirty;
        _views.Clear();
        _hovered = null;
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
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.sizeDelta = size;
        return rect;
    }

    private void Background(RectTransform rect, Color color, bool raycast)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
    }

    private Text Label(string name, Transform parent, int fontSize, Vector2 size)
    {
        Text label = Rect(name, parent, size).gameObject.AddComponent<Text>();
        label.font = _font;
        label.fontSize = fontSize;
        label.color = new Color(0.84f, 0.8f, 0.72f);
        label.alignment = TextAnchor.UpperLeft;
        label.supportRichText = false;
        label.raycastTarget = false;
        return label;
    }
}
