using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class NetworkActorStats : NetworkBehaviour
{
    private readonly NetworkVariable<NetworkActorStatsSnapshot> _snapshot =
        new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private ActorStats _stats;
    private PlayerInputReader _input;
    private Stat[] _allStats;

    private void Awake()
    {
        _stats = GetComponent<ActorStats>();
        _input = GetComponent<PlayerInputReader>();
    }

    private void Update()
    {
        if (!IsOwner || Keyboard.current == null || (_input != null && _input.IsMenuOpen))
        {
            return;
        }
        if (Keyboard.current.tKey.wasPressedThisFrame) 
        {
            IncreaseViewDistanceRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void IncreaseViewDistanceRpc()
    {
        float newBaseValue = _stats.ViewDistance.BaseValue + 1f;
        _stats.ViewDistance.SetBaseValue(newBaseValue);
    }

    public override void OnNetworkSpawn()
    {
        _allStats = new[]
{
            _stats.ViewDistance,
            _stats.ViewAngle,
            _stats.MaxHealth,
            _stats.MaxSanity,
            _stats.HealthRegeneration,
            _stats.ReviveSpeed,
            _stats.MoveSpeed,
            _stats.AttackSpeed,
            _stats.CooldownReduction,
            _stats.PhysicalDamage,
            _stats.PhysicalDamageMultiplier,
            _stats.MagicDamageMultiplier,
            _stats.PhysicalLifeSteal,
            _stats.MagicLifeSteal,
            _stats.PhysicalResistance,
            _stats.MagicResistance,
            _stats.ForceResistance,
            _stats.MadnessResistance,
            _stats.EffectResistance,
            _stats.Stability,
            _stats.CameraDistance,
            _stats.CastSpeed,
            _stats.ProjectileSpeed,
            _stats.ProjectileSpeedMultiplier,
            _stats.ProjectileRange,
            _stats.ProjectilePiercing,
            _stats.SanityCostReduction,
            _stats.CriticalChance,
            _stats.CriticalDamageMultiplier
        };
        if (IsServer)
        {
            foreach (Stat stat in _allStats)
                stat.ValueChanged += PublishSnapshot;
            _stats.TargetPriorityChanged += PublishTargetPriority;

            PublishSnapshot(0f);
            return;
        }

        _snapshot.OnValueChanged += ReceiveSnapshot;
        ApplySnapshot(_snapshot.Value);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            foreach (Stat stat in _allStats)
                stat.ValueChanged -= PublishSnapshot;
            _stats.TargetPriorityChanged -= PublishTargetPriority;

            return;
        }

        _snapshot.OnValueChanged -= ReceiveSnapshot;
    }


    private void PublishSnapshot(float _)
    {
        _snapshot.Value = new NetworkActorStatsSnapshot(_stats);
    }

    private void PublishTargetPriority(int priority)
    {
        PublishSnapshot(0f);
    }
    private void ReceiveSnapshot(NetworkActorStatsSnapshot _, NetworkActorStatsSnapshot newSnapshot)
    {
        ApplySnapshot(newSnapshot);
    }

    private void ApplySnapshot(NetworkActorStatsSnapshot snapshot)
    {
        _stats.ViewDistance.SetReplicatedValues(snapshot.ViewDistance.BaseValue, snapshot.ViewDistance.Value);
        _stats.ViewAngle.SetReplicatedValues(snapshot.ViewAngle.BaseValue, snapshot.ViewAngle.Value);
        _stats.MaxHealth.SetReplicatedValues(snapshot.MaxHealth.BaseValue, snapshot.MaxHealth.Value);
        _stats.MaxSanity.SetReplicatedValues(snapshot.MaxSanity.BaseValue, snapshot.MaxSanity.Value);
        _stats.HealthRegeneration.SetReplicatedValues(snapshot.HealthRegeneration.BaseValue, snapshot.HealthRegeneration.Value);
        _stats.ReviveSpeed.SetReplicatedValues(snapshot.ReviveSpeed.BaseValue, snapshot.ReviveSpeed.Value);
        _stats.MoveSpeed.SetReplicatedValues(snapshot.MoveSpeed.BaseValue, snapshot.MoveSpeed.Value);
        _stats.AttackSpeed.SetReplicatedValues(snapshot.AttackSpeed.BaseValue, snapshot.AttackSpeed.Value);
        _stats.CooldownReduction.SetReplicatedValues(snapshot.CooldownReduction.BaseValue, snapshot.CooldownReduction.Value);
        _stats.PhysicalDamage.SetReplicatedValues(snapshot.PhysicalDamage.BaseValue, snapshot.PhysicalDamage.Value);
        _stats.PhysicalDamageMultiplier.SetReplicatedValues(snapshot.PhysicalDamageMultiplier.BaseValue, snapshot.PhysicalDamageMultiplier.Value);
        _stats.MagicDamageMultiplier.SetReplicatedValues(snapshot.MagicDamageMultiplier.BaseValue, snapshot.MagicDamageMultiplier.Value);
        _stats.PhysicalLifeSteal.SetReplicatedValues(snapshot.PhysicalLifeSteal.BaseValue, snapshot.PhysicalLifeSteal.Value);
        _stats.MagicLifeSteal.SetReplicatedValues(snapshot.MagicLifeSteal.BaseValue, snapshot.MagicLifeSteal.Value);
        _stats.PhysicalResistance.SetReplicatedValues(snapshot.PhysicalResistance.BaseValue, snapshot.PhysicalResistance.Value);
        _stats.MagicResistance.SetReplicatedValues(snapshot.MagicResistance.BaseValue, snapshot.MagicResistance.Value);
        _stats.ForceResistance.SetReplicatedValues(snapshot.ForceResistance.BaseValue, snapshot.ForceResistance.Value);
        _stats.MadnessResistance.SetReplicatedValues(snapshot.MadnessResistance.BaseValue, snapshot.MadnessResistance.Value);
        _stats.EffectResistance.SetReplicatedValues(snapshot.EffectResistance.BaseValue, snapshot.EffectResistance.Value);
        _stats.Stability.SetReplicatedValues(snapshot.Stability.BaseValue, snapshot.Stability.Value);
        _stats.CameraDistance.SetReplicatedValues(snapshot.CameraDistance.BaseValue, snapshot.CameraDistance.Value);
        _stats.CastSpeed.SetReplicatedValues(snapshot.CastSpeed.BaseValue, snapshot.CastSpeed.Value);
        _stats.ProjectileSpeed.SetReplicatedValues(snapshot.ProjectileSpeed.BaseValue, snapshot.ProjectileSpeed.Value);
        _stats.ProjectileSpeedMultiplier.SetReplicatedValues(snapshot.ProjectileSpeedMultiplier.BaseValue, snapshot.ProjectileSpeedMultiplier.Value);
        _stats.ProjectileRange.SetReplicatedValues(snapshot.ProjectileRange.BaseValue, snapshot.ProjectileRange.Value);
        _stats.ProjectilePiercing.SetReplicatedValues(snapshot.ProjectilePiercing.BaseValue, snapshot.ProjectilePiercing.Value);
        _stats.SanityCostReduction.SetReplicatedValues(snapshot.SanityCostReduction.BaseValue, snapshot.SanityCostReduction.Value);
        _stats.CriticalChance.SetReplicatedValues(snapshot.CriticalChance.BaseValue, snapshot.CriticalChance.Value);
        _stats.CriticalDamageMultiplier.SetReplicatedValues(snapshot.CriticalDamageMultiplier.BaseValue, snapshot.CriticalDamageMultiplier.Value);
        _stats.SetTargetPriority(snapshot.TargetPriority);
    }
}
