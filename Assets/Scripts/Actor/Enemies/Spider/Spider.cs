using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpellHolder))]
public sealed class Spider : Enemy
{
    [SerializeField] private Collider2D _bodyCollider;
    [SerializeField] private float _preferredDistance = 4f;
    [SerializeField] private float _biteRadius = 0.45f;

    private readonly HashSet<IDamageable> _hitTargets = new();
    private SpellHolder _spellHolder;
    private SpiderWebSpellConfig _webConfig;
    private float _nextTargetSearchTime;
    private bool _isMainTargetLocked;

    public event Action<Vector2> AimChanged;
    public event Action<string> AnimationTriggered;

    protected override State InitialState => GetState<SpiderAriseState>();
    protected override State ChaseState => GetState<SpiderChaseState>();
    protected override State DeadState => GetState<SpiderDeathState>();

    protected override void Awake()
    {
        base.Awake();
        _spellHolder = GetComponent<SpellHolder>();
        _webConfig = (SpiderWebSpellConfig)_spellHolder.GetConfig(0);

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
        _nextTargetSearchTime = 0f;
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
        if (CurrentState != null && !Health.IsDead && Time.time >= _nextTargetSearchTime)
        {
            _nextTargetSearchTime = Time.time + 0.2f;
            if (TemporaryTarget == null && (!_isMainTargetLocked || MainTarget == null))
                SetTemporaryTarget(FindPlayerTarget());
        }

        base.Update();
    }

    private void FixedUpdate()
    {
        _body.linearVelocity = _moveDirection * Stats.MoveSpeed.Value;
    }

    public bool ShouldChase(Transform target)
    {
        Vector2 offset = target.position - transform.position;
        if (offset.sqrMagnitude > _preferredDistance * _preferredDistance || target.GetComponent<Crystal>() != null)
            return true;

        ActorHealth targetHealth = target.GetComponent<ActorHealth>();
        if (targetHealth == null || targetHealth.CurrentHealth >= targetHealth.MaxHealth * 0.5f)
            return false;

        EffectHandler targetEffects = target.GetComponent<EffectHandler>();
        return targetEffects != null && targetEffects.HasEffect(_webConfig.WebEffect);
    }

    public override void Chase(Transform target)
    {
        SetAim(target.position - transform.position);
        _moveDirection = GetChaseDirection(target.position);
        _animator.SetBool("IsMoving", _moveDirection.sqrMagnitude > 0f);
    }

    public void SetAim(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.000001f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
        _facing.rotation = Quaternion.Euler(0, 0, angle);
        AimChanged?.Invoke(direction);
    }

    public bool CanCastWeb(Transform target)
    {
        if (_spellHolder.Spells.Count == 0 || IsStunned)
            return false;
        if (Effects != null && (Effects.IsSilenced || Effects.IsDowned))
            return false;

        Spell webSpell = _spellHolder.Spells[0];
        if (!webSpell.IsActive || !webSpell.isActiveAndEnabled || webSpell.CooldownRemaining > 0f)
            return false;

        return webSpell.CanCast(new SpellCastData { Target = target.gameObject });
    }

    public void CastWeb(Transform target)
    {
        _spellHolder.CastSpell(0, new SpellCastData { Target = target.gameObject });
    }

    public void FinishCast()
    {
        if (isActiveAndEnabled && CurrentState is SpiderCastState)
            ChangeState<SpiderChaseState>();
    }

    public override bool CanAttack() => Stats.AttackSpeed.Value > 0f && base.CanAttack();

    public override void Attack()
    {
        NotifyAttackStarted();
        if (Health.IsDead)
            return;

        Transform target = CurrentTarget;
        if (target == MainTarget)
            _isMainTargetLocked = true;

        SetAim(target.position - transform.position);
        StopChase();
        _isAttacking = true;
        _hitTargets.Clear();
        TriggerAnimation("Attack");
    }

    public void Bite()
    {
        if (!isActiveAndEnabled || !_isAttacking || Health.IsDead || IsStunned)
            return;

        Vector2 biteCenter = (Vector2)transform.position - (Vector2)_facing.up * 0.5f;
        foreach (Collider2D target in Physics2D.OverlapCircleAll(biteCenter, _biteRadius, _targetMask))
        {
            if (target.GetComponentInParent<Enemy>() != null)
                continue;

            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable == null || !_hitTargets.Add(damageable))
                continue;

            float damageDealt = DamageCalculator.DealCriticalDamage(damageable, Stats.PhysicalDamage.Value, EDamageType.Physical, Stats);
            if (damageDealt > 0f && target.GetComponentInParent<PlayerInputReader>() != null)
                target.GetComponentInParent<EffectHandler>().AddEffect(_webConfig.WebEffect);
        }
    }

    public void FinishAttack()
    {
        if (isActiveAndEnabled && CurrentState is SpiderAttackState)
            ChangeState<SpiderChaseState>();
    }

    public override void CancelAttack()
    {
        _isAttacking = false;
        _animator.ResetTrigger("Attack");
        _animator.ResetTrigger("Cast");
        _spellHolder.CancelCast();
    }

    public override void Arise()
    {
        StopChase();
        _animator.Play("Arise", 0, 0);
    }

    public void FinishArise()
    {
        if (isActiveAndEnabled && CurrentState is SpiderAriseState)
            ChangeState<SpiderChaseState>();
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

    private void UpdateAttackSpeed(float speed) => _animator.SetFloat("AttackSpeed", speed);
}
