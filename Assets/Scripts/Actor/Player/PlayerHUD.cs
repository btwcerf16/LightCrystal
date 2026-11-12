using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHUD : NetworkBehaviour
{
    [SerializeField] private Sprite _currencyIcon;
    private ActorWallet _wallet;
    private NetworkPlayerWallet _networkWallet;
    private Text _currencyText;
    private Text _hintText;
    private CanvasGroup _hintGroup;
    private RectTransform _hintRect;
    private Sequence _hintAnimation;
    private GameObject _canvas;
    private EventNotificationsUI _eventNotifications;
    private RectTransform _root;
    private RectTransform _waveTimer;
    private Text _waveTimerText;
    private Text _waveTimerTitle;
    private bool _shownPaused;
    private int _shownSeconds = -1;
    private WaveDirector _waveDirector;
    private ActorArsenal _arsenal;
    private RectTransform _comboRect;
    private Text _comboText;
    private CanvasGroup _comboGroup;
    private Tween _comboPulse;
    private Tween _comboFade;
    private readonly NetworkVariable<int> _combo = new(0,
        NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<double> _waveStartsAt = new(0,
        NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> _pausedCalmRemaining = new(-1f,
        NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

    private float ArsenalWidth => GetComponent<ArsenalUI>().Width;
    private float CenterWidth => GetComponent<PlayerVitalsUI>().Width;
    private float InventoryWidth => GetComponent<InventoryUI>().Width;
    private float Height => Mathf.Max(160f, GetComponent<InventoryUI>().Height, GetComponent<ArsenalUI>().Height);

    public RectTransform ArsenalPanel => Section("ArsenalPanel", Vector2.zero, new Vector2(ArsenalWidth, Height));
    public RectTransform VitalsPanel => Section("VitalsPanel", new Vector2(ArsenalWidth, 0f), new Vector2(CenterWidth, 68f));
    public RectTransform SpellPanel => Section("Spells", new Vector2(ArsenalWidth, 68f), new Vector2(CenterWidth, Height - 68f));
    public RectTransform InventoryPanel => Section("InventoryPanel", new Vector2(ArsenalWidth + CenterWidth, 0f), new Vector2(InventoryWidth, Height));

    public override void OnNetworkSpawn()
    {
        _arsenal = GetComponent<ActorArsenal>();
        if (IsOwner)
            _combo.OnValueChanged += ReceiveCombo;
        if (!IsServer)
            return;
        _waveDirector = NetworkManager.GetComponent<WaveDirector>();
        if (_waveDirector == null)
            return;
        _waveDirector.CalmStarted += PublishWaveTimer;
        _waveDirector.WaveStarted += PublishWaveTimer;
        _waveDirector.PauseChanged += PublishWaveTimer;
        PublishWaveTimer();
    }

    private void PublishWaveTimer()
    {
        if (!IsSpawned || !IsServer)
            return;
        _pausedCalmRemaining.Value = _waveDirector.IsPaused ? _waveDirector.CalmTimeRemaining : -1f;
        _waveStartsAt.Value = _waveDirector.CurrentState is CalmState
            ? NetworkManager.ServerTime.Time + _waveDirector.CalmTimeRemaining : 0;
    }

    private void Update()
    {
        if (IsSpawned && IsServer)
        {
            int count = _arsenal.ActiveWeapon != null ? _arsenal.ActiveWeapon.ComboCount : 0;
            if (_combo.Value != count)
                _combo.Value = count;
        }
        if (!IsSpawned || !IsOwner || _waveTimer == null)
            return;
        bool paused = _pausedCalmRemaining.Value >= 0f;
        double remaining = paused ? _pausedCalmRemaining.Value : _waveStartsAt.Value - NetworkManager.ServerTime.Time;
        bool visible = paused || (_waveStartsAt.Value > 0 && remaining > 0);
        _waveTimer.gameObject.SetActive(visible);
        if (!visible)
            return;
        int seconds = Mathf.CeilToInt((float)remaining);
        if (seconds == _shownSeconds && paused == _shownPaused)
            return;
        _shownSeconds = seconds;
        _shownPaused = paused;
        _waveTimerTitle.text = paused ? "Волны на паузе" : "До следующей волны";
        _waveTimerText.text = paused && _waveStartsAt.Value == 0 ? "Пауза" : $"{seconds / 60:00}:{seconds % 60:00}";
        _waveTimerText.color = !paused && seconds <= 5 ? new Color(1f, 0.48f, 0.3f) : HudStyle.Text;
    }

    private RectTransform Section(string name, Vector2 position, Vector2 size)
    {
        CreateRoot();
        Transform existing = _root.Find(name);
        RectTransform rect = existing != null ? (RectTransform)existing
            : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(_root, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    public void RefreshLayout()
    {
        if (_root == null)
            return;
        _root.sizeDelta = new Vector2(ArsenalWidth + CenterWidth + InventoryWidth, Height);
        Section("ArsenalPanel", Vector2.zero, new Vector2(ArsenalWidth, Height));
        Section("VitalsPanel", new Vector2(ArsenalWidth, 0f), new Vector2(CenterWidth, 68f));
        Section("Spells", new Vector2(ArsenalWidth, 68f), new Vector2(CenterWidth, Height - 68f));
        Section("InventoryPanel", new Vector2(ArsenalWidth + CenterWidth, 0f), new Vector2(InventoryWidth, Height));
        RectTransform currency = Section("Currency", new Vector2(ArsenalWidth + CenterWidth, Height - 24f), new Vector2(InventoryWidth, 24f));
        currency.SetAsLastSibling();
    }

    private void CreateRoot()
    {
        if (_canvas != null)
            return;
        _canvas = new GameObject("PlayerHUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas.transform.SetParent(transform, false);
        Canvas canvas = _canvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = _canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        _root = new GameObject("HUD", typeof(RectTransform), typeof(Image), typeof(Outline)).GetComponent<RectTransform>();
        _root.SetParent(_canvas.transform, false);
        _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0f);
        _root.pivot = new Vector2(0.5f, 0f);
        _root.sizeDelta = new Vector2(ArsenalWidth + CenterWidth + InventoryWidth, Height);
        _root.anchoredPosition = new Vector2(0f, 16f);
        _root.GetComponent<Image>().color = HudStyle.Surface;
        Outline border = _root.GetComponent<Outline>();
        border.effectColor = HudStyle.Line;
        border.effectDistance = new Vector2(1f, -1f);
        CreateWalletUI();
        CreateWaveTimer();
        if (IsClient && IsOwner)
        {
            var bossBars = new GameObject("BossBars", typeof(RectTransform), typeof(BossBarsUI));
            bossBars.transform.SetParent(_canvas.transform, false);
            bossBars.GetComponent<BossBarsUI>().Initialize(NetworkManager);
        }
        CreateComboUI();
        _eventNotifications = new GameObject("EventNotifications", typeof(RectTransform)).AddComponent<EventNotificationsUI>();
        _eventNotifications.Initialize(_canvas.transform);
        ShopSpawnArea area = FindFirstObjectByType<ShopSpawnArea>();
        Crystal crystal = FindFirstObjectByType<Crystal>();
        if (IsClient && IsOwner && area != null && area.Ground != null && crystal != null)
        {
            GameObject minimap = new GameObject("Minimap", typeof(RectTransform));
            minimap.transform.SetParent(_canvas.transform, false);
            minimap.AddComponent<PlayerMinimapUI>().Initialize(transform, area.Ground, crystal.transform);
        }
    }

    public void SetEventsMenuOpen(bool isOpen)
    {
        if (isOpen)
            CreateRoot();
        if (_eventNotifications != null)
            _eventNotifications.SetMenuOpen(isOpen);
    }

    public static void NotifyPlayers(NetworkManager manager, string message, Color textColor)
    {
        if (!manager.IsServer)
            return;
        foreach (NetworkClient client in manager.ConnectedClientsList)
            if (client.PlayerObject != null)
                client.PlayerObject.GetComponent<PlayerHUD>().ShowEventRpc(message, textColor);
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void ShowEventRpc(string message, Color textColor)
    {
        CreateRoot();
        _eventNotifications.Show(message, textColor);
    }

    private void CreateComboUI()
    {
        _comboRect = new GameObject("Combo", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
        _comboRect.SetParent(_canvas.transform, false);
        _comboRect.anchorMin = _comboRect.anchorMax = _comboRect.pivot = new Vector2(0f, 1f);
        _comboRect.anchoredPosition = new Vector2(260f, -18f);
        _comboRect.sizeDelta = new Vector2(240f, 48f);
        _comboGroup = _comboRect.GetComponent<CanvasGroup>();
        _comboGroup.alpha = 0f;
        _comboGroup.blocksRaycasts = false;
        _comboText = CreateLabel(_comboRect, "Count", Color.white);
        _comboText.alignment = TextAnchor.MiddleLeft;
        _comboText.fontSize = 28;
        _comboText.fontStyle = FontStyle.Bold;
        ReceiveCombo(0, _combo.Value);
    }

    private void ReceiveCombo(int previous, int current)
    {
        if (_comboRect == null)
            return;
        _comboPulse?.Kill();
        _comboFade?.Kill();
        if (current <= 0)
        {
            _comboFade = _comboGroup.DOFade(0f, 0.2f);
            return;
        }
        _comboText.text = $"КОМБО ×{current}";
        Color red = new Color(1f, 0.15f, 0.12f);
        _comboText.color = current <= 25
            ? Color.Lerp(Color.white, red, (current - 1f) / 24f)
            : Color.Lerp(red, new Color(0.55f, 0.04f, 0.12f), (current - 25f) / 25f);
        _comboGroup.alpha = 1f;
        _comboRect.localScale = Vector3.one;
        _comboPulse = _comboRect.DOPunchScale(new Vector3(0.18f, 0.18f, 0f), 0.22f, 3, 0.6f);
    }

    private void CreateWaveTimer()
    {
        _waveTimer = new GameObject("WaveTimer", typeof(RectTransform), typeof(Image), typeof(Outline)).GetComponent<RectTransform>();
        _waveTimer.SetParent(_canvas.transform, false);
        _waveTimer.anchorMin = _waveTimer.anchorMax = _waveTimer.pivot = new Vector2(0.5f, 1f);
        _waveTimer.anchoredPosition = new Vector2(0f, -24f);
        _waveTimer.sizeDelta = new Vector2(176f, 64f);
        Image background = _waveTimer.GetComponent<Image>();
        background.color = HudStyle.Surface;
        background.raycastTarget = false;
        Outline border = _waveTimer.GetComponent<Outline>();
        border.effectColor = HudStyle.Line;
        border.effectDistance = new Vector2(1f, -1f);

        Text title = CreateLabel(_waveTimer, "Title", HudStyle.Muted);
        _waveTimerTitle = title;
        title.text = "До следующей волны";
        title.fontSize = 12;
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = Vector2.one;
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.sizeDelta = new Vector2(0f, 18f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -9f);

        _waveTimerText = CreateLabel(_waveTimer, "Time", Color.white);
        _waveTimerText.fontSize = 30;
        _waveTimerText.rectTransform.anchorMin = _waveTimerText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _waveTimerText.rectTransform.sizeDelta = new Vector2(180f, 38f);
        _waveTimerText.rectTransform.anchoredPosition = new Vector2(0f, -10f);
        _shownSeconds = -1;
        _waveTimer.gameObject.SetActive(false);
    }

    private void CreateWalletUI()
    {
        _wallet = GetComponent<ActorWallet>();
        _networkWallet = GetComponent<NetworkPlayerWallet>();
        RectTransform panel = Section("Currency", new Vector2(ArsenalWidth + CenterWidth, Height - 24f), new Vector2(InventoryWidth, 24f));
        panel.gameObject.AddComponent<Image>().color = HudStyle.Surface;
        var border = panel.gameObject.AddComponent<Outline>();
        border.effectColor = HudStyle.Line;
        border.effectDistance = new Vector2(1f, -1f);

        RectTransform coin = new GameObject("Coin", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        coin.SetParent(panel, false);
        coin.anchorMin = coin.anchorMax = new Vector2(0f, 0.5f);
        coin.anchoredPosition = new Vector2(16f, 0f);
        coin.sizeDelta = new Vector2(16f, 16f);
        coin.GetComponent<Image>().sprite = _currencyIcon;
        coin.GetComponent<Image>().raycastTarget = false;
        _currencyText = CreateLabel(panel, "CurrencyValue", new Color(0.95f, 0.79f, 0.33f));
        _currencyText.alignment = TextAnchor.MiddleLeft;
        _currencyText.fontSize = 14;
        _currencyText.rectTransform.offsetMin = new Vector2(32f, 0f);
        _wallet.CurrencyChanged += UpdateCurrency;
        _networkWallet.HintRequested += ShowHint;
        UpdateCurrency(_wallet.Currency);

        _hintRect = Section("Hint", new Vector2(ArsenalWidth, Height + 38f), new Vector2(CenterWidth, 36f));
        _hintRect.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.025f, 0.02f, 0.9f);
        _hintGroup = _hintRect.gameObject.AddComponent<CanvasGroup>();
        _hintGroup.alpha = 0f;
        _hintGroup.interactable = false;
        _hintGroup.blocksRaycasts = false;
        _hintText = CreateLabel(_hintRect, "HintText", new Color(1f, 0.45f, 0.35f));
        _hintText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _hintText.rectTransform.offsetMin = new Vector2(12f, 0f);
        _hintText.rectTransform.offsetMax = new Vector2(-12f, 0f);
    }

    private Text CreateLabel(RectTransform parent, string name, Color color)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(parent, false);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 18;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    private void UpdateCurrency(int currency)
    {
        _currencyText.text = currency.ToString();
    }

    private void ShowHint(string message)
    {
        _hintAnimation?.Kill();
        _hintText.text = message;
        _hintGroup.alpha = 0f;
        float width = Mathf.Max(CenterWidth, _hintText.preferredWidth + 24f);
        _hintRect.sizeDelta = new Vector2(width, 36f);
        _hintRect.anchoredPosition = new Vector2(ArsenalWidth + (CenterWidth - width) * 0.5f, Height + 38f);
        _hintAnimation = DOTween.Sequence()
            .Append(_hintGroup.DOFade(1f, 0.12f))
            .Join(_hintRect.DOAnchorPosY(Height + 46f, 0.12f))
            .AppendInterval(1.08f)
            .Append(_hintGroup.DOFade(0f, 0.3f))
            .SetUpdate(true);
    }

    public override void OnNetworkDespawn()
    {
        _combo.OnValueChanged -= ReceiveCombo;
        if (_waveDirector != null)
        {
            _waveDirector.CalmStarted -= PublishWaveTimer;
            _waveDirector.WaveStarted -= PublishWaveTimer;
            _waveDirector.PauseChanged -= PublishWaveTimer;
        }
        Hide();
    }
    public override void OnLostOwnership() => Hide();
    private void OnDisable() => Hide();

    private void Hide()
    {
        if (_wallet != null)
            _wallet.CurrencyChanged -= UpdateCurrency;
        if (_networkWallet != null)
            _networkWallet.HintRequested -= ShowHint;
        _hintAnimation?.Kill();
        _comboPulse?.Kill();
        _comboFade?.Kill();
        _comboRect = null;
        if (_canvas == null)
            return;
        _canvas.SetActive(false);
        Destroy(_canvas);
        _canvas = null;
        _root = null;
        _waveTimer = null;
    }
}
