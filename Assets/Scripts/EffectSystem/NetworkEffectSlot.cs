using System;
using Unity.Netcode;

public struct NetworkEffectSlot : INetworkSerializable, IEquatable<NetworkEffectSlot>
{
    public int InstanceId;
    public int EffectIndex;
    public double EndsAt;

    public bool Equals(NetworkEffectSlot other)
    {
        return InstanceId == other.InstanceId && EffectIndex == other.EffectIndex && EndsAt == other.EndsAt;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref InstanceId);
        serializer.SerializeValue(ref EffectIndex);
        serializer.SerializeValue(ref EndsAt);
    }
}
