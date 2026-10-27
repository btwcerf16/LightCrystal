using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[RequireComponent(typeof(SpellHolder), typeof(NetworkActorVitals), typeof(PlayerSlotBindings))]
public sealed class SpellUI : NetworkBehaviour
{
    private sealed class SlotView
    {
        public Button Button;
        public Text Binding;
        public Text Cost;
        public Text Timer;
        public GameObject Cooldown;
    }

    private readonly List<SlotView> _slots = new();
    private SpellHolder _holder;
    private PlayerSlotBindings _bindings;
    private PlayerInputReader _input;
    private NetworkActorVitals _vitals;
    private GameObject _canvas;
    private RectTransform _panel;
    private Font _font;
    private GameObject _tooltipCanvas;
    private RectTransform _tooltipArea;
    private RectTransform _tooltip;
    private Text _tooltipTitle;
    private Text _tooltipBody;
    private int _hoveredIndex = -1;

    private void Awake()
    {
        _holder = GetComponent<SpellHolder>();
        _bindings = GetComponent<PlayerSlotBindings>();
        _input = GetComponent<PlayerInputReader>();
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
        if (!IsSpawned || !IsClient || !IsOwner || !isActiveAndEnabled || _canvas != null)
            return;
        CreatePanel();
        _bindings.BindingsChanged += RefreshBindings;
    }

    private void CreatePanel()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        _panel = GetComponent<PlayerHUD>().SpellPanel;
        _canvas = _panel.gameObject;
        Image(_panel, new Color(0.025f, 0.024f, 0.022f), true);
        CreateTooltip();
        BuildSlots();
    }

    private void BuildSlots()
    {
        foreach (Transform child in _panel)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        _slots.Clear();
        HideTooltip();
        float width = _panel.rect.width / Mathf.Max(1, _holder.Spells.Count);
        for (int i = 0; i < _holder.Spells.Count; i++)
        {
            int index = i;
            SpellConfig config = _holder.GetConfig(i);
            RectTransform card = Rect("Spell_" + i, _panel, new Vector2(i * width, 0f), new Vector2(width, _panel.rect.height));
            Image background = Image(card, new Color(0.035f, 0.032f, 0.028f), true);
            Outline border = card.gameObject.AddComponent<Outline>();
            border.effectColor = new Color(0.32f, 0.26f, 0.16f);
            border.effectDistance = new Vector2(1f, -1f);
            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            if (config != null && config.IsPassive)
                button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => _bindings.UseSpell(index));
            EventTrigger hover = card.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(data => ShowTooltip(index));
            hover.triggers.Add(enter);
            EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(data => HideTooltip());
            hover.triggers.Add(exit);

            Text binding = Label(card, "Binding", "", new Vector2(5f, -4f), new Vector2(width - 10f, 16f), 11);
            RectTransform iconRect = Rect("Icon", card, new Vector2((width - 44f) * 0.5f, -22f), new Vector2(44f, 44f));
            Image icon = Image(iconRect, Color.white, false);
            icon.sprite = config != null ? config.SpriteIcon : null;
            icon.preserveAspect = true;
            icon.enabled = icon.sprite != null;
            RectTransform cooldown = Rect("Cooldown", card, new Vector2((width - 44f) * 0.5f, -22f), new Vector2(44f, 44f));
            Image(cooldown, new Color(0f, 0f, 0f, 0.78f), false);
            Text timer = Label(cooldown, "Timer", "", Vector2.zero, new Vector2(44f, 44f), 22);
            cooldown.gameObject.SetActive(false);
            Label(card, "Name", config != null ? config.SpellName : "Пусто", new Vector2(3f, -69f), new Vector2(width - 6f, 19f), 12);
            Text cost = Label(card, "Cost", "", new Vector2(3f, -90f), new Vector2(width - 6f, 14f), 11);
            cost.text = config != null && !config.IsPassive ? config.SanityCost.ToString("0.#") + " расс." : "";
            _slots.Add(new SlotView { Button = button, Binding = binding, Cost = cost, Timer = timer, Cooldown = cooldown.gameObject });
        }
        RefreshBindings();
    }

    private void RefreshBindings()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            SpellConfig config = _holder.GetConfig(i);
            _slots[i].Binding.text = config != null && config.IsPassive ? "ПАССИВНО" : _bindings.GetBindingLabel(ESlotBindingType.Spell, i);
        }
    }

    private void LateUpdate()
    {
        if (_canvas == null)
            return;
        if (_slots.Count != _holder.Spells.Count)
            BuildSlots();
        if (_input.IsMenuOpen || Pointer.current == null)
            HideTooltip();
        else if (_hoveredIndex >= 0)
            PositionTooltip(Pointer.current.position.ReadValue());
        for (int i = 0; i < _slots.Count; i++)
        {
            Spell spell = _holder.Spells[i];
            if (spell == null)
            {
                _slots[i].Button.interactable = false;
                continue;
            }
            bool passive = spell.SpellData.IsPassive;
            float remaining = passive ? 0f : spell.CooldownRemaining;
            bool affordable = _vitals.State.z >= spell.SpellData.SanityCost;
            _slots[i].Cooldown.SetActive(remaining > 0f);
            _slots[i].Timer.text = remaining >= 1f ? Mathf.CeilToInt(remaining).ToString() : remaining.ToString("0.0");
            _slots[i].Cost.color = affordable ? new Color(0.9f, 0.57f, 0.23f) : new Color(0.95f, 0.3f, 0.25f);
            _slots[i].Button.interactable = !passive && remaining <= 0f && affordable && _vitals.State.x > 0f && !_input.IsMenuOpen && !_input.IsStatsOpen;
        }
    }

    private void Hide()
    {
        if (_bindings != null)
            _bindings.BindingsChanged -= RefreshBindings;
        if (_canvas != null)
        {
            _canvas.SetActive(false);
            Destroy(_canvas);
            _canvas = null;
        }
        _slots.Clear();
        HideTooltip();
        if (_tooltipCanvas != null)
        {
            _tooltipCanvas.SetActive(false);
            Destroy(_tooltipCanvas);
            _tooltipCanvas = null;
        }
    }

    private void CreateTooltip()
    {
        _tooltipCanvas = new GameObject("SpellTooltipCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        _tooltipCanvas.transform.SetParent(transform, false);
        Canvas canvas = _tooltipCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;
        CanvasScaler scaler = _tooltipCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        _tooltipArea = (RectTransform)_tooltipCanvas.transform;
        _tooltip = Rect("SpellTooltip", _tooltipArea, Vector2.zero, new Vector2(340f, 180f));
        _tooltip.anchorMin = _tooltip.anchorMax = new Vector2(0.5f, 0.5f);
        Image(_tooltip, new Color(0.025f, 0.024f, 0.022f, 0.98f), false);
        Outline border = _tooltip.gameObject.AddComponent<Outline>();
        border.effectColor = new Color(0.4f, 0.32f, 0.19f);
        border.effectDistance = new Vector2(2f, -2f);
        _tooltipTitle = Label(_tooltip, "Title", "", new Vector2(14f, -12f), new Vector2(312f, 28f), 18);
        _tooltipTitle.alignment = TextAnchor.MiddleLeft;
        _tooltipBody = Label(_tooltip, "Description", "", new Vector2(14f, -48f), new Vector2(312f, 200f), 14);
        _tooltipBody.alignment = TextAnchor.UpperLeft;
        _tooltipBody.supportRichText = false;
        _tooltip.gameObject.SetActive(false);
    }

    private void ShowTooltip(int index)
    {
        if (_input.IsMenuOpen)
            return;
        SpellConfig config = _holder.GetConfig(index);
        if (config == null)
            return;
        _hoveredIndex = index;
        _tooltipTitle.text = config.SpellName;
        _tooltipBody.text = config.Description;
        if (!string.IsNullOrEmpty(config.Stats))
            _tooltipBody.text += "\n\n" + config.Stats;
        if (config.IsPassive)
            _tooltipBody.text += "\n\nПассивная способность";
        else
        {
            Spell spell = index < _holder.Spells.Count ? _holder.Spells[index] : null;
            float cooldown = spell != null ? spell.CooldownTime : config.CooldownTime;
            _tooltipBody.text += $"\n\nРассудок: {config.SanityCost:0.#}\nКулдаун: {cooldown:0.#} с";
        }
        _tooltip.gameObject.SetActive(true);
        if (Pointer.current != null)
            PositionTooltip(Pointer.current.position.ReadValue());
    }

    private void HideTooltip()
    {
        _hoveredIndex = -1;
        if (_tooltip != null)
            _tooltip.gameObject.SetActive(false);
    }

    private void PositionTooltip(Vector2 pointer)
    {
        Rect area = _tooltipArea.rect;
        float width = Mathf.Min(340f, area.width - 32f);
        _tooltipTitle.rectTransform.sizeDelta = new Vector2(width - 28f, 28f);
        _tooltipBody.rectTransform.sizeDelta = new Vector2(width - 28f, 600f);
        float bodyHeight = Mathf.Min(_tooltipBody.preferredHeight, area.height - 94f);
        float height = bodyHeight + 62f;
        _tooltipBody.rectTransform.sizeDelta = new Vector2(width - 28f, bodyHeight);
        _tooltip.sizeDelta = new Vector2(width, height);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_tooltipArea, pointer, null, out Vector2 position);
        position.x += position.x + width + 16f > area.xMax ? -width - 16f : 16f;
        position.y -= 16f;
        if (position.y - height < area.yMin)
            position.y += height + 32f;
        position.x = Mathf.Clamp(position.x, area.xMin + 8f, area.xMax - width - 8f);
        position.y = Mathf.Clamp(position.y, area.yMin + height + 8f, area.yMax - 8f);
        _tooltip.anchoredPosition = position;
    }

    private RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private Image Image(RectTransform rect, Color color, bool raycast)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    private Text Label(Transform parent, string name, string text, Vector2 position, Vector2 size, int fontSize)
    {
        RectTransform rect = Rect(name, parent, position, size);
        Text label = rect.gameObject.AddComponent<Text>();
        label.font = _font;
        label.fontSize = fontSize;
        label.text = text;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.87f, 0.83f, 0.74f);
        label.raycastTarget = false;
        return label;
    }
}
