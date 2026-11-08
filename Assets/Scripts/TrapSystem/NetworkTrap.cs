using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(Trap))]
public sealed class NetworkTrap : NetworkBehaviour
{
    private Trap _trap;

    private void Awake() => _trap = GetComponent<Trap>();

    public override void OnNetworkSpawn()
    {
        _trap.enabled = IsServer;
        if (IsServer)
            _trap.Finished += Despawn;
    }

    public override void OnNetworkDespawn() => _trap.Finished -= Despawn;

    private void Despawn()
    {
        if (IsServer && IsSpawned)
            NetworkManager.GetComponent<NetworkObjectPool>().ReturnObject(NetworkObject);
    }
}
