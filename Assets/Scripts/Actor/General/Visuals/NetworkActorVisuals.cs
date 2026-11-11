using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(ActorVisuals), typeof(ActorHealth))]
public sealed class NetworkActorVisuals : NetworkBehaviour
{
    private const int FullOutlineHealthPercent = 40;

    private readonly NetworkVariable<byte> _outlineHealthPercent = new(FullOutlineHealthPercent);
    private ActorVisuals _visuals;
    private ActorHealth _health;
    private NetworkEffectHandler _effects;
    private bool _fadeOutlineWithHealth;

    private void Awake()
    {
        _visuals = GetComponent<ActorVisuals>();
        _health = GetComponent<ActorHealth>();
        _effects = GetComponent<NetworkEffectHandler>();
        _fadeOutlineWithHealth = TryGetComponent<Enemy>(out _);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            _health.DamageReceived += OnDamageTaken;
        if (_fadeOutlineWithHealth)
        {
            if (IsServer)
            {
                _health.HealthChanged += PublishOutlineHealth;
                PublishOutlineHealth(_health.CurrentHealth, _health.MaxHealth);
            }
            _outlineHealthPercent.OnValueChanged += UpdateOutline;
            UpdateOutline(_outlineHealthPercent.Value, _outlineHealthPercent.Value);
        }
        if (_effects != null)
        {
            _effects.EffectsChanged += RefreshTint;
            RefreshTint();
        }
    }

    public override void OnNetworkDespawn()
    {
        _health.DamageReceived -= OnDamageTaken;
        if (_fadeOutlineWithHealth)
        {
            _health.HealthChanged -= PublishOutlineHealth;
            _outlineHealthPercent.OnValueChanged -= UpdateOutline;
            _visuals.SetOutlineOpacity(1f);
        }
        if (_effects != null)
            _effects.EffectsChanged -= RefreshTint;
        _visuals.ClearEffectTint();
        _visuals.ClearHit();
    }

    private void PublishOutlineHealth(float currentHealth, float maxHealth)
    {
        float healthPercent = maxHealth > 0f ? currentHealth / maxHealth * 100f : 0f;
        _outlineHealthPercent.Value = (byte)Mathf.Clamp(Mathf.RoundToInt(healthPercent), 0, FullOutlineHealthPercent);
    }

    private void UpdateOutline(byte previousHealthPercent, byte healthPercent)
    {
        float opacity = Mathf.Lerp(0.35f, 1f, (float)healthPercent / FullOutlineHealthPercent);
        _visuals.SetOutlineOpacity(opacity);
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
