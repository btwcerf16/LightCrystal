using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[DefaultExecutionOrder(-10)]
public sealed class PlayerInputReader : MonoBehaviour
{
    private PlayerControls _controls;
    private bool _isDisabling;

    public event Action<Vector2> MoveChanged;
    public event Action<Vector2> AimChanged;
    public event Action PrimaryPressed;
    public event Action PrimaryReleased;
    public event Action SecondaryPressed;
    public event Action SecondaryReleased;
    public event Action ActionsCanceled;
    public event Action StatsToggled;
    public event Action InteractPressed;
    public event Action DropWeaponPressed;
    public bool IsStatsOpen { get; private set; }
    public bool IsMenuOpen { get; private set; }

    public Vector2 MoveDirection { get; private set; }
    public Vector2 AimScreenPosition { get; private set; }


    private void Awake()
    {
        _controls = new PlayerControls();
    }

    private void OnEnable()
    {
        _isDisabling = false;
        _controls.PlayerGameInput.Move.performed += OnMove;
        _controls.PlayerGameInput.Move.canceled += OnMove;
        _controls.PlayerGameInput.Aim.performed += OnAim;
        _controls.PlayerGameInput.PrimaryUse.performed += OnPrimaryUse;
        _controls.PlayerGameInput.PrimaryUse.canceled += OnPrimaryUse;
        _controls.PlayerGameInput.SecondaryUse.performed += OnSecondaryUse;
        _controls.PlayerGameInput.SecondaryUse.canceled += OnSecondaryUse;


        _controls.PlayerGameInput.Interact.performed += OnInteract;
        _controls.PlayerGameInput.DropWeapon.performed += OnDropWeapon;
        _controls.PlayerGameInput.ToggleStats.performed += OnToggleStats;
        _controls.PlayerGameInput.Enable();

        SetAimPosition(_controls.PlayerGameInput.Aim.ReadValue<Vector2>());
    }

    public void OnSecondaryUse(InputAction.CallbackContext context)
    {
        if (_isDisabling || IsStatsOpen || IsMenuOpen)
            return;
        if (context.performed)
        {
            if (IsPointerOverUI())
                return;
            SecondaryPressed?.Invoke();
        }
        else if (context.canceled)
        {
            SecondaryReleased?.Invoke();
        }
    }

    public void OnPrimaryUse(InputAction.CallbackContext context)
    {
        if (_isDisabling || IsStatsOpen || IsMenuOpen)
            return;
        if (context.performed)
        {
            if (IsPointerOverUI())
                return;
            PrimaryPressed?.Invoke();
        }
        else if (context.canceled) 
        {
            PrimaryReleased?.Invoke();
        }
        
    }

    public void CloseStats()
    {
        if (!IsStatsOpen)
            return;
        IsStatsOpen = false;
        StatsToggled?.Invoke();
    }

    private void OnToggleStats(InputAction.CallbackContext context)
    {
        if (IsMenuOpen)
            return;
        IsStatsOpen = !IsStatsOpen;
        if (IsStatsOpen)
            ActionsCanceled?.Invoke();
        StatsToggled?.Invoke();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (!_isDisabling && !IsStatsOpen && !IsMenuOpen)
            InteractPressed?.Invoke();
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null || Mouse.current == null)
            return false;
        PointerEventData pointer = new PointerEventData(EventSystem.current);
        pointer.position = Mouse.current.position.ReadValue();
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);
        return results.Count > 0;
    }

    private void OnDropWeapon(InputAction.CallbackContext context)
    {
        if (!_isDisabling && !IsStatsOpen && !IsMenuOpen)
            DropWeaponPressed?.Invoke();
    }

    private void OnDisable()
    {
        CloseStats();
        _controls.PlayerGameInput.Interact.performed -= OnInteract;
        _controls.PlayerGameInput.DropWeapon.performed -= OnDropWeapon;
        _controls.PlayerGameInput.ToggleStats.performed -= OnToggleStats;
        _isDisabling = true;
        ActionsCanceled?.Invoke();
       
        _controls.PlayerGameInput.Disable();

        _controls.PlayerGameInput.Move.performed -= OnMove;
        _controls.PlayerGameInput.Move.canceled -= OnMove;
        _controls.PlayerGameInput.Aim.performed -= OnAim;
        _controls.PlayerGameInput.PrimaryUse.performed -= OnPrimaryUse;
        _controls.PlayerGameInput.PrimaryUse.canceled -= OnPrimaryUse;
        _controls.PlayerGameInput.SecondaryUse.performed -= OnSecondaryUse;
        _controls.PlayerGameInput.SecondaryUse.canceled -= OnSecondaryUse;

        SetMoveDirection(Vector2.zero);
    }

    private void OnDestroy()
    {
        _controls.Dispose();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        SetMoveDirection(IsMenuOpen ? Vector2.zero : context.ReadValue<Vector2>());
    }

    private void OnAim(InputAction.CallbackContext context)
    {
        if (IsMenuOpen)
            return;
        SetAimPosition(context.ReadValue<Vector2>());
    }

    public void SetMenuOpen(bool isOpen)
    {
        IsMenuOpen = isOpen;
        if (isOpen)
        {
            CloseStats();
            SetMoveDirection(Vector2.zero);
            ActionsCanceled?.Invoke();
        }
        else if (isActiveAndEnabled)
        {
            SetMoveDirection(_controls.PlayerGameInput.Move.ReadValue<Vector2>());
            SetAimPosition(_controls.PlayerGameInput.Aim.ReadValue<Vector2>());
        }
    }

    public bool IsGameplayBinding(string path)
    {
        foreach (InputBinding binding in _controls.asset.bindings)
            if (!binding.isComposite && string.Equals(binding.effectivePath, path, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private void SetMoveDirection(Vector2 direction)
    {
        if (MoveDirection == direction)
            return;

        MoveDirection = direction;
        MoveChanged?.Invoke(direction);
    }

    private void SetAimPosition(Vector2 screenPosition)
    {
        if (AimScreenPosition == screenPosition)
            return;

        AimScreenPosition = screenPosition;
        AimChanged?.Invoke(screenPosition);
    }
}
