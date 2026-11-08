using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameLobby : NetworkBehaviour
{
    [SerializeField] private CharacterDatabase _characters;
    [SerializeField] private string _mapScene = "Arena01";
    private NetworkList<LobbyPlayer> _players;
    private readonly NetworkVariable<bool> _starting = new(false);
    public static GameLobby Instance { get; private set; }
    public int Count => _players.Count;
    public int CharacterCount => _characters.Count;
    public bool IsStarting => _starting.Value;
    public LobbyPlayer GetPlayer(int index) => _players[index];
    public event Action Changed;

    private void Awake()
    {
        _players = new NetworkList<LobbyPlayer>();
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        Instance = this;
        _players.OnListChanged += OnPlayersChanged;
        _starting.OnValueChanged += OnStartingChanged;
        if (!IsServer) return;
        NetworkManager.OnClientConnectedCallback += AddPlayer;
        NetworkManager.OnClientDisconnectCallback += RemovePlayer;
        NetworkManager.SceneManager.OnLoadEventCompleted += OnMapLoaded;
        foreach (ulong id in NetworkManager.ConnectedClientsIds) AddPlayer(id);
    }

    public override void OnNetworkDespawn()
    {
        _players.OnListChanged -= OnPlayersChanged;
        _starting.OnValueChanged -= OnStartingChanged;
        if (Instance == this) Instance = null;
        if (!IsServer) return;
        NetworkManager.OnClientConnectedCallback -= AddPlayer;
        NetworkManager.OnClientDisconnectCallback -= RemovePlayer;
        NetworkManager.SceneManager.OnLoadEventCompleted -= OnMapLoaded;
    }

    private void OnPlayersChanged(NetworkListEvent<LobbyPlayer> change) => Changed?.Invoke();
    private void OnStartingChanged(bool previous, bool current) => Changed?.Invoke();
    private int FindPlayer(ulong id)
    {
        for (int i = 0; i < Count; i++) if (_players[i].ClientId == id) return i;
        return -1;
    }
    private void AddPlayer(ulong id)
    {
        if (FindPlayer(id) >= 0) return;
        for (int i = 0; i < CharacterCount; i++)
        {
            if (!IsCharacterAvailable(i, id)) continue;
            _players.Add(new LobbyPlayer { ClientId = id, CharacterIndex = i });
            return;
        }
        NetworkManager.DisconnectClient(id, "Все классы заняты.");
    }

    public bool IsCharacterAvailable(int characterIndex, ulong clientId)
    {
        if (characterIndex < 0 || characterIndex >= CharacterCount) return false;
        for (int i = 0; i < Count; i++)
            if (_players[i].ClientId != clientId && _players[i].CharacterIndex == characterIndex) return false;
        return true;
    }
    private void RemovePlayer(ulong id)
    {
        int index = FindPlayer(id);
        if (index >= 0) _players.RemoveAt(index);
        TryStart();
    }

    public void SelectCharacter(int index) => SelectCharacterRpc(index);
    public void SetReady(bool ready, int characterIndex = -1) => SetReadyRpc(ready, characterIndex);

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SelectCharacterRpc(int characterIndex, RpcParams rpc = default)
    {
        if (IsStarting || !IsCharacterAvailable(characterIndex, rpc.Receive.SenderClientId)) return;
        int index = FindPlayer(rpc.Receive.SenderClientId);
        if (index < 0) return;
        LobbyPlayer player = _players[index];
        player.CharacterIndex = characterIndex;
        player.IsReady = false;
        _players[index] = player;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetReadyRpc(bool ready, int characterIndex = -1, RpcParams rpc = default)
    {
        if (IsStarting) return;
        int index = FindPlayer(rpc.Receive.SenderClientId);
        if (index < 0) return;
        LobbyPlayer player = _players[index];
        if (characterIndex >= 0)
        {
            if (!IsCharacterAvailable(characterIndex, rpc.Receive.SenderClientId)) return;
            player.CharacterIndex = characterIndex;
        }
        player.IsReady = ready;
        _players[index] = player;
        TryStart();
    }

    private void TryStart()
    {
        if (!IsServer || IsStarting || Count == 0) return;
        for (int i = 0; i < Count; i++) if (!_players[i].IsReady) return;
        _starting.Value = true;
        CoverClientsRpc();
        StartCoroutine(LoadScene(_mapScene));
    }

    public void ReturnToLobby()
    {
        if (!IsServer)
            return;
        _starting.Value = false;
        for (int i = 0; i < Count; i++)
        {
            LobbyPlayer player = _players[i];
            player.IsReady = false;
            _players[i] = player;
        }
        foreach (NetworkClient client in NetworkManager.ConnectedClients.Values)
            if (client.PlayerObject != null)
                client.PlayerObject.Despawn();
        CoverClientsRpc();
        StartCoroutine(LoadScene("MainMenu"));
    }

    [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
    private void CoverClientsRpc() => SceneTransition.Instance.StartCoroutine(SceneTransition.Instance.Cover());

    private IEnumerator LoadScene(string sceneName)
    {
        yield return SceneTransition.Instance.Cover();
        SceneEventProgressStatus status = NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        if (status == SceneEventProgressStatus.Started) yield break;
        Debug.LogError("Cannot load map: " + status, this);
        _starting.Value = false;
        for (int i = 0; i < Count; i++)
        {
            LobbyPlayer player = _players[i]; player.IsReady = false; _players[i] = player;
        }
        OpenScreenRpc();
    }

    private void OnMapLoaded(string sceneName, LoadSceneMode mode, List<ulong> completed, List<ulong> timedOut)
    {
        if (sceneName == "MainMenu")
        {
            OpenScreenRpc();
            return;
        }
        if (sceneName != _mapScene || !IsStarting) return;
        foreach (ulong id in timedOut) NetworkManager.DisconnectClient(id);
        NetworkManager.GetComponent<GameDefeatController>().BeginMatch();
        Vector3 center = FindFirstObjectByType<Crystal>().transform.position;
        for (int i = 0; i < Count; i++)
        {
            LobbyPlayer choice = _players[i];
            if (!completed.Contains(choice.ClientId) || !NetworkManager.ConnectedClients.ContainsKey(choice.ClientId)) continue;
            CharacterConfig config = _characters.Get(choice.CharacterIndex);
            Vector3 position = center + new Vector3((i % 2 == 0 ? -1f : 1f) * (1f + i / 2), -2f, 0f);
            NetworkObject player = Instantiate(config.PlayerPrefab, position, Quaternion.identity);
            player.GetComponent<ActorStats>().ApplyConfig(config.Stats);
            player.GetComponent<ActorHealth>().ResetHealth();
            player.GetComponent<ActorSanity>().RestoreSanity(config.Stats.MaxSanity);
            player.GetComponent<NetworkSpellHolder>().SetInitialCharacter(choice.CharacterIndex);
            player.SpawnAsPlayerObject(choice.ClientId, true);
        }
        NetworkManager.GetComponent<NetworkWaveDirector>().StartCycle();
        OpenScreenRpc();
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void OpenScreenRpc() => SceneTransition.Instance.Open();
}
