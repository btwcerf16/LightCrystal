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
    [SerializeField, Range(0f, 1f)] private float _goldDropChance = 0.4f;
    [SerializeField, Min(0), Tooltip("Base gold before multipliers. Zero uses the enemy catalog weight.")]
    private int _goldDropAmount;

    protected Rigidbody2D _body;
    protected Animator _animator;
    protected Vector2 _moveDirection;
    protected bool _isAttacking;

    private readonly Dictionary<Type, State> _states = new();
    private readonly HashSet<ActorWallet> _goldContributors = new();
    private float _goldDropMultiplier = 1f;
    private readonly StateMachine _stateMachine = new();
    private bool _isInitialized;
    private ActorHealth _mainTargetHealth;
    [SerializeField, Min(0.1f)] private float _separationRadius = 0.85f;
    [SerializeField, Min(0f)] private float _separationStrength = 0.9f;
    private readonly Collider2D[] _neighbors = new Collider2D[24];
    private readonly List<Collider2D> _targetHits = new();
    private Vector2 _separation;
    private Coroutine _knockback;
    private float _nextSeparationCheck;

    protected EffectHandler Effects { get; private set; }
    protected float AttackDistance => _attackDistance;
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

    protected Transform FindPlayerTarget(bool requireCenterInRange = false)
    {
        Vector2 position = transform.position;
        float viewDistance = Stats.ViewDistance.Value;
        var filter = new ContactFilter2D { useTriggers = Physics2D.queriesHitTriggers };
        filter.SetLayerMask(_targetMask & DamageCalculator.CombatLayers);
        int count = Physics2D.OverlapCircle(position, viewDistance, filter, _targetHits);
        Transform bestTarget = null;
        int bestPriority = int.MinValue;
        float bestDistanceSquared = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            PlayerInputReader player = _targetHits[i].GetComponentInParent<PlayerInputReader>();
            if (player == null || !player.TryGetComponent(out ActorHealth health) || health.IsDead)
                continue;

            float distanceSquared = ((Vector2)player.transform.position - position).sqrMagnitude;
            if (requireCenterInRange && distanceSquared > viewDistance * viewDistance)
                continue;
            int priority = player.TryGetComponent(out ActorStats stats) ? stats.TargetPriority : 0;
            if (priority > bestPriority || (priority == bestPriority && distanceSquared < bestDistanceSquared))
            {
                bestTarget = player.transform;
                bestPriority = priority;
                bestDistanceSquared = distanceSquared;
            }
        }

        _targetHits.Clear();
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

    public void SetGoldDropMultiplier(float multiplier) => _goldDropMultiplier = multiplier;

    public void RecordGoldContributor(ActorStats attacker)
    {
        if (attacker.TryGetComponent(out NetworkObject player) && player.IsSpawned && player.IsPlayerObject
            && player.NetworkManager.IsServer && attacker.TryGetComponent(out ActorWallet wallet))
            _goldContributors.Add(wallet);
    }

    private void GiveGold()
    {
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (_goldContributors.Count == 0 || networkObject == null || !networkObject.IsSpawned || !networkObject.NetworkManager.IsServer)
            return;
        if (_goldDropChance <= 0f || (_goldDropChance < 1f && UnityEngine.Random.value >= _goldDropChance))
            return;

        int amount = _goldDropAmount;
        if (amount == 0)
        {
            EnemyDatabase database = networkObject.NetworkManager.GetComponent<WaveDirector>()?.EnemyDatabase;
            amount = (database != null ? database.GetWeight(this) : 0) * 3;
        }
        if (amount <= 0)
            return;

        _goldContributors.RemoveWhere(wallet => wallet == null || !wallet.GetComponent<NetworkObject>().IsSpawned);
        if (_goldContributors.Count == 0)
            return;

        int share = Mathf.FloorToInt(amount * _goldDropMultiplier) / _goldContributors.Count;
        foreach (ActorWallet wallet in _goldContributors)
            wallet.AddCurrency(share, transform.position);
    }

    private void OnDied()
    {
        GiveGold();
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
        _goldContributors.Clear();
        _goldDropMultiplier = 1f;
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
    public virtual bool CanBeExecuted => !CompareTag("Boss");
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
