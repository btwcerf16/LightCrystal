public sealed class SkeletonEnchanterFleeState : State
{
    private readonly SkeletonEnchanter _enemy;

    public SkeletonEnchanterFleeState(SkeletonEnchanter enemy) => _enemy = enemy;

    public override void Enter() => _enemy.StartFlee();

    public override void Update()
    {
        if (_enemy.Threat == null)
            _enemy.ChangeState<SkeletonEnchanterApproachState>();
        else
            _enemy.Retreat();
    }

    public override void Exit() => _enemy.StopChase();
}