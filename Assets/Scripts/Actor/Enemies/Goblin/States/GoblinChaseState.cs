using UnityEngine;

public sealed class GoblinChaseState : State
{
    private readonly Goblin _enemy;

    public GoblinChaseState(Goblin enemy)
    {
        _enemy = enemy;
    }

    public override void Update()
    {
        Transform target = _enemy.CurrentTarget;
        if (target == null)
        {
            _enemy.StopChase();
            return;
        }
        if (_enemy.CanAttack())
            _enemy.ChangeState<GoblinAttackState>();
        else
            _enemy.Chase(target);
    }

    public override void Exit()
    {
        _enemy.StopChase();
    }
}
