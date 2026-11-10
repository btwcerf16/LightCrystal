using System;
using Unity.Netcode;

public struct NetworkRuneState : INetworkSerializable, IEquatable<NetworkRuneState>
{
    public int EnchantId;
    public int SpriteIndex;
    public bool IsBroken;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref EnchantId);
        serializer.SerializeValue(ref SpriteIndex);
        serializer.SerializeValue(ref IsBroken);
    }

    public bool Equals(NetworkRuneState other) => EnchantId == other.EnchantId && SpriteIndex == other.SpriteIndex && IsBroken == other.IsBroken;
}
