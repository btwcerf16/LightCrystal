using System;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(VerticalLayoutGroup), typeof(ContentSizeFitter))]
public sealed class BossBarsUI : MonoBehaviour
{
    private sealed class Row
    {
        public BossBarSource Source;
        public CanvasGroup Group;
        public RectTransform Fill;
        public Text Value;
        public Action<Vector4> OnVitalsChanged;
        public Tween Fade;
    }

    private readonly Dictionary<ulong, Row> _rows = new();
    private NetworkManager _manager;
    private Font _font;

    public void Initialize(NetworkManager manager)
    {
        _manager = manager;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform panel = (RectTransform)transform;
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0f, -98f);
        panel.sizeDelta = new Vector2(440f, 0f);

        var layout = GetComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        BossBarSource.Spawned += AddBoss;
        BossBarSource.Despawned += RemoveBoss;
        foreach (NetworkObject networkObject in manager.SpawnManager.SpawnedObjectsList)
            if (networkObject.TryGetComponent(out BossBarSource boss))
                AddBoss(boss);
    }

    private void AddBoss(BossBarSource source)
    {
        if (source.NetworkManager != _manager || !source.IsSpawned || source.Named == null ||
            _rows.ContainsKey(source.NetworkObjectId))
            return;

        RectTransform track = CreateRect($"Boss {source.NetworkObjectId}", transform);
        AddImage(track, HudStyle.Surface);
        var border = track.gameObject.AddComponent<Outline>();
        border.effectColor = HudStyle.Line;
        border.effectDistance = new Vector2(1f, -1f);
        track.gameObject.AddComponent<LayoutElement>().preferredHeight = 25f;

        RectTransform fill = CreateRect("Fill", track);
        AddImage(fill, new Color32(129, 38, 42, 255));
        CreateLabel(track, "Name", source.Named.Name, TextAnchor.MiddleLeft);
        Text value = CreateLabel(track, "Health", "", TextAnchor.MiddleRight);

        var row = new Row
        {
            Source = source,
            Group = track.gameObject.AddComponent<CanvasGroup>(),
            Fill = fill,
            Value = value
        };
        row.OnVitalsChanged = state => Refresh(row, state);
        _rows.Add(source.NetworkObjectId, row);
        source.Vitals.Changed += row.OnVitalsChanged;
        Refresh(row, source.Vitals.State);
    }

    private static void Refresh(Row row, Vector4 state)
    {
        row.Group.gameObject.SetActive(state.y > 0f);
        if (state.y <= 0f)
            return;

        row.Fill.anchorMax = new Vector2(Mathf.Clamp01(state.x / state.y), 1f);
        row.Value.text = $"{state.x:0.#} / {state.y:0.#}";
        if (state.x <= 0f && row.Fade == null)
            row.Fade = row.Group.DOFade(0f, 0.25f).SetUpdate(true).SetAutoKill(false);
        else if (state.x > 0f && row.Fade != null)
        {
            row.Fade.Kill();
            row.Fade = null;
            row.Group.alpha = 1f;
        }
    }

    private void RemoveBoss(BossBarSource source)
    {
        if (!_rows.TryGetValue(source.NetworkObjectId, out Row row) || row.Source != source)
            return;

        _rows.Remove(source.NetworkObjectId);
        Release(row);
    }

    private void OnDisable()
    {
        BossBarSource.Spawned -= AddBoss;
        BossBarSource.Despawned -= RemoveBoss;
        foreach (Row row in _rows.Values)
            Release(row);
        _rows.Clear();
    }

    private void Release(Row row)
    {
        row.Source.Vitals.Changed -= row.OnVitalsChanged;
        row.Fade?.Kill();
        row.Group.gameObject.SetActive(false);
        Destroy(row.Group.gameObject);
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        RectTransform rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static void AddImage(RectTransform rect, Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private Text CreateLabel(Transform parent, string objectName, string content, TextAnchor alignment)
    {
        RectTransform rect = CreateRect(objectName, parent);
        rect.offsetMin = new Vector2(10f, 0f);
        rect.offsetMax = new Vector2(-10f, 0f);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = _font;
        text.fontSize = 14;
        text.color = HudStyle.Text;
        text.text = content;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }
}
