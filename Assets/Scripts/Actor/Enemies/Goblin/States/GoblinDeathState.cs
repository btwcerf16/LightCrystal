public sealed class GoblinDeathState : State
{
    private readonly Goblin _enemy;

    public GoblinDeathState(Goblin enemy)
    {
        _enemy = enemy;
    }

    public override void Enter()
    {
        _enemy.Death();
    }
}
