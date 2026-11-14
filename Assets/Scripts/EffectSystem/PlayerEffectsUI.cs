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
        public RectTransform ElapsedFill;
        public Text StackLabel;
        public int Count;
        public bool IsPresent;
    }

    private readonly List<EffectView> _views = new();
    private NetworkEffectHandler _effects;
    private GameObject _canvasObject;
    private RectTransform _canvasRect;
    private RectTransform _viewport;
    private RectTransform _row;
    private HoverTooltipUI _tooltip;
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
        _viewport.anchoredPosition = new Vector2(-24f, -24f);
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

        _tooltip = HoverTooltipUI.Create(transform, "EffectTooltipCanvas", "EffectTooltip");
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
            if (view.IsPresent && !view.Data.IsPermanent)
            {
                float remaining = Mathf.Max(0f, (float)(view.EndsAt - NetworkManager.ServerTime.Time));
                float fraction = Mathf.Clamp01(remaining / Mathf.Max(0.01f, view.Data.EffectDuration));
                view.ElapsedFill.anchorMin = new Vector2(0f, fraction);
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
        if (_hovered != null && _hovered.IsPresent && IsUnderPointer(_hovered, pointer))
        {
            ShowTooltip(_hovered, pointer);
            return;
        }

        HideTooltip();
        for (int i = 0; i < _views.Count; i++)
        {
            if (_views[i].IsPresent && IsUnderPointer(_views[i], pointer))
            {
                ShowTooltip(_views[i], pointer);
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
        {
            view.IsPresent = false;
            view.Count = 0;
        }

        for (int i = 0; i < _effects.Count; i++)
        {
            NetworkEffectSlot slot = _effects.GetEffect(i);
            EffectData data = _effects.GetData(slot.EffectIndex);
            if (data == null)
                continue;

            EffectView view = _views.Find(candidate => data.Stackable
                ? candidate.Data == data
                : candidate.InstanceId == slot.InstanceId);
            if (view == null)
            {
                view = CreateView(slot.InstanceId, data);
                _views.Add(view);
            }
            view.EndsAt = view.IsPresent ? System.Math.Max(view.EndsAt, slot.EndsAt) : slot.EndsAt;
            view.IsPresent = true;
            view.Count++;
        }

        for (int i = _views.Count - 1; i >= 0; i--)
        {
            EffectView view = _views[i];
            if (view.IsPresent)
            {
                view.Rect.gameObject.SetActive(true);
                view.StackLabel.text = view.Count.ToString();
                continue;
            }
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
        RectTransform rect = Rect("Effect_" + instanceId, _row, new Vector2(48f, 48f));
        Background(rect, HudStyle.Surface, true);
        Outline border = rect.gameObject.AddComponent<Outline>();
        border.effectColor = GetBorderColor(data);
        border.effectDistance = new Vector2(2f, -2f);
        RectTransform iconRect = Rect("Icon", rect, new Vector2(40f, 40f));
        iconRect.anchoredPosition = new Vector2(-4f, -4f);
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
        RectTransform elapsedFill = Rect("Elapsed", iconRect, Vector2.zero);
        elapsedFill.anchorMin = new Vector2(0f, 1f);
        elapsedFill.anchorMax = Vector2.one;
        Background(elapsedFill, new Color(0f, 0f, 0f, 0.65f), false);
        Text stackLabel = Label("Stacks", rect, 16, new Vector2(38f, 38f));
        stackLabel.rectTransform.anchoredPosition = new Vector2(-4f, -5f);
        stackLabel.alignment = TextAnchor.LowerRight;
        stackLabel.fontStyle = FontStyle.Bold;
        Outline stackOutline = stackLabel.gameObject.AddComponent<Outline>();
        stackOutline.effectColor = Color.black;
        return new EffectView { InstanceId = instanceId, Data = data, Rect = rect, ElapsedFill = elapsedFill, StackLabel = stackLabel };
    }

    private void ShowTooltip(EffectView view, Vector2 pointer)
    {
        _hovered = view;
        _tooltip.SetBorderColor(GetBorderColor(view.Data));

        string description = view.Data.FormatText(view.Data.EffectDescription);
        string stats = view.Data.FormatText(view.Data.EffectStats);
        string body = string.IsNullOrEmpty(stats) ? description : description + "\n\n" + stats;
        if (!view.Data.IsPermanent)
        {
            float remaining = Mathf.Max(0f, (float)(view.EndsAt - NetworkManager.ServerTime.Time));
            body += $"\n\nОсталось {Mathf.CeilToInt(remaining)} с";
        }
        _tooltip.Show(view, view.Data.EffectName, body, pointer);
    }

    private Color GetBorderColor(EffectData data)
    {
        return data.IsBuff ? new Color(0.2f, 0.65f, 0.28f) : new Color(0.8f, 0.18f, 0.16f);
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
        _tooltip.Hide();
    }

    private void Unbind()
    {
        if (_effects != null)
            _effects.EffectsChanged -= MarkDirty;
        _views.Clear();
        _hovered = null;
        if (_tooltip != null)
        {
            _tooltip.gameObject.SetActive(false);
            Destroy(_tooltip.gameObject);
            _tooltip = null;
        }
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
        label.color = HudStyle.Text;
        label.alignment = TextAnchor.UpperLeft;
        label.supportRichText = false;
        label.raycastTarget = false;
        return label;
    }
}
