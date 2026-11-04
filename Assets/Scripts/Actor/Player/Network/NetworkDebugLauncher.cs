using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
public sealed class NetworkDebugLauncher : MonoBehaviour
{
    private NetworkManager _networkManager;
    private LobbyConnection _connection;

    private void Awake()
    {
        _networkManager = GetComponent<NetworkManager>();
        _connection = GetComponent<LobbyConnection>();
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10f, 10f, 160f, 120f), GUI.skin.box);

        if (!_networkManager.IsListening)
        {
            if (GUILayout.Button("Start Host"))
            {
                if (_connection != null) _connection.CreateLobby();
                else _networkManager.StartHost();
            }

            if (GUILayout.Button("Start Client"))
            {
                if (_connection != null) _connection.Connect(GetComponent<UnityTransport>().ConnectionData.Address);
                else _networkManager.StartClient();
            }
        }
        else
        {
            GUILayout.Label(_networkManager.IsHost ? "Host" : "Client");
            GUILayout.Label($"Client ID: {_networkManager.LocalClientId}");
        }

        GUILayout.EndArea();
    }
}
