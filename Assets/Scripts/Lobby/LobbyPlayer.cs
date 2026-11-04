using System;
using Unity.Netcode;

public struct LobbyPlayer : INetworkSerializable, IEquatable<LobbyPlayer>
{
    public ulong ClientId;
    public int CharacterIndex;
    public bool IsReady;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref CharacterIndex);
        serializer.SerializeValue(ref IsReady);
    }
    public bool Equals(LobbyPlayer other) => ClientId == other.ClientId && CharacterIndex == other.CharacterIndex && IsReady == other.IsReady;
}
