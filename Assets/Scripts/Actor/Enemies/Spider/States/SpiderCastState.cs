public sealed class SpiderCastState : State
{
    private readonly Spider _spider;

    public SpiderCastState(Spider spider) => _spider = spider;

    public override void Enter() => _spider.CastWeb(_spider.CurrentTarget);

    public override void Update()
    {
        if (_spider.CurrentTarget == null)
            _spider.ChangeState<SpiderChaseState>();
    }

    public override void Exit() => _spider.CancelAttack();
}
