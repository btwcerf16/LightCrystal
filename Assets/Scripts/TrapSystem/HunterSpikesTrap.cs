using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class HunterSpikesTrap : Trap
{
    [SerializeField] private EffectData _rootEffect;
    [SerializeField] private EffectData _projectileSpeedEffect;
    private readonly HashSet<ulong> _caughtEnemyIds = new();

    public void Initialize(ActorStats owner)
    {
        _caughtEnemyIds.Clear();
        base.Initialize(new TrapData
        {
            Owner = owner, TargetsLayerMask = _targetsLayerMask, EffectOnCaught = _rootEffect, LifeTime = 8f
        });
    }

    protected override void OnCaught(Collider2D targetCollider)
    {
        ActorStats caughtEnemy = targetCollider.GetComponentInParent<ActorStats>();
        if (!caughtEnemy.TryGetComponent<Enemy>(out _) || !caughtEnemy.TryGetComponent(out NetworkObject enemyObject)
            || !_caughtEnemyIds.Add(enemyObject.NetworkObjectId))
            return;

        base.OnCaught(targetCollider);
        if (_owner != null && _owner.TryGetComponent(out NetworkObject ownerObject) && ownerObject.IsSpawned)
            _owner.GetComponent<EffectHandler>()?.AddEffect(_projectileSpeedEffect);
    }
}
