using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkManager), typeof(WaveDirector))]
public sealed class NetworkWaveDirector : MonoBehaviour
{
    [SerializeField] private NetworkSummonZone _summonZonePrefab;
    [SerializeField, Min(0f)] private float _zoneMinimumCrystalDistance = 30f;
    private NetworkSummonZone _summonZone;
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
        _director.CalmStarted += SpawnSummonZone;
        _director.WaveStarted += SummonEvent;
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

    private void SpawnSummonZone()
    {
        Crystal crystal = FindFirstObjectByType<Crystal>();
        ShopSpawnArea area = FindFirstObjectByType<ShopSpawnArea>();
        if (!_networkManager.IsServer || _summonZonePrefab == null || crystal == null || area == null)
            return;

        if (area.TryGetPosition(crystal.transform.position, _zoneMinimumCrystalDistance, out Vector3 position))
        {
            _summonZone = Instantiate(_summonZonePrefab, position, Quaternion.identity);
            _summonZone.SetWaitForWave(true);
            _summonZone.NetworkObject.Spawn();
            return;
        }
        Debug.LogError("No summon zone position found inside the map", this);
    }

    private void SummonEvent()
    {
        if (_summonZone != null && _summonZone.IsSpawned)
            _summonZone.SummonEvent();
        _summonZone = null;
    }

    private void StopCycle(bool wasHost)
    {
        _director.enabled = false;
        _director.Stop();
        _summonZone = null;
    }

    private void OnDisable()
    {
        _networkManager.OnServerStarted -= StartCycle;
        _networkManager.OnServerStopped -= StopCycle;
        _director.EnemyActivated -= SpawnEnemy;
        _director.CalmStarted -= SpawnSummonZone;
        _director.WaveStarted -= SummonEvent;
        _director.enabled = false;
    }
}
