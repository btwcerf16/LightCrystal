using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(Shop))]
public class NetworkShop : NetworkBehaviour, IWaveEvent
{
    private Shop _shop;
    private float _capacity;
    [SerializeField, Min(0.1f)] private float _lifetime = 60f;
    private double _expiresAt;
    private bool _wasUsed;
    public event System.Action<bool> Finished;

    [SerializeField] private List<WorldItem> _worldItems = new();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _shop = GetComponent<Shop>();
            _wasUsed = false;
            PlayerHUD.NotifyPlayers(NetworkManager, "Появился торговец", new Color32(245, 205, 106, 255));
        }
    }

    public void Initialize(bool closeOnNextWave = false)
    {
        if (!IsServer || !IsSpawned)
            return;
        _shop.Initialize(_capacity);
        SpawnItems();
        if (!closeOnNextWave)
            _expiresAt = NetworkManager.ServerTime.Time + _lifetime;
    }

    public void Initialize(EShopCategory category)
    {
        if (!IsServer || !IsSpawned)
            return;
        _shop.Initialize(category);
        SpawnItems();
        _expiresAt = NetworkManager.ServerTime.Time + _lifetime;
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer || NetworkManager.ShutdownInProgress || _expiresAt <= 0
            || NetworkManager.ServerTime.Time < _expiresAt)
            return;

        Close();
    }

    public void Close()
    {
        if (!IsSpawned || !IsServer || NetworkManager.ShutdownInProgress)
            return;

        _expiresAt = 0;
        if (_worldItems.Count > 0)
            Finished?.Invoke(_wasUsed);
        NetworkObject.Despawn();
    }

    public override void OnNetworkDespawn()
    {
        Finished = null;
        _expiresAt = 0;
        if (!IsServer)
            return;
        foreach (WorldItem item in _worldItems)
        {
            if (item == null)
                continue;
            item.Purchased -= MarkUsed;
            if (!NetworkManager.ShutdownInProgress && item.IsSpawned && item.IsForSale)
                item.NetworkObject.Despawn();
        }
    }

    private void SpawnItems()
    {
        _worldItems = _shop.GetItems();
        foreach (WorldItem worldItem in _worldItems)
        {
            worldItem.Purchased += MarkUsed;
            worldItem.NetworkObject.Spawn();
        }
    }

    private void MarkUsed() => _wasUsed = true;

    public void SetCapacity(float capacity)
    {
        _capacity = capacity;
    }
}
