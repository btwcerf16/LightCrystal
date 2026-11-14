using System;
using Unity.Collections;
using Unity.Netcode;

public struct LobbyPlayer : INetworkSerializable, IEquatable<LobbyPlayer>
{
    public ulong ClientId;
    public int CharacterIndex;
    public bool IsReady;
    public FixedString64Bytes Nickname;
    public string DisplayName => Nickname.IsEmpty ? $"Player{ClientId + 1}" : Nickname.ToString();
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref CharacterIndex);
        serializer.SerializeValue(ref IsReady);
        serializer.SerializeValue(ref Nickname);
    }
    public bool Equals(LobbyPlayer other) => ClientId == other.ClientId && CharacterIndex == other.CharacterIndex
        && IsReady == other.IsReady && Nickname.Equals(other.Nickname);
}
