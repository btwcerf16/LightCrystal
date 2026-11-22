using System;
using UnityEngine;

[RequireComponent(typeof(SpellHolder))]
public sealed class SkeletonEnchanter : Enemy
{
    private SpellHolder _spells;
    private Transform _threat;
    private float _nextTargetSearch;
    private readonly RaycastHit2D[] _wallHits = new RaycastHit2D[1];

    internal Transform Threat => _threat;
    internal bool IsCasting => _spells.Spells[0].CastProgress >= 0f;
    internal void StartFlee() => _animator.Play("Walk", 0, 0f);

    public event Action<bool> FacingChanged;
    public event Action DiedVisual;

    protected override State InitialState => GetState<SkeletonEnchanterAriseState>();
    protected override State ChaseState => GetState<SkeletonEnchanterApproachState>();
    protected override State DeadState => GetState<SkeletonEnchanterDeathState>();

    protected override void Awake()
    {
        base.Awake();
        _spells = GetComponent<SpellHolder>();
        RegisterState(new SkeletonEnchanterAriseState(this));
        RegisterState(new SkeletonEnchanterApproachState(this));
        RegisterState(new SkeletonEnchanterCastingState(this));
        RegisterState(new SkeletonEnchanterFleeState(this));
        RegisterState(new SkeletonEnchanterDeathState(this));
    }

    protected override void Update()
    {
        if (CurrentState != null && !Health.IsDead && Time.time >= _nextTargetSearch)
        {
            _nextTargetSearch = Time.time + 0.2f;
            if (TemporaryTarget == null)
                SetTemporaryTarget(FindPlayerTarget());
        }

        base.Update();
    }

    protected override void OnDisable()
    {
        _threat = null;
        _nextTargetSearch = 0f;
        base.OnDisable();
    }

    private void FixedUpdate() => _body.linearVelocity = _moveDirection * Stats.MoveSpeed.Value;

    public override void Arise()
    {
        StopChase();
        _animator.Play("Arise", 0, 0f);
    }

    public void FinishArise()
    {
        if (isActiveAndEnabled && CurrentState is SkeletonEnchanterAriseState)
            ChangeState<SkeletonEnchanterApproachState>();
    }

    public void UpdateThreat(Transform player)
    {
        _threat = player;
        if (player != null && CurrentState != null && !Health.IsDead)
        {
            SetTemporaryTarget(player);
            ChangeState<SkeletonEnchanterFleeState>();
        }
    }

    public override void Chase(Transform target) => MoveToward(target.position);

    private void MoveToward(Vector2 target)
    {
        _moveDirection = GetChaseDirection(target);
        FaceDirection(_moveDirection.x);
        _animator.SetBool("IsMoving", _moveDirection.sqrMagnitude > 0f);
    }

    internal void Retreat()
    {
        Vector2 position = transform.position;
        Vector2 away = (position - (Vector2)_threat.position).normalized;
        if (away == Vector2.zero)
            away = _facing.localScale.x < 0f ? Vector2.left : Vector2.right;

        var walls = new ContactFilter2D { useTriggers = false };
        walls.SetLayerMask(LayerMask.GetMask("Default"));
        Collider2D body = GetComponent<Collider2D>();
        if (body.Cast(away, walls, _wallHits, 0.25f) > 0)
        {
            Vector2 sideways = Vector2.Perpendicular(_wallHits[0].normal);
            if (body.Cast(sideways, walls, _wallHits, 0.25f) > 0)
                sideways = -sideways;
            away = sideways;
        }

        MoveToward(position + away);
    }

    internal bool InCastingRange(Transform target)
    {
        Vector2 offset = target.position - transform.position;
        return offset.sqrMagnitude <= AttackDistance * AttackDistance;
    }

    public override bool CanAttack() => CurrentTarget != null && InCastingRange(CurrentTarget)
        && base.CanAttack() && CurrentTarget.TryGetComponent<PlayerInputReader>(out _);

    public override void Attack()
    {
        if (!CanAttack())
            return;

        Spell spell = _spells.Spells[0];
        if (spell.AvailableCharges == 0 || (Effects != null && Effects.IsSilenced))
            return;

        Transform target = CurrentTarget;
        NotifyAttackStarted();
        if (Health.IsDead || CurrentState is SkeletonEnchanterFleeState)
            return;

        _spells.CastSpell(0, new SpellCastData { Target = target.gameObject });
        if (spell.CastProgress >= 0f)
            ChangeState<SkeletonEnchanterCastingState>();
    }

    public override void CancelAttack()
    {
        if (_spells == null || _spells.Spells.Count == 0 || _spells.Spells[0].CastProgress < 0f)
            return;

        _spells.CancelCast();
        _animator.ResetTrigger("Cast");
        _animator.Play("Idle", 0, 0f);
    }

    public override void Death()
    {
        StopChase();
        GetComponent<Collider2D>().enabled = false;
        DiedVisual?.Invoke();
    }

    public void SetFacing(bool facesLeft) => _facing.localScale = new Vector3(facesLeft ? -1f : 1f, 1f, 1f);

    internal void FaceDirection(float x)
    {
        if (Mathf.Abs(x) < 0.001f || (x < 0f) == (_facing.localScale.x < 0f))
            return;

        SetFacing(x < 0f);
        FacingChanged?.Invoke(x < 0f);
    }
}
