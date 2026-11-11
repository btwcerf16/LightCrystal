using UnityEngine;

public sealed class FireMageChaseState : State
{
    private readonly FireMage _mage;

    public FireMageChaseState(FireMage mage) => _mage = mage;

    public override void Update()
    {
        Transform target = _mage.CurrentTarget;
        if (target == null)
        {
            _mage.StopChase();
            return;
        }

        if (!_mage.CanAttack())
        {
            _mage.Chase(target);
            return;
        }

        _mage.StopChase();
        _mage.Attack();
    }

    public override void Exit() => _mage.StopChase();
}
