using System;
using System.Collections.Generic;
using UnityEngine;

public class WaveDirector : MonoBehaviour
{
    [SerializeField] private EnemyDatabase _enemyDatabase;
    [SerializeField] private NightmareGrowthData _waveEffect;
    [SerializeField] private Transform _mainTarget;
    [SerializeField] private ShopSpawnArea _spawnArea;
    [SerializeField, Min(0f)] private float _minimumCrystalDistance = 20f;
    [SerializeField, Min(1)] private int _waveBudget = 10;
    [SerializeField, Min(0)] private int _budgetPerWave = 5;
    [SerializeField, Min(1)] private int _calmBudget = 4;
    [SerializeField, Min(1)] private int _enemiesPerSubwave = 3;
    [SerializeField, Range(0f, 45f)] private float _initialCalmDuration = 20f;
    [SerializeField, Min(0f)] private float _calmDurationPerWave = 5f;

    private readonly StateMachine _stateMachine = new();
    private readonly Dictionary<Enemy, Action> _deathHandlers = new();
    private WaveStartState _waveState;
    private CalmState _calmState;
    private bool _isInitialized;

    public bool IsPaused { get; private set; }
    public int WaveCount { get; private set; }
    public float CalmTimeRemaining { get; private set; }
    public State CurrentState => _stateMachine.CurrentState;
    public List<Enemy> ActiveEnemies { get; private set; } = new();
    public List<Enemy> WaveEnemies { get; private set; } = new();
    public List<Enemy> CalmEnemies { get; private set; } = new();
    public EnemyDatabase EnemyDatabase => _enemyDatabase;
    public event Action<Enemy> EnemyActivated;
    public event Action CalmStarted;
    public event Action WaveStarted;
    public event Action PauseChanged;

    private void Awake()
    {
        _waveState = new WaveStartState(this);
        _calmState = new CalmState(this);
    }

    public void Initialize()
    {
        if (_isInitialized)
            return;
        if (_mainTarget == null)
        {
            Crystal crystal = FindFirstObjectByType<Crystal>();
            if (crystal != null)
                _mainTarget = crystal.transform;
        }
        if (_spawnArea == null)
            _spawnArea = FindFirstObjectByType<ShopSpawnArea>();
        if (_enemyDatabase == null || _spawnArea == null || _mainTarget == null)
        {
            Debug.LogError("WaveDirector requires an enemy database, a crystal and a map spawn area.", this);
            return;
        }
        _isInitialized = true;
        _stateMachine.Initialize(_calmState);
    }

    private void Update()
    {
        if (!IsPaused)
            _stateMachine.Update();
    }

    public void SetPaused(bool paused)
    {
        if (IsPaused == paused)
            return;
        IsPaused = paused;
        PauseChanged?.Invoke();
    }

    public void CreateWaveEnemies()
    {
        if (WaveEnemies.Count == 0)
            CreateEnemies(_waveBudget + WaveCount * _budgetPerWave, WaveEnemies, false);
    }

    public void CreateCalmEnemies()
    {
        if (CalmEnemies.Count == 0)
            CreateEnemies(_calmBudget, CalmEnemies, true);
    }

    private void CreateEnemies(int budget, List<Enemy> enemies, bool isCalm)
    {
        List<EnemyDatabase.Entry> candidates = new();
        while (budget > 0)
        {
            candidates.Clear();
            foreach (EnemyDatabase.Entry entry in _enemyDatabase.Enemies)
            {
                if (entry != null && entry.Prefab != null && entry.Weight > 0 && entry.Weight <= budget)
                    candidates.Add(entry);
            }
            if (candidates.Count == 0)
                break;

            EnemyDatabase.Entry selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            if (!_spawnArea.TryGetPosition(_mainTarget.position, _minimumCrystalDistance, out Vector3 position))
            {
                Debug.LogError("No enemy spawn position found away from the crystal.", this);
                break;
            }
            Enemy enemy = GetComponent<NetworkObjectPool>().GetObject(selected.Prefab, position, Quaternion.identity);
            enemy.gameObject.SetActive(false);
            enemy.SetMainTarget(_mainTarget);
            enemy.SetWaitingForWave(isCalm);
            enemies.Add(enemy);
            Action onDied = () => RemoveEnemy(enemy);
            _deathHandlers.Add(enemy, onDied);
            enemy.GetComponent<ActorHealth>().Died += onDied;
            budget -= selected.Weight;
        }

        if (isCalm)
        {
            foreach (Enemy enemy in enemies)
                ActivateEnemy(enemy);
        }
    }

    private void ActivateEnemy(Enemy enemy)
    {
        enemy.gameObject.SetActive(true);
        EnemyActivated?.Invoke(enemy);
        StrengthenEnemy(enemy);
        enemy.enabled = true;
        enemy.Initialize(_mainTarget);
    }

    private void StrengthenEnemy(Enemy enemy)
    {
        if (_waveEffect == null || WaveCount == 0)
            return;
        EffectHandler effects = enemy.GetComponent<EffectHandler>();
        int stacks = 0;
        foreach (Effect effect in effects.ActiveEffects)
            if (effect.EffectData == _waveEffect)
                stacks++;
        for (int i = stacks; i < WaveCount; i++)
            effects.AddEffect(_waveEffect);
    }

    private void RemoveEnemy(Enemy enemy)
    {
        ActiveEnemies.Remove(enemy);
        WaveEnemies.Remove(enemy);
        CalmEnemies.Remove(enemy);
        if (_deathHandlers.TryGetValue(enemy, out Action onDied))
        {
            enemy.GetComponent<ActorHealth>().Died -= onDied;
            _deathHandlers.Remove(enemy);
        }
    }

    public void StartCalm()
    {
        CalmTimeRemaining = Mathf.Min(45f, _initialCalmDuration + WaveCount * _calmDurationPerWave);
        CreateWaveEnemies();
        CreateCalmEnemies();
        CalmStarted?.Invoke();
    }

    public void UpdateCalm(float deltaTime)
    {
        if (IsPaused)
            return;
        CalmTimeRemaining = Mathf.Max(0f, CalmTimeRemaining - Mathf.Max(0f, deltaTime));
        if (CalmTimeRemaining == 0f)
            _stateMachine.ChangeState(_waveState);
    }

    public void StartWave()
    {
        WaveCount++;
        StartNextSubwave();
        foreach (Enemy enemy in CalmEnemies)
        {
            enemy.SetWaitingForWave(false);
            ActiveEnemies.Add(enemy);
        }
        CalmEnemies.Clear();
        foreach (Enemy enemy in ActiveEnemies)
            StrengthenEnemy(enemy);
        WaveStarted?.Invoke();
    }

    public bool StartNextSubwave()
    {
        if (IsPaused || ActiveEnemies.Count > 0)
            return false;

        int count = 0;
        foreach (Enemy enemy in WaveEnemies)
        {
            if (enemy.gameObject.activeSelf)
                continue;
            ActiveEnemies.Add(enemy);
            ActivateEnemy(enemy);
            count++;
            if (count >= _enemiesPerSubwave)
                break;
        }
        return count > 0;
    }

    public void UpdateWave()
    {
        if (!IsPaused && ActiveEnemies.Count == 0 && !StartNextSubwave())
            _stateMachine.ChangeState(_calmState);
    }

    public void Stop()
    {
        _stateMachine.Stop();
        _isInitialized = false;
        WaveCount = 0;
        CalmTimeRemaining = 0f;
        SetPaused(false);
        foreach (var pair in _deathHandlers)
        {
            if (pair.Key == null)
                continue;
            pair.Key.GetComponent<ActorHealth>().Died -= pair.Value;
            GetComponent<NetworkObjectPool>().ReturnObject(pair.Key.GetComponent<Unity.Netcode.NetworkObject>());
        }
        _deathHandlers.Clear();
        ActiveEnemies.Clear();
        WaveEnemies.Clear();
        CalmEnemies.Clear();
    }

    private void OnDestroy()
    {
        Stop();
    }
}
