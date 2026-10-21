using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerInputReader), typeof(ActorStats))]
public sealed class PlayerStatsUI : NetworkBehaviour
{
    private readonly List<(Stat Stat, Action<float> Handler)> _subscriptions = new();
    private PlayerInputReader _input;
    private NetworkActorVitals _vitals;
    private GameObject _canvas;
    private RectTransform _panel;
    private Font _font;
    private Text _resources;
    private readonly Color _textColor = new Color(0.84f, 0.8f, 0.72f);

    private void Awake()
    {
        _input = GetComponent<PlayerInputReader>();
        _vitals = GetComponent<NetworkActorVitals>();
    }

    public override void OnNetworkSpawn() => Bind();
    public override void OnGainedOwnership() => Bind();
    public override void OnLostOwnership() => Unbind();
    public override void OnNetworkDespawn() => Unbind();
    private void OnEnable() => Bind();
    private void OnDisable() => Unbind();

    private void Bind()
    {
        if (!IsSpawned || !IsClient || !IsOwner || !isActiveAndEnabled || _canvas != null)
            return;
        ActorStats stats = GetComponent<ActorStats>();
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _canvas = new GameObject("StatsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        _canvas.transform.SetParent(transform, false);
        _canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.GetComponent<Canvas>().sortingOrder = 60;
        CanvasScaler scaler = _canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _panel = Rect("StatsPanel", _canvas.transform);
        _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
        _panel.pivot = new Vector2(0.5f, 0.5f);
        _panel.sizeDelta = new Vector2(660f, 656f);
        Image background = _panel.gameObject.AddComponent<Image>();
        background.color = new Color(0.025f, 0.024f, 0.022f, 0.99f);
        background.raycastTarget = false;
        Outline border = _panel.gameObject.AddComponent<Outline>();
        border.effectColor = new Color(0.26f, 0.22f, 0.16f);
        border.effectDistance = new Vector2(1f, -1f);

        Text title = Label(_panel, "Title", "ХАРАКТЕРИСТИКИ", 20f, 20f, 420f, 28f, TextAnchor.MiddleLeft);
        title.fontSize = 20;
        Label(_panel, "CloseHint", "TAB — закрыть", 470f, 20f, 170f, 28f, TextAnchor.MiddleRight);
        _resources = Label(_panel, "Resources", "", 20f, 56f, 620f, 28f, TextAnchor.MiddleLeft);
        Label(_panel, "Names", "Параметр", 20f, 98f, 290f, 24f, TextAnchor.MiddleLeft);
        Label(_panel, "Base", "База", 320f, 98f, 95f, 24f, TextAnchor.MiddleRight);
        Label(_panel, "Bonus", "Бонус", 425f, 98f, 95f, 24f, TextAnchor.MiddleRight);
        Label(_panel, "Total", "Итог", 530f, 98f, 110f, 24f, TextAnchor.MiddleRight);

        var rows = new (string Name, Stat Stat, bool Percent)[]
        {
            ("Физический урон", stats.PhysicalDamage, false),
            ("Множитель физ. урона", stats.PhysicalDamageMultiplier, false),
            ("Множитель маг. урона", stats.MagicDamageMultiplier, false),
            ("Скорость атаки", stats.AttackSpeed, false),
            ("Скорость движения", stats.MoveSpeed, false),
            ("Сокращение перезарядки", stats.CooldownReduction, true),
            ("Максимум здоровья", stats.MaxHealth, false),
            ("Восстановление HP/с", stats.HealthRegeneration, false),
            ("Максимум рассудка", stats.MaxSanity, false),
            ("Физическая защита", stats.PhysicalResistance, true),
            ("Магическая защита", stats.MagicResistance, true),
            ("Сопротивление отбрасыванию", stats.ForceResistance, true),
            ("Сопротивление безумию", stats.MadnessResistance, true),
            ("Сопротивление эффектам", stats.EffectResistance, true),
            ("Физический вампиризм", stats.PhysicalLifeSteal, true),
            ("Магический вампиризм", stats.MagicLifeSteal, true),
            ("Дальность обзора", stats.ViewDistance, false),
            ("Угол обзора", stats.ViewAngle, false)
        };
        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            float y = 130f + i * 27f;
            Label(_panel, row.Name, row.Name, 20f, y, 290f, 24f, TextAnchor.MiddleLeft);
            Text baseText = Label(_panel, "BaseValue", "", 320f, y, 95f, 24f, TextAnchor.MiddleRight);
            Text bonusText = Label(_panel, "BonusValue", "", 425f, y, 95f, 24f, TextAnchor.MiddleRight);
            Text totalText = Label(_panel, "TotalValue", "", 530f, y, 110f, 24f, TextAnchor.MiddleRight);
            Action<float> handler = value =>
            {
                float bonus = row.Stat.Value - row.Stat.BaseValue;
                baseText.text = Format(row.Stat.BaseValue, row.Percent);
                bonusText.text = bonus == 0f ? "—" : (bonus > 0f ? "+" : "") + Format(bonus, row.Percent);
                bonusText.color = bonus >= 0f ? new Color(0.5f, 0.72f, 0.42f) : new Color(0.8f, 0.35f, 0.28f);
                totalText.text = Format(row.Stat.Value, row.Percent);
            };
            row.Stat.ValueChanged += handler;
            _subscriptions.Add((row.Stat, handler));
            handler(row.Stat.Value);
        }
        Label(_panel, "Footer", "Белое — базовое значение, зелёное — бонус", 20f, 622f, 620f, 20f, TextAnchor.MiddleLeft).fontSize = 12;
        _vitals.Changed += UpdateResources;
        UpdateResources(_vitals.State);
        _input.StatsToggled += Toggle;
        _canvas.SetActive(_input.IsStatsOpen);
    }

    private string Format(float value, bool percent) => percent ? $"{value * 100f:0.#}%" : $"{value:0.##}";

    private void UpdateResources(Vector4 state)
    {
        _resources.text = $"Здоровье  {state.x:0.#} / {state.y:0.#}      Рассудок  {state.z:0.#} / {state.w:0.#}";
    }

    private void Toggle()
    {
        if (_canvas != null)
            _canvas.SetActive(_input.IsStatsOpen);
    }

    private void LateUpdate()
    {
        if (_canvas == null || !_canvas.activeSelf)
            return;
        Rect area = ((RectTransform)_canvas.transform).rect;
        float scale = Mathf.Min(1f, (area.width - 32f) / 660f, (area.height - 32f) / 656f);
        _panel.localScale = Vector3.one * Mathf.Max(0.1f, scale);
    }

    private void Unbind()
    {
        if (_input != null)
            _input.StatsToggled -= Toggle;
        if (_vitals != null)
            _vitals.Changed -= UpdateResources;
        foreach (var subscription in _subscriptions)
            subscription.Stat.ValueChanged -= subscription.Handler;
        _subscriptions.Clear();
        if (_canvas != null)
        {
            _canvas.SetActive(false);
            Destroy(_canvas);
            _canvas = null;
        }
        if (_input != null && IsOwner)
            _input.CloseStats();
    }

    private RectTransform Rect(string name, Transform parent)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private Text Label(Transform parent, string name, string content, float x, float y, float width, float height, TextAnchor alignment)
    {
        RectTransform rect = Rect(name, parent);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = _font;
        text.fontSize = 14;
        text.text = content;
        text.color = _textColor;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }
}
