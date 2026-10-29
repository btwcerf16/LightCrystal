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
    private HoverTooltipUI _tooltip;
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
        float width = Mathf.Min(64f, _panel.rect.width / Mathf.Max(1, _holder.Spells.Count));
        float startX = (_panel.rect.width - width * _holder.Spells.Count) * 0.5f;
        float top = (_panel.rect.height - width) * 0.5f;
        for (int i = 0; i < _holder.Spells.Count; i++)
        {
            int index = i;
            SpellConfig config = _holder.GetConfig(i);
            RectTransform card = Rect("Spell_" + i, _panel, new Vector2(startX + i * width, -top), new Vector2(width, width));
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

            RectTransform iconRect = Rect("Icon", card, new Vector2(2f, -2f), new Vector2(width - 4f, width - 4f));
            Image icon = Image(iconRect, Color.white, false);
            icon.sprite = config != null ? config.SpriteIcon : null;
            icon.preserveAspect = true;
            icon.enabled = icon.sprite != null;
            RectTransform cooldown = Rect("Cooldown", card, new Vector2(2f, -2f), new Vector2(width - 4f, width - 4f));
            Image(cooldown, new Color(0f, 0f, 0f, 0.78f), false);
            Text timer = Label(cooldown, "Timer", "", Vector2.zero, cooldown.sizeDelta, 22);
            cooldown.gameObject.SetActive(false);
            bool passive = config != null && config.IsPassive;
            RectTransform bindingBadge = Rect("BindingBadge", card, new Vector2(2f, -2f), new Vector2(22f, 17f));
            Image(bindingBadge, new Color(0f, 0f, 0f, 0.8f), false);
            Text binding = Label(bindingBadge, "Binding", "", Vector2.zero, bindingBadge.sizeDelta, 12);
            bindingBadge.gameObject.SetActive(!passive);
            RectTransform costBadge = Rect("CostBadge", card, new Vector2(width - 30f, -width + 19f), new Vector2(28f, 17f));
            Image(costBadge, new Color(0f, 0f, 0f, 0.8f), false);
            Text cost = Label(costBadge, "Cost", "", Vector2.zero, costBadge.sizeDelta, 12);
            cost.text = config != null && !passive ? config.SanityCost.ToString("0.#") : "";
            costBadge.gameObject.SetActive(config != null && !passive);
            _slots.Add(new SlotView { Button = button, Binding = binding, Cost = cost, Timer = timer, Cooldown = cooldown.gameObject });
        }
        RefreshBindings();
    }

    private void RefreshBindings()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            SpellConfig config = _holder.GetConfig(i);
            _slots[i].Binding.text = config != null && config.IsPassive ? "" : _bindings.GetBindingLabel(ESlotBindingType.Spell, i);
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
            _tooltip.Move(Pointer.current.position.ReadValue());
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
        if (_tooltip != null)
        {
            _tooltip.gameObject.SetActive(false);
            Destroy(_tooltip.gameObject);
            _tooltip = null;
        }
    }

    private void CreateTooltip()
    {
        _tooltip = HoverTooltipUI.Create(transform, "SpellTooltipCanvas", "SpellTooltip");
    }

    private void ShowTooltip(int index)
    {
        if (_input.IsMenuOpen)
            return;
        SpellConfig config = _holder.GetConfig(index);
        if (config == null)
            return;
        _hoveredIndex = index;
        string description = config.Description;
        if (!string.IsNullOrEmpty(config.Stats))
            description += "\n\n" + config.Stats;
        if (config.IsPassive)
            description += "\n\nПассивная способность";
        else
        {
            Spell spell = index < _holder.Spells.Count ? _holder.Spells[index] : null;
            float cooldown = spell != null ? spell.CooldownTime : config.CooldownTime;
            description += $"\n\nРассудок: {config.SanityCost:0.#}\nКулдаун: {cooldown:0.#} с";
        }

        if (Pointer.current != null)
            _tooltip.Show(index, config.SpellName, description, Pointer.current.position.ReadValue());
    }

    private void HideTooltip()
    {
        _hoveredIndex = -1;
        if (_tooltip != null)
            _tooltip.Hide();
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
