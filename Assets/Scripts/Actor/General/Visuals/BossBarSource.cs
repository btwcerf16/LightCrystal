using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkActorVitals))]
public sealed class BossBarSource : NetworkBehaviour
{
    public static event Action<BossBarSource> Spawned;
    public static event Action<BossBarSource> Despawned;

    public INamed Named { get; private set; }
    public NetworkActorVitals Vitals { get; private set; }

    private void Awake()
    {
        Named = GetComponent<INamed>();
        Vitals = GetComponent<NetworkActorVitals>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsClient)
            Spawned?.Invoke(this);
    }

    public override void OnNetworkDespawn()
    {
        if (IsClient)
            Despawned?.Invoke(this);
    }
}
