using System;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkManager))]
public sealed class ItemUidGenerator : MonoBehaviour
{
    private NetworkManager _networkManager;
    private ulong _lastUid;

    private void Awake()
    {
        _networkManager = GetComponent<NetworkManager>();
    }

    public ulong GenerateUid()
    {
        if (!_networkManager.IsServer)
            throw new InvalidOperationException("Only the server can generate item UIDs.");

        _lastUid = checked(_lastUid + 1);
        return _lastUid;
    }
}
