using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerSlotBindings : NetworkBehaviour
{
    [SerializeField] private LocalPlayerSettings _settings;

    private InputActionMap _actions;
    private InputAction[] _inventoryActions;
    private InputAction[] _arsenalActions;
    private InputAction[] _spellActions;
    private SpellHolder _spells;
    private InputActionRebindingExtensions.RebindingOperation _rebind;
    private PlayerInputReader _input;

    public event Action BindingsChanged;
    public bool IsRebinding => _rebind != null;

    private void Awake()
    {
        _input = GetComponent<PlayerInputReader>();
        Inventory inventory = GetComponent<Inventory>();
        ActorArsenal arsenal = GetComponent<ActorArsenal>();
        _actions = new InputActionMap("Slots");
        _inventoryActions = new InputAction[inventory.Size];
        _arsenalActions = new InputAction[arsenal.WeaponSize];
        _spells = GetComponent<SpellHolder>();
        _spellActions = new InputAction[_spells.Count];
        _settings.Load(inventory.Size, arsenal.WeaponSize, _spells.Count);

        for (int i = 0; i < _inventoryActions.Length; i++)
        {
            int index = i;
            string path = _settings.GetBinding(false, i);
            _inventoryActions[i] = CreateAction(ESlotBindingType.Inventory, i, path);
            _inventoryActions[i].performed += context =>
            {
                if (!_input.IsMenuOpen && !_input.IsStatsOpen)
                    inventory.RequestUseItem(index);
            };
        }

        for (int i = 0; i < _arsenalActions.Length; i++)
        {
            int index = i;
            string path = _settings.GetBinding(true, i);
            _arsenalActions[i] = CreateAction(ESlotBindingType.Arsenal, i, path);
            _arsenalActions[i].performed += context =>
            {
                if (!_input.IsMenuOpen && !_input.IsStatsOpen)
                    arsenal.RequestSelectWeapon(index);
            };
        }

        for (int i = 0; i < _spellActions.Length; i++)
        {
            SpellConfig config = _spells.GetConfig(i);
            if (config == null || config.IsPassive)
                continue;
            int index = i;
            _spellActions[i] = CreateAction(ESlotBindingType.Spell, i, _settings.GetBinding(ESlotBindingType.Spell, i));
            _spellActions[i].performed += context => UseSpell(index);
        }

        HashSet<string> usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (InputAction action in _actions)
        {
            string path = action.bindings[0].effectivePath;
            if (!string.IsNullOrEmpty(path) && (IsReserved(path) || !usedPaths.Add(path)))
                action.ApplyBindingOverride(0, "");
        }

        InputSystem.onDeviceChange += OnDeviceChanged;
    }

    private void OnDeviceChanged(InputDevice device, InputDeviceChange change)
    {
        if (!IsSpawned || !IsOwner || !IsClient || !(device is Keyboard))
            return;

        if (change == InputDeviceChange.ConfigurationChanged || change == InputDeviceChange.Reconnected)
            BindingsChanged?.Invoke();
    }

    private InputAction CreateAction(ESlotBindingType type, int index, string path)
    {
        InputAction action = _actions.AddAction(type.ToString() + index, InputActionType.Button);
        action.AddBinding(path);
        return action;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner && IsClient)
            _actions.Enable();
    }

    public override void OnGainedOwnership()
    {
        if (IsClient && IsSpawned)
            _actions.Enable();
    }

    public override void OnLostOwnership() => StopInput();
    public override void OnNetworkDespawn() => StopInput();

    private void StopInput()
    {
        CancelRebind();
        _actions.Disable();
    }

    public string GetBindingLabel(bool isWeapon, int index)
    {
        return GetBindingLabel(isWeapon ? ESlotBindingType.Arsenal : ESlotBindingType.Inventory, index);
    }

    public string GetBindingLabel(ESlotBindingType type, int index)
    {
        InputAction action = GetAction(type, index);
        if (action == null)
            return "—";
        string label = action.GetBindingDisplayString(0);
        return string.IsNullOrEmpty(label) ? "—" : label;
    }

    public void BeginRebind(bool isWeapon, int index, Action<string> completed)
    {
        BeginRebind(isWeapon ? ESlotBindingType.Arsenal : ESlotBindingType.Inventory, index, completed);
    }

    public void BeginRebind(ESlotBindingType type, int index, Action<string> completed)
    {
        if (!IsOwner || !_input.IsMenuOpen || IsRebinding)
            return;

        InputAction action = GetAction(type, index);
        if (action == null)
        {
            completed("Пассивной способности клавиша не нужна.");
            return;
        }
        string previousPath = action.bindings[0].effectivePath;
        action.Disable();
        _rebind = action.PerformInteractiveRebinding(0)
            .WithControlsHavingToMatchPath("<Keyboard>")
            .WithControlsExcluding("<Keyboard>/anyKey")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnCancel(operation =>
            {
                FinishRebind(action);
                completed("Привязка не изменена.");
            })
            .OnComplete(operation =>
            {
                string path = action.bindings[0].effectivePath;
                string message;
                if (IsReserved(path) || IsAssigned(path, action))
                {
                    action.ApplyBindingOverride(0, previousPath);
                    message = "Клавиша уже используется. Выбери другую.";
                }
                else
                {
                    _settings.SetBinding(type, index, path);
                    message = "Привязка сохранена.";
                }
                FinishRebind(action);
                BindingsChanged?.Invoke();
                completed(message);
            });
        _rebind.Start();
    }

    public void CancelRebind()
    {
        if (_rebind != null)
            _rebind.Cancel();
    }

    public void ResetBindings()
    {
        if (!IsSpawned || !IsOwner || !IsClient || !_input.IsMenuOpen)
            return;

        CancelRebind();
        _settings.ResetBindings();
        _actions.Disable();
        for (int i = 0; i < _inventoryActions.Length; i++)
            _inventoryActions[i].ApplyBindingOverride(0, _settings.GetBinding(false, i));
        for (int i = 0; i < _arsenalActions.Length; i++)
            _arsenalActions[i].ApplyBindingOverride(0, _settings.GetBinding(true, i));
        for (int i = 0; i < _spellActions.Length; i++)
            if (_spellActions[i] != null)
                _spellActions[i].ApplyBindingOverride(0, _settings.GetBinding(ESlotBindingType.Spell, i));
        _actions.Enable();
        BindingsChanged?.Invoke();
    }

    public void UseSpell(int index)
    {
        if (!IsSpawned || !IsOwner || !IsClient || !_input.isActiveAndEnabled || _input.IsMenuOpen || _input.IsStatsOpen)
            return;
        _spells.CastSpell(index, new SpellCastData { Position = transform.position });
    }

    private InputAction GetAction(ESlotBindingType type, int index)
    {
        InputAction[] actions;
        switch (type)
        {
            case ESlotBindingType.Spell: actions = _spellActions; break;
            case ESlotBindingType.Arsenal: actions = _arsenalActions; break;
            default: actions = _inventoryActions; break;
        }
        return index >= 0 && index < actions.Length ? actions[index] : null;
    }

    private void FinishRebind(InputAction action)
    {
        _rebind.Dispose();
        _rebind = null;
        if (IsSpawned && IsOwner && IsClient)
            action.Enable();
    }

    private bool IsReserved(string path)
    {
        return string.Equals(path, "<Keyboard>/escape", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "<Keyboard>/backquote", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "<Keyboard>/t", StringComparison.OrdinalIgnoreCase)
            || _input.IsGameplayBinding(path);
    }

    private bool IsAssigned(string path, InputAction ignoredAction)
    {
        if (string.IsNullOrEmpty(path))
            return false;
        foreach (InputAction action in _actions)
            if (action != ignoredAction && string.Equals(action.bindings[0].effectivePath, path, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    public override void OnDestroy()
    {
        InputSystem.onDeviceChange -= OnDeviceChanged;
        StopInput();
        _actions.Dispose();
        base.OnDestroy();
    }
}
