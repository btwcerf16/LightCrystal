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
    private Button _inventoryTab;
    private Button _arsenalTab;
    private Font _font;
    private bool _isArsenalTab;
    private readonly List<Text> _bindingLabels = new List<Text>();
    private readonly List<Button> _bindingButtons = new List<Button>();
    private readonly Color _textColor = new Color(0.84f, 0.8f, 0.72f);

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

        RectTransform main = Panel("Menu", new Vector2(380f, 350f));
        _mainPanel = main.gameObject;
        Label(main, "МЕНЮ", new Vector2(30f, -24f), new Vector2(320f, 40f), 28);
        Label(main, "Игра продолжается", new Vector2(30f, -68f), new Vector2(320f, 24f), 14);
        MakeButton(main, "Продолжить", new Vector2(30f, -124f), new Vector2(320f, 48f), CloseMenu);
        MakeButton(main, "Настройки", new Vector2(30f, -184f), new Vector2(320f, 48f), () => ShowSettings(false));
        MakeButton(main, "Выйти из игры", new Vector2(30f, -244f), new Vector2(320f, 48f), QuitGame);

        RectTransform settings = Panel("Settings", new Vector2(900f, 650f));
        _settingsPanel = settings.gameObject;
        Label(settings, "ПРИВЯЗКИ КЛАВИШ", new Vector2(30f, -24f), new Vector2(840f, 40f), 26);
        _inventoryTab = MakeButton(settings, "Инвентарь", new Vector2(30f, -86f), new Vector2(200f, 40f), () => ShowSettings(false));
        _arsenalTab = MakeButton(settings, "Арсенал", new Vector2(242f, -86f), new Vector2(200f, 40f), () => ShowSettings(true));

        RectTransform scrollArea = Rect("Slots", settings, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -142f), new Vector2(840f, 390f));
        Paint(scrollArea, new Color(0.035f, 0.032f, 0.028f));
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
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        _rows.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = _rows;

        _message = Label(settings, "Нажми на привязку, затем на новую клавишу. Esc — отмена.", new Vector2(30f, -544f), new Vector2(840f, 40f), 15);
        MakeButton(settings, "Назад", new Vector2(30f, -594f), new Vector2(180f, 38f), ShowMain);
        MakeButton(settings, "Сбросить все привязки", new Vector2(570f, -594f), new Vector2(300f, 38f), ResetBindings);
        _bindings.BindingsChanged += RefreshBindings;
        _settingsPanel.SetActive(false);
        _canvas.SetActive(false);
        _toggleMenu.Enable();
    }

    private void OnToggleMenu(InputAction.CallbackContext context)
    {
        if (_canvas == null || _bindings.IsRebinding)
            return;
        if (!_canvas.activeSelf)
        {
            _input.SetMenuOpen(true);
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
    }

    private void ShowMain()
    {
        _bindings.CancelRebind();
        _settingsPanel.SetActive(false);
        _mainPanel.SetActive(true);
    }

    private void ShowSettings(bool isArsenal)
    {
        _bindings.CancelRebind();
        _isArsenalTab = isArsenal;
        _inventoryTab.targetGraphic.color = isArsenal ? new Color(0.12f, 0.105f, 0.08f) : new Color(0.25f, 0.2f, 0.12f);
        _arsenalTab.targetGraphic.color = isArsenal ? new Color(0.25f, 0.2f, 0.12f) : new Color(0.12f, 0.105f, 0.08f);
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
        int count = isArsenal ? GetComponent<ActorArsenal>().WeaponSize : GetComponent<Inventory>().Size;
        for (int i = 0; i < count; i++)
        {
            int index = i;
            RectTransform row = Rect("Slot_" + i, _rows, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 54f;
            Paint(row, new Color(0.075f, 0.067f, 0.054f));
            Label(row, (isArsenal ? "Выбрать оружие " : "Использовать предмет ") + (i + 1), new Vector2(16f, -12f), new Vector2(480f, 30f), 18);
            Button button = MakeButton(row, "", Vector2.zero, new Vector2(190f, 38f), () => RebindSlot(index));
            RectTransform buttonRect = (RectTransform)button.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1f, 0.5f);
            buttonRect.pivot = new Vector2(1f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(-10f, 0f);
            _bindingButtons.Add(button);
            _bindingLabels.Add(button.GetComponentInChildren<Text>());
        }
        _rows.anchoredPosition = Vector2.zero;
        RefreshBindings();
    }

    private void RebindSlot(int index)
    {
        _toggleMenu.Disable();
        _message.text = "Нажми новую клавишу. Esc — отмена.";
        _bindingLabels[index].text = "Нажми клавишу";
        foreach (Button button in _bindingButtons)
            button.interactable = false;
        EventSystem.current.SetSelectedGameObject(null);
        _bindings.BeginRebind(_isArsenalTab, index, message =>
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
            _bindingLabels[i].text = _bindings.GetBindingLabel(_isArsenalTab, i);
    }

    private void ResetBindings()
    {
        _bindings.ResetBindings();
        _message.text = "Привязки инвентаря и арсенала сброшены к стандартным.";
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
        if (_canvas != null)
        {
            _input.SetMenuOpen(false);
            _canvas.SetActive(false);
            Destroy(_canvas);
            _canvas = null;
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
        Paint(panel, new Color(0.025f, 0.024f, 0.022f));
        Outline border = panel.gameObject.AddComponent<Outline>();
        border.effectColor = new Color(0.28f, 0.23f, 0.16f);
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
        Paint(rect, new Color(0.12f, 0.105f, 0.08f));
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(action);
        Text label = Label(rect, text, Vector2.zero, size, 18);
        label.alignment = TextAnchor.MiddleCenter;
        return button;
    }
}
