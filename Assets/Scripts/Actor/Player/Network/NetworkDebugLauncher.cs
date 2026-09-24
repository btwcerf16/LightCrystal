using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
public sealed class NetworkDebugLauncher : MonoBehaviour
{
    private NetworkManager _networkManager;

    private void Awake()
    {
        _networkManager = GetComponent<NetworkManager>();
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10f, 10f, 160f, 120f), GUI.skin.box);

        if (!_networkManager.IsListening)
        {
            if (GUILayout.Button("Start Host"))
                _networkManager.StartHost();

            if (GUILayout.Button("Start Client"))
                _networkManager.StartClient();
        }
        else
        {
            GUILayout.Label(_networkManager.IsHost ? "Host" : "Client");
            GUILayout.Label($"Client ID: {_networkManager.LocalClientId}");
        }

        GUILayout.EndArea();
    }
}