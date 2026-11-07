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
    private RectTransform _pinnedPanel;
    private int _pinnedCount;
    private readonly List<(string Key, RectTransform Rect, Toggle Favorite, Toggle Pinned, Text Value)> _rows = new();
    private readonly Color _textColor = HudStyle.Text;

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
        _canvas = new GameObject("StatsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
        _panel.sizeDelta = new Vector2(760f, 656f);
        Image background = _panel.gameObject.AddComponent<Image>();
        background.color = HudStyle.Surface;
        background.raycastTarget = false;
        Outline border = _panel.gameObject.AddComponent<Outline>();
        border.effectColor = HudStyle.Line;
        border.effectDistance = new Vector2(1f, -1f);

        Text title = Label(_panel, "Title", "Характеристики", 20f, 20f, 420f, 28f, TextAnchor.MiddleLeft);
        title.fontSize = 20;
        Label(_panel, "CloseHint", "TAB — закрыть", 570f, 20f, 170f, 28f, TextAnchor.MiddleRight);
        _resources = Label(_panel, "Resources", "", 20f, 56f, 720f, 28f, TextAnchor.MiddleLeft);
        Label(_panel, "Names", "Параметр", 92f, 98f, 290f, 24f, TextAnchor.MiddleLeft);
        Label(_panel, "Base", "База", 392f, 98f, 95f, 24f, TextAnchor.MiddleRight);
        Label(_panel, "Bonus", "Бонус", 497f, 98f, 95f, 24f, TextAnchor.MiddleRight);
        Label(_panel, "Total", "Итог", 602f, 98f, 138f, 24f, TextAnchor.MiddleRight);

        Label(_panel, "FavoriteHeader", "Вверх", 16f, 98f, 36f, 24f, TextAnchor.MiddleCenter).fontSize = 10;
        Label(_panel, "PinnedHeader", "HUD", 52f, 98f, 32f, 24f, TextAnchor.MiddleCenter).fontSize = 10;
        _pinnedPanel = Rect("PinnedStats", _canvas.transform);
        _pinnedPanel.anchorMin = _pinnedPanel.anchorMax = _pinnedPanel.pivot = new Vector2(1f, 0.5f);
        _pinnedPanel.anchoredPosition = new Vector2(-24f, 0f);
        _pinnedPanel.sizeDelta = new Vector2(352f, 0f);
        _pinnedPanel.gameObject.AddComponent<Image>().color = HudStyle.Surface;
        _pinnedPanel.GetComponent<Image>().raycastTarget = false;
        VerticalLayoutGroup layout = _pinnedPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 8, 8);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        _pinnedPanel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var rows = new (string Key, string Name, Stat Stat, bool Percent)[]
        {
            (nameof(ActorStats.PhysicalDamage), "Физический урон", stats.PhysicalDamage, false),
            (nameof(ActorStats.PhysicalDamageMultiplier), "Множитель физ. урона", stats.PhysicalDamageMultiplier, false),
            (nameof(ActorStats.MagicDamageMultiplier), "Множитель маг. урона", stats.MagicDamageMultiplier, false),
            (nameof(ActorStats.AttackSpeed), "Скорость атаки", stats.AttackSpeed, false),
            (nameof(ActorStats.MoveSpeed), "Скорость движения", stats.MoveSpeed, false),
            (nameof(ActorStats.CooldownReduction), "Сокращение перезарядки", stats.CooldownReduction, true),
            (nameof(ActorStats.MaxHealth), "Максимум здоровья", stats.MaxHealth, false),
            (nameof(ActorStats.ReviveSpeed), "Скорость помощи союзникам", stats.ReviveSpeed, false),
            (nameof(ActorStats.HealthRegeneration), "Восстановление HP/с", stats.HealthRegeneration, false),
            (nameof(ActorStats.MaxSanity), "Максимум рассудка", stats.MaxSanity, false),
            (nameof(ActorStats.PhysicalResistance), "Физическая защита", stats.PhysicalResistance, true),
            (nameof(ActorStats.MagicResistance), "Магическая защита", stats.MagicResistance, true),
            (nameof(ActorStats.ForceResistance), "Сопротивление отбрасыванию", stats.ForceResistance, true),
            (nameof(ActorStats.MadnessResistance), "Сопротивление безумию", stats.MadnessResistance, true),
            (nameof(ActorStats.EffectResistance), "Сопротивление эффектам", stats.EffectResistance, true),
            (nameof(ActorStats.PhysicalLifeSteal), "Физический вампиризм", stats.PhysicalLifeSteal, true),
            (nameof(ActorStats.MagicLifeSteal), "Магический вампиризм", stats.MagicLifeSteal, true),
            (nameof(ActorStats.ViewDistance), "Дальность обзора", stats.ViewDistance, false),
            (nameof(ActorStats.ViewAngle), "Угол обзора", stats.ViewAngle, false),
            (nameof(ActorStats.Stability), "Стабилизация, градусы", stats.Stability, false),
            (nameof(ActorStats.CameraDistance), "Отдаление камеры", stats.CameraDistance, false),
            (nameof(ActorStats.CastSpeed), "Скорость каста", stats.CastSpeed, false),
            (nameof(ActorStats.ProjectileSpeed), "Бонус скорости снаряда", stats.ProjectileSpeed, false),
            (nameof(ActorStats.ProjectileSpeedMultiplier), "Множитель скорости снаряда", stats.ProjectileSpeedMultiplier, false),
            (nameof(ActorStats.ProjectileRange), "Бонус дальности снаряда", stats.ProjectileRange, false),
            (nameof(ActorStats.ProjectilePiercing), "Пронзания снаряда", stats.ProjectilePiercing, false),
            (nameof(ActorStats.SanityCostReduction), "Снижение затрат рассудка", stats.SanityCostReduction, true),
            (nameof(ActorStats.CriticalChance), "Шанс крита", stats.CriticalChance, true),
            (nameof(ActorStats.CriticalDamageMultiplier), "Критический урон", stats.CriticalDamageMultiplier, true)
        };
        _panel.sizeDelta = new Vector2(760f, 170f + rows.Length * 28f);
        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            RectTransform line = Rect(row.Key, _panel);
            line.anchorMin = line.anchorMax = line.pivot = new Vector2(0f, 1f);
            line.sizeDelta = new Vector2(760f, 28f);
            Toggle favorite = Checkbox(line, "Favorite", 20f);
            Toggle pinned = Checkbox(line, "Pinned", 56f);
            Text pinnedText = Label(_pinnedPanel, row.Key, "", 0f, 0f, 328f, 28f, TextAnchor.MiddleLeft);
            pinnedText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
            _rows.Add((row.Key, line, favorite, pinned, pinnedText));
            favorite.onValueChanged.AddListener(value => SaveSelection("Favorite", row.Key, value));
            pinned.onValueChanged.AddListener(value => SaveSelection("Pinned", row.Key, value));
            Label(line, row.Name, row.Name, 92f, 0f, 290f, 24f, TextAnchor.MiddleLeft);
            Text baseText = Label(line, "BaseValue", "", 392f, 0f, 95f, 24f, TextAnchor.MiddleRight);
            Text bonusText = Label(line, "BonusValue", "", 497f, 0f, 95f, 24f, TextAnchor.MiddleRight);
            Text totalText = Label(line, "TotalValue", "", 602f, 0f, 138f, 24f, TextAnchor.MiddleRight);
            Action<float> handler = value =>
            {
                float bonus = row.Stat.Value - row.Stat.BaseValue;
                baseText.text = Format(row.Stat.BaseValue, row.Percent);
                bonusText.text = bonus == 0f ? "—" : (bonus > 0f ? "+" : "") + Format(bonus, row.Percent);
                bonusText.color = bonus >= 0f ? new Color(0.5f, 0.72f, 0.42f) : new Color(0.8f, 0.35f, 0.28f);
                totalText.text = Format(row.Stat.Value, row.Percent);
                pinnedText.text = row.Name + ": " + totalText.text;
            };
            row.Stat.ValueChanged += handler;
            _subscriptions.Add((row.Stat, handler));
            handler(row.Stat.Value);
        }
        Label(_panel, "Footer", "Вверх — приоритет в списке. HUD — показать справа.", 20f, _panel.sizeDelta.y - 30f, 720f, 20f, TextAnchor.MiddleLeft).fontSize = 12;
        _vitals.Changed += UpdateResources;
        UpdateResources(_vitals.State);
        _input.StatsToggled += Toggle;
        GetComponent<SpellHolder>().SpellsChanged += LoadSelection;
        LoadSelection();
        _panel.gameObject.SetActive(_input.IsStatsOpen);
    }

    private string PreferenceKey(string option, string stat)
    {
        CharacterConfig character = GetComponent<NetworkSpellHolder>().Character;
        return "StatsUI." + (character != null ? character.name : "Default") + "." + option + "." + stat;
    }

    private void SaveSelection(string option, string stat, bool value)
    {
        PlayerPrefs.SetInt(PreferenceKey(option, stat), value ? 1 : 0);
        PlayerPrefs.Save();
        RefreshRows();
    }

    private void LoadSelection()
    {
        foreach (var row in _rows)
        {
            row.Favorite.SetIsOnWithoutNotify(PlayerPrefs.GetInt(PreferenceKey("Favorite", row.Key)) != 0);
            row.Pinned.SetIsOnWithoutNotify(PlayerPrefs.GetInt(PreferenceKey("Pinned", row.Key)) != 0);
        }
        RefreshRows();
    }

    private void RefreshRows()
    {
        int position = 0;
        _pinnedCount = 0;
        foreach (bool favorite in new[] { true, false })
            foreach (var row in _rows)
                if (row.Favorite.isOn == favorite)
                    row.Rect.anchoredPosition = new Vector2(0f, -130f - position++ * 28f);
        foreach (var row in _rows)
        {
            row.Value.gameObject.SetActive(row.Pinned.isOn);
            if (row.Pinned.isOn) _pinnedCount++;
        }
        _pinnedPanel.gameObject.SetActive(_pinnedCount > 0);
    }

    private Toggle Checkbox(Transform parent, string name, float x)
    {
        RectTransform rect = Rect(name, parent);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -2f);
        rect.sizeDelta = new Vector2(24f, 24f);
        Image background = rect.gameObject.AddComponent<Image>();
        background.color = HudStyle.Slot;
        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = HudStyle.Line;
        outline.effectDistance = Vector2.one;
        RectTransform mark = Rect("Check", rect);
        mark.anchorMin = Vector2.zero;
        mark.anchorMax = Vector2.one;
        mark.offsetMin = new Vector2(6f, 6f);
        mark.offsetMax = new Vector2(-6f, -6f);
        Image graphic = mark.gameObject.AddComponent<Image>();
        graphic.color = HudStyle.Text;
        graphic.raycastTarget = false;
        Toggle toggle = rect.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = graphic;
        toggle.navigation = new Navigation { mode = Navigation.Mode.None };
        return toggle;
    }

    private string Format(float value, bool percent) => percent ? $"{value * 100f:0.#}%" : $"{value:0.##}";

    private void UpdateResources(Vector4 state)
    {
        _resources.text = $"Здоровье  {state.x:0.#} / {state.y:0.#}      Рассудок  {state.z:0.#} / {state.w:0.#}";
    }

    private void Toggle()
    {
        if (_canvas != null)
            _panel.gameObject.SetActive(_input.IsStatsOpen);
    }

    private void LateUpdate()
    {
        if (_canvas == null || !_canvas.activeSelf)
            return;
        _pinnedPanel.gameObject.SetActive(_pinnedCount > 0 && !_input.IsMenuOpen && !_input.IsConsoleOpen);
        Rect area = ((RectTransform)_canvas.transform).rect;
        float scale = Mathf.Min(1f, (area.width - 32f) / 760f, (area.height - 32f) / _panel.sizeDelta.y);
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
        GetComponent<SpellHolder>().SpellsChanged -= LoadSelection;
        _rows.Clear();
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
