using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerInputReader))]
public sealed class GameConsole : NetworkBehaviour
{
    private readonly ConsoleCommandRegistry _commands = new();
    private readonly List<string> _log = new();
    private readonly List<string> _history = new();
    private PlayerInputReader _input;
    private InputAction _toggle;
    private GameObject _canvas;
    private RectTransform _panel;
    private InputField _commandLine;
    private Text _completionHint;
    private List<string> _completionMatches = new();
    private string _completionLeading = "";
    private int _completionIndex;
    private bool _isCyclingCompletions;
    private Text _output;
    private ScrollRect _scroll;
    private Font _font;
    private int _historyIndex;
    private string _draft = "";
    private bool _needsFocus;
    private bool _needsScroll;
    private bool _cursorWasVisible;
    private CursorLockMode _previousCursorLock;

    private void Awake()
    {
        _input = GetComponent<PlayerInputReader>();
        _toggle = new InputAction("ToggleConsole", InputActionType.Button, "<Keyboard>/backquote");
        _toggle.performed += OnToggle;
    }

    public override void OnNetworkSpawn() => Bind();
    public override void OnGainedOwnership() => Bind();
    public override void OnLostOwnership() => Unbind();
    public override void OnNetworkDespawn() => Unbind();
    private void OnEnable() => Bind();
    private void OnDisable() => Unbind();

    private void Bind()
    {
        if (!IsSpawned || !IsOwner || !IsClient || !isActiveAndEnabled || _canvas != null)
            return;

        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _canvas = new GameObject("ConsoleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas.transform.SetParent(transform, false);
        Canvas canvas = _canvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = _canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        Paint((RectTransform)_canvas.transform, new Color(0f, 0f, 0f, 0.3f));

        _panel = Rect("Console", _canvas.transform);
        _panel.anchorMin = new Vector2(0f, 1f);
        _panel.anchorMax = Vector2.one;
        _panel.pivot = new Vector2(0.5f, 1f);
        _panel.anchoredPosition = new Vector2(0f, -16f);
        _panel.sizeDelta = new Vector2(-32f, 360f);
        Paint(_panel, new Color(0.025f, 0.024f, 0.022f, 0.99f));
        Outline border = _panel.gameObject.AddComponent<Outline>();
        border.effectColor = new Color(0.27f, 0.24f, 0.18f);
        border.effectDistance = Vector2.one;

        Text title = Label("Title", _panel, 16);
        title.text = "КОНСОЛЬ  ·  ~ / Esc — закрыть  ·  ↑↓ — история  ·  Tab — дополнить  ·  Help — команды";
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = Vector2.one;
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -12f);
        title.rectTransform.sizeDelta = new Vector2(-32f, 26f);

        RectTransform viewport = Rect("Output", _panel);
        viewport.offsetMin = new Vector2(16f, 62f);
        viewport.offsetMax = new Vector2(-16f, -46f);
        Paint(viewport, new Color(0.035f, 0.033f, 0.028f));
        viewport.gameObject.AddComponent<RectMask2D>();
        _scroll = viewport.gameObject.AddComponent<ScrollRect>();
        _scroll.viewport = viewport;
        _scroll.horizontal = false;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.scrollSensitivity = 24f;
        _output = Label("Text", viewport, 15);
        _output.rectTransform.anchorMin = new Vector2(0f, 1f);
        _output.rectTransform.anchorMax = Vector2.one;
        _output.rectTransform.pivot = new Vector2(0.5f, 1f);
        _output.rectTransform.sizeDelta = new Vector2(-12f, 0f);
        _output.rectTransform.anchoredPosition = new Vector2(0f, -6f);
        _output.verticalOverflow = VerticalWrapMode.Overflow;
        _output.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _scroll.content = _output.rectTransform;

        RectTransform entry = Rect("Command", _panel);
        entry.anchorMin = Vector2.zero;
        entry.anchorMax = Vector2.right;
        entry.pivot = new Vector2(0.5f, 0f);
        entry.anchoredPosition = new Vector2(0f, 14f);
        entry.sizeDelta = new Vector2(-32f, 36f);
        Paint(entry, new Color(0.07f, 0.064f, 0.052f));
        _commandLine = entry.gameObject.AddComponent<InputField>();
        _commandLine.targetGraphic = entry.GetComponent<Image>();
        _commandLine.lineType = InputField.LineType.SingleLine;
        _commandLine.characterLimit = 512;
        _commandLine.customCaretColor = true;
        _commandLine.caretColor = new Color(0.9f, 0.84f, 0.68f);
        _commandLine.navigation = new Navigation { mode = Navigation.Mode.None };
        _commandLine.onValidateInput = (value, index, character) => character == '\t' ? '\0' : character;
        entry.gameObject.AddComponent<RectMask2D>();
        Text text = Label("InputText", entry, 16);
        text.rectTransform.offsetMin = new Vector2(10f, 4f);
        text.rectTransform.offsetMax = new Vector2(-10f, -4f);
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        _commandLine.textComponent = text;
        Text placeholder = Label("Placeholder", entry, 16);
        placeholder.rectTransform.offsetMin = text.rectTransform.offsetMin;
        placeholder.rectTransform.offsetMax = text.rectTransform.offsetMax;
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.color = new Color(0.43f, 0.41f, 0.36f);
        placeholder.text = "Введите команду…";
        _commandLine.placeholder = placeholder;

        _completionHint = Label("Completion", entry, 16);
        _completionHint.rectTransform.offsetMin = text.rectTransform.offsetMin;
        _completionHint.rectTransform.offsetMax = text.rectTransform.offsetMax;
        _completionHint.alignment = TextAnchor.MiddleLeft;
        _completionHint.horizontalOverflow = HorizontalWrapMode.Overflow;
        _completionHint.color = new Color(0.43f, 0.43f, 0.43f);
        _commandLine.onValueChanged.AddListener(OnCommandEdited);

        _output.text = string.Join("\n", _log);
        if (_log.Count == 0)
            Write("Введи Help. Пример: SetAttackSpeed 2. Дробные числа вводятся через точку.");
        _canvas.SetActive(false);
        _toggle.Enable();
    }

    private void OnToggle(InputAction.CallbackContext context)
    {
        if (_canvas == null)
            return;
        if (_input.IsConsoleOpen)
            Close();
        else if (!_input.IsMenuOpen)
            Open();
    }

    private void Open()
    {
        RefreshCommands();
        _cursorWasVisible = Cursor.visible;
        _previousCursorLock = Cursor.lockState;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _input.SetConsoleOpen(true);
        _canvas.SetActive(true);
        _commandLine.text = "";
        OnCommandEdited("");
        _historyIndex = _history.Count;
        _draft = "";
        _needsFocus = true;
        _needsScroll = true;
    }

    private void Close()
    {
        _commandLine.DeactivateInputField();
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null
            && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(_canvas.transform))
            EventSystem.current.SetSelectedGameObject(null);
        _canvas.SetActive(false);
        _input.SetConsoleOpen(false);
        Cursor.lockState = _previousCursorLock;
        Cursor.visible = _cursorWasVisible;
        _needsFocus = false;
        _completionHint.text = "";
    }

    private void Update()
    {
        if (_canvas == null || !_canvas.activeSelf || Keyboard.current == null)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }
        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            Submit();
        else if (_commandLine.isFocused && keyboard.upArrowKey.wasPressedThisFrame)
            Recall(-1);
        else if (_commandLine.isFocused && keyboard.downArrowKey.wasPressedThisFrame)
            Recall(1);
    }

    private void LateUpdate()
    {
        if (_canvas == null || !_canvas.activeSelf)
            return;
        _panel.sizeDelta = new Vector2(-32f, Mathf.Min(390f, ((RectTransform)_canvas.transform).rect.height * 0.6f));
        if (_needsFocus)
        {
            _commandLine.Select();
            _commandLine.ActivateInputField();
            _commandLine.MoveTextEnd(false);
            _needsFocus = false;
        }
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && _commandLine.isFocused && keyboard.tabKey.wasPressedThisFrame)
            CompleteCommand(keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        if (_needsScroll)
        {
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 0f;
            _needsScroll = false;
        }
        RefreshCompletionHint();
    }

    private void OnCommandEdited(string text)
    {
        _completionIndex = 0;
        _isCyclingCompletions = false;
        string prefix = text.TrimStart();
        _completionLeading = text.Substring(0, text.Length - prefix.Length);
        _completionMatches = _commands.GetCompletions(prefix);
    }

    private bool CanComplete()
    {
        return _commandLine.isFocused && _completionMatches.Count > 0
            && _commandLine.caretPosition == _commandLine.text.Length
            && _commandLine.selectionAnchorPosition == _commandLine.selectionFocusPosition;
    }

    private void CompleteCommand(bool isPrevious)
    {
        if (!CanComplete())
            return;

        if (_isCyclingCompletions)
            _completionIndex = (_completionIndex + (isPrevious ? -1 : 1) + _completionMatches.Count) % _completionMatches.Count;
        else
            _completionIndex = isPrevious ? _completionMatches.Count - 1 : 0;

        string completed = _completionLeading + _completionMatches[_completionIndex];
        _commandLine.SetTextWithoutNotify(completed);
        _commandLine.caretPosition = completed.Length;
        _isCyclingCompletions = true;
    }

    private void RefreshCompletionHint()
    {
        if (!CanComplete())
        {
            _completionHint.text = "";
            return;
        }

        string text = _commandLine.text.TrimStart();
        string candidate = _completionMatches[_completionIndex];
        if (!candidate.StartsWith(text, StringComparison.OrdinalIgnoreCase) || candidate.Length <= text.Length)
        {
            _completionHint.text = "";
            return;
        }

        Text inputText = _commandLine.textComponent;
        float width = inputText.preferredWidth;
        if (width >= inputText.rectTransform.rect.width)
        {
            _completionHint.text = "";
            return;
        }

        Vector2 inset = inputText.rectTransform.offsetMin;
        _completionHint.rectTransform.offsetMin = new Vector2(inset.x + width, inset.y);
        _completionHint.text = candidate.Substring(text.Length);
    }

    private void Submit()
    {
        string text = _commandLine.text.Trim();
        _commandLine.text = "";
        _needsFocus = true;
        if (text.Length == 0)
            return;
        if (_history.Count == 0 || _history[_history.Count - 1] != text)
        {
            _history.Add(text);
            if (_history.Count > 100)
                _history.RemoveAt(0);
        }
        _historyIndex = _history.Count;
        _draft = "";
        Write("> " + text);
        try
        {
            RefreshCommands();
            string result = _commands.Execute(text, IsServer);
            if (!string.IsNullOrEmpty(result))
                Write(result);
        }
        catch (Exception error)
        {
            Write("Ошибка: " + error.Message);
        }
    }

    private void RefreshCommands()
    {
        _commands.Clear();
        foreach (MonoBehaviour component in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (component == null)
                continue;
            if (component is GameConsole)
            {
                if (component == this)
                    _commands.Register(component);
                continue;
            }
            NetworkObject owner = component.GetComponentInParent<NetworkObject>();
            if (owner != null && owner.IsPlayerObject)
            {
                if (owner.IsSpawned)
                    _commands.Register(component, "Player" + (owner.OwnerClientId + 1), owner == NetworkObject);
            }
            else
                _commands.Register(component);
        }
    }

    private void Recall(int direction)
    {
        if (_historyIndex == _history.Count)
            _draft = _commandLine.text;
        _historyIndex = Mathf.Clamp(_historyIndex + direction, 0, _history.Count);
        _commandLine.text = _historyIndex == _history.Count ? _draft : _history[_historyIndex];
        _needsFocus = true;
    }

    private void Write(string message)
    {
        _log.Add(message);
        if (_log.Count > 100)
            _log.RemoveAt(0);
        _output.text = string.Join("\n", _log);
        _needsScroll = true;
    }

    [ConsoleCommand(ServerOnly = false)]
    private string Help(string command = "") => _commands.GetHelp(command);

    [ConsoleCommand(ServerOnly = false)]
    private string Players()
    {
        List<NetworkObject> players = new();
        foreach (NetworkObject player in FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
        {
            if (player.IsSpawned && player.IsPlayerObject)
                players.Add(player);
        }
        players.Sort((first, second) => first.OwnerClientId.CompareTo(second.OwnerClientId));
        List<string> lines = new();
        foreach (NetworkObject player in players)
        {
            string label = $"Player{player.OwnerClientId + 1} | ClientId: {player.OwnerClientId}";
            if (player == NetworkObject)
                label += " | ты";
            lines.Add(label);
        }
        return string.Join("\n", lines);
    }

    [ConsoleCommand(ServerOnly = false)]
    private void Clear()
    {
        _log.Clear();
        _output.text = "";
    }

    private void Unbind()
    {
        _toggle?.Disable();
        if (_canvas == null)
            return;
        if (_input.IsConsoleOpen)
            Close();
        _commandLine.onValueChanged.RemoveListener(OnCommandEdited);
        _completionMatches.Clear();
        _commands.Clear();
        Destroy(_canvas);
        _canvas = null;
    }

    public override void OnDestroy()
    {
        Unbind();
        _toggle?.Dispose();
        base.OnDestroy();
    }

    private RectTransform Rect(string name, Transform parent)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private void Paint(RectTransform rect, Color color)
    {
        rect.gameObject.AddComponent<Image>().color = color;
    }

    private Text Label(string name, Transform parent, int fontSize)
    {
        Text label = Rect(name, parent).gameObject.AddComponent<Text>();
        label.font = _font;
        label.fontSize = fontSize;
        label.color = new Color(0.84f, 0.8f, 0.72f);
        label.supportRichText = false;
        label.raycastTarget = false;
        label.alignment = TextAnchor.UpperLeft;
        return label;
    }
}
