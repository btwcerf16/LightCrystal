using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(SummonZone))]
public sealed class NetworkSummonZone : NetworkBehaviour
{
    [SerializeField] private WaveEventDatabase _eventDatabase;
    [SerializeField, Min(0f)] private float _minimumCrystalDistance = 30f;
    private readonly NetworkVariable<float> _capacity = new();
    private readonly NetworkVariable<double> _startedAt = new();
    private SummonZone _zone;
    private double _lastTick;
    private bool _isWaveEvent;
    private NetworkObject _eventPrefab;
    private Vector3 _eventPosition;
    private System.Action<NetworkObject> _configureEvent;

    public float Capacity => _capacity.Value;
    public event System.Action<NetworkObject> EventSummoned;

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
        if (_zone.IsComplete)
            CreateEvent();
    }

    private void ProcessZone(double now)
    {
        _zone.Process((float)(now - _lastTick), HasPlayer(), _isWaveEvent);
        _lastTick = now;
        _capacity.Value = _zone.Capacity;
    }

    public void SetWaveEvent() => _isWaveEvent = true;

    public void SetEvent(NetworkObject prefab, Vector3 position, System.Action<NetworkObject> configure = null)
    {
        _eventPrefab = prefab;
        _eventPosition = position;
        _configureEvent = configure;
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
        NetworkObject prefab = _eventPrefab != null ? _eventPrefab : _eventDatabase?.GetRandomPrefab(_isWaveEvent);
        if (prefab == null || !prefab.TryGetComponent<IWaveEvent>(out _))
        {
            Debug.LogError("В базе зоны призыва нужен префаб события с положительным весом.", this);
            NetworkObject.Despawn();
            return;
        }

        Vector3 position = _eventPosition;
        if (_eventPrefab == null)
        {
            Crystal crystal = FindFirstObjectByType<Crystal>();
            ShopSpawnArea area = FindFirstObjectByType<ShopSpawnArea>();
            if (crystal == null || area == null
                || !area.TryGetPosition(crystal.transform.position, _minimumCrystalDistance, out position))
            {
                Debug.LogError("Не удалось создать событие на карте вдали от кристалла.", this);
                NetworkObject.Despawn();
                return;
            }
        }

        NetworkObject summonedEvent = Instantiate(prefab, position, Quaternion.identity);
        summonedEvent.GetComponent<IWaveEvent>().ConfigureSpawn(Capacity, _isWaveEvent);
        _configureEvent?.Invoke(summonedEvent);
        summonedEvent.Spawn(destroyWithScene: true);
        EventSummoned?.Invoke(summonedEvent);
        NetworkObject.Despawn();
    }
}
