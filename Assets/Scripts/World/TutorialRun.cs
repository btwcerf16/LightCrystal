using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialRun : MonoBehaviour
{
    public const string SceneName = "TutorialArena";

    [SerializeField] private CharacterConfig _character;
    [SerializeField] private Goblin _goblinPrefab;
    [SerializeField] private Transform _goblinSpawn;
    [SerializeField] private WorldItem _worldItemPrefab;
    [SerializeField] private SpearWeapon _spearPrefab;
    [SerializeField] private Transform _spearSpawn;
    [SerializeField] private RestorationBottleItem _bottlePrefab;
    [SerializeField] private ToggleSpellConfig _bodyStrength;
    [SerializeField] private Shader _grayHudShader;
    [SerializeField] private NetworkSummonZone _summonZonePrefab;
    [SerializeField] private NetworkShop _merchantPrefab;
    [SerializeField] private Item[] _merchantStock;
    [SerializeField, Min(0.1f)] private float _zoneFillTime = 5f;

    public CharacterConfig Character => _character;

    private PlayerInputReader _input;
    private ActorHealth _health;
    private ActorArsenal _arsenal;
    private PlayerSlotBindings _bindings;
    private PlayerNetworkInteractions _interactions;
    private Inventory _inventory;
    private EffectHandler _effects;
    private Crystal _crystal;
    private ActorHealth _strongHealth;
    private ActorStats _strongStats;
    private RectTransform _arsenalPanel;
    private RectTransform _vitalsPanel;
    private RectTransform _inventoryPanel;
    private RectTransform _spellPanel;
    private Outline _abilityOutline;
    private int _abilitySiblingIndex;
    private GameObject _abilityPrompt;
    private Material _grayHudMaterial;
    private readonly Dictionary<Graphic, Material> _hudMaterials = new();
    private Transform _focusTarget;
    private float _focusRadius;
    private bool _swungSpear;
    private CanvasGroup _hint;
    private Text _title;
    private Text _instruction;
    private ActorWallet _wallet;
    private PlayerHUD _hud;
    private bool _readyToContinue;

    public void Begin(NetworkObject player)
    {
        _input = player.GetComponent<PlayerInputReader>();
        _input.InteractPressed += ConfirmMovement;
        _wallet = player.GetComponent<ActorWallet>();
        _wallet.SetCurrency(0);
        _health = player.GetComponent<ActorHealth>();
        _health.TryRegisterLethalHitRecovery(PreventLethalHit);
        _arsenal = player.GetComponent<ActorArsenal>();
        _bindings = player.GetComponent<PlayerSlotBindings>();
        _inventory = player.GetComponent<Inventory>();
        _effects = player.GetComponent<EffectHandler>();
        _interactions = player.GetComponent<PlayerNetworkInteractions>();
        _interactions.WeaponUseStarted += OnWeaponUseStarted;
        _crystal = FindFirstObjectByType<Crystal>();
        _hud = player.GetComponent<PlayerHUD>();
        _hud.SetMinimalLayout();
        _arsenalPanel = _hud.ArsenalPanel;
        _vitalsPanel = _hud.VitalsPanel;
        _inventoryPanel = _hud.InventoryPanel;
        _spellPanel = _hud.SpellPanel;
        _arsenalPanel.gameObject.SetActive(false);
        _vitalsPanel.gameObject.SetActive(false);
        _inventoryPanel.gameObject.SetActive(false);
        _spellPanel.gameObject.SetActive(false);
        player.GetComponent<TeamVitalsUI>().enabled = false;
        player.GetComponent<PlayerStatsUI>().PinnedStatsVisible = false;
        BuildHint();
        StartCoroutine(Teach(player.GetComponent<ActorSanity>()));
    }

    private IEnumerator Teach(ActorSanity sanity)
    {
        Vector2 startingPosition = _health.transform.position;
        Show("Учебная арена", "Походи на WASD и попробуй наводить руку мышью.");
        yield return new WaitUntil(() => ((Vector2)_health.transform.position - startingPosition).sqrMagnitude >= 6.25f);
        _readyToContinue = false;
        Show("Осмотрись", "Когда будешь готов, нажми E, чтобы перейти к оружию.");
        yield return new WaitUntil(() => _readyToContinue);
        _input.InteractPressed -= ConfirmMovement;

        WorldItem spearDrop = SpawnItem(_spearPrefab, _spearSpawn.position);
        _arsenalPanel.gameObject.SetActive(true);
        Focus(spearDrop.transform, 1.7f);
        Show("Подними копьё", "Подойди к копью справа от кристалла. Наведи на него курсор и нажми E.");
        yield return new WaitUntil(() => _arsenal.ActiveWeapon is SpearWeapon);
        Weapon spear = _arsenal.ActiveWeapon;

        Focus(spear.transform, 1.6f);
        Show("Обычная атака", "У оружия два действия. Нажми ЛКМ, чтобы сделать выпад копьём.");
        _swungSpear = false;
        yield return new WaitUntil(() => _swungSpear);
        _vitalsPanel.gameObject.SetActive(true);
        Show("Особая атака", "Нажми ПКМ, чтобы метнуть копьё. Бросок расходует рассудок.");
        yield return new WaitUntil(() => !spear.IsAvailable);

        SpearProjectile thrownSpear = FindFirstObjectByType<SpearProjectile>();
        Focus(thrownSpear != null ? thrownSpear.transform : null, 0.85f);
        Show("Верни копьё", "Подойди к месту, куда упало копьё, чтобы оно вернулось в руку.");
        yield return new WaitUntil(() => spear.IsAvailable);
        Focus(null);
        Show("За пределами света", "Выйди из света справа от кристалла. В темноте постепенно убывает рассудок.");
        yield return new WaitUntil(() => !sanity.IsInLight);

        Goblin goblin = SpawnGoblin();
        ActorHealth goblinHealth = goblin.GetComponent<ActorHealth>();
        Focus(goblin.transform, 1.1f);
        Show("Гоблин в темноте", "Наведи на него оружие и отбейся. ЛКМ — выпад, ПКМ — бросок.");
        yield return new WaitUntil(() => goblinHealth == null || goblinHealth.IsDead);

        Focus(_crystal.transform, 1.6f);
        Show("Восстанови рассудок", "Вернись в свет кристалла. Он восстанавливает рассудок и постепенно лечит.");
        yield return new WaitUntil(() => sanity.IsInLight && sanity.CurrentSanity >= sanity.MaxSanity * 0.8f);

        Goblin strongGoblin = SpawnGoblin();
        _strongHealth = strongGoblin.GetComponent<ActorHealth>();
        _strongStats = strongGoblin.GetComponent<ActorStats>();
        _strongStats.MaxHealth.AddModifier(new StatModifier(12f, StatModifierType.Flat, this));
        _strongStats.PhysicalDamage.AddModifier(new StatModifier(3f, StatModifierType.Flat, this));
        _strongHealth.ResetHealth();
        _strongHealth.TryRegisterLethalHitRecovery(PreventLethalHit);

        _spellPanel.gameObject.SetActive(true);
        int abilityIndex = System.Array.IndexOf(_character.Spells, _bodyStrength);
        string abilityKey = _bindings.GetBindingLabel(ESlotBindingType.Spell, abilityIndex);
        Focus(null);
        HighlightAbility(abilityIndex, abilityKey);
        Show("Укрепление тела", $"У всех героев есть способности. Нажми {abilityKey}, чтобы снять защиту гоблина.");
        yield return new WaitUntil(BodyStrengthIsActive);
        _strongHealth.UnregisterLethalHitRecovery(PreventLethalHit);
        ClearAbilityFocus();

        Focus(strongGoblin.transform, 1.3f);
        Show("Одолей сильного гоблина", "Защита снята. «Укрепление тела» повышает шанс и урон крита и тратит рассудок, пока включено.");
        yield return new WaitUntil(() => _strongHealth == null || _strongHealth.IsDead);
        Vector3 bottlePosition = _strongHealth != null ? _strongHealth.transform.position : _spearSpawn.position;
        ReleaseStrongGoblin();

        WorldItem bottleDrop = SpawnItem(_bottlePrefab, bottlePosition);
        _inventoryPanel.gameObject.SetActive(true);
        _hud.SetCurrencyVisible(true);
        int reward = _merchantStock[0].Price;
        _wallet.AddCurrency(reward, bottlePosition);
        Focus(bottleDrop.transform, 0.8f);
        Show("Подними бутылку", $"С гоблина выпало {reward} золота. Подойди к бутылке, наведи курсор и нажми E.");
        yield return new WaitUntil(() => FindBottleSlot() >= 0);

        int bottleSlot = FindBottleSlot();
        Focus(null);
        Show("Примени предмет", $"Нажми {_bindings.GetBindingLabel(false, bottleSlot)}, чтобы использовать бутылку восстановления.");
        yield return new WaitUntil(() => _effects.HasEffect(_bottlePrefab.RestorationEffect));

        Show("Выбрось оружие", "Возьми копьё в руку и нажми Q. Оружие останется на земле, и его можно снова поднять.");
        yield return new WaitUntil(() => !OwnsSpear());

        Show("Верни оружие", "Подними копьё обратно на E. Оно ещё пригодится для защиты кристалла.");
        yield return new WaitUntil(OwnsSpear);

        Show("Характеристики", "Нажми Tab, чтобы посмотреть основные и дополнительные характеристики.");
        yield return new WaitUntil(() => _input.IsStatsOpen);
        Show("Закрой характеристики", "Нажми Tab ещё раз, чтобы продолжить.");
        yield return new WaitUntil(() => !_input.IsStatsOpen);

        yield return TeachMerchant();
        yield return RunFinalWave();
    }

    private void ConfirmMovement() => _readyToContinue = true;

    private IEnumerator TeachMerchant()
    {
        Show("Затишье", "В начале затишья между волнами появляется зона призыва.");
        yield return new WaitForSeconds(3f);

        Vector3 center = _crystal.transform.position;
        NetworkSummonZone zone = Instantiate(_summonZonePrefab, center + new Vector3(-3f, -5.25f), Quaternion.identity);
        SummonZone area = zone.GetComponent<SummonZone>();
        area.SetFillDuration(_zoneFillTime);
        zone.SetWaveEvent();
        zone.SetEvent(_merchantPrefab.GetComponent<NetworkObject>(), center + new Vector3(2.8f, -5.25f),
            summoned => summoned.GetComponent<NetworkShop>().SetStock(_merchantStock));

        NetworkShop merchant = null;
        zone.EventSummoned += summoned => merchant = summoned.GetComponent<NetworkShop>();
        zone.NetworkObject.Spawn(true);

        Focus(zone.transform, area.Radius + 0.5f);
        Show("Зона призыва", "Встань в золотой круг. Чем больше он заполнен, тем лучше награда.");
        yield return new WaitUntil(() => merchant != null || (zone != null && zone.Capacity >= 10f));
        if (merchant == null)
        {
            Show("Призыв события", "Событие появится при 100% или в конце затишья. Здесь круг призовёт торговца.");
            yield return new WaitUntil(() => merchant != null);
        }

        Focus(merchant.transform, 2.7f);
        Show("Круг призвал торговца", "Подойди к товару, наведи курсор и нажми E. Золота хватит на одну покупку.");
        List<WorldItem> goods = merchant.GetComponent<Shop>().GetItems();
        yield return new WaitUntil(() => goods.Exists(item => item != null && !item.IsForSale));
        merchant.Close();
        Focus(null);
    }

    private IEnumerator RunFinalWave()
    {
        Show("Последняя волна", "Гоблины идут прямо к кристаллу. Попробуй его защитить!");
        yield return new WaitForSeconds(2.5f);

        ActorHealth crystalHealth = _crystal.GetComponent<ActorHealth>();
        crystalHealth.Died += HideHint;
        NetworkManager.Singleton.GetComponent<GameDefeatController>().BeginCrystalDefense();

        var attackers = new List<Goblin>();
        float spawnAngle = 0f;
        while (!crystalHealth.IsDead)
        {
            attackers.RemoveAll(enemy => enemy == null || !enemy.GetComponent<NetworkObject>().IsSpawned);
            for (int i = 0; i < 12 && attackers.Count < 24; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, 0f, spawnAngle + i * 30f) * Vector3.right * 8f;
                Goblin goblin = SpawnGoblin(_crystal.transform.position + offset, _crystal.transform);
                goblin.LockMainTarget();
                StrengthenWaveEnemy(goblin);
                attackers.Add(goblin);
            }

            spawnAngle += 13f;
            yield return new WaitForSeconds(2.5f);
        }

        crystalHealth.Died -= HideHint;
    }

    private void StrengthenWaveEnemy(Goblin goblin)
    {
        ActorStats stats = goblin.GetComponent<ActorStats>();
        ActorHealth health = goblin.GetComponent<ActorHealth>();
        stats.MaxHealth.AddModifier(new StatModifier(24f, StatModifierType.Flat, this));
        stats.PhysicalDamage.AddModifier(new StatModifier(12f, StatModifierType.Flat, this));
        stats.MoveSpeed.AddModifier(new StatModifier(1.25f, StatModifierType.Flat, this));
        health.ResetHealth();
        health.Died += RemoveBonuses;

        void RemoveBonuses()
        {
            health.Died -= RemoveBonuses;
            stats.MaxHealth.RemoveModifiersFrom(this);
            stats.PhysicalDamage.RemoveModifiersFrom(this);
            stats.MoveSpeed.RemoveModifiersFrom(this);
        }
    }

    private void HideHint() => _hint.gameObject.SetActive(false);

    private WorldItem SpawnItem(Item prefab, Vector3 position)
    {
        WorldItem drop = Instantiate(_worldItemPrefab, position, Quaternion.identity);
        drop.SetItem(prefab.gameObject);
        drop.NetworkObject.Spawn(true);
        return drop;
    }

    private Goblin SpawnGoblin() => SpawnGoblin(_goblinSpawn.position, _health.transform);

    private Goblin SpawnGoblin(Vector3 position, Transform target)
    {
        var pool = NetworkManager.Singleton.GetComponent<NetworkObjectPool>();
        Goblin goblin = pool.GetObject(_goblinPrefab, position, Quaternion.identity);
        goblin.SetMainTarget(target);
        goblin.SetGoldDropMultiplier(0f);
        goblin.GetComponent<NetworkObject>().Spawn(true);
        return goblin;
    }

    private void OnWeaponUseStarted(Weapon weapon, EAttackType type)
    {
        if (weapon is SpearWeapon && type == EAttackType.Melee)
            _swungSpear = true;
    }

    private bool BodyStrengthIsActive() => _effects.HasEffect(_bodyStrength.Effect);

    private static System.Action PreventLethalHit() => KeepAlive;

    private static void KeepAlive() { }

    private void ReleaseStrongGoblin()
    {
        if (_strongHealth != null)
            _strongHealth.UnregisterLethalHitRecovery(PreventLethalHit);
        if (_strongStats != null)
        {
            _strongStats.MaxHealth.RemoveModifiersFrom(this);
            _strongStats.PhysicalDamage.RemoveModifiersFrom(this);
        }
        _strongHealth = null;
        _strongStats = null;
    }

    private int FindBottleSlot()
    {
        for (int i = 0; i < _inventory.Size; i++)
            if (_inventory.GetItem(i) is RestorationBottleItem)
                return i;
        return -1;
    }

    private bool OwnsSpear()
    {
        for (int i = 0; i < _arsenal.WeaponSize; i++)
            if (_arsenal.GetWeapon(i) is SpearWeapon)
                return true;
        return false;
    }

    private static readonly int FocusTarget = Shader.PropertyToID("_TutorialFocusTarget");
    private static readonly int FocusAmount = Shader.PropertyToID("_TutorialFocusAmount");

    private void Focus(Transform target, float radius = 0f)
    {
        _focusTarget = target;
        _focusRadius = radius;
    }

    private void LateUpdate()
    {
        if (_abilityPrompt != null)
        {
            if (_abilityOutline != null)
                _abilityOutline.effectColor = new Color32(255, 217, 128, 255);
            Shader.SetGlobalVector(FocusTarget, Vector4.zero);
            Shader.SetGlobalFloat(FocusAmount, 1f);
            return;
        }

        if (_focusTarget == null)
        {
            Shader.SetGlobalFloat(FocusAmount, 0f);
            return;
        }

        Vector3 position = _focusTarget.position;
        Shader.SetGlobalVector(FocusTarget, new Vector4(position.x, position.y, position.z, _focusRadius));
        Shader.SetGlobalFloat(FocusAmount, 1f);
    }

    private void HighlightAbility(int index, string binding)
    {
        RectTransform slot = _spellPanel.Find("Spell_" + index) as RectTransform;
        if (slot == null)
            return;

        _abilityOutline = slot.GetComponent<Outline>();
        _abilitySiblingIndex = slot.GetSiblingIndex();
        slot.SetAsLastSibling();
        if (_abilityOutline != null)
            _abilityOutline.effectDistance = new Vector2(4f, -4f);

        _grayHudMaterial = new Material(_grayHudShader);
        Canvas hud = _spellPanel.GetComponentInParent<Canvas>();
        foreach (Graphic graphic in hud.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic.transform.IsChildOf(slot))
                continue;

            _hudMaterials.Add(graphic, graphic.material);
            graphic.material = _grayHudMaterial;
        }

        var prompt = new GameObject("Ability Prompt", typeof(Image)).GetComponent<Image>();
        _abilityPrompt = prompt.gameObject;
        prompt.color = HudStyle.Surface;
        prompt.raycastTarget = false;
        RectTransform rect = prompt.rectTransform;
        rect.SetParent(slot, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, 31f);
        rect.sizeDelta = new Vector2(148f, 36f);
        Text label = MakeLabel("Binding", rect, 22, 0f, 36f);
        label.text = $"Нажми {binding}";

        var pointer = new GameObject("Pointer", typeof(Image)).GetComponent<Image>();
        pointer.color = new Color32(255, 217, 128, 255);
        pointer.raycastTarget = false;
        RectTransform tip = pointer.rectTransform;
        tip.SetParent(rect, false);
        tip.anchorMin = tip.anchorMax = new Vector2(0.5f, 0f);
        tip.anchoredPosition = new Vector2(0f, -5f);
        tip.sizeDelta = new Vector2(8f, 8f);
        tip.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private void ClearAbilityFocus()
    {
        if (_abilityOutline != null)
        {
            _abilityOutline.effectColor = HudStyle.Line;
            _abilityOutline.effectDistance = new Vector2(1f, -1f);
            _abilityOutline.transform.SetSiblingIndex(_abilitySiblingIndex);
        }
        _abilityOutline = null;

        foreach (var entry in _hudMaterials)
            if (entry.Key != null)
                entry.Key.material = entry.Value;
        _hudMaterials.Clear();
        if (_grayHudMaterial != null)
            Destroy(_grayHudMaterial);
        if (_abilityPrompt != null)
        {
            _abilityPrompt.SetActive(false);
            Destroy(_abilityPrompt);
        }
        _abilityPrompt = null;
    }



    private void BuildHint()
    {
        var canvas = new GameObject("Tutorial HUD", typeof(Canvas), typeof(CanvasScaler));
        canvas.transform.SetParent(transform, false);
        var overlay = canvas.GetComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        overlay.sortingOrder = 21;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = new GameObject("Hint", typeof(Image), typeof(CanvasGroup));
        var image = panel.GetComponent<Image>();
        image.color = new Color32(22, 24, 25, 236);
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -38f);
        rect.sizeDelta = new Vector2(640f, 126f);

        _hint = panel.GetComponent<CanvasGroup>();
        _hint.alpha = 0f;
        _title = MakeLabel("Title", rect, 29, 10f, 36f);
        _instruction = MakeLabel("Instruction", rect, 23, 51f, 66f);

    }

    private static Text MakeLabel(string name, Transform parent, int fontSize, float top, float height)
    {
        var label = new GameObject(name, typeof(Text)).GetComponent<Text>();
        label.transform.SetParent(parent, false);
        label.font = HudStyle.PixelFont;
        label.fontSize = fontSize;
        label.color = HudStyle.Text;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(16f, -top - height);
        rect.offsetMax = new Vector2(-16f, -top);
        return label;
    }

    private void Show(string title, string instruction)
    {
        _title.text = title;
        _instruction.text = instruction;
        _hint.DOKill();
        _hint.alpha = 0f;
        _hint.DOFade(1f, 0.25f).SetUpdate(true);
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.UnregisterLethalHitRecovery(PreventLethalHit);
        if (_input != null)
            _input.InteractPressed -= ConfirmMovement;
        if (_interactions != null)
            _interactions.WeaponUseStarted -= OnWeaponUseStarted;
        ReleaseStrongGoblin();
        ClearAbilityFocus();
        Shader.SetGlobalFloat(FocusAmount, 0f);
        if (_hint != null)
            _hint.DOKill();
    }
}

