using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(NetworkActorVitals), typeof(CrystalVisuals))]
public sealed class NetworkCrystal : NetworkBehaviour, ITooltipSource
{
    private ActorHealth _health;
    private NetworkActorVitals _vitals;
    private Crystal _crystal;
    private CrystalVisuals _visuals;

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
    }

    public override void OnNetworkDespawn()
    {
        _vitals.Changed -= Apply;
        _crystal.enabled = false;
    }

    private void Apply(Vector4 state)
    {
        _visuals.SetHealth(state.x, state.y);
        _crystal.SetDestroyed(state.x <= 0f);
    }
}
