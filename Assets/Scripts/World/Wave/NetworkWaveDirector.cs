using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkManager), typeof(WaveDirector))]
public sealed class NetworkWaveDirector : MonoBehaviour
{
    [SerializeField] private NetworkSummonZone _summonZonePrefab;
    [SerializeField] private BossFightData _bossFightEffect;
    [SerializeField, Min(0f)] private float _zoneMinimumCrystalDistance = 30f;
    private NetworkSummonZone _summonZone;
    private NetworkObject _currentWaveEvent;
    private NetworkObject _nextWaveEvent;
    private NetworkObject _revealedWaveEvent;
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
        _director.CalmEnded += SummonPendingEvent;
        _director.WaveStarted += OnWaveStarted;
        _director.CycleStopped += ClearWaveEvents;
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
        _director.Initialize(_networkManager.ConnectedClientsIds.Count);
        if (_networkManager.IsServer && LocalPlayerSettings.RecordRunStatistics && _director.CurrentState != null)
        {
            LocalPlayerSettings.RecordRunStatistics = false;
            GameObject recorder = new GameObject("Run Statistics");
            recorder.transform.SetParent(transform, false);
            recorder.AddComponent<RunStatisticsRecorder>().Begin(_networkManager, _director, GetComponent<GameDefeatController>());
        }
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
        ClearBossFightEffect();
        Crystal crystal = FindFirstObjectByType<Crystal>();
        ShopSpawnArea area = FindFirstObjectByType<ShopSpawnArea>();
        if (!_networkManager.IsServer || _summonZonePrefab == null || crystal == null || area == null)
            return;

        if (area.TryGetPosition(crystal.transform.position, _zoneMinimumCrystalDistance, out Vector3 position))
        {
            _summonZone = Instantiate(_summonZonePrefab, position, Quaternion.identity);
            _summonZone.SetWaveEvent();
            _summonZone.EventSummoned += OnEventSummoned;
            _summonZone.NetworkObject.Spawn();
            return;
        }
        Debug.LogError("No summon zone position found inside the map", this);
    }

    private void SummonPendingEvent()
    {
        if (_summonZone != null && _summonZone.IsSpawned)
            _summonZone.SummonEvent();
    }

    private void OnWaveStarted()
    {
        if (_currentWaveEvent != null && _currentWaveEvent.TryGetComponent(out IWaveEvent waveEvent))
            waveEvent.Close();
        _currentWaveEvent = _nextWaveEvent;
        _nextWaveEvent = null;
        if (_currentWaveEvent != null)
            TryRevealEvent(_currentWaveEvent);

        if (_director.IsBossWave)
            foreach (NetworkClient client in _networkManager.ConnectedClientsList)
                if (client.PlayerObject != null)
                    ApplyBossFightEffect(client.PlayerObject.gameObject);
    }

    private void OnEventSummoned(NetworkObject spawnedEvent)
    {
        bool fullyCharged = _summonZone.Capacity >= 100f;
        _summonZone.EventSummoned -= OnEventSummoned;
        _summonZone = null;
        if (!spawnedEvent.TryGetComponent(out IWaveEvent waveEvent))
            return;

        _nextWaveEvent = spawnedEvent;
        void OnFinished(bool wasUsed)
        {
            waveEvent.Finished -= OnFinished;
            _director.RecordEventResult(wasUsed);
        }

        waveEvent.Finished += OnFinished;
        if (fullyCharged)
            _revealedWaveEvent = spawnedEvent;
        else
            TryRevealEvent(spawnedEvent);
    }

    private void TryRevealEvent(NetworkObject spawnedEvent)
    {
        if (spawnedEvent == _revealedWaveEvent || !_director.TryConsumeEventReveal())
            return;

        _revealedWaveEvent = spawnedEvent;
        PlayerHUD.RevealWaveEvent(spawnedEvent);
    }

    private void ClearWaveEvents()
    {
        if (_summonZone != null)
        {
            _summonZone.EventSummoned -= OnEventSummoned;
            if (_summonZone.IsSpawned && _networkManager.IsServer && !_networkManager.ShutdownInProgress)
                _summonZone.NetworkObject.Despawn();
        }
        _summonZone = null;
        _currentWaveEvent = null;
        _nextWaveEvent = null;
        _revealedWaveEvent = null;
    }

    public void ApplyBossFightEffect(GameObject player)
    {
        if (_networkManager.IsServer && _director.IsBossWave && _bossFightEffect != null)
            player.GetComponent<EffectHandler>().AddEffect(_bossFightEffect);
    }

    private void ClearBossFightEffect()
    {
        if (!_networkManager.IsServer || _bossFightEffect == null)
            return;

        foreach (NetworkClient client in _networkManager.ConnectedClientsList)
        {
            if (client.PlayerObject == null || !client.PlayerObject.TryGetComponent(out EffectHandler effects))
                continue;

            for (int i = effects.ActiveEffects.Count - 1; i >= 0; i--)
                if (effects.ActiveEffects[i].EffectData == _bossFightEffect)
                    effects.RemoveEffect(effects.ActiveEffects[i]);
        }
    }

    private void StopCycle(bool wasHost)
    {
        ClearBossFightEffect();
        _director.enabled = false;
        _director.Stop();
    }

    private void OnDisable()
    {
        ClearBossFightEffect();
        _networkManager.OnServerStarted -= StartCycle;
        _networkManager.OnServerStopped -= StopCycle;
        _director.EnemyActivated -= SpawnEnemy;
        _director.CalmStarted -= SpawnSummonZone;
        _director.CalmEnded -= SummonPendingEvent;
        _director.WaveStarted -= OnWaveStarted;
        _director.CycleStopped -= ClearWaveEvents;
        ClearWaveEvents();
        _director.enabled = false;
    }
}
