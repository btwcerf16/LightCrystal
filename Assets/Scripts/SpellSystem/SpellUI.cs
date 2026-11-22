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
    [SerializeField] private SpellRangePreview _rangePreviewPrefab;
    private SpellRangePreview _rangePreview;
    private ActorStats _stats;
    public float Width => Mathf.Max(1, GetComponent<SpellHolder>().Count) * 64f + 16f;

    private sealed class SlotView
    {
        public Button Button;
        public Text Binding;
        public Text Cost;
        public Text Timer;
        public Text Charges;
        public Text RechargeTimer;
        public GameObject Cooldown;
        public Image CastFill;
        public Outline Border;
        public UnityEngine.UI.Text ActionLock;
    }

    private readonly List<SlotView> _slots = new();
    private SpellHolder _holder;
    private NetworkSpellHolder _networkHolder;
    private Sprite _castSprite;
    private PlayerSlotBindings _bindings;
    private PlayerInputReader _input;
    private NetworkActorVitals _vitals;
    private EffectHandler _effects;
    private GameObject _canvas;
    private RectTransform _panel;
    private Font _font;
    private HoverTooltipUI _tooltip;
    private int _hoveredIndex = -1;

    private void Awake()
    {
        _stats = GetComponent<ActorStats>();
        _holder = GetComponent<SpellHolder>();
        _networkHolder = GetComponent<NetworkSpellHolder>();
        _bindings = GetComponent<PlayerSlotBindings>();
        _input = GetComponent<PlayerInputReader>();
        _vitals = GetComponent<NetworkActorVitals>();
        _effects = GetComponent<EffectHandler>();
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
        _holder.SpellsChanged += BuildSlots;
    }

    private void CreatePanel()
    {
        _font = HudStyle.PixelFont;
        _castSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        _panel = GetComponent<PlayerHUD>().SpellPanel;
        _canvas = _panel.gameObject;
        Image(_panel, HudStyle.Surface, true);
        CreateTooltip();
        _rangePreview = Instantiate(_rangePreviewPrefab, transform);
        _rangePreview.Hide();
        BuildSlots();
    }

    private void BuildSlots()
    {
        GetComponent<PlayerHUD>().RefreshLayout();
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
            Image background = Image(card, HudStyle.Slot, true);
            Outline border = card.gameObject.AddComponent<Outline>();
            border.effectColor = HudStyle.Line;
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
            RectTransform casting = Rect("Casting", card, new Vector2(2f, -2f), new Vector2(width - 4f, width - 4f));
            Image castFill = Image(casting, new Color(1f, 0.77f, 0.3f, 0.55f), false);
            castFill.sprite = _castSprite;
            castFill.type = UnityEngine.UI.Image.Type.Filled;
            castFill.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            castFill.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
            castFill.fillClockwise = true;
            castFill.fillAmount = 0f;
            casting.gameObject.SetActive(false);
            bool passive = config != null && config.IsPassive;
            Text binding = HudStyle.Binding(card, "");
            binding.transform.parent.gameObject.SetActive(!passive);
            UnityEngine.UI.Text cost = Label(card, "Cost", "", new Vector2(-4f, 3f), new Vector2(36f, 18f), 16);
            RectTransform costRect = cost.rectTransform;
            costRect.anchorMin = costRect.anchorMax = costRect.pivot = Vector2.right;
            cost.alignment = TextAnchor.LowerRight;
            UnityEngine.UI.Outline costOutline = cost.gameObject.AddComponent<UnityEngine.UI.Outline>();
            costOutline.effectColor = Color.black;
            costOutline.effectDistance = new Vector2(1f, -1f);
            cost.text = config != null && !passive ? config.SanityCost.ToString("0.#") : "";
            cost.gameObject.SetActive(config != null && !passive);
            Text charges = Label(card, "Charges", "", Vector2.zero, new Vector2(width, 26f), 21);
            charges.rectTransform.anchorMin = charges.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            charges.rectTransform.pivot = new Vector2(0.5f, 0f);
            charges.rectTransform.anchoredPosition = new Vector2(0f, 7f);
            Outline chargeOutline = charges.gameObject.AddComponent<Outline>();
            chargeOutline.effectColor = Color.black;
            chargeOutline.effectDistance = new Vector2(1f, -1f);
            charges.gameObject.SetActive(false);
            Text rechargeTimer = Label(card, "RechargeTimer", "", Vector2.zero, new Vector2(34f, 16f), 13);
            rechargeTimer.rectTransform.anchorMin = rechargeTimer.rectTransform.anchorMax = rechargeTimer.rectTransform.pivot = Vector2.zero;
            rechargeTimer.rectTransform.anchoredPosition = new Vector2(4f, 3f);
            rechargeTimer.alignment = TextAnchor.LowerLeft;
            rechargeTimer.gameObject.AddComponent<Outline>().effectColor = Color.black;
            rechargeTimer.gameObject.SetActive(false);
            UnityEngine.UI.Text actionLock = HudStyle.CreateActionLock(card);
            _slots.Add(new SlotView
            {
                Button = button, Binding = binding, Cost = cost, Timer = timer,
                Charges = charges, RechargeTimer = rechargeTimer,
                Cooldown = cooldown.gameObject, CastFill = castFill, Border = border, ActionLock = actionLock
            });
        }
        RefreshBindings();
    }

    private void RefreshBindings()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            SpellConfig config = _holder.GetConfig(i);
            HudStyle.SetBinding(_slots[i].Binding, config != null && config.IsPassive ? "" : _bindings.GetBindingLabel(ESlotBindingType.Spell, i));
        }
    }

    private void LateUpdate()
    {
        if (_canvas == null)
            return;
        if (_slots.Count != _holder.Spells.Count)
            BuildSlots();
        if (_input.IsMenuOpen || _input.IsStatsOpen || Pointer.current == null)
            HideTooltip();
        else if (_hoveredIndex >= 0)
            ShowTooltip(_hoveredIndex);
        for (int i = 0; i < _slots.Count; i++)
        {
            Spell spell = _holder.Spells[i];
            if (spell == null)
            {
                HudStyle.SetActionLock(_slots[i].ActionLock, false);
                _slots[i].Button.interactable = false;
                _slots[i].CastFill.gameObject.SetActive(false);
                _slots[i].Cooldown.SetActive(false);
                _slots[i].Charges.gameObject.SetActive(false);
                _slots[i].RechargeTimer.gameObject.SetActive(false);
                continue;
            }
            bool passive = spell.SpellData.IsPassive;
            bool blocked = !passive && _effects != null && (_effects.IsStunned || _effects.IsSilenced);
            HudStyle.SetActionLock(_slots[i].ActionLock,
                !passive && _effects != null && _effects.IsStunned,
                !passive && _effects != null && _effects.IsSilenced);
            bool toggled = _networkHolder != null ? _networkHolder.IsSpellToggled(i) : spell is ToggleSpell { IsToggled: true };
            float remaining = passive ? 0f : spell.CooldownRemaining;
            float progress = _networkHolder != null ? _networkHolder.GetCastProgress(i) : spell.CastProgress;
            bool casting = !passive && progress >= 0f;
            _slots[i].CastFill.gameObject.SetActive(casting);
            _slots[i].CastFill.fillAmount = Mathf.Clamp01(progress);
            _slots[i].Cost.text = passive ? "" : spell.SanityCost.ToString("0.#");
            bool affordable = toggled || _vitals.State.z >= spell.SanityCost;
            int availableCharges = spell.AvailableCharges;
            int maxCharges = spell.MaxCharges;
            bool hasCharge = availableCharges > 0;
            bool showCharges = !passive && maxCharges > 1;
            _slots[i].Border.effectColor = toggled ? new Color(0.4f, 0.9f, 0.5f) : HudStyle.Line;
            string timer = remaining >= 1f ? Mathf.CeilToInt(remaining).ToString() : remaining.ToString("0.0");
            _slots[i].Cooldown.SetActive(!casting && !hasCharge && remaining > 0f);
            _slots[i].Timer.text = timer;
            _slots[i].Charges.gameObject.SetActive(showCharges);
            if (showCharges)
                _slots[i].Charges.text = availableCharges.ToString();
            bool showRecharge = showCharges && hasCharge && remaining > 0f;
            _slots[i].RechargeTimer.gameObject.SetActive(showRecharge);
            if (showRecharge)
                _slots[i].RechargeTimer.text = timer + " с";
            _slots[i].Cost.color = affordable ? new Color32(255, 227, 148, 255) : new Color32(255, 126, 114, 255);
            _slots[i].Button.interactable = !blocked && !passive && !casting && hasCharge && affordable && _vitals.State.x > 0f && !_input.IsMenuOpen && !_input.IsStatsOpen;
        }
    }

    private void Hide()
    {
        if (_bindings != null)
            _bindings.BindingsChanged -= RefreshBindings;
        _holder.SpellsChanged -= BuildSlots;
        if (_canvas != null)
        {
            _canvas.SetActive(false);
            Destroy(_canvas);
            _canvas = null;
        }
        _slots.Clear();
        if (_castSprite != null)
        {
            Destroy(_castSprite);
            _castSprite = null;
        }
        HideTooltip();
        if (_rangePreview != null)
        {
            Destroy(_rangePreview.gameObject);
            _rangePreview = null;
        }
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
        if (_input.IsMenuOpen || _input.IsStatsOpen)
            return;
        SpellConfig config = _holder.GetConfig(index);
        if (config == null)
            return;
        _hoveredIndex = index;
        _rangePreview.Show(config.GetRange(_stats));
        Spell spell = index < _holder.Spells.Count ? _holder.Spells[index] : null;
        string description = spell != null ? spell.Description : config.Description;
        string stats = spell != null ? spell.StatsDescription : config.Stats;
        if (!string.IsNullOrEmpty(stats))
            description += "\n\n" + stats;
        if (config.IsPassive)
            description += "\n\nПассивная способность";
        else
        {
            float cooldown = spell != null ? spell.CooldownTime : config.CooldownTime;
            if (spell != null && spell.CastTime > 0f)
                description += $"\nВремя чтения: {spell.CastTime:0.##} с";
            description += $"\n\nРассудок: {(spell != null ? spell.SanityCost : config.SanityCost):0.#}";
            int maxCharges = spell != null ? spell.MaxCharges : config.MaxCharges;
            description += maxCharges > 1
                ? $"\nЗаряды: {spell?.AvailableCharges ?? maxCharges}/{maxCharges}\nВосстановление заряда: {cooldown:0.#} с"
                : $"\nКулдаун: {cooldown:0.#} с";
            if (maxCharges > 1 && spell != null && spell.CooldownRemaining > 0f)
                description += $"\nСледующий заряд через {spell.CooldownRemaining:0.0} с";
        }

        if (Pointer.current != null)
            _tooltip.Show(index, config.SpellName, description, Pointer.current.position.ReadValue());
    }

    private void HideTooltip()
    {
        _hoveredIndex = -1;
        if (_rangePreview != null) _rangePreview.Hide();
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
        label.color = HudStyle.Text;
        label.raycastTarget = false;
        return label;
    }
}
