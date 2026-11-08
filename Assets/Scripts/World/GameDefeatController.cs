using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkManager))]
public sealed class GameDefeatController : MonoBehaviour
{
    [SerializeField, Min(1f)] private float _defeatScreenDuration = 5f;
    private readonly HashSet<ActorHealth> _registeredPlayers = new();
    private NetworkManager _networkManager;
    private ActorHealth _crystalHealth;
    private int _downedPlayersCount;
    private bool _isMatchActive;
    private bool _isDefeated;

    private void OnEnable()
    {
        _networkManager = GetComponent<NetworkManager>();
        _networkManager.OnServerStarted += BeginMatch;
        _networkManager.OnServerStopped += OnServerStopped;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Arena01" && _networkManager.IsServer)
            BeginMatch();
        else if (scene.name == "MainMenu")
            ClearMatch();
    }

    public void BeginMatch()
    {
        if (SceneManager.GetActiveScene().name != "Arena01")
            return;
        ClearMatch();
        _isMatchActive = true;
        Crystal crystal = FindFirstObjectByType<Crystal>();
        if (crystal != null)
        {
            _crystalHealth = crystal.GetComponent<ActorHealth>();
            _crystalHealth.Died += Defeat;
        }
        foreach (NetworkClient client in _networkManager.ConnectedClients.Values)
            if (client.PlayerObject != null)
                RegisterPlayer(client.PlayerObject.GetComponent<ActorHealth>());
    }

    public void RegisterPlayer(ActorHealth playerHealth)
    {
        if (!_networkManager.IsServer || !_isMatchActive || !_registeredPlayers.Add(playerHealth))
            return;
        playerHealth.Died += OnPlayerDowned;
        playerHealth.Revived += OnPlayerRevived;
        if (playerHealth.IsDead)
            _downedPlayersCount++;
    }

    public void UnregisterPlayer(ActorHealth playerHealth)
    {
        if (!_registeredPlayers.Remove(playerHealth))
            return;
        playerHealth.Died -= OnPlayerDowned;
        playerHealth.Revived -= OnPlayerRevived;
        if (playerHealth.IsDead)
            _downedPlayersCount--;
        CheckDefeat();
    }

    private void OnPlayerDowned()
    {
        _downedPlayersCount++;
        CheckDefeat();
    }

    private void OnPlayerRevived() => _downedPlayersCount--;

    private void CheckDefeat()
    {
        if (_registeredPlayers.Count > 0 && _downedPlayersCount == _registeredPlayers.Count)
            Defeat();
    }

    private void Defeat()
    {
        if (!_networkManager.IsServer || !_isMatchActive || _isDefeated)
            return;
        _isDefeated = true;
        WaveDirector waveDirector = GetComponent<WaveDirector>();
        int completedWaves = Mathf.Max(0, waveDirector.WaveCount - (waveDirector.CurrentState is WaveStartState ? 1 : 0));
        waveDirector.Stop();
        foreach (ActorHealth playerHealth in _registeredPlayers)
        {
            playerHealth.GetComponent<PlayableActorMovement>().Stop();
            playerHealth.GetComponent<NetworkPlayerSetup>().ShowDefeatRpc(completedWaves);
        }
        StartCoroutine(ReturnToLobby());
    }

    private IEnumerator ReturnToLobby()
    {
        yield return new WaitForSecondsRealtime(_defeatScreenDuration);
        if (GameLobby.Instance == null)
            GetComponent<LobbyConnection>().SpawnLobby();
        GameLobby.Instance.ReturnToLobby();
    }

    private void OnServerStopped(bool wasHost) => ClearMatch();

    private void ClearMatch()
    {
        StopAllCoroutines();
        if (_crystalHealth != null)
            _crystalHealth.Died -= Defeat;
        _crystalHealth = null;
        foreach (ActorHealth playerHealth in _registeredPlayers)
        {
            if (playerHealth == null)
                continue;
            playerHealth.Died -= OnPlayerDowned;
            playerHealth.Revived -= OnPlayerRevived;
        }
        _registeredPlayers.Clear();
        _downedPlayersCount = 0;
        _isMatchActive = false;
        _isDefeated = false;
    }

    private void OnDisable()
    {
        _networkManager.OnServerStarted -= BeginMatch;
        _networkManager.OnServerStopped -= OnServerStopped;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ClearMatch();
    }
}
