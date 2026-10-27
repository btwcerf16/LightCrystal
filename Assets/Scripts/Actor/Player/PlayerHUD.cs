using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHUD : NetworkBehaviour
{
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
    }

    public override void OnNetworkDespawn() => Hide();
    public override void OnLostOwnership() => Hide();
    private void OnDisable() => Hide();

    private void Hide()
    {
        if (_canvas == null)
            return;
        _canvas.SetActive(false);
        Destroy(_canvas);
        _canvas = null;
        _root = null;
    }
}
