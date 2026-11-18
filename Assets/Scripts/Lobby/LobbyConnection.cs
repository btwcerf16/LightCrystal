using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkManager))]
public sealed class LobbyConnection : MonoBehaviour
{
    [SerializeField] private GameObject _lobbyPrefab;
    [SerializeField] private CharacterDatabase _characters;
    public CharacterDatabase Characters => _characters;
    [SerializeField] private int _maximumPlayers = 4;
    private NetworkManager _manager;
    private ushort _multiplayerPort;
    private bool _returning;
    private bool _quitting;
    private bool _leaving;
    private static string _lastError;
    public string Error { get; private set; }
    public bool IsSinglePlayer { get; private set; }
    public bool IsTutorial { get; private set; }

    private void Awake()
    {
        Error = _lastError;
        _manager = GetComponent<NetworkManager>();
        _multiplayerPort = GetComponent<UnityTransport>().ConnectionData.Port;
        _manager.ConnectionApprovalCallback += ApproveConnection;
        _manager.OnServerStarted += OnServerStarted;
        _manager.OnClientStopped += OnStopped;
    }

    public void CreateLobby(bool singlePlayer = false, bool tutorial = false)
    {
        if (_manager.IsListening || _manager.ShutdownInProgress || _returning) return;
        if (_lobbyPrefab == null) { StartupFailed("Префаб лобби не назначен."); return; }
        if (_characters == null || _characters.Count == 0) { StartupFailed("Каталог классов не назначен или пуст."); return; }
        Error = "";
        _leaving = false;
        IsTutorial = tutorial;
        IsSinglePlayer = singlePlayer || tutorial;
        _manager.NetworkConfig.ConnectionApproval = true;
        UnityTransport transport = GetComponent<UnityTransport>();
        transport.SetConnectionData(IsSinglePlayer, "127.0.0.1", IsSinglePlayer ? (ushort)0 : _multiplayerPort,
            IsSinglePlayer ? "127.0.0.1" : "0.0.0.0");
        if (!_manager.StartHost())
        {
            StartupFailed("Не удалось запустить хост на UDP-порту " + transport.ConnectionData.Port + ".");
            return;
        }

        if (tutorial)
            GameLobby.Instance?.StartTutorial();
    }

    public void Connect(string address)
    {
        if (_manager.IsListening || _manager.ShutdownInProgress || _returning) return;
        if (_characters == null || _characters.Count == 0) { StartupFailed("Каталог классов не назначен или пуст."); return; }
        address = address.Trim();
        if (!System.Net.IPAddress.TryParse(address, out _)) { Error = "Введите IP-адрес хоста."; return; }
        Error = "";
        _leaving = false;
        IsSinglePlayer = false;
        IsTutorial = false;
        _manager.NetworkConfig.ConnectionApproval = true;
        UnityTransport transport = GetComponent<UnityTransport>();
        transport.SetConnectionData(address, _multiplayerPort);
        if (!_manager.StartClient()) StartupFailed("Не удалось начать подключение.");
    }

    private void StartupFailed(string message)
    {
        Error = _lastError = message;
        if (_returning) return;
        _returning = true;
        // StartHost can fail after opening the socket and reset its roles; Shutdown then does nothing.
        SceneTransition.Instance.StartCoroutine(ReturnToMenu());
    }

    private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        bool full = _manager.ConnectedClientsIds.Count >= Mathf.Min(_maximumPlayers, _characters.Count);
        bool started = GameLobby.Instance != null && GameLobby.Instance.IsStarting;
        bool closedRoom = IsSinglePlayer && request.ClientNetworkId != NetworkManager.ServerClientId;
        response.Approved = !closedRoom && !full && !started;
        response.CreatePlayerObject = SceneManager.GetActiveScene().name == "Arena01" && GameLobby.Instance == null;
        response.Pending = false;
        response.Reason = closedRoom ? "Одиночная игра." : full ? "Комната заполнена." : started ? "Игра уже началась." : "";
    }

    private void OnServerStarted()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu")
            SpawnLobby();
    }

    public void SpawnLobby()
    {
        if (_manager.IsServer && GameLobby.Instance == null)
            Instantiate(_lobbyPrefab).GetComponent<NetworkObject>().Spawn(false);
    }

    public void Leave()
    {
        _leaving = true;
        if (_manager.IsListening) _manager.Shutdown();
    }

    private void OnStopped(bool wasHost)
    {
        if (_quitting || _returning) return;
        if (_leaving && IsSinglePlayer && SceneManager.GetActiveScene().name == "MainMenu")
        {
            _leaving = false;
            IsSinglePlayer = false;
            IsTutorial = false;
            Error = _lastError = "";
            return;
        }
        if (SceneTransition.Instance == null) return;
        _lastError = _leaving ? "" : string.IsNullOrEmpty(_manager.DisconnectReason)
            ? "Соединение с хостом потеряно." : _manager.DisconnectReason;
        _returning = true;
        SceneTransition.Instance.StartCoroutine(ReturnToMenu());
    }

    private IEnumerator ReturnToMenu()
    {
        yield return SceneTransition.Instance.Cover();
        while (_manager.ShutdownInProgress) yield return null;
        Destroy(_manager.gameObject);
        yield return null;
        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MainMenu");
        SceneTransition.Instance.Open();
    }

    private void OnApplicationQuit() => _quitting = true;
    private void OnDestroy()
    {
        if (_manager == null) return;
        _manager.ConnectionApprovalCallback -= ApproveConnection;
        _manager.OnServerStarted -= OnServerStarted;
        _manager.OnClientStopped -= OnStopped;
    }
}
