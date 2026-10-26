using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public sealed class Goblin : Enemy
{
    [SerializeField] private Transform _facing;
    [SerializeField] private Transform _weaponAim;
    [SerializeField] private Transform _attackPoint;
    [SerializeField] private Collider2D _bodyCollider;
    [SerializeField, Min(0f)] private float _attackRadius = 0.25f;
    [SerializeField] private LayerMask _targetMask = ~0;

    private Rigidbody2D _body;
    private Animator _animator;
    private Vector2 _moveDirection;
    private Vector2 _aimDirection = Vector2.right;
    private float _nextTargetSearch;
    private bool _isAttacking;
    private bool _isSubscribed;
    private readonly HashSet<IDamageable> _hitTargets = new();

    public event Action<string> AnimationTriggered;
    public event Action<Vector2> AimChanged;

    protected override State InitialState => GetState<GoblinAriseState>();
    protected override State DeadState => GetState<GoblinDeathState>();

    protected override void Awake()
    {
        base.Awake();
        _body = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        RegisterState(new GoblinAriseState(this));
        RegisterState(new GoblinChaseState(this));
        RegisterState(new GoblinAttackState(this));
        RegisterState(new GoblinDeathState(this));
    }

    private void OnEnable()
    {
        Stats.AttackSpeed.ValueChanged += UpdateAttackSpeed;
        _isSubscribed = true;
        UpdateAttackSpeed(Stats.AttackSpeed.Value);
    }

    protected override void OnDisable()
    {
        if (_isSubscribed)
        {
            Stats.AttackSpeed.ValueChanged -= UpdateAttackSpeed;
            _isSubscribed = false;
        }
        base.OnDisable();
        StopChase();
    }

    protected override void Update()
    {
        if (CurrentState != null && !Health.IsDead && Time.time >= _nextTargetSearch)
        {
            _nextTargetSearch = Time.time + 0.2f;
            FindTarget();
        }
        base.Update();
    }

    private void FixedUpdate()
    {
        _body.linearVelocity = _moveDirection * Stats.MoveSpeed.Value;
    }

    private void FindTarget()
    {
        Transform bestTarget = null;
        int bestPriority = int.MinValue;
        float bestDistance = float.PositiveInfinity;
        foreach (Collider2D candidate in Physics2D.OverlapCircleAll(transform.position, Stats.ViewDistance.Value, _targetMask))
        {
            PlayerInputReader player = candidate.GetComponentInParent<PlayerInputReader>();
            if (player == null)
                continue;
            ActorHealth health = player.GetComponent<ActorHealth>();
            if (health == null || health.IsDead)
                continue;
            ActorStats stats = player.GetComponent<ActorStats>();
            int priority = stats != null ? stats.TargetPriority : 0;
            float distance = ((Vector2)player.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (priority > bestPriority || (priority == bestPriority && distance < bestDistance))
            {
                bestTarget = player.transform;
                bestPriority = priority;
                bestDistance = distance;
            }
        }
        SetTemporaryTarget(bestTarget);
    }

    private void AimAt(Transform target)
    {
        Collider2D targetCollider = target.GetComponent<Collider2D>();
        Vector2 targetPosition = targetCollider != null ? targetCollider.bounds.center : target.position;
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.375f;
        SetAim(targetPosition - origin);
    }

    public void SetAim(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.000001f)
            return;
        direction.Normalize();
        float side = direction.x < 0f ? -1f : 1f;
        _facing.localScale = new Vector3(side, 1f, 1f);
        float angle = Mathf.Atan2(direction.y, direction.x * side) * Mathf.Rad2Deg;
        _weaponAim.localRotation = Quaternion.Euler(0f, 0f, angle);
        if ((_aimDirection - direction).sqrMagnitude > 0.0001f)
        {
            _aimDirection = direction;
            AimChanged?.Invoke(direction);
        }
    }

    public override void Arise()
    {
        StopChase();
        _animator.Play("Arise", 0, 0f);
    }

    public void FinishArise()
    {
        if (isActiveAndEnabled && CurrentState is GoblinAriseState)
            ChangeState<GoblinChaseState>();
    }

    public override void Chase(Transform target)
    {
        AimAt(target);
        _moveDirection = ((Vector2)target.position - _body.position).normalized;
        _animator.SetBool("IsMoving", _moveDirection.sqrMagnitude > 0f);
    }

    public override void StopChase()
    {
        _moveDirection = Vector2.zero;
        if (_body != null)
            _body.linearVelocity = Vector2.zero;
        if (_animator != null)
            _animator.SetBool("IsMoving", false);
    }

    public override bool CanAttack()
    {
        return base.CanAttack() && Stats.AttackSpeed.Value > 0f;
    }

    public override void Attack()
    {
        AimAt(CurrentTarget);
        _isAttacking = true;
        _hitTargets.Clear();
        TriggerAnimation("Attack");
    }

    public void StartThrust()
    {
        if (!isActiveAndEnabled || !_isAttacking || Health.IsDead)
            return;
        foreach (Collider2D target in Physics2D.OverlapCircleAll(_attackPoint.position, _attackRadius, _targetMask))
        {
            if (target.GetComponentInParent<Enemy>() != null)
                continue;
            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable == null || !_hitTargets.Add(damageable))
                continue;
            damageable.TakeDamage(Stats.PhysicalDamage.Value, EDamageType.Physical, Stats);
        }
    }

    public void FinishAttack()
    {
        if (!isActiveAndEnabled || CurrentState is not GoblinAttackState)
            return;
        _isAttacking = false;
        ChangeState<GoblinChaseState>();
    }

    public override void CancelAttack()
    {
        _isAttacking = false;
        _animator.ResetTrigger("Attack");
    }

    public override void Death()
    {
        StopChase();
        CancelAttack();
        _bodyCollider.enabled = false;
        TriggerAnimation("Death");
    }

    private void TriggerAnimation(string trigger)
    {
        _animator.SetTrigger(trigger);
        AnimationTriggered?.Invoke(trigger);
    }

    private void UpdateAttackSpeed(float speed)
    {
        _animator.SetFloat("AttackSpeed", Mathf.Max(0f, speed));
    }

    private void OnDrawGizmosSelected()
    {
        if (_attackPoint == null)
            return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_attackPoint.position, _attackRadius);
    }
}
