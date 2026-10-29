using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(NetworkTransform))]
public sealed class NetworkProjectile : NetworkBehaviour
{
    private Projectile _projectile;

    private void Awake()
    {
        _projectile = GetComponent<Projectile>();
    }

    public override void OnNetworkSpawn()
    {
        _projectile.enabled = IsServer;
        if (IsServer)
            _projectile.Finished += Despawn;
    }

    public override void OnNetworkDespawn()
    {
        _projectile.Finished -= Despawn;
        _projectile.enabled = false;
    }

    private void Despawn()
    {
        if (IsSpawned && IsServer)
            NetworkObject.Despawn();
    }
}
