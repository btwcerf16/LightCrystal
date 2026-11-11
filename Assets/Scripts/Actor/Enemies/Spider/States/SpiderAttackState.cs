public sealed class SpiderAttackState : State
{
    private readonly Spider _spider;

    public SpiderAttackState(Spider spider) => _spider = spider;

    public override void Enter() => _spider.Attack();

    public override void Update()
    {
        if (_spider.CurrentTarget == null)
            _spider.ChangeState<SpiderChaseState>();
    }

    public override void Exit() => _spider.CancelAttack();
}
