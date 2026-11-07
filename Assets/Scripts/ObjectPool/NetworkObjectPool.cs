using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
[DefaultExecutionOrder(-50)]
public sealed class NetworkObjectPool : MonoBehaviour
{
    private NetworkManager _manager;
    private readonly Dictionary<uint, NetworkPoolHandler> _handlers = new();

    private void Awake()
    {
        _manager = GetComponent<NetworkManager>();
        foreach (NetworkPrefabsList list in _manager.NetworkConfig.Prefabs.NetworkPrefabsLists)
            if (list != null)
                foreach (NetworkPrefab entry in list.PrefabList)
                    Register(entry.Prefab);
        foreach (NetworkPrefab entry in _manager.NetworkConfig.Prefabs.Prefabs)
            Register(entry.Prefab);
    }

    private void Register(GameObject prefab)
    {
        if (prefab == null || (!prefab.TryGetComponent<Enemy>(out _) && !prefab.TryGetComponent<Projectile>(out _)))
            return;
        uint hash = prefab.GetComponent<NetworkObject>().PrefabIdHash;
        if (_handlers.ContainsKey(hash))
            return;
        NetworkPoolHandler handler = new NetworkPoolHandler(prefab);
        if (!_manager.PrefabHandler.AddHandler(prefab, handler))
            throw new InvalidOperationException("Network prefab already has a handler: " + prefab.name);
        _handlers.Add(hash, handler);
    }

    public T GetObject<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
    {
        Register(prefab.gameObject);
        uint hash = prefab.GetComponent<NetworkObject>().PrefabIdHash;
        return _handlers[hash].Instantiate(0, position, rotation).GetComponent<T>();
    }

    public void ReturnObject(NetworkObject item)
    {
        if (item.IsSpawned)
        {
            if (_manager.IsServer)
                item.Despawn();
            return;
        }
        if (_handlers.TryGetValue(item.PrefabIdHash, out NetworkPoolHandler handler))
            handler.Destroy(item);
        else
            Destroy(item.gameObject);
    }

    private void OnDestroy()
    {
        foreach (var pair in _handlers)
        {
            if (_manager != null)
                _manager.PrefabHandler.RemoveHandler(pair.Key);
            pair.Value.Clear();
        }
        _handlers.Clear();
    }
}
