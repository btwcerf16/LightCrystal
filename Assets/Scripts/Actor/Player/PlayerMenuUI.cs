using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class PlayerMenuUI : NetworkBehaviour
{
    private PlayerInputReader _input;
    private PlayerSlotBindings _bindings;
    private InputAction _toggleMenu;
    private GameObject _canvas;
    private GameObject _mainPanel;
    private GameObject _settingsPanel;
    private RectTransform _rows;
    private Text _message;
    private Text _wavePauseLabel;
    private Button _inventoryTab;
    private Button _arsenalTab;
    private Button _spellsTab;
    private Font _font;
    private ESlotBindingType _bindingType;
    private readonly List<Text> _bindingLabels = new List<Text>();
    private readonly List<Button> _bindingButtons = new List<Button>();
    private readonly Color _textColor = HudStyle.Text;

    private void Awake()
    {
        _input = GetComponent<PlayerInputReader>();
        _bindings = GetComponent<PlayerSlotBindings>();
        _toggleMenu = new InputAction("ToggleMenu", InputActionType.Button, "<Keyboard>/escape");
        _toggleMenu.performed += OnToggleMenu;
    }

    public override void OnNetworkSpawn() => CreateMenu();
    public override void OnGainedOwnership() => CreateMenu();
    public override void OnLostOwnership() => RemoveMenu();
    public override void OnNetworkDespawn() => RemoveMenu();
    private void OnEnable() => CreateMenu();
    private void OnDisable() => RemoveMenu();

    private void CreateMenu()
    {
        if (!IsSpawned || !IsClient || !IsOwner || _canvas != null)
            return;

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        _canvas = new GameObject("MenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas.transform.SetParent(transform, false);
        Canvas canvas = _canvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = _canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        Paint((RectTransform)_canvas.transform, new Color(0f, 0f, 0f, 0.78f));

        RectTransform main = Panel("Menu", new Vector2(384f, IsServer ? 400f : 336f));
        _mainPanel = main.gameObject;
        Label(main, "Меню", new Vector2(24f, -24f), new Vector2(336f, 32f), 24);
        Label(main, "Игра продолжается", new Vector2(24f, -64f), new Vector2(336f, 24f), 14);
        MakeButton(main, "Продолжить", new Vector2(24f, -112f), new Vector2(336f, 48f), CloseMenu);
        MakeButton(main, "Настройки", new Vector2(24f, -176f), new Vector2(336f, 48f), () => ShowSettings(ESlotBindingType.Inventory));
        if (IsServer)
            _wavePauseLabel = MakeButton(main, "Приостановить волны", new Vector2(24f, -240f), new Vector2(336f, 48f), ToggleWavePause).GetComponentInChildren<Text>();
        MakeButton(main, "Выйти из игры", new Vector2(24f, IsServer ? -304f : -240f), new Vector2(336f, 48f), QuitGame);

        RectTransform settings = Panel("Settings", new Vector2(800f, 600f));
        _settingsPanel = settings.gameObject;
        Label(settings, "Привязки клавиш", new Vector2(24f, -24f), new Vector2(752f, 40f), 24);
        _inventoryTab = MakeButton(settings, "Инвентарь", new Vector2(24f, -88f), new Vector2(240f, 40f), () => ShowSettings(ESlotBindingType.Inventory));
        _arsenalTab = MakeButton(settings, "Арсенал", new Vector2(280f, -88f), new Vector2(240f, 40f), () => ShowSettings(ESlotBindingType.Arsenal));
        _spellsTab = MakeButton(settings, "Способности", new Vector2(536f, -88f), new Vector2(240f, 40f), () => ShowSettings(ESlotBindingType.Spell));

        RectTransform scrollArea = Rect("Slots", settings, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -144f), new Vector2(752f, 336f));
        Paint(scrollArea, HudStyle.Surface);
        scrollArea.gameObject.AddComponent<RectMask2D>();
        ScrollRect scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.viewport = scrollArea;
        _rows = Rect("Content", scrollArea, new Vector2(0f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        _rows.anchorMax = Vector2.one;
        _rows.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = _rows.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        _rows.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = _rows;

        _message = Label(settings, "Нажми на привязку, затем на новую клавишу. Esc — отмена.", new Vector2(24f, -492f), new Vector2(752f, 36f), 14);
        MakeButton(settings, "Назад", new Vector2(24f, -544f), new Vector2(160f, 40f), ShowMain);
        MakeButton(settings, "Сбросить все привязки", new Vector2(512f, -544f), new Vector2(264f, 40f), ResetBindings);
        _bindings.BindingsChanged += RefreshBindings;
        GetComponent<SpellHolder>().SpellsChanged += RefreshSpellRows;
        _settingsPanel.SetActive(false);
        _canvas.SetActive(false);
        _toggleMenu.Enable();
    }

    private void OnToggleMenu(InputAction.CallbackContext context)
    {
        if (_canvas == null || _bindings.IsRebinding || _input.IsConsoleOpen || GetComponent<NetworkPlayerSetup>().IsRunGuideOpen)
            return;
        if (!_canvas.activeSelf)
        {
            _input.SetMenuOpen(true);
            GetComponent<PlayerHUD>().SetEventsMenuOpen(true);
            _canvas.SetActive(true);
            ShowMain();
        }
        else if (_settingsPanel.activeSelf)
            ShowMain();
        else
            CloseMenu();
    }

    private void CloseMenu()
    {
        _bindings.CancelRebind();
        _canvas.SetActive(false);
        _input.SetMenuOpen(false);
        GetComponent<PlayerHUD>().SetEventsMenuOpen(false);
    }

    private void ShowMain()
    {
        _bindings.CancelRebind();
        _settingsPanel.SetActive(false);
        _mainPanel.SetActive(true);
        RefreshWavePause();
    }

    private void ToggleWavePause()
    {
        NetworkManager.GetComponent<NetworkWaveDirector>().PauseWaves(!NetworkManager.GetComponent<WaveDirector>().IsPaused);
        RefreshWavePause();
    }

    private void RefreshWavePause()
    {
        if (_wavePauseLabel != null)
            _wavePauseLabel.text = NetworkManager.GetComponent<WaveDirector>().IsPaused ? "Продолжить волны" : "Приостановить волны";
    }

    private void ShowSettings(ESlotBindingType type)
    {
        _bindings.CancelRebind();
        _bindingType = type;
        Color selected = HudStyle.Line;
        Color normal = HudStyle.Slot;
        _inventoryTab.targetGraphic.color = type == ESlotBindingType.Inventory ? selected : normal;
        _arsenalTab.targetGraphic.color = type == ESlotBindingType.Arsenal ? selected : normal;
        _spellsTab.targetGraphic.color = type == ESlotBindingType.Spell ? selected : normal;
        _mainPanel.SetActive(false);
        _settingsPanel.SetActive(true);
        _message.text = "Нажми на привязку, затем на новую клавишу. Esc — отмена.";
        foreach (Transform child in _rows)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        _bindingLabels.Clear();
        _bindingButtons.Clear();
        int count;
        switch (type)
        {
            case ESlotBindingType.Arsenal: count = GetComponent<ActorArsenal>().WeaponSize; break;
            case ESlotBindingType.Spell: count = GetComponent<SpellHolder>().Count + 1; break;
            default: count = GetComponent<Inventory>().Size; break;
        }
        for (int i = 0; i < count; i++)
        {
            int index = i;
            RectTransform row = Rect("Slot_" + i, _rows, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
            Paint(row, HudStyle.Slot);
            string title = type == ESlotBindingType.Arsenal ? "Выбрать оружие " : "Использовать предмет ";
            title += i + 1;
            if (type == ESlotBindingType.Spell)
            {
                SpellConfig config = GetComponent<SpellHolder>().GetConfig(i);
                title = i == count - 1 ? "Отменить чтение" : config != null ? config.SpellName : "Пустой слот";
            }
            Label(row, title, new Vector2(16f, -16f), new Vector2(488f, 24f), 16);
            Button button = MakeButton(row, "", Vector2.zero, new Vector2(176f, 40f), () => RebindSlot(index));
            button.targetGraphic.color = HudStyle.Surface;
            RectTransform buttonRect = (RectTransform)button.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1f, 0.5f);
            buttonRect.pivot = new Vector2(1f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(-8f, 0f);
            _bindingButtons.Add(button);
            _bindingLabels.Add(button.GetComponentInChildren<Text>());
        }
        _rows.anchoredPosition = Vector2.zero;
        RefreshBindings();
    }

    private void RefreshSpellRows()
    {
        if (_settingsPanel != null && _settingsPanel.activeSelf && _bindingType == ESlotBindingType.Spell)
            ShowSettings(ESlotBindingType.Spell);
    }

    private void RebindSlot(int index)
    {
        _toggleMenu.Disable();
        _message.text = "Нажми новую клавишу. Esc — отмена.";
        _bindingLabels[index].text = "Нажми клавишу";
        foreach (Button button in _bindingButtons)
            button.interactable = false;
        EventSystem.current.SetSelectedGameObject(null);
        bool cancelCast = _bindingType == ESlotBindingType.Spell && index == GetComponent<SpellHolder>().Count;
        _bindings.BeginRebind(cancelCast ? ESlotBindingType.CancelCast : _bindingType, cancelCast ? 0 : index, message =>
        {
            if (_canvas == null)
                return;
            _message.text = message;
            foreach (Button button in _bindingButtons)
                button.interactable = true;
            RefreshBindings();
            _toggleMenu.Enable();
        });
    }

    private void RefreshBindings()
    {
        for (int i = 0; i < _bindingLabels.Count; i++)
        {
            SpellConfig config = _bindingType == ESlotBindingType.Spell ? GetComponent<SpellHolder>().GetConfig(i) : null;
            bool passive = config != null && config.IsPassive;
            bool cancelCast = _bindingType == ESlotBindingType.Spell && i == GetComponent<SpellHolder>().Count;
            _bindingLabels[i].text = passive ? "Пассивная" : _bindings.GetBindingLabel(cancelCast ? ESlotBindingType.CancelCast : _bindingType, cancelCast ? 0 : i);
            _bindingButtons[i].interactable = !passive && !_bindings.IsRebinding;
        }
    }

    private void ResetBindings()
    {
        _bindings.ResetBindings();
        _message.text = "Все привязки сброшены к стандартным.";
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RemoveMenu()
    {
        _bindings.CancelRebind();
        _toggleMenu.Disable();
        _bindings.BindingsChanged -= RefreshBindings;
        GetComponent<SpellHolder>().SpellsChanged -= RefreshSpellRows;
        if (_canvas != null)
        {
            _input.SetMenuOpen(false);
            GetComponent<PlayerHUD>().SetEventsMenuOpen(false);
            _canvas.SetActive(false);
            Destroy(_canvas);
            _canvas = null;
            _wavePauseLabel = null;
        }
    }

    public override void OnDestroy()
    {
        RemoveMenu();
        _toggleMenu.Dispose();
        base.OnDestroy();
    }

    private RectTransform Panel(string name, Vector2 size)
    {
        RectTransform panel = Rect(name, _canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        Paint(panel, HudStyle.Surface);
        Outline border = panel.gameObject.AddComponent<Outline>();
        border.effectColor = HudStyle.Line;
        border.effectDistance = Vector2.one;
        return panel;
    }

    private RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private void Paint(RectTransform rect, Color color)
    {
        rect.gameObject.AddComponent<Image>().color = color;
    }

    private Text Label(Transform parent, string value, Vector2 position, Vector2 size, int fontSize)
    {
        RectTransform rect = Rect("Label", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = _font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = _textColor;
        text.raycastTarget = false;
        return text;
    }

    private Button MakeButton(Transform parent, string text, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = Rect(text, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
        Paint(rect, HudStyle.Slot);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(action);
        Text label = Label(rect, text, Vector2.zero, size, 16);
        label.alignment = TextAnchor.MiddleCenter;
        return button;
    }
}
