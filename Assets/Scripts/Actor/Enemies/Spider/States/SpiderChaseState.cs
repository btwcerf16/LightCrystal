using UnityEngine;

public sealed class SpiderChaseState : State
{
    private readonly Spider _spider;

    public SpiderChaseState(Spider spider) => _spider = spider;

    public override void Update()
    {
        Transform target = _spider.CurrentTarget;
        if (target == null)
        {
            _spider.StopChase();
            return;
        }

        if (_spider.CanAttack())
        {
            _spider.ChangeState<SpiderAttackState>();
            return;
        }

        if (_spider.ShouldChase(target))
        {
            _spider.Chase(target);
            return;
        }

        _spider.StopChase();
        _spider.SetAim(target.position - _spider.transform.position);
        if (_spider.CanCastWeb(target))
            _spider.ChangeState<SpiderCastState>();
    }

    public override void Exit() => _spider.StopChase();
}
