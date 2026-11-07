using Unity.Netcode;
using UnityEngine;

internal sealed class NetworkPoolHandler : INetworkPrefabInstanceHandler
{
    private readonly ObjectPool<NetworkObject> _pool;

    public NetworkPoolHandler(GameObject prefab)
    {
        _pool = new ObjectPool<NetworkObject>(() => UnityEngine.Object.Instantiate(prefab).GetComponent<NetworkObject>());
    }

    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        NetworkObject item = _pool.GetObject();
        item.transform.SetPositionAndRotation(position, rotation);
        if (item.TryGetComponent(out Enemy enemy))
            enemy.ResetForSpawn();
        return item;
    }

    public void Destroy(NetworkObject item) => _pool.ReturnObject(item);
    public void Clear() => _pool.Clear();
}
