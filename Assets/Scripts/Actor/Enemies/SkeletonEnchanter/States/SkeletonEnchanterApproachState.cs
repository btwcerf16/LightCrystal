using UnityEngine;

public sealed class SkeletonEnchanterApproachState : State
{
    private readonly SkeletonEnchanter _enemy;

    public SkeletonEnchanterApproachState(SkeletonEnchanter enemy) => _enemy = enemy;

    public override void Update()
    {
        Transform target = _enemy.CurrentTarget;
        if (target == null)
        {
            _enemy.StopChase();
            return;
        }
        if (!_enemy.InCastingRange(target))
        {
            _enemy.Chase(target);
            return;
        }

        _enemy.StopChase();
        _enemy.FaceDirection(target.position.x - _enemy.transform.position.x);
        _enemy.Attack();
    }

    public override void Exit() => _enemy.StopChase();
}