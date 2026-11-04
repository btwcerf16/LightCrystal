using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public sealed class PlayerMinimapUI : MonoBehaviour
{
    private sealed class TargetMarker
    {
        public RectTransform Dot;
        public NetworkActorVitals Vitals;
        public ActorHealth Health;
        public Animator Animator;
        public Color Color;
    }

    private readonly Dictionary<NetworkObject, TargetMarker> _targets = new();
    private readonly List<NetworkObject> _removed = new();
    private ActorVision _vision;
    private Image _visionImage;
    private Texture2D _visionTexture;
    private Sprite _visionSprite;
    private float _pixelsPerUnit;
    private float _nextScan;
    private Transform _player;
    private Transform _crystal;
    private Tilemap _ground;
    private BoundsInt _cells;
    private RectTransform _map;
    private RectTransform _playerDot;
    private RectTransform _crystalDot;
    private Texture2D _mapTexture;
    private Texture2D _dotTexture;
    private Sprite _dotSprite;

    public void Initialize(Transform player, Tilemap ground, Transform crystal)
    {
        _player = player;
        _vision = player.GetComponentInChildren<ActorVision>(true);
        _crystal = crystal;
        _ground = ground;
        _cells = ground.cellBounds;
        if (_cells.size.x <= 0 || _cells.size.y <= 0)
        {
            gameObject.SetActive(false);
            return;
        }

        RectTransform panel = (RectTransform)transform;
        panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.zero;
        panel.anchoredPosition = new Vector2(24f, 24f);
        panel.sizeDelta = new Vector2(168f, 168f);

        _map = new GameObject("Map", typeof(RectTransform), typeof(RawImage)).GetComponent<RectTransform>();
        _map.SetParent(panel, false);
        _map.anchoredPosition = Vector2.zero;
        Vector3 cellSize = ground.transform.TransformVector(ground.cellSize);
        Vector2 worldSize = new Vector2(_cells.size.x * Mathf.Abs(cellSize.x), _cells.size.y * Mathf.Abs(cellSize.y));
        _pixelsPerUnit = 168f / Mathf.Max(worldSize.x, worldSize.y);
        _map.sizeDelta = worldSize * _pixelsPerUnit;
        _map.gameObject.AddComponent<RectMask2D>();
        _mapTexture = new Texture2D(_cells.size.x, _cells.size.y, TextureFormat.RGBA32, false);
        _mapTexture.filterMode = FilterMode.Point;
        _mapTexture.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[_cells.size.x * _cells.size.y];
        for (int y = 0; y < _cells.size.y; y++)
            for (int x = 0; x < _cells.size.x; x++)
                pixels[y * _cells.size.x + x] = ground.HasTile(new Vector3Int(_cells.xMin + x, _cells.yMin + y, _cells.zMin))
                    ? new Color32(48, 65, 48, 255) : new Color32(16, 19, 17, 255);
        _mapTexture.SetPixels32(pixels);
        _mapTexture.Apply(false, true);
        RawImage mapImage = _map.GetComponent<RawImage>();
        mapImage.texture = _mapTexture;
        mapImage.raycastTarget = false;

        _crystalDot = Marker("Crystal", new Color(0.3f, 0.9f, 1f), 9f);
        _crystalDot.localRotation = Quaternion.Euler(0f, 0f, 45f);
        _dotTexture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        _dotTexture.filterMode = FilterMode.Point;
        Color32[] dot = new Color32[64];
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
                dot[y * 8 + x] = (new Vector2(x - 3.5f, y - 3.5f)).sqrMagnitude <= 12.25f ? Color.white : Color.clear;
        _dotTexture.SetPixels32(dot);
        _dotTexture.Apply(false, true);
        _dotSprite = Sprite.Create(_dotTexture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f));
        _playerDot = Marker("Player", Color.white, 8f);
        _playerDot.GetComponent<Image>().sprite = _dotSprite;
        CreateVision();
        LateUpdate();
    }

    private void CreateVision()
    {
        _visionTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        _visionTexture.filterMode = FilterMode.Bilinear;
        _visionTexture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[64 * 64];
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float radius = new Vector2(x - 31.5f, y - 31.5f).magnitude / 31.5f;
                pixels[y * 64 + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - radius) * 5f));
            }
        _visionTexture.SetPixels(pixels);
        _visionTexture.Apply(false, true);
        _visionSprite = Sprite.Create(_visionTexture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f));
        _visionImage = new GameObject("Vision", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        _visionImage.transform.SetParent(_map, false);
        _visionImage.transform.SetAsFirstSibling();
        _visionImage.sprite = _visionSprite;
        _visionImage.type = Image.Type.Filled;
        _visionImage.fillMethod = Image.FillMethod.Radial360;
        _visionImage.fillOrigin = (int)Image.Origin360.Top;
        _visionImage.fillClockwise = true;
        _visionImage.color = new Color(1f, 0.92f, 0.65f, 0.25f);
        _visionImage.raycastTarget = false;
    }

    private void ScanTargets()
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsListening)
            return;
        foreach (NetworkObject target in manager.SpawnManager.SpawnedObjectsList)
        {
            if (target.transform == _player || _targets.ContainsKey(target))
                continue;
            Color color;
            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy != null)
                color = new Color(1f, 0.2f, 0.18f);
            else if (target.GetComponent<PlayerInputReader>() != null)
                color = new Color(0.25f, 0.55f, 1f);
            else if (target.GetComponent<Shop>() != null || target.GetComponent<ShopSummonZone>() != null)
                color = new Color(1f, 0.83f, 0.2f);
            else
                continue;
            _targets.Add(target, new TargetMarker
            {
                Color = color,
                Vitals = target.GetComponent<NetworkActorVitals>(),
                Health = target.GetComponent<ActorHealth>(),
                Animator = enemy != null ? target.GetComponent<Animator>() : null
            });
        }
    }

    private void UpdateTargets()
    {
        _removed.Clear();
        foreach (var entry in _targets)
        {
            NetworkObject target = entry.Key;
            TargetMarker marker = entry.Value;
            if (target == null || !target.IsSpawned || !target.gameObject.activeInHierarchy)
            {
                if (marker.Dot != null)
                    Destroy(marker.Dot.gameObject);
                _removed.Add(target);
                continue;
            }
            bool dying = marker.Animator != null && marker.Animator.runtimeAnimatorController != null
                && (marker.Animator.GetCurrentAnimatorStateInfo(0).IsName("Death")
                    || (marker.Animator.IsInTransition(0) && marker.Animator.GetNextAnimatorStateInfo(0).IsName("Death")));
            bool visible = !dying && _vision != null && _vision.CanSee(target.transform.position)
                && (marker.Vitals != null ? marker.Vitals.State.x > 0f : marker.Health == null || !marker.Health.IsDead);
            if (visible && marker.Dot == null)
            {
                marker.Dot = Marker(target.name, marker.Color, 6f);
                marker.Dot.GetComponent<Image>().sprite = _dotSprite;
            }
            if (marker.Dot == null)
                continue;
            marker.Dot.gameObject.SetActive(visible);
            if (visible)
                marker.Dot.anchoredPosition = MapPosition(target.transform.position);
        }
        foreach (NetworkObject target in _removed)
            _targets.Remove(target);
        _crystalDot.SetAsLastSibling();
        _playerDot.SetAsLastSibling();
    }

    private RectTransform Marker(string markerName, Color color, float size)
    {
        RectTransform marker = new GameObject(markerName, typeof(RectTransform), typeof(Image), typeof(Outline)).GetComponent<RectTransform>();
        marker.SetParent(_map, false);
        marker.sizeDelta = new Vector2(size, size);
        Image image = marker.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        Outline outline = marker.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);
        return marker;
    }

    private Vector2 MapPosition(Vector3 position)
    {
        Vector3 cell = _ground.LocalToCellInterpolated(_ground.transform.InverseTransformPoint(position));
        Vector2 point = new Vector2((cell.x - _cells.xMin) / _cells.size.x - 0.5f,
            (cell.y - _cells.yMin) / _cells.size.y - 0.5f) * _map.sizeDelta;
        Vector2 edge = _map.sizeDelta * 0.5f - Vector2.one * 7f;
        return new Vector2(Mathf.Clamp(point.x, -edge.x, edge.x), Mathf.Clamp(point.y, -edge.y, edge.y));
    }

    private void LateUpdate()
    {
        if (_playerDot == null || _ground == null)
            return;
        if (Time.unscaledTime >= _nextScan)
        {
            _nextScan = Time.unscaledTime + 0.2f;
            ScanTargets();
        }
        bool hasVision = _vision != null && _vision.isActiveAndEnabled;
        _visionImage.gameObject.SetActive(hasVision);
        if (hasVision)
        {
            _visionImage.rectTransform.anchoredPosition = MapPosition(_vision.transform.position);
            _visionImage.rectTransform.sizeDelta = Vector2.one * (_vision.Radius * 2f * _pixelsPerUnit);
            float angle = Mathf.Atan2(_vision.Direction.y, _vision.Direction.x) * Mathf.Rad2Deg;
            _visionImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f + _vision.Angle * 0.5f);
            _visionImage.fillAmount = _vision.Angle / 360f;
        }
        UpdateTargets();
        _playerDot.gameObject.SetActive(_player != null);
        _crystalDot.gameObject.SetActive(_crystal != null);
        if (_player != null)
            _playerDot.anchoredPosition = MapPosition(_player.position);
        if (_crystal != null)
            _crystalDot.anchoredPosition = MapPosition(_crystal.position);
    }

    private void OnDestroy()
    {
        if (_visionSprite != null) Destroy(_visionSprite);
        if (_visionTexture != null) Destroy(_visionTexture);
        if (_mapTexture != null) Destroy(_mapTexture);
        if (_dotSprite != null) Destroy(_dotSprite);
        if (_dotTexture != null) Destroy(_dotTexture);
    }
}
