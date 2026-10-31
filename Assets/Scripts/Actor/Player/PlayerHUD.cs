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
    private RectTransform _root;

    private float ArsenalWidth => GetComponent<ArsenalUI>().Width;
    private float CenterWidth => GetComponent<PlayerVitalsUI>().Width;
    private float InventoryWidth => GetComponent<InventoryUI>().Width;
    private float Height => Mathf.Max(176f, GetComponent<InventoryUI>().Height, GetComponent<ArsenalUI>().Height);

    public RectTransform ArsenalPanel => Section("ArsenalPanel", Vector2.zero, new Vector2(ArsenalWidth, Height));
    public RectTransform VitalsPanel => Section("VitalsPanel", new Vector2(ArsenalWidth, 0f), new Vector2(CenterWidth, 68f));
    public RectTransform SpellPanel => Section("Spells", new Vector2(ArsenalWidth, 68f), new Vector2(CenterWidth, Height - 68f));
    public RectTransform InventoryPanel => Section("InventoryPanel", new Vector2(ArsenalWidth + CenterWidth, 0f), new Vector2(InventoryWidth, Height));

    private RectTransform Section(string name, Vector2 position, Vector2 size)
    {
        CreateRoot();
        Transform existing = _root.Find(name);
        if (existing != null)
            return (RectTransform)existing;
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(_root, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
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
        _root.GetComponent<Image>().color = new Color(0.025f, 0.024f, 0.022f);
        Outline border = _root.GetComponent<Outline>();
        border.effectColor = new Color(0.32f, 0.26f, 0.16f);
        border.effectDistance = new Vector2(2f, -2f);
        CreateWalletUI();
    }

    private void CreateWalletUI()
    {
        _wallet = GetComponent<ActorWallet>();
        _networkWallet = GetComponent<NetworkPlayerWallet>();
        RectTransform panel = Section("Currency", new Vector2(ArsenalWidth + CenterWidth, Height), new Vector2(InventoryWidth, 32f));
        panel.gameObject.AddComponent<Image>().color = new Color(0.065f, 0.055f, 0.025f);
        var border = panel.gameObject.AddComponent<Outline>();
        border.effectColor = new Color(0.32f, 0.26f, 0.16f);
        border.effectDistance = new Vector2(1f, -1f);

        RectTransform coin = new GameObject("Coin", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        coin.SetParent(panel, false);
        coin.anchorMin = coin.anchorMax = new Vector2(0f, 0.5f);
        coin.anchoredPosition = new Vector2(20f, 0f);
        coin.sizeDelta = new Vector2(24f, 24f);
        coin.GetComponent<Image>().sprite = _currencyIcon;
        coin.GetComponent<Image>().raycastTarget = false;
        _currencyText = CreateLabel(panel, "CurrencyValue", new Color(0.95f, 0.79f, 0.33f));
        _currencyText.alignment = TextAnchor.MiddleLeft;
        _currencyText.rectTransform.offsetMin = new Vector2(40f, 0f);
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
        _hintRect.anchoredPosition = new Vector2(ArsenalWidth, Height + 38f);
        _hintAnimation = DOTween.Sequence()
            .Append(_hintGroup.DOFade(1f, 0.12f))
            .Join(_hintRect.DOAnchorPosY(Height + 46f, 0.12f))
            .AppendInterval(1.08f)
            .Append(_hintGroup.DOFade(0f, 0.3f))
            .SetUpdate(true);
    }

    public override void OnNetworkDespawn() => Hide();
    public override void OnLostOwnership() => Hide();
    private void OnDisable() => Hide();

    private void Hide()
    {
        if (_wallet != null)
            _wallet.CurrencyChanged -= UpdateCurrency;
        if (_networkWallet != null)
            _networkWallet.HintRequested -= ShowHint;
        _hintAnimation?.Kill();
        if (_canvas == null)
            return;
        _canvas.SetActive(false);
        Destroy(_canvas);
        _canvas = null;
        _root = null;
    }
}
