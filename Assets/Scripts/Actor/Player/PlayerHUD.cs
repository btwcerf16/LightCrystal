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
    private readonly object _currencyGainTweens = new();
    private Text _performanceText;
    private float _performanceElapsed;
    private int _performanceFrames;
    private Text _hintText;
    private CanvasGroup _hintGroup;
    private RectTransform _hintRect;
    private Sequence _hintAnimation;
    private GameObject _canvas;
    private EventNotificationsUI _eventNotifications;
    private PlayerMinimapUI _minimap;
    private RectTransform _root;
    private RectTransform _waveTimer;
    private Text _waveTimerText;
    private Text _waveTimerTitle;
    private bool _shownPaused;
    private int _shownSeconds = -1;
    private int _shownWaveNumber;
    private WaveDirector _waveDirector;
    private NetworkPlayerArsenal _networkArsenal;
    private RectTransform _weaponChargePanel;
    private readonly RectTransform[] _weaponChargeFills = new RectTransform[4];
    private float _shownWeaponCharge = -1f;
    private readonly NetworkVariable<double> _waveStartsAt = new(0,
        NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> _activeWaveNumber = new(0,
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

    private bool _minimalLayout;

    public void SetMinimalLayout()
    {
        CreateRoot();
        _minimalLayout = true;
        _root.GetComponent<Image>().enabled = false;
        SetCurrencyVisible(false);
        if (_performanceText != null)
            _performanceText.transform.parent.gameObject.SetActive(false);
    }

    public void SetCurrencyVisible(bool visible) => _currencyText.transform.parent.gameObject.SetActive(visible);

    public override void OnNetworkSpawn()
    {
        _networkArsenal = GetComponent<NetworkPlayerArsenal>();
        if (!IsServer)
            return;
        _waveDirector = NetworkManager.GetComponent<WaveDirector>();
        if (_waveDirector == null)
            return;
        _waveDirector.CalmStarted += PublishWaveTimer;
        _waveDirector.WaveStarted += PublishWaveTimer;
        _waveDirector.PauseChanged += PublishWaveTimer;
        _waveDirector.CycleStopped += PublishWaveTimer;
        PublishWaveTimer();
    }

    private void PublishWaveTimer()
    {
        if (!IsSpawned || !IsServer)
            return;
        bool isCalm = _waveDirector.CurrentState is CalmState;
        _activeWaveNumber.Value = _waveDirector.CurrentState is WaveStartState ? _waveDirector.WaveCount : 0;
        _pausedCalmRemaining.Value = isCalm && _waveDirector.IsPaused ? _waveDirector.CalmTimeRemaining : -1f;
        _waveStartsAt.Value = isCalm
            ? NetworkManager.ServerTime.Time + _waveDirector.CalmTimeRemaining : 0;
    }

    private void Update()
    {
        if (IsSpawned && IsOwner && _performanceText != null && !_minimalLayout)
            UpdatePerformanceStats();

        if (!IsSpawned || !IsOwner || _waveTimer == null)
            return;
        int waveNumber = _activeWaveNumber.Value;
        bool inWave = waveNumber > 0;
        bool paused = _pausedCalmRemaining.Value >= 0f;
        double remaining = paused ? _pausedCalmRemaining.Value : _waveStartsAt.Value - NetworkManager.ServerTime.Time;
        bool visible = inWave || paused || _waveStartsAt.Value > 0;
        _waveTimer.gameObject.SetActive(visible);
        if (!visible)
            return;
        int seconds = inWave ? 0 : Mathf.CeilToInt(Mathf.Max(0f, (float)remaining));
        if (waveNumber == _shownWaveNumber && seconds == _shownSeconds && paused == _shownPaused)
            return;
        _shownWaveNumber = waveNumber;
        _shownSeconds = seconds;
        _shownPaused = paused;
        _waveTimerTitle.gameObject.SetActive(!inWave);
        _waveTimerText.rectTransform.anchoredPosition = inWave ? Vector2.zero : new Vector2(0f, -10f);
        _waveTimerTitle.text = paused ? "Волны на паузе" : "До следующей волны";
        _waveTimerText.text = inWave ? $"Волна {waveNumber}" : $"{seconds / 60:00}:{seconds % 60:00}";
        _waveTimerText.color = !inWave && !paused && seconds <= 5 ? new Color(1f, 0.48f, 0.3f) : HudStyle.Text;
    }

    private void UpdatePerformanceStats()
    {
        bool show = LocalPlayerSettings.ShowPerformanceStats;
        _performanceText.transform.parent.gameObject.SetActive(show);
        if (!show)
        {
            _performanceElapsed = 0f;
            _performanceFrames = 0;
            return;
        }

        _performanceElapsed += Time.unscaledDeltaTime;
        _performanceFrames++;
        if (_performanceElapsed < 0.5f)
            return;

        int fps = Mathf.RoundToInt(_performanceFrames / _performanceElapsed);
        ulong ping = IsServer ? 0 : NetworkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(Unity.Netcode.NetworkManager.ServerClientId);
        _performanceText.text = $"FPS {fps}   ·   Пинг {ping} мс";
        _performanceElapsed = 0f;
        _performanceFrames = 0;
    }

    private void LateUpdate()
    {
        if (!IsSpawned || !IsOwner || _weaponChargePanel == null)
            return;

        float chargeProgress = _networkArsenal.WeaponCharge;
        Camera worldCamera = chargeProgress >= 0f ? Camera.main : null;
        _weaponChargePanel.gameObject.SetActive(worldCamera != null);
        if (worldCamera == null)
            return;

        Vector3 screenPosition = worldCamera.WorldToScreenPoint(transform.position + Vector3.up * 0.55f);
        RectTransform canvasRect = (RectTransform)_weaponChargePanel.parent;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 canvasPosition);
        _weaponChargePanel.anchoredPosition = canvasPosition;

        if (_shownWeaponCharge == chargeProgress)
            return;

        _shownWeaponCharge = chargeProgress;
        for (int i = 0; i < _weaponChargeFills.Length; i++)
        {
            float squareFill = Mathf.Clamp01(chargeProgress * _weaponChargeFills.Length - i);
            _weaponChargeFills[i].anchorMax = new Vector2(squareFill, 1f);
        }
    }

    private void CreateWeaponChargeUI()
    {
        const float squareSize = 10f;
        const float spacing = 3f;
        float panelWidth = _weaponChargeFills.Length * (squareSize + spacing) - spacing;
        _weaponChargePanel = new GameObject("WeaponCharge", typeof(RectTransform)).GetComponent<RectTransform>();
        _weaponChargePanel.SetParent(_canvas.transform, false);
        _weaponChargePanel.anchorMin = _weaponChargePanel.anchorMax = new Vector2(0.5f, 0.5f);
        _weaponChargePanel.pivot = new Vector2(0.5f, 0f);
        _weaponChargePanel.sizeDelta = new Vector2(panelWidth, squareSize);

        for (int i = 0; i < _weaponChargeFills.Length; i++)
        {
            Image square = new GameObject("Square", typeof(Image), typeof(Outline)).GetComponent<Image>();
            square.transform.SetParent(_weaponChargePanel, false);
            square.color = HudStyle.Surface;
            square.raycastTarget = false;
            square.GetComponent<Outline>().effectColor = HudStyle.Muted;
            RectTransform squareRect = square.rectTransform;
            squareRect.anchorMin = squareRect.anchorMax = squareRect.pivot = Vector2.zero;
            squareRect.anchoredPosition = new Vector2(i * (squareSize + spacing), 0f);
            squareRect.sizeDelta = Vector2.one * squareSize;

            Image fill = new GameObject("Fill", typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(squareRect, false);
            fill.color = HudStyle.Text;
            fill.raycastTarget = false;
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.up;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            _weaponChargeFills[i] = fillRect;
        }
        _shownWeaponCharge = -1f;
        _weaponChargePanel.gameObject.SetActive(false);
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
        if (_performanceText != null)
            Section("PerformanceStats", new Vector2(ArsenalWidth + CenterWidth, Height + 4f), new Vector2(InventoryWidth, 24f));
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
            RectTransform performance = Section("PerformanceStats", new Vector2(ArsenalWidth + CenterWidth, Height + 4f), new Vector2(InventoryWidth, 24f));
            Image performanceBackground = performance.gameObject.AddComponent<Image>();
            performanceBackground.color = HudStyle.Surface;
            performanceBackground.raycastTarget = false;
            _performanceText = CreateLabel(performance, "Stats", HudStyle.Text);
            _performanceText.fontSize = 13;
            performance.gameObject.SetActive(false);
            CreateWeaponChargeUI();
            var bossBars = new GameObject("BossBars", typeof(RectTransform), typeof(BossBarsUI));
            bossBars.transform.SetParent(_canvas.transform, false);
            bossBars.GetComponent<BossBarsUI>().Initialize(NetworkManager);
        }
        _eventNotifications = new GameObject("EventNotifications", typeof(RectTransform)).AddComponent<EventNotificationsUI>();
        _eventNotifications.Initialize(_canvas.transform);
        ShopSpawnArea area = FindFirstObjectByType<ShopSpawnArea>();
        Crystal crystal = FindFirstObjectByType<Crystal>();
        if (IsClient && IsOwner && area != null && area.Ground != null && crystal != null)
        {
            GameObject minimap = new GameObject("Minimap", typeof(RectTransform));
            minimap.transform.SetParent(_canvas.transform, false);
            _minimap = minimap.AddComponent<PlayerMinimapUI>();
            _minimap.Initialize(transform, area.Ground, crystal.transform);
        }
    }

    public static void RevealWaveEvent(NetworkObject waveEvent)
    {
        NetworkManager manager = waveEvent.NetworkManager;
        double revealedUntil = manager.ServerTime.Time + 10d;
        foreach (NetworkClient client in manager.ConnectedClientsList)
            client.PlayerObject?.GetComponent<PlayerHUD>()?.RevealWaveEventRpc(waveEvent.NetworkObjectId, revealedUntil);
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void RevealWaveEventRpc(ulong objectId, double revealedUntil)
    {
        CreateRoot();
        if (_minimap != null)
            _minimap.RevealWaveEvent(objectId, revealedUntil);
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

    private void CreateWaveTimer()
    {
        _waveTimer = new GameObject("WaveTimer", typeof(RectTransform), typeof(Image), typeof(Outline)).GetComponent<RectTransform>();
        _waveTimer.SetParent(_canvas.transform, false);
        _waveTimer.anchorMin = _waveTimer.anchorMax = _waveTimer.pivot = new Vector2(0.5f, 1f);
        _waveTimer.anchoredPosition = new Vector2(0f, -24f);
        _waveTimer.sizeDelta = new Vector2(176f, 64f);
        _waveTimer.localScale = new Vector3(0.7f, 0.7f, 1f);
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
        _networkWallet.CurrencyGainReceived += PlayCurrencyGain;
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
        text.font = HudStyle.PixelFont;
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

    private void PlayCurrencyGain(int amount, Vector2 sourcePosition)
    {
        if (_currencyIcon == null)
            return;

        int coinCount = Mathf.Min(amount, 12);
        float coinScale = 0.28f / _currencyIcon.bounds.size.y;
        for (int i = 0; i < coinCount; i++)
        {
            Vector2 arc = Vector2.up * (0.36f + 0.04f * (i % 3));
            SpriteRenderer coin = new GameObject("Gold Gain Coin").AddComponent<SpriteRenderer>();
            coin.transform.SetParent(transform, false);
            coin.transform.localScale = Vector3.one * coinScale;
            coin.sprite = _currencyIcon;
            coin.sortingOrder = 5;
            coin.enabled = false;

            DOVirtual.Float(0f, 1f, 0.48f, progress =>
                {
                    coin.enabled = true;
                    Vector2 destination = (Vector2)transform.position + Vector2.up * 0.15f;
                    Vector2 position = Vector2.Lerp(sourcePosition, destination, progress) + arc * Mathf.Sin(progress * Mathf.PI);
                    coin.transform.position = new Vector3(position.x, position.y, transform.position.z);
                    coin.transform.localScale = Vector3.one * (coinScale * Mathf.Lerp(1f, 0.35f, progress));
                })
                .SetEase(Ease.InQuad)
                .SetDelay(i * 0.045f)
                .SetTarget(_currencyGainTweens)
                .SetLink(coin.gameObject)
                .OnKill(() =>
                {
                    if (coin != null)
                        Destroy(coin.gameObject);
                });
        }
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
        if (_waveDirector != null)
        {
            _waveDirector.CalmStarted -= PublishWaveTimer;
            _waveDirector.WaveStarted -= PublishWaveTimer;
            _waveDirector.PauseChanged -= PublishWaveTimer;
            _waveDirector.CycleStopped -= PublishWaveTimer;
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
        {
            _networkWallet.HintRequested -= ShowHint;
            _networkWallet.CurrencyGainReceived -= PlayCurrencyGain;
        }
        DOTween.Kill(_currencyGainTweens);
        _hintAnimation?.Kill();
        if (_canvas == null)
            return;
        _canvas.SetActive(false);
        Destroy(_canvas);
        _canvas = null;
        _root = null;
        _minimap = null;
        _waveTimer = null;
        _performanceText = null;
        _performanceElapsed = 0f;
        _performanceFrames = 0;
        _weaponChargePanel = null;
    }
}
