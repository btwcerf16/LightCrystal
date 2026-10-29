public sealed class SkeletonAriseState : State
{
    private readonly Skeleton _skeleton;
    public SkeletonAriseState(Skeleton skeleton) => _skeleton = skeleton;
    public override void Enter() => _skeleton.Arise();
}
