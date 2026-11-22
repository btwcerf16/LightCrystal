using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(NetworkTransform))]
public sealed class NetworkProjectile : NetworkBehaviour
{
    private Projectile _projectile;
    private TrailRenderer _trail;

    private void Awake()
    {
        _projectile = GetComponent<Projectile>();
        _trail = GetComponent<TrailRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        if (_trail != null)
        {
            _trail.Clear();
            _trail.emitting = true;
        }
        _projectile.enabled = IsServer;
        if (IsServer)
            _projectile.Finished += Despawn;
    }

    public override void OnNetworkDespawn()
    {
        if (_trail != null)
        {
            _trail.emitting = false;
            _trail.Clear();
        }
        _projectile.Finished -= Despawn;
        _projectile.enabled = false;
    }

    private void Despawn()
    {
        if (!IsSpawned || !IsServer)
            return;
        NetworkManager.GetComponent<NetworkObjectPool>().ReturnObject(NetworkObject);
    }
}
