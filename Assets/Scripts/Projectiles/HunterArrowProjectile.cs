using Unity.Netcode;
using UnityEngine;

public sealed class HunterArrowProjectile : Projectile
{
    [SerializeField] private HunterSpikesTrap _spikesPrefab;

    public override void OnHit(Collider2D targetCollider)
    {
        ActorHealth targetHealth = targetCollider.GetComponentInParent<ActorHealth>();
        bool canLeaveSpikes = targetHealth != null && !targetHealth.IsDead && targetHealth.TryGetComponent<Enemy>(out _);
        Vector3 targetPosition = canLeaveSpikes ? targetHealth.transform.position : Vector3.zero;
        ActorStats attacker = Data.Attacker;
        NetworkManager manager = GetComponent<NetworkObject>().NetworkManager;

        base.OnHit(targetCollider);

        if (!canLeaveSpikes || !targetHealth.IsDead || attacker == null || Random.value >= attacker.CriticalChance.Value)
            return;

        NetworkObjectPool pool = manager.GetComponent<NetworkObjectPool>();
        HunterSpikesTrap spikes = pool.GetObject(_spikesPrefab, targetPosition, Quaternion.identity);
        spikes.Initialize(attacker);
        spikes.GetComponent<NetworkObject>().Spawn();
    }
}
