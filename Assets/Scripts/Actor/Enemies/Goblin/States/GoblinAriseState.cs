public sealed class GoblinAriseState : State
{
    private readonly Goblin _enemy;

    public GoblinAriseState(Goblin enemy)
    {
        _enemy = enemy;
    }

    public override void Enter()
    {
        _enemy.Arise();
    }
}
