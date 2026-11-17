using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class RunStatisticsRecorder : MonoBehaviour
{
    [Serializable]
    private sealed class SessionStart
    {
        public string type = "start";
        public int schemaVersion = 1;
        public string startedAtUtc;
        public string buildVersion;
        public string buildGuid;
        public string arena;
        public int playerCount;
    }

    [Serializable]
    private sealed class SessionEnd
    {
        public string type = "end";
        public string reason;
        public int completedWaves;
        public double elapsedSeconds;
    }

    [Serializable]
    private sealed class PlayerPhase
    {
        public string type = "playerPhase";
        public string character;
        public int player;
        public int playerCount;
        public int wave;
        public string phase;
        public bool completed;
        public double elapsedSeconds;
        public int kills;
        public int downs;
        public int revives;
        public float damageDealt;
        public float damageTaken;
        public float physicalDamageTaken;
        public float magicDamageTaken;
        public float minHealthFraction;
        public float minSanityFraction;
        public float sanitySpent;
        public float sanityLostInDarkness;
        public float sanityLostOther;

        private readonly ActorHealth _health;
        private readonly ActorSanity _sanity;

        public PlayerPhase(NetworkObject playerObject, int playerNumber)
        {
            player = playerNumber;
            character = playerObject.GetComponent<NetworkSpellHolder>()?.Character?.DisplayName ?? "";
            _health = playerObject.GetComponent<ActorHealth>();
            _sanity = playerObject.GetComponent<ActorSanity>();
            _health.DamageReceived += RecordDamageTaken;
            _health.DamageDealt += RecordDamageDealt;
            _health.EnemyKilled += RecordKill;
            _health.Died += RecordDown;
            _health.Revived += RecordRevive;
            _health.HealthChanged += RecordHealth;
            _sanity.SanityChanged += RecordSanity;
            _sanity.SanitySpent += RecordSanitySpent;
            _sanity.SanityLost += RecordSanityLoss;
        }

        public void Reset(string nextPhase, int nextWave, int teamSize)
        {
            phase = nextPhase;
            wave = nextWave;
            playerCount = teamSize;
            kills = downs = revives = 0;
            damageDealt = damageTaken = physicalDamageTaken = magicDamageTaken = 0f;
            sanitySpent = sanityLostInDarkness = sanityLostOther = 0f;
            minHealthFraction = _health.MaxHealth > 0f ? _health.CurrentHealth / _health.MaxHealth : 0f;
            minSanityFraction = _sanity.MaxSanity > 0f ? _sanity.CurrentSanity / _sanity.MaxSanity : 0f;
        }

        public void Unsubscribe()
        {
            _health.DamageReceived -= RecordDamageTaken;
            _health.DamageDealt -= RecordDamageDealt;
            _health.EnemyKilled -= RecordKill;
            _health.Died -= RecordDown;
            _health.Revived -= RecordRevive;
            _health.HealthChanged -= RecordHealth;
            _sanity.SanityChanged -= RecordSanity;
            _sanity.SanitySpent -= RecordSanitySpent;
            _sanity.SanityLost -= RecordSanityLoss;
        }

        private void RecordDamageTaken(float amount, EDamageType damageType, bool critical)
        {
            damageTaken += amount;
            if (damageType == EDamageType.Physical)
                physicalDamageTaken += amount;
            if (damageType == EDamageType.Magic)
                magicDamageTaken += amount;
        }

        private void RecordDamageDealt(float amount) => damageDealt += amount;
        private void RecordKill(ActorHealth enemy) => kills++;
        private void RecordDown() => downs++;
        private void RecordRevive() => revives++;
        private void RecordHealth(float current, float maximum) => minHealthFraction = Mathf.Min(minHealthFraction, maximum > 0f ? current / maximum : 0f);
        private void RecordSanity(float current, float maximum) => minSanityFraction = Mathf.Min(minSanityFraction, maximum > 0f ? current / maximum : 0f);
        private void RecordSanitySpent(float amount) => sanitySpent += amount;
        private void RecordSanityLoss(float amount, bool fromDarkness)
        {
            if (fromDarkness)
                sanityLostInDarkness += amount;
            else
                sanityLostOther += amount;
        }
    }

    private readonly Dictionary<ulong, PlayerPhase> _players = new();
    private NetworkManager _networkManager;
    private WaveDirector _director;
    private GameDefeatController _defeat;
    private string _path;
    private string _phase;
    private string _endReason = "stopped";
    private double _runStartedAt;
    private double _phaseStartedAt;
    private int _arenaHandle;
    private int _completedWaves;
    private bool _finishPending;

    public void Begin(NetworkManager networkManager, WaveDirector director, GameDefeatController defeat)
    {
        _networkManager = networkManager;
        _director = director;
        _defeat = defeat;
        Scene arena = SceneManager.GetActiveScene();
        _arenaHandle = arena.handle;
        _runStartedAt = Time.realtimeSinceStartupAsDouble;
        DateTime startedAt = DateTime.UtcNow;
        string directory = Path.Combine(Application.persistentDataPath, "RunStatistics");
        try
        {
            Directory.CreateDirectory(directory);
            _path = Path.Combine(directory, $"run-{startedAt:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.jsonl");
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            Debug.LogWarning($"Не удалось создать папку статистики забегов: {error.Message}", this);
            Destroy(gameObject);
            return;
        }

        foreach (NetworkClient client in _networkManager.ConnectedClientsList)
            if (client.PlayerObject != null)
                _players.Add(client.ClientId, new PlayerPhase(client.PlayerObject, _players.Count + 1));

        Append(JsonUtility.ToJson(new SessionStart
        {
            startedAtUtc = startedAt.ToString("O"),
            buildVersion = Application.version,
            buildGuid = Application.buildGUID,
            arena = arena.name,
            playerCount = _players.Count
        }));
        if (_path == null)
            return;

        _director.CalmStarted += OnCalmStarted;
        _director.WaveStarted += OnWaveStarted;
        _director.CycleStopped += OnCycleStopped;
        _defeat.Defeated += OnDefeated;
        _networkManager.OnClientDisconnectCallback += OnPlayerDisconnected;
        SceneManager.activeSceneChanged += OnSceneChanged;
        BeginPhase("calm", _director.WaveCount + 1);
    }

    private void OnWaveStarted() => BeginPhase("wave", _director.WaveCount);

    private void OnCalmStarted()
    {
        _completedWaves = _director.WaveCount;
        BeginPhase("calm", _completedWaves + 1);
    }

    private void BeginPhase(string phase, int wave)
    {
        if (_phase != null)
            SavePhase(completed: true);
        _phase = phase;
        _phaseStartedAt = Time.realtimeSinceStartupAsDouble;
        foreach (PlayerPhase player in _players.Values)
            player.Reset(phase, wave, _players.Count);
    }

    private void SavePhase(bool completed)
    {
        var lines = new StringBuilder();
        double elapsed = Time.realtimeSinceStartupAsDouble - _phaseStartedAt;
        foreach (PlayerPhase player in _players.Values)
            lines.AppendLine(SerializePhase(player, completed, elapsed));
        if (lines.Length > 0)
            Append(lines.ToString().TrimEnd('\r', '\n'));
    }

    private static string SerializePhase(PlayerPhase player, bool completed, double elapsed)
    {
        player.completed = completed;
        player.elapsedSeconds = elapsed;
        return JsonUtility.ToJson(player);
    }

    private void OnPlayerDisconnected(ulong clientId)
    {
        if (_finishPending || !_players.Remove(clientId, out PlayerPhase player))
            return;
        player.Unsubscribe();
        Append(SerializePhase(player, completed: false, Time.realtimeSinceStartupAsDouble - _phaseStartedAt));
    }

    private void OnDefeated() => _endReason = "defeat";
    private void OnCycleStopped() => _finishPending = true;

    private void LateUpdate()
    {
        // The last down can stop the wave before the player's death subscribers have all run.
        if (!_finishPending)
            return;
        Finish();
        Destroy(gameObject);
    }

    private void OnSceneChanged(Scene previous, Scene next)
    {
        if (previous.handle != _arenaHandle)
            return;
        Finish();
        Destroy(gameObject);
    }

    private void Finish()
    {
        if (_path == null)
            return;
        if (_phase != null)
            SavePhase(completed: false);
        Append(JsonUtility.ToJson(new SessionEnd
        {
            reason = _endReason,
            completedWaves = _completedWaves,
            elapsedSeconds = Time.realtimeSinceStartupAsDouble - _runStartedAt
        }));
        _path = null;
    }

    private void Append(string lines)
    {
        if (_path == null)
            return;
        try
        {
            File.AppendAllText(_path, lines + Environment.NewLine);
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            Debug.LogWarning($"Не удалось записать статистику забега: {error.Message}", this);
            _path = null;
            Destroy(gameObject);
        }
    }

    private void OnApplicationQuit() => Finish();

    private void OnDestroy()
    {
        Finish();
        SceneManager.activeSceneChanged -= OnSceneChanged;
        if (_director != null)
        {
            _director.CalmStarted -= OnCalmStarted;
            _director.WaveStarted -= OnWaveStarted;
            _director.CycleStopped -= OnCycleStopped;
        }
        if (_defeat != null)
            _defeat.Defeated -= OnDefeated;
        if (_networkManager != null)
            _networkManager.OnClientDisconnectCallback -= OnPlayerDisconnected;
        foreach (PlayerPhase player in _players.Values)
            player.Unsubscribe();
    }
}
