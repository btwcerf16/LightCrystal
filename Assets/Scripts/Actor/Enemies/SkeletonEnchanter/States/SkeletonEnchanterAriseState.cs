public sealed class SkeletonEnchanterAriseState : State
{
    private readonly SkeletonEnchanter _enemy;

    public SkeletonEnchanterAriseState(SkeletonEnchanter enemy) => _enemy = enemy;

    public override void Enter() => _enemy.Arise();
}