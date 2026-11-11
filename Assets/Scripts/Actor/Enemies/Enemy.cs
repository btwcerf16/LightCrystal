using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(ActorStats), typeof(ActorHealth))]
[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public abstract class Enemy : MonoBehaviour, ILightSensitive
{
    [SerializeField, Min(0f)] private float _attackDistance = 1.4f;
    [SerializeField] private Transform _mainTarget;
    [SerializeField] protected LayerMask _targetMask = ~0;
    [SerializeField] protected Transform _facing;

    protected Rigidbody2D _body;
    protected Animator _animator;
    protected Vector2 _moveDirection;
    protected bool _isAttacking;

    private readonly Dictionary<Type, State> _states = new();
    private readonly StateMachine _stateMachine = new();
    private bool _isInitialized;
    private ActorHealth _mainTargetHealth;
    [SerializeField, Min(0.1f)] private float _separationRadius = 0.85f;
    [SerializeField, Min(0f)] private float _separationStrength = 0.9f;
    private readonly Collider2D[] _neighbors = new Collider2D[24];
    private Vector2 _separation;
    private Coroutine _knockback;
    private float _nextSeparationCheck;

    protected EffectHandler Effects { get; private set; }
    protected bool IsStunned => Effects != null && Effects.IsStunned;
    protected abstract State ChaseState { get; }
    protected ActorStats Stats { get; private set; }
    protected ActorHealth Health { get; private set; }
    protected abstract State InitialState { get; }
    protected abstract State DeadState { get; }

    public Transform MainTarget => _mainTargetHealth != null && _mainTargetHealth.IsDead ? null : _mainTarget;
    private Transform _temporaryTarget;
    private ActorHealth _temporaryTargetHealth;
    public bool IsWaitingForWave { get; private set; }
    public Transform TemporaryTarget => _temporaryTargetHealth != null && _temporaryTargetHealth.IsDead ? null : _temporaryTarget;
    public Transform CurrentTarget => TemporaryTarget != null ? TemporaryTarget : IsWaitingForWave ? null : MainTarget;
    public State CurrentState => _stateMachine.CurrentState;
    public bool IsChasing => _isInitialized && CurrentState == ChaseState;

    protected virtual void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        Stats = GetComponent<ActorStats>();
        Effects = GetComponent<EffectHandler>();
        _nextSeparationCheck = Time.time + UnityEngine.Random.Range(0f, 0.12f);
        Health = GetComponent<ActorHealth>();
        if (_mainTarget != null)
            _mainTargetHealth = _mainTarget.GetComponent<ActorHealth>();
    }

    public void Initialize(Transform mainTarget)
    {
        if (_isInitialized)
            return;

        SetMainTarget(mainTarget);
        _isInitialized = true;
        Health.Died += OnDied;
        if (Effects != null)
            Effects.StatusChanged += OnStatusChanged;
        _stateMachine.Initialize(Health.IsDead ? DeadState : InitialState);
    }

    protected virtual void Update()
    {
        if (IsStunned)
        {
            StopChase();
            return;
        }
        _stateMachine.Update();
    }

    protected virtual void OnDisable()
    {
        if (_isInitialized)
            Health.Died -= OnDied;
        if (Effects != null)
            Effects.StatusChanged -= OnStatusChanged;
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
        if (!_isInitialized || Health.IsDead || IsStunned || CurrentTarget == null)
            return false;
        Collider2D collider = CurrentTarget.GetComponent<Collider2D>();
        Vector2 targetPosition = collider != null ? collider.ClosestPoint(transform.position) : CurrentTarget.position;
        Vector2 offset = targetPosition - (Vector2)transform.position;
        return offset.sqrMagnitude <= _attackDistance * _attackDistance;
    }

    private void OnStatusChanged()
    {
        if (!IsStunned || !_isInitialized || Health.IsDead)
            return;
        StopChase();
        CancelAttack();
        _stateMachine.ChangeState(ChaseState);
    }

    protected Transform FindPlayerTarget()
    {
        Transform bestTarget = null;
        int bestPriority = int.MinValue;
        float bestDistanceSquared = float.PositiveInfinity;
        foreach (Collider2D candidate in Physics2D.OverlapCircleAll(transform.position, Stats.ViewDistance.Value, _targetMask))
        {
            PlayerInputReader player = candidate.GetComponentInParent<PlayerInputReader>();
            if (player == null || player.GetComponent<ActorHealth>().IsDead)
                continue;

            int priority = player.GetComponent<ActorStats>().TargetPriority;
            float distanceSquared = ((Vector2)(player.transform.position - transform.position)).sqrMagnitude;
            if (priority > bestPriority || (priority == bestPriority && distanceSquared < bestDistanceSquared))
            {
                bestTarget = player.transform;
                bestPriority = priority;
                bestDistanceSquared = distanceSquared;
            }
        }

        return bestTarget;
    }

    protected Vector2 GetChaseDirection(Vector2 target)
    {
        if (IsStunned)
            return Vector2.zero;
        Vector2 position = transform.position;
        if (Time.time >= _nextSeparationCheck)
        {
            _nextSeparationCheck = Time.time + 0.12f;
            _separation = Vector2.zero;
            ContactFilter2D filter = new();
            filter.SetLayerMask(1 << gameObject.layer);
            filter.useTriggers = true;
            int count = Physics2D.OverlapCircle(position, _separationRadius, filter, _neighbors);
            for (int i = 0; i < count; i++)
            {
                Enemy other = _neighbors[i].GetComponentInParent<Enemy>();
                if (other == null || other == this || !other.isActiveAndEnabled || other.Health.IsDead)
                    continue;
                Vector2 offset = position - (Vector2)other.transform.position;
                float distance = offset.magnitude;
                if (distance < 0.001f)
                {
                    float angle = (Mathf.Min(GetInstanceID(), other.GetInstanceID()) % 360) * Mathf.Deg2Rad;
                    offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (GetInstanceID() < other.GetInstanceID() ? -1f : 1f);
                }
                _separation += offset.normalized * (1f - Mathf.Clamp01(distance / _separationRadius));
            }
        }
        Vector2 direction = (target - position).normalized;
        return Vector2.ClampMagnitude(direction + _separation * _separationStrength, 1f);
    }

    public void GiveGold(ActorWallet killerWallet)
    {
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (killerWallet == null || networkObject == null || !networkObject.IsSpawned || !networkObject.NetworkManager.IsServer)
            return;
        EnemyDatabase database = networkObject.NetworkManager.GetComponent<WaveDirector>()?.EnemyDatabase;
        int weight = database != null ? database.GetWeight(this) : 0;
        if (weight > 0 && UnityEngine.Random.value < 0.4f)
            killerWallet.AddCurrency(weight * 3);
    }

    private void OnDied()
    {
        _stateMachine.ChangeState(DeadState);
        StartCoroutine(ReturnAfterDeath());
    }

    private IEnumerator ReturnAfterDeath()
    {
        yield return new WaitForSeconds(1f);
        NetworkObject item = GetComponent<NetworkObject>();
        if (item != null && item.IsSpawned && item.NetworkManager.IsServer)
            item.NetworkManager.GetComponent<NetworkObjectPool>().ReturnObject(item);
    }

    public void ResetForSpawn()
    {
        StopAllCoroutines();
        _knockback = null;
        ClearTemporaryTarget();
        _mainTarget = null;
        _mainTargetHealth = null;
        IsWaitingForWave = false;
        _separation = Vector2.zero;
        _nextSeparationCheck = 0f;
        Health.ResetHealth();
        CancelAttack();
        StopChase();
        foreach (Collider2D body in GetComponents<Collider2D>())
            body.enabled = true;
        _animator.Rebind();
        _animator.Update(0f);
    }

    public void ApplyKnockback(Vector2 direction, float strength, float duration)
    {
        if (Health.IsDead || strength <= 0f || duration <= 0f)
            return;
        strength *= 1f - Stats.ForceResistance.Value;
        if (strength <= 0f)
            return;
        if (_knockback != null)
            StopCoroutine(_knockback);
        _knockback = StartCoroutine(Knockback(direction.normalized * strength, duration));
    }

    private IEnumerator Knockback(Vector2 velocity, float duration)
    {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        float remaining = duration;
        while (remaining > 0f && !Health.IsDead)
        {
            yield return wait;
            _body.MovePosition(_body.position + velocity * (remaining / duration) * Time.fixedDeltaTime);
            remaining -= Time.fixedDeltaTime;
        }
        _knockback = null;
    }

    public void SetMainTarget(Transform target)
    {
        if (_mainTarget != null || target == null)
            return;
        _mainTarget = target;
        _mainTargetHealth = target.GetComponent<ActorHealth>();
    }

    public void SetTemporaryTarget(Transform target)
    {
        _temporaryTarget = target;
        _temporaryTargetHealth = target != null ? target.GetComponent<ActorHealth>() : null;
    }

    public void ClearTemporaryTarget()
    {
        _temporaryTarget = null;
        _temporaryTargetHealth = null;
    }

    public void SetWaitingForWave(bool waiting)
    {
        IsWaitingForWave = waiting;
    }

    public virtual void EnterLight() { }
    public virtual void ExitLight() { }

    public virtual void ReceiveLight(float amount)
    {
        Health.TakeDamage(amount, EDamageType.Magic);
    }

    public abstract void Arise();
    public abstract void Chase(Transform target);
    public event Action AttackStarted;
    public virtual bool CanBeExecuted => true;
    protected void NotifyAttackStarted() => AttackStarted?.Invoke();
    public abstract void Attack();
    public abstract void Death();
    public virtual void StopChase()
    {
        _moveDirection = Vector2.zero;
        if (_body != null)
            _body.linearVelocity = Vector2.zero;
        if (_animator != null)
            _animator.SetBool("IsMoving", false);
    }

    public virtual void CancelAttack() { }
}
