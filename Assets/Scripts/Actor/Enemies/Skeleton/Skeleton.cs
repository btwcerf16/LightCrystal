using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public sealed class Skeleton : Enemy
{
    [SerializeField] private Transform _facing;
    [SerializeField] private Transform _weaponAim;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private Collider2D _bodyCollider;
    [SerializeField] private SkeletonArrow _arrowPrefab;
    [SerializeField, Min(0f)] private float _arrowSpeed = 12f;
    [SerializeField, Min(0.1f)] private float _arrowLifetime = 4f;
    [SerializeField] private LayerMask _arrowHitMask = ~0;
    [SerializeField] private LayerMask _targetMask = ~0;

    private Rigidbody2D _body;
    private Animator _animator;
    private Vector2 _aimDirection = Vector2.right;
    private Vector2 _shotDirection;
    private float _nextTargetSearch;
    private bool _isShooting;
    private bool _isMainTargetLocked;

    public event Action<Vector2> AimChanged;
    public event Action<string> AnimationTriggered;
    protected override State InitialState => GetState<SkeletonAriseState>();
    protected override State DeadState => GetState<SkeletonDeathState>();

    protected override void Awake()
    {
        base.Awake();
        _body = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        RegisterState(new SkeletonAriseState(this));
        RegisterState(new SkeletonChaseState(this));
        RegisterState(new SkeletonAttackState(this));
        RegisterState(new SkeletonDeathState(this));
    }

    private void OnEnable()
    {
        Stats.AttackSpeed.ValueChanged += UpdateAttackSpeed;
        UpdateAttackSpeed(Stats.AttackSpeed.Value);
    }

    protected override void OnDisable()
    {
        Stats.AttackSpeed.ValueChanged -= UpdateAttackSpeed;
        _isMainTargetLocked = false;
        _isShooting = false;
        base.OnDisable();
        StopChase();
    }

    protected override void Update()
    {
        if (!_isMainTargetLocked && TemporaryTarget != null && !IsInRange(TemporaryTarget))
            ClearTemporaryTarget();
        if (CurrentState != null && !Health.IsDead && Time.time >= _nextTargetSearch)
        {
            _nextTargetSearch = Time.time + 0.2f;
            FindTarget();
        }
        base.Update();
    }

    private bool IsInRange(Transform target)
    {
        Vector2 offset = target.position - transform.position;
        return offset.sqrMagnitude <= Stats.ViewDistance.Value * Stats.ViewDistance.Value;
    }

    private void FindTarget()
    {
        if (_isMainTargetLocked && MainTarget != null)
            return;
        _isMainTargetLocked = false;
        Transform best = null;
        int priority = int.MinValue;
        float distance = float.PositiveInfinity;
        foreach (Collider2D candidate in Physics2D.OverlapCircleAll(transform.position, Stats.ViewDistance.Value, _targetMask))
        {
            PlayerInputReader player = candidate.GetComponentInParent<PlayerInputReader>();
            if (player == null || !IsInRange(player.transform))
                continue;
            ActorHealth health = player.GetComponent<ActorHealth>();
            if (health == null || health.IsDead)
                continue;
            ActorStats stats = player.GetComponent<ActorStats>();
            int candidatePriority = stats != null ? stats.TargetPriority : 0;
            float candidateDistance = ((Vector2)(player.transform.position - transform.position)).sqrMagnitude;
            if (candidatePriority > priority || (candidatePriority == priority && candidateDistance < distance))
            {
                best = player.transform;
                priority = candidatePriority;
                distance = candidateDistance;
            }
        }
        SetTemporaryTarget(best);
    }

    public void SetAim(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.000001f)
            return;
        _aimDirection = direction;
        float side = direction.x < 0f ? -1f : 1f;
        _facing.localScale = new Vector3(side, 1f, 1f);
        float angle = Mathf.Atan2(direction.y, direction.x * side) * Mathf.Rad2Deg;
        _weaponAim.localRotation = Quaternion.Euler(0f, 0f, angle);
        AimChanged?.Invoke(direction);
    }

    public override void Arise()
    {
        StopChase();
        _animator.Play("Arise", 0, 0f);
    }

    public void FinishArise()
    {
        if (isActiveAndEnabled && CurrentState is SkeletonAriseState)
            ChangeState<SkeletonChaseState>();
    }

    public override void Chase(Transform target)
    {
        SetAim(target.position - _weaponAim.position);
        _body.linearVelocity = ((Vector2)(target.position - transform.position)).normalized * Stats.MoveSpeed.Value;
        _animator.SetBool("IsMoving", true);
    }

    public override void StopChase()
    {
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
        if (CurrentTarget == MainTarget)
            _isMainTargetLocked = true;
        SetAim(CurrentTarget.position - _weaponAim.position);
        _shotDirection = _aimDirection.normalized;
        _isShooting = true;
        TriggerAnimation("Shoot");
    }

    public void ReleaseArrow()
    {
        if (!isActiveAndEnabled || !_isShooting || Health.IsDead)
            return;
        _isShooting = false;
        SkeletonArrow arrow = Instantiate(_arrowPrefab, _muzzle.position, Quaternion.identity);
        ProjectileData data = new ProjectileData
        {
            Direction = _shotDirection,
            Speed = _arrowSpeed,
            Damage = Stats.PhysicalDamage.Value,
            LifeTime = _arrowLifetime,
            LayerMask = _arrowHitMask,
            DamageType = EDamageType.Physical,
            Attacker = Stats
        };
        arrow.Launch(data);
        arrow.GetComponent<NetworkObject>().Spawn();
    }

    public void FinishAttack()
    {
        if (isActiveAndEnabled && CurrentState is SkeletonAttackState)
            ChangeState<SkeletonChaseState>();
    }

    public override void CancelAttack()
    {
        _isShooting = false;
        _animator.ResetTrigger("Shoot");
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
        _animator.SetFloat("AttackSpeed", speed);
    }
}
