public sealed class SkeletonAttackState : State
{
    private readonly Skeleton _skeleton;
    public SkeletonAttackState(Skeleton skeleton) => _skeleton = skeleton;
    public override void Enter() => _skeleton.Attack();
    public override void Update()
    {
        if (_skeleton.CurrentTarget == null)
            _skeleton.ChangeState<SkeletonChaseState>();
    }
    public override void Exit() => _skeleton.CancelAttack();
}
