using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class LobbyMenuUI : MonoBehaviour
{
    [SerializeField] private CharacterDatabase _database;
    private CharacterConfig[] _characters => _database.Characters;
    [SerializeField] private Font _menuFont;
    [SerializeField] private Sprite _crystalSprite;
    private static readonly Color Ink = new Color32(21, 25, 23, 255);
    private static readonly Color Paper = new Color32(228, 226, 211, 255);
    private static readonly Color Secondary = new Color32(164, 172, 160, 255);
    private static readonly Color Accent = new Color32(150, 208, 207, 255);
    private LobbyConnection _connection;
    private GameLobby _lobby;
    private Font _font;
    private GameObject _entry;
    private GameObject _room;
    private InputField _address;
    private Text _status;
    private Text _roster;
    private Text _className;
    private Text _description;
    private Text _readyLabel;
    private Button _ready;
    private Button _previous;
    private Button _next;
    private Button _cancelConnection;
    private ScrollRect _scroll;
    private ScrollSnap _snap;
    private RectTransform[] _cards;
    private RectTransform[] _cardVisuals;
    private CanvasGroup[] _cardGroups;
    private readonly Image[] _spellIcons = new Image[4];
    private HoverTooltipUI _spellTooltip;
    private int _hoveredSpell = -1;
    private GameObject _statsPanel;
    private Text _statsTitle;
    private Text _statsNames;
    private Text _statsValues;
    private RectTransform _statsContent;
    private int _selected;
    private bool _localReady;
    private bool _isMoving;
    private bool _selectionSynced;

    private void Start()
    {
        _connection = NetworkManager.Singleton.GetComponent<LobbyConnection>();
        _font = _menuFont != null ? _menuFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        RectTransform canvas = new GameObject("LobbyCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<RectTransform>();
        canvas.SetParent(transform, false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform background = Rect("Background", canvas, Vector2.zero, new Vector2(1920, 1080));
        RectTransform backdrop = Rect("Backdrop", canvas, Vector2.zero, Vector2.zero);
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
        Paint(backdrop, Ink).raycastTarget = false;
        backdrop.SetAsFirstSibling();
        Label(background, "Light Crystal", new Vector2(-608, 436), new Vector2(480, 64), 40, Paper, TextAnchor.MiddleLeft);
        Paint(Rect("HeaderRule", background, new Vector2(0, 380), new Vector2(1696, 2)), new Color(1, 1, 1, 0.08f));
        Button(background, "Выход", new Vector2(784, 436), new Vector2(128, 48), Quit, quiet: true);

        _entry = Rect("Connection", background, Vector2.zero, new Vector2(1696, 704)).gameObject;
        Label(_entry.transform, "Собрать отряд", new Vector2(-528, 208), new Vector2(640, 80), 64, Paper, TextAnchor.MiddleLeft);
        Label(_entry.transform, "Создай комнату или подключись к друзьям", new Vector2(-528, 128), new Vector2(640, 48), 28, Secondary, TextAnchor.MiddleLeft);
        Button(_entry.transform, "Создать комнату", new Vector2(-608, 32), new Vector2(480, 64), () => _connection.CreateLobby(), primary: true);
        Label(_entry.transform, "Адрес хоста", new Vector2(-688, -64), new Vector2(320, 40), 24, Secondary, TextAnchor.MiddleLeft);
        RectTransform address = Rect("HostAddress", _entry.transform, new Vector2(-688, -120), new Vector2(320, 64));
        Paint(address, new Color32(38, 44, 40, 255));
        _address = address.gameObject.AddComponent<InputField>();
        Text addressText = Label(address, "127.0.0.1", Vector2.zero, new Vector2(288, 56), 28, Paper, TextAnchor.MiddleLeft);
        _address.textComponent = addressText;
        _address.text = "127.0.0.1";
        _address.characterLimit = 45;
        Button(_entry.transform, "Подключиться", new Vector2(-392, -120), new Vector2(240, 64), () => _connection.Connect(_address.text));
        _cancelConnection = Button(_entry.transform, "Отменить подключение", new Vector2(-608, -216), new Vector2(480, 48), () => _connection.Leave(), quiet: true);
        _cancelConnection.gameObject.SetActive(false);
        Image crystal = Paint(Rect("Crystal", _entry.transform, new Vector2(480, 64), new Vector2(256, 384)), Color.white);
        crystal.sprite = _crystalSprite;
        crystal.preserveAspect = true;
        crystal.raycastTarget = false;
        _status = Label(background, "", new Vector2(0, -436), new Vector2(1696, 48), 24, Secondary, TextAnchor.MiddleLeft);

        _room = Rect("CharacterSelection", background, Vector2.zero, new Vector2(1696, 704)).gameObject;
        Label(_room.transform, "Отряд", new Vector2(-712, 288), new Vector2(272, 48), 32, Paper, TextAnchor.MiddleLeft);
        _roster = Label(_room.transform, "", new Vector2(-712, 8), new Vector2(272, 448), 24, Secondary, TextAnchor.UpperLeft);
        _roster.lineSpacing = 1.25f;
        Label(_room.transform, "Выбор персонажа", new Vector2(-96, 288), new Vector2(672, 48), 28, Secondary);
        BuildCarousel();
        RectTransform selection = Rect("Selected", _room.transform, new Vector2(-96, -128), new Vector2(12, 12));
        Paint(selection, Accent).raycastTarget = false;
        selection.localRotation = Quaternion.Euler(0, 0, 45);
        _className = Label(_room.transform, "", new Vector2(656, 176), new Vector2(384, 144), 40, Paper, TextAnchor.MiddleLeft);
        _description = Label(_room.transform, "", new Vector2(656, -24), new Vector2(384, 224), 28, Secondary, TextAnchor.UpperLeft);
        _description.lineSpacing = 1.2f;
        _spellTooltip = HoverTooltipUI.Create(transform, "LobbySpellTooltipCanvas", "LobbySpellTooltip");
        for (int i = 0; i < _spellIcons.Length; i++)
        {
            int index = i;
            Image icon = Paint(Rect("Spell_" + i, _room.transform, new Vector2(524 + i * 88, -184), new Vector2(64, 64)), Color.white);
            icon.preserveAspect = true;
            EventTrigger hover = icon.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(data => ShowSpellTooltip(index));
            hover.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(data => HideSpellTooltip());
            hover.triggers.Add(exit);
            _spellIcons[i] = icon;
        }
        _previous = Button(_room.transform, "<", new Vector2(-548, 48), new Vector2(48, 64), () => _snap.SnapToPrev(), quiet: true);
        _next = Button(_room.transform, ">", new Vector2(356, 48), new Vector2(48, 64), () => _snap.SnapToNext(), quiet: true);
        _ready = Button(_room.transform, "Готов", new Vector2(656, -272), new Vector2(384, 64), () => _lobby.SetReady(!_localReady, _selected), primary: true);
        _readyLabel = _ready.GetComponentInChildren<Text>();
        Button(_room.transform, "Покинуть комнату", new Vector2(-712, -272), new Vector2(272, 48), () => _connection.Leave(), quiet: true);
        Label(_room.transform, "< / >   или перетаскивание", new Vector2(-96, -216), new Vector2(672, 40), 24, Secondary);
        Button(_room.transform, "Характеристики [Tab]", new Vector2(-96, -280), new Vector2(400, 48), ToggleStats, quiet: true);
        BuildStatsPanel(canvas);
        UpdateClassDescription();
        _room.SetActive(false);
    }

    private void BuildCarousel()
    {
        RectTransform clip = Rect("Panorama", _room.transform, new Vector2(-96, 48), new Vector2(864, 352));
        clip.gameObject.AddComponent<RectMask2D>();
        RectTransform viewport = Rect("Carousel", clip, Vector2.zero, new Vector2(288, 352));
        viewport.gameObject.SetActive(false);
        Paint(viewport, new Color(0, 0, 0, 0));
        _scroll = viewport.gameObject.AddComponent<ScrollRect>();
        _scroll.horizontal = true;
        _scroll.vertical = false;
        _scroll.inertia = false;
        _scroll.movementType = ScrollRect.MovementType.Unrestricted;
        _scroll.scrollSensitivity = 0f;
        _scroll.viewport = viewport;
        RectTransform content = Rect("Cards", viewport, Vector2.zero, new Vector2(864, 352));
        content.anchorMin = content.anchorMax = new Vector2(0, 0.5f);
        content.pivot = new Vector2(0, 0.5f);
        GridLayoutGroup layout = content.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(288, 352);
        layout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        layout.constraintCount = 1;
        _scroll.content = content;
        _cards = new RectTransform[_characters.Length];
        _cardVisuals = new RectTransform[_characters.Length];
        _cardGroups = new CanvasGroup[_characters.Length];
        // One card on either side; ScrollSnap recycles them around the selected card.
        for (int position = 0; position < _characters.Length; position++)
        {
            int index = (position + _characters.Length - 1) % _characters.Length;
            CharacterConfig character = _characters[index];
            RectTransform holder = Rect(character.name, content, Vector2.zero, new Vector2(288, 352));
            holder.gameObject.AddComponent<LayoutElement>();
            RectTransform visual = Rect("Card", holder, Vector2.zero, new Vector2(272, 320));
            Paint(visual, Color.clear);
            Image portrait = Paint(Rect("Portrait", visual, Vector2.zero, new Vector2(224, 224)), Color.white);
            portrait.sprite = character.Preview;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            Button select = visual.gameObject.AddComponent<Button>();
            select.targetGraphic = visual.GetComponent<Image>();
            select.onClick.AddListener(() =>
            {
                if (_localReady || _lobby == null || _lobby.IsStarting) return;
                float offset = holder.position.x - viewport.position.x;
                if (offset < -1f) _snap.SnapToPrev();
                else if (offset > 1f) _snap.SnapToNext();
            });
            _cards[index] = holder;
            _cardVisuals[index] = visual;
            _cardGroups[index] = visual.gameObject.AddComponent<CanvasGroup>();
        }
        _snap = viewport.gameObject.AddComponent<ScrollSnap>();
        _snap.startingIndex = Mathf.Min(1, _characters.Length - 1);
        _snap.wrapAround = true;
        _snap.lerpTimeMilliSeconds = 260f;
        viewport.gameObject.SetActive(true);
        _snap.onLerpComplete.AddListener(CommitSelection);
        _snap.onRelease.AddListener(index =>
        {
            _isMoving = true;
            if (_ready != null) _ready.interactable = false;
        });
    }

    private void CommitSelection()
    {
        _isMoving = false;
        for (int i = 0; i < _cards.Length; i++)
        {
            if (_cards[i] != _snap.CurrentItem)
                continue;
            _selected = i;
            break;
        }
        UpdateClassDescription();
        if (_lobby != null && !_localReady && !_lobby.IsStarting && _lobby.IsCharacterAvailable(_selected, NetworkManager.Singleton.LocalClientId))
            _lobby.SelectCharacter(_selected);
        RefreshRoom();
    }

    private void UpdateClassDescription()
    {
        HideSpellTooltip();
        CharacterConfig character = _characters[_selected];
        _className.text = character.DisplayName;
        _description.text = character.Description + "\n\nЗдоровье  " + character.Stats.MaxHealth + "\nРассудок  " + character.Stats.MaxSanity;
        SpellConfig[] spells = character.Spells;
        for (int i = 0; i < _spellIcons.Length; i++)
        {
            SpellConfig config = i < spells.Length ? spells[i] : null;
            _spellIcons[i].sprite = config != null ? config.SpriteIcon : null;
            _spellIcons[i].gameObject.SetActive(config != null);
        }
        if (_statsPanel.activeSelf) UpdateStatsPanel();
    }

    private void BuildStatsPanel(Transform canvas)
    {
        RectTransform overlay = Rect("CharacterStats", canvas, Vector2.zero, Vector2.zero);
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        Paint(overlay, new Color(0, 0, 0, 0.75f));
        _statsPanel = overlay.gameObject;
        RectTransform panel = Rect("Panel", overlay, Vector2.zero, new Vector2(880, 880));
        Paint(panel, Ink);
        _statsTitle = Label(panel, "", new Vector2(-40, 380), new Vector2(640, 64), 32, Paper, TextAnchor.MiddleLeft);
        Label(panel, "Базовые характеристики", new Vector2(-40, 324), new Vector2(640, 40), 24, Secondary, TextAnchor.MiddleLeft);
        Button(panel, "Закрыть", new Vector2(328, 380), new Vector2(160, 48), ToggleStats, quiet: true);
        RectTransform viewport = Rect("Viewport", panel, new Vector2(0, -40), new Vector2(784, 672));
        Paint(viewport, Color.clear);
        viewport.gameObject.AddComponent<RectMask2D>();
        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 32;
        scroll.viewport = viewport;
        _statsContent = Rect("Rows", viewport, Vector2.zero, new Vector2(784, 1000));
        _statsContent.anchorMin = _statsContent.anchorMax = _statsContent.pivot = new Vector2(0.5f, 1);
        scroll.content = _statsContent;
        _statsNames = Label(_statsContent, "", Vector2.zero, new Vector2(560, 1000), 24, Secondary, TextAnchor.UpperLeft);
        _statsValues = Label(_statsContent, "", Vector2.zero, new Vector2(176, 1000), 24, Paper, TextAnchor.UpperRight);
        foreach (Text text in new[] { _statsNames, _statsValues })
        {
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = text.rectTransform.pivot = new Vector2(0, 1);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.lineSpacing = 1.5f;
        }
        _statsNames.rectTransform.anchoredPosition = new Vector2(8, 0);
        _statsValues.rectTransform.anchoredPosition = new Vector2(600, 0);
        Label(panel, "Колёсико — прокрутка · Tab / Esc — закрыть", new Vector2(0, -408), new Vector2(784, 40), 20, Secondary);
        _statsPanel.SetActive(false);
    }

    private void ToggleStats()
    {
        HideSpellTooltip();
        _statsPanel.SetActive(!_statsPanel.activeSelf);
        if (_statsPanel.activeSelf) UpdateStatsPanel();
    }

    private void UpdateStatsPanel()
    {
        CharacterConfig character = _characters[_selected];
        ActorStatsConfig stats = character.Stats;
        _statsTitle.text = character.DisplayName;
        var rows = new (string Name, float Value, bool Percent)[]
        {
            ("Максимум здоровья", stats.MaxHealth, false),
            ("Восстановление HP/с", stats.HealthRegeneration, false),
            ("Скорость помощи союзникам", stats.ReviveSpeed, false),
            ("Максимум рассудка", stats.MaxSanity, false),
            ("Физический урон", stats.PhysicalDamage, false),
            ("Множитель физ. урона", stats.PhysicalDamageMultiplier, false),
            ("Множитель маг. урона", stats.MagicDamageMultiplier, false),
            ("Скорость атаки", stats.AttackSpeed, false),
            ("Скорость движения", stats.MoveSpeed, false),
            ("Сокращение перезарядки", stats.CooldownReduction, true),
            ("Физическая защита", stats.PhysicalResistance, true),
            ("Магическая защита", stats.MagicResistance, true),
            ("Сопротивление отбрасыванию", stats.ForceResistance, true),
            ("Сопротивление безумию", stats.MadnessResistance, true),
            ("Сопротивление эффектам", stats.EffectResistance, true),
            ("Физический вампиризм", stats.PhysicalLifeSteal, true),
            ("Магический вампиризм", stats.MagicLifeSteal, true),
            ("Дальность обзора", stats.ViewDistance, false),
            ("Угол обзора", stats.ViewAngle, false),
            ("Стабилизация, градусы", stats.Stability, false),
            ("Отдаление камеры", stats.CameraDistance, false),
            ("Скорость каста", stats.CastSpeed, false),
            ("Бонус скорости снаряда", stats.ProjectileSpeed, false),
            ("Множитель скорости снаряда", stats.ProjectileSpeedMultiplier, false),
            ("Бонус дальности снаряда", stats.ProjectileRange, false),
            ("Пронзания снаряда", stats.ProjectilePiercing, false),
            ("Снижение затрат рассудка", stats.SanityCostReduction, true),
            ("Шанс крита", stats.CriticalChance, true),
            ("Критический урон", stats.CriticalDamageMultiplier, true),
            ("Приоритет для врагов", stats.TargetPriority, false)
        };
        _statsNames.text = string.Join("\n", System.Array.ConvertAll(rows, row => row.Name));
        _statsValues.text = string.Join("\n", System.Array.ConvertAll(rows, row => (row.Percent ? row.Value * 100 : row.Value).ToString("0.##") + (row.Percent ? "%" : "")));
        float height = _statsNames.preferredHeight;
        _statsContent.sizeDelta = new Vector2(784, height);
        _statsNames.rectTransform.sizeDelta = new Vector2(560, height);
        _statsValues.rectTransform.sizeDelta = new Vector2(176, height);
        _statsContent.anchoredPosition = Vector2.zero;
    }

    private void ShowSpellTooltip(int index)
    {
        if (Pointer.current == null) return;
        _hoveredSpell = index;
        CharacterConfig character = _characters[_selected];
        SpellConfig config = character.Spells[index];
        string description = config.Description + "\n\n" + config.Stats;
        if (config.IsPassive) description += "\n\nПассивная способность";
        else description += $"\n\nРассудок: {config.SanityCost:0.#}\nКулдаун: {config.CooldownTime:0.#} с";
        if (config.CastTime > 0) description += $"\nВремя чтения: {config.CastTime:0.##} с";
        _spellTooltip.Show(config, config.SpellName, description, Pointer.current.position.ReadValue());
    }

    private void HideSpellTooltip()
    {
        _hoveredSpell = -1;
        if (_spellTooltip != null) _spellTooltip.Hide();
    }

    private void Update()
    {
        if (_connection == null) return;
        if (_lobby != GameLobby.Instance)
        {
            if (_lobby != null) _lobby.Changed -= RefreshRoom;
            _lobby = GameLobby.Instance;
            _selectionSynced = false;
            _localReady = false;
            if (_lobby != null) _lobby.Changed += RefreshRoom;
            RefreshRoom();
        }
        if (_lobby == null)
        {
            HideSpellTooltip();
            _statsPanel.SetActive(false);
            _entry.SetActive(true);
            _room.SetActive(false);
            _cancelConnection.gameObject.SetActive(NetworkManager.Singleton.IsListening);
            _status.text = NetworkManager.Singleton.IsListening ? "Подключение…" : _connection.Error;
            return;
        }
        if (Keyboard.current == null) return;
        if (Keyboard.current.tabKey.wasPressedThisFrame) ToggleStats();
        if (_statsPanel.activeSelf)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame) ToggleStats();
            return;
        }
        if (_localReady || _lobby.IsStarting) return;
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame) _snap.SnapToPrev();
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame) _snap.SnapToNext();
    }

    private void LateUpdate()
    {
        if (_room == null || !_room.activeSelf) return;
        if (_hoveredSpell >= 0 && Pointer.current != null) _spellTooltip.Move(Pointer.current.position.ReadValue());
        for (int i = 0; i < _cards.Length; i++)
        {
            float distance = Mathf.Clamp01(Mathf.Abs(_cards[i].position.x - _scroll.transform.position.x) / (_cards[i].rect.width * _cards[i].lossyScale.x));
            _cardVisuals[i].localScale = Vector3.one * Mathf.Lerp(1f, 0.73f, distance);
            _cardVisuals[i].anchoredPosition = new Vector2(0, -distance * 32f);
            _cardGroups[i].alpha = Mathf.Lerp(1f, 0.55f, distance);
        }
    }

    private void RefreshRoom()
    {
        if (_room == null || _lobby == null) return;
        _entry.SetActive(false);
        _room.SetActive(true);
        _roster.text = "";
        for (int i = 0; i < _lobby.Count; i++)
        {
            LobbyPlayer player = _lobby.GetPlayer(i);
            string state = player.IsReady ? "<color=#96D0CF>готов</color>" : "выбирает";
            if (i > 0) _roster.text += "\n\n";
            string className = _characters[player.CharacterIndex].DisplayName;
            if (className.Length > 18) className = className.Substring(0, 17) + "…";
            _roster.text += "Player" + (player.ClientId + 1) + "\n" + className + " · " + state;
            if (player.ClientId == NetworkManager.Singleton.LocalClientId)
            {
                _localReady = player.IsReady;
                if (!_selectionSynced)
                {
                    _selectionSynced = true;
                    StartCoroutine(ShowInitialSelection());
                }
            }
        }
        bool canChoose = !_localReady && !_lobby.IsStarting;
        _scroll.enabled = canChoose;
        _previous.interactable = _next.interactable = canChoose;
        bool available = _lobby.IsCharacterAvailable(_selected, NetworkManager.Singleton.LocalClientId);
        _ready.interactable = !_lobby.IsStarting && !_isMoving && available;
        _readyLabel.text = !available ? "Класс занят" : _localReady ? "Не готов" : "Готов";
        _status.text = _lobby.IsStarting ? "Все готовы. Загружаем карту…" : !available ? "Этот класс уже выбран другим игроком" : _localReady ? "Ожидание остальных игроков" : "Карта запустится, когда все будут готовы";
    }

    private IEnumerator ShowInitialSelection()
    {
        _isMoving = true;
        yield return null;
        if (_lobby == null)
            yield break;
        for (int i = 0; i < _lobby.Count; i++)
        {
            LobbyPlayer player = _lobby.GetPlayer(i);
            if (player.ClientId != NetworkManager.Singleton.LocalClientId)
                continue;
            _selected = player.CharacterIndex;
            UpdateClassDescription();
            Canvas.ForceUpdateCanvases();
            _snap.SnapToIndex(_cards[_selected].GetSiblingIndex());
            yield break;
        }
        _isMoving = false;
    }

    private void OnDestroy()
    {
        if (_lobby != null) _lobby.Changed -= RefreshRoom;
    }

    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
    private Image Paint(RectTransform rect, Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>(); image.color = color; return image;
    }
    private Text Label(Transform parent, string text, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        Text label = Rect("Label", parent, position, size).gameObject.AddComponent<Text>();
        label.font = _font; label.text = text; label.fontSize = fontSize; label.color = color;
        label.alignment = alignment; label.raycastTarget = false;
        return label;
    }
    private Button Button(Transform parent, string text, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, bool primary = false, bool quiet = false)
    {
        RectTransform rect = Rect(text, parent, position, size);
        Image background = Paint(rect, primary ? Paper : quiet ? Color.clear : new Color32(38, 44, 40, 255));
        Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = background;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.84f, 0.94f, 0.93f);
        colors.pressedColor = new Color(0.65f, 0.8f, 0.78f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(action);
        Text label = Label(rect, text, Vector2.zero, size - new Vector2(16, 8), 28, primary ? Ink : Paper);
        if (quiet) button.targetGraphic = label;
        return button;
    }
}
