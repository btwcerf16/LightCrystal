using System;
using Unity.Netcode;

public struct EnchantState : INetworkSerializable, IEquatable<EnchantState>
{
    public int First;
    public int Second;
    public int Third;

    public int GetId(int index)
    {
        if (index == 0) return First;
        if (index == 1) return Second;
        if (index == 2) return Third;
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    public bool Equals(EnchantState other)
    {
        return First == other.First && Second == other.Second && Third == other.Third;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref First);
        serializer.SerializeValue(ref Second);
        serializer.SerializeValue(ref Third);
    }
}
