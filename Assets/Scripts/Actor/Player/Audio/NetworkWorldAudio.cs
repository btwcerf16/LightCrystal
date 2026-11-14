using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(WorldAudio))]
public sealed class NetworkWorldAudio : NetworkBehaviour
{
    private WorldAudio _worldAudio;

    private void Awake()
    {
        _worldAudio = GetComponent<WorldAudio>();
    }

    public static void Play(NetworkManager manager, WorldSoundId sound, Vector3 position)
    {
        if (sound == WorldSoundId.None || manager == null || !manager.IsListening || !manager.IsServer)
            return;

        foreach (NetworkClient client in manager.ConnectedClientsList)
        {
            NetworkObject player = client.PlayerObject;
            if (player != null && player.IsSpawned && player.TryGetComponent(out NetworkWorldAudio audio))
                audio.PlaySoundRpc((int)sound, position);
        }
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void PlaySoundRpc(int soundId, Vector3 position)
    {
        _worldAudio.Play(soundId, position);
    }
}
