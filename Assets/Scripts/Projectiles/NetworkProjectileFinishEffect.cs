using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Projectile))]
public sealed class NetworkProjectileFinishEffect : NetworkBehaviour
{
    [SerializeField] private MagicOrbExplosion _effectPrefab;
    [SerializeField, Min(0f)] private float _finishRadius;

    private static readonly Dictionary<MagicOrbExplosion, ObjectPool<MagicOrbExplosion>> Pools = new();
    private Projectile _projectile;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPools()
    {
        foreach (ObjectPool<MagicOrbExplosion> pool in Pools.Values)
            pool.Clear();
        Pools.Clear();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer || _finishRadius <= 0f)
            return;

        _projectile = GetComponent<Projectile>();
        _projectile.Finishing += PlayOnFinish;
    }

    public override void OnNetworkDespawn()
    {
        if (_projectile != null)
            _projectile.Finishing -= PlayOnFinish;
        _projectile = null;
    }

    private void PlayOnFinish() => Play(_finishRadius);

    internal void Play(float radius)
    {
        if (IsSpawned && IsServer)
            PlayRpc(transform.position, radius);
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void PlayRpc(Vector3 position, float radius)
    {
        if (_effectPrefab == null)
            return;

        if (!Pools.TryGetValue(_effectPrefab, out ObjectPool<MagicOrbExplosion> pool))
        {
            MagicOrbExplosion prefab = _effectPrefab;
            pool = new ObjectPool<MagicOrbExplosion>(() => Instantiate(prefab), 32);
            Pools.Add(prefab, pool);
        }
        pool.GetObject().Play(pool, position, radius);
    }
}
