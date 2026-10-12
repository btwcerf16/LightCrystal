using System;
using Unity.Netcode;

public struct NetworkStatValue : INetworkSerializable, IEquatable<NetworkStatValue>
{
    public float BaseValue;
    public float Value;
    public float BonusValue => Value - BaseValue;

    public NetworkStatValue(float baseValue, float value)
    {
        BaseValue = baseValue;
        Value = value;
    }
    
    public NetworkStatValue(Stat stat)
    {
        BaseValue = stat.BaseValue;
        Value = stat.Value;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref BaseValue);
        serializer.SerializeValue(ref Value);
    }

    public bool Equals(NetworkStatValue other)
    {
        return Value == other.Value && BaseValue == other.BaseValue;
    }

    public override bool Equals(object obj)
    {
        return obj is NetworkStatValue other && Equals(other);
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(BaseValue, Value);
    }
}

