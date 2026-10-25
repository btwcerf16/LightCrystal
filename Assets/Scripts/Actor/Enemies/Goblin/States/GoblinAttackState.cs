public sealed class GoblinAttackState : State
{
    private readonly Goblin _enemy;

    public GoblinAttackState(Goblin enemy)
    {
        _enemy = enemy;
    }

    public override void Enter()
    {
        _enemy.Attack();
    }

    public override void Update()
    {
        if (_enemy.CurrentTarget == null)
            _enemy.ChangeState<GoblinChaseState>();
    }

    public override void Exit()
    {
        _enemy.CancelAttack();
    }
}
