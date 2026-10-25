using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(ActorStats), typeof(ActorHealth))]
public abstract class Enemy : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _attackDistance = 1.4f;
    [SerializeField] private Transform _mainTarget;
    private readonly Dictionary<Type, State> _states = new();
    private readonly StateMachine _stateMachine = new();
    private bool _isInitialized;

    protected ActorStats Stats { get; private set; }
    protected ActorHealth Health { get; private set; }
    protected abstract State InitialState { get; }
    protected abstract State DeadState { get; }

    public Transform MainTarget => _mainTarget;
    public Transform TemporaryTarget { get; private set; }
    public Transform CurrentTarget => TemporaryTarget != null ? TemporaryTarget : MainTarget;
    public State CurrentState => _stateMachine.CurrentState;

    protected virtual void Awake()
    {
        Stats = GetComponent<ActorStats>();
        Health = GetComponent<ActorHealth>();
    }

    public void Initialize(Transform mainTarget)
    {
        if (_isInitialized)
            return;

        SetMainTarget(mainTarget);
        _isInitialized = true;
        Health.Died += OnDied;
        _stateMachine.Initialize(Health.IsDead ? DeadState : InitialState);
    }

    protected virtual void Update()
    {
        _stateMachine.Update();
    }

    protected virtual void OnDisable()
    {
        if (_isInitialized)
            Health.Died -= OnDied;
        _isInitialized = false;
        _stateMachine.Stop();
    }

    protected void RegisterState(State state)
    {
        _states[state.GetType()] = state;
    }

    protected T GetState<T>() where T : State
    {
        return (T)_states[typeof(T)];
    }

    public void ChangeState<T>() where T : State
    {
        if (!_isInitialized || CurrentState == DeadState)
            return;
        if (Health.IsDead)
            return;
        _stateMachine.ChangeState(GetState<T>());
    }

    public virtual bool CanAttack()
    {
        if (!_isInitialized || Health.IsDead || CurrentTarget == null)
            return false;
        Vector2 offset = CurrentTarget.position - transform.position;
        return offset.sqrMagnitude <= _attackDistance * _attackDistance;
    }

    private void OnDied()
    {
        _stateMachine.ChangeState(DeadState);
    }

    public void SetMainTarget(Transform target)
    {
        if (MainTarget != null || target == null)
            return;
        _mainTarget = target;
    }

    public void SetTemporaryTarget(Transform target)
    {
        TemporaryTarget = target;
    }

    public void ClearTemporaryTarget()
    {
        TemporaryTarget = null;
    }

    public abstract void Arise();
    public abstract void Chase(Transform target);
    public abstract void Attack();
    public abstract void Death();
    public virtual void StopChase() { }
    public virtual void CancelAttack() { }
}
