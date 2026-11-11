using System;
using UnityEngine;

[RequireComponent(typeof(SpellHolder))]
public sealed class FireMage : Enemy
{
    private SpellHolder _spells;
    private float _nextTargetSearchTime;
    private bool _isMainTargetLocked;

    public event Action<string> AnimationTriggered;
    public event Action<bool> FacingChanged;

    protected override State InitialState => GetState<FireMageAriseState>();
    protected override State ChaseState => GetState<FireMageChaseState>();
    protected override State DeadState => GetState<FireMageDeathState>();

    protected override void Awake()
    {
        base.Awake();
        _spells = GetComponent<SpellHolder>();
        RegisterState(new FireMageAriseState(this));
        RegisterState(new FireMageChaseState(this));
        RegisterState(new FireMageDeathState(this));
    }

    protected override void OnDisable()
    {
        _isMainTargetLocked = false;
        _nextTargetSearchTime = 0f;
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

    public override void Arise()
    {
        StopChase();
        _animator.Play("Arise", 0, 0f);
    }

    public void FinishArise()
    {
        if (isActiveAndEnabled && CurrentState is FireMageAriseState)
            ChangeState<FireMageChaseState>();
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
        FaceTarget(target);
        Spell pillar = _spells.Spells[0];
        if (pillar.CooldownRemaining > 0f || (Effects != null && Effects.IsSilenced))
            return;

        NotifyAttackStarted();
        if (Health.IsDead)
            return;

        _isMainTargetLocked = target == MainTarget;
        _spells.CastSpell(0, new SpellCastData { Target = target.gameObject });
    }

    public override void Death()
    {
        StopChase();
        GetComponent<Collider2D>().enabled = false;
        AnimationTriggered?.Invoke("Death");
    }

    public void SetFacing(bool facesLeft)
    {
        _facing.localScale = new Vector3(facesLeft ? -1f : 1f, 1f, 1f);
    }

    private void FaceTarget(Transform target)
    {
        bool facesLeft = target.position.x < transform.position.x;
        if (facesLeft == (_facing.localScale.x < 0f))
            return;

        SetFacing(facesLeft);
        FacingChanged?.Invoke(facesLeft);
    }
}
