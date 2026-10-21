using System;
using Unity.Netcode;

public struct NetworkInventorySlot : INetworkSerializable, IEquatable<NetworkInventorySlot>
{
    public bool IsConquered;
    public int SlotIndex;
    public int ItemId;

    public bool Equals(NetworkInventorySlot other)
    {
        return other.IsConquered == this.IsConquered && other.SlotIndex == this.SlotIndex && other.ItemId == this.ItemId;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {   
        serializer.SerializeValue(ref IsConquered);
        serializer.SerializeValue(ref SlotIndex);
        serializer.SerializeValue(ref ItemId);
    }
}

