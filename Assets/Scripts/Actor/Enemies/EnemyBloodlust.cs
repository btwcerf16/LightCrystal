using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Enemy), typeof(ActorHealth), typeof(EffectHandler))]
public sealed class EnemyBloodlust : MonoBehaviour
{
    [SerializeField] private float _chaseDelay = 10f;
    [SerializeField] private BloodlustData _effect;
    private Enemy _enemy;
    private ActorHealth _health;
    private NetworkObject _networkObject;
    private EffectHandler _effects;
    private float _chaseTime;
    private float _nextStackAt;
    public int Stacks { get; private set; }

    private void Awake()
    {
        _enemy = GetComponent<Enemy>();
        _health = GetComponent<ActorHealth>();
        _networkObject = GetComponent<NetworkObject>();
        _effects = GetComponent<EffectHandler>();
    }

    private void OnEnable()
    {
        _nextStackAt = _chaseDelay;
    }

    private void Update()
    {
        if (_networkObject == null || !_networkObject.IsSpawned || !_networkObject.NetworkManager.IsServer
            || _effect == null || Stacks >= _effect.MaxStacks)
            return;
        Transform target = _enemy.CurrentTarget;
        if (!_enemy.isActiveAndEnabled || _health.IsDead || target == null || target.GetComponent<PlayerInputReader>() == null)
        {
            _chaseTime = 0f;
            _nextStackAt = _chaseDelay;
            return;
        }
        _chaseTime += Time.deltaTime;
        if (_chaseTime < _nextStackAt)
            return;
        _nextStackAt += 1f;
        if (_effects.AddEffect(_effect) != null)
            Stacks++;
    }

    private void OnDisable()
    {
        for (int i = _effects.ActiveEffects.Count - 1; i >= 0; i--)
            if (_effects.ActiveEffects[i].EffectData == _effect)
                _effects.RemoveEffect(_effects.ActiveEffects[i]);
        Stacks = 0;
        _chaseTime = 0f;
    }
}
