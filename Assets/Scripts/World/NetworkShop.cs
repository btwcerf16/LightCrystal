using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(Shop))]
public class NetworkShop : NetworkBehaviour
{
    private Shop _shop;
    private float _capacity;
    [SerializeField, Min(0.1f)] private float _lifetime = 60f;
    private double _expiresAt;

    [SerializeField] private List<WorldItem> _worldItems = new();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            _shop = GetComponent<Shop>();
    }

    public void Initialize()
    {
        if (!IsServer || !IsSpawned)
            return;
        _shop.Initialize(_capacity);
        SpawnItems();
    }

    public void Initialize(EShopCategory category)
    {
        if (!IsServer || !IsSpawned)
            return;
        _shop.Initialize(category);
        SpawnItems();
    }

    private void Update()
    {
        if (IsSpawned && IsServer && _expiresAt > 0 && NetworkManager.ServerTime.Time >= _expiresAt)
            NetworkObject.Despawn();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer || NetworkManager.ShutdownInProgress)
            return;
        foreach (WorldItem item in _worldItems)
        {
            if (item != null && item.IsSpawned && item.IsForSale)
                item.NetworkObject.Despawn();
        }
    }

    private void SpawnItems()
    {
        _worldItems = _shop.GetItems();
        foreach (WorldItem worldItem in _worldItems)
            worldItem.NetworkObject.Spawn();
        _expiresAt = NetworkManager.ServerTime.Time + _lifetime;
    }

    public void SetCapacity(float capacity)
    {
        _capacity = capacity;
    }
}
