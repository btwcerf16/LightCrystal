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
            _health.DamageReceived += OnDamageTaken;
        if (_effects != null)
        {
            _effects.EffectsChanged += RefreshTint;
            RefreshTint();
        }
    }

    public override void OnNetworkDespawn()
    {
        _health.DamageReceived -= OnDamageTaken;
        if (_effects != null)
            _effects.EffectsChanged -= RefreshTint;
        _visuals.ClearEffectTint();
        _visuals.ClearHit();
    }

    private void OnDamageTaken(float damage, EDamageType type, bool critical)
    {
        FlashHitRpc(damage, type, critical);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void FlashHitRpc(float damage, EDamageType type, bool critical)
    {
        _visuals.FlashHit();
        _visuals.ShowDamage(damage, type, critical);
    }

    public void PlayExplosion(float radius)
    {
        if (IsSpawned && IsServer)
            ExplosionRpc(radius);
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void ExplosionRpc(float radius)
    {
        _visuals.Explosion(radius);
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
