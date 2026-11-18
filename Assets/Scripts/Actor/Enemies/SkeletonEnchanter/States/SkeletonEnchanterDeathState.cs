public sealed class SkeletonEnchanterDeathState : State
{
    private readonly SkeletonEnchanter _enemy;

    public SkeletonEnchanterDeathState(SkeletonEnchanter enemy) => _enemy = enemy;

    public override void Enter() => _enemy.Death();
}