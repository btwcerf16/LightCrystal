using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerInputReader : MonoBehaviour
{
    private PlayerControls _controls;

    public event Action<Vector2> MoveChanged;
    public event Action<Vector2> AimChanged;

    public Vector2 MoveDirection { get; private set; }
    public Vector2 AimScreenPosition { get; private set; }

    private void Awake()
    {
        _controls = new PlayerControls();
    }

    private void OnEnable()
    {
        _controls.PlayerGameInput.Move.performed += OnMove;
        _controls.PlayerGameInput.Move.canceled += OnMove;
        _controls.PlayerGameInput.Aim.performed += OnAim;

        _controls.PlayerGameInput.Enable();

        SetAimPosition(_controls.PlayerGameInput.Aim.ReadValue<Vector2>());
    }

    private void OnDisable()
    {
        _controls.PlayerGameInput.Move.performed -= OnMove;
        _controls.PlayerGameInput.Move.canceled -= OnMove;
        _controls.PlayerGameInput.Aim.performed -= OnAim;

        _controls.PlayerGameInput.Disable();

        SetMoveDirection(Vector2.zero);
    }

    private void OnDestroy()
    {
        _controls.Dispose();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        SetMoveDirection(context.ReadValue<Vector2>());
    }

    private void OnAim(InputAction.CallbackContext context)
    {
        SetAimPosition(context.ReadValue<Vector2>());
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