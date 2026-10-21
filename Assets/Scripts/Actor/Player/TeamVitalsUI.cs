using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public sealed class TeamVitalsUI : NetworkBehaviour
{
    [SerializeField] private float _leftMargin = 18f;
    [SerializeField] private float _width = 220f;
    private readonly Dictionary<ulong, Row> _rows = new();
    private readonly List<NetworkActorVitals> _players = new();
    private readonly List<ulong> _removed = new();
    private GameObject _canvas;
    private RectTransform _panel;
    private Font _font;
    private float _nextRefresh;

    private sealed class Row
    {
        public NetworkActorVitals Source;
        public GameObject Root;
        public Text Name;
        public RectTransform HealthFill;
        public RectTransform SanityFill;
        public Text HealthText;
        public Text SanityText;
        public Action<Vector4> Handler;
    }

    public override void OnNetworkSpawn() => Show();
    public override void OnGainedOwnership() => Show();
    public override void OnLostOwnership() => Hide();
    public override void OnNetworkDespawn() => Hide();
    private void OnEnable() => Show();
    private void OnDisable() => Hide();

    private void Show()
    {
        if (!isActiveAndEnabled || !IsSpawned || !IsClient || !IsOwner || _canvas != null)
            return;

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _canvas = new GameObject("TeamVitalsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        _canvas.transform.SetParent(transform, false);
        _canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.GetComponent<Canvas>().sortingOrder = 20;
        CanvasScaler scaler = _canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        _panel = Rect("Players", _canvas.transform);
        _panel.anchorMin = _panel.anchorMax = new Vector2(0f, 0.5f);
        _panel.pivot = new Vector2(0f, 0.5f);
        _panel.anchoredPosition = new Vector2(_leftMargin, 0f);
        RefreshPlayers();
    }

    private void Update()
    {
        if (_canvas == null || Time.unscaledTime < _nextRefresh)
            return;
        // Discover spawned player copies, including late joins, on both host and clients.
        _nextRefresh = Time.unscaledTime + 0.5f;
        RefreshPlayers();
    }

    private void RefreshPlayers()
    {
        _players.Clear();
        foreach (NetworkObject obj in NetworkManager.SpawnManager.SpawnedObjectsList)
        {
            if (obj.IsPlayerObject && obj.IsSpawned && !obj.IsOwner && obj.TryGetComponent(out NetworkActorVitals source) && source.IsSpawned)
                _players.Add(source);
        }
        _players.Sort((a, b) => a.OwnerClientId.CompareTo(b.OwnerClientId));
        _removed.Clear();
        foreach (KeyValuePair<ulong, Row> entry in _rows)
        {
            bool exists = false;
            foreach (NetworkActorVitals player in _players)
                if (player.NetworkObjectId == entry.Key && player == entry.Value.Source)
                    exists = true;
            if (!exists)
                _removed.Add(entry.Key);
        }
        foreach (ulong id in _removed)
        {
            Row row = _rows[id];
            if (row.Source != null)
                row.Source.Changed -= row.Handler;
            row.Root.SetActive(false);
            Destroy(row.Root);
            _rows.Remove(id);
        }

        float rowHeight = 78f;
        float totalHeight = _players.Count * rowHeight;
        _panel.sizeDelta = new Vector2(_width, totalHeight);
        // Keep a large party inside the screen without dropping players from the list.
        float availableHeight = ((RectTransform)_canvas.transform).rect.height - 48f;
        float scale = availableHeight > 0f && totalHeight > 0f ? Mathf.Min(1f, availableHeight / totalHeight) : 1f;
        _panel.localScale = Vector3.one * scale;
        for (int i = 0; i < _players.Count; i++)
        {
            NetworkActorVitals player = _players[i];
            if (!_rows.TryGetValue(player.NetworkObjectId, out Row row))
            {
                row = CreateRow(player);
                _rows.Add(player.NetworkObjectId, row);
            }
            RectTransform rect = (RectTransform)row.Root.transform;
            rect.anchoredPosition = new Vector2(0f, -i * rowHeight);
            row.Name.text = $"Игрок {player.OwnerClientId + 1}";
        }
    }

    private Row CreateRow(NetworkActorVitals source)
    {
        RectTransform root = Rect("Player_" + source.OwnerClientId, _panel);
        root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.sizeDelta = new Vector2(_width, 78f);
        Image(root, new Color(0.025f, 0.024f, 0.022f, 1f));
        Row row = new Row { Source = source, Root = root.gameObject };
        row.Name = Label(root, "Name", TextAnchor.MiddleLeft, 13);
        row.Name.rectTransform.anchorMin = new Vector2(0f, 1f);
        row.Name.rectTransform.anchorMax = Vector2.one;
        row.Name.rectTransform.offsetMin = new Vector2(10f, -24f);
        row.Name.rectTransform.offsetMax = new Vector2(-10f, -4f);
        Bar(root, "HP", -28f, new Color(0.19f, 0.34f, 0.23f), out row.HealthFill, out row.HealthText);
        Bar(root, "Рассудок", -52f, new Color(0.72f, 0.32f, 0.075f), out row.SanityFill, out row.SanityText);
        row.Handler = state =>
        {
            row.HealthFill.anchorMax = new Vector2(state.y > 0f ? Mathf.Clamp01(state.x / state.y) : 0f, 1f);
            row.SanityFill.anchorMax = new Vector2(state.w > 0f ? Mathf.Clamp01(state.z / state.w) : 0f, 1f);
            row.HealthText.text = $"HP  {state.x:0.#} / {state.y:0.#}";
            row.SanityText.text = $"Рассудок  {state.z:0.#} / {state.w:0.#}";
        };
        source.Changed += row.Handler;
        row.Handler(source.State);
        return row;
    }

    private void Hide()
    {
        foreach (Row row in _rows.Values)
            if (row.Source != null)
                row.Source.Changed -= row.Handler;
        _rows.Clear();
        _players.Clear();
        if (_canvas != null)
        {
            _canvas.SetActive(false);
            Destroy(_canvas);
            _canvas = null;
        }
    }

    private void Bar(RectTransform parent, string name, float y, Color color, out RectTransform fill, out Text value)
    {
        RectTransform track = Rect(name, parent);
        track.anchorMin = new Vector2(0f, 1f);
        track.anchorMax = Vector2.one;
        track.pivot = new Vector2(0.5f, 1f);
        track.anchoredPosition = new Vector2(0f, y);
        track.sizeDelta = new Vector2(-20f, 18f);
        Image(track, new Color(0.045f, 0.04f, 0.035f));
        fill = Rect("Fill", track);
        Stretch(fill);
        Image(fill, color);
        value = Label(track, "Value", TextAnchor.MiddleCenter, 11);
    }

    private RectTransform Rect(string name, Transform parent)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void Image(RectTransform rect, Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private Text Label(Transform parent, string name, TextAnchor alignment, int size)
    {
        RectTransform rect = Rect(name, parent);
        Stretch(rect);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = _font;
        text.fontSize = size;
        text.color = new Color(0.87f, 0.83f, 0.74f);
        text.alignment = alignment;
        text.raycastTarget = false;
        Shadow shadow = rect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        shadow.effectDistance = new Vector2(1f, -1f);
        return text;
    }
}
