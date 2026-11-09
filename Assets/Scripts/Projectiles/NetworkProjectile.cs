using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(NetworkTransform))]
public sealed class NetworkProjectile : NetworkBehaviour
{
    [SerializeField] private MagicOrbExplosion _orbExplosionPrefab;
    [SerializeField, Min(0f)] private float _finishExplosionRadius;
    private static readonly Dictionary<MagicOrbExplosion, ObjectPool<MagicOrbExplosion>> _explosionPools = new();
    private Projectile _projectile;
    private MagicOrbProjectile _orb;
    private TrailRenderer _trail;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEffects()
    {
        foreach (ObjectPool<MagicOrbExplosion> pool in _explosionPools.Values)
            pool.Clear();
        _explosionPools.Clear();
    }

    private void Awake()
    {
        _projectile = GetComponent<Projectile>();
        _orb = _projectile as MagicOrbProjectile;
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
        {
            _projectile.Finished += Despawn;
            if (_orb != null) _orb.Exploded += PlayOrbExplosion;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (_trail != null)
        {
            _trail.emitting = false;
            _trail.Clear();
        }
        _projectile.Finished -= Despawn;
        if (_orb != null) _orb.Exploded -= PlayOrbExplosion;
        _projectile.enabled = false;
    }

    private void Despawn()
    {
        if (!IsSpawned || !IsServer)
            return;
        if (_finishExplosionRadius > 0f)
            OrbExplosionRpc(transform.position, _finishExplosionRadius);
        NetworkManager.GetComponent<NetworkObjectPool>().ReturnObject(NetworkObject);
    }

    private void PlayOrbExplosion(Vector3 position, float radius) => OrbExplosionRpc(position, radius);

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void OrbExplosionRpc(Vector3 position, float radius)
    {
        if (_orbExplosionPrefab == null)
            return;
        if (!_explosionPools.TryGetValue(_orbExplosionPrefab, out ObjectPool<MagicOrbExplosion> pool))
        {
            MagicOrbExplosion prefab = _orbExplosionPrefab;
            pool = new ObjectPool<MagicOrbExplosion>(() => Instantiate(prefab), 32);
            _explosionPools.Add(prefab, pool);
        }
        pool.GetObject().Play(pool, position, radius);
    }
}
