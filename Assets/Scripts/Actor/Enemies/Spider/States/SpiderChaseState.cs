public sealed class SpiderChaseState : State
{
    private readonly Spider _spider;
    public SpiderChaseState(Spider spider) => _spider = spider;

    public override void Update()
    {
        if (_spider.CurrentTarget == null)
        {
            _spider.StopChase();
            return;
        }
        if (_spider.CanAttack())
        {
            _spider.ChangeState<SpiderAttackState>();
            return;
        }
        if (_spider.ShouldChase(_spider.CurrentTarget))
        {
            _spider.Chase(_spider.CurrentTarget);
            return;
        }
        _spider.StopChase();
        _spider.SetAim(_spider.CurrentTarget.position - _spider.transform.position);
        if (_spider.CanCastWeb(_spider.CurrentTarget))
            _spider.ChangeState<SpiderCastState>();
    }

    public override void Exit() => _spider.StopChase();
}
