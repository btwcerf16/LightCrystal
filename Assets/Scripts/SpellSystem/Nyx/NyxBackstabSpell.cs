using UnityEngine;

public sealed class NyxBackstabSpell : PassiveSpell
{
    private ActorHealth _health;
    private Effect _activeBuff;

    protected override void Apply()
    {
        _health = _owner.GetComponent<ActorHealth>();
        _health.EnemyKilled += OnEnemyKilled;
    }

    protected override void Remove()
    {
        _health.EnemyKilled -= OnEnemyKilled;
        if (_activeBuff != null)
            _ownerEffects.RemoveEffect(_activeBuff);
        _activeBuff = null;
    }

    private void OnEnemyKilled(ActorHealth health)
    {
        if (!health.TryGetComponent(out Enemy enemy))
            return;

        Transform target = enemy.CurrentTarget;
        if (target != null && !target.IsChildOf(_owner.transform))
            _activeBuff = _ownerEffects.AddEffect(((NyxBackstabConfig)SpellData).StackEffect);
    }
}
