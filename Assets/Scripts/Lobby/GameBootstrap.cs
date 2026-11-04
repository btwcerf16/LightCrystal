using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public sealed class GameBootstrap : MonoBehaviour
{
    [SerializeField] private NetworkManager _networkPrefab;
    [SerializeField] private SceneTransition _transitionPrefab;

    private void Awake()
    {
        if (SceneTransition.Instance == null)
            Instantiate(_transitionPrefab);
        if (NetworkManager.Singleton != null)
            return;
        NetworkManager manager = Instantiate(_networkPrefab);
        if (SceneManager.GetActiveScene().name == "Arena01")
            manager.gameObject.AddComponent<NetworkDebugLauncher>();
    }
}
