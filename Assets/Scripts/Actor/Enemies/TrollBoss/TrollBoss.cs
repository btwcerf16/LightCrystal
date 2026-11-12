using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class TrollBoss : Enemy, INamed
{
    [SerializeField] private string _name = "Тролль";
    [SerializeField] private Transform _landingZone;
    [SerializeField] private EffectData _landingStun;
    [SerializeField, Min(0.1f)] private float _jumpRange = 9f;
    [SerializeField, Min(0.1f)] private float _jumpCooldown = 8f;
    [SerializeField, Min(0.1f)] private float _hammerCooldown = 3f;
    [SerializeField, Min(0.1f)] private float _landingRadius = 3f;
    [SerializeField, Min(0.1f)] private float _hammerRadius = 2f;
    [SerializeField, Min(0f)] private float _knockbackSpeed = 11f;

    private readonly HashSet<ActorHealth> _hitActors = new();
    private BoxCollider2D _hitbox;
    private Transform _hammerZone;
    private NetworkTransform _networkTransform;
    private Coroutine _attackRoutine;
    private float _nextJumpTime;
    private float _nextHammerTime;
    private float _nextTargetSearchTime;

    public string Name => _name;
    public event Action<string> AnimationTriggered;
    public event Action<bool> FacingChanged;
    public event Action<Vector2, Vector2> DustRaised;

    protected override State InitialState => ChaseState;
    protected override State ChaseState => GetState<TrollBossChaseState>();
    protected override State DeadState => GetState<TrollBossDeathState>();

    protected override void Awake()
    {
        base.Awake();
        _hitbox = GetComponent<BoxCollider2D>();
        _networkTransform = GetComponent<NetworkTransform>();
        RegisterState(new TrollBossChaseState(this));
        RegisterState(new TrollBossDeathState(this));

        _landingZone.localScale = new Vector3(_landingRadius * 2f, _landingRadius * 2f, 1f);
        _hammerZone = _facing.Find("HammerZone");
        if (_hammerZone != null)
            _hammerZone.localScale = new Vector3(_hammerRadius * 2f, _hammerRadius * 2f, 1f);
    }

    protected override void OnDisable()
    {
        CancelAttack();
        base.OnDisable();
        StopChase();
    }

    private void FixedUpdate()
    {
        _body.linearVelocity = _moveDirection * Stats.MoveSpeed.Value;
    }

    public override void Arise()
    {
        _nextJumpTime = Time.time + 2f;
        _nextHammerTime = 0f;
        _nextTargetSearchTime = 0f;
    }

    public void UpdateCombat()
    {
        if (_attackRoutine != null)
            return;

        if (TemporaryTarget == null && Time.time >= _nextTargetSearchTime)
        {
            _nextTargetSearchTime = Time.time + 0.2f;
            SetTemporaryTarget(FindPlayerTarget());
        }

        Transform target = CurrentTarget;
        if (target == null)
        {
            StopChase();
            return;
        }

        if (CanAttack() || CanJump(target))
        {
            StopChase();
            Attack();
            return;
        }

        Chase(target);
    }

    public override void Chase(Transform target)
    {
        FaceTarget(target);
        _moveDirection = GetChaseDirection(target.position);
        _animator.SetBool("IsMoving", _moveDirection.sqrMagnitude > 0f);
    }

    public override void Attack()
    {
        Transform target = CurrentTarget;
        if (_attackRoutine != null || target == null)
            return;

        bool useJump = CanJump(target);
        if (!useJump && Time.time < _nextHammerTime)
            return;

        NotifyAttackStarted();
        if (Health.IsDead || IsStunned)
            return;

        FaceTarget(target);
        if (useJump)
        {
            _nextJumpTime = Time.time + _jumpCooldown;
            _attackRoutine = StartCoroutine(Jump(target));
        }
        else
        {
            _nextHammerTime = Time.time + _hammerCooldown;
            _attackRoutine = StartCoroutine(SlamHammer());
        }
    }

    private bool CanJump(Transform target)
    {
        Vector2 offset = target.position - transform.position;
        return Time.time >= _nextJumpTime && offset.sqrMagnitude <= _jumpRange * _jumpRange;
    }

    private IEnumerator Jump(Transform target)
    {
        Vector3 landingPoint = target.position;
        _hitbox.enabled = false;
        AnimationTriggered?.Invoke("Leap");
        DustRaised?.Invoke(transform.position, new Vector2(1.1f, 2f));
        yield return new WaitForSeconds(1.35f);

        if (target != null)
            landingPoint = target.position;
        _networkTransform.Teleport(landingPoint, transform.rotation, transform.localScale);
        AnimationTriggered?.Invoke("WarnLanding");
        yield return new WaitForSeconds(0.7f);

        AnimationTriggered?.Invoke("Land");
        yield return new WaitForSeconds(0.2f);

        _hitbox.enabled = true;
        HitArea(landingPoint, _landingRadius, _landingStun);
        yield return new WaitForSeconds(0.3f);
        _attackRoutine = null;
    }

    private IEnumerator SlamHammer()
    {
        AnimationTriggered?.Invoke("Hammer");
        yield return new WaitForSeconds(0.65f);

        Vector2 center = _hammerZone != null ? _hammerZone.position : _facing.TransformPoint(Vector3.right * 1.1f);
        HitArea(center, _hammerRadius, null);
        yield return new WaitForSeconds(0.4f);
        _attackRoutine = null;
    }

    private void HitArea(Vector2 center, float radius, EffectData stun)
    {
        DustRaised?.Invoke(center, Vector2.one * radius);
        _hitActors.Clear();
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(center, radius, _targetMask))
        {
            ActorHealth target = collider.GetComponentInParent<ActorHealth>();
            if (target == null || target.IsDead ||
                (!target.TryGetComponent<PlayerInputReader>(out _) && !target.TryGetComponent<Crystal>(out _)) ||
                !_hitActors.Add(target))
                continue;

            DamageCalculator.DealCriticalDamage(target, Stats.PhysicalDamage.Value, EDamageType.Physical, Stats);
            if (target.IsDead || !target.TryGetComponent(out PlayableActorMovement movement))
                continue;

            Vector2 direction = (Vector2)target.transform.position - center;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.right * Mathf.Sign(_facing.localScale.x);

            float resistance = target.GetComponent<ActorStats>().ForceResistance.Value;
            movement.ApplyKnockback(direction, _knockbackSpeed * (1f - resistance), 0.35f);
            if (stun != null)
                target.GetComponent<EffectHandler>().AddEffect(stun);
        }
    }

    public void SetFacing(bool facesLeft)
    {
        _facing.localScale = new Vector3(facesLeft ? -1f : 1f, 1f, 1f);
    }

    private void FaceTarget(Transform target)
    {
        float offset = target.position.x - transform.position.x;
        if (Mathf.Abs(offset) < 0.001f || (offset < 0f) == (_facing.localScale.x < 0f))
            return;

        SetFacing(offset < 0f);
        FacingChanged?.Invoke(offset < 0f);
    }

    public override void CancelAttack()
    {
        if (_attackRoutine == null)
            return;

        StopCoroutine(_attackRoutine);
        _attackRoutine = null;
        _hitbox.enabled = true;
        if (!Health.IsDead)
            AnimationTriggered?.Invoke("Cancel");
    }

    public override void Death()
    {
        CancelAttack();
        StopChase();
        _hitbox.enabled = false;
        AnimationTriggered?.Invoke("Death");
    }
}
