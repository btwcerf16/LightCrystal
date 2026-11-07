using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkManager), typeof(WaveDirector))]
public sealed class NetworkWaveDirector : MonoBehaviour
{
    [SerializeField] private NetworkShopSummonZone _shopZonePrefab;
    [SerializeField, Min(0f)] private float _zoneMinimumCrystalDistance = 30f;
    private NetworkShopSummonZone _shopZone;
    private NetworkManager _networkManager;
    private WaveDirector _director;

    private void OnEnable()
    {
        _networkManager = GetComponent<NetworkManager>();
        _director = GetComponent<WaveDirector>();
        _director.enabled = false;
        _networkManager.OnServerStarted += StartCycle;
        _networkManager.OnServerStopped += StopCycle;
        _director.EnemyActivated += SpawnEnemy;
        _director.CalmStarted += SpawnShopZone;
        _director.WaveStarted += OpenShop;
    }

    private void Start()
    {
        if (_networkManager.IsServer && _networkManager.IsListening)
            StartCycle();
    }

    public void StartCycle()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Arena01") return;
        _director.enabled = true;
        _director.Initialize();
    }

    [ConsoleCommand]
    public string PauseWaves(bool paused = true)
    {
        if (!_networkManager.IsServer)
            throw new System.InvalidOperationException("Паузой волн управляет хост.");
        _director.SetPaused(paused);
        return paused ? "Цикл волн приостановлен." : "Цикл волн продолжен.";
    }

    private void SpawnEnemy(Enemy enemy)
    {
        if (_networkManager.IsServer)
            enemy.GetComponent<NetworkObject>().Spawn();
    }

    private void SpawnShopZone()
    {
        Crystal crystal = FindFirstObjectByType<Crystal>();
        ShopSpawnArea area = FindFirstObjectByType<ShopSpawnArea>();
        if (!_networkManager.IsServer || _shopZonePrefab == null || crystal == null || area == null)
            return;

        if (area.TryGetPosition(crystal.transform.position, _zoneMinimumCrystalDistance, out Vector3 position))
        {
            _shopZone = Instantiate(_shopZonePrefab, position, Quaternion.identity);
            _shopZone.SetWaitForWave(true);
            _shopZone.NetworkObject.Spawn();
            return;
        }
        Debug.LogError("No shop zone position found inside the map.", this);
    }

    private void OpenShop()
    {
        if (_shopZone != null && _shopZone.IsSpawned)
            _shopZone.OpenShop();
        _shopZone = null;
    }

    private void StopCycle(bool wasHost)
    {
        _director.enabled = false;
        _director.Stop();
        _shopZone = null;
    }

    private void OnDisable()
    {
        _networkManager.OnServerStarted -= StartCycle;
        _networkManager.OnServerStopped -= StopCycle;
        _director.EnemyActivated -= SpawnEnemy;
        _director.CalmStarted -= SpawnShopZone;
        _director.WaveStarted -= OpenShop;
        _director.enabled = false;
    }
}
