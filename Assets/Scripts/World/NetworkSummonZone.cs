using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(SummonZone))]
public sealed class NetworkSummonZone : NetworkBehaviour
{
    [SerializeField] private NetworkShop _shopPrefab;
    [SerializeField] private NetworkEnchanter _enchanterPrefab;
    [SerializeField, Range(0f, 1f)] private float _enchanterChance = 0.3f;
    [SerializeField, Min(0f)] private float _minimumCrystalDistance = 30f;
    private readonly NetworkVariable<float> _capacity = new();
    private readonly NetworkVariable<double> _startedAt = new();
    private SummonZone _zone;
    private double _lastTick;
    private bool _waitForWave;

    public float Capacity => _capacity.Value;

    private void Awake()
    {
        _zone = GetComponent<SummonZone>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _startedAt.Value = NetworkManager.ServerTime.Time;
            _lastTick = _startedAt.Value;
            PlayerHUD.NotifyPlayers(NetworkManager, "Появилась зона призыва", new Color32(113, 210, 230, 255));
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
            CreateEvent();
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

    public void SummonEvent()
    {
        if (!IsServer || !IsSpawned)
            return;
        ProcessZone(NetworkManager.ServerTime.Time);
        CreateEvent();
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

    private void CreateEvent()
    {
        Crystal crystal = FindFirstObjectByType<Crystal>();
        ShopSpawnArea area = FindFirstObjectByType<ShopSpawnArea>();
        if (_shopPrefab == null || _enchanterPrefab == null || crystal == null || area == null
            || !area.TryGetPosition(crystal.transform.position, _minimumCrystalDistance, out Vector3 position))
        {
            Debug.LogError("Не удалось создать событие на карте вдали от кристалла.", this);
            NetworkObject.Despawn();
            return;
        }

        if (Random.value < _enchanterChance)
        {
            NetworkEnchanter enchanter = Instantiate(_enchanterPrefab, position, Quaternion.identity);
            enchanter.SetCapacity(Capacity);
            enchanter.NetworkObject.Spawn();
        }
        else
        {
            NetworkShop shop = Instantiate(_shopPrefab, position, Quaternion.identity);
            shop.SetCapacity(Capacity);
            shop.NetworkObject.Spawn();
            shop.Initialize();
        }
        NetworkObject.Despawn();
    }
}
