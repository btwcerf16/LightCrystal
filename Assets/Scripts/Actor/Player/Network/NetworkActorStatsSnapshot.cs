using System;
using Unity.Netcode;
using UnityEngine;

public struct NetworkActorStatsSnapshot : INetworkSerializable, IEquatable<NetworkActorStatsSnapshot>
{
    public NetworkStatValue ViewDistance;
    public NetworkStatValue ViewAngle;
    public NetworkStatValue MaxHealth;
    public NetworkStatValue MaxSanity;
    public NetworkStatValue HealthRegeneration;
    public NetworkStatValue MoveSpeed;
    public NetworkStatValue AttackSpeed;
    public NetworkStatValue CooldownReduction;
    public NetworkStatValue PhysicalDamage;
    public NetworkStatValue PhysicalDamageMultiplier;
    public NetworkStatValue MagicDamageMultiplier;
    public NetworkStatValue PhysicalLifeSteal;
    public NetworkStatValue MagicLifeSteal;
    public NetworkStatValue PhysicalResistance;
    public NetworkStatValue MagicResistance;
    public NetworkStatValue ForceResistance;
    public NetworkStatValue MadnessResistance;
    public NetworkStatValue EffectResistance;

    public NetworkActorStatsSnapshot(ActorStats stats)
    {
        ViewDistance = new NetworkStatValue(stats.ViewDistance);
        ViewAngle = new NetworkStatValue(stats.ViewAngle);
        MaxHealth = new NetworkStatValue(stats.MaxHealth);
        MaxSanity = new NetworkStatValue(stats.MaxSanity);
        HealthRegeneration = new NetworkStatValue(stats.HealthRegeneration);
        MoveSpeed = new NetworkStatValue(stats.MoveSpeed);
        AttackSpeed = new NetworkStatValue(stats.AttackSpeed);
        CooldownReduction = new NetworkStatValue(stats.CooldownReduction);
        PhysicalDamage = new NetworkStatValue(stats.PhysicalDamage);
        PhysicalDamageMultiplier = new NetworkStatValue(stats.PhysicalDamageMultiplier);
        MagicDamageMultiplier = new NetworkStatValue(stats.MagicDamageMultiplier);
        PhysicalLifeSteal = new NetworkStatValue(stats.PhysicalLifeSteal);
        MagicLifeSteal = new NetworkStatValue(stats.MagicLifeSteal);
        PhysicalResistance = new NetworkStatValue(stats.PhysicalResistance);
        MagicResistance = new NetworkStatValue(stats.MagicResistance);
        ForceResistance = new NetworkStatValue(stats.ForceResistance);
        MadnessResistance = new NetworkStatValue(stats.MadnessResistance);
        EffectResistance = new NetworkStatValue(stats.EffectResistance);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ViewDistance);
        serializer.SerializeValue(ref ViewAngle);
        serializer.SerializeValue(ref MaxHealth);
        serializer.SerializeValue(ref MaxSanity);
        serializer.SerializeValue(ref HealthRegeneration);
        serializer.SerializeValue(ref MoveSpeed);
        serializer.SerializeValue(ref AttackSpeed);
        serializer.SerializeValue(ref CooldownReduction);
        serializer.SerializeValue(ref PhysicalDamage);
        serializer.SerializeValue(ref PhysicalDamageMultiplier);
        serializer.SerializeValue(ref MagicDamageMultiplier);
        serializer.SerializeValue(ref PhysicalLifeSteal);
        serializer.SerializeValue(ref MagicLifeSteal);
        serializer.SerializeValue(ref PhysicalResistance);
        serializer.SerializeValue(ref MagicResistance);
        serializer.SerializeValue(ref ForceResistance);
        serializer.SerializeValue(ref MadnessResistance);
        serializer.SerializeValue(ref EffectResistance);
    }

    public bool Equals(NetworkActorStatsSnapshot other)
    {
        return ViewDistance.Equals(other.ViewDistance)
            && ViewAngle.Equals(other.ViewAngle)
            && MaxHealth.Equals(other.MaxHealth)
            && MaxSanity.Equals(other.MaxSanity)
            && HealthRegeneration.Equals(other.HealthRegeneration)
            && MoveSpeed.Equals(other.MoveSpeed)
            && AttackSpeed.Equals(other.AttackSpeed)
            && CooldownReduction.Equals(other.CooldownReduction)
            && PhysicalDamage.Equals(other.PhysicalDamage)
            && PhysicalDamageMultiplier.Equals(other.PhysicalDamageMultiplier)
            && MagicDamageMultiplier.Equals(other.MagicDamageMultiplier)
            && PhysicalLifeSteal.Equals(other.PhysicalLifeSteal)
            && MagicLifeSteal.Equals(other.MagicLifeSteal)
            && PhysicalResistance.Equals(other.PhysicalResistance)
            && MagicResistance.Equals(other.MagicResistance)
            && ForceResistance.Equals(other.ForceResistance)
            && MadnessResistance.Equals(other.MadnessResistance)
            && EffectResistance.Equals(other.EffectResistance);
    }
}

