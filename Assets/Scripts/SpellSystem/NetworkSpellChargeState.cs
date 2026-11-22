using System;
using Unity.Netcode;

internal struct NetworkSpellChargeState : INetworkSerializable, IEquatable<NetworkSpellChargeState>
{
    public int AvailableCharges;
    public double RechargesAt;

    public bool Equals(NetworkSpellChargeState other) =>
        AvailableCharges == other.AvailableCharges && RechargesAt == other.RechargesAt;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref AvailableCharges);
        serializer.SerializeValue(ref RechargesAt);
    }
}
