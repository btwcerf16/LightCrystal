using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(NetworkActorVitals), typeof(CrystalVisuals))]
public sealed class NetworkCrystal : NetworkBehaviour, ITooltipSource
{
    private ActorHealth _health;
    private NetworkActorVitals _vitals;
    private Crystal _crystal;
    private CrystalVisuals _visuals;
    private WaveDirector _director;

    public Vector2 HealthState => IsSpawned ? new Vector2(_vitals.State.x, _vitals.State.y) : new Vector2(_health.CurrentHealth, _health.MaxHealth);
    public string TooltipTitle => "Кристалл";
    public string TooltipDescription => $"Здоровье: {HealthState.x:0.#} / {HealthState.y:0.#}";
    public float TooltipDelay => 1.5f;

    private void Awake()
    {
        _health = GetComponent<ActorHealth>();
        _vitals = GetComponent<NetworkActorVitals>();
        _crystal = GetComponent<Crystal>();
        _visuals = GetComponent<CrystalVisuals>();
    }

    public override void OnNetworkSpawn()
    {
        _vitals.Changed += Apply;
        Apply(_vitals.State);
        if (IsServer && NetworkManager.TryGetComponent(out _director))
            _director.CalmStarted += OnCalmStarted;
    }

    public override void OnNetworkDespawn()
    {
        _vitals.Changed -= Apply;
        if (_director != null)
            _director.CalmStarted -= OnCalmStarted;
        _crystal.enabled = false;
    }

    private void Apply(Vector4 state)
    {
        _visuals.SetHealth(state.x, state.y);
        _crystal.SetDestroyed(state.x <= 0f);
    }

    private void OnCalmStarted()
    {
        if (_director.WaveCount == 0 || _health.IsDead)
            return;

        PlayWavePulseRpc();
        NetworkWorldAudio.Play(NetworkManager, WorldSoundId.WaveCompleted, transform.position);
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void PlayWavePulseRpc() => _visuals.PlayWavePulse();
}
