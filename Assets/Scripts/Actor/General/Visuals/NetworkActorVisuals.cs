using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(ActorVisuals), typeof(ActorHealth))]
public sealed class NetworkActorVisuals : NetworkBehaviour
{
    private ActorVisuals _visuals;
    private ActorHealth _health;
    private NetworkEffectHandler _effects;

    private void Awake()
    {
        _visuals = GetComponent<ActorVisuals>();
        _health = GetComponent<ActorHealth>();
        _effects = GetComponent<NetworkEffectHandler>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            _health.DamageTaken += OnDamageTaken;
        if (_effects != null)
        {
            _effects.EffectsChanged += RefreshTint;
            RefreshTint();
        }
    }

    public override void OnNetworkDespawn()
    {
        _health.DamageTaken -= OnDamageTaken;
        if (_effects != null)
            _effects.EffectsChanged -= RefreshTint;
        _visuals.ClearEffectTint();
        _visuals.ClearHit();
    }

    private void OnDamageTaken(float damage)
    {
        FlashHitRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void FlashHitRpc()
    {
        _visuals.FlashHit();
    }

    private void RefreshTint()
    {
        EffectData strongest = null;
        for (int i = 0; i < _effects.Count; i++)
        {
            EffectData data = _effects.GetData(_effects.GetEffect(i).EffectIndex);
            if (data != null && data.TintActor && (strongest == null || data.ActorTintStrength >= strongest.ActorTintStrength))
                strongest = data;
        }
        if (strongest != null)
            _visuals.SetEffectTint(strongest.EffectColor, strongest.ActorTintStrength);
        else
            _visuals.ClearEffectTint();
    }
}
