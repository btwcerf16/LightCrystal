using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(ShopSummonZone))]
public sealed class NetworkShopSummonZone : NetworkBehaviour
{
    [SerializeField] private NetworkShop _shopPrefab;
    [SerializeField, Min(0f)] private float _minimumCrystalDistance = 30f;
    private readonly NetworkVariable<float> _capacity = new();
    private readonly NetworkVariable<double> _startedAt = new();
    private ShopSummonZone _zone;
    private double _lastTick;
    private bool _waitForWave;

    public float Capacity => _capacity.Value;

    private void Awake()
    {
        _zone = GetComponent<ShopSummonZone>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _startedAt.Value = NetworkManager.ServerTime.Time;
            _lastTick = _startedAt.Value;
        }
    }

    private void Update()
    {
        if (!IsSpawned)
            return;
        double now = NetworkManager.ServerTime.Time;
        _zone.SetVisuals(Capacity, (float)((now - _startedAt.Value) / 1.6 % 1.0));
        if (!IsServer || now - _lastTick < 0.1)
            return;

        ProcessZone(now);
        if (_zone.IsComplete && !_waitForWave)
            CreateShop();
    }

    private void ProcessZone(double now)
    {
        _zone.Process((float)(now - _lastTick), HasPlayer());
        _lastTick = now;
        _capacity.Value = _zone.Capacity;
    }

    public void SetWaitForWave(bool waitForWave)
    {
        _waitForWave = waitForWave;
    }

    public void OpenShop()
    {
        if (!IsServer || !IsSpawned)
            return;
        ProcessZone(NetworkManager.ServerTime.Time);
        CreateShop();
    }

    private bool HasPlayer()
    {
        foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
        {
            NetworkObject player = client.PlayerObject;
            if (player == null)
                continue;
            ActorHealth health = player.GetComponent<ActorHealth>();
            if (health != null && !health.IsDead
                && ((Vector2)player.transform.position - (Vector2)transform.position).sqrMagnitude <= _zone.Radius * _zone.Radius)
                return true;
        }
        return false;
    }

    private void CreateShop()
    {
        Crystal crystal = FindFirstObjectByType<Crystal>();
        ShopSpawnArea area = FindFirstObjectByType<ShopSpawnArea>();
        if (_shopPrefab == null || crystal == null || area == null
            || !area.TryGetPosition(crystal.transform.position, _minimumCrystalDistance, out Vector3 position))
        {
            Debug.LogError("Не удалось найти место для магазина на карте вдали от кристалла.", this);
            NetworkObject.Despawn();
            return;
        }

        NetworkShop shop = Instantiate(_shopPrefab, position, Quaternion.identity);
        shop.SetCapacity(Capacity);
        shop.NetworkObject.Spawn();
        shop.Initialize();
        NetworkObject.Despawn();
    }
}
