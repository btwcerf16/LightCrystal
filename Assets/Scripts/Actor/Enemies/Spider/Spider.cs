using System;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(SpellHolder))]
public sealed class Spider : Enemy
{
    [SerializeField] private Transform _facing;
    [SerializeField] private Collider2D _bodyCollider;
    [SerializeField] private LayerMask _targetMask = ~0;
    [SerializeField] private float _preferredDistance = 4f;
    [SerializeField] private float _biteRadius = 0.45f;
    private Rigidbody2D _body;
    private Animator _animator;
    private SpellHolder _spells;
    private SpiderWebSpellConfig _webConfig;
    private Vector2 _moveDirection;
    private float _nextTargetSearch;
    private bool _isAttacking;
    private bool _isMainTargetLocked;
    private readonly HashSet<IDamageable> _hitTargets = new();
    public event Action<Vector2> AimChanged;
    public event Action<string> AnimationTriggered;
    protected override State ChaseState => GetState<SpiderChaseState>();
    protected override State InitialState => GetState<SpiderAriseState>();
    protected override State DeadState => GetState<SpiderDeathState>();
    protected override void Awake()
    {
        base.Awake();
        _body = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _spells = GetComponent<SpellHolder>();
        _webConfig = (SpiderWebSpellConfig)_spells.GetConfig(0);
        RegisterState(new SpiderAriseState(this));
        RegisterState(new SpiderChaseState(this));
        RegisterState(new SpiderAttackState(this));
        RegisterState(new SpiderCastState(this));
        RegisterState(new SpiderDeathState(this));
    }
    private void OnEnable()
    {
        Stats.AttackSpeed.ValueChanged += UpdateAttackSpeed;
        UpdateAttackSpeed(Stats.AttackSpeed.Value);
        _nextTargetSearch = 0f;
    }
    protected override void OnDisable()
    {
        Stats.AttackSpeed.ValueChanged -= UpdateAttackSpeed;
        _isMainTargetLocked = false;
        _isAttacking = false;
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
    private void FixedUpdate() => _body.linearVelocity = _moveDirection * Stats.MoveSpeed.Value;
    private void FindTarget()
    {
        if (TemporaryTarget != null || (_isMainTargetLocked && MainTarget != null))
            return;
        Transform bestTarget = null;
        int bestPriority = int.MinValue;
        float bestDistance = float.PositiveInfinity;
        foreach (Collider2D candidate in Physics2D.OverlapCircleAll(transform.position, Stats.ViewDistance.Value, _targetMask))
        {
            PlayerInputReader player = candidate.GetComponentInParent<PlayerInputReader>();
            if (player == null || player.GetComponent<ActorHealth>().IsDead)
                continue;
            int priority = player.GetComponent<ActorStats>().TargetPriority;
            float distance = ((Vector2)(player.transform.position - transform.position)).sqrMagnitude;
            if (priority > bestPriority || (priority == bestPriority && distance < bestDistance))
            {
                bestTarget = player.transform;
                bestPriority = priority;
                bestDistance = distance;
            }
        }
        SetTemporaryTarget(bestTarget);
    }
    public void SetAim(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.000001f)
            return;
        _facing.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f);
        AimChanged?.Invoke(direction);
    }
    public bool ShouldPursue(Transform target)
    {
        if (target.GetComponent<Crystal>() != null)
            return true;
        ActorHealth health = target.GetComponent<ActorHealth>();
        EffectHandler effects = target.GetComponent<EffectHandler>();
        return health != null && health.CurrentHealth < health.MaxHealth * 0.5f && effects != null && effects.HasEffect(_webConfig.WebEffect);
    }
    public bool ShouldChase(Transform target)
    {
        return ShouldPursue(target) || ((Vector2)(target.position - transform.position)).sqrMagnitude > _preferredDistance * _preferredDistance;
    }

    public bool CanCastWeb(Transform target)
    {
        if (IsStunned || (Effects != null && (Effects.IsSilenced || Effects.IsDowned)) || _spells.Spells.Count == 0)
            return false;
        Spell spell = _spells.Spells[0];
        return spell.IsActive && spell.isActiveAndEnabled && spell.CooldownRemaining <= 0f && spell.CanCast(new SpellCastData { Target = target.gameObject });
    }

    public void FinishCast()
    {
        if (isActiveAndEnabled && CurrentState is SpiderCastState)
            ChangeState<SpiderChaseState>();
    }

    public override void Chase(Transform target)
    {
        Vector2 offset = target.position - transform.position;
        SetAim(offset);
        if (ShouldChase(target))
            _moveDirection = GetChaseDirection(target.position);
        else
            _moveDirection = Vector2.zero;
        _animator.SetBool("IsMoving", _moveDirection.sqrMagnitude > 0f);
    }
    public void CastWeb(Transform target) => _spells.CastSpell(0, new SpellCastData { Target = target.gameObject });
    public override void StopChase()
    {
        _moveDirection = Vector2.zero;
        if (_body != null) _body.linearVelocity = Vector2.zero;
        if (_animator != null) _animator.SetBool("IsMoving", false);
    }
    public override bool CanAttack() => base.CanAttack() && Stats.AttackSpeed.Value > 0f;
    public override void Arise() { StopChase(); _animator.Play("Arise", 0, 0); }
    public void FinishArise() { if (isActiveAndEnabled && CurrentState is SpiderAriseState) ChangeState<SpiderChaseState>(); }
    public override void Attack()
    {
        NotifyAttackStarted();
        if (Health.IsDead) return;
        if (CurrentTarget == MainTarget) _isMainTargetLocked = true;
        SetAim(CurrentTarget.position - transform.position);
        StopChase();
        _isAttacking = true;
        _hitTargets.Clear();
        TriggerAnimation("Attack");
    }
    public void Bite()
    {
        if (!isActiveAndEnabled || !_isAttacking || Health.IsDead || IsStunned)
            return;
        foreach (Collider2D target in Physics2D.OverlapCircleAll((Vector2)transform.position - (Vector2)_facing.up * 0.5f, _biteRadius, _targetMask))
        {
            if (target.GetComponentInParent<Enemy>() != null) continue;
            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable == null || !_hitTargets.Add(damageable)) continue;
            if (DamageCalculator.DealCriticalDamage(damageable, Stats.PhysicalDamage.Value, EDamageType.Physical, Stats) > 0 && target.GetComponentInParent<PlayerInputReader>() != null)
                target.GetComponentInParent<EffectHandler>().AddEffect(_webConfig.WebEffect);
        }
    }
    public void FinishAttack() { if (isActiveAndEnabled && CurrentState is SpiderAttackState) ChangeState<SpiderChaseState>(); }
    public override void CancelAttack() { _isAttacking = false; _animator.ResetTrigger("Attack"); _animator.ResetTrigger("Cast"); _spells.CancelCast(); }
    public override void Death() { StopChase(); CancelAttack(); _bodyCollider.enabled = false; TriggerAnimation("Death"); }
    private void TriggerAnimation(string trigger) { _animator.SetTrigger(trigger); AnimationTriggered?.Invoke(trigger); }
    private void UpdateAttackSpeed(float speed) => _animator.SetFloat("AttackSpeed", speed);
}
