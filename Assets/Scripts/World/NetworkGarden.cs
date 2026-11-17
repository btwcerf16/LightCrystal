using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(NetworkObject))]
public sealed class NetworkGarden : NetworkBehaviour, IWaveEvent
{
    [SerializeField] private WorldItem _worldItemPrefab;
    [SerializeField] private CropItem[] _crops;
    [SerializeField, Min(0.1f)] private float _lifetime = 60f;
    private readonly List<WorldItem> _unpickedCrops = new();
    private bool _wasHarvested;
    private float _capacity;
    private bool _isWaveEvent;
    private double _expiresAt;

    public event Action<bool> Finished;

    public void ConfigureSpawn(float capacity, bool isWaveEvent)
    {
        _capacity = capacity;
        _isWaveEvent = isWaveEvent;
    }

    protected override void OnNetworkPostSpawn()
    {
        if (!IsServer)
            return;

        int cropCount = _capacity < 50f ? 1 : _capacity < 80f ? 2 : 3;
        List<CropItem> availableCrops = new(_crops);
        for (int i = 0; i < cropCount; i++)
        {
            int cropIndex = Random.Range(0, availableCrops.Count);
            CropItem crop = availableCrops[cropIndex];
            availableCrops.RemoveAt(cropIndex);
            Vector3 position = transform.position + Vector3.right * (i - (cropCount - 1) * 0.5f);
            WorldItem worldItem = Instantiate(_worldItemPrefab, position, Quaternion.identity);
            worldItem.SetItem(crop.gameObject);
            worldItem.PickedUp += OnCropPicked;
            _unpickedCrops.Add(worldItem);
            worldItem.NetworkObject.Spawn(destroyWithScene: true);
        }

        if (!_isWaveEvent)
            _expiresAt = NetworkManager.ServerTime.Time + _lifetime;
        PlayerHUD.NotifyPlayers(NetworkManager, "Появилась грядка", new Color32(160, 211, 117, 255));
    }

    private void Update()
    {
        if (IsSpawned && IsServer && _expiresAt > 0 && NetworkManager.ServerTime.Time >= _expiresAt)
            Close();
    }

    private void OnCropPicked(WorldItem item)
    {
        _wasHarvested = true;
        _unpickedCrops.Remove(item);
        item.PickedUp -= OnCropPicked;
    }

    public void Close()
    {
        if (!IsServer || !IsSpawned || NetworkManager.ShutdownInProgress)
            return;
        Finished?.Invoke(_wasHarvested);
        NetworkObject.Despawn();
    }

    public override void OnNetworkDespawn()
    {
        Finished = null;
        _expiresAt = 0;
        foreach (WorldItem item in _unpickedCrops)
        {
            if (item == null)
                continue;
            item.PickedUp -= OnCropPicked;
            if (IsServer && !NetworkManager.ShutdownInProgress && item.IsSpawned)
                item.NetworkObject.Despawn();
        }
        _unpickedCrops.Clear();
        _wasHarvested = false;
    }
}
