public sealed class SkeletonChaseState : State
{
    private readonly Skeleton _skeleton;
    public SkeletonChaseState(Skeleton skeleton) => _skeleton = skeleton;
    public override void Update()
    {
        if (_skeleton.CurrentTarget == null)
            _skeleton.StopChase();
        else if (_skeleton.CanAttack())
            _skeleton.ChangeState<SkeletonAttackState>();
        else
            _skeleton.Chase(_skeleton.CurrentTarget);
    }
    public override void Exit() => _skeleton.StopChase();
}
