public sealed class SkeletonDeathState : State
{
    private readonly Skeleton _skeleton;
    public SkeletonDeathState(Skeleton skeleton) => _skeleton = skeleton;
    public override void Enter() => _skeleton.Death();
}
