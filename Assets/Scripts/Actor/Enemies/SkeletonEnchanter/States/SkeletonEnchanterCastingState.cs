public sealed class SkeletonEnchanterCastingState : State
{
    private readonly SkeletonEnchanter _enemy;

    public SkeletonEnchanterCastingState(SkeletonEnchanter enemy) => _enemy = enemy;

    public override void Update()
    {
        if (!_enemy.IsCasting)
            _enemy.ChangeState<SkeletonEnchanterApproachState>();
    }

    public override void Exit() => _enemy.CancelAttack();
}